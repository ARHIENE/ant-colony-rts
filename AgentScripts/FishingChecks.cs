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
    using Object = UnityEngine.Object;

    public static class FishingChecks
    {
        public static async Task<string> Main()
        {
            if (!Application.isPlaying) throw new Exception("Run in Play mode.");
            // 새 Play 세션은 메인 메뉴(일시정지)로 시작하므로 필요하면 게임을 직접 시작한다.
            if (!GameSession.Instance.GameStarted)
            {
                while (Save.SaveSystem.Busy) await Task.Delay(50);
                Save.SaveSystem.NewGame(new NewGameOptions());
                while (Save.SaveSystem.Busy) await Task.Delay(50);
                UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
            }
            var gm = GameManager.Instance;
            var rm = ResourceManager.Instance;
            var spot = GameObject.Find("FishingSpot").GetComponent<ResourceNode>();
            var worker = Object.FindAnyObjectByType<CommanderAnt>();
            // 다른 장수의 자율 채집이 식량을 늘려 낚시 반납으로 오인하지 않게 한다.
            foreach (var c in CommanderRoster.Instance.Commanders) { c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop(); }
            typeof(GameManager).GetProperty("FishingUnlocked").SetValue(gm, false);
            var originalFood = rm.GetAmount(ResourceType.Food);
            var originalSoil = rm.GetAmount(ResourceType.Soil);
            var upkeep = Object.FindAnyObjectByType<UpkeepManager>();
            var wasEnabled = upkeep.enabled;
            upkeep.enabled = false;
            var threats = Object.FindObjectsByType<MonoBehaviour>().Where(m => m.enabled
                && (m is WildMonster || m is ColonyInvasion)).ToArray();
            foreach (var threat in threats) threat.enabled = false;
            var labObject = new GameObject("FishingTestLab");
            var lab = labObject.AddComponent<QueenChamber>();
            var secondObject = new GameObject("FishingSecondLab");
            var second = secondObject.AddComponent<QueenChamber>();
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
                Set(lab, "fishingResearchSeconds", .15f);
                var food = rm.GetAmount(ResourceType.Food);
                var soil = rm.GetAmount(ResourceType.Soil);
                Assert(lab.TryResearchFishing(), "start research");
                Assert(!second.TryResearchFishing() && !lab.TryResearchFishing(), "concurrent research rejected");
                Assert(rm.GetAmount(ResourceType.Food) == food - 30 && rm.GetAmount(ResourceType.Soil) == soil - 20, "single research charge");
                labObject.SetActive(false);
                await Task.Delay(200);
                Assert(!gm.FishingUnlocked && !(bool)Get(lab, "isFishingResearching"), "interrupted research stays locked");
                labObject.SetActive(true);
                Assert(lab.TryResearchFishing(), "restart research");
                await Until(() => gm.FishingUnlocked, 2000);
                Assert(spot.CanGather && spot.GatherRateMultiplier == 2f, "unlock and fishing rate");
                newWorkerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                newWorkerObject.transform.position = worker.transform.position;
                var newWorker = newWorkerObject.AddComponent<CommanderAnt>();
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
                // 낚시는 20초마다 한 번 잡고(Food 6 × 낚시 배율) 창고에 반납한다.
                await Until(() => spot.AmountRemaining < fishAmount && rm.GetAmount(ResourceType.Food) > food, 120000);
                Assert(spot.AmountRemaining < fishAmount, "real shoreline harvest and deposit");
                spot.Extract(1000);
                Assert(spot.FishedOut && !spot.CanGather && !spot.IsRegrowing, "monthly fishing cap reached");
                typeof(ResourceNode).GetProperty("FishMonth", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(spot, GameCalendar.TotalMonths - 1);
                spot.RefreshFishingMonth();
                Assert(spot.CanGather && spot.AmountRemaining == GameBalance.FishingMonthlyFood, "next month restocks");
                return "PASS: locked command/extraction, single charge, concurrent research guard, cancellation, restart, unlock, lab destruction, real shoreline harvest/deposit, monthly cap and restock.";
            }
            finally
            {
                if (labObject != null) Object.Destroy(labObject);
                if (newWorkerObject != null) Object.Destroy(newWorkerObject);
                Object.Destroy(secondObject);
                upkeep.enabled = wasEnabled;
                foreach (var threat in threats) if (threat != null) threat.enabled = true;
                typeof(GameManager).GetProperty("FishingUnlocked").SetValue(gm, false);
                Set(spot, "amountRemaining", fishAmount);
                Set(spot, "regrowTimer", 0f);
                rm.TrySpend(Math.Max(0, rm.GetAmount(ResourceType.Food) - originalFood), Math.Max(0, rm.GetAmount(ResourceType.Soil) - originalSoil));
                rm.Add(ResourceType.Food, originalFood - rm.GetAmount(ResourceType.Food));
                rm.Add(ResourceType.Soil, originalSoil - rm.GetAmount(ResourceType.Soil));
            }
        }
        static FieldInfo Field(object value, string name)
        {
            for (var type = value.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            throw new Exception("Missing field " + name);
        }
        static object Get(object value, string field) => Field(value, field).GetValue(value);
        static void Set(object value, string field, object data) => Field(value, field).SetValue(value, data);
        static void Assert(bool ok, string message) { if (!ok) throw new Exception("FAIL: " + message); }
        static async Task Until(Func<bool> check, int milliseconds)
        {
            var start = DateTime.UtcNow;
            while (!check() && (DateTime.UtcNow - start).TotalMilliseconds < milliseconds) await Task.Delay(20);
            Assert(check(), "timed out waiting for gameplay result");
        }
    }
}

