using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;
using R = AntColony.Data.ResourceType;
public static class StorageFilters1029Checks
{
    static int count;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("StorageFilters " + count + ": " + label); count++; }
    static async Task Ready() { var end = DateTime.UtcNow.AddSeconds(100); while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(80); Check(!SaveSystem.Busy, "ready"); }
    static Storage Put(BuildingKind kind, Vector3 at)
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = UnityEngine.Object.Instantiate(template, at, Quaternion.identity); go.SetActive(true); return go.GetComponent<Storage>();
    }
    static Button Button(string label) => UnityEngine.Object.FindObjectsByType<Button>().First(b => b.GetComponentInChildren<Text>()?.text == label);
    public static async Task<string> Main()
    {
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "StorageFilters-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 291029, mapSize = MapSize.Small }); await Ready();
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var menu = GameMenuController.Instance; var rm = ResourceManager.Instance;
            typeof(GameMenuController).GetField("storageAllowedClipboard", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(menu, null);
            var source = Put(BuildingKind.Storage, new Vector3(300, 0, 300));
            var target = Put(BuildingKind.Storage, new Vector3(310, 0, 300));
            menu.ShowStorage(target.Bin); Check(!Button("허용 품목 붙여넣기").interactable, "no clipboard paste disabled");
            rm.SetAllowed(source.Bin, R.Rice, false); rm.SetAllowed(source.Bin, R.Food, false); rm.SetPriority(source.Bin, 9); rm.SetPriority(target.Bin, 2);
            Check(rm.AddTo(target.Bin, R.Rice, 7) == 7, "stock source for ejection");
            var lots = new FoodLots(); lots.Add(5, 70); Check(rm.AddTo(target.Bin, R.Food, 5, lots: lots) == 5, "old food stored");
            menu.ShowStorage(source.Bin); Button("허용 품목 복사").onClick.Invoke();
            var copied = Enum.GetValues(typeof(R)).Cast<R>().Select(source.Bin.Allows).ToArray();
            rm.SetAllowed(source.Bin, R.Rice, true);
            menu.ShowStorage(target.Bin); Check(Button("허용 품목 붙여넣기").interactable, "same class paste enabled"); Button("허용 품목 붙여넣기").onClick.Invoke();
            Check(Enum.GetValues(typeof(R)).Cast<R>().All(t => target.Bin.Allows(t) == copied[(int)t]), "all 78 filters including IDs above 63 copied snapshot");
            Check(source.Bin.Allows(R.Rice) && !target.Bin.Allows(R.Rice), "clipboard independent from later source edits");
            Check(target.Bin.priority == 2 && source.Bin.priority == 9, "priority not copied");
            Check(target.Bin.Amount(R.Rice) == 0 && target.Bin.Amount(R.Food) == 0, "forbidden inventory ejected");
            var floor = ResourceNode.Available.Where(n => n.IsLooseCargo && Vector3.Distance(n.transform.position, target.Position) < 8).ToArray();
            Check(floor.Where(n => n.ResourceType == R.Rice).Sum(n => n.AmountRemaining) == 7, "ejected rice conserved");
            Check(floor.Where(n => n.ResourceType == R.Food).Sum(n => n.AmountRemaining) == 5, "ejected food conserved");
            Check(floor.Where(n => n.ResourceType == R.Food).All(n => Mathf.Abs(n.GetComponent<FloorFood>().Sync().Stalest - 70) < .01f), "ejection does not refresh old food");
            Check(source.Bin.Amount(R.Rice) == 0, "paste never transfers stock into other bin");
            var fridge = Put(BuildingKind.Fridge, new Vector3(320, 0, 300)); var freezer = Put(BuildingKind.Freezer, new Vector3(330, 0, 300));
            menu.ShowStorage(fridge.Bin); Check(!Button("허용 품목 붙여넣기").interactable, "general filters cannot override cold storage restrictions");
            rm.SetAllowed(fridge.Bin, R.CookedRice, false); menu.ShowStorage(fridge.Bin); Button("허용 품목 복사").onClick.Invoke();
            menu.ShowStorage(freezer.Bin); Check(Button("허용 품목 붙여넣기").interactable, "fridge to freezer supported"); Button("허용 품목 붙여넣기").onClick.Invoke();
            Check(!freezer.Bin.Allows(R.CookedRice) && !freezer.Bin.Allows(R.Soil), "cold food filters preserved");
            menu.ShowStorage(target.Bin); Check(!Button("허용 품목 붙여넣기").interactable, "cold filters cannot overwrite general storage");
            Check(SaveSystem.TrySave(false, 0, out var error) && SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "actual save/load " + error); await Ready();
            target = SceneObjects.All<Storage>().First(s => Vector3.Distance(s.Position, new Vector3(310, 0, 300)) < 1);
            Check(!target.Bin.Allows(R.Rice) && !target.Bin.Allows(R.Food) && target.Bin.priority == 2, "applied filters and priority persist");
            return "PASS " + count + " storage filter checks";
        }
        finally { GameMenuController.Instance?.Resume(); Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
