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
            var root = Save.SaveStorage.RootOverride;
            Save.SaveStorage.RootOverride = System.IO.Path.Combine(Application.temporaryCachePath, "FishingChecks-" + Guid.NewGuid().ToString("N"));
            try { return await Run(); }
            finally { Save.SaveStorage.RootOverride = root; }
        }

        private static async Task<string> Run()
        {
            if (!Application.isPlaying) throw new Exception("Run in Play mode.");
            // 새 Play 세션은 메인 메뉴(일시정지)로 시작하므로 필요하면 게임을 직접 시작한다.
            if (!GameSession.Instance.GameStarted)
            {
                await Until(() => !Save.SaveSystem.Busy, 90000, "scene ready");
                var candidate = StartingCandidate.Roll(0); candidate.traits.values.Clear();
                Save.SaveSystem.NewGame(new NewGameOptions { seed = 261024, startingCount = 1, startingTeam = new[] { candidate } });
                await Until(() => !Save.SaveSystem.Busy, 90000, "new game ready");
            }
            UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
            var savedAutoPause = UserSettings.Current.autoPauseThreat;
            UserSettings.Current.autoPauseThreat = false;
            UI.GameMenuController.Instance.SetSpeed(1);
            var gm = GameManager.Instance;
            var rm = ResourceManager.Instance;
            var spot = GameObject.Find("FishingSpot").GetComponent<ResourceNode>();
            var worker = Object.FindAnyObjectByType<CommanderAnt>();
            // 다른 장수의 자율 채집이 식량을 늘려 낚시 반납으로 오인하지 않게 한다.
            foreach (var c in CommanderRoster.Instance.Commanders) { c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop(); }
            var savedUnlock = gm.FishingUnlocked;
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
            var lab = labObject.AddComponent<ScienceLab>();
            var research = CampaignResearch.Instance;
            var savedResearch = research.CaptureState();
            research.RestoreState(new CampaignResearch.State());
            var secondObject = new GameObject("FishingSecondLab");
            var second = secondObject.AddComponent<ScienceLab>();
            var fishAmount = spot.AmountRemaining;
            GameObject newWorkerObject = null;
            // 불시착 시작은 비축더미가 없으므로 반납 시설을 검사용으로 준비한다.
            var pileObject = new GameObject("FishingTestStockpile");
            pileObject.transform.position = worker.Position;
            pileObject.AddComponent<Stockpile>();
            try
            {
                Assert(!gm.FishingUnlocked && !spot.CanGather && spot.Extract(10) == 0, "locked extraction");
                var previousTarget = Get(worker, "targetNode");
                worker.CommandGather(spot);
                Assert(ReferenceEquals(Get(worker, "targetNode"), previousTarget), "locked worker command preserves previous task");
                rm.Add(ResourceType.Food, 100);
                rm.Add(ResourceType.Soil, 100);
                var definition = CampaignResearch.Technologies[(int)ScienceTechnology.Fishing];
                var food = rm.GetAmount(ResourceType.Food);
                var soil = rm.GetAmount(ResourceType.Soil);
                Assert(research.TryStart(ScienceTechnology.Fishing), "start science fishing");
                Assert(!research.TryStart(ScienceTechnology.Fishing), "concurrent research rejected");
                Assert(rm.GetAmount(ResourceType.Food) == food - definition.Food && rm.GetAmount(ResourceType.Soil) == soil - definition.Soil, "single research charge");
                // 불시착 시작 장수는 연구 금지 특성일 수 있어 연구 가능한 장수를 고른다.
                foreach (var x in CommanderRoster.Instance.Commanders) x.SetJobEnabled(CommanderJobs.Research, true);
                var researcher = CommanderRoster.Instance.Commanders.First(c => c.CivilianWorkReady && c.CanReceiveOrders && c.CanDoJob(CommanderJobs.Research));
                lab.transform.position = researcher.Position;
                Assert(lab.TryAssign(researcher), "assign scientist");
                labObject.SetActive(false);
                research.Tick(definition.Work * 10);
                Assert(!gm.FishingUnlocked && research.Progress == 0, "no researcher pauses science");
                labObject.SetActive(true);
                Assert(lab.TryAssign(researcher), "reassign scientist");
                research.Tick(definition.Work * 100);
                Assert(gm.FishingUnlocked && research.Has(ScienceTechnology.Fishing), "science completes global unlock");
                lab.ReleaseResearcher();
                researcher.SetJobEnabled(CommanderJobs.All, false);
                Assert(spot.CanGather && spot.GatherRateMultiplier == 2f, "unlock and fishing rate");
                newWorkerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                newWorkerObject.transform.position = worker.transform.position;
                var newWorker = newWorkerObject.AddComponent<CommanderAnt>();
                newWorker.Initialize(worker.Data, null, null);
                newWorker.CommandGather(spot);
                Assert((ResourceNode)Get(newWorker, "targetNode") == spot, "new worker inherits fishing unlock");
                Object.Destroy(newWorkerObject);
                Assert(!research.TryStart(ScienceTechnology.Fishing), "completed research cannot charge again");
                Object.Destroy(labObject);
                await Task.Delay(50);
                Assert(gm.FishingUnlocked && spot.CanGather, "learned fishing survives lab destruction");
                food = rm.GetAmount(ResourceType.Food);
                // 낮 100초 안에 이동·어획·반납을 끝내도록 다음 날 아침에서 시작한다(밤에는 수면으로 작업 중단).
                GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, (GameCalendar.Day + 1) * GameCalendar.SecondsPerDay);
                await Task.Delay(100);
                Assert(worker.TryWorkApproach(spot.transform.position, out var approach), "fishing approach lies on a reachable navmesh");
                worker.SetJobEnabled(CommanderJobs.Fishing, true);
                worker.CommandGather(spot);
                Assert(worker.CurrentResourceNode == spot && worker.IsWorking, "fisher accepts gathering order");
                var destination = approach; // 장거리 경유지 경로라면 최종 목적지를 반복 요청해야 한다.
                await Until(() =>
                {
                    AntColony.Map.NavRoute.SetDestination(worker.Agent, destination);
                    return !worker.Agent.pathPending;
                }, 10000, "repeated identical destination finishes path calculation", () =>
                    $"scale={Time.timeScale} pause={string.Join(",", UI.SimulationPause.Reasons)} pos={worker.Position} dest={destination} stopped={worker.Agent.isStopped}");
                Assert(worker.Agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete, "complete path to fishing approach");
                var startedGameSeconds = GameCalendar.GameSeconds;
                var trace = new System.Text.StringBuilder(); var last = ""; var catchStart = spot.AmountRemaining; // 날짜 이동으로 계절 보충이 일어날 수 있어 대기 직전 값을 기준으로 한다
                // 낚시는 20초마다 한 번 잡고(Food 6 × 낚시 배율) 창고에 반납한다.
                // 한 짐 운반량이 커져(2026-09-28 근력 규칙) 다 찰 때까지 기다리지 않고 첫 어획 뒤 바로 반납시킨다.
                // 낚시 자체만 검사한다: 식사·위생·피로 욕구로 작업을 놓지 않게 채워 둔다.
                await Until(() =>
                {
                    worker.PersonalState.meal.satiety = 100; worker.PersonalState.hygiene.hygiene = 100; worker.PersonalState.sleep.fatigue = 0;
                    var now = $"{Get(worker, "state")}/{UI.CommanderOverhead.Activity(worker)}/{worker.CurrentResourceNode?.name}/{worker.Agent.destination}";
                    if (now != last) { trace.Append($"[{GameCalendar.GameSeconds - startedGameSeconds:0.0}s {now} pos={worker.Position}] "); last = now; }
                    return spot.AmountRemaining < catchStart;
                }, 120000, "catch", () =>
                    $"trace={trace} scale={Time.timeScale} activity={UI.CommanderOverhead.Activity(worker)} joy={worker.PersonalState.joy.joy} simulated={GameCalendar.GameSeconds - startedGameSeconds:0.00}s screen={UI.GameMenuController.Instance.ScreenName} "
                    + $"state={Get(worker, "state")} target={worker.CurrentResourceNode?.name} pos={worker.Position} approach={approach} "
                    + $"nav={worker.Agent.isOnNavMesh} pending={worker.Agent.pathPending} path={worker.Agent.pathStatus} stopped={worker.Agent.isStopped} "
                    + $"dest={worker.Agent.destination} speed={worker.Agent.speed} asleep={worker.IsAsleep} orders={worker.CanReceiveOrders}");
                typeof(WorkerAnt).GetMethod("ReturnCargoToStorage", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(worker, null);
                await Until(() => rm.GetAmount(ResourceType.Food) > food, 60000, "deposit (stockpile " + (Object.FindAnyObjectByType<Stockpile>() != null) + ")");
                Assert(spot.AmountRemaining < catchStart, "real shoreline harvest and deposit");
                spot.Extract(1000);
                Assert(spot.FishedOut && !spot.CanGather && !spot.IsRegrowing, "seasonal fishing cap reached");
                typeof(ResourceNode).GetProperty("FishSeason", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(spot, GameCalendar.TotalSeasons - 1);
                spot.RefreshFishingSeason();
                Assert(spot.CanGather && spot.AmountRemaining == GameBalance.FishingSeasonFood, "next season restocks (G08)");
                return "PASS: locked command/extraction, single charge, concurrent research guard, scientist interruption/resume, unlock, lab destruction, real shoreline harvest/deposit, seasonal cap and restock.";
            }
            finally
            {
                UserSettings.Current.autoPauseThreat = savedAutoPause;
                if (labObject != null) Object.Destroy(labObject);
                if (newWorkerObject != null) Object.Destroy(newWorkerObject);
                Object.Destroy(secondObject);
                upkeep.enabled = wasEnabled;
                foreach (var threat in threats) if (threat != null) threat.enabled = true;
                research.RestoreState(savedResearch);
                typeof(GameManager).GetProperty("FishingUnlocked").SetValue(gm, savedUnlock);
                Object.Destroy(pileObject);
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
        static async Task Until(Func<bool> check, int milliseconds, string label = "", Func<string> diagnose = null)
        {
            var start = DateTime.UtcNow;
            while (!check() && (DateTime.UtcNow - start).TotalMilliseconds < milliseconds) await Task.Delay(20);
            Assert(check(), "timed out waiting for gameplay result " + label + (diagnose == null ? "" : " " + diagnose()));
        }
    }
}

