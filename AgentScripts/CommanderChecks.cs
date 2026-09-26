using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;


public static class CommanderChecks
{
    static int checks;
    static void Check(bool value, string name)
    {
        if (!value) throw new Exception("FAIL: " + name);
        checks++;
    }
    static async Task Until(Func<bool> condition)
    {
        var end = DateTime.UtcNow.AddSeconds(20);
        while (!condition() && DateTime.UtcNow < end) await Task.Delay(50);
    }
    // 새 Play 세션은 메인 메뉴(일시정지)로 시작하므로 필요하면 게임을 직접 시작한다.
    static async System.Threading.Tasks.Task StartGame()
    {
        if (AntColony.Core.GameSession.Instance.GameStarted) return;
        while (AntColony.Save.SaveSystem.Busy) await System.Threading.Tasks.Task.Delay(50);
        AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions());
        while (AntColony.Save.SaveSystem.Busy) await System.Threading.Tasks.Task.Delay(50);
        AntColony.UI.GameMenuController.Instance.Resume(); UnityEngine.Time.timeScale = 1;
    }
    // 무기=역할 개편: 예전 보직 변경을 해당 무기(날개) 장착으로 대신한다.
    static bool Arm(AntColony.Units.CommanderAnt c, AntColony.Data.UnitRole role)
    {
        var inv = AntColony.Units.EquipmentInventory.Instance;
        var item = role == AntColony.Data.UnitRole.Flying
            ? new AntColony.Units.EquipmentItem { slot = AntColony.Units.EquipmentSlot.Armor, armor = AntColony.Units.ArmorKind.Wings, quality = 1 }
            : new AntColony.Units.EquipmentItem { slot = AntColony.Units.EquipmentSlot.Weapon, quality = 1,
                weapon = role == AntColony.Data.UnitRole.Ranged ? AntColony.Units.WeaponKind.AcidSprayer : role == AntColony.Data.UnitRole.Defense ? AntColony.Units.WeaponKind.Shield
                    : role == AntColony.Data.UnitRole.Support ? AntColony.Units.WeaponKind.Pheromone : AntColony.Units.WeaponKind.Mandible };
        if (inv.Full) inv.Items.RemoveAt(0);
        if (!inv.Add(item) || !inv.Equip(c, item)) return false;
        inv.Items.RemoveAll(e => e.slot == item.slot && e.quality == 1 && e != item);
        return role == AntColony.Data.UnitRole.Flying ? c.IsFlying : c.Role == (role == AntColony.Data.UnitRole.Worker ? AntColony.Data.UnitRole.Melee : role);
    }
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        await StartGame();
        checks = 0;
        var pool = AntPool.Instance;
        var rm = ResourceManager.Instance;
        var upkeep = Object.FindAnyObjectByType<UpkeepManager>();
        var wasEnabled = upkeep.enabled;
        upkeep.enabled = false;
        var threats = Object.FindObjectsByType<MonoBehaviour>().Where(m => m.enabled
            && (m is AntColony.World.WildMonster || m is AntColony.World.ColonyInvasion)).ToArray();
        foreach (var threat in threats) threat.enabled = false;
        var commanders = Object.FindObjectsByType<CommanderAnt>(FindObjectsSortMode.None);
        var commander = commanders.First(c => true);
        GameObject siteObject = null, building = null, barracksObject = null;
        try
        {
            Check(commanders.Length == 12, "12 actual scene commanders");
            Check(AntUnitBase.Active.All(a => a is CommanderAnt), "only commanders are spawned as units");
            Check(pool.Assigned == commanders.Sum(c => c.TroopCount), "assigned count matches roster");
            pool.Breed(10);
            commander.TryAssign(2);
            var total = pool.Total;
            var original = commander.TroopCount;
            Check(commander.TryAssign(2) && pool.Total == total, "assignment conserves pool");
            Check(!commander.TryAssign(int.MaxValue), "overflow assignment rejected");
            Check(commander.ReturnTroops(2) == 2 && commander.TroopCount == original && pool.Total == total, "return conserves pool");
            Check(Arm(commander, UnitRole.Ranged) && commander.Data.attackRange == 6f, "ranged profile updates range");
            commander.TakeDamage(commander.Armor + 1f);
            Check(commander.TroopCount == original - 1 && pool.Total == total - 1, "damage removes one ant");
            var health = commander.CurrentHealth;
            Check(Arm(commander, UnitRole.Worker) && commander.CurrentHealth == health, "role change never heals");
            barracksObject = new GameObject("CommanderResearchCheck");
            barracksObject.SetActive(false);
            var barracks = barracksObject.AddComponent<Barracks>();
            typeof(Barracks).GetField("role", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(barracks, UnitRole.Ranged);
            typeof(BuildingBase).GetField("countsTowardPlayerDefeat", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(barracks, false);
            barracksObject.SetActive(true);
            Check(Arm(commander, UnitRole.Ranged), "return to ranged");
            var tierField = typeof(Barracks).GetField("currentTier", BindingFlags.NonPublic | BindingFlags.Instance);
            var oldTier = (int)tierField.GetValue(barracks);
            var attack = commander.AttackDamage;
            tierField.SetValue(barracks, oldTier + 1);
            Check(commander.AttackDamage == attack + commander.TroopCount, "live barracks bonus updates existing troops");
            tierField.SetValue(barracks, oldTier);
            Check(pool.TryReserve(3), "construction reserves free ants");
            var free = pool.Free;
            siteObject = new GameObject("CommanderCheckSite");
            building = new GameObject("CommanderCheckBuilding");
            building.SetActive(false);
            var site = siteObject.AddComponent<BuildingConstructionSite>();
            site.Initialize(building, 1f, pool, 3);
            site.Complete();
            site.Complete();
            await Task.Delay(50);
            Check(pool.Free == free + 3 && pool.Reserved == 0 && building.activeSelf, "completion and destruction return once");
            Object.Destroy(building);
            Check(pool.TryReserve(3), "second construction reserve");
            free = pool.Free;
            siteObject = new GameObject("CommanderCancelSite");
            building = new GameObject("CommanderCancelBuilding");
            building.SetActive(false);
            site = siteObject.AddComponent<BuildingConstructionSite>();
            site.Initialize(building, 1f, pool, 3);
            site.Cancel();
            site.Cancel();
            await Task.Delay(50);
            Check(pool.Free == free + 3 && pool.Reserved == 0 && building == null, "cancel returns once and removes unfinished building");
            var queen = Object.FindAnyObjectByType<QueenChamber>();
            rm.Add(AntColony.Data.ResourceType.Food, 200);
            rm.Add(AntColony.Data.ResourceType.Soil, 200);
            var beforeProduction = pool.Total;
            Check(queen.TryProduceWorker() && !queen.TryProduceWorker(), "queen starts single production");
            await Until(() => pool.Total == beforeProduction + 1);
            Check(pool.Total == beforeProduction + 1 && AntUnitBase.Active.Count == 12, "queen produces numeric ant only");
            typeof(GameManager).GetProperty("FishingUnlocked").SetValue(GameManager.Instance, false);
            Check(queen.TryResearchFishing() && !queen.TryResearchFishing(), "queen fishing research starts once");
            await Until(() => GameManager.Instance.FishingUnlocked);
            Check(GameManager.Instance.FishingUnlocked && !queen.TryResearchFishing(), "global fishing unlock");
            var beforeFood = rm.GetAmount(AntColony.Data.ResourceType.Food);
            var due = upkeep.FoodDue;
            typeof(UpkeepManager).GetMethod("RunCycle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(upkeep, null);
            Check(due >= pool.Total && rm.GetAmount(AntColony.Data.ResourceType.Food) == beforeFood - due, "upkeep charges pool and commanders once");
            var beforeLoss = pool.Total;
            var troops = commander.TroopCount;
            commander.TakeDamage(float.PositiveInfinity);
            Check(commander.TroopCount == 0 && pool.Total == beforeLoss - troops && !commander.CanStartConstruction,
                "zero troops disable work without phantom health");
            Check(!CombatTargeting.IsAlive(commander), "zero troops do not tank enemies");
            Check(commander.TryAssign(1), "zero troop commander can be replenished");
            free = pool.Free;
            commander.gameObject.SetActive(false);
            Check(pool.Free == free + 1, "disable returns troops");
            commander.gameObject.SetActive(true);
            Check(commander.TryAssign(1), "reactivated commander can assign");
            commander.gameObject.SetActive(false);
            Check(pool.Free == free + 1, "second disable also returns exactly once");
            commander.gameObject.SetActive(true);
            return "PASS: " + checks + " commander/production/research/construction/upkeep checks";
        }
        finally
        {
            if (siteObject != null) Object.Destroy(siteObject);
            if (building != null) Object.Destroy(building);
            if (barracksObject != null) Object.Destroy(barracksObject);
            upkeep.enabled = wasEnabled;
            foreach (var threat in threats) if (threat != null) threat.enabled = true;
        }
    }
}


