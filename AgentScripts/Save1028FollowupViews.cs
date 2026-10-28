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
public static class Save1028FollowupViews
{
    const string Dir = "Temp/save-20261028-followup";
    static async Task Wait(Func<bool> condition) { var end = DateTime.UtcNow.AddSeconds(100); while (!condition() && DateTime.UtcNow < end) await Task.Delay(80); if (!condition()) throw new Exception("SAVE view load timeout"); }
    static T Put<T>(B kind, Vector3 at) where T : BuildingBase
    {
        var prefab = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, AntColony.Data.UnitRole.Worker });
        var go = Object.Instantiate(prefab, at, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true); return go.GetComponent<T>();
    }
    static void View(Vector3 at, float size = 9)
    {
        var camera = SceneObjects.Any<AntColony.Camera.IsometricCameraController>(); camera.enabled = false;
        typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(camera, new object[] { at, 0f, size });
        UnityEditor.EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }
    static void Cargo(R type, Vector3 at)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.SetActive(false); go.name = "SAVE followup " + type; go.transform.position = at + Vector3.up * .25f; go.transform.localScale = Vector3.one * .5f; go.GetComponent<Collider>().isTrigger = true;
        go.AddComponent<ResourceNode>().ConfigureLoot(type, 4); go.SetActive(true);
    }
    public static async Task<string> Prepare()
    {
        Directory.CreateDirectory(Dir); File.WriteAllText(Dir + "/settings-before.json", JsonUtility.ToJson(UserSettings.Current)); File.WriteAllText(Dir + "/root-before.txt", SaveStorage.RootOverride ?? "");
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "SaveFollowup28-" + Guid.NewGuid().ToString("N"));
        var config = UserSettings.Current.Clone(); config.autoSaveEnabled = config.autoPauseThreat = config.autoPauseDowned = config.autoPauseRocket = false; config.edgeScrollThickness = 0; UserSettings.Apply(config, false);
        await Wait(() => !SaveSystem.Busy);
        var team = Enumerable.Range(0, 3).Select(i => { var candidate = StartingCandidate.Roll(i); candidate.traits.values.Clear(); return candidate; }).ToArray();
        SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Small, startingCount = 3, startingTeam = team }); await Wait(() => !SaveSystem.Busy && CommanderRoster.Instance.Commanders.Count == 3);
        AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 0; GameSession.Instance.MarkStarted(gameSeconds: 20);
        var crew = CommanderRoster.Instance.Commanders; foreach (var c in crew) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); }
        var home = crew[0].Position; var at = new Vector3(Mathf.Floor(home.x) + .5f, home.y, Mathf.Floor(home.z) + .5f);
        foreach (var n in ResourceNode.Available.Where(n => n.GetComponent<BuildingBase>() == null && (n.transform.position - at).sqrMagnitude < 300).ToArray()) n.gameObject.SetActive(false);
        foreach (var b in SceneObjects.All<BuildingBase>().Where(b => b.Data != null && b.Data.kind == B.CrashWreck)) b.gameObject.SetActive(false);
        var tech = CampaignResearch.Instance.CaptureState(); tech.completed.Add((int)ScienceTechnology.Fermentation); tech.completed.Add((int)ScienceTechnology.UndergroundSurvey); CampaignResearch.Instance.RestoreState(tech);
        var kinds = new[] { B.CharcoalKiln, B.CokeOven, B.Mill, B.OilPress, B.BrewingBarrel, B.Still };
        for (var i = 0; i < kinds.Length; i++) Put<Processor>(kinds[i], at + new Vector3((i % 3) * 4 - 4, 0, (i / 3) * 4 + 2));
        var chemical = Put<Processor>(B.ChemistryBench, at + new Vector3(8, 0, 2)); chemical.AddOrder(1, Workshop.OrderMode.KeepStock, 10);
        var farm = Put<BuildingBase>(B.Farm, at + new Vector3(8, 0, 6)); var plot = farm.GetComponent<FarmPlot>() ?? farm.gameObject.AddComponent<FarmPlot>(); plot.Configure(FarmCrop.Wheat, false); FarmSeeds.Add(FarmCrop.Wheat, 2);
        var barrel = SceneObjects.All<Processor>().First(p => p.Brewing); var state = new Processor.State { current = 0, fermenting = true, fermentation = FoodProcessingRecipes.FermentSeconds / 2 }; state.orders.Add(new Processor.Order { recipe = 4, mode = Workshop.OrderMode.KeepStock, amount = 10 });
        typeof(Processor).GetMethod("RestoreState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(barrel, new object[] { state });
        var types = new[] { R.Charcoal, R.Coke, R.Pottery, R.WheatFlour, R.SunflowerOil, R.Beer, R.RubberSap, R.Rubber };
        for (var i = 0; i < types.Length; i++) Cargo(types[i], at + new Vector3(-5 + i * 1.8f, 0, -1));
        for (var i = 0; i < crew.Count; i++) { crew[i].Agent.Warp(at + new Vector3(-3 + i * 2.5f, 0, -3)); crew[i].Agent.ResetPath(); }
        View(at + Vector3.right * 1 + Vector3.forward, 9); await Task.Delay(300); return "Prepared isolated followup SAVE review";
    }
    public static async Task<string> Show(string screen)
    {
        var menu = AntColony.UI.GameMenuController.Instance;
        if (screen == "brewing") menu.ShowProcessor(SceneObjects.All<Processor>().First(p => p.Brewing));
        else if (screen == "distilling") menu.ShowProcessor(SceneObjects.All<Processor>().First(p => p.Data.kind == B.Still));
        else if (screen == "chemical") menu.ShowProcessor(SceneObjects.All<Processor>().First(p => p.Chemical));
        else if (screen == "farm") menu.ShowFarm(FarmPlot.All.First(p => p.Crop == FarmCrop.Wheat));
        else if (screen == "settings" || screen == "controls")
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic; menu.GetType().GetField("settingsTab", flags).SetValue(menu, screen == "settings" ? 0 : 2); menu.GetType().GetMethod("Settings", flags).Invoke(menu, null);
        }
        else if (screen == "fishing") { menu.Resume(); var node = ResourceNode.Available.FirstOrDefault(n => n.RequiresFishing); if (node == null) return "No fishing target"; View(node.transform.position, 8); }
        else { menu.Resume(); var p = SceneObjects.All<Processor>().First(p => p.Data.kind == B.Mill); View(p.Position - Vector3.forward * 1.5f + Vector3.right, 9); }
        Time.timeScale = 0; Canvas.ForceUpdateCanvases(); UnityEditor.EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus(); await Task.Delay(250); return screen + " ready";
    }
    public static string Finish()
    {
        Time.timeScale = 0; var root = File.ReadAllText(Dir + "/root-before.txt"); SaveStorage.RootOverride = string.IsNullOrEmpty(root) ? null : root;
        UserSettings.Apply(JsonUtility.FromJson<UserSettingsData>(File.ReadAllText(Dir + "/settings-before.json")), false);
        SceneObjects.Any<AntColony.Camera.IsometricCameraController>().enabled = true; return "settings/root/camera restored";
    }
}
