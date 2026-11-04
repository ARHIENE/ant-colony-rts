using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ImplC1Checks
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static int count;
    static readonly List<GameObject> made = new List<GameObject>();
    static void Check(bool ok, string label) { if (!ok) throw new Exception("C1 FAIL " + count + ": " + label); count++; }
    static object Call(object target, string method, params object[] args) => typeof(CommanderAnt).GetMethod(method, Hidden).Invoke(target, args);
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .01f;
    static T Put<T>(BuildingKind kind, Vector3 position) where T : Component
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { kind, UnitRole.Worker });
        Check(template != null, "template " + kind);
        var go = Object.Instantiate(template); made.Add(go); go.transform.position = position; go.SetActive(true);
        return go.GetComponent<T>();
    }
    static ResourceNode Node(AntColony.Data.ResourceType type, Vector3 point)
    {
        var go = new GameObject("C1 natural sample fixture"); made.Add(go); go.SetActive(false);
        go.transform.position = point; var node = go.AddComponent<ResourceNode>(); node.ConfigureNatural(type, 20, 0); go.SetActive(true); return node;
    }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "load complete"); Time.timeScale = 0;
    }
    public static Task<string> Main() => Run();
    public static async Task<string> Run()
    {
        count = 0; made.Clear(); Check(Application.isPlaying, "Play mode required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone(); var scale = Time.timeScale;
        var random = UnityEngine.Random.state;
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplC1-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            var team = Enumerable.Range(0, 2).Select(StartingCandidate.Roll).ToArray();
            foreach (var candidate in team) candidate.traits.values.Clear();
            CrashLandingStart.Begin(new NewGameOptions { startingCount = 2, startingTeam = team, seed = 261024, mapSize = MapSize.Small, biome = MapBiome.Forest });
            await Ready();
            Check(CommanderRoster.Instance.Count == 2 && Object.FindAnyObjectByType<Stockpile>() == null, "two commander crash without stockpile");
            var c = CommanderRoster.Instance.Commanders[0]; var other = CommanderRoster.Instance.Commanders[1];
            foreach (var unit in new[] { c, other }) { unit.CommandStop(); unit.SetJobEnabled(CommanderJobs.All, false); unit.PersonalState.meal.satiety = 100; }
            var research = CampaignResearch.Instance;
            Check(research != null, "campaign exists");
            var home = c.Position;
            // G48: quiet timer resets while either toggle or final-deployment hold suppresses return.
            c.WorkState.duty = CommanderDuty.Deployed; c.WorkState.returnPosition = home;
            WorldMapManager.Instance.DefenseReturn.Set(c, false); c.WorkState.quietSeconds = 19;
            c.TickDuty(2); Check(!c.IsReturning && c.WorkState.quietSeconds == 0, "G48 disabled formation resets timer");
            WorldMapManager.Instance.DefenseReturn.Set(c, true); c.WorkState.holdDeployment = true; c.WorkState.quietSeconds = 19;
            c.TickDuty(2); Check(!c.IsReturning && c.WorkState.quietSeconds == 0, "G61 hold overrides enabled return");
            c.WorkState.holdDeployment = false;
            Check(HomeDefenseReturn.Allows(c), "G48 ordinary home defense allowed");
            c.TickDuty(GameBalance.AutoReturnSeconds);
            Check(c.IsReturning, "G48 ordinary defense returns after quiet twenty seconds");
            c.CommandStop(); c.WorkState.duty = CommanderDuty.Civilian;

            // G59: actual service consumes work only at its destination, and never overflows to floor water.
            var source = Put<SupplyFacility>(BuildingKind.WaterSource, home + Vector3.right * 2);
            var well = Put<SupplyFacility>(BuildingKind.ManualWell, home);
            var tank = Put<SupplyFacility>(BuildingKind.FluidTank, home + Vector3.left);
            c.SetJobEnabled(CommanderJobs.Hauling, true);
            Check(CommanderAnt.ManualWellNeedsWork(well), "G59 water source and free buffer required");
            var candidates = (IEnumerable<(Component target, Func<bool> start)>)Call(c, "AutoCandidates", CommanderJobs.Hauling);
            Check(candidates.Any(candidate => candidate.target == well), "G59 manual well included in automatic hauling candidates");
            Check(c.StartService(well, CommanderJobs.Hauling), "G59 manual well is a hauling service");
            await Task.Delay(100); c.Agent.ResetPath();
            Call(c, "TickService", 2f); Check(Near(well.Supply.work, 2), "G59 partial work");
            var savedWell = JsonUtility.FromJson<SupplyFacility.State>(JsonUtility.ToJson(well.Supply));
            well.RestoreSupply(savedWell);
            Call(c, "TickService", 3f); Check(tank.Supply.amount + well.Supply.amount == 10, "G59 five seconds produces ten once");
            tank.RestoreSupply(new SupplyFacility.State { fluid = (int)AntColony.Data.ResourceType.Water, amount = SupplyFacility.Capacity });
            well.RestoreSupply(new SupplyFacility.State { fluid = (int)AntColony.Data.ResourceType.Water, amount = SupplyFacility.Capacity - 10 });
            var floor = ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == AntColony.Data.ResourceType.Water).Sum(n => n.AmountRemaining);
            Call(c, "TickService", 1000f);
            Check(well.Supply.amount == SupplyFacility.Capacity && c.ServiceTarget == null, "G59 large tick stops at full buffer");
            Check(!c.StartService(well, CommanderJobs.Hauling), "G59 full well refuses automatic and direct restart");
            Check(Near(floor, ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == AntColony.Data.ResourceType.Water).Sum(n => n.AmountRemaining)), "G59 no endless floor output");
            source.gameObject.SetActive(false);
            well.RestoreSupply(new SupplyFacility.State());
            Check(!CommanderAnt.ManualWellNeedsWork(well), "G59 missing source blocks work");
            c.CommandStop();

            // G06: isolate natural sulfur from starting loose ores; two commanders cannot take duplicate samples.
            research.RestoreState(new CampaignResearch.State());
            var node = Node(AntColony.Data.ResourceType.Sulfur, home + Vector3.forward * 5);
            c.PersonalState.toolTier = 0;
            Check(!node.CanGatherWith(c) && node.Extract(10) == 0, "G06 bare hands and workerless extraction blocked");
            c.PersonalState.toolTier = 1; other.PersonalState.toolTier = 1;
            Check(node.CanGatherWith(c) && node.ExtractFor(c, .5f) == 0, "G06 previous tool waits for complete sample");
            Check(node.ExtractFor(c, 10) == 1, "G06 previous tool gets one sample");
            typeof(WorkerAnt).GetMethod("RestoreCargo", Hidden).Invoke(c, new object[] { 1f, AntColony.Data.ResourceType.Sulfur, null });
            Check(!node.CanGatherWith(other), "G06 in-flight sample prevents duplicate acquisition");
            research.RecordSampleDelivery(AntColony.Data.ResourceType.Sulfur, 1);
            Check(!node.CanGatherWith(c), "G06 sample delivered still blocks mining before research");
            Check(research.TryCompleteResourceResearch(AntColony.Data.ResourceType.Sulfur), "G06 resource research completes after sample");
            Check(!node.CanGatherWith(c), "G06 previous tool cannot bulk mine after research");
            c.PersonalState.toolTier = 2; Check(node.ExtractFor(c, 3) == 3, "G06 researched resource with proper tool mines normally");
            typeof(WorkerAnt).GetMethod("RestoreCargo", Hidden).Invoke(c, new object[] { 0f, AntColony.Data.ResourceType.Sulfur, null });

            CheckBosses(c, research);
            CheckEquipment(c);
            return "PASS " + count + " (ImplC1Checks)";
        }
        finally
        {
            foreach (var go in made) if (go != null) Object.DestroyImmediate(go);
            made.Clear(); SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); UnityEngine.Random.state = random; Time.timeScale = scale;
        }
    }
    static void CheckBosses(CommanderAnt c, CampaignResearch research)
    {
        research.RestoreState(new CampaignResearch.State());
        var state = research.CaptureState(); state.bosses.revealed = new[] { true, true, true, true }; research.RestoreState(state);
        var inventory = EquipmentInventory.Instance;
        if (inventory == null) { var go = new GameObject("C1 inventory"); made.Add(go); inventory = go.AddComponent<EquipmentInventory>(); }
        inventory.Items.Clear();
        foreach (BossId boss in Enum.GetValues(typeof(BossId)))
        {
            Check(research.BossProgress.RecordKill(boss, 0), "G19 kill " + boss);
            Check(research.BossProgress.TryAcquire(boss, c.BossCarrier, out var parcel), "G19 commander carrier " + boss);
            Check(research.BossProgress.Obtained(boss) && !research.BossProgress.Delivered(boss), "G19 pickup distinct from delivery " + boss);
            if (boss == BossId.A)
            {
                var before = c.BossCargoCount;
                c.WorkState.bossPickup = 2;
                var personal = JsonUtility.FromJson<CommanderPersonalState>(JsonUtility.ToJson(c.CapturePersonalState()));
                Check(personal.work.bossPickup == 2 && before == 2, "G19 pending command and carrier cargo represented in saved state");
                c.CommandStop();
            }
            var countBefore = inventory.Items.Count;
            Check(CommanderAnt.DeliverBossEquipment(parcel.id, c.Position), "G19/G22 delivery " + boss);
            Check(!CommanderAnt.DeliverBossEquipment(parcel.id, c.Position), "G22 no repeated parcel payout " + boss);
            Check(inventory.Items.Count == countBefore + (boss == BossId.D ? 0 : 1), "G22 actual unique equipment " + boss);
        }
        Check(research.BossProgress.TryRespawn(BossId.A, BossProgress.RespawnSeconds, false, false, false), "G19 respawn next cycle");
        research.BossProgress.RecordKill(BossId.A, BossProgress.RespawnSeconds);
        research.BossProgress.TryAcquire(BossId.A, c.BossCarrier, out _);
        c.gameObject.SetActive(false); c.gameObject.SetActive(true);
        Check(c.BossCargoCount == 2, "G19 normal disable is not loss");
        c.LoseBossCargo(); Check(c.BossCargoCount == 0 && research.BossProgress.Obtained(BossId.A), "G19 loss retains recovery eligibility");
        c.LoseBossCargo(); Check(BossProgress.Validate(research.CaptureState().bosses), "G19 repeated loss preserves valid campaign");
        Check(research.BossProgress.TryRespawn(BossId.B, BossProgress.RespawnSeconds, false, false, false), "G22 next B reward cycle");
        research.BossProgress.RecordKill(BossId.B, BossProgress.RespawnSeconds);
        research.BossProgress.TryAcquire(BossId.B, c.BossCarrier, out var overflow);
        while (!inventory.Full) Check(inventory.Add(new EquipmentItem()), "G22 fill inventory fixture");
        var lootBefore = new HashSet<EquipmentLoot>(Object.FindObjectsByType<EquipmentLoot>(FindObjectsSortMode.None));
        Check(CommanderAnt.DeliverBossEquipment(overflow.id, c.Position), "G22 full inventory still delivers core");
        var dropped = Object.FindObjectsByType<EquipmentLoot>(FindObjectsSortMode.None).Where(l => !lootBefore.Contains(l)).ToArray();
        foreach (var loot in dropped) made.Add(loot.gameObject);
        Check(dropped.Sum(l => l.Items.Count) == 1 && dropped[0].Items[0].bossEquipment == BossEquipmentKind.HeatShield, "G22 full inventory preserves unique equipment on floor");
    }
    static void CheckEquipment(CommanderAnt c)
    {
        var a = EquipmentItem.BossReward(BossId.A, "a"); var b = EquipmentItem.BossReward(BossId.B, "b"); var weapon = EquipmentItem.BossReward(BossId.C, "c");
        Check(a.IsValid && b.IsValid && weapon.IsValid && a.Label.Contains("내구") && b.Label.Contains("방열") && weapon.Label.Contains("범위"), "G22 distinct valid effects and labels");
        c.PersonalState.equipment.Clear(); c.PersonalState.equipment.Add(a);
        var after = (float)Call(c, "AbsorbDurableDamage", 20f);
        Check(after < 20 && a.durability < EquipmentItem.DurableCapacity, "G22 A absorbs damage and spends durability");
        var restored = JsonUtility.FromJson<EquipmentItem>(JsonUtility.ToJson(a));
        Check(restored.IsValid && restored.durability == a.durability, "G22 worn unique equipment JSON roundtrip");
        a.durability = 0; Check((float)Call(c, "AbsorbDurableDamage", 20f) == 20, "G22 exhausted durability stops absorbing");
        c.PersonalState.equipment.Clear(); c.PersonalState.equipment.Add(b);
        var hp = c.PersonalHealth; c.TakeHeatDamage(40); var heatLoss = hp - c.PersonalHealth;
        c.WorkState.health = hp; c.TakeDamage(40); Check(heatLoss < hp - c.PersonalHealth, "G22 B reduces typed heat damage");
        c.WorkState.health = hp;
        Check(weapon.AttackRadius > 0 && EquipmentItem.BossReward(BossId.D, "d") == null, "G22 C area attack and D core-only reward");
        c.PersonalState.equipment.Clear(); c.PersonalState.equipment.Add(weapon);
        var one = new GameObject("C1 area primary"); var two = new GameObject("C1 area secondary"); var far = new GameObject("C1 area outside");
        made.Add(one); made.Add(two); made.Add(far);
        one.transform.position = c.Position + Vector3.forward * 20; two.transform.position = one.transform.position + Vector3.right;
        far.transform.position = one.transform.position + Vector3.right * (weapon.AttackRadius + 2);
        var primary = one.AddComponent<AntColony.Boss.BossHealth>(); var secondary = two.AddComponent<AntColony.Boss.BossHealth>(); var outside = far.AddComponent<AntColony.Boss.BossHealth>();
        Call(c, "StrikeWithEquipment", primary, 20f);
        Check(Near(primary.MaxHp - primary.CurrentHp, 20) && Near(secondary.MaxHp - secondary.CurrentHp, 20), "G22 C actually damages two nearby enemies once");
        Check(outside.CurrentHp == outside.MaxHp && c.PersonalHealth == hp, "G22 C respects radius and excludes friendly commander");
        var old = JsonUtility.FromJson<EquipmentItem>("{\"id\":\"old\",\"slot\":0,\"quality\":1}");
        Check(old.IsValid && old.bossEquipment == BossEquipmentKind.None, "G22 legacy equipment defaults compatible");
    }
}
