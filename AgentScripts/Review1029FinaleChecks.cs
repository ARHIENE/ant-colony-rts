using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class Review1029FinaleChecks
{
    public static async Task<string> Main()
    {
        await Task.CompletedTask;
        int count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
        var economy = new AntColony.Core.RocketEconomy();
        economy.Observe(0, 100, 20); economy.Observe(300, 400, 100);
        var s = economy.Preview(900, 50, 10);
        Check(Math.Abs(s.averageWealth - 300) < .001 && Math.Abs(s.averagePopulation - 73.333333) < .001, "duration weighted inputs");
        Check(s.Wealth == 300 && Math.Abs(s.Population - 73.333333) < .001, "max current/season keeps all wealth");
        var fixedWealth = s.Wealth; economy.Observe(950, 10000, 10000);
        Check(s.Wealth == fixedWealth, "activation snapshot independent of population growth");
        economy.Observe(1850, 0, 0); var latest = economy.Preview(1850, 0, 0);
        Check(latest.Wealth == 10000, "entire last season retained");
        economy.Observe(2750, 0, 0); Check(economy.Preview(2750, 0, 0).Wealth == 0, "wealth ages out after full season");
        Check(AntColony.Buildings.AirshipYard.PreparationSeconds == 2250 && AntColony.Core.FinalBattleEconomy.DefenseSeconds == 2250, "15 days unchanged");
        foreach (int population in new[] { 20, 100, 500, 1000 })
        {
            // 기본10석·모듈50석·최대1010석. 인구는 장수까지 포함한 구출 대상 전체다.
            int modules = Math.Max(0, (int)Math.Ceiling((population - 10) / 50d));
            int seats = AntColony.Buildings.AirshipYard.BaseSeats + modules * AntColony.Buildings.AirshipYard.SeatsPerModule;
            Check(seats >= population && modules <= 20, "expand seats before activation " + population);
        }
        var progressState = new AntColony.Boss.BossProgress.State(); var progress = new AntColony.Boss.BossProgress(progressState);
        Check(!progress.TryConsumeRocketCore(), "activation cannot invent core");
        progressState.revealed[3] = true;
        Check(progress.RecordKill(AntColony.Boss.BossId.D, 0), "D killed");
        Check(progress.TryAcquire(AntColony.Boss.BossId.D, "party", out var parcel) && progress.TryDeliver(parcel.id), "D core imported");
        Check(progress.TryConsumeRocketCore() && progress.Home(AntColony.Boss.BossId.D) == 0, "one core consumed per activation");
        Check(!progress.TryConsumeRocketCore(), "destroyed rocket retry needs another core");
        Check(progress.Delivered(AntColony.Boss.BossId.D), "research delivery history retained");
        Check(AntColony.Boss.BossProgress.CraftCost(AntColony.Boss.BossId.D, true)[AntColony.Data.ResourceType.Steel] == 100, "core recovery alternative remains");
        var oldRocket = new AntColony.Buildings.AirshipYard.State();
        Check(AntColony.Buildings.AirshipYard.Validate(oldRocket, 0, out _), "old rocket default accepted");
        oldRocket.builders = null; Check(AntColony.Buildings.AirshipYard.Validate(oldRocket, 0, out _), "missing builder list old save");
        var construction = new AntColony.Save.ConstructionDto { kind = "ScienceLab", position = new AntColony.Save.Vec3Dto(),
            scale = new AntColony.Save.Vec3Dto(), buildingPosition = new AntColony.Save.Vec3Dto(), buildTime = 10, remaining = 5,
            rocket = new AntColony.Buildings.AirshipYard.State() };
        var validConstruction = typeof(AntColony.Save.ConstructionDto).Assembly.GetType("AntColony.Save.SaveConstruction")
            .GetMethod("Valid", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        bool ConstructionValid() => (bool)validConstruction.Invoke(null, new object[] { new List<AntColony.Save.ConstructionDto> { construction } });
        Check(ConstructionValid(), "ordinary construction accepts null-to-empty rocket placeholder");
        construction.hasRocketState = true; Check(!ConstructionValid(), "rocket marker cannot attach to another building kind");
        construction.kind = "AirshipYard"; Check(ConstructionValid(), "marked rocket construction validated by shared state path");
        var foundation = new AntColony.Buildings.AirshipYard.State { phase = AntColony.Buildings.RocketPhase.Destroyed,
            babelEligible = true, constructionWork = 100, builders = new List<AntColony.Buildings.RocketBuilderWork> {
                new AntColony.Buildings.RocketBuilderWork { id = "builder", work = 75 } } };
        Check(AntColony.Buildings.AirshipYard.Validate(foundation, 0, out _), "Babel 75 percent qualifies");
        foundation.builders[0].work = 74.99f; Check(!AntColony.Buildings.AirshipYard.Validate(foundation, 0, out _), "false eligibility rejected");
        foundation.builders[0].work = 100.1f; Check(!AntColony.Buildings.AirshipYard.Validate(foundation, 0, out _), "excess contribution rejected");
        Check(AntColony.Core.CampaignOutcome.Validate(new AntColony.Core.CampaignOutcome.State { legacy = null }), "old ending narrative optional");
        var ending = new AntColony.Core.CampaignOutcome.State { citizensEscaped = 10, citizensStaying = 20,
            legacy = new List<string> { "가족: 탈출 / 잔류", "재건 뒤 재도전" } };
        Check(AntColony.Core.CampaignOutcome.Validate(ending), "saved narrative valid");
        ending.legacy.Add(new string('x', 1001)); Check(!AntColony.Core.CampaignOutcome.Validate(ending), "oversize narrative rejected");
        Check(AntColony.World.DiplomacyManager.HomecomingBaseSeconds == 900, "safe homecoming 15min unchanged");
        Check(AntColony.World.DiplomacyManager.HomecomingTimeLabel(900).Contains("6.0일")
            && AntColony.World.DiplomacyManager.HomecomingTimeLabel(900).Contains("15.0분"), "return shown in current calendar");
        Check(AntColony.World.DiplomacyManager.HomecomingTimeLabel(0).Contains("도착 대기"), "blocked arrival not negative ETA");
        return $"PASS {count} finale/prisoner checks";
    }
}
