namespace AntColony.Regression
{
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using ResourceType = AntColony.Data.ResourceType;
using SelectionManager = AntColony.Units.SelectionManager;
using SelectableObject = AntColony.Units.SelectableObject;

public static class PlayableLoopChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    static void Check(bool condition, string label)
    { if (!condition) throw new Exception("FAIL: " + label); checks++; }
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    static async Task Wait(Func<bool> predicate, string label, int seconds = 30)
    {
        var end = DateTime.UtcNow.AddSeconds(seconds);
        while (!predicate() && DateTime.UtcNow < end) await Task.Delay(50);
        Check(predicate(), label);
    }
    static async Task Ready()
    { await Wait(() => !SaveSystem.Busy, "scene ready", 60); await Task.Delay(300); }
    static void Move(CommanderAnt commander, Vector3 point)
    {
        commander.CommandStop();
        Check(NavMesh.SamplePosition(point, out var hit, 5, NavMesh.AllAreas), "walkable crew position");
        Check(commander.Agent.Warp(hit.position), "crew warp");
    }
    static async Task Carry(CommanderAnt commander)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "CargoCheck"; go.SetActive(false);
        Check(NavMesh.SamplePosition(commander.Position + Vector3.right * 2, out var hit, 5, NavMesh.AllAreas), "resource position");
        go.transform.position = hit.position; go.transform.localScale = Vector3.one * .3f;
        var node = go.AddComponent<ResourceNode>(); node.ConfigureLoot(ResourceType.Food, 1000);
        go.SetActive(true); commander.CommandGather(node);
        await Wait(() => (float)typeof(WorkerAnt).GetField("carriedAmount", Private).GetValue(commander) >= 2f, "actual harvesting starts");
        commander.CommandStop(); Object.Destroy(go);
        Check(commander.IsCarrying && !commander.IsWorking, "stop preserves carried cargo");
    }
    public static async Task<string> Main()
    {
        Check(Application.isPlaying, "Play required"); checks = 0;
        var oldRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "PlayableLoop-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 23456 });
            await Ready();
            Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
            Object.FindAnyObjectByType<LocalIncursions>().enabled = false;
            var progress = Object.FindAnyObjectByType<BetaProgress>();
            Check(!string.IsNullOrEmpty(progress.CurrentObjective), "objective shown"); // 2026-11-01: 인구 조건 삭제(인구 제한 없음) 반영
            var commander = CommanderRoster.Instance.Commanders[0];
            foreach (var c in CommanderRoster.Instance.Commanders)
            {
                c.SetJobEnabled(CommanderJobs.All, false);
                typeof(WorkerAnt).GetMethod("SuspendWork", Private).Invoke(c, null);
            }
            Check(commander.CanStartConstruction, "civilian builder needs no troops");
            var selection = Object.FindAnyObjectByType<SelectionManager>();
            selection.ClearSelection(); Call(selection, "AddToSelection", commander.GetComponent<SelectableObject>());
            var placement = Object.FindAnyObjectByType<BuildingPlacementController>();
            var template = Object.FindObjectsByType<Storage>(FindObjectsInactive.Include).Single(s => s.name == "StorageTemplate");
            // 불시착에는 창고가 없다. 실물 배송 출발점과 건설비를 검사 픽스처로 마련한다.
            var supply = Object.Instantiate(template, commander.Position + Vector3.right * 3, Quaternion.identity);
            supply.name = "LoopSupply"; supply.gameObject.SetActive(true);
            commander.SetJobEnabled(CommanderJobs.Building | CommanderJobs.Hauling, true);
            var foodBonus = template.Data.foodCapacityBonus;
            var resources = ResourceManager.Instance;
            var before = resources.GetCapacity(ResourceType.Food);
            var specialBefore = resources.GetCapacity(ResourceType.Special);
            resources.AddCapacity(ResourceType.Soil, template.Data.soilCost);
            supply.DepositResources(ResourceType.Soil, template.Data.soilCost);
            supply.DepositResources(ResourceType.Food, template.Data.foodCost);
            supply.DepositResources(ResourceType.Special, template.Data.specialCost);
            var storageCount = Object.FindObjectsByType<Storage>().Length;
            BuildScreen.Open(); await Task.Delay(50);
            Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include).Single(b => b.name == "Build Storage").onClick.Invoke();
            await Task.Delay(50);
            Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b => b.name == "Builder 0").onClick.Invoke();
            Check(placement.IsPlacing && !BuildScreen.IsOpen, "build screen enters storage placement");
            placement.CancelPlacement(); Check(resources.GetCapacity(ResourceType.Food) == before, "preview adds no capacity");
            Check(placement.BeginStoragePlacement(), "storage placement API");
            placement.PendingMaterial = ResourceType.Soil; // 픽스처 재고와 실제 배송 재료를 일치시킨다.
            var point = Vector3.zero; var found = false;
            for (var i = 0; i < 64 && !found; i++)
            {
                var candidate = commander.Position + Quaternion.Euler(0, i * 45, 0) * Vector3.forward * (5 + i / 8 * 3);
                if (!NavMesh.SamplePosition(candidate, out var hit, 2, NavMesh.AllAreas) || !commander.CanReach(hit.position)) continue;
                var center = (Vector3)Call(placement, "GetPlacementPosition", template.gameObject, hit.position);
                if ((bool)Call(placement, "HasObstruction", center, null)) continue;
                point = hit.position; found = true;
            }
            Check(found, "clear reachable construction location");
            var free = AntPool.Instance.Free;
            var soil = resources.GetAmount(ResourceType.Soil);
            Set(placement, "placementValid", true);
            Call(placement, "TryPlace", (Vector3)Call(placement, "GetPlacementPosition", template.gameObject, point), point);
            Check(resources.GetAmount(ResourceType.Soil) == soil, "예정지 배치에는 건설비를 차감하지 않음");
            var construction = Object.FindObjectsByType<BuildingConstructionSite>()
                .Single(x => x.BuildingKind == BuildingKind.Storage);
            await Wait(() => construction == null || construction.Started, "실물 배송 후 시공 시작", 60);
            Check(resources.GetAmount(ResourceType.Soil) == soil - template.Data.soilCost, "배송·시공 건설비가 한 번만 빠짐");
            Check(AntPool.Instance.Free == free, "construction borrows no ants");
            Check(resources.GetCapacity(ResourceType.Food) == before, "unfinished storage grants no capacity");
            await Wait(() => Object.FindObjectsByType<Storage>().Length == storageCount + 1, "commander walks and completes warehouse", 60);
            Check(resources.GetAmount(ResourceType.Soil) == soil - template.Data.soilCost, "완공 때 건설비 중복 차감 없음");
            Check(resources.GetCapacity(ResourceType.Food) == before + template.Data.foodCapacityBonus, "food capacity expanded once");
            Check(resources.GetCapacity(ResourceType.Special) == specialBefore + template.Data.specialCapacityBonus, "loot storage expanded");
            Check(AntPool.Instance.Free == free, "builders return to free pool");
            var storage = Object.FindObjectsByType<Storage>().OrderBy(s => Vector3.Distance(s.Position, point)).First();
            await Carry(commander);
            var barracksTemplate = Object.FindObjectsByType<Barracks>(FindObjectsInactive.Include).First(b => b.name.EndsWith("Template"));
            var nonDeposit = Object.Instantiate(barracksTemplate, storage.Position + Vector3.right * 10, Quaternion.identity);
            nonDeposit.gameObject.SetActive(true);
            Check(!commander.TryReturnCargo(nonDeposit), "real non-deposit building rejected");
            Object.Destroy(nonDeposit.gameObject);
            Check(commander.TryReturnCargo(storage), "interrupted cargo resumes to warehouse");
            await Wait(() => !commander.IsCarrying && !commander.IsWorking, "real deposit completes");
            commander.CommandStop(); await Task.Delay(100);
            SimulationPause.UserSpeed(0);
            Check(SaveSystem.TrySave(false, 0, out var error), "built storage save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "built storage load: " + error);
            await Ready();
            SimulationPause.UserSpeed(1);
            resources = ResourceManager.Instance;
            Check(Object.FindObjectsByType<Storage>().Length == storageCount + 1, "warehouse type restored");
            Check(resources.GetCapacity(ResourceType.Food) == before + foodBonus, "load does not duplicate capacity");
            Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
            Object.FindAnyObjectByType<LocalIncursions>().enabled = false;
            commander = CommanderRoster.Instance.Commanders[0];
            var homeStorage = Object.FindObjectsByType<Storage>().First();
            var world = WorldMapManager.Instance;
            var ship = world.CreateTransport(false, commander.Position + Vector3.right * 3);
            progress = Object.FindAnyObjectByType<BetaProgress>();
            // BetaProgress는 11/01부터 단계 목표를 표시한다. 빈 차량 생성만으로 첫 원정이 달성되면 안 된다.
            GoalChain.Refresh(Time.unscaledTime);
            Check(!string.IsNullOrEmpty(progress.CurrentObjective) && (GoalChain.Capture().completed & (1 << 4)) == 0,
                "empty transport preserves current goal and does not complete first expedition");
            commander.WorkState.duty = CommanderDuty.Deployed;
            AntPool.Instance.Breed(100); // 불시착의 시민 0명과 모병제 5%를 반영한 실제 편성 인원.
            Check(commander.TryAssign(2), "assign deployed crew under military policy");
            Move(commander, ship.Position + Vector3.right * 3);
            Check(ship.TryBoard(new[] { commander }) && ship.PendingCrewOrderCount == 0 && ship.Crew.Contains(commander), "board deployed crew");
            GoalChain.Refresh(Time.unscaledTime);
            Check((GoalChain.Capture().completed & (1 << 4)) == 0, "boarding alone does not complete first expedition");
            var site = world.Sites.First(s => s.Kind == ExpeditionSiteKind.ResourceSite);
            Check(ship.TryDepart(site), "depart neutral resource site");
            GoalChain.Refresh(Time.unscaledTime);
            Check((GoalChain.Capture().completed & (1 << 4)) != 0 && progress.CurrentObjective.Contains("원정 이동"), "departure completes milestone and shows travel context");
            ship.Tick(ship.TravelRemainingSeconds);
            Check(!placement, "old scene objects disposed");
            Move(commander, ship.Position + Vector3.right * 4);
            await Carry(commander);
            Check(!commander.TryReturnCargo(homeStorage), "remote delivery to real home storage rejected");
            Check(ship.TryReturn() && ship.IsWithdrawing && ship.State == ExpeditionState.Deployed, "carried resources wait in physical withdrawal");
            ship.Tick(.01f);
            Check(ship.State == ExpeditionState.Deployed && commander.IsCarrying, "carried resources prevent immediate return travel");
            ship.CancelWithdrawal();
            var foodAtHome = resources.GetAmount(ResourceType.Food);
            Check(commander.TryReturnCargo(ship), "resume deposit into assigned transport");
            await Wait(() => !commander.IsCarrying && !commander.IsWorking, "expedition cargo delivered");
            Check(ship.GetCargo(ResourceType.Food) >= 2, "harvested resources remain in cargo");
            Check(resources.GetAmount(ResourceType.Food) == foodAtHome, "expedition cargo is not credited at home yet");
            Check(ship.TryReturn(), "return after delivery");
            Move(commander, ship.Position); // 집결 뒤 탑승 시간과 귀환 이동 시간을 따로 진행한다.
            ship.Tick(ExpeditionWithdraw.BoardSeconds * (commander.TroopCount + 1) + .01f);
            Check(ship.State == ExpeditionState.Returning, "boarding completes before return travel");
            GoalChain.Refresh(Time.unscaledTime);
            Check(progress.CurrentObjective.Contains("귀환") && progress.CurrentObjective.Contains("본거지 도착 후 하역"), "return countdown context");
            ship.Tick(ship.TravelRemainingSeconds + .01f);
            Check(commander.Transport == null && !commander.IsEmbarked, "crew arrives home");
            return "PASS: " + checks + " playable loop / warehouse / cargo / objectives checks";
        }
        finally { SaveStorage.RootOverride = oldRoot; Encyclopedia.ResetCache(); }
    }
}
}
