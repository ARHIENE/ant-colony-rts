using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Units;
using AntColony.Save;
using UnityEngine;
public static class EconomyGrowth1028Checks
{
    static int count;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("Economy " + count + ": " + label); count++; }
    static async Task Wait() { var end = DateTime.UtcNow.AddSeconds(100); while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(80); Check(!SaveSystem.Busy, "ready"); }
    static void Pool(int count) => typeof(AntPool).GetMethod("RestoreCounts", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(AntPool.Instance, new object[] { count, 0, 0 });
    static float Accrue(ColonyPopulation pop, float work, float step, float total)
    {
        pop.S.adminWork = work; pop.S.taxFood = pop.S.taxSoil = 0;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        while (total > 0)
        {
            var dt = Mathf.Min(step, total); total -= dt;
            typeof(ColonyPopulation).GetMethod("AccrueTax", flags).Invoke(pop, new object[] { dt });
            typeof(ColonyPopulation).GetMethod("TickAdministration", flags).Invoke(pop, new object[] { dt });
        }
        return pop.S.taxFood + pop.S.taxSoil;
    }
    public static async Task<string> Main(bool baseline = false)
    {
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "EconomyGrowth-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            await Wait(); SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Small }); await Wait();
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 0;
            foreach (var c in CommanderRoster.Instance.Commanders) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); }
            var pop = ColonyPopulation.Instance; pop.S = new ColonyPopulation.State { taxRate = .2f, immigrationStopped = true };
            var report = new StringBuilder("Population: all adult, volunteer, tax20%, balanced, no research/advanced housing/facilities, no admin\nN,taxpayers,food/day,materials/day,serviceCoverage,adminWork/day,mobilization\n");
            foreach (var n in new[] { 20, 100, 500, 1000 })
            {
                Pool(n); pop.S.adminWork = 0;
                var food = pop.MonthlyTax * ColonyPopulation.FoodShare(pop.S.taxFocus) * ColonyPopulation.TaxSeconds / GameCalendar.SecondsPerMonth;
                report.AppendLine($"{n},{pop.Taxpayers},{food:0.####},{food:0.####},{pop.PublicServiceCoverage:0.###},{pop.AdminDemandWork:0.###},{pop.MobilizationLimit(false)}");
                Check(Mathf.Abs(food - n * .2125f) < .001f, "base tax " + n);
                Check(Mathf.Abs(pop.AdminDemandWork - n / 500f) < .00001f && pop.Taxpayers == n, $"workload/taxpayer {n} actualWork={pop.AdminDemandWork:R} total={pop.Total} payers={pop.Taxpayers}");
            }
            Pool(100); var required = pop.AdminDemandWork;
            var single = Accrue(pop, required, 150, 150); var frames = Accrue(pop, required, 1, 150);
            report.AppendLine($"Admin decay tax, 150-second tick={single:0.######}, one-second ticks={frames:0.######}, delta={single-frames:0.######}");
            if (!baseline) Check(Mathf.Abs(single - frames) < .001f, "tax independent of time subdivision during admin decay");
            for (var i = 0; i <= 4; i++)
            {
                var work = required * i / 4; single = Accrue(pop, work, 150, 150); frames = Accrue(pop, work, .5f, 150);
                if (!baseline) Check(Mathf.Abs(single - frames) < .002f, "partial admin integration " + i);
            }
            if (!baseline)
            {
                pop.S.adminWork = required * .5f; pop.S.taxFood = 2.75f; pop.S.taxSoil = 1.5f; pop.S.taxWeek = 75;
                var predictedFood = pop.NextFoodTax; var predictedMaterial = pop.NextMaterialTax;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(ColonyPopulation).GetMethod("AccrueTax", flags).Invoke(pop, new object[] { 75f });
                typeof(ColonyPopulation).GetMethod("TickAdministration", flags).Invoke(pop, new object[] { 75f });
                Check(predictedFood == Mathf.FloorToInt(pop.S.taxFood) && predictedMaterial == Mathf.FloorToInt(pop.S.taxSoil), "next payment forecast includes accrued fraction and remaining admin decay");
                pop.S.taxWeek = ColonyPopulation.TaxSeconds;
                Check(pop.NextFoodTax == Mathf.FloorToInt(pop.S.taxFood), "zero remaining time forecasts accrued amount only");
                var saved = pop.Capture(); var preview = pop.NextFoodTax; pop.Restore(saved);
                Check(preview == pop.NextFoodTax, "forecast after population state restore");
            }
            var xp = Enumerable.Range(0, 20).Sum(CommanderTalents.Required);
            Check(xp == 21000 && GameCalendar.SecondsPerYear == 3600, "growth/calendar baseline");
            report.AppendLine("Skill from0 to20: XP=" + xp + "; real hours assuming learning1, work fraction50%, main-share80%=" + (xp / .5f / .8f / 3600));
            report.AppendLine("Passion2 reduces that to " + (xp / .5f / .8f / 2f / 3600) + " hours; PA/head injury/other work may slow it.");
            report.AppendLine("Ten hours at1x=10 game years; starting adults18-30 reach28-40, child growth to18 takes1 game year. Old age is not promised in a ten-hour escape run.");
            report.AppendLine("Hut30 material /20 seats: filled housing's material-only tax payback without admin = " + (30f / (20 * .2125f)) + " game days; immigration delay and other costs excluded.");
            var talents = new CommanderTalents { potential = 200 }; talents.Train(CommanderActivity.Cooking, 100, _ => 1);
            Check(Mathf.Abs(talents.Xp(CommanderActivity.Cooking) - 80) < .001f && Mathf.Abs(talents.Xp(CommanderActivity.Crafting) - 20) < .001f, "real main/support split");
            var result = (baseline ? "BASELINE " : "PASS ") + count + " economic/growth checks\n" + report;
            File.WriteAllText("Temp/economy-growth-" + (baseline ? "before" : "result") + ".txt", result); return result;
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
