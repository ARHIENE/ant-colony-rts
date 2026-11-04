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
using Object = UnityEngine.Object;

// Run in Play; files use an isolated RootOverride. Each load has an unscaled deadline.
public static class ImplB3Checks
{
    private static int checks, reloads;
    private static DateTime started;
    private static void Budget(string phase)
    {
        Debug.Log($"B3 {phase}: reloads={reloads}, elapsed={(DateTime.UtcNow - started).TotalSeconds:F1}s, busy={SaveSystem.Busy}, scale={Time.timeScale}");
        Check(DateTime.UtcNow - started < TimeSpan.FromSeconds(300), "300s suite budget: " + phase);
    }
    private static void Check(bool value, string label) { if (!value) throw new Exception("B3 FAIL: " + label); checks++; }
    private static T Copy<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    private static void InactiveMonsterAgent()
    {
        var go = new GameObject("B3 inactive restore regression"); go.SetActive(false);
        try
        {
            var monster = go.AddComponent<WildMonster>();
            Check(monster.Agent != null && monster.Agent == go.GetComponent<UnityEngine.AI.NavMeshAgent>(), "inactive runtime enemy resolves Agent before Awake");
            monster.Agent.enabled = false; // SaveRuntime.RestoreMonsters의 최초 접근과 동일.
            var state = new WildMonsterDto { raidSite = -1, speed = 2.5f, range = 1.5f, maxHealth = 15 };
            monster.RestoreHealth(0);
            typeof(WildMonster).GetMethod("RestoreRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(monster, new object[] { state, false });
            Check(!monster.Agent.enabled && !go.activeSelf && monster.CurrentHealth == 0, "dead runtime enemy restores without activation or Awake");
        }
        finally { Object.Destroy(go); }
    }
    private static async Task Ready()
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(20);
        Budget("load finished");
        Check(!SaveSystem.Busy && GameSession.Instance.GameStarted, "load finished within 30s (see last B3 phase and GameBootstrap errors)");
        GameMenuController.Instance.Pause(); Time.timeScale = 0;
    }
    private static async Task<SaveFileV1> Load(SaveFileV1 file)
    {
        Budget("fixture load " + ++reloads);
        var path = Path.Combine(SaveStorage.Root, "b3-fixture.json");
        Check(SaveValidator.Validate(file, out var error), "fixture validated: " + error);
        SaveStorage.WriteAtomic(path, JsonUtility.ToJson(file));
        Check(SaveSystem.TryLoad(path, out error), "load accepted: " + error);
        await Ready();
        return SaveSnapshot.Capture(); // Never CommandStop: that would conceal lost orders.
    }
    private static async Task<SaveFileV1> Roundtrip(bool auto = false)
    {
        Budget("roundtrip " + ++reloads);
        Check(SaveSystem.TrySave(auto, 0, out var error), "active save accepted: " + error);
        Check(SaveStorage.TryReadText(SaveSlots.PathFor(auto, 0), out var text, out error)
            && SaveValidator.TryParse(text, out _, out error), "written save parses: " + error);
        Check(SaveSystem.TryLoad(SaveSlots.PathFor(auto, 0), out error), "slot load accepted: " + error);
        await Ready(); return SaveSnapshot.Capture();
    }
    private static void Same(float a, float b, string label) => Check(Mathf.Abs(a - b) < .001f, label);
    private static string Equipment(SaveFileV1 f) => string.Join(",", f.equipmentInventory.Select(i => i.id)
        .Concat(f.world.transports.SelectMany(s => s.equipmentCargo.Select(i => i.id))).OrderBy(id => id));
    private static void Ledger(SaveFileV1 a, SaveFileV1 b)
    {
        Check(a.colony.food == b.colony.food && a.colony.soil == b.colony.soil && a.colony.special == b.colony.special
            && a.colony.materials.SequenceEqual(b.colony.materials), "no resource/tool-material consumption on load");
        Check(Equipment(a) == Equipment(b), "reward/equipment ownership unchanged");
        Check(a.randomState == b.randomState, "random stream unchanged by restore");
        Check(a.colony.antsAssigned == b.colony.antsAssigned && a.colony.antsReserved == b.colony.antsReserved, "troop reservations unchanged");
    }
    public static async Task<string> Main()
    {
        checks = reloads = 0; started = DateTime.UtcNow; Check(Application.isPlaying, "Play required");
        InactiveMonsterAgent();
        var previousRoot = SaveStorage.RootOverride;
        var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplB3-" + Guid.NewGuid().ToString("N"));
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(20);
            Check(!SaveSystem.Busy, "initial load finished within 30s");
            var options = new NewGameOptions { seed = 261024, mapSize = MapSize.Small, startingCount = 2,
                startingTeam = Enumerable.Range(0, 2).Select(StartingCandidate.Roll).ToArray() };
            foreach (var candidate in options.startingTeam) candidate.traits.values.Clear();
            SaveSystem.NewGame(options); await Ready();
            var roster = CommanderRoster.Instance;
            var helper = roster.Create("B3 test crafter", CommanderRank.Sergeant, new[] { UnitRole.Worker }, UnitRole.Worker,
                new CommanderTraits(), roster.Commanders[0].Position);
            Check(helper != null, "third fixture commander created after two-commander crash start");
            var crew = roster.Commanders.ToArray();
            foreach (var c in crew) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); }
            var rm = ResourceManager.Instance;
            foreach (var t in new[] { AntColony.Data.ResourceType.Wood, AntColony.Data.ResourceType.Stone }) {
                rm.AddCapacity(t, 1000); rm.Add(t, 100);
            }
            crew[2].SetJobPriority(CommanderJobs.Crafting, 5); crew[2].PersonalState.toolTier = 1;
            Check(crew[2].StartGroundCraft(GroundRecipe.StoneTools), "start tools once"); crew[2].TickGroundCraft(2);
            var paidWood = rm.GetAmount(AntColony.Data.ResourceType.Wood);
            var paidStone = rm.GetAmount(AntColony.Data.ResourceType.Stone);
            var placement = Object.FindAnyObjectByType<BuildingPlacementController>();
            Check(placement.BeginPlacement(BuildingKind.Carpentry, UnitRole.Worker, crew[1]), "construction placement");
            var at = crew[1].Position + Vector3.forward * 6;
            var construction = (BuildingConstructionSite)typeof(BuildingPlacementController).GetMethod("PlaceOne", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(placement, new object[] { at, at, false, null });
            placement.CancelPlacement(); Check(construction != null, "construction site created");
            foreach (var need in construction.Needs) need.delivered = need.need;
            construction.BeginWork(); typeof(BuildingConstructionSite).GetProperty("RemainingWork").SetValue(construction, construction.BuildTimeSeconds / 2);
            crew[1].CommandBuild(construction); Check(crew[1].IsConstructing, "construction in progress");
            var destination = crew[0].Position + Vector3.right * 6;
            crew[0].CommandMove(destination);
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.SetActive(false); go.name = "B3 Intruder";
            go.transform.position = crew[0].Position + Vector3.forward * 3;
            var enemy = go.AddComponent<WildMonster>(); enemy.ConfigureWeakIntruder(); enemy.MakeRaider(); go.SetActive(true); enemy.RestoreHealth(11);
            typeof(WildMonster).GetMethod("Aggro", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(enemy, new object[] { crew[0] });
            var expected = SaveSnapshot.Capture();
            var actual = await Roundtrip(); Ledger(expected, actual);
            var e = actual.monsters.Single(m => m.hasRuntime && m.runtime.name == "B3 Intruder");
            Same(e.health, 11, "runtime invader health"); Check(e.runtime.raider && e.runtime.target == "commander:0", "runtime invader target and allegiance");
            Check(actual.commanders[0].order.soldier.moving, "manual move retained");
            Same(actual.commanders[0].order.soldier.destination.x, destination.x, "move goal x");
            Same(actual.commanders[0].order.soldier.destination.z, destination.z, "move goal z");
            Check(actual.commanders[1].order.worker.construction == expected.commanders[1].order.worker.construction, "construction worker target retained");
            Same(actual.construction.Single().remaining, expected.construction.Single().remaining, "construction progress retained");
            Check(actual.construction.Single().started, "paid construction remains started");
            var crafter = CommanderRoster.Instance.Commanders[2];
            Same(crafter.PersonalState.groundCraft.work, expected.commanders[2].personalState.groundCraft.work, "tool craft progress retained");
            crafter.TickGroundCraft(1000); crafter.TickGroundCraft(1000);
            Check(crafter.PersonalState.toolTier == 2 && !crafter.IsGroundCrafting, "tools complete once");
            Check(rm != ResourceManager.Instance && ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Wood) == paidWood
                && ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Stone) == paidStone, "tool materials paid only before save");
            expected = SaveSnapshot.Capture(); actual = await Roundtrip(true); Ledger(expected, actual);
            Check(actual.commanders[2].personalState.toolTier == 2 && !CommanderRoster.Instance.Commanders[2].IsGroundCrafting, "completed tools not replayed");

            // Material already picked up must remain assigned to its original construction site.
            var fixture = Copy(actual);
            var delivery = fixture.commanders[1]; var materialNeed = fixture.construction.Single().needs[0];
            fixture.construction.Single().started = false; materialNeed.delivered = materialNeed.need - 2; materialNeed.reserved = 0;
            delivery.carriedAmount = 2; delivery.carriedType = (int)materialNeed.type;
            delivery.order.worker.state = 8; delivery.order.worker.construction = null;
            delivery.order.worker.delivery = "construction:0"; delivery.order.worker.fetchType = (int)materialNeed.type; delivery.order.worker.fetchAmount = 2;
            delivery.order.soldier.moving = true; delivery.order.soldier.destination = Copy(fixture.construction.Single().position);
            actual = await Load(fixture); Ledger(fixture, actual);
            var courier = CommanderRoster.Instance.Commanders[1];
            Check(courier.DeliverySite != null && courier.IsCarrying && actual.commanders[1].order.worker.state == 8, "picked material and destination retained");
            Check(Object.FindAnyObjectByType<BuildingConstructionSite>().Couriers.Contains(courier), "construction in-transit reservation relinked");

            // A deployed route with an active collector and a live settlement raid.
            fixture = Copy(actual);
            var site = fixture.world.sites.First(s => s.colony != null && s.colony.buildingHealth.Count > 0);
            site.cleared = true; site.disposition = (int)ConquestDisposition.Annexed;
            site.defenseUnderAttack = true; site.defenseRemaining = 81; site.defenseCaptureProgress = 7;
            // 지도 생성에 따라 100m 안에 자원이 없을 수 있어 가장 가까운 남은 자원을 쓴다.
            var source = fixture.nodes.Where(n => n.exists && n.amount > 0).OrderBy(n => (n.position.ToVector3() - site.colony.rootPosition.ToVector3()).sqrMagnitude).First();
            var liveSite = WorldMapManager.Instance.Sites[site.index];
            var home = WorldMapManager.Instance.HomePosition;
            fixture.world.transports.Add(new TransportDto { state = (int)ExpeditionState.Deployed, siteIndex = site.index,
                position = new Vec3Dto(liveSite.Landing), homePosition = new Vec3Dto(home), cargoFood = 3,
                route = new RouteDto { running = true, destinationIndex = site.index, crew = new System.Collections.Generic.List<int> { 0 },
                    nextNode = new[] { 1 }, nodes = new System.Collections.Generic.List<string> { "node:" + source.key }, status = "Collecting / depositing" } });
            var traveler = fixture.commanders[0]; traveler.location = 1; traveler.transportIndex = fixture.world.transports.Count - 1;
            fixture.colony.antsAssigned += 5 - traveler.troopCount; traveler.troopCount = 5; traveler.pendingDamage = 0;
            traveler.personalState.work.duty = CommanderDuty.Deployed;
            traveler.position = Copy(source.position); traveler.order.soldier.moving = false;
            traveler.order.worker.state = 2; traveler.order.worker.node = "node:" + source.key;
            traveler.carriedAmount = 2; traveler.carriedType = source.type;
            var raider = fixture.monsters.Single(m => m.hasRuntime && m.runtime.name == "B3 Intruder");
            raider.runtime.parentSite = site.index; raider.runtime.raidSite = site.index; raider.runtime.defenseSite = site.index;
            raider.position = new Vec3Dto(liveSite.Landing + Vector3.forward * 3);
            actual = await Load(fixture); Ledger(fixture, actual);
            var transport = WorldMapManager.Instance.Transports.Last();
            var route = actual.world.transports.Last().route;
            Check(route.running && route.nextNode.SequenceEqual(new[] { 1 }) && !route.rallying, "deployed collection cursor retained");
            Check(transport.Crew.Count == 1 && transport.Crew[0].IsWorking && transport.Crew[0].IsCarrying, "collector and cargo retained");
            Check(!transport.TryBoard(new[] { transport.Crew[0] }) && transport.Crew.Count == 1, "boarding cannot complete twice");
            Check(actual.world.sites[site.index].defenseUnderAttack, "defense attack remains active");
            Same(actual.world.sites[site.index].defenseRemaining, 81, "defense timer retained");
            Same(actual.world.sites[site.index].defenseCaptureProgress, 7, "landing capture progress retained");
            Check(WorldMapManager.Instance.Sites[site.index].Defense.Attackers.Count == 1, "raid ownership relinked");
            fixture = Copy(actual); fixture.world.transports.Last().route.rallying = true;
            fixture.commanders[0].order.worker.state = 0; fixture.commanders[0].order.soldier.moving = true;
            fixture.commanders[0].order.soldier.destination = Copy(fixture.world.transports.Last().position);
            actual = await Load(fixture); Ledger(fixture, actual);
            Check(actual.world.transports.Last().route.rallying, "rally stage retained");

            // Pay a real expedition reward once, then load and attempt to collect it again.
            fixture = Copy(actual); fixture.world.sites[site.index].rewardsClaimed = false;
            fixture.diplomacy.owners[site.index] = "";
            fixture.world.transports.Last().route.running = false;
            fixture.commanders[0].order.worker.state = 0; fixture.commanders[0].order.soldier.moving = false;
            fixture.commanders[0].position = new Vec3Dto(fixture.world.transports.Last().position.ToVector3() + Vector3.right * 3);
            actual = await Load(fixture);
            var equipmentBeforeReward = Equipment(actual);
            Check(WorldMapManager.Instance.Transports.Last().TryCollectRewards(), "first expedition reward paid");
            var rewardState = SaveSnapshot.Capture(); Check(Equipment(rewardState) != equipmentBeforeReward, "reward added equipment");
            actual = await Roundtrip(); Ledger(rewardState, actual);
            Check(!WorldMapManager.Instance.Transports.Last().TryCollectRewards(), "claimed rewards rejected after load");
            var noReward = SaveSnapshot.Capture(); Ledger(rewardState, noReward);
            Check(actual.world.sites[site.index].rewardsClaimed, "reward completion persisted");
            var invalid = Copy(actual); invalid.world.transports.Last().route.nextNode[0] = int.MaxValue;
            Check(!SaveValidator.Validate(invalid, out _), "invalid route cursor rejected");
            invalid = Copy(actual); invalid.commanders[0].order.soldier.attackTimer = float.NaN;
            Check(!SaveValidator.Validate(invalid, out _), "invalid combat timer rejected");
            invalid = Copy(actual); invalid.commanders[0].order.worker.node = "node:missing";
            Check(!SaveValidator.Validate(invalid, out _), "missing target rejected before reload");
            Budget("complete");
            return "ImplB3: " + checks + " checks passed (movement, construction, combat, deployed route, defense, tools, rewards, boarding).";
        }
        finally { SaveStorage.RootOverride = previousRoot; UserSettings.Apply(settings, false); }
    }
}
