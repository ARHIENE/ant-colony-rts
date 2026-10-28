using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using AntColony.Save;
using UnityEngine;
using R = AntColony.Data.ResourceType;
using B = AntColony.Data.BuildingKind;
using Object = UnityEngine.Object;
public static class FoodIndustry1028Checks
{
    static int count;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("Food industry " + count + ": " + label); count++; File.WriteAllText("Temp/food-industry-progress.txt", count + " " + label); }
    static async Task Wait(Func<bool> condition, int seconds = 100) { var end = DateTime.UtcNow.AddSeconds(seconds); while (!condition() && DateTime.UtcNow < end) await Task.Delay(80); Check(condition(), "wait completed"); }
    static T Put<T>(B kind, Vector3 at) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, AntColony.Data.UnitRole.Worker });
        var go = Object.Instantiate(template, at, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true); return go.GetComponent<T>();
    }
    static float Floor(R t) => ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == t).Sum(n => n.AmountRemaining);
    static void Feed(Processor p, Storage store, int recipe)
    {
        foreach (var input in p.Recipes[recipe].inputs) ResourceManager.Instance.AddTo(store.Bin, input.type, input.amount);
        Check(p.AddOrder(recipe, Workshop.OrderMode.Count, 1), "order " + p.Recipes[recipe].name);
        foreach (var input in p.Recipes[recipe].inputs)
        {
            var courier = new object(); Check(p.BeginPickup(courier, input.type, input.amount), "reserve " + input.type);
            Check(ResourceManager.Instance.TrySpend(input.type, input.amount, ResourceReason.Production), "withdraw " + input.type);
            Check(p.Deliver(courier, input.type, input.amount) == input.amount, "deliver " + input.type);
        }
    }
    static void Park(CommanderAnt c, Processor p) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.Crafting, true); Check(c.StartService(p, CommanderJobs.Crafting), "assign worker"); c.Agent.Warp(c.Agent.destination); c.Agent.ResetPath(); }
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        count = 0; var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "FoodIndustry-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            await Wait(() => !SaveSystem.Busy);
            var team = Enumerable.Range(0, 3).Select(i => { var x = StartingCandidate.Roll(i); x.traits.values.Clear(); return x; }).ToArray();
            SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Small, startingCount = 3, startingTeam = team }); await Wait(() => !SaveSystem.Busy);
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 0;
            foreach (var person in CommanderRoster.Instance.Commanders) { person.CommandStop(); person.SetJobEnabled(CommanderJobs.All, false); }
            var c = CommanderRoster.Instance.Commanders[0]; var home = c.Position;
            var research = CampaignResearch.Instance; var rs = research.CaptureState(); rs.completed.Add((int)ScienceTechnology.Fermentation); rs.completed.Add((int)ScienceTechnology.UndergroundSurvey); research.RestoreState(rs);
            var store = Put<Storage>(B.Storage, home + Vector3.left * 3); var bin = store.Bin;
            Check(StoreBin.TypeCount == 78 && (int)R.Rubber >= 64, "expanded resource catalog crosses old 64 bit boundary");
            var alias = (R)((int)R.Rubber % 64); var oldAlias = bin.Allows(alias);
            ResourceManager.Instance.SetAllowed(bin, R.Rubber, false); Check(!bin.Allows(R.Rubber) && bin.Allows(alias) == oldAlias, "high filter does not alias low bit");
            Check(bin.Capture().extraForbidden.Contains((int)R.Rubber), "high filter serialized");
            foreach (FarmCrop crop in Enum.GetValues(typeof(FarmCrop)))
            {
                if (FoodProcessingRecipes.CropResource(crop) is not R type) continue;
                var source = ResourceNode.Available.FirstOrDefault(n => !n.IsLooseCargo && n.ResourceType == type && n.GetComponent<FarmSeedSource>() != null);
                Check(ResourceNode.Available.Count(n => !n.IsLooseCargo && n.ResourceType == type && n.GetComponent<FarmSeedSource>() != null) >= 2, "two seed sources support wide farms " + crop);
                Check(source != null && source.GetComponent<FarmSeedSource>().Crop == crop, "wild raw/seed source " + crop);
                Check(FarmSeeds.Collect(source.GetComponent<FarmSeedSource>()) && FarmSeeds.Count(crop) > 0, "collect seed " + crop);
                var field = Put<BuildingBase>(B.Farm, home + Vector3.forward * (8 + (int)crop * 2));
                var plot = field.GetComponent<FarmPlot>() ?? field.gameObject.AddComponent<FarmPlot>(); plot.Configure(crop, false);
                Check(plot.Node.ResourceType == type && plot.SeedCost > 0 && ScienceEffects.CropUnlocked(crop), "typed farm " + crop);
                var seeds = FarmSeeds.Count(crop); plot.Tick(1);
                Check(!plot.Data.seedPending && plot.Node.NeedsSowing && FarmSeeds.Count(crop) == seeds - plot.SeedCost, "typed seed consumed once " + crop);
                plot.Node.TickGrowth(1000); Check(plot.Node.NeedsSowing && plot.Node.IsDepleted, "cannot sow without a worker " + crop);
                if (crop == FarmCrop.Wheat)
                {
                    c.SetJobEnabled(CommanderJobs.Farming, true); c.CommandGather(plot.Node); c.Agent.Warp(c.Agent.destination); c.Agent.ResetPath(); Time.timeScale = 1;
                    await Wait(() => !plot.Node.NeedsSowing, 15); Time.timeScale = 0; c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false);
                    plot.Node.TickGrowth(100000); Check(plot.Node.AmountRemaining > 0 && plot.Node.ResourceType == R.Wheat, "actual sowing grows typed wheat");
                }
            }
            Check(ResourceNode.Available.Any(n => !n.IsLooseCargo && n.ResourceType == R.RubberSap), "natural rubber sap available");
            var mill = Put<Processor>(B.Mill, home + Vector3.right * 7); ResourceManager.Instance.AddTo(bin, R.Wheat, 2);
            Check(mill.AddOrder(0, Workshop.OrderMode.Count, 1) && mill.NeedsIngredients && !mill.NeedsWork, "mill requires physical delivery");
            WorkPriorities.Set(mill, 9, false); c.SetJobEnabled(CommanderJobs.Hauling, true); c.SetJobEnabled(CommanderJobs.Crafting, true);
            Time.timeScale = 1; c.TickDuty(2); await Wait(() => Floor(R.WheatFlour) + ResourceManager.Instance.GetAmount(R.WheatFlour) >= 1, 50);
            Time.timeScale = 0; c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false);
            Check(ResourceManager.Instance.GetAmount(R.Wheat) == 0 && mill.Current == null, "actual mill delivery/work finishes once");
            var press = Put<Processor>(B.OilPress, home + Vector3.forward * 4); Feed(press, store, 0); Park(c, press); Check(press.Work(c, 1000) && Floor(R.SunflowerOil) == 1, "pressed oil output"); c.CommandStop();
            var barrel = Put<Processor>(B.BrewingBarrel, home + Vector3.right * 12); Feed(barrel, store, 0); Park(c, barrel);
            Check(barrel.Work(c, 1000) && !barrel.NeedsWork && Floor(R.Beer) == 0, "raw inputs start unattended fermentation, no instant beer");
            c.CommandStop(); barrel.TickFermentation(FoodProcessingRecipes.FermentSeconds / 2); barrel.Paused = true; var status = barrel.FermentationStatus; barrel.TickFermentation(10000); Check(barrel.FermentationStatus == status, "paused fermentation preserved"); barrel.Paused = false;
            foreach (var food in new[] { R.WheatFlour, R.SunflowerOil, R.Mead, R.SimpleMeal }) Check(ResourceManager.Instance.AddTo(store.Bin, food, 2) == 2, "store food " + food);
            Check(SaveSystem.TrySave(false, 0, out var error), "save mid fermentation " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "reload fermentation " + error); await Wait(() => !SaveSystem.Busy); Time.timeScale = 0;
            c = CommanderRoster.Instance.Commanders[0]; store = SceneObjects.All<Storage>().Single(); barrel = SceneObjects.All<Processor>().Single(p => p.Brewing);
            Check(!store.Bin.Allows(R.Rubber) && store.Bin.Allows(alias) == oldAlias, "filter survives real reload");
            foreach (var food in new[] { R.WheatFlour, R.SunflowerOil, R.Mead, R.SimpleMeal }) Check(ResourceManager.Instance.GetAmount(food) >= 2, "food stock survives reload " + food);
            Check(barrel.FermentationStatus.Contains("50") && !barrel.NeedsWork, "fermentation halfway survives real reload");
            barrel.TickFermentation(FoodProcessingRecipes.FermentSeconds); Check(barrel.NeedsWork && Floor(R.Beer) == 0, "finished drink awaits worker recovery"); Park(c, barrel); Check(barrel.Work(c, 1) && Floor(R.Beer) == 2, "worker recovers finished batch"); c.CommandStop();
            Feed(barrel, store, 4); Park(c, barrel); Check(barrel.Work(c, 1000), "honey batch starts"); c.CommandStop(); var honey = Floor(R.Honey); barrel.CancelCurrent(); Check(barrel.Current == null && Floor(R.Honey) == honey, "cancelled fermentation returns no consumed raw material");
            var still = Put<Processor>(B.Still, home + Vector3.back * 6); Feed(still, store, 0); Park(c, still); Check(still.Work(c, 1000) && Floor(R.Spirits) == 1, "fermented alcohol and fuel distill into spirits"); c.CommandStop();
            var chemical = Put<Processor>(B.ChemistryBench, home + Vector3.back * 12); Feed(chemical, store, 1); chemical.Powered = true; Park(c, chemical); Time.timeScale = 1;
            Check(chemical.Work(c, 1000) && Floor(R.Rubber) == 1 && chemical.BufferedOil == 0, "rubber uses sap without taking crude oil"); Time.timeScale = 0; c.CommandStop();
            Check(SaveSystem.TrySave(false, 1, out error), "new chain final save " + error);
            var json = File.ReadAllText(SaveSlots.PathFor(false, 1)); Check(SaveValidator.TryParse(json, out var file, out error) && file.version == SaveFileV1.CurrentVersion, "new save version validated");
            file.version = 19; file.colony.baseBin.extraForbidden = null; foreach (var b in file.buildings) if (b.storeBin != null) b.storeBin.extraForbidden = null;
            Check(SaveValidator.Validate(file, out error) && file.version == SaveFileV1.CurrentVersion, "v19 save migrates without changing legacy bits");
            var corrupt = file.buildings.First(b => b.storeBin != null).storeBin; corrupt.extraForbidden = new System.Collections.Generic.List<int> { 9999 }; Check(!SavePreflight.Validate(file, out error), "unknown high filter ID rejected");
            var result = "PASS " + count + " typed raw/seed/farm, actual mill work, oil, fermentation/cancel/recovery/save, distillation, rubber and extended filter checks"; File.WriteAllText("Temp/food-industry-result.txt", result); return result;
        }
        catch (Exception e) { File.WriteAllText("Temp/food-industry-result.txt", e.ToString()); throw; }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
