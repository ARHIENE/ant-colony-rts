namespace AntColony.Regression
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using AntColony.Buildings;
    using AntColony.Core;
    using AntColony.Data;
    using AntColony.Units;
    using AntColony.World;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public static class FishingChecks
    {
        public static async Task<string> Main()
        {
            if (!Application.isPlaying) throw new Exception("Run in Play mode.");
            var gm = GameManager.Instance;
            var rm = ResourceManager.Instance;
            var spot = GameObject.Find("FishingSpot").GetComponent<ResourceNode>();
            var worker = Object.FindAnyObjectByType<WorkerAnt>();
            var originalFood = rm.GetAmount(ResourceType.Food);
            var originalSoil = rm.GetAmount(ResourceType.Soil);
            var upkeep = Object.FindAnyObjectByType<UpkeepManager>();
            var wasEnabled = upkeep.enabled;
            upkeep.enabled = false;
            var labObject = new GameObject("FishingTestLab");
            var lab = labObject.AddComponent<ResearchLab>();
            var secondObject = new GameObject("FishingSecondLab");
            var second = secondObject.AddComponent<ResearchLab>();
            var fishAmount = spot.AmountRemaining;
            GameObject newWorkerObject = null;
            try
            {
                Assert(!gm.FishingUnlocked && !spot.CanGather && spot.Extract(10) == 0, "locked extraction");
                var previousTarget = Get(worker, "targetNode");
                worker.CommandGather(spot);
                Assert(ReferenceEquals(Get(worker, "targetNode"), previousTarget), "locked worker command preserves previous task");
                rm.Add(ResourceType.Food, 100);
                rm.Add(ResourceType.Soil, 100);
                Set(lab, "researchTimeSeconds", .15f);
                var food = rm.GetAmount(ResourceType.Food);
                var soil = rm.GetAmount(ResourceType.Soil);
                Assert(lab.TryResearchFishing(), "start research");
                Assert(!second.TryResearchFishing() && !lab.TryResearchAttack(), "concurrent research rejected");
                Assert(rm.GetAmount(ResourceType.Food) == food - 30 && rm.GetAmount(ResourceType.Soil) == soil - 20, "single research charge");
                labObject.SetActive(false);
                await Task.Delay(200);
                Assert(!gm.FishingUnlocked && !ResearchLab.IsFishingResearching, "interrupted research stays locked");
                labObject.SetActive(true);
                Assert(lab.TryResearchFishing(), "restart research");
                await Until(() => gm.FishingUnlocked, 2000);
                Assert(spot.CanGather && spot.GatherRateMultiplier == 2f, "unlock and fishing rate");
                newWorkerObject = Object.Instantiate(worker.gameObject, worker.transform.position, Quaternion.identity);
                var newWorker = newWorkerObject.GetComponent<WorkerAnt>();
                newWorker.Initialize(worker.Data, null, null);
                newWorker.CommandGather(spot);
                Assert((ResourceNode)Get(newWorker, "targetNode") == spot, "new worker inherits fishing unlock");
                Object.Destroy(newWorkerObject);
                Assert(!second.TryResearchFishing(), "completed research cannot charge again");
                Object.Destroy(labObject);
                await Task.Delay(50);
                Assert(gm.FishingUnlocked && spot.CanGather, "learned fishing survives lab destruction");
                food = rm.GetAmount(ResourceType.Food);
                worker.CommandGather(spot);
                await Until(() => rm.GetAmount(ResourceType.Food) >= food + worker.Data.carryCapacity, 25000);
                Assert(spot.AmountRemaining < fishAmount, "real shoreline harvest and deposit");
                spot.Extract(1000);
                Assert(spot.IsRegrowing && !spot.CanGather, "fish restocking");
                Set(spot, "regrowTimer", .05f);
                await Until(() => spot.CanGather, 2000);
                return "PASS: locked command/extraction, single charge, concurrent research guard, cancellation, restart, unlock, lab destruction, real shoreline harvest/deposit, restocking.";
            }
            finally
            {
                if (labObject != null) Object.Destroy(labObject);
                if (newWorkerObject != null) Object.Destroy(newWorkerObject);
                Object.Destroy(secondObject);
                upkeep.enabled = wasEnabled;
                typeof(GameManager).GetProperty("FishingUnlocked").SetValue(gm, false);
                Set(spot, "amountRemaining", fishAmount);
                Set(spot, "regrowTimer", 0f);
                rm.TrySpend(Math.Max(0, rm.GetAmount(ResourceType.Food) - originalFood), Math.Max(0, rm.GetAmount(ResourceType.Soil) - originalSoil));
                rm.Add(ResourceType.Food, originalFood - rm.GetAmount(ResourceType.Food));
                rm.Add(ResourceType.Soil, originalSoil - rm.GetAmount(ResourceType.Soil));
            }
        }
        static object Get(object value, string field) => value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
        static void Set(object value, string field, object data) => value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, data);
        static void Assert(bool ok, string message) { if (!ok) throw new Exception("FAIL: " + message); }
        static async Task Until(Func<bool> check, int milliseconds)
        {
            var start = DateTime.UtcNow;
            while (!check() && (DateTime.UtcNow - start).TotalMilliseconds < milliseconds) await Task.Delay(20);
            Assert(check(), "timed out waiting for gameplay result");
        }
    }
}
