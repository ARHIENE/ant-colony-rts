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

// 2단계 낮밤·수면 (2026-09-28): 밤 수면, 야행성, 숙소 정원·배정, 노숙 페널티, 잠 설침, 피로, 밤 조명.
public static class DayNightChecks
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
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
        Check(NavMesh.SamplePosition(position, out var hit, 10, NavMesh.AllAreas), "building position");
        var go = Object.Instantiate(template, hit.position, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true); return go.GetComponent<T>();
    }
    static void Warp(CommanderAnt c, Vector3 p) { c.CommandStop(); Check(NavMesh.SamplePosition(p, out var hit, 10, NavMesh.AllAreas), "walkable"); Check(c.Agent.Warp(hit.position), "warp"); }
    static void At(float timeOfDay, int day = 0) => GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, day * GameCalendar.SecondsPerDay + timeOfDay);
    // 한 밤을 1초 단위로 흘린다(TickDuty가 수면·이동·회복을 처리).
    static void Night(CommanderAnt[] cs, int day, float seconds = 300)
    {
        for (var t = 0f; t < seconds; t += 1) { At(GameCalendar.DaySeconds + t, day); foreach (var c in cs) c.TickDuty(1); }
        At(1, day + 1); foreach (var c in cs) c.TickDuty(1);
    }

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode"); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "DayNight-" + Guid.NewGuid().ToString("N"));
        var dorms = new System.Collections.Generic.List<GameObject>();
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 260928, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var all = CommanderRoster.Instance.Commanders.Where(c => c.IsColonyMember && !c.IsDead).ToArray();
            foreach (var x in all) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); x.Traits.values.Remove(CommanderTrait.Nocturnal); x.Traits.values.Remove(CommanderTrait.Workaholic); }
            var c = all[0]; var home = c.Position;

            // 낮: 일하면 피로가 찬다.
            At(10); c.TickDuty(1);
            Check(!c.IsAsleep && !GameCalendar.IsNight, "awake by day");
            // 휴식 명령은 가까운 휴게실 대신 배정된 숙소로 보낸다.
            var restBed = Build<Dormitory>(BuildingKind.SingleBed, home + Vector3.right * 20); dorms.Add(restBed.gameObject);
            var oldRestRoom = Build<RestRoom>(BuildingKind.RestRoom, home + Vector3.left * 6); dorms.Add(oldRestRoom.gameObject);
            c.PersonalState.sleep.fatigue = 90;
            Check(c.SendToRest() && c.WorkState.resting && Dormitory.Of(c) == restBed, "rest assigns a bed");
            Check((c.Agent.destination - restBed.Position).sqrMagnitude <= 49, $"rest destination is dormitory, not recreation room: dest={c.Agent.destination} bed={restBed.Position} pos={c.Position} pending={c.Agent.pathPending} reach={c.CanReach(restBed.Position)}");
            c.CommandStop(); c.WorkState.resting = false;
            restBed.gameObject.SetActive(false);
            Check(c.SendToRest() && c.WorkState.resting && !c.Agent.hasPath, "no bed rests in place, not recreation room");
            c.CommandStop(); c.WorkState.resting = false;
            oldRestRoom.gameObject.SetActive(false);
            // 밤: 숙소가 없으면 제자리 노숙.
            At(GameCalendar.DaySeconds + 1); c.TickDuty(1);
            Check(c.IsAsleep && c.SleepsRough && CommanderOverhead.Activity(c) == "수면", "sleeps rough at night without dormitory");
            c.PersonalState.sleep.fatigue = 80;
            Night(new[] { c }, 0);
            Check(!c.IsAsleep && c.SleptPoorly && c.PersonalState.moodFactors.Exists(f => f.reason == "노숙" && f.value == GameBalance.RoughSleepMood), "rough night: mood -15, poor sleep");
            Check(c.Fatigue > 0 && c.Fatigue < 80, "rough sleep only partly recovers fatigue " + c.Fatigue);
            Check(Mathf.Approximately(c.WorkFactor, c.Traits.WorkMultiplier * GameBalance.PoorSleepWork), "poor sleep -20% work");

            // 연속 3일 노숙 → 기분 -5(한 달, Phase 3 충성심 대체).
            Night(new[] { c }, 1); Night(new[] { c }, 2);
            Check(c.PersonalState.moodFactors.Exists(f => f.reason == "연속 노숙" && f.value == GameBalance.RoughSleepStreakMood), "third rough night costs mood");

            // 숙소: 정원 4, 한 밤 푹 자면 피로 0·정상 속도.
            var dorm = Build<Dormitory>(BuildingKind.Dormitory, home + Vector3.right * 6); dorms.Add(dorm.gameObject);
            Check(dorm.Data.displayName == "숙소" && GameBalance.DormitoryBeds == 4, "dormitory template");
            var five = all.Take(5).ToArray();
            foreach (var x in five) Warp(x, dorm.Position + Vector3.forward * 3);
            At(GameCalendar.DaySeconds + 1, 3); foreach (var x in five) x.TickDuty(1);
            Check(dorm.Residents.Count == 4 && five.Count(x => x.SleepsRough) == 1, "four beds, fifth sleeps rough");
            c.PersonalState.sleep.fatigue = 90;
            var bedded = five.Where(x => !x.SleepsRough).ToArray();
            foreach (var x in bedded) x.PersonalState.sleep.fatigue = 90;
            Night(five, 3);
            Check(bedded.All(x => x.Fatigue == 0 && !x.SleptPoorly), "full night in dormitory restores fatigue: " + string.Join(",", bedded.Select(x => x.Fatigue)));

            // 배정된 숙소에 도달할 수 없으면 침상 수면으로 계산하지 않는다.
            var dormPosition = dorm.transform.position;
            dorm.transform.position += Vector3.right * 10000;
            At(GameCalendar.DaySeconds + 1, 4); c.TickDuty(1);
            Check(c.SleepsRough && c.PersonalState.sleep.bedSeconds == 0, "unreachable dormitory counts as rough sleep");
            dorm.transform.position = dormPosition;

            // 배우자는 같은 숙소를 고른다.
            var dorm2 = Build<Dormitory>(BuildingKind.Dormitory, home + Vector3.left * 6); dorms.Add(dorm2.gameObject);
            Object.Destroy(dorm.gameObject); await Task.Delay(50);
            var a = all[5]; var b = all[6];
            a.PersonalState.Relation(b.PersonalState.id).spouse = true; b.PersonalState.Relation(a.PersonalState.id).spouse = true;
            var dorm3 = Build<Dormitory>(BuildingKind.Dormitory, home + Vector3.back * 6); dorms.Add(dorm3.gameObject);
            var first = Dormitory.Assign(a); var second = Dormitory.Assign(b);
            Check(first != null && first == second, "spouses share a dormitory");

            // 야행성: 낮에 자고 밤에 일한다.
            var owl = all[7]; owl.Traits.values.Add(CommanderTrait.Nocturnal);
            At(GameCalendar.DaySeconds + 5, 5); owl.TickDuty(1);
            Check(!owl.IsAsleep && owl.IsNocturnal, "nocturnal awake at night");
            At(20, 6); owl.TickDuty(1);
            Check(owl.IsAsleep, "nocturnal sleeps by day");
            owl.Traits.values.Remove(CommanderTrait.Nocturnal);

            // 잠든 장수 출전: 즉시 깨고 '잠 설침', 다음날 설친 잠.
            var sleeper = all[8];
            At(GameCalendar.DaySeconds + 2, 7); sleeper.TickDuty(1);
            Check(sleeper.IsAsleep, "sleeper asleep");
            typeof(CommanderAnt).GetMethod("Mobilize", Private).Invoke(sleeper, new object[] { 0, sleeper.Position });
            Check(!sleeper.IsAsleep && sleeper.IsDeployed && sleeper.PersonalState.moodFactors.Exists(f => f.reason == "잠 설침"), "deploying wakes sleeper with bad-sleep mood");
            typeof(CommanderAnt).GetMethod("FinishReturn", Private).Invoke(sleeper, null);
            At(1, 8); sleeper.TickDuty(1);
            Check(sleeper.SleptPoorly, "interrupted night = poor sleep");

            // 저장: 수면 상태 왕복·검증.
            var copy = sleeper.CapturePersonalState();
            Check(copy.sleep != null && copy.sleep.poorly && copy.Validate(out _), "sleep state saves and validates");
            copy.sleep.fatigue = 500; Check(!copy.Validate(out _), "reject fatigue out of range");

            // 밤 화면: 한낮 0, 한밤 1.
            At(300); Check(DayNightLighting.NightAmount == 0, "noon light");
            At(750); Check(DayNightLighting.NightAmount == 1, "midnight light");
            Check(Object.FindAnyObjectByType<DayNightLighting>() != null, "lighting component present");
            return "PASS " + checks + " day/night checks";
        }
        finally { foreach (var d in dorms) if (d != null) Object.Destroy(d); At(10); SaveStorage.RootOverride = root; Time.timeScale = 0; }
    }
}
