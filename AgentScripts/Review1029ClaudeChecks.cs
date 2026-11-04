using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
// 2026-10-29 Claude 담당 100항목 수정(45·58·59·95·다이아 역발산/무중생유) 회귀 검사.
public static class Review1029ClaudeChecks
{
    static int count;
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("Review1029Claude " + count + ": " + label); count++; File.WriteAllText("Temp/review1029-claude-progress.txt", count + " " + label); }
    static async Task Wait(Func<bool> condition) { var end = DateTime.UtcNow.AddSeconds(100); while (!condition() && DateTime.UtcNow < end) await Task.Delay(80); Check(condition(), "wait completed"); }
    static object Call(object target, string name, params object[] args) => (target is Type t ? t : target.GetType()).GetMethod(name, Any).Invoke(target is Type ? null : target, args);

    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Review1029Claude-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = false; quiet.autoPauseThreat = quiet.autoPauseDowned = true; UserSettings.Apply(quiet, false);
            await Wait(() => !SaveSystem.Busy);
            var team = Enumerable.Range(0, 3).Select(i => { var x = StartingCandidate.Roll(i); x.traits.values.Clear(); return x; }).ToArray();
            SaveSystem.NewGame(new NewGameOptions { seed = 291029, mapSize = MapSize.Small, startingCount = 3, startingTeam = team }); await Wait(() => !SaveSystem.Busy);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var all = CommanderRoster.Instance.Commanders.ToArray();
            foreach (var p in all) { p.CommandStop(); p.SetJobEnabled(CommanderJobs.All, false); }
            CommanderAnt a = all[0], b = all[1], c = all[2];

            // 45: 전투 시간 경험치는 작업과 같은 초당1(10초당10).
            Check(CommanderProgression.CombatXpPer10Seconds == 10, "combat xp per 10 seconds matches work rate");

            // 58: 살아 있는 배우자가 있으면 다른 상대와 연인·출산 불성립, 배우자가 떠나면 해소.
            void Rel(CommanderAnt x, CommanderAnt y, float v, bool spouse = false) { var r = x.PersonalState.Relation(y.PersonalState.id); r.value = v; r.spouse = spouse; }
            Rel(a, b, 80, true); Rel(b, a, 80, true); Rel(a, c, 80); Rel(c, a, 80);
            Check(a.HasOtherSpouse(c) && !a.HasOtherSpouse(b), "spouse blocks other lover");
            Check(!(bool)Call(typeof(NurseryChamber), "IsLover", a, c), "nursery refuses non-spouse lover pair");
            Check((bool)Call(typeof(NurseryChamber), "IsLover", a, b), "nursery keeps spouse pair");
            var social = b.Social; var before = social.departure; social.departure = DepartureState.Left;
            Check(!a.HasOtherSpouse(c) && (bool)Call(typeof(NurseryChamber), "IsLover", a, c), "departed spouse no longer blocks");
            social.departure = before;
            // 기획 공백: 배우자 사망 뒤 1계절 애도 동안 새 연인 불가.
            Call(a, "RecordDeathMood", b.PersonalState.id, b.CommanderName);
            Check(a.InMourning && !(bool)Call(typeof(NurseryChamber), "IsLover", a, c), "widowed spouse mourns one season");
            a.PersonalState.Relation(b.PersonalState.id).widowedAt = GameCalendar.GameSeconds - GameCalendar.SecondsPerSeason - 1;
            Check(!a.InMourning, "mourning ends after a season");
            Rel(a, c, 0); Rel(c, a, 0);

            // 59: 다른 전장에서 쓰러진 친구 때문에 분노하지 않는다.
            if (c.HasTroops)
            {
                c.PersonalState.rageRemaining = 0;
                Call(c, "StartRevenge", new Vector3(1e6f, 0, 1e6f), false);
                Check(c.PersonalState.rageRemaining == 0, "no revenge for friend on other battlefield");
                Call(c, "StartRevenge", c.Position, false);
                Check(c.PersonalState.rageRemaining > 0, "revenge on same battlefield");
                c.PersonalState.rageRemaining = 0;
            }
            Check(a.BreakChance >= 0 && a.BreakChance <= .4f, "break chance exposed in range");

            // 95: 같은 위협 30초 안 재발·쓰러짐 10초 안 연속은 다시 멈추지 않는다.
            SimulationPause.UserSpeed(1); var tag = Guid.NewGuid().ToString("N"); typeof(SimulationPause).GetField("lastDownPause", Any).SetValue(null, float.NegativeInfinity);
            SimulationPause.Observe(new Dictionary<string, string> { ["threat:" + tag] = "검사: 새 위협" }, true);
            Check(SimulationPause.Reasons.Any(r => r.StartsWith("incident:threat:" + tag)), "first threat pauses");
            SimulationPause.UserSpeed(1); SimulationPause.Observe(new Dictionary<string, string>(), true);
            SimulationPause.Observe(new Dictionary<string, string> { ["threat:" + tag] = "검사: 새 위협" }, true);
            Check(!SimulationPause.Reasons.Any(r => r.StartsWith("incident:")), "returning threat within 30s does not pause again");
            SimulationPause.Observe(new Dictionary<string, string> { ["down:a" + tag] = "x1", ["down:b" + tag] = "x2" }, true);
            Check(SimulationPause.Reasons.Count(r => r.StartsWith("incident:down:")) == 1, "simultaneous downs pause once");
            SimulationPause.UserSpeed(1); SimulationPause.Observe(new Dictionary<string, string>(), true); Time.timeScale = 0;

            // 다이아 역발산: 운반 한도 ×5.
            var load = a.TotalLoadCapacity / a.Traits.CarryMultiplier; a.Traits.values.Add(CommanderTrait.DiamondStrength);
            Check(Mathf.Abs(a.TotalLoadCapacity - load * a.Traits.CarryMultiplier * 5) < .01f, "diamond strength carry x5 after strength bonus");
            a.Traits.values.Remove(CommanderTrait.DiamondStrength);

            // 다이아 무중생유: 재생 없는 자연 자원 고갈 → 30초 뒤 처음 양의 50%, 노드당 1회.
            var node = AntColony.Map.MaterialDeposits.Spawned.FirstOrDefault(n => n != null && n.isActiveAndEnabled && !n.IsLooseCargo && !n.RequiresFishing && n.AmountRemaining > 2
                && (float)typeof(ResourceNode).GetField("regrowSeconds", Any).GetValue(n) <= 0);
            Check(node != null, "natural non-regrowing node found");
            var peak = node.AmountRemaining; Call(node, "NotePeak");
            Call(node, "RestoreState", 0f, 0f, 0f); a.Traits.values.Add(CommanderTrait.DiamondGathering);
            Call(node, "TryRebirth", a);
            Check(node.RebirthUsed && Mathf.Approximately(node.RebirthTimer, ResourceNode.RebirthSeconds) && node.gameObject.activeSelf, "rebirth starts after holder depletes");
            Call(node, "TickRebirth", ResourceNode.RebirthSeconds);
            Check(Mathf.Approximately(node.AmountRemaining, Mathf.Max(1, peak * .5f)), "rebirth restores half of first amount");
            Call(node, "RestoreState", 0f, 0f, 0f); Call(node, "TryRebirth", a);
            Check(node.RebirthTimer == 0, "rebirth only once per node");
            a.Traits.values.Remove(CommanderTrait.DiamondGathering);
            Call(node, "RestoreRebirth", true, 12f, 40f);
            Check(node.RebirthTimer == 12 && node.PeakAmount == 40, "rebirth state restore");
            return "Review1029Claude passed " + count;
        }
        finally
        {
            UserSettings.Apply(settings, false); SaveStorage.RootOverride = root;
        }
    }
}
