using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;
using Resource = AntColony.Data.ResourceType;

// 2026-10-09: 대상 우선순위 1~9(9 최우선)·노란 경보(대상 최우선)·빨간 경보(소굴 전체 식사·수면·치료·간호 중단, 작업 금지 유지)·저장.
public static class WorkAlertChecks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static void At(float seconds) => GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, seconds);
    public static async Task<string> Main()
    {
        checks = 0; var previousRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "WorkAlert-" + Guid.NewGuid().ToString("N"));
        try
        {
            Check(Application.isPlaying, "play mode"); await Ready();
            SaveSystem.NewGame(new NewGameOptions { seed = 261010, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            Check(!WorkPriorities.Red, "new game starts without red alert");
            var rm = ResourceManager.Instance;
            foreach (Resource type in new[] { Resource.Food, Resource.Soil, Resource.Special }) { rm.AddCapacity(type, 20000); rm.Add(type, 500); } // 재료 종류는 한도를 공유하므로 기본 3종만(2026-10-10)
            var list = CommanderRoster.Instance.Commanders.Where(x => x.IsColonyMember).ToList();
            foreach (var x in list) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }
            var c = list[0]; At(20);
            var nodes = ResourceNode.Available.Where(n => n.CanGather && CommanderAnt.JobFor(n) == CommanderJobs.Gathering && n.GetComponentInParent<ExpeditionSite>() == null
                && !n.IsRaidLoot && c.TryWorkApproach(n.transform.position, out _)).OrderBy(n => (n.transform.position - c.Position).sqrMagnitude).ToList();
            Check(nodes.Count >= 2, "two reachable nodes");
            var near = nodes[0]; var far = nodes.Last();

            // 대상 우선순위: 기본 5, 1~9로 제한, 높은 대상이 가까운 대상보다 먼저.
            Check(WorkPriorities.Level(near) == WorkPriorities.Default && !WorkPriorities.Yellow(near), "default priority 5");
            WorkPriorities.Set(far, 42, false); Check(WorkPriorities.Level(far) == WorkPriorities.Max, "priority clamps to 9");
            c.SetJobEnabled(CommanderJobs.Gathering, true); c.TickDuty(2);
            Check(c.CurrentResourceNode == far, "higher target priority beats distance");
            c.CommandStop(); WorkPriorities.Set(far, WorkPriorities.Default, false); await Task.Delay(50);
            Check(far.GetComponent<TargetPriority>() == null, "default priority removes marker");
            c.TickDuty(2); Check(c.CurrentResourceNode == near, "equal priority falls back to distance"); c.CommandStop();

            // 노란 경보: 작업 종류 우선순위보다 먼저(건설 5 > 채집 1이어도 노란 경보 채집 대상이 먼저).
            var siteGo = new GameObject("Alert site"); siteGo.transform.position = c.Position;
            var site = siteGo.AddComponent<BuildingConstructionSite>(); site.Initialize(null, 50);
            c.SetJobPriority(CommanderJobs.Building, 5); c.SetJobPriority(CommanderJobs.Gathering, 1);
            c.TickDuty(2); Check(c.ConstructionTarget == site, "job priority picks building without alerts"); c.CommandStop();
            // 같은 작업 우선순위 단계면 작업 종류 순서보다 대상 우선순위가 먼저(2026-10-10 리뷰).
            c.SetJobPriority(CommanderJobs.Building, 3); c.SetJobPriority(CommanderJobs.Gathering, 3); WorkPriorities.Set(site, 1, false); WorkPriorities.Set(far, 9, false);
            c.TickDuty(2); Check(c.CurrentResourceNode == far && c.ConstructionTarget == null, "same job tier compares target priority across jobs"); c.CommandStop();
            WorkPriorities.Set(site, WorkPriorities.Default, false); WorkPriorities.Set(far, WorkPriorities.Default, false); await Task.Delay(50);
            c.SetJobPriority(CommanderJobs.Building, 5); c.SetJobPriority(CommanderJobs.Gathering, 1);
            WorkPriorities.Set(far, WorkPriorities.Level(far), true);
            c.TickDuty(2); Check(c.CurrentResourceNode == far && c.ConstructionTarget == null, "yellow alert target first");
            c.CommandStop(); c.SetJobEnabled(CommanderJobs.Gathering, false);
            c.TickDuty(2); Check(c.CurrentResourceNode == null && c.ConstructionTarget == site, "yellow alert never overrides forbidden job"); c.CommandStop();
            // 노란 경보는 식사·수면을 강제로 끊지 않는다.
            c.SetJobEnabled(CommanderJobs.Gathering, true); At(620); c.TickDuty(1);
            Check(c.IsAsleep && c.CurrentResourceNode == null, "yellow alert does not wake sleepers");

            // 빨간 경보: 잠을 깨워 작업, 금지 작업은 그대로.
            WorkPriorities.SetRed(true);
            Check(ToastManager.VisibleLines.Any(l => l.Contains("빨간 경보")), "red alert notice");
            c.TickDuty(1); Check(!c.IsAsleep && c.CivilianWorkReady, "red alert wakes and allows night work");
            c.TickDuty(2); Check(c.CurrentResourceNode == far || c.ConstructionTarget == site, "red alert works at night");
            c.CommandStop();
            var other = list[1]; other.TickDuty(2); Check(other.CurrentResourceNode == null && other.ConstructionTarget == null, "red alert keeps forbidden jobs forbidden");
            Check(!c.CanRest && !c.CanSendToTreatment, "red alert blocks rest and treatment");
            // 식사 중이어도 멈춘다.
            var meal = c.PersonalState.meal; meal.eatSeconds = 5; meal.satiety = 0; c.TickDuty(1);
            Check(meal.eatSeconds == 0 && !c.IsEating, "red alert stops eating");
            // 해제하면 정상 생활(밤이면 다시 잠).
            WorkPriorities.SetRed(false); c.CommandStop(); c.TickDuty(1);
            Check(c.IsAsleep, "release returns to normal life");
            Check(!ToastManager.VisibleLines.Any(l => l.Contains("빨간 경보")), "notice cleared");

            // 저장·불러오기: 대상 우선순위·노란 경보·빨간 경보 유지.
            At(20); c.TickDuty(1); foreach (var x in list) x.CommandStop();
            Object.Destroy(siteGo); await Task.Delay(50);
            WorkPriorities.Set(near, 8, false); WorkPriorities.SetRed(true);
            var nearPos = near.transform.position; var farPos = far.transform.position;
            Check(SaveSystem.TrySave(false, 0, out var error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
            var loadedNear = ResourceNode.Available.First(n => (n.transform.position - nearPos).sqrMagnitude < .01f);
            var loadedFar = ResourceNode.Available.First(n => (n.transform.position - farPos).sqrMagnitude < .01f);
            Check(WorkPriorities.Level(loadedNear) == 8 && WorkPriorities.Yellow(loadedFar) && WorkPriorities.Red, "priorities and alerts survive load");
            WorkPriorities.SetRed(false);

            // UI: 기본 명령 카드에 우선 지정·단계·빨간 경보.
            var names = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(b => b.name).ToList();
            Check(names.Contains("Colony Target Priority") && names.Contains("Colony Priority Level") && names.Contains("Colony Red Alert"), "command card slots");
            GatherDesignation.PriorityLevel = 9; GatherDesignation.CyclePriority(); Check(GatherDesignation.PriorityLabel == "노란 경보", "level cycles to yellow");
            GatherDesignation.CyclePriority(); Check(GatherDesignation.PriorityLevel == 1, "yellow cycles back to 1");
            return "PASS " + checks + " WorkAlert checks";
        }
        finally { Time.timeScale = 0; WorkPriorities.SetRed(false); SaveStorage.RootOverride = previousRoot; }
    }
}
