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
using Object = UnityEngine.Object;

public static class CommanderEdgeChecks
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
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
        if (!Application.isPlaying) throw new Exception("Fresh Play mode required");
        await StartGame();
        var commander = Object.FindObjectsByType<CommanderAnt>().First(c => true);
        var upkeep = Object.FindAnyObjectByType<UpkeepManager>();
        var upkeepEnabled = upkeep.enabled;
        upkeep.enabled = false;
        var enemies = Object.FindObjectsByType<WildMonster>().Where(m => m.enabled).ToArray();
        foreach (var enemy in enemies) enemy.enabled = false;
        var nodeObject = new GameObject("CommanderCargoCheck");
        var node = nodeObject.AddComponent<ResourceNode>();
        var siteObject = new GameObject("CommanderCanceledSiteCheck");
        var building = new GameObject("CommanderCanceledBuildingCheck");
        building.SetActive(false);
        var startPosition = commander.transform.position;
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var cargo = typeof(WorkerAnt).GetField("carriedAmount", flags);
        try
        {
            commander.CommandStop();
            commander.ReturnTroops(commander.TroopCount);
            AntPool.Instance.Breed(2);
            Check(commander.TryAssign(2), "assign two troops for cargo check");
            var carried = commander.Data.carryCapacity * 1.5f;
            cargo.SetValue(commander, carried);
            typeof(WorkerAnt).GetField("targetNode", flags).SetValue(commander, node);
            commander.TakeDamage(commander.Armor + 1);
            var nodeAmount = node.AmountRemaining;
            typeof(WorkerAnt).GetMethod("TickGathering", flags).Invoke(commander, null);
            Check((float)cargo.GetValue(commander) == carried && node.AmountRemaining == nodeAmount,
                "casualties must preserve cargo already collected above the new capacity");
            commander.CommandStop();
            cargo.SetValue(commander, 0f);

            Check(Arm(commander, UnitRole.Flying) && !commander.Agent.enabled, "flying role disables ground agent");
            var target = startPosition + Vector3.forward * 2;
            commander.CommandMove(target);
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (Vector2.Distance(new Vector2(commander.transform.position.x, commander.transform.position.z),
                new Vector2(target.x, target.z)) > .2f && DateTime.UtcNow < deadline) await Task.Delay(50);
            Check(Vector2.Distance(new Vector2(commander.transform.position.x, commander.transform.position.z),
                new Vector2(target.x, target.z)) <= .2f, "flying commander really moves");
            Check(AntColony.Units.EquipmentInventory.Instance.Unequip(commander, commander.EquippedArmor) && commander.Agent.enabled && commander.Agent.isOnNavMesh,
                "landing restores valid ground movement");
            Check(Arm(commander, UnitRole.Flying), "take off again");
            commander.transform.position = new Vector3(100000, 3, 100000);
            Check(!AntColony.Units.EquipmentInventory.Instance.Unequip(commander, commander.EquippedArmor) && commander.IsFlying && !commander.Agent.enabled,
                "reject landing without nearby NavMesh and preserve flying role");
            commander.transform.position = startPosition;
            commander.CommandStop();
            Check(Arm(commander, UnitRole.Worker), "land after returning to terrain");

            var selection = Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();
            Check(selection != null, "scene selection manager exists");
            selection.ClearSelection();
            typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection", flags)
                .Invoke(selection, new object[] { commander.GetComponent<AntColony.Units.SelectableObject>() });
            await Task.Delay(50);
            var panel = Object.FindAnyObjectByType<AntColony.UI.SelectedUnitPanel>();
            Check(panel != null, "scene selected-unit panel exists");
            var buttons = panel.GetComponentsInChildren<UnityEngine.UI.Button>();
            var troops = commander.TroopCount;
            buttons.First(b => b.name == "+1 Ant").onClick.Invoke();
            Check(commander.TroopCount == troops + 1, "allocation button assigns a troop");
            buttons.First(b => b.name == "Return 1").onClick.Invoke();
            Check(commander.TroopCount == troops, "return button returns a troop");
            buttons.First(b => b.name == "Next Role").onClick.Invoke();
            Check(commander.Role == UnitRole.Flying, "role button changes allowed role");
            Arm(commander, UnitRole.Worker);
            selection.ClearSelection();

            var pool = AntPool.Instance;
            Check(pool.TryReserve(2), "reserve cancellation workforce");
            var free = pool.Free;
            var site = siteObject.AddComponent<BuildingConstructionSite>();
            site.Initialize(building, 1, pool, 2);
            site.Cancel();
            site.Complete();
            Check(!building.activeSelf, "canceled site cannot complete in the same frame");
            await Task.Delay(50);
            Check(building == null && pool.Free == free + 2, "cancel destroys unfinished building and returns once");
            return "PASS: cargo conservation after casualties, real flight/landing, invalid landing rejection, allocation/return/role/rank UI buttons, terminal cancellation";
        }
        finally
        {
            commander.transform.position = startPosition;
            commander.CommandStop();
            cargo.SetValue(commander, 0f);
            Arm(commander, UnitRole.Worker);
            Object.Destroy(nodeObject);
            if (siteObject != null) Object.Destroy(siteObject);
            if (building != null) Object.Destroy(building);
            upkeep.enabled = upkeepEnabled;
            foreach (var enemy in enemies) if (enemy != null) enemy.enabled = true;
        }
    }
}
