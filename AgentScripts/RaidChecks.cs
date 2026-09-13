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

    public static class RaidChecks
    {
        public static async Task<string> Main()
        {
            if (!Application.isPlaying) throw new Exception("Run in Play mode; consumes the prototype nest until Play is restarted.");
            var colony = GameObject.Find("EnemyNestPrototype").GetComponent<EnemyColony>();
            var nodes = colony.GetComponentsInChildren<ResourceNode>();
            var buildings = colony.GetComponentsInChildren<BuildingBase>();
            var loot = Array.Find(nodes, n => n.ResourceType == ResourceType.Special);
            var rm = ResourceManager.Instance;
            var original = rm.GetAmount(ResourceType.Special);
            var originalCapacity = rm.GetCapacity(ResourceType.Special);
            var upkeep = Object.FindAnyObjectByType<UpkeepManager>();
            var upkeepEnabled = upkeep.enabled;
            upkeep.enabled = false;
            var threats = Object.FindObjectsByType<MonoBehaviour>().Where(m => m.enabled
                && (m is WildMonster || m is ColonyInvasion)).ToArray();
            foreach (var threat in threats) threat.enabled = false;
            var go = new GameObject("RaidTestSoldier");
            var unitData = ScriptableObject.CreateInstance<UnitData>();
            var template = Object.FindAnyObjectByType<WorkerAnt>();
            var workerData = Object.Instantiate(template.Data);
            workerData.moveSpeed = 15;
            workerData.maxHealth = 10000;
            var workerObject = new GameObject("RaidTestCommander");
            workerObject.transform.position = template.transform.position;
            var worker = workerObject.AddComponent<CommanderAnt>();
            worker.Initialize(workerData, null, null);
            try
            {
                AntPool.Instance.Breed(2);
                Check(worker.TryAssign(1), "raid worker receives one troop without cloning allocation");
                Check(colony.RemainingBuildings == 2 && !colony.IsDefeated, "two live buildings");
                Check(!loot.CanGather && loot.Extract(100) == 0, "no loot before conquest");
                Check(buildings[0].CountsTowardPlayerDefeat == false, "enemy excluded from player defeat");
                Check(ReferenceEquals(CombatTargeting.FindNearestEnemy(buildings[0].Position, 2, UnitRole.Melee), buildings[0]), "automatic nest targeting");
                Check(CombatTargeting.FindNearestEnemy(buildings[0].Position, 2, UnitRole.Worker) == null, "worker cannot attack nest");
                buildings[1].gameObject.SetActive(false);
                Check(!colony.IsDefeated && colony.RemainingBuildings == 2, "disabled building is not destroyed");
                buildings[1].gameObject.SetActive(true);
                buildings[0].TakeDamage(1000);
                Check(colony.RemainingBuildings == 1 && !loot.CanGather, "partial destruction stays locked");
                Check(rm.GetAmount(ResourceType.Special) == original, "no instant reward");
                if (!NavMesh.SamplePosition(buildings[1].Position + Vector3.forward * 2, out var spawn, 4, NavMesh.AllAreas))
                    throw new Exception("No soldier spawn.");
                go.transform.position = spawn.position;
                var soldier = go.AddComponent<CommanderAnt>();
                unitData.role = UnitRole.Melee;
                unitData.attackDamage = 30;
                unitData.attackInterval = .1f;
                soldier.ConfigureCommander("Raid", CommanderRank.Sergeant, new[] { UnitRole.Melee }, UnitRole.Melee);
                soldier.Initialize(unitData, null, null);
                Check(soldier.TryAssign(1), "raid attacker receives a troop");
                soldier.CommandAttackMove(buildings[1].Position);
                await Until(() => colony.IsDefeated, 10000);
                Check(loot.CanGather && loot.AmountRemaining == 20, "all buildings destroyed unlock actual stock");
                Check(rm.GetAmount(ResourceType.Special) == original, "conquest itself adds no resources");
                worker.CommandGather(loot);
                await Until(() => rm.GetAmount(ResourceType.Special) >= original + worker.Data.carryCapacity, 45000);
                var expectedRemaining = 20 - worker.Data.carryCapacity;
                Check(Mathf.Abs(loot.AmountRemaining - expectedRemaining) < .0001f,
                    $"real gather and deposit deduct only cargo: expected {expectedRemaining}, actual {loot.AmountRemaining:R}");
                var rest = loot.Extract(1000);
                Check(Mathf.Abs(rest - expectedRemaining) < .0001f && loot.Extract(1000) == 0, "finite loot cannot duplicate");
                await Task.Delay(100);
                Check(colony.IsDefeated && colony.RemainingBuildings == 0 && loot.IsDepleted, "no rebuilding or loot regeneration");
                var capacity = rm.GetCapacity(ResourceType.Special);
                rm.Add(ResourceType.Special, capacity + 1);
                Check(rm.GetAmount(ResourceType.Special) == capacity, "special storage cap enforced");
                rm.AddCapacity(ResourceType.Special, 25);
                rm.Add(ResourceType.Special, 25);
                Check(rm.GetAmount(ResourceType.Special) == capacity + 25, "special storage expands");
                rm.AddCapacity(ResourceType.Special, -25);
                return "PASS: locked loot, all-buildings condition, disabled-building guard, enemy targeting, real attack-move destruction, real special haul/deposit, no instant or duplicate rewards, no regeneration, storage cap/expansion.";
            }
            finally
            {
                Object.Destroy(go);
                Object.Destroy(unitData);
                Object.Destroy(workerObject);
                Object.Destroy(workerData);
                rm.AddCapacity(ResourceType.Special, originalCapacity - rm.GetCapacity(ResourceType.Special));
                upkeep.enabled = upkeepEnabled;
                foreach (var threat in threats) if (threat != null) threat.enabled = true;
                var amounts = (System.Collections.Generic.Dictionary<ResourceType, int>)typeof(ResourceManager).GetField("amounts", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rm);
                amounts[ResourceType.Special] = original;
                rm.AddCapacity(ResourceType.Special, 0);
            }
        }
        private static void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); }
        private static async Task Until(Func<bool> condition, int milliseconds)
        {
            var end = DateTime.UtcNow.AddMilliseconds(milliseconds);
            while (!condition()) { if (DateTime.UtcNow > end) throw new Exception("Timed out."); await Task.Delay(50); }
        }
    }
}
