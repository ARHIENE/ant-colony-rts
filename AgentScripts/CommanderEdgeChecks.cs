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

    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Fresh Play mode required");
        var commander = Object.FindObjectsByType<CommanderAnt>().First(c => c.AllowedRoles.Contains(UnitRole.Flying));
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

            Check(commander.TrySetRole(UnitRole.Flying) && !commander.Agent.enabled, "flying role disables ground agent");
            var target = startPosition + Vector3.forward * 2;
            commander.CommandMove(target);
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (Vector2.Distance(new Vector2(commander.transform.position.x, commander.transform.position.z),
                new Vector2(target.x, target.z)) > .2f && DateTime.UtcNow < deadline) await Task.Delay(50);
            Check(Vector2.Distance(new Vector2(commander.transform.position.x, commander.transform.position.z),
                new Vector2(target.x, target.z)) <= .2f, "flying commander really moves");
            Check(commander.TrySetRole(UnitRole.Worker) && commander.Agent.enabled && commander.Agent.isOnNavMesh,
                "landing restores valid ground movement");
            Check(commander.TrySetRole(UnitRole.Flying), "take off again");
            commander.transform.position = new Vector3(100000, 3, 100000);
            Check(!commander.TrySetRole(UnitRole.Worker) && commander.IsFlying && !commander.Agent.enabled,
                "reject landing without nearby NavMesh and preserve flying role");
            commander.transform.position = startPosition;
            commander.CommandStop();
            Check(commander.TrySetRole(UnitRole.Worker), "land after returning to terrain");

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
            var rank = commander.Rank;
            buttons.First(b => b.name == "Next Rank").onClick.Invoke();
            Check(commander.Rank != rank, "rank button changes command limit");
            commander.TrySetRole(UnitRole.Worker);
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
            commander.TrySetRole(UnitRole.Worker);
            Object.Destroy(nodeObject);
            if (siteObject != null) Object.Destroy(siteObject);
            if (building != null) Object.Destroy(building);
            upkeep.enabled = upkeepEnabled;
            foreach (var enemy in enemies) if (enemy != null) enemy.enabled = true;
        }
    }
}
