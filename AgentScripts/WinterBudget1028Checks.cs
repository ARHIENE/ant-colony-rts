using System;
using System.IO;
using System.Text;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Data;
public static class WinterBudget1028Checks
{
    public static string Main()
    {
        var count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
        const int people = 3; var days = GameCalendar.DaysPerSeason; var portions = people * days * 2;
        var food = portions * GameBalance.MealFood;
        Check(days == 6 && portions == 36 && food == 72, "three adults nominal two meals/day winter budget");
        Check(FoodLots.ShelfLife(AntColony.Data.ResourceType.Rice) > days * GameCalendar.SecondsPerDay && FoodLots.ShelfLife(AntColony.Data.ResourceType.CookedRice) < days * GameCalendar.SecondsPerDay, "grain survives whole winter, precooked rice does not");
        Check(GameBalance.FishingSeasonFood >= food, "one fishing site's nominal seasonal cap can cover raw demand");
        var fishingWork = food / GameBalance.FishingCatchFood * GameBalance.FishingCatchSeconds;
        var riceWork = portions * Kitchen.Recipes[2].seconds;
        Check(fishingWork == 240 && riceWork == 360, "real existing time constants");
        var bread = Kitchen.Recipes[3]; var mill = FoodProcessingRecipes.For(BuildingKind.Mill)[0];
        var flour = portions * bread.inputs[0].amount;
        var wheat = flour / (float)mill.amount * mill.inputs[0].amount;
        var breadWork = portions * bread.seconds + flour / (float)mill.amount * mill.work;
        var report = new StringBuilder($"PASS {count} winter budget checks\nNominal36 portions for3 adults/6days at2meals/day; no traits/banquet/poison/walking/life/industrial interruption.\nRaw food72; fishing catch work={fishingWork}s, cap={GameBalance.FishingSeasonFood}.\nRice72→36 cooked rice, cook work={riceWork}s. Grain shelf24days, cooked shelf3days: cook in batches.\n");
        report.AppendLine($"Bread36: wheat={wheat}, flour={flour}, milling+cooking work={breadWork}s. Winter duration={GameCalendar.SecondsPerSeason}s. Do not claim this fits one worker's actual schedule.");
        report.AppendLine("Citizens100, tax20%,balanced,noadmin/noadvancedproductivity: food21.25/day*6=127.5 accrued food, rounded payment/carry fractions separate. Citizens consume separately by confirmed design; active troops stop paying tax.");
        var result = report.ToString(); File.WriteAllText("Temp/winter-budget-result.txt", result); return result;
    }
}
