using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
// 2026-10-31 Claude: 전투 다이아(무간·참경·역린·엑스텐, 지휘20=레기온) 획득·효과, 병영 훈련 회귀 검사.
public static class Review1031ClaudeChecks
{
    static int count;
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("Review1031Claude " + count + ": " + label); count++; File.WriteAllText("Temp/review1031-claude-progress.txt", count + " " + label); }
    static async Task Wait(Func<bool> condition) { var end = DateTime.UtcNow.AddSeconds(100); while (!condition() && DateTime.UtcNow < end) await Task.Delay(80); Check(condition(), "wait completed"); }
    static object Call(object target, string name, params object[] args) => (target is Type t ? t : target.GetType()).GetMethod(name, Any).Invoke(target is Type ? null : target, args);
    static object Get(object target, string name) => target.GetType().GetProperty(name, Any).GetValue(target);

    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        var roll = typeof(SpecialtyAcquisition).GetField("Roll", Any); var oldRoll = roll.GetValue(null);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Review1031Claude-" + Guid.NewGuid().ToString("N"));
        GameObject barracksObject = null;
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = false; UserSettings.Apply(quiet, false);
            await Wait(() => !SaveSystem.Busy);
            var team = Enumerable.Range(0, 2).Select(i => { var x = StartingCandidate.Roll(i); x.traits.values.Clear(); return x; }).ToArray();
            SaveSystem.NewGame(new NewGameOptions { seed = 311031, mapSize = MapSize.Small, startingCount = 2, startingTeam = team }); await Wait(() => !SaveSystem.Busy);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var all = CommanderRoster.Instance.Commanders.ToArray();
            foreach (var p in all) { p.CommandStop(); p.SetJobEnabled(CommanderJobs.All, false); }
            CommanderAnt a = all[0], b = all[1];

            // 이름·등급: enum 끝 추가, 다이아 등급.
            Check(TraitRules.Name(CommanderTrait.DiamondDoubleStrike) == "무간" && TraitRules.Name(CommanderTrait.DiamondPierce) == "엑스텐", "combat diamond names");
            Check(TraitRules.Grade(CommanderTrait.DiamondWrath) == TraitGrade.Diamond && (int)CommanderTrait.DiamondDoubleStrike == (int)CommanderTrait.Legion + 1, "appended diamond grade");

            // 획득: 첫 관찰 때 이미 도달한 전투 기술은 소급 제외, 이후 최초 도달은 5% 추첨.
            a.Traits.values.Clear(); SpecialtyAcquisition.Initialize(a); var s = a.Traits.specialties; s.combat = null;
            var levels = a.Talents.levels; levels[(int)CommanderActivity.Melee] = 20; levels[(int)CommanderActivity.Ranged] = 0; levels[(int)CommanderActivity.Command] = 0;
            roll.SetValue(null, (Func<float>)(() => 0f));
            SpecialtyAcquisition.Observe(a);
            Check(s.combat.Length == 3 && s.combat[0] == 1 && !a.Traits.Has(CommanderTrait.DiamondDoubleStrike), "existing combat level is not retro-rolled");
            levels[(int)CommanderActivity.Ranged] = 20; SpecialtyAcquisition.Observe(a);
            Check(s.combat[1] == 4 && a.Traits.Has(CommanderTrait.DiamondPierce), "ranged 20 grants X-ten on success");
            roll.SetValue(null, (Func<float>)(() => .99f)); levels[(int)CommanderActivity.Command] = 20; SpecialtyAcquisition.Observe(a);
            Check(s.combat[2] == 2 && !a.Traits.Has(CommanderTrait.Legion), "failed command roll (Legion) spends lifetime chance");
            s.combat[0] = 0; roll.SetValue(null, (Func<float>)(() => 0f)); SpecialtyAcquisition.Observe(a);
            Check(s.combat[0] == 4 && a.Traits.Has(CommanderTrait.DiamondDoubleStrike), "melee 20 grants one of Mugan/Chamgyeong/Yeokrin");
            Check(SpecialtyProgress.Valid(s) && !SpecialtyProgress.Valid(new SpecialtyProgress { version = 1, combat = new int[2] }), "combat progress validation");
            var copy = SpecialtyProgress.Copy(s);
            Check(copy.combat.SequenceEqual(s.combat), "combat progress json roundtrip");
            Check(SpecialtyProgress.Copy(new SpecialtyProgress { version = 1 }).combat?.Length == 0, "old save combat field reads as empty (retro init)");

            // 참경: 대상 방어 30% 무시분.
            a.Traits.values.Clear(); a.Traits.values.Add(CommanderTrait.DiamondArmorBreak);
            if (a.Role != AntColony.Data.UnitRole.Ranged && (a.Weapon?.AttackRadius ?? 0) <= 0)
                Check(Mathf.Approximately((float)Call(a, "ArmorBreakBonus", b), Mathf.Max(0, b.Armor) * .3f), "armor break ignores 30% target armor");
            a.Traits.values.Clear();
            Check((float)Call(a, "ArmorBreakBonus", b) == 0, "no armor break without trait");

            // 역린: 10m 안 사망 → 20초 ×3, 끝나면 탈진 60초 ×0.5, 탈진 중 재발동.
            a.Traits.values.Add(CommanderTrait.DiamondWrath);
            Call(a, "TryWrath", a.Position + Vector3.right * 50);
            Check(a.PersonalState.wrathRemaining == 0, "wrath ignores far death");
            Call(a, "TryWrath", a.Position);
            Check(a.PersonalState.wrathRemaining == CommanderAnt.WrathSeconds && (float)Get(a, "WrathFactor") == 3, "wrath triples four stats");
            a.PersonalState.Tick(CommanderAnt.WrathSeconds + 1, a.Traits);
            Check(a.PersonalState.wrathRemaining == 0 && a.PersonalState.wrathExhaust == CommanderAnt.WrathExhaustSeconds && (float)Get(a, "WrathFactor") == .5f, "exhaust halves after wrath");
            Call(a, "TryWrath", a.Position);
            Check(a.PersonalState.wrathRemaining == CommanderAnt.WrathSeconds && a.PersonalState.wrathExhaust == 0, "wrath retriggers during exhaust");
            Call(a, "TryWrath", a.Position);
            Check(a.PersonalState.wrathRemaining == CommanderAnt.WrathSeconds * 2, "extra death adds 20s");
            a.PersonalState.wrathRemaining = 115; Call(a, "TryWrath", a.Position);
            Check(a.PersonalState.wrathRemaining == CommanderAnt.WrathMaxSeconds, "wrath capped at 120s");
            Check(a.PersonalState.Validate(out _), "wrath state validates");
            a.PersonalState.wrathRemaining = a.PersonalState.wrathExhaust = 0;
            Check((float)Get(a, "WrathFactor") == 1, "no wrath factor at rest");
            a.Traits.values.Clear();

            // 엑스텐 사거리: 원거리만 ×2.
            a.Traits.values.Add(CommanderTrait.DiamondPierce);
            Check((float)Get(a, "PierceRangeMultiplier") == (a.Role == AntColony.Data.UnitRole.Ranged ? 2f : 1f), "x-ten range only for ranged");
            a.Traits.values.Clear();

            // 병영 훈련: 역할 기술을 작업의 절반 속도로, 한 번 30초.
            barracksObject = new GameObject("Review1031 Barracks"); var barracks = barracksObject.AddComponent<Barracks>();
            Check(barracks.FreeForTraining && barracks.TrainedSkill == CommanderActivity.Melee, "barracks free for melee training");
            Check(!barracks.Train(a, 1), "training needs a started session");
            levels[(int)CommanderActivity.Melee] = 0; a.Talents.experience[(int)CommanderActivity.Melee] = 0;
            barracks.BeginTraining();
            Check(barracks.Train(a, 10) && a.Talents.Xp(CommanderActivity.Melee) > 0, "training grants melee experience");
            Check(barracks.Train(a, 20) && !barracks.Train(a, 1), "session ends after 30s");
            // 배우자 이혼: 관계 -40 이하면 양쪽 배우자 해제.
            a.PersonalState.Relation(b.PersonalState.id).spouse = b.PersonalState.Relation(a.PersonalState.id).spouse = true;
            a.PersonalState.Relation(b.PersonalState.id).value = b.PersonalState.Relation(a.PersonalState.id).value = -30;
            Call(a, "TickRelations", 0f); Call(b, "TickRelations", 0f);
            Check(a.PersonalState.Relation(b.PersonalState.id).spouse, "spouse kept above rival line");
            a.PersonalState.Relation(b.PersonalState.id).value = b.PersonalState.Relation(a.PersonalState.id).value = -40;
            Call(a, "TickRelations", 0f); Call(b, "TickRelations", 0f);
            Check(!a.PersonalState.Relation(b.PersonalState.id).spouse && !b.PersonalState.Relation(a.PersonalState.id).spouse, "divorce at -40 clears both");
            return "Review1031Claude passed " + count;
        }
        finally
        {
            if (barracksObject != null) UnityEngine.Object.Destroy(barracksObject);
            roll.SetValue(null, oldRoll);
            UserSettings.Apply(settings, false); SaveStorage.RootOverride = root;
        }
    }
}
