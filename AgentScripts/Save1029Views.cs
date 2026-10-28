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
using AntColony.UI;
using UnityEngine;
using R = AntColony.Data.ResourceType;
using B = AntColony.Data.BuildingKind;
public static class Save1029Views
{
    const string Dir = "Temp/save-20261029";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Put<T>(B kind, Vector3 at) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, AntColony.Data.UnitRole.Worker });
        var go = UnityEngine.Object.Instantiate(template, at, Quaternion.identity); go.name = "SAVE29 " + kind; go.SetActive(true); return go.GetComponent<T>();
    }
    public static async Task<string> Prepare()
    {
        Directory.CreateDirectory(Dir);
        if (!File.Exists(Dir + "/settings-before.json")) File.WriteAllText(Dir + "/settings-before.json", JsonUtility.ToJson(UserSettings.Current));
        if (!File.Exists(Dir + "/root-before.txt")) File.WriteAllText(Dir + "/root-before.txt", SaveStorage.RootOverride ?? "");
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Save29-" + Guid.NewGuid().ToString("N"));
        var config = UserSettings.Current.Clone(); config.autoSaveEnabled = config.autoPauseThreat = config.autoPauseDowned = config.autoPauseRocket = false; config.edgeScrollThickness = 0; UserSettings.Apply(config, false);
        SaveSystem.NewGame(new NewGameOptions { seed = 291029, mapSize = MapSize.Small });
        var end = DateTime.UtcNow.AddSeconds(100); while ((SaveSystem.Busy || CommanderRoster.Instance == null || CommanderRoster.Instance.Commanders.Count < 3) && DateTime.UtcNow < end) await Task.Delay(80);
        if (SaveSystem.Busy || CommanderRoster.Instance == null || CommanderRoster.Instance.Commanders.Count < 3) throw new Exception("SAVE fixture load timeout");
        GameMenuController.Instance.Resume(); Time.timeScale = 0; GameSession.Instance.MarkStarted(gameSeconds: 20);
        var crew = CommanderRoster.Instance.Commanders; foreach (var c in crew) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); }
        var home = crew[0].Position;
        var store = Put<Storage>(B.Storage, home + Vector3.left * 6);
        var k = Put<Kitchen>(B.Hearth, home + Vector3.forward * 6); k.Orders.Clear();
        for (int i = 2; i < 5; i++) k.AddOrder(i, Workshop.OrderMode.KeepStock, 10);
        var rm = ResourceManager.Instance;
        rm.AddTo(store.Bin, R.Rice, 40); rm.AddTo(store.Bin, R.WheatFlour, 20); rm.AddTo(store.Bin, R.SunflowerOil, 10);
        rm.AddTo(store.Bin, R.CookedRice, 6); rm.AddTo(store.Bin, R.Flatbread, 6); rm.AddTo(store.Bin, R.FriedRice, 4);
        Put<ColdStorage>(B.Freezer, home + Vector3.right * 5);
        var camera = SceneObjects.Any<AntColony.Camera.IsometricCameraController>(); camera.enabled = false;
        typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView", Flags).Invoke(camera, new object[] { home, 0f, 10f });
        var c0 = crew[0]; c0.ApplyTraits(new CommanderTraits { values = new System.Collections.Generic.List<CommanderTrait> { CommanderTrait.Brave } });
        SpecialtyAcquisition.Initialize(c0); c0.Talents.levels[(int)CommanderActivity.Gathering] = 10;
        c0.Traits.TryAdd(CommanderTrait.GoldGathering); c0.Traits.specialties.results[0] = 4;
        await Show("kitchen"); return "Isolated SAVE29 review fixture ready; simulated specialty state";
    }
    public static async Task<string> Show(string screen)
    {
        var menu = GameMenuController.Instance;
        if (screen == "kitchen") menu.ShowKitchen(SceneObjects.All<Kitchen>().First(k => k.name.StartsWith("SAVE29")));
        else if (screen == "diet") menu.ShowDiet(CommanderRoster.Instance.Commanders[0]);
        else if (screen == "storage") menu.ShowStorage(SceneObjects.All<Storage>().First(s => s.name == "SAVE29 Storage").Bin);
        else if (screen == "research")
        {
            typeof(GameMenuController).GetField("researchPick", Flags).SetValue(menu, (ScienceTechnology?)ScienceTechnology.Engine);
            typeof(GameMenuController).GetField("researchEra", Flags).SetValue(menu, 6); menu.Science();
        }
        else if (screen == "specialty")
        {
            menu.Details(CommanderRoster.Instance.Commanders[0]); Canvas.ForceUpdateCanvases();
            menu.Tooltip(SpecialtyAcquisition.Description(CommanderRoster.Instance.Commanders[0], CommanderActivity.Gathering));
        }
        else if (screen == "population") menu.Population();
        else if (screen == "rocket")
        {
            if (!SceneObjects.All<AirshipYard>().Any()) Put<AirshipYard>(B.AirshipYard, CommanderRoster.Instance.Commanders[0].Position + Vector3.back * 10);
            menu.ScienceFacilities(); Canvas.ForceUpdateCanvases();
            var plan = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().First(t => t.text.StartsWith("가동 후15일"));
            var scroll = plan.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, plan.rectTransform);
            scroll.content.anchoredPosition += new Vector2(0, scroll.viewport.rect.yMax - bounds.max.y - 30);
        }
        else if (screen == "tasks") menu.ShowTasks(GameMenuController.TaskCandidates().ToList());
        else if (screen == "power")
        {
            menu.Resume(); var cold = SceneObjects.All<ColdStorage>().First(s => s.name.StartsWith("SAVE29")); WorkTargetPanel.Select(cold);
            var layout = UnityEngine.Object.FindObjectsByType<HudResponsiveLayout>().First();
            typeof(HudResponsiveLayout).GetMethod("LateUpdate", Flags).Invoke(layout, null);
            UnityEngine.Object.FindObjectsByType<WorkTargetPanel>().First().GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "V6 Details").onClick.Invoke();
            typeof(HudResponsiveLayout).GetMethod("LateUpdate", Flags).Invoke(layout, null);
        }
        Time.timeScale = 0; Canvas.ForceUpdateCanvases();
        UnityEditor.EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        await Task.Delay(400); return screen + " ready";
    }
    public static string Finish()
    {
        GameMenuController.Instance.Resume(); WorkTargetPanel.Clear(); Time.timeScale = 0;
        SaveStorage.RootOverride = File.ReadAllText(Dir + "/root-before.txt"); if (string.IsNullOrEmpty(SaveStorage.RootOverride)) SaveStorage.RootOverride = null;
        UserSettings.Apply(JsonUtility.FromJson<UserSettingsData>(File.ReadAllText(Dir + "/settings-before.json")), false);
        SceneObjects.Any<AntColony.Camera.IsometricCameraController>().enabled = true;
        return "SAVE29 settings/root/camera restored";
    }
}
