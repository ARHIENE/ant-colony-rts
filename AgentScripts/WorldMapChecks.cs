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
        // 메인 메뉴 도입 후에는 새 게임이 지형과 NavMesh를 준비한다.
        if (!GameSession.Instance.GameStarted)
        {
            Check(await Wait(() => !AntColony.Save.SaveSystem.Busy), "main menu ready");
            AntColony.Save.SaveSystem.NewGame(new NewGameOptions());
            Check(await Wait(() => !AntColony.Save.SaveSystem.Busy, 60), "new game ready");
        }
        checks = 0;
        var world = WorldMapManager.Instance;
        Check(world != null && world.Sites.Count == 30, "thirty expedition sites configured");
        Check(world.Sites.Select(s => s.Title).Distinct().Count() == 30, "site names are unique");
        Check(world.Sites.Count(s => s.Kind == ExpeditionSiteKind.Settlement) == 18
            && world.Sites.Count(s => s.Kind == ExpeditionSiteKind.BossNest) == 6
            && world.Sites.Count(s => s.Kind == ExpeditionSiteKind.ResourceSite) == 6, "three playable site types");
        var factions = world.Sites.Where(s => s.Kind == ExpeditionSiteKind.Settlement).GroupBy(s => s.Faction).ToArray();
        Check(factions.Count(g => g.Count() == 6) == 2 && factions.Count(g => g.Count() == 1) == 6,
            "large civilizations and independent colonies coexist");
        Check(factions.All(g => g.Select(s => s.Color).Distinct().Count() == 1), "same civilization shares marker color");
        var difficulties = world.Sites.Select(s => s.Difficulty).ToArray();
        Check(difficulties.All(d => d >= 1 && d <= 3) && difficulties.Distinct().Count() == 3
            && !difficulties.SequenceEqual(difficulties.OrderBy(d => d)), "difficulty is fixed per site, not scaled by map order");
        Check(world.Sites.Where(s => s.Kind == ExpeditionSiteKind.Settlement)
            .All(s => s.GetComponentsInChildren<EnemyCommander>().Length == s.Difficulty), "each settlement defends with its own difficulty");
        var bossTemplate = (AntColony.Boss.BossHealth)Get(world, "bossTemplate");
        Check(world.Sites.Where(s => s.Kind == ExpeditionSiteKind.BossNest)
            .All(s => s.Boss.MaxHp == bossTemplate.MaxHp * s.Difficulty && s.Boss.CurrentHp == s.Boss.MaxHp),
            "boss difficulty scales template health and starts at full health");
        Check(world.Sites.Where(s => s.Kind == ExpeditionSiteKind.ResourceSite)
            .All(s => s.GetComponentsInChildren<ResourceNode>().All(n => n.AmountRemaining == 100 * s.Difficulty)),
            "neutral stock scales with fixed site difficulty");
        Check(world.Sites.All(s => s.MapPosition.x >= 0 && s.MapPosition.x <= 1 && s.MapPosition.y >= 0 && s.MapPosition.y <= 1)
            && world.Sites.Select(s => s.MapPosition).Distinct().Count() == 30, "map positions are distinct and normalized");
        var home = Object.FindFirstObjectByType<QueenChamber>();
        Check(Object.FindObjectsByType<EnemyColony>().All(c => Vector3.Distance(c.transform.position, home.Position) > 1000), "no local colony");
        Check(Object.FindObjectsByType<AntColony.Boss.BossHealth>().All(b => Vector3.Distance(b.Position, home.Position) > 1000), "no local boss");
        Check(!Object.FindObjectsByType<ColonyInvasion>().Any(i => i.enabled), "old scaling invasions disabled");
        Check(!world.Unlocked && !world.VehicleResearched && !world.AircraftResearched, "science and world map begin locked");
        var homeCommander = CommanderRoster.Instance.Commanders[0];
        Check(!homeCommander.CanReach(world.Sites[0].Landing), "home cannot walk into expedition site");
        Check(!world.ViewSite(world.Sites[0]), "cannot view unvisited battlefield");
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
        Check(!placement.BeginScienceLabPlacement(), "cannot rush science before prerequisites");
        pool.Breed(100);
        // 건설 가능 상태는 과학 전제와 별개다. Play 시작 후 경과 시간에 좌우되지 않도록 여기서 맞춘다.
        homeCommander.CommandStop();
        if (!homeCommander.HasTroops) Check(homeCommander.TryAssign(5), "builder takes troops");
        Check(!ScienceLab.PrerequisitesMet, "population alone does not unlock science");
        typeof(GameManager).GetProperty("FishingUnlocked").SetValue(GameManager.Instance, true);
        Check(!ScienceLab.PrerequisitesMet, "fishing alone does not bypass barracks tier");
        var barracks = Object.Instantiate(Template<Barracks>(), home.Position + Vector3.right * 12, Quaternion.identity);
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
        Set(placement, "placementValid", true);
        typeof(BuildingPlacementController).GetMethod("TryPlace", Flags).Invoke(placement, new object[] { labPos + Vector3.up, labPos });
        Check(pool.Reserved == 8 && pool.Free == free - 8 && resources.GetAmount(ResourceType.Food) == food - 100,
            "science construction pays and reserves once");
        Check(await Wait(() => Object.FindObjectsByType<ScienceLab>().Any(l => l.isActiveAndEnabled)), "commander actually completes science lab");
        Check(pool.Reserved == 0 && pool.Free == free, "construction workforce returns");
        var lab = Object.FindFirstObjectByType<ScienceLab>();
        // 과학 트리 도입 후: 연구는 CampaignResearch 공용 진행, 연구소 tier와 배정된 연구 장수가 진행시킨다.
        var research = CampaignResearch.Instance;
        Check(!lab.TryResearch(true) && !lab.TryConstruct(false), "aircraft and construction blocked before vehicle research");
        Check(!lab.TryResearch(false), "vehicle research requires lab tier 2");
        Check(lab.TryUpgrade() && lab.TryResearch(false) && !lab.TryResearch(false), "research starts once");
        var scientist = CommanderRoster.Instance.Commanders[2];
        Move(scientist, lab.Position + Vector3.right * 3);
        Check(lab.TryAssign(scientist), "researcher assigned");
        research.Tick(1);
        Check(research.Progress > 0 && !world.VehicleResearched, "assigned researcher advances shared research");
        lab.gameObject.SetActive(false);
        research.Tick(10000);
        Check(scientist.ScienceAssignment == null && !world.VehicleResearched, "disabled lab releases researcher and stops progress");
        lab.gameObject.SetActive(true);
        Check(lab.TryAssign(scientist), "researcher reassigned after lab returns");
        research.Tick(10000);
        Check(world.VehicleResearched && research.Active == null && !world.Unlocked, "research alone does not open world map");
        Check(lab.TryConstruct(false), "vehicle construction starts");
        lab.Tick(ScienceLab.ConstructionSeconds);
        Check(world.Unlocked && world.Transports.Count == 1, "first completed transport opens map");
        var vehicle = world.Transports[0];
        Check(vehicle.Capacity == 40 && !vehicle.TryDepart(world.Sites[0]), "empty vehicle cannot depart");
        Check(!lab.TryResearch(true), "aircraft research requires gliding and lab tier 3");
        Check(lab.TryUpgrade() && research.TryStart(ScienceTechnology.Gliding), "gliding research starts");
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
            && homeCommander.Talents.Level(CommanderActivity.Gathering) == level && homeCommander.Talents.Xp(CommanderActivity.Gathering) == workXp,
            "landing restores controls without resetting growth");
        Check(!homeCommander.TryAssign(1) && homeCommander.ReturnTroops(1) == 0, "no remote transfer from home ant pool");
        Check(world.ViewSite(world.Sites[0]) && world.ViewedSite == world.Sites[0], "battlefield camera switch");
        Check(!placement.BeginFarmPlacement(), "expedition is not a new home colony");
        var colony = world.Sites[0].Colony;
        foreach (var enemy in colony.GetComponentsInChildren<BuildingBase>()) enemy.TakeDamage(float.MaxValue);
        await Task.Delay(100);
        Check(!world.Sites[0].TryResolveConquest(ConquestDisposition.Annexed), "surviving guards prevent annexation");
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
        Check(homeCommander.Talents.Level(CommanderActivity.Gathering) > level || homeCommander.Talents.Xp(CommanderActivity.Gathering) > workXp, "expedition harvesting gives proficiency");
        var conquestUi = Object.FindFirstObjectByType<AntColony.UI.WorldMapPanel>();
        if (!conquestUi.IsOpen) conquestUi.Toggle();
        Set(conquestUi, "selectedSite", world.Sites[0]);
        await Task.Delay(100);
        var annex = conquestUi.PanelRect.Find("Annex").GetComponent<UnityEngine.UI.Button>();
        var abandon = conquestUi.PanelRect.Find("Abandon").GetComponent<UnityEngine.UI.Button>();
        Check(annex.interactable && abandon.interactable, "conquest choices enabled for cleared occupied settlement");
        Check(!world.Sites[0].TryResolveConquest(ConquestDisposition.Undecided)
            && !world.Sites[0].TryResolveConquest((ConquestDisposition)99), "invalid conquest choices rejected");
        var foodStock = colony.GetStock(ResourceType.Food);
        var soilStock = colony.GetStock(ResourceType.Soil);
        annex.onClick.Invoke();
        await Task.Delay(100);
        Check(world.Sites[0].Disposition == ConquestDisposition.Annexed && !annex.interactable && !abandon.interactable,
            "annex button records ownership and disables resolved choices");
        Check(((UnityEngine.UI.Text)Get(conquestUi, "status")).text.Contains("Annexed"), "annexed status shown");
        Check(!world.Sites[0].TryResolveConquest(ConquestDisposition.Annexed)
            && !world.Sites[0].TryResolveConquest(ConquestDisposition.Abandoned), "conquest choice is one-time");
        Check(colony.GetStock(ResourceType.Food) == foodStock && colony.GetStock(ResourceType.Soil) == soilStock
            && vehicle.GetCargo(ResourceType.Special) == Mathf.RoundToInt(amount)
            && resources.GetAmount(ResourceType.Special) == specialBefore, "annexation preserves stocks and cargo without rewards");
        Check(vehicle.TryReturn(), "crew at landing can return");
        vehicle.Tick(vehicle.TravelSeconds);
        Check(vehicle.State == ExpeditionState.Home && homeCommander.Transport == null && !homeCommander.IsEmbarked,
            "return restores home commander and clears membership");
        Check(resources.GetAmount(ResourceType.Special) == specialBefore + Mathf.RoundToInt(amount), "cargo delivered exactly once");
        vehicle.Tick(vehicle.TravelSeconds);
        Check(resources.GetAmount(ResourceType.Special) == specialBefore + Mathf.RoundToInt(amount), "repeat tick cannot duplicate cargo");
        Check(world.Sites[0].Visitor == null && world.Sites[0].Cleared, "cleared site stays cleared after leaving");
        Check(world.Sites[0].Disposition == ConquestDisposition.Annexed, "annexation persists after return");
        Move(homeCommander, vehicle.Position + Vector3.right * 3);
        Check(vehicle.TryBoard(new[] { homeCommander }) && vehicle.TryDepart(world.Sites[0]), "annexed settlement allows revisit");
        vehicle.Tick(vehicle.TravelSeconds);
        Check(vehicle.TryReturn(), "revisited settlement allows return");
        vehicle.Tick(vehicle.TravelSeconds);
        Check(aircraft.State == ExpeditionState.Deployed && second.Transport == aircraft, "return does not disturb other expedition");
        world.ViewSite(world.Sites[2]);
        var boss = world.Sites[2].Boss;
        boss.TakeDamage(boss.MaxHp);
        await Task.Delay(100);
        // 보스 처치는 베타 승리 화면을 띄워 시뮬레이션을 멈춘다. "Continue Colony"와 같이 이어서 진행한다.
        Check(Time.timeScale == 0 && AntColony.UI.GameMenuController.Instance.ScreenName.Contains("VICTORY"), "boss kill shows beta victory");
        typeof(AntColony.UI.GameMenuController).GetField("resumeScale", Flags).SetValue(AntColony.UI.GameMenuController.Instance, 1f);
        AntColony.UI.GameMenuController.Instance.Resume();
        Check(Time.timeScale == 1, "colony continues after beta victory");
        var bossLoot = Object.FindObjectsByType<ResourceNode>().Where(n => n.name.StartsWith("BossLoot ")).ToArray();
        Check(world.Sites[2].Cleared && bossLoot.Length == 2, "boss on expedition drops existing loot once");
        Check(bossLoot.Single(n => n.ResourceType == ResourceType.Food).AmountRemaining
                == (int)Get(bossTemplate, "foodReward") * world.Sites[2].Difficulty
            && bossLoot.Single(n => n.ResourceType == ResourceType.Special).AmountRemaining
                == (int)Get(bossTemplate, "specialReward") * world.Sites[2].Difficulty,
            "boss death drops difficulty-scaled food and special");
        Check(!world.Sites[2].TryResolveConquest(ConquestDisposition.Annexed)
            && !world.Sites[2].TryResolveConquest(ConquestDisposition.Abandoned), "boss nest has no settlement conquest choice");
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
        var homeFood = resources.GetAmount(ResourceType.Food);
        var homeSoil = resources.GetAmount(ResourceType.Soil);
        foreach (var node in fieldNodes)
        {
            Check(node.CanGather && !node.IsRaidLocked && homeCommander.CanReach(node.transform.position),
                "neutral resources are unlocked and reachable");
            Set(node, "amountRemaining", 1f);
            Move(homeCommander, node.transform.position + Vector3.left);
            homeCommander.CommandGather(node);
            Check(await Wait(() => node.IsDepleted && !homeCommander.IsCarrying, 45), "neutral harvesting returns to transport: remaining="
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
            "no remote resource transfer to home");
        Check(vehicle.TryReturn(), "resource expedition can return");
        vehicle.Tick(vehicle.TravelSeconds);
        Check(resources.GetAmount(ResourceType.Food) == homeFood + 1 && resources.GetAmount(ResourceType.Soil) == homeSoil + 1,
            "neutral resource cargo delivered on return");
        Check(field.Cleared && field.Visitor == null, "depleted site persists after returning");

        var abandoned = world.Sites[1];
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
            && ((UnityEngine.UI.Text)Get(conquestUi, "status")).text.Contains("Abandoned"), "abandon button resolves and displays state");
        Check(world.ViewSite(abandoned), "abandonment retains current battlefield access");
        var remainingLoot = abandoned.Colony.GetComponentsInChildren<ResourceNode>().First(n => n.CanGather);
        Set(remainingLoot, "amountRemaining", 1f);
        var deliveredBefore = resources.GetAmount(remainingLoot.ResourceType);
        Move(homeCommander, remainingLoot.transform.position + Vector3.left);
        homeCommander.CommandGather(remainingLoot);
        Check(await Wait(() => remainingLoot.IsDepleted && !homeCommander.IsCarrying, 45), "abandonment permits finishing current loot");
        Check(vehicle.GetCargo(remainingLoot.ResourceType) == 1 && vehicle.TryReturn(), "abandoned expedition returns with cargo");
        vehicle.Tick(vehicle.TravelSeconds);
        Check(resources.GetAmount(remainingLoot.ResourceType) == deliveredBefore + 1
            && abandoned.Visitor == null && abandoned.Disposition == ConquestDisposition.Abandoned, "abandoned return delivers cargo and retains decision");
        Move(homeCommander, vehicle.Position + Vector3.right * 3);
        Check(vehicle.TryBoard(new[] { homeCommander }) && !vehicle.TryDepart(abandoned)
            && vehicle.State == ExpeditionState.Home && abandoned.Visitor == null, "abandoned site rejects new departure without claiming visitor");
        Check(vehicle.TryUnloadCrew(), "failed abandoned departure leaves crew recoverable");

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
        var map = ui.PanelRect.Find("WorldMap");
        var markers = map.GetComponentsInChildren<UnityEngine.UI.Button>()
            .Where(b => world.Sites.Any(s => s.Title == b.name)).ToArray();
        Check(markers.Length == 30, "all thirty map markers are available");
        for (var i = 0; i < markers.Length; i++)
        {
            markers[i].onClick.Invoke();
            Check((ExpeditionSite)Get(ui, "selectedSite") == world.Sites[i], "marker selects its own site");
            var a = (RectTransform)markers[i].transform;
            var bounds = new Rect(a.anchoredPosition + new Vector2(0, -a.rect.height), a.rect.size);
            for (var j = i + 1; j < markers.Length; j++)
            {
                var b = (RectTransform)markers[j].transform;
                Check(!bounds.Overlaps(new Rect(b.anchoredPosition + new Vector2(0, -b.rect.height), b.rect.size)),
                    "map markers do not overlap");
            }
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
}
}
