using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.Save;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using R = AntColony.Data.ResourceType;
using B = AntColony.Data.BuildingKind;
using Object = UnityEngine.Object;
public static class RemainingIndustry1028Checks
{
    static int count;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("Remaining industry " + count + ": " + label); count++; File.WriteAllText("Temp/remaining-industry-progress.txt", count + " " + label); }
    static float Floor(R type) => ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == type).Sum(n => n.AmountRemaining);
    static async Task Wait(Func<bool> ready, int seconds = 90) { var end = DateTime.UtcNow.AddSeconds(seconds); while (!ready() && DateTime.UtcNow < end) await Task.Delay(80); Check(ready(), "wait completed"); }
    static T Put<T>(B kind, Vector3 position) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, AntColony.Data.UnitRole.Worker });
        var go = Object.Instantiate(template, position, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true); return go.GetComponent<T>();
    }
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        count = 0; var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "RemainingIndustry-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            await Wait(() => !SaveSystem.Busy);
            var team = Enumerable.Range(0, 3).Select(i => { var c = StartingCandidate.Roll(i); c.traits.values.Clear(); return c; }).ToArray();
            SaveSystem.NewGame(new NewGameOptions { seed = 281028, startingCount = 3, startingTeam = team }); await Wait(() => !SaveSystem.Busy);
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 0; await Task.Delay(250);
            foreach (var unit in CommanderRoster.Instance.Commanders) { unit.CommandStop(); unit.SetJobEnabled(CommanderJobs.All, false); }
            var c = CommanderRoster.Instance.Commanders[0]; var start = c.Position;
            var store = Put<Storage>(B.Storage, start + Vector3.left * 3);
            ResourceManager.Instance.AddTo(store.Bin, R.Wood, 20);
            var kiln = Put<Processor>(B.CharcoalKiln, start + Vector3.right * 7);
            Check(kiln != null && !SupplyFacility.IsKind(B.CharcoalKiln), "new kiln is a processor, not a fluid facility");
            Check(kiln.AddOrder(0, Workshop.OrderMode.Count, 1), "charcoal order");
            Check(kiln.NeedsIngredients && !kiln.NeedsWork, "raw stock cannot bypass physical delivery");
            WorkPriorities.Set(kiln, 9, false); c.SetJobEnabled(CommanderJobs.Hauling, true); c.SetJobEnabled(CommanderJobs.Crafting, true);
            Time.timeScale = 1; c.TickDuty(2); await Wait(() => Floor(R.Charcoal) >= 1 || ResourceManager.Instance.GetAmount(R.Charcoal) >= 1, 55);
            Time.timeScale = 0; c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false);
            Check(ResourceManager.Instance.GetAmount(R.Wood) == 18, "actual delivery and work consume exactly two wood");
            Check(kiln.Current == null && kiln.Orders.Count == 0, "count order finishes once");
            var kinds = new[] { B.CokeOven, B.Kiln, B.Smelter };
            foreach (var kind in kinds)
            {
                var p = Put<Processor>(kind, start + Vector3.forward * (8 + Array.IndexOf(kinds, kind) * 5));
                var recipe = p.Recipes.Select((r, i) => (r, i)).First(x => kind == B.CokeOven ? x.r.output == R.Coke : kind == B.Kiln ? x.r.output == R.Pottery && x.r.inputs.Any(n => n.type == R.Charcoal) : x.r.output == R.Iron && x.r.inputs.Any(n => n.type == R.Coke));
                foreach (var input in recipe.r.inputs) ResourceManager.Instance.AddTo(store.Bin, input.type, input.amount + 2);
                Check(p.AddOrder(recipe.i, Workshop.OrderMode.Count, 1), kind + " order accepted");
                foreach (var input in recipe.r.inputs)
                {
                    var courier = new object(); Check(p.BeginPickup(courier, input.type, input.amount), "reserve " + input.type);
                    Check(ResourceManager.Instance.TrySpend(input.type, input.amount, ResourceReason.Production), "take " + input.type);
                    Check(p.Deliver(courier, input.type, input.amount) == input.amount, "deliver " + input.type);
                }
                c.SetJobEnabled(CommanderJobs.Crafting, true);
                Check(c.StartService(p, CommanderJobs.Crafting), "assign " + kind);
                Check(p.Work(c, .1f) && p.Current != null, "start " + kind);
                var before = p.Progress; p.Paused = true; Check(!p.Work(c, 1000) && p.Progress == before, "pause retains unfinished batch"); p.Paused = false;
                Check(c.StartService(p, CommanderJobs.Crafting), "resume assignment");
                var state = typeof(Processor).GetMethod("CaptureState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(p, null);
                typeof(Processor).GetMethod("RestoreState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(p, new[] { state });
                var floor = Floor(recipe.r.output); Check(p.Work(c, 1000), "finish after state restore");
                Check(Floor(recipe.r.output) == floor + recipe.r.amount && p.Current == null, "one output batch");
                c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); await Task.Delay(100);
                Check(p.GetComponent<FacilityVisual>()?.Model != null && p.GetComponent<Renderer>().forceRenderingOff, "facility model connected");
            }
            var legacy = Processor.RecipesFor(B.Smelter); Check(legacy[0].output == R.Iron && legacy[1].output == R.Copper && legacy[2].output == R.Steel && legacy[3].output == AntColony.Data.ResourceType.Bronze, "legacy save recipe indexes unchanged");
            Check(ReferenceEquals(legacy, Processor.RecipesFor(B.Smelter)), "immutable recipe catalog reused");
            Check(StoreBin.TypeCount == Enum.GetValues(typeof(R)).Length, "resource catalog matches expanded storage");
            foreach (var pair in new[] { (kind: B.WoodGenerator, primary: R.Wood, fallback: R.Charcoal), (kind: B.SteamGenerator, primary: R.Coal, fallback: R.Coke) })
            {
                var rm = ResourceManager.Instance;
                rm.TrySpend(pair.primary, rm.GetAmount(pair.primary), ResourceReason.Production);
                rm.AddTo(store.Bin, pair.fallback, 2);
                var at = start + Vector3.left * (15 + (pair.kind == B.SteamGenerator ? 8 : 0));
                var generator = Put<PowerNode>(pair.kind, at); Put<PowerNode>(B.ElectricLamp, at + Vector3.forward);
                var fuel = rm.GetAmount(pair.fallback); PowerGrid.Tick(.5f); Time.timeScale = 1;
                await Wait(() => generator.FuelSeconds > 0, 5); Time.timeScale = 0;
                Check(rm.GetAmount(pair.fallback) == fuel - 1, pair.kind + " burns exactly one fallback fuel");
                Check(generator.Output > 0, pair.kind + " actually generates power");
                var seconds = generator.FuelSeconds; await Task.Delay(100);
                Check(generator.FuelSeconds == seconds, "pause preserves burning fuel");
            }
            foreach (var type in new[] { R.Charcoal, R.Coke, R.Pottery })
            {
                var node = ResourceNode.Available.First(n => n.IsLooseCargo && n.ResourceType == type); await Task.Delay(80);
                Check(node.transform.Find("ResourceModel") != null && node.GetComponent<Renderer>().forceRenderingOff, type + " floor model connected");
            }
            var expected = new[] { Floor(R.Charcoal), Floor(R.Coke), Floor(R.Pottery) };
            Check(SaveSystem.TrySave(false, 0, out var error), "save " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load " + error); await Wait(() => !SaveSystem.Busy); Time.timeScale = 0;
            Check(Floor(R.Charcoal) == expected[0] && Floor(R.Coke) == expected[1] && Floor(R.Pottery) == expected[2], "new resources survive real save reload");
            Check(SceneObjects.All<Processor>().Any(p => p.Data.kind == B.CokeOven) && SceneObjects.All<Processor>().Any(p => p.Data.kind == B.CharcoalKiln), "new facilities survive real save reload");
            var result = "PASS " + count + " new processing, actual charcoal delivery/work, fuel variants, legacy indices, models and save reload checks"; File.WriteAllText("Temp/remaining-industry-result.txt", result); return result;
        }
        catch (Exception e) { File.WriteAllText("Temp/remaining-industry-result.txt", e.ToString()); throw; }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
