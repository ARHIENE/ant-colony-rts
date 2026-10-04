using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// 3번 자원 규칙: 기본 균류 180초/Food40, 가을 수확 ×1.25, 겨울 성장 정지, 낚시 20초/Food6×배율, 낚시터 월 100 한도·저장.
public static class ResourceRuleChecks
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static int checks;
    static void Check(bool ok, string text) { if (!ok) throw new Exception("FAIL " + text); checks++; }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .01f;
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static void SetMonth(int totalMonths) => typeof(GameSession).GetProperty("GameSeconds").SetValue(GameSession.Instance, totalMonths * GameCalendar.SecondsPerMonth + 1);
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode"); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ResRules-" + Guid.NewGuid().ToString("N"));
        GameObject farmObject = null;
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 260927, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            foreach (var x in CommanderRoster.Instance.Commanders) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }

            // 밭: 새로 심은 기본 균류는 180초 전체를 자란 뒤 Food 40.
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { BuildingKind.Farm, UnitRole.Worker });
            Check(template != null, "farm template");
            farmObject = Object.Instantiate(template); farmObject.name = "Rule farm"; farmObject.SetActive(true);
            var farm = farmObject.GetComponent<ResourceNode>();
            farmObject.AddComponent<FarmPlot>().Configure(FarmCrop.Fungus, false);
            Check(farm.RegrowSeconds == GameBalance.FungusSeconds && farm.RegrowAmount == GameBalance.FungusFood, "fungus 180s / Food 40");
            Check(Near(farm.RegrowTimeRemaining, 180), "fresh farm grows full 180s, got " + farm.RegrowTimeRemaining);
            SetMonth(9); Check(GameCalendar.CurrentSeason == Season.Winter, "winter month");
            // 밭은 농사 중인 장수가 있어야 자란다(속도 = 장수 작업 속도 합). 장수 하나를 농사 상태로 둔다.
            var farmer = CommanderRoster.Instance.Commanders[0];
            typeof(WorkerAnt).GetField("targetNode", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(farmer, farm);
            var farmState = typeof(WorkerAnt).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);
            farmState.SetValue(farmer, Enum.Parse(farmState.FieldType, "Gathering"));
            Check(farmer.IsGatheringAnimation && farmer.CurrentResourceNode == farm, "farmer working the farm");
            farm.TickGrowth(60); Check(Near(farm.RegrowTimeRemaining, 180), "winter stops growth");
            SetMonth(0); farm.TickGrowth(60); Check(farm.RegrowTimeRemaining < 180, "spring grows: " + farm.RegrowTimeRemaining);
            farm.TickGrowth(100000); var springYield = farm.AmountRemaining;
            Check(Near(springYield, GameBalance.FungusFood * ScienceEffects.FarmYieldMultiplier), "spring harvest x1: " + springYield);
            farm.Extract(1000); Check(farm.IsRegrowing, "harvest restarts growth");
            SetMonth(6); Check(GameCalendar.CurrentSeason == Season.Autumn, "autumn month");
            farm.TickGrowth(100000); Check(Near(farm.AmountRemaining, springYield * 1.25f), "autumn harvest x1.25: " + farm.AmountRemaining);
            farm.Extract(1000); farm.GetComponent<FarmPlot>().Configure(FarmCrop.Honeydew, false);
            Check(farm.RegrowSeconds == GameBalance.HoneydewSeconds && Near(farm.RegrowTimeRemaining, GameBalance.HoneydewSeconds), "replanting after harvest uses new crop time");
            SetMonth(0); farm.TickGrowth(10); farm.GetComponent<FarmPlot>().Configure(FarmCrop.Fungus, false);
            Check(Near(farm.RegrowTimeRemaining, GameBalance.FungusSeconds), "mid-growth crop change clamps to new time");
            farmer.CommandStop(); Object.Destroy(farmObject); farmObject = null;

            // 낚시터: 월 한도 100, 소진 후 다음 달 회복.
            var gm = GameManager.Instance; typeof(GameManager).GetProperty("FishingUnlocked").SetValue(gm, true);
            var spot = GameObject.Find("FishingSpot").GetComponent<ResourceNode>();
            SetMonth(1); spot.RefreshFishingMonth();
            Check(spot.AmountRemaining == GameBalance.FishingMonthlyFood && spot.FishMonthPublic() == 1 && spot.CanGather, "monthly stock 100");
            Check(Near(spot.Extract(1000), 100) && spot.FishedOut && !spot.CanGather && spot.gameObject.activeSelf && !spot.IsRegrowing, "cap reached, no regrowth");
            spot.RefreshFishingMonth(); Check(spot.FishedOut, "same month stays empty");
            SetMonth(2); spot.RefreshFishingMonth(); Check(spot.AmountRemaining == 100 && spot.CanGather, "next month restocks");

            // 낚시 1회: 20초마다 Food 6 × 낚시 배율.
            var c = CommanderRoster.Instance.Commanders[0];
            typeof(WorkerAnt).GetField("targetNode", Private).SetValue(c, spot);
            var stateField = typeof(WorkerAnt).GetField("state", Private);
            stateField.SetValue(c, Enum.Parse(stateField.FieldType, "Gathering"));
            typeof(WorkerAnt).GetMethod("RestoreCargo", Private).Invoke(c, new object[] { 0f, AntColony.Data.ResourceType.Food });
            var multiplier = (float)typeof(CommanderAnt).GetProperty("FishingCatchMultiplier", Private).GetValue(c);
            Check(Near(multiplier, c.Talents.Multiplier(CommanderActivity.Fishing)), "commander multiplier = fishing skill (no cold)");
            var tick = typeof(WorkerAnt).GetMethod("TickGathering", Private);
            typeof(WorkerAnt).GetField("fishingProgress", Private).SetValue(c, 5f);
            Time.timeScale = 1; await Task.Yield(); tick.Invoke(c, null);
            Check(c.CarriedAmount == 0, "no catch before 20s");
            typeof(WorkerAnt).GetField("fishingProgress", Private).SetValue(c, GameBalance.FishingCatchSeconds);
            // 프레임 사이 자율 판단이 대상을 비울 수 있어 직전에 다시 지정한다.
            typeof(WorkerAnt).GetField("targetNode", Private).SetValue(c, spot); stateField.SetValue(c, Enum.Parse(stateField.FieldType, "Gathering"));
            tick.Invoke(c, null); Time.timeScale = 0;
            var expected = Mathf.Min(GameBalance.FishingCatchFood * multiplier, 100);
            Check(Near(c.CarriedAmount, expected) && Near(spot.AmountRemaining, 100 - expected), $"one catch = 6 x skill ({c.CarriedAmount} vs {expected})");
            c.CommandStop(); typeof(WorkerAnt).GetMethod("RestoreCargo", Private).Invoke(c, new object[] { 0f, AntColony.Data.ResourceType.Food });

            // 저장 왕복: 이번 달 잔량과 월 기록이 유지되어 불러와도 다시 채워지지 않는다.
            spot.Extract(60); var left = spot.AmountRemaining;
            var fisher = CommanderRoster.Instance.Commanders[0]; var fisherName = fisher.CommanderName;
            typeof(WorkerAnt).GetField("fishingProgress", Private).SetValue(fisher, 12f);
            Check(SaveSystem.TrySave(false, 0, out var error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready(); await Task.Delay(300);
            spot = GameObject.Find("FishingSpot").GetComponent<ResourceNode>(); spot.RefreshFishingMonth();
            Check(Near(spot.AmountRemaining, left) && spot.FishMonthPublic() == GameCalendar.TotalMonths, $"fishing month survives save/load amt={spot.AmountRemaining} left={left} fm={spot.FishMonthPublic()} tm={GameCalendar.TotalMonths}");
            fisher = CommanderRoster.Instance.Commanders.First(x => x.CommanderName == fisherName);
            Check(Near((float)typeof(WorkerAnt).GetField("fishingProgress", Private).GetValue(fisher), 12), "fishing progress survives save/load");
            return "PASS ResourceRuleChecks " + checks;
        }
        finally { if (farmObject != null) Object.Destroy(farmObject); SaveStorage.RootOverride = root; Time.timeScale = 0; }
    }
    static int FishMonthPublic(this ResourceNode n) => (int)typeof(ResourceNode).GetProperty("FishMonth", Private).GetValue(n);
}
