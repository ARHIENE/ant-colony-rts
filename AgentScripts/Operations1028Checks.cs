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
public static class Operations1028Checks
{
    static int count;
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("Operations " + count + ": " + label); count++; File.WriteAllText("Temp/operations-progress.txt", count + " " + label); }
    static async Task Wait(Func<bool> condition) { var end = DateTime.UtcNow.AddSeconds(100); while (!condition() && DateTime.UtcNow < end) await Task.Delay(80); Check(condition(), "wait completed"); }
    static T Put<T>(B kind, Vector3 at) where T : BuildingBase
    {
        var prefab = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = Object.Instantiate(prefab, at, Quaternion.identity); go.SetActive(true); return go.GetComponent<T>();
    }
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Operations-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false; UserSettings.Apply(quiet, false);
            await Wait(() => !SaveSystem.Busy);
            var team = Enumerable.Range(0, 3).Select(i => { var x = StartingCandidate.Roll(i); x.traits.values.Clear(); return x; }).ToArray();
            SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Small, startingCount = 3, startingTeam = team }); await Wait(() => !SaveSystem.Busy);
            var menu = GameMenuController.Instance; menu.Resume(); Time.timeScale = 0;
            foreach (var person in CommanderRoster.Instance.Commanders) { person.CommandStop(); person.SetJobEnabled(CommanderJobs.All, false); person.PersonalState.carriedRations = new FoodLots(); }
            var c = CommanderRoster.Instance.Commanders[0]; var home = c.Position; c.PersonalState.meal.diet = CommanderAnt.AllDiet;
            var store = Put<Storage>(B.Storage, home + Vector3.left * 3); var k = Put<Kitchen>(B.Hearth, home + Vector3.forward * 6); k.Orders.Clear();
            var rm = ResourceManager.Instance;
            rm.AddTo(store.Bin, R.Food, c.RationPortion * 2); rm.AddTo(store.Bin, R.CookedRice, 2); rm.AddTo(store.Bin, R.FineMeal, 3); rm.AddTo(store.Bin, R.WheatFlour, 4);
            var data = FoodAvailability.Read(c);
            Check(FoodAvailability.StoredEdibleCount == c.RationPortion * 2 + 5, "HUD counts normal cooked food but excludes flour");
            Check(data.AvailableMeals == 7, "raw portion and cooked meal units distinguished");
            c.PersonalState.meal.diet = CommanderAnt.DietRaw; data = FoodAvailability.Read(c);
            Check(data.rows.Single(r => r.type == R.CookedRice).available == 0 && data.AvailableMeals == 2, "individual diet filters availability");
            c.PersonalState.meal.diet = 0; data = FoodAvailability.Read(c);
            Check(data.AvailableMeals == 0 && data.Summary.Contains("금지"), "food-present starvation explains diet prohibition");
            c.PersonalState.carriedRations.Add(c.RationPortion, 100);
            Check(FoodAvailability.Read(c).Summary.Contains("휴대 보존식") && FoodAvailability.Read(c).Summary.Contains("금지"), "carried ration still respects raw diet prohibition"); c.PersonalState.carriedRations = new FoodLots();
            c.PersonalState.meal.diet = CommanderAnt.AllDiet;
            var rottenOnly = new FoodAvailability { observer = c }; rottenOnly.rows.Add(new FoodAvailability.Row { type = R.RottenFood, stored = 2 });
            Check(rottenOnly.Summary.Contains("썩은 식품만"), "rotten-only supply is not described as cooking ingredients");
            var lots = store.Bin.Lots(R.FineMeal); lots.lots.Clear(); lots.Add(3, 2);
            data = FoodAvailability.Read(c); Check(data.rows.Single(r => r.type == R.FineMeal).spoiling == 3, "warehouse food near spoil shown without renewing freshness");
            typeof(DiplomacyManager).GetMethod("DropFloor", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(R), typeof(int), typeof(Vector3), typeof(FoodLots) }, null).Invoke(null, new object[] { R.CookedRice, 1, home, null });
            Check(k.AddOrder(2, Workshop.OrderMode.Count, 1) && k.Deliver(new object(), R.Rice, 2) == 2, "staged rice for report");
            typeof(WorkerAnt).GetMethod("RestoreCargo", Flags).Invoke(c, new object[] { 2f, R.Flatbread, null });
            var marker = new GameObject("operations construction"); marker.transform.position = home + Vector3.right * 6;
            var site = marker.AddComponent<BuildingConstructionSite>(); site.Require(R.Food, 3); site.Needs[0].reserved = 2;
            data = FoodAvailability.Read(c);
            Check(data.rows.Single(r => r.type == R.CookedRice).floor >= 1 && data.rows.Single(r => r.type == R.Flatbread).carried == 2, "floor and actual carried cargo separated");
            Check(data.rows.Single(r => r.type == R.Rice).cooking == 2 && data.rows.Single(r => r.type == R.Rice).stored == 0, "staged ingredient is not warehouse stock");
            var raw = data.rows.Single(r => r.type == R.Food); Check(raw.construction == 2 && raw.available == raw.stored && site.Needs[0].reserved == 2, "soft construction allocation reported without false stock subtraction or mutation");
            typeof(WorkerAnt).GetMethod("RestoreCargo", Flags).Invoke(c, new object[] { 0f, R.Food, null });
            var original = store.transform.position; store.transform.position = new Vector3(10000, home.y, 10000);
            data = FoodAvailability.Read(c); Check(data.AvailableMeals == 0 && data.rows.Single(r => r.type == R.Food).remote == c.RationPortion * 2, "other-region warehouse is not local available food"); store.transform.position = original;
            Check(WorkDiagnostics.Describe(k).Contains("작업 금지"), "disabled cooking explains work table fix");
            c.SetJobEnabled(CommanderJobs.Cooking, true); Check(!WorkDiagnostics.Describe(k).Contains("작업 금지"), "allowed cooking no false prohibition");
            k.RemoveOrder(k.Orders[0]); k.AddOrder(4, Workshop.OrderMode.Count, 1);
            Check(WorkDiagnostics.Describe(k).Contains("해바라기유") && WorkDiagnostics.Describe(k).Contains("창고"), "mixed recipe missing exact ingredient and stock shown");
            k.Paused = true; Check(WorkDiagnostics.Describe(k).Contains("재개"), "paused work gives resume action"); k.Paused = false;
            k.RemoveOrder(k.Orders[0]); Check(WorkDiagnostics.Describe(k).Contains("추가"), "empty list is normal configuration wait");
            var mill = Put<Processor>(B.Mill, home + Vector3.right * 10); mill.AddOrder(0, Workshop.OrderMode.Count, 1); c.SetJobEnabled(CommanderJobs.Crafting, true);
            Check(WorkDiagnostics.Describe(mill).Contains("밀") && WorkDiagnostics.Describe(mill).Contains("도착"), "unaffordable processor previews exact missing material");
            menu.ShowDiet(c); await Task.Delay(80); Canvas.ForceUpdateCanvases();
            var text = menu.GetComponentsInChildren<Text>().Select(t => t.text).ToArray();
            Check(text.Any(t => t.StartsWith("식량 상태")) && text.Any(t => t.Contains("부패 임박")) && text.Any(t => t.Contains("건설 배정")), "existing diet screen shows availability breakdown");
            menu.ShowTasks(new System.Collections.Generic.List<Component> { k, mill }); Check(menu.GetComponentsInChildren<Text>().Any(t => t.text.Contains("작업 상태")), "existing task screen shows diagnostics"); menu.Resume(); Time.timeScale = 0;
            var toast = SceneObjects.Any<ToastManager>();
            foreach (var field in new[] { "toasts", "crises", "pins", "locations", "dismissed" }) { var collection = typeof(ToastManager).GetField(field, Flags).GetValue(toast); collection.GetType().GetMethod("Clear").Invoke(collection, null); }
            ToastManager.Expand(false);
            for (var i = 0; i < 7; i++) ToastManager.SetCrisis("beds-test:" + i, "low crisis " + i);
            ToastManager.SetCrisis("med:operations", "urgent medical test");
            ToastManager.Show("hidden warning", ToastKind.Warning); ToastManager.Show("normal information");
            typeof(ToastManager).GetMethod("Rebuild", Flags).Invoke(toast, null);
            Check(ToastManager.VisibleLines[0] == "urgent medical test" && ToastManager.OverflowCount == 5 && ToastManager.VisibleLines.Count == 6, "urgent medical outranks old low crises with collapsed cap");
            var toasts = (System.Collections.IList)typeof(ToastManager).GetField("toasts", Flags).GetValue(toast);
            var warning = toasts.Cast<object>().Single(t => (string)t.GetType().GetField("message").GetValue(t) == "hidden warning");
            warning.GetType().GetField("remaining").SetValue(warning, -1f);
            typeof(ToastManager).GetMethod("Update", Flags).Invoke(toast, null);
            Check(ToastManager.Count == 2, "unseen warning does not expire behind five crises");
            var more = toast.GetComponentsInChildren<Button>().FirstOrDefault(b => b.GetComponentInChildren<Text>()?.text == "전체 알림 보기");
            Check(more != null, "overflow is actionable in existing toast panel"); more.onClick.Invoke(); typeof(ToastManager).GetMethod("Rebuild", Flags).Invoke(toast, null);
            Check(ToastManager.VisibleLines.Contains("hidden warning") && ToastManager.VisibleLines.Contains("normal information") && ToastManager.VisibleLines.Count == 10, "expanded scroll exposes all hidden alerts");
            typeof(ToastManager).GetMethod("Update", Flags).Invoke(toast, null); Check(ToastManager.Count == 1, "seen warning expires normally");
            ToastManager.Expand(false); ToastManager.Dismiss(0); Check(!ToastManager.VisibleLines.Contains("urgent medical test"), "ranked dismissal targets shown alert");
            foreach (var field in new[] { "toasts", "crises", "pins", "locations", "dismissed" }) { var collection = typeof(ToastManager).GetField(field, Flags).GetValue(toast); collection.GetType().GetMethod("Clear").Invoke(collection, null); }
            var result = "PASS " + count + " food availability/diet/regions/allocation/freshness, task explanations, critical alert ordering/overflow/unseen expiry and existing UI checks"; File.WriteAllText("Temp/operations-result.txt", result); return result;
        }
        catch (Exception e) { File.WriteAllText("Temp/operations-result.txt", e.ToString()); throw; }
        finally { ToastManager.Expand(false); GameMenuController.Instance.Resume(); Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
