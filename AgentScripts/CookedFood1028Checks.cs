using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
using UnityEngine.UI;
using R = AntColony.Data.ResourceType;
using B = AntColony.Data.BuildingKind;
using Object = UnityEngine.Object;
public static class CookedFood1028Checks
{
    static int count;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("Cooked food " + count + ": " + label); count++; File.WriteAllText("Temp/cooked-food-progress.txt", count + " " + label); }
    static async Task Wait(Func<bool> condition, int seconds = 100) { var end = DateTime.UtcNow.AddSeconds(seconds); while (!condition() && DateTime.UtcNow < end) await Task.Delay(80); Check(condition(), "wait completed"); }
    static T Put<T>(B kind, Vector3 at) where T : BuildingBase
    {
        var prefab = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = Object.Instantiate(prefab, at, Quaternion.identity); go.SetActive(true); return go.GetComponent<T>();
    }
    static float Floor(R type) => ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == type).Sum(n => n.AmountRemaining);
    static void Feed(Kitchen k, Storage store, int recipe)
    {
        Check(k.AddOrder(recipe, Workshop.OrderMode.Count, 1), "add " + recipe);
        foreach (var need in Kitchen.Recipes[recipe].inputs)
        {
            ResourceManager.Instance.AddTo(store.Bin, need.type, need.amount);
            var courier = new object(); Check(k.BeginPickup(courier, need.type, need.amount), "reserve " + need.type);
            var lots = store.Bin.Lots(need.type); var shelf = lots?.Stalest ?? 0;
            var taken = ResourceManager.Instance.TakeFrom(store.Bin, need.type, need.amount, out var moved);
            Check(taken == need.amount && k.DeliverFood(courier, need.type, taken, moved) == taken, "deliver exact " + need.type);
            if (FoodLots.Perishable(need.type)) Check(k.Meals.ingredients.Single(s => s.type == need.type).lots.Stalest <= shelf, "freshness retained");
        }
    }
    static void Restore(Kitchen k, Kitchen.State state) => typeof(Kitchen).GetProperty("Meals").SetValue(k, state);
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        count = 0; var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "CookedFood-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false; UserSettings.Apply(quiet, false);
            await Wait(() => !SaveSystem.Busy);
            var team = Enumerable.Range(0, 3).Select(i => { var x = StartingCandidate.Roll(i); x.traits.values.Clear(); return x; }).ToArray();
            SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Small, startingCount = 3, startingTeam = team }); await Wait(() => !SaveSystem.Busy);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            foreach (var person in CommanderRoster.Instance.Commanders) { person.CommandStop(); person.SetJobEnabled(CommanderJobs.All, false); }
            var c = CommanderRoster.Instance.Commanders[0]; var home = c.Position;
            var store = Put<Storage>(B.Storage, home + Vector3.left * 3); var k = Put<Kitchen>(B.Hearth, home + Vector3.forward * 6); k.Orders.Clear();
            Check(StoreBin.TypeCount == 78 && SaveFileV1.CurrentVersion == 21, "catalog/save appended");
            Check(Kitchen.Recipes[0].output == R.SimpleMeal && Kitchen.Recipes[1].output == R.FineMeal && (int)R.Spirits == 74, "old ids unchanged");
            foreach (var t in new[] { R.CookedRice, R.Flatbread, R.FriedRice })
            {
                Check(t.IsFood() && t.IsEdible() && !t.IsMaterial() && FoodLots.Perishable(t) && ColdStorage.Holds(t), "edible cooked food " + t);
                var model = Resources.Load<GameObject>("ResourceModels/" + CookedFoods.Model(t));
                Check(model != null && model.GetComponentsInChildren<Renderer>().All(r => r.sharedMaterials.All(m => m != null && m.shader.name.StartsWith("Universal Render Pipeline/"))), "model/URP " + t);
            }
            ResourceManager.Instance.AddTo(store.Bin, R.Rice, 2); ResourceManager.Instance.AddTo(store.Bin, R.SunflowerOil, 1);
            Check(k.AddOrder(4, Workshop.OrderMode.Count, 1) && k.NeedsIngredients && !k.NeedsCook, "fried rice waits for real delivery");
            WorkPriorities.Set(k, 9, false); c.SetJobEnabled(CommanderJobs.Cooking, true); Time.timeScale = 1; c.TickDuty(2);
            await Wait(() => Floor(R.FriedRice) >= 1, 65); Time.timeScale = 0; c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false);
            Check(ResourceManager.Instance.GetAmount(R.Rice) == 0 && ResourceManager.Instance.GetAmount(R.SunflowerOil) == 0 && k.Staged == 0 && k.Current == null && k.Orders.Count == 0, "actual cook delivery/work consumes once");
            for (int i = 0; i < Kitchen.Recipes.Length; i++)
            {
                Feed(k, store, i); var recipe = Kitchen.Recipes[i]; var before = Floor(recipe.output);
                k.Paused = true; Check(!k.NeedsCook && !k.Work(c, 1000) && k.Staged == recipe.food, "pause preserves staged " + i); k.Paused = false;
                Check(k.Work(c, .1f) && k.Current == recipe && k.Staged == 0, "start consumes delivered inputs " + i);
                var progress = k.Progress;
                if (i == 3)
                {
                    k.Paused = true;
                    Check(SaveSystem.TrySave(false, 0, out var error), "save active bread " + error);
                    Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load active bread " + error); await Wait(() => !SaveSystem.Busy); Time.timeScale = 0;
                    c = CommanderRoster.Instance.Commanders[0]; store = SceneObjects.All<Storage>().Single(); k = SceneObjects.All<Kitchen>().Single(x => !x.IsTable);
                    Check(k.Paused && k.Current.output == R.Flatbread && Mathf.Abs(k.Progress - progress) < .01f && k.Staged == 0, "active bread progress/payment restored"); k.Paused = false;
                }
                Check(k.Work(c, 1000) && Floor(recipe.output) == before + 1 && k.Current == null, "finish exactly one " + i);
            }
            var legacyBread = new Kitchen.State { configured = true, pending = 3, orders = new System.Collections.Generic.List<Kitchen.Order> {
                new Kitchen.Order { recipe = 3, mode = Workshop.OrderMode.Count, amount = 1 } }, ingredients = new System.Collections.Generic.List<ProductionInputs.Stock> {
                new ProductionInputs.Stock { type = R.WheatFlour, amount = 2 } } };
            Check(Kitchen.Valid(legacyBread), "old two-flour staged bread remains valid");
            typeof(Kitchen).GetProperty("Meals").SetValue(k, legacyBread); var flourFloor = Floor(R.WheatFlour); var breadFloor = Floor(R.Flatbread);
            Check(k.Work(c, 1000) && k.Staged == 0 && Floor(R.Flatbread) == breadFloor + 1, "rebalance cooks old staged bread exactly once");
            Check(Floor(R.WheatFlour) == flourFloor + 1, "unused legacy flour returned to floor without loss");
            Feed(k, store, 4); var riceBefore = Floor(R.Rice); var oilBefore = Floor(R.SunflowerOil);
            k.RemoveOrder(k.Orders[0]); Check(k.Staged == 0 && Floor(R.Rice) == riceBefore + 2 && Floor(R.SunflowerOil) == oilBefore + 1, "delete pending returns typed inputs");
            Feed(k, store, 2); k.Work(c, .1f); riceBefore = Floor(R.Rice); k.CancelCurrent(); Check(k.Current == null && k.Staged == 0 && Floor(R.Rice) == riceBefore, "started cancellation no refund");
            Check(k.AddOrder(4, Workshop.OrderMode.Count, 1), "partial batch order");
            var nearSpoil = new FoodLots(); nearSpoil.Add(2, 2); Check(k.DeliverFood(new object(), R.Rice, 2, nearSpoil) == 2, "partial rice arrives");
            Check(!k.NeedsCook && !k.BeginPickup(new object(), R.WheatFlour, 1) && k.Deliver(new object(), R.Food, 2) == 0, "wrong type rejected and missing oil blocks cook");
            k.Paused = true; var rottenBefore = Floor(R.RottenFood); k.AgeIngredients(3); Check(k.Staged == 0 && Floor(R.RottenFood) == rottenBefore + 2, "paused typed input spoils"); k.Paused = false;
            Check(k.Deliver(new object(), R.Rice, 2) == 2, "restock partial rice");
            Check(SaveSystem.TrySave(false, 1, out var saveError), "save partial typed batch " + saveError);
            var json = File.ReadAllText(SaveSlots.PathFor(false, 1));
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 1), out saveError), "load partial typed batch " + saveError); await Wait(() => !SaveSystem.Busy); Time.timeScale = 0;
            c = CommanderRoster.Instance.Commanders[0]; store = SceneObjects.All<Storage>().Single(); k = SceneObjects.All<Kitchen>().Single(x => !x.IsTable);
            Check(k.Staged == 2 && k.Meals.pending == 4 && k.Meals.ingredients.Single().type == R.Rice && !k.NeedsCook, "partial rice/pending saved without substitution");
            Check(k.Deliver(new object(), R.SunflowerOil, 1) == 1 && k.Work(c, 1000), "complete restored mixed recipe");
            foreach (var t in CookedFoods.MealTypes) foreach (var bin in ResourceManager.Instance.Bins.ToArray()) if (bin.Amount(t) > 0) ResourceManager.Instance.TakeFrom(bin, t, bin.Amount(t));
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            foreach (var t in new[] { R.CookedRice, R.Flatbread, R.FriedRice })
            {
                ResourceManager.Instance.AddTo(store.Bin, t, 1); var meal = c.PersonalState.meal; meal.diet = CommanderAnt.AllDiet; meal.served = false; meal.pickedFood = 0; meal.retrySeconds = meal.eatSeconds = meal.poisonSeconds = 0; meal.satiety = 5;
                typeof(CommanderAnt).GetMethod("StartMeal", flags).Invoke(c, new object[] { null });
                Check(meal.eatSeconds > 0 && meal.quality == CookedFoods.Quality(t) && ResourceManager.Instance.GetAmount(t) == 0, "eat new food " + t);
                typeof(CommanderAnt).GetMethod("FinishMeal", flags).Invoke(c, null); meal.eatSeconds = 0; Check(c.Satiety == 100, "meal satisfies hunger " + t);
            }
            var original = JsonUtility.FromJson<Kitchen.State>(JsonUtility.ToJson(k.Meals));
            Restore(k, new Kitchen.State { configured = true, staged = 2, stagedLots = nearSpoil, orders = { new Kitchen.Order { recipe = 0, mode = Workshop.OrderMode.Count, amount = 1 } } });
            Check(k.Staged == 2 && k.Meals.ingredients.Single().type == R.Food && Kitchen.Valid(k.Meals), "legacy staged Food migration"); Restore(k, original);
            Check(SaveValidator.TryParse(json, out var file, out saveError), "new saved file valid " + saveError);
            file.version = 20; var ship = new TransportDto { cargoAmounts = new int[75], cargoFood = 0 }; ship.cargoAmounts[74] = 2; file.world.transports.Add(ship);
            Check(SaveValidator.Validate(file, out saveError) && file.version == 21 && ship.cargoAmounts.Length == 78 && ship.cargoAmounts[74] == 2 && ship.cargoAmounts.Skip(75).All(n => n == 0), "v20 cargo padding preserves old ids " + saveError);
            var corrupt = new Kitchen.State { configured = true, ingredients = new System.Collections.Generic.List<ProductionInputs.Stock> { new ProductionInputs.Stock { type = (R)999, amount = 1 } } };
            Check(!Kitchen.Valid(corrupt), "unknown staged type rejected");
            GameMenuController.Instance.ShowKitchen(k); await Task.Delay(80); Canvas.ForceUpdateCanvases();
            var texts = GameMenuController.Instance.GetComponentsInChildren<Text>().Select(x => x.text).ToArray();
            Check(texts.Any(x => x.Contains("밥 ← 쌀 2")) && texts.Any(x => x.Contains("납작빵 ← 밀가루 1")) && texts.Any(x => x.Contains("볶음밥 ← 쌀 2 + 해바라기유 1")), "UI typed costs");
            Check(GameMenuController.Instance.GetComponentsInChildren<Button>().Count(b => b.GetComponentInChildren<Text>()?.text.StartsWith("목록 추가:") == true) == 5, "UI keeps all five recipes in scroll");
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var result = "PASS " + count + " real typed cooking/delivery, legacy recipes, pause/freshness/cancel, active/partial save, eating, v20 migration, models and UI checks"; File.WriteAllText("Temp/cooked-food-result.txt", result); return result;
        }
        catch (Exception e) { File.WriteAllText("Temp/cooked-food-result.txt", e.ToString()); throw; }
        finally { GameMenuController.Instance.Resume(); Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
