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
        // 원정 거점이 늘면서 소굴 복제본에도 같은 이름의 자식이 생길 수 있다. 본거지 쪽 템플릿만 고른다.
        .First(t => t.name.EndsWith("Template") && t.GetComponentInParent<ExpeditionSite>(true) == null);
    static async Task<bool> Wait(Func<bool> condition, int seconds = 25)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(25);
        return condition();
    }
    // 귀환 착륙은 본거지에 위협 적이 있으면 보류된다(정상 동작). 검사 중 무작위로 나타난 위협을 치우고, 보급 소비 없이 실제 프레임으로 귀환시킨다.
    static async Task ReturnHome(ExpeditionTransport ship)
    {
        var debt = typeof(ExpeditionTransport).GetProperty("FoodDebt");
        foreach (var m in WildMonster.All.ToArray()) if (m != null && m.Threatening && m.GetComponentInParent<ExpeditionSite>() == null) m.TakeDamage(float.MaxValue);
        await Wait(() => { debt.SetValue(ship, 0f); if (ship.State == ExpeditionState.Returning && ship.PendingCrewOrderCount == 0)
                for (var t = 0; t < 600 && ship.State == ExpeditionState.Returning; t++) { debt.SetValue(ship, 0f); ship.Tick(1f); } // 1초씩: 한 번에 돌리면 그 안에서 보급이 화물을 먹는다
            return ship.State == ExpeditionState.Home; }, 60);
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
        var savedAutoPause = UserSettings.Current.autoPauseThreat;
        UserSettings.Current.autoPauseThreat = false;
        try
        {
        // 메인 메뉴 도입 후에는 새 게임이 지형과 NavMesh를 준비한다.
        if (!GameSession.Instance.GameStarted)
        {
            Check(await Wait(() => !AntColony.Save.SaveSystem.Busy), "main menu ready");
            var team = Enumerable.Range(0, 3).Select(StartingCandidate.Roll).ToArray();
            foreach (var candidate in team) candidate.traits.values.Clear();
            AntColony.Save.SaveSystem.NewGame(new NewGameOptions { startingCount = 3, startingTeam = team });
            Check(await Wait(() => !AntColony.Save.SaveSystem.Busy, 60), "new game ready");
        }
        AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
        checks = 0;
        var world = WorldMapManager.Instance;
        Check(world != null && world.Sites.Count == 33, "thirty-three expedition sites configured");
        Check(world.Sites.Select(s => s.Title).Distinct().Count() == 33, "site names are unique");
        Check(world.Sites.Count(s => s.Kind == ExpeditionSiteKind.Settlement) == 18
            && world.Sites.Count(s => s.Kind == ExpeditionSiteKind.BossNest) == 6
            && world.Sites.Count(s => s.Kind == ExpeditionSiteKind.ResourceSite) == 6, "three playable site types");
        var factions = world.Sites.Where(s => s.Kind == ExpeditionSiteKind.Settlement).GroupBy(s => s.Faction).ToArray();
        Check(factions.Count(g => g.Count() == 3) == 4 && factions.Count(g => g.Count() == 1) == 6,
            "large civilizations and independent colonies coexist");
        Check(factions.All(g => g.Select(s => s.Color).Distinct().Count() == 1), "same civilization shares marker color");
        var difficulties = world.Sites.Select(s => s.Difficulty).ToArray();
        Check(difficulties.All(d => d >= 1 && d <= 3) && difficulties.Distinct().Count() == 3
            , "difficulty is fixed per site, not scaled by map order");
        Check(world.Sites.Where(s => s.Kind == ExpeditionSiteKind.Settlement)
            .All(s => s.GetComponentsInChildren<EnemyCommander>().Length == s.Difficulty), "each settlement defends with its own difficulty");
        var bossTemplate = (AntColony.Boss.BossHealth)Get(world, "bossTemplate");
        Check(world.Sites.Where(s => s.Kind == ExpeditionSiteKind.BossNest)
            .All(s => s.Boss.MaxHp == bossTemplate.MaxHp * s.Difficulty && s.Boss.CurrentHp == s.Boss.MaxHp),
            "boss difficulty scales template health and starts at full health");
        Check(world.Sites.Where(s => s.Kind == ExpeditionSiteKind.ResourceSite)
            .All(s => s.GetComponentsInChildren<ResourceNode>().All(n => n.AmountRemaining == Mathf.Round(100 * s.Difficulty * AntColony.Core.BiomeRules.NodeMultiplier(s.Biome, n.ResourceType)))), // 거점 바이옴 배율
            "neutral stock scales with fixed site difficulty");
        Check(world.Sites.All(s => s.MapPosition.x >= 0 && s.MapPosition.x <= 1 && s.MapPosition.y >= 0 && s.MapPosition.y <= 1)
            && world.Sites.Select(s => s.MapPosition).Distinct().Count() == 33, "map positions are distinct and normalized");
        var home = world.HomePosition; // 불시착 시작에는 비축더미가 없다.
        Check(Object.FindObjectsByType<EnemyColony>().All(c => Vector3.Distance(c.transform.position, home) > 1000), "no local colony");
        Check(Object.FindObjectsByType<AntColony.Boss.BossHealth>().All(b => Vector3.Distance(b.Position, home) > 1000), "no local boss");
        Check(!Object.FindObjectsByType<ColonyInvasion>().Any(i => i.enabled), "old scaling invasions disabled");
        Check(world.Unlocked && !world.VehicleResearched && !world.AircraftResearched, "world map available before vehicle research");
        var homeCommander = CommanderRoster.Instance.Commanders[0];
        Check(!homeCommander.CanReach(world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).Landing), "home cannot walk into expedition site");
        Check(!world.ViewSite(world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement)), "cannot view unvisited battlefield");
        Check(world.Sites.All(s => !s.CanResolveConquest
            && !s.TryResolveConquest(ConquestDisposition.Annexed)), "unconquered sites reject annexation");
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
        pool.Breed(100);
        // 건설 가능 상태는 과학 전제와 별개다. Play 시작 후 경과 시간에 좌우되지 않도록 여기서 맞춘다.
        homeCommander.CommandStop();
        foreach (var c in CommanderRoster.Instance.Commanders)
        {
            c.SetJobEnabled(CommanderJobs.All, false);
            typeof(WorkerAnt).GetMethod("SuspendWork", Flags).Invoke(c, null);
        }
        Check(homeCommander.CanStartConstruction, "civilian builder needs no troops");
        homeCommander.SetJobEnabled(CommanderJobs.Building, true);
        // 건설 작업이 자재를 직접 배송한다. 일반 운반을 켜면 G02 바닥 돌 20개도 재고에 합쳐진다.
        var pile = new GameObject("WorldMapTestStockpile"); pile.transform.position = homeCommander.Position; pile.AddComponent<Stockpile>();
        Check(ScienceLab.PrerequisitesMet, "G06 basic research bench has no population gate");
        typeof(GameManager).GetProperty("FishingUnlocked").SetValue(GameManager.Instance, true);
        Check(ScienceLab.PrerequisitesMet, "basic research bench does not require fishing or barracks");
        var barracks = Object.Instantiate(Template<Barracks>(), home + Vector3.right * 12, Quaternion.identity);
        Set(barracks, "currentTier", 2);
        barracks.gameObject.SetActive(true);
        // 실패 시 어떤 전제가 빠졌는지 바로 드러나도록 조건별 상태를 함께 보고한다.
        var prereq = ScienceLab.PrerequisitesMet;
        Check(prereq && placement.BeginScienceLabPlacement(), "combined prerequisites unlock placement:"
            + " prereq=" + prereq + " pop=" + pool.Total + " fishing=" + GameManager.Instance.FishingUnlocked
            + " barracks=" + barracks.name + "/active=" + barracks.isActiveAndEnabled + "/tier=" + barracks.CurrentTier
            + " tier2Barracks=" + Object.FindObjectsByType<Barracks>(FindObjectsSortMode.None).Count(b => b.isActiveAndEnabled && b.CurrentTier >= 2)
            + " labTemplates=" + Object.FindObjectsByType<ScienceLab>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(l => l.gameObject.scene.IsValid() && l.name.EndsWith("Template"))
            + " viewed=" + (world.ViewedSite == null ? "none" : world.ViewedSite.Title)
            + " selected=" + selection.GetSelectedObjects().Count + " builder=" + homeCommander.CanStartConstruction
            + " troops=" + homeCommander.TroopCount + " role=" + homeCommander.Role + " dead=" + homeCommander.IsDead
            + " transport=" + (homeCommander.Transport != null) + " workerState=" + Get(homeCommander, "state"));
        Check(world.CanCreateTransport(homeCommander.Position, out var labPos), "room for science lab");
        Move(homeCommander, labPos + Vector3.right * 3);
        var free = pool.Free;
        var food = resources.GetAmount(ResourceType.Food);
        var materials = EarlyConstruction.Cost(BuildingKind.ScienceLab).ToDictionary(c => c.type, c => resources.GetAmount(c.type));
        Set(placement, "placementValid", true);
        typeof(BuildingPlacementController).GetMethod("TryPlace", Flags).Invoke(placement, new object[] { labPos + Vector3.up, labPos });
        var labSite = Object.FindObjectsByType<BuildingConstructionSite>().OrderBy(s => (s.Position - labPos).sqrMagnitude).First();
        Check(labSite != null && resources.GetAmount(ResourceType.Food) == food,
            "basic research bench does not charge legacy food cost");
        Check(await Wait(() => Object.FindObjectsByType<ScienceLab>().Any(l => l.isActiveAndEnabled), 120), "commander actually completes science lab"); // 건설 속도는 인력·날씨 보정을 받는다
        Check(EarlyConstruction.Cost(BuildingKind.ScienceLab).All(c => resources.GetAmount(c.type) == materials[c.type] - c.amount), "research bench consumes fixed raw materials once: "
            + string.Join(", ", EarlyConstruction.Cost(BuildingKind.ScienceLab).Select(c => $"{c.type} {materials[c.type]}-{c.amount}->{resources.GetAmount(c.type)}")));
        Check(pool.Reserved == 0 && pool.Free == free, "construction workforce returns");
        var lab = Object.FindFirstObjectByType<ScienceLab>();
        // 과학 트리 도입 후: 연구는 CampaignResearch 공용 진행, 연구소 tier와 배정된 연구 장수가 진행시킨다.
        var research = CampaignResearch.Instance;
        Check(!lab.TryResearch(true) && !lab.TryConstruct(false), "aircraft and construction blocked before vehicle research");
        Check(!lab.TryResearch(false), "vehicle research requires lab tier 2");
        Check(lab.TryUpgrade() && lab.TryResearch(false) && !lab.TryResearch(false), "research starts once");
        var scientist = CommanderRoster.Instance.Commanders.Skip(1).First(x => x.CanDoJob(CommanderJobs.Research) && !x.IsDeployed && !x.IsAwayFromHome); // 무작위 특성이 연구를 막을 수 있다
        scientist.SetJobEnabled(CommanderJobs.Research, true);
        scientist.CommandStop(); // 자율 작업 중이면 배정이 거부된다
        Move(scientist, lab.Position + Vector3.right * 3);
        Check(lab.TryAssign(scientist), "researcher assigned: working=" + scientist.IsWorking + " deployed=" + scientist.IsDeployed + " alloc=" + scientist.CanChangeAllocation + " dist=" + Vector3.Distance(scientist.Position, lab.Position) + " busy=" + lab.Busy + " target=" + lab.Target + " canResearch=" + scientist.CanDoJob(CommanderJobs.Research) + " labBusy=" + scientist.LabUpgradeBusy);
        research.Tick(1);
        Check(research.Progress > 0 && !world.VehicleResearched, "assigned researcher advances shared research");
        lab.gameObject.SetActive(false);
        research.Tick(10000);
        Check(scientist.ScienceAssignment == null && !world.VehicleResearched, "disabled lab releases researcher and stops progress");
        lab.gameObject.SetActive(true);
        Check(lab.TryAssign(scientist), "researcher reassigned after lab returns");
        research.Tick(10000);
        Check(world.VehicleResearched && research.Active == null && world.Unlocked, "vehicle research preserves available world map");
        Check(lab.TryConstruct(false), "vehicle construction starts");
        lab.Tick(ScienceLab.ConstructionSeconds);
        Check(world.Unlocked && world.Transports.Count == 1, "first completed transport opens map");
        var vehicle = world.Transports[0];
        Check(vehicle.Capacity == 40 && !vehicle.TryDepart(world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement)), "empty vehicle cannot depart");
        Check(!lab.TryResearch(true), "aircraft research requires gliding and lab tier 4 (electric era)");
        Check(lab.TryUpgrade() && lab.TryUpgrade() && research.TryStart(ScienceTechnology.Gliding), "gliding research starts");
        research.Tick(10000);
        Check(research.Has(ScienceTechnology.Gliding) && lab.TryResearch(true), "aircraft research requires completed prerequisites");
        research.Tick(10000);
        Check(world.AircraftResearched, "aircraft research completes");
        lab.ReleaseResearcher();
        Check(lab.TryConstruct(true), "aircraft construction starts");
        lab.Tick(ScienceLab.ConstructionSeconds);
        Check(world.Transports.Count == 2 && world.Transports[1].Capacity == 100, "multiple transports with separate capacities");
        var aircraft = world.Transports[1];

        homeCommander.CommandStop();
        // 무기 기반 역할·9종 기술 이후: 성장은 CommanderTalents 경험치로 확인한다.
        homeCommander.GainExperience(CommanderActivity.Gathering, 250);
        var level = homeCommander.Talents.Level(CommanderActivity.Gathering);
        var workXp = homeCommander.Talents.Xp(CommanderActivity.Gathering);
        Move(homeCommander, vehicle.Position + Vector3.right * 3);
        homeCommander.WorkState.duty = CommanderDuty.Deployed;
        Check(homeCommander.TryAssign(5), "expedition crew mobilized");
        var assigned = pool.Assigned;
        var troops = homeCommander.TroopCount;
        Check(!vehicle.TryBoard(new[] { homeCommander, homeCommander }), "duplicate passenger rejected atomically");
        Check(vehicle.TryBoard(new[] { homeCommander }), "nearby selected commander boards");
        Check(homeCommander.IsEmbarked && pool.Assigned == assigned && homeCommander.TroopCount == troops,
            "boarding retains assigned troops");
        Check(!homeCommander.TryAssign(1) && homeCommander.ReturnTroops(1) == 0, "cannot replenish or refund embarked troops");
        Check(!CombatTargeting.IsAlive(homeCommander), "embarked commander is not an attack target");
        Check(!aircraft.TryBoard(new[] { homeCommander }), "commander cannot board twice");
        Check(vehicle.TryDepart(world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement)) && !vehicle.TryDepart(world.Sites.Where(s => s.Kind == ExpeditionSiteKind.Settlement).Skip(1).First()), "departure claims a single destination");
        var second = CommanderRoster.Instance.Commanders[1];
        second.CommandStop();
        second.WorkState.duty = CommanderDuty.Deployed;
        ColonyPopulation.Instance.S.policy = MilitaryPolicy.Total; // 기본 정책 동원 한도(5%)는 첫 원정 장수 5명으로 이미 찼다
        if (!second.HasTroops) Check(second.TryAssign(5), $"second expedition commander takes troops (deployed {second.IsDeployed} away {second.IsAwayFromHome} active {second.isActiveAndEnabled} alloc {second.CanChangeAllocation} orders {second.CanReceiveOrders} carry {second.IsCarrying} free {second.FreeRanks} military {ColonyPopulation.Instance?.AvailableMilitary(AntColony.UI.EnemyAlert.CrisisActive)} pool {AntPool.Instance?.Free})");
        Move(second, aircraft.Position + Vector3.right * 3);
        Check(aircraft.TryBoard(new[] { second }), "second transport boards crew");
        Check(aircraft.TryDepart(world.Sites.First(s => s.Kind == ExpeditionSiteKind.BossNest)), "another site supports concurrent expedition");
        vehicle.Tick(vehicle.TravelSeconds);
        aircraft.Tick(aircraft.TravelSeconds);
        Check(vehicle.State == ExpeditionState.Deployed && aircraft.State == ExpeditionState.Deployed, "both expeditions arrive");
        Check(!homeCommander.IsEmbarked && homeCommander.Agent.isOnNavMesh
            && homeCommander.Talents.Level(CommanderActivity.Gathering) == level && homeCommander.Talents.Xp(CommanderActivity.Gathering) == workXp,
            "landing restores controls without resetting growth");
        Check(!homeCommander.TryAssign(1) && homeCommander.ReturnTroops(1) == 0, "no remote transfer from home ant pool");
        Check(world.ViewSite(world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement)) && world.ViewedSite == world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement), "battlefield camera switch");
        Check(!placement.BeginFarmPlacement(), "expedition is not a new home colony");
        var colony = world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).Colony;
        foreach (var enemy in colony.GetComponentsInChildren<BuildingBase>()) enemy.TakeDamage(float.MaxValue);
        await Task.Delay(100);
        Check(!world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).TryResolveConquest(ConquestDisposition.Annexed), "surviving guards prevent annexation");
        // 수비 병력이 남아 있으면 채집 중인 장수를 전멸시킨다. 약탈은 전장을 정리한 뒤의 상황을 검사한다.
        foreach (var defender in Object.FindObjectsByType<WildMonster>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (Vector3.Distance(defender.Position, world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).transform.position) < 60) defender.TakeDamage(float.MaxValue);
        await Task.Delay(100);
        Check(world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).Cleared && colony.IsDefeated, "destroying buildings clears site");
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
        // 원정지 수동 채집은 한 번 왕복 후 멈춘다. 운반량 < 재고면 빈손일 때 다시 지시한다.
        Check(await Wait(() =>
        {
            if (loot.gameObject.activeSelf && loot.CanGather && !homeCommander.IsCarrying && !homeCommander.IsWorking) homeCommander.CommandGather(loot);
            return !loot.gameObject.activeSelf && !homeCommander.IsCarrying;
        }, 90),
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
        Check(homeCommander.Talents.Level(CommanderActivity.Gathering) > level || homeCommander.Talents.Xp(CommanderActivity.Gathering) > workXp, "expedition harvesting gives proficiency");
        var conquestUi = Object.FindFirstObjectByType<AntColony.UI.WorldMapPanel>();
        if (!conquestUi.IsOpen) conquestUi.Toggle();
        Set(conquestUi, "selectedSite", world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement));
        await Task.Delay(100);
        var annex = conquestUi.PanelRect.GetComponentsInChildren<Transform>(true).First(t => t.name == "Annex").GetComponent<UnityEngine.UI.Button>();
        var abandon = conquestUi.PanelRect.GetComponentsInChildren<Transform>(true).First(t => t.name == "Abandon").GetComponent<UnityEngine.UI.Button>();
        Check(annex.interactable && abandon.interactable, "conquest choices enabled for cleared occupied settlement");
        Check(!world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).TryResolveConquest(ConquestDisposition.Undecided)
            && !world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).TryResolveConquest((ConquestDisposition)99), "invalid conquest choices rejected");
        var foodStock = colony.GetStock(ResourceType.Food);
        var soilStock = colony.GetStock(ResourceType.Soil);
        annex.onClick.Invoke();
        await Task.Delay(100);
        Check(world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).Disposition == ConquestDisposition.Annexed && !annex.interactable && !abandon.interactable,
            "annex button records ownership and disables resolved choices");
        Check(((UnityEngine.UI.Text)Get(conquestUi, "siteInfo")).text.Contains("상태 <b>편입</b>"), "annexed status shown");
        Check(!world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).TryResolveConquest(ConquestDisposition.Annexed)
            && !world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).TryResolveConquest(ConquestDisposition.Abandoned), "conquest choice is one-time");
        Check(colony.GetStock(ResourceType.Food) == foodStock && colony.GetStock(ResourceType.Soil) == soilStock
            && vehicle.GetCargo(ResourceType.Special) == Mathf.RoundToInt(amount)
            && resources.GetAmount(ResourceType.Special) == specialBefore, "annexation preserves stocks and cargo without rewards");
        Check(vehicle.TryReturn(), "crew at landing can return");
        await ReturnHome(vehicle);
        await Wait(() => vehicle.PendingCrewOrderCount == 0 && homeCommander.Transport == null, 30); // G42: 귀환 하차는 대기 지시로 처리된다
        Check(vehicle.State == ExpeditionState.Home && homeCommander.Transport == null && !homeCommander.IsEmbarked,
            $"return restores home commander and clears membership (state {vehicle.State} transport {homeCommander.Transport?.name} embarked {homeCommander.IsEmbarked} pending {vehicle.PendingCrewOrderCount} status {vehicle.CrewOrderStatus} crew {vehicle.Crew.Count} pos {homeCommander.Position} ship {vehicle.Position})");
        Check(resources.GetAmount(ResourceType.Special) == specialBefore + Mathf.RoundToInt(amount), "cargo delivered exactly once");
        vehicle.Tick(vehicle.TravelSeconds);
        Check(resources.GetAmount(ResourceType.Special) == specialBefore + Mathf.RoundToInt(amount), "repeat tick cannot duplicate cargo");
        Check(world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).Visitor == null && world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).Cleared, "cleared site stays cleared after leaving");
        Check(world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).Disposition == ConquestDisposition.Annexed, "annexation persists after return");
        Move(homeCommander, vehicle.Position + Vector3.right * 3);
        Check(vehicle.TryBoard(new[] { homeCommander }) && vehicle.TryDepart(world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement)), "annexed settlement allows revisit");
        vehicle.Tick(vehicle.TravelSeconds);
        Check(vehicle.TryReturn(), "revisited settlement allows return");
        vehicle.Tick(vehicle.TravelSeconds);
        Check(aircraft.State == ExpeditionState.Deployed && second.Transport == aircraft, "return does not disturb other expedition");
        world.ViewSite(world.Sites.First(s => s.Kind == ExpeditionSiteKind.BossNest));
        var boss = world.Sites.First(s => s.Kind == ExpeditionSiteKind.BossNest).Boss;
        Object.FindFirstObjectByType<AntColony.UI.BetaProgress>().BetaScenario = true; // B6: 정식 캠페인은 보스로 끝나지 않는다. 결과 화면은 베타 시나리오에서만
        boss.TakeDamage(boss.MaxHp);
        await Task.Delay(100);
        // 보스 처치는 베타 승리 화면을 띄워 시뮬레이션을 멈춘다. "Continue Colony"와 같이 이어서 진행한다.
        Check(Time.timeScale == 0 && AntColony.UI.GameMenuController.Instance.ScreenName.Contains("VICTORY"), "boss kill shows beta victory");
        typeof(AntColony.UI.GameMenuController).GetField("resumeScale", Flags).SetValue(AntColony.UI.GameMenuController.Instance, 1f);
        AntColony.UI.GameMenuController.Instance.Resume();
        Check(Time.timeScale == 1, "colony continues after beta victory");
        var bossLoot = Object.FindObjectsByType<ResourceNode>().Where(n => n.name.StartsWith("BossLoot ")).ToArray();
        Check(world.Sites.First(s => s.Kind == ExpeditionSiteKind.BossNest).Cleared && bossLoot.Length == 2, "boss on expedition drops existing loot once");
        Check(bossLoot.Single(n => n.ResourceType == ResourceType.Food).AmountRemaining
                == (int)Get(bossTemplate, "foodReward") * world.Sites.First(s => s.Kind == ExpeditionSiteKind.BossNest).Difficulty
            && bossLoot.Single(n => n.ResourceType == ResourceType.Special).AmountRemaining
                == (int)Get(bossTemplate, "specialReward") * world.Sites.First(s => s.Kind == ExpeditionSiteKind.BossNest).Difficulty,
            "boss death drops difficulty-scaled food and special");
        Check(!world.Sites.First(s => s.Kind == ExpeditionSiteKind.BossNest).TryResolveConquest(ConquestDisposition.Annexed)
            && !world.Sites.First(s => s.Kind == ExpeditionSiteKind.BossNest).TryResolveConquest(ConquestDisposition.Abandoned), "boss nest has no settlement conquest choice");
        foreach (var drop in bossLoot) Check(second.CanReach(drop.transform.position), "boss loot reachable in expedition");
        Check(aircraft.TryReturn(), "second expedition can return independently");
        aircraft.Tick(aircraft.TravelSeconds);

        var field = world.Sites.First(s => s.Kind == ExpeditionSiteKind.ResourceSite);
        Check(field.Colony == null && field.Boss == null && field.GetComponentsInChildren<WildMonster>().Length == 0,
            "neutral resource site has no hostile colony or defenders");
        Check(!field.Cleared, "resource site starts with stock");
        Move(homeCommander, vehicle.Position + Vector3.right * 3);
        Check(vehicle.TryBoard(new[] { homeCommander }) && vehicle.TryDepart(field), "resource site accepts expedition");
        vehicle.Tick(vehicle.TravelSeconds);
        Check(world.ViewSite(field), "resource battlefield can be viewed");
        var fieldNodes = field.GetComponentsInChildren<ResourceNode>();
        Check(fieldNodes.Length == 2 && fieldNodes.Any(n => n.ResourceType == ResourceType.Food)
            && fieldNodes.Any(n => n.ResourceType == ResourceType.Soil), "resource field contains food and soil");
        ColonyPopulation.Instance.SetTaxRate(0); // G08 아침 세금이 본거지 식량·흙 비교를 흔들지 않게(검사마다 Play 재시작이라 복원 불필요)
        var homeFood = resources.GetAmount(ResourceType.Food);
        var homeSoil = resources.GetAmount(ResourceType.Soil);
        foreach (var node in fieldNodes)
        {
            Check(node.CanGather && !node.IsRaidLocked && homeCommander.CanReach(node.transform.position),
                "neutral resources are unlocked and reachable");
            Set(node, "amountRemaining", 1f);
            Move(homeCommander, node.transform.position + Vector3.left);
            homeCommander.CommandGather(node);
            // G05 원정 보급은 식량 화물을 먹는다. 반입 보존만 보려고 대기 중 보급 부채를 0으로 둔다.
            var debt = typeof(ExpeditionTransport).GetProperty("FoodDebt");
            Check(await Wait(() => { debt.SetValue(vehicle, 0f); return node.IsDepleted && !homeCommander.IsCarrying; }, 45), "neutral harvesting returns to transport: remaining="
                + node.AmountRemaining + " carrying=" + homeCommander.IsCarrying + " state=" + Get(homeCommander, "state")
                + " deposit=" + Get(homeCommander, "targetDeposit") + " cargo=" + vehicle.CargoLoad + "/" + vehicle.CargoCapacity
                + " troops=" + homeCommander.TroopCount + " ts=" + Time.timeScale + " break=" + homeCommander.PersonalState.mentalBreak + " embarked=" + homeCommander.IsEmbarked + " agentOn=" + homeCommander.Agent.enabled + " navOn=" + homeCommander.Agent.isOnNavMesh + " path=" + homeCommander.Agent.pathStatus + " canGather=" + node.CanGather + " pos=" + homeCommander.Position + " node=" + node.transform.position);
            Check(vehicle.GetCargo(node.ResourceType) == 1, "harvest remains in cargo");
        }
        await Task.Delay(100);
        Check(field.Cleared, "resource site depletes only after all stock is harvested");
        Check(!field.TryResolveConquest(ConquestDisposition.Annexed)
            && !field.TryResolveConquest(ConquestDisposition.Abandoned), "resource site has no settlement conquest choice");
        Check(resources.GetAmount(ResourceType.Food) == homeFood && resources.GetAmount(ResourceType.Soil) == homeSoil,
            $"no remote resource transfer to home (food {resources.GetAmount(ResourceType.Food)} vs {homeFood}, soil {resources.GetAmount(ResourceType.Soil)} vs {homeSoil}, cargo {vehicle.GetCargo(ResourceType.Food)}/{vehicle.GetCargo(ResourceType.Soil)})");
        Check(vehicle.TryReturn(), "resource expedition can return");
        var supplyDebt = typeof(ExpeditionTransport).GetProperty("FoodDebt");
        for (var t = 0f; t < vehicle.TravelSeconds; t += 1f) { supplyDebt.SetValue(vehicle, 0f); vehicle.Tick(Mathf.Min(1f, vehicle.TravelSeconds - t)); } // 귀환 중 보급이 식량 화물을 먹지 않게(G05는 별도 검사)
        Check(resources.GetAmount(ResourceType.Food) == homeFood + 1 && resources.GetAmount(ResourceType.Soil) == homeSoil + 1,
            "neutral resource cargo delivered on return");
        Check(field.Cleared && field.Visitor == null, "depleted site persists after returning");

        var abandoned = world.Sites.Where(s => s.Kind == ExpeditionSiteKind.Settlement).Skip(1).First();
        foreach (var enemy in abandoned.Colony.GetComponentsInChildren<BuildingBase>()) enemy.TakeDamage(float.MaxValue);
        foreach (var guard in abandoned.GetComponentsInChildren<WildMonster>()) guard.TakeDamage(float.MaxValue);
        await Task.Delay(100);
        Check(abandoned.Cleared && !abandoned.TryResolveConquest(ConquestDisposition.Abandoned), "cannot decide remotely without deployed crew");
        Move(homeCommander, vehicle.Position + Vector3.right * 3);
        Check(vehicle.TryBoard(new[] { homeCommander }) && vehicle.TryDepart(abandoned), "undecided conquered settlement allows revisit");
        Check(!abandoned.CanResolveConquest, "outbound transport cannot decide conquest");
        vehicle.Tick(vehicle.TravelSeconds);
        if (!conquestUi.IsOpen) conquestUi.Toggle();
        Set(conquestUi, "selectedSite", abandoned);
        await Task.Delay(100);
        Check(abandon.interactable, "abandon button enabled after arrival");
        abandon.onClick.Invoke();
        await Task.Delay(100);
        Check(abandoned.Disposition == ConquestDisposition.Abandoned && !annex.interactable && !abandon.interactable
            && ((UnityEngine.UI.Text)Get(conquestUi, "siteInfo")).text.Contains("상태 <b>포기</b>"), "abandon button resolves and displays state");
        Check(world.ViewSite(abandoned), "abandonment retains current battlefield access");
        var remainingLoot = abandoned.Colony.GetComponentsInChildren<ResourceNode>().First(n => n.CanGather);
        Set(remainingLoot, "amountRemaining", 1f);
        var deliveredBefore = resources.GetAmount(remainingLoot.ResourceType);
        Move(homeCommander, remainingLoot.transform.position + Vector3.left);
        homeCommander.CommandGather(remainingLoot);
        Check(await Wait(() => { supplyDebt.SetValue(vehicle, 0f); return remainingLoot.IsDepleted && !homeCommander.IsCarrying; }, 45), "abandonment permits finishing current loot"); // 보급이 식량 화물을 먹지 않게
        Check(vehicle.GetCargo(remainingLoot.ResourceType) == 1 && vehicle.TryReturn(), "abandoned expedition returns with cargo");
        await ReturnHome(vehicle);
        Check(resources.GetAmount(remainingLoot.ResourceType) == deliveredBefore + 1
            && abandoned.Visitor == null && abandoned.Disposition == ConquestDisposition.Abandoned, $"abandoned return delivers cargo and retains decision ({remainingLoot.ResourceType} {resources.GetAmount(remainingLoot.ResourceType)} vs {deliveredBefore}+1, state {vehicle.State}, cargo {vehicle.GetCargo(remainingLoot.ResourceType)}, visitor {abandoned.Visitor?.name}, disp {abandoned.Disposition}, cap {resources.GetCapacity(remainingLoot.ResourceType)})");
        Move(homeCommander, vehicle.Position + Vector3.right * 3);
        Check(vehicle.TryBoard(new[] { homeCommander }) && !vehicle.TryDepart(abandoned)
            && vehicle.State == ExpeditionState.Home && abandoned.Visitor == null, "abandoned site rejects new departure without claiming visitor");
        Check(vehicle.TryUnloadCrew(), "failed abandoned departure leaves crew recoverable");

        var camp = Object.Instantiate(Template<PrisonerCamp>(), home + Vector3.left * 12, Quaternion.identity);
        camp.gameObject.SetActive(true);
        var incursions = Object.FindFirstObjectByType<LocalIncursions>();
        Check(incursions.TrySpawnIndependent(3) && incursions.Visitors.Count == 3 && !incursions.TrySpawnIndependent(3), "arrived independent wave has no overlap (G62 dispatch is separate)");
        var countBefore = camp.Count;
        foreach (var visitor in incursions.Visitors.ToArray()) visitor.TakeDamage(1000);
        await Task.Delay(100);
        Check(camp.Count == countBefore + 1, "local invading commander becomes prisoner");
        Check(incursions.TrySpawnIndependent(3) && incursions.Visitors.Count == 3, "later arrived wave does not grow");
        var ui = Object.FindFirstObjectByType<AntColony.UI.WorldMapPanel>();
        Check(ui != null, "world map UI attached");
        if (!ui.IsOpen) ui.Toggle();
        Canvas.ForceUpdateCanvases();
        var map = ui.PanelRect.GetComponentsInChildren<Transform>(true).First(t => t.name == "WorldMap");
        // 3D 행성: 뒷면 거점 마커는 숨는다. 거점마다 행성을 돌려 정면에 오면 마커가 행성 안에 보여야 한다.
        var markers = map.GetComponentsInChildren<UnityEngine.UI.Button>(true)
            .Where(b => world.Sites.Any(s => s.Title == b.name)).ToArray();
        Check(markers.Length == 33, "all thirty-three map markers are available");
        var planet = ui.Planet.Rect;
        for (var i = 0; i < markers.Length; i++)
        {
            markers[i].onClick.Invoke();
            Check((ExpeditionSite)Get(ui, "selectedSite") == world.Sites[i], "marker selects its own site");
            ui.Planet.Focus(world.Sites[i].MapPosition); ui.Planet.SnapToTarget();
            typeof(AntColony.UI.WorldMapPanel).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            var a = (RectTransform)markers[i].transform;
            // 2026-10-24 전체 화면 지구: 마커 중심(화면 좌표)이 지구 이미지 영역 안이면 된다.
            var center = a.TransformPoint(a.rect.center);
            if (!WorldMapManager.SiteRevealed(world.Sites[i])) { Check(!markers[i].gameObject.activeSelf, "unrevealed site marker stays hidden: " + markers[i].name); continue; } // 미발견 거점은 숨김(설계)
            Check(markers[i].gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(planet, center), "focused site marker visible on planet: " + markers[i].name);
        }
        foreach (var button in ui.PanelRect.GetComponentsInChildren<UnityEngine.UI.Button>())
        {
            var corners = new Vector3[4];
            ((RectTransform)button.transform).GetWorldCorners(corners);
            var local = ui.PanelRect.InverseTransformPoint(corners[2]);
            Check(ui.PanelRect.rect.Contains(local), "button inside panel: " + button.name);
        }
        return "PASS: " + checks + " world map / science / expedition / cargo / local incursion checks";
        }
        finally { UserSettings.Current.autoPauseThreat = savedAutoPause; }
    }
}
}

