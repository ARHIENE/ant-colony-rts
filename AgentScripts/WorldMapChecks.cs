namespace AntColony.Regression
{
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// 새 Play 세션에서 실행. 테스트 장수/시설/원정 상태는 Play 종료로 복구한다.
public static class WorldMapChecks
{
    static int checks;
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL: " + message); checks++; }
    static void Set(object target, string field, object value)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        { var f = type.GetField(field, Flags); if (f != null) { f.SetValue(target, value); return; } }
        throw new Exception("Missing field " + field);
    }
    static object Get(object target, string field)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        { var f = type.GetField(field, Flags); if (f != null) return f.GetValue(target); }
        return "missing";
    }
    static T Template<T>() where T : Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .First(t => t.name.EndsWith("Template"));
    static async Task<bool> Wait(Func<bool> condition, int seconds = 25)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(25);
        return condition();
    }
    static void Move(CommanderAnt c, Vector3 position)
    {
        c.CommandStop();
        c.Agent.enabled = false;
        Check(NavMesh.SamplePosition(position, out var hit, 8, NavMesh.AllAreas), "walkable test position");
        c.transform.position = hit.position;
        c.ApplyMovementMode();
        Physics.SyncTransforms();
    }

    public static async Task<string> Main()
    {
        Check(Application.isPlaying, "Play mode required");
        checks = 0;
        var world = WorldMapManager.Instance;
        Check(world != null && world.Sites.Count == 3, "three expedition sites configured");
        var home = Object.FindFirstObjectByType<QueenChamber>();
        Check(Object.FindObjectsByType<EnemyColony>().All(c => Vector3.Distance(c.transform.position, home.Position) > 1000), "no local colony");
        Check(Object.FindObjectsByType<AntColony.Boss.BossHealth>().All(b => Vector3.Distance(b.Position, home.Position) > 1000), "no local boss");
        Check(!Object.FindObjectsByType<ColonyInvasion>().Any(i => i.enabled), "old scaling invasions disabled");
        Check(!world.Unlocked && !world.VehicleResearched && !world.AircraftResearched, "science and world map begin locked");
        var homeCommander = CommanderRoster.Instance.Commanders[0];
        Check(!homeCommander.CanReach(world.Sites[0].Landing), "home cannot walk into expedition site");
        Check(!world.ViewSite(world.Sites[0]), "cannot view unvisited battlefield");
        var upkeep = Object.FindFirstObjectByType<UpkeepManager>();
        upkeep.enabled = false;

        var resources = ResourceManager.Instance;
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        { resources.AddCapacity(type, 10000); resources.Add(type, 8000); }
        var pool = AntPool.Instance;
        var placement = Object.FindFirstObjectByType<BuildingPlacementController>();
        var selection = Object.FindFirstObjectByType<SelectionManager>();
        selection.ClearSelection();
        typeof(SelectionManager).GetMethod("AddToSelection", Flags).Invoke(selection, new object[] { homeCommander.GetComponent<SelectableObject>() });
        Check(!placement.BeginScienceLabPlacement(), "cannot rush science before prerequisites");
        pool.Breed(100);
        Check(!ScienceLab.PrerequisitesMet, "population alone does not unlock science");
        typeof(GameManager).GetProperty("FishingUnlocked").SetValue(GameManager.Instance, true);
        Check(!ScienceLab.PrerequisitesMet, "fishing alone does not bypass barracks tier");
        var barracks = Object.Instantiate(Template<Barracks>(), home.Position + Vector3.right * 12, Quaternion.identity);
        Set(barracks, "currentTier", 2);
        barracks.gameObject.SetActive(true);
        Check(ScienceLab.PrerequisitesMet && placement.BeginScienceLabPlacement(), "combined prerequisites unlock placement");
        Check(world.CanCreateTransport(homeCommander.Position, out var labPos), "room for science lab");
        Move(homeCommander, labPos + Vector3.right * 3);
        var free = pool.Free;
        var food = resources.GetAmount(ResourceType.Food);
        Set(placement, "placementValid", true);
        typeof(BuildingPlacementController).GetMethod("TryPlace", Flags).Invoke(placement, new object[] { labPos + Vector3.up, labPos });
        Check(pool.Reserved == 8 && pool.Free == free - 8 && resources.GetAmount(ResourceType.Food) == food - 100,
            "science construction pays and reserves once");
        Check(await Wait(() => Object.FindObjectsByType<ScienceLab>().Any(l => l.isActiveAndEnabled)), "commander actually completes science lab");
        Check(pool.Reserved == 0 && pool.Free == free, "construction workforce returns");
        var lab = Object.FindFirstObjectByType<ScienceLab>();
        Check(!lab.TryResearch(true) && !lab.TryConstruct(false), "aircraft and construction blocked before vehicle research");
        Check(lab.TryResearch(false) && !lab.TryResearch(false), "research starts once");
        lab.gameObject.SetActive(false);
        Check(world.Researcher == null && !world.VehicleResearched, "disabled lab cancels incomplete research");
        lab.gameObject.SetActive(true);
        Check(lab.TryResearch(false), "canceled research can restart");
        lab.Tick(ScienceLab.ResearchSeconds);
        Check(world.VehicleResearched && world.Researcher == null && !world.Unlocked, "research alone does not open world map");
        Check(lab.TryConstruct(false), "vehicle construction starts");
        lab.Tick(ScienceLab.ConstructionSeconds);
        Check(world.Unlocked && world.Transports.Count == 1, "first completed transport opens map");
        var vehicle = world.Transports[0];
        Check(vehicle.Capacity == 40 && !vehicle.TryDepart(world.Sites[0]), "empty vehicle cannot depart");
        Check(lab.TryResearch(true), "aircraft research requires completed vehicle technology");
        lab.Tick(ScienceLab.ResearchSeconds);
        Check(lab.TryConstruct(true), "aircraft construction starts");
        lab.Tick(ScienceLab.ConstructionSeconds);
        Check(world.Transports.Count == 2 && world.Transports[1].Capacity == 100, "multiple transports with separate capacities");
        var aircraft = world.Transports[1];

        homeCommander.CommandStop();
        homeCommander.TrySetRole(UnitRole.Worker);
        homeCommander.WorkProficiency.AddGathered(200);
        homeCommander.Progression.AddXp(250);
        var level = homeCommander.Progression.Level;
        var workLevel = homeCommander.WorkProficiency.Level;
        Move(homeCommander, vehicle.Position + Vector3.right * 3);
        var assigned = pool.Assigned;
        var troops = homeCommander.TroopCount;
        Check(!vehicle.TryBoard(new[] { homeCommander, homeCommander }), "duplicate passenger rejected atomically");
        Check(vehicle.TryBoard(new[] { homeCommander }), "nearby selected commander boards");
        Check(homeCommander.IsEmbarked && pool.Assigned == assigned && homeCommander.TroopCount == troops,
            "boarding retains assigned troops");
        Check(!homeCommander.TryAssign(1) && homeCommander.ReturnTroops(1) == 0, "cannot replenish or refund embarked troops");
        Check(!CombatTargeting.IsAlive(homeCommander), "embarked commander is not an attack target");
        Check(!aircraft.TryBoard(new[] { homeCommander }), "commander cannot board twice");
        Check(vehicle.TryDepart(world.Sites[0]) && !vehicle.TryDepart(world.Sites[1]), "departure claims a single destination");
        var second = CommanderRoster.Instance.Commanders[1];
        second.CommandStop();
        if (!second.HasTroops) Check(second.TryAssign(5), "second commander takes troops");
        Move(second, aircraft.Position + Vector3.right * 3);
        Check(aircraft.TryBoard(new[] { second }) && !aircraft.TryDepart(world.Sites[0]), "occupied site rejects another transport");
        Check(aircraft.TryDepart(world.Sites[2]), "another site supports concurrent expedition");
        vehicle.Tick(vehicle.TravelSeconds);
        aircraft.Tick(aircraft.TravelSeconds);
        Check(vehicle.State == ExpeditionState.Deployed && aircraft.State == ExpeditionState.Deployed, "both expeditions arrive");
        Check(!homeCommander.IsEmbarked && homeCommander.Agent.isOnNavMesh
            && homeCommander.Progression.Level == level && homeCommander.WorkProficiency.Level == workLevel,
            "landing restores controls without resetting growth");
        Check(!homeCommander.TryAssign(1) && homeCommander.ReturnTroops(1) == 0, "no remote transfer from home ant pool");
        Check(world.ViewSite(world.Sites[0]) && world.ViewedSite == world.Sites[0], "battlefield camera switch");
        Check(!placement.BeginFarmPlacement(), "expedition is not a new home colony");
        var colony = world.Sites[0].Colony;
        foreach (var enemy in colony.GetComponentsInChildren<BuildingBase>()) enemy.TakeDamage(float.MaxValue);
        // 수비 병력이 남아 있으면 채집 중인 장수를 전멸시킨다. 약탈은 전장을 정리한 뒤의 상황을 검사한다.
        foreach (var defender in Object.FindObjectsByType<WildMonster>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (Vector3.Distance(defender.Position, world.Sites[0].transform.position) < 60) defender.TakeDamage(float.MaxValue);
        await Task.Delay(100);
        Check(world.Sites[0].Cleared && colony.IsDefeated, "destroying buildings clears site");
        var loot = colony.GetComponentsInChildren<ResourceNode>().Single(n => n.ResourceType == ResourceType.Special);
        Check(loot.CanGather, "existing raid stock unlocks");
        Move(homeCommander, loot.transform.position + Vector3.left);
        // 운반 반올림 없이 한 번에 전량을 채집할 병력을 출정 전에 태우는 대신, 작은 노드로 실제 왕복 경로를 확인한다.
        var amount = Mathf.Min(loot.AmountRemaining, homeCommander.Data.carryCapacity * homeCommander.TroopCount);
        Set(loot, "amountRemaining", amount);
        var specialBefore = resources.GetAmount(ResourceType.Special);
        Check(homeCommander.CanReach(vehicle.Position), "transport reachable from loot: obstacle="
            + (vehicle.GetComponent<UnityEngine.AI.NavMeshObstacle>() != null)
            + " sampled=" + NavMesh.SamplePosition(vehicle.Position, out _, 1f, NavMesh.AllAreas));
        homeCommander.CommandGather(loot);
        Check(await Wait(() => !loot.gameObject.activeSelf && !homeCommander.IsCarrying, 60),
            "real gathering deposits into expedition transport: nodeActive=" + loot.gameObject.activeSelf
            + " remaining=" + loot.AmountRemaining + " carrying=" + homeCommander.IsCarrying
            + " canGather=" + loot.CanGather + " cargo=" + vehicle.GetCargo(ResourceType.Special)
            + " commanderPos=" + homeCommander.Position + " nodePos=" + loot.transform.position
            + " transportPos=" + vehicle.Position + " onNavMesh=" + homeCommander.Agent.isOnNavMesh
            + " dest=" + homeCommander.Agent.destination + " pathStatus=" + homeCommander.Agent.pathStatus
            + " pending=" + homeCommander.Agent.pathPending + " remainingDist=" + homeCommander.Agent.remainingDistance
            + " stopDist=" + homeCommander.Agent.stoppingDistance + " isStopped=" + homeCommander.Agent.isStopped
            + " workerState=" + Get(homeCommander, "state") + " deposit=" + Get(homeCommander, "targetDeposit")
            + " transport=" + (homeCommander.Transport == null ? "null" : homeCommander.Transport.name));
        Check(vehicle.GetCargo(ResourceType.Special) == Mathf.RoundToInt(amount)
            && resources.GetAmount(ResourceType.Special) == specialBefore, "loot remains cargo until return");
        Check(homeCommander.WorkProficiency.Progress > 0, "expedition harvesting gives proficiency");
        Check(vehicle.TryReturn(), "crew at landing can return");
        vehicle.Tick(vehicle.TravelSeconds);
        Check(vehicle.State == ExpeditionState.Home && homeCommander.Transport == null && !homeCommander.IsEmbarked,
            "return restores home commander and clears membership");
        Check(resources.GetAmount(ResourceType.Special) == specialBefore + Mathf.RoundToInt(amount), "cargo delivered exactly once");
        vehicle.Tick(vehicle.TravelSeconds);
        Check(resources.GetAmount(ResourceType.Special) == specialBefore + Mathf.RoundToInt(amount), "repeat tick cannot duplicate cargo");
        Check(world.Sites[0].Visitor == null && world.Sites[0].Cleared, "cleared site stays cleared after leaving");
        Check(aircraft.State == ExpeditionState.Deployed && second.Transport == aircraft, "return does not disturb other expedition");
        world.ViewSite(world.Sites[2]);
        var boss = world.Sites[2].Boss;
        boss.TakeDamage(boss.MaxHp);
        await Task.Delay(100);
        var bossLoot = Object.FindObjectsByType<ResourceNode>().Where(n => n.name.StartsWith("BossLoot ")).ToArray();
        Check(world.Sites[2].Cleared && bossLoot.Length == 2, "boss on expedition drops existing loot once");
        foreach (var drop in bossLoot) Check(second.CanReach(drop.transform.position), "boss loot reachable in expedition");
        Check(aircraft.TryReturn(), "second expedition can return independently");
        aircraft.Tick(aircraft.TravelSeconds);

        var camp = Object.Instantiate(Template<PrisonerCamp>(), home.Position + Vector3.left * 12, Quaternion.identity);
        camp.gameObject.SetActive(true);
        var incursions = Object.FindFirstObjectByType<LocalIncursions>();
        Check(incursions.TrySpawn() && incursions.Visitors.Count == 3 && !incursions.TrySpawn(), "rare incursion fixed size and no overlapping waves");
        var countBefore = camp.Count;
        foreach (var visitor in incursions.Visitors.ToArray()) visitor.TakeDamage(1000);
        await Task.Delay(100);
        Check(camp.Count == countBefore + 1, "local invading commander becomes prisoner");
        Check(incursions.TrySpawn() && incursions.Visitors.Count == 3, "later incursion does not grow");
        var ui = Object.FindFirstObjectByType<AntColony.UI.WorldMapPanel>();
        Check(ui != null, "world map UI attached");
        if (!ui.IsOpen) ui.Toggle();
        Canvas.ForceUpdateCanvases();
        foreach (var button in ui.PanelRect.GetComponentsInChildren<UnityEngine.UI.Button>())
        {
            var corners = new Vector3[4];
            ((RectTransform)button.transform).GetWorldCorners(corners);
            var local = ui.PanelRect.InverseTransformPoint(corners[2]);
            Check(ui.PanelRect.rect.Contains(local), "button inside panel: " + button.name);
        }
        return "PASS: " + checks + " world map / science / expedition / cargo / local incursion checks";
    }
}
}
