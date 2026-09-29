using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// 5단계 오락 (2026-09-28/29): 오락 욕구 감소·부족 기분, 모닥불·도박장, 질림, 다양성 보너스, 장식 근처 +20%, 도박 승패, 저장.
public static class JoyChecks
{
    static int checks;
    static void Check(bool ok, string text) { if (!ok) throw new Exception("FAIL " + text); checks++; }
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static T Build<T>(BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        Check(template != null, "template " + kind);
        Check(NavMesh.SamplePosition(position, out var hit, 10, NavMesh.AllAreas), "building position");
        var go = Object.Instantiate(template, hit.position, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true); return go.GetComponent<T>();
    }
    static void Warp(CommanderAnt c, Vector3 p) { c.CommandStop(); Check(NavMesh.SamplePosition(p, out var hit, 10, NavMesh.AllAreas), "walkable"); Check(c.Agent.Warp(hit.position), "warp"); }
    static void At(float timeOfDay) => GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, timeOfDay);
    static float Mood(CommanderAnt c, string reason) => c.PersonalState.moodFactors.Find(f => f.reason == reason)?.value ?? 0;
    static void Bored(CommanderAnt c, float joy = 20)
    {
        c.PersonalState.joy = new CommanderJoyState { joy = joy };
        c.PersonalState.meal = new CommanderMealState(); c.PersonalState.moodFactors.Clear();
    }
    static void Play(params CommanderAnt[] cs)
    {
        foreach (var c in cs) { c.TickDuty(.01f); Check(c.IsPlaying, "starts playing"); }
        for (var i = 0; i < 30 && cs.Any(c => c.IsPlaying); i++) foreach (var c in cs) c.TickDuty(1);
        Check(cs.All(c => !c.IsPlaying && c.PlaySpot == null), "play finishes and leaves seat");
    }

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode"); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Joy-" + Guid.NewGuid().ToString("N"));
        var made = new System.Collections.Generic.List<GameObject>();
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 260930, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var all = CommanderRoster.Instance.Commanders.Where(c => c.IsColonyMember && !c.IsDead).ToArray();
            foreach (var x in all) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); x.Traits.values.Clear(); }
            var c = all[0]; var d = all[1];
            At(10);

            // 오락은 깨어 있는 동안 줄고, 부족하면 기분이 떨어진다. 시설이 없으면 그냥 지낸다.
            Bored(c, 100); c.TickDuty(60);
            Check(Mathf.Approximately(c.Joy, 100 - GameBalance.JoyPerSecond * 60), "joy decays " + c.Joy);
            Bored(c, 25); c.TickDuty(1);
            Check(Mood(c, "오락 부족") == GameBalance.LowJoyMood && !c.IsPlaying, "low joy mood, no facility");
            Bored(c, 5); c.TickDuty(1);
            Check(Mood(c, "오락 부족") == GameBalance.VeryLowJoyMood, "very low joy mood");

            // 처음부터 2종(모닥불·도박장)을 지을 수 있다.
            Check(ScienceEffects.BuildingUnlocked(BuildingKind.Campfire) && ScienceEffects.BuildingUnlocked(BuildingKind.GamblingDen), "initial recreation unlocked");
            var fire = Build<RecreationSpot>(BuildingKind.Campfire, c.Position + Vector3.right * 8); made.Add(fire.gameObject);
            Check(fire.Data.displayName == "이야기 모닥불" && fire.Data.soilCost == GameBalance.CampfireSoil && fire.GetComponent<Light>() != null, "campfire template with light");
            Warp(c, fire.Position + Vector3.forward * 3);

            // 놀면 +50, 같은 종류는 질린다.
            Bored(c); Play(c);
            Check(Mathf.Abs(c.Joy - (20 - GameBalance.JoyPerSecond * 20 + GameBalance.PlayJoy)) < .1f && c.PersonalState.joy.boredom[0] > 0, "play restores joy " + c.Joy);
            Check(CommanderOverhead.Activity(c) != "오락", "activity cleared after play");
            var first = c.Joy - (20 - GameBalance.JoyPerSecond * 20);
            c.PersonalState.joy.joy = 20; c.PersonalState.joy.retrySeconds = 0; c.PersonalState.meal = new CommanderMealState();
            Play(c);
            var second = c.Joy - (20 - GameBalance.JoyPerSecond * 20);
            Check(second < first - 1, $"boredom reduces gain {first}->{second}");

            // 다른 종류를 쓰면 다양성 보너스.
            var den = Build<RecreationSpot>(BuildingKind.GamblingDen, fire.Position + Vector3.left * 8); made.Add(den.gameObject);
            Check(den.IsGambling && den.Seats == GameBalance.GamblingDenSeats, "gambling den template");
            Object.Destroy(fire.gameObject); await Task.Delay(50);
            Warp(c, den.Position + Vector3.forward * 3); Warp(d, den.Position + Vector3.back * 3);
            Bored(c); Bored(d); c.PersonalState.joy.boredom[0] = .5f;
            // 도박: 함께 한 판 끝나면 한쪽 승리 +5, 한쪽 패배 -3.
            Play(c, d);
            Check(Mood(c, "다양한 오락") == 2 * GameBalance.VarietyMoodPerKind, "variety bonus");
            var wins = new[] { c, d }.Count(x => Mood(x, "도박 승리") == GameBalance.GambleWinMood);
            var losses = new[] { c, d }.Count(x => Mood(x, "도박 패배") == GameBalance.GambleLoseMood);
            Check(wins >= 1 && losses >= 1, $"gambling win/lose {wins}/{losses}");

            // 장식 근처면 회복 +20%.
            Bored(c); var plain = 0f;
            c.PersonalState.joy.boredom[1] = 0; Play(c); plain = c.Joy;
            var deco = Build<Decoration>(BuildingKind.FlowerPot, den.Position + Vector3.right * 4); made.Add(deco.gameObject);
            Check(den.NearDecoration, "decoration near den");
            Bored(c); Play(c);
            Check(c.Joy > plain + GameBalance.PlayJoy * .15f, $"decoration bonus {plain}->{c.Joy}");

            // 배고픔이 오락보다 먼저지만, 노는 중엔 끝까지 논다. 출전하면 자리에서 빠진다.
            Bored(c); c.TickDuty(.01f); Check(c.IsPlaying, "playing");
            c.PersonalState.meal.satiety = 10; c.TickDuty(1);
            Check(c.IsPlaying && !c.IsEating, "hunger waits until play ends");
            typeof(CommanderAnt).GetMethod("Mobilize", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, new object[] { 0, c.Position });
            c.TickDuty(1);
            Check(!c.IsPlaying && c.PlaySpot == null && !den.Users.Contains(c), "deploy leaves seat");
            typeof(CommanderAnt).GetMethod("FinishReturn", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, null);

            // 저장: 오락 상태 왕복·검증, 옛 저장은 오락 100.
            c.PersonalState.joy = new CommanderJoyState { joy = 42, playSeconds = 3, kind = 1 };
            c.PersonalState.joy.boredom[1] = .6f;
            var copy = c.CapturePersonalState();
            Check(copy.joy.joy == 42 && copy.joy.kind == 1 && copy.joy.boredom[1] == .6f && copy.Validate(out _), "joy roundtrip");
            copy.joy.boredom = new float[1];
            Check(!copy.Validate(out _), "bad boredom array rejected");
            var legacy = JsonUtility.FromJson<CommanderPersonalState>("{\"id\":\"x\"}");
            Check(legacy.joy != null && legacy.joy.joy == 100 && legacy.joy.Valid, "old save gets full joy");
            return $"JoyChecks passed: {checks}";
        }
        finally
        {
            foreach (var go in made) if (go != null) Object.Destroy(go);
            SaveStorage.RootOverride = root; Time.timeScale = 1;
        }
    }
}
