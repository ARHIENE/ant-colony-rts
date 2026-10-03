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
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;


public static class HudV4Checks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); checks++; }
    static RectTransform Rect(string name) => Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include).First(r => r.name == name);
    static Button Button(string name) => Rect(name).GetComponent<Button>();
    static async Task Frames() { var frame = Time.frameCount; for (int i = 0; i < 200 && Time.frameCount < frame + 5; i++) await Task.Delay(20); Canvas.ForceUpdateCanvases(); }
    static async Task Ready() { for (int i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50); Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0; }
    static Rect Bounds(RectTransform r) { var c = new Vector3[4]; r.GetWorldCorners(c); return new Rect(c[0], c[2] - c[0]); }
    static void Inside(RectTransform child, RectTransform parent)
    {
        var c = Bounds(child); var p = Bounds(parent);
        Check(c.width > 0 && c.height > 0 && c.xMin >= p.xMin - 1 && c.xMax <= p.xMax + 1 && c.yMin >= p.yMin - 1 && c.yMax <= p.yMax + 1,
            child.name + " inside " + parent.name + " " + c + " / " + p);
    }
    static void Geometry()
    {
        var canvas = Rect("HUDCanvas");
        foreach (var name in new[] { "ResourceBar", "RosterBar", "HudClock", "ConsoleCenter", "CommandCard" }) Inside(Rect(name), canvas);
        var middle = Bounds(HudConsole.Center); var commands = Bounds(HudConsole.Right);
        Check(middle.xMax <= commands.xMin + .5f || middle.yMin >= commands.yMax - .5f, "info and commands separate: " + middle + " / " + commands);
        if (HudConsole.Left.gameObject.activeSelf) Inside(Rect("Minimap"), HudConsole.Left);
        foreach (var button in HudConsole.Right.GetComponentsInChildren<Button>()) Inside((RectTransform)button.transform, HudConsole.Right);
        var selected = Rect("SelectedUnitPanel");
        if (selected.gameObject.activeSelf)
            foreach (var label in selected.GetComponentsInChildren<Text>()) Inside(label.rectTransform, selected);
        Check(!Bounds(Rect("RosterBar")).Overlaps(Bounds(Rect("MenuToolbar"))), "roster avoids menu");
        Check(!Bounds(Rect("WorkforceSummary")).Overlaps(Bounds(Rect("DemandBar"))) && !Bounds(Rect("WorkforceSummary")).Overlaps(Bounds((RectTransform)Rect("DemandBar").parent)), "workforce below resources at " + Screen.width + "x" + Screen.height);
        Check(!Bounds(Rect("RosterBar")).Overlaps(Bounds(Rect("Food"))),"roster avoids resources at " + Screen.width + "x" + Screen.height + " roster " + Bounds(Rect("RosterBar")) + " food " + Bounds(Rect("Food")));
    }

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        string original = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "HudV4-" + Guid.NewGuid().ToString("N"));
        GameObject made = null;
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261003, mapSize = MapSize.Small }); await Ready();
            GameMenuController.Instance.Resume(); Time.timeScale = 0; await Frames();
            var selection = Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();
            selection.ClearSelection(); await Frames();
            Check(Rect("ColonyCommands").gameObject.activeSelf && Rect("ColonySummary").gameObject.activeSelf, "empty selection overview");
            Check(Rect("WorkforceSummary").GetComponent<Text>().text.Contains("대기 " + AntPool.Instance.Free), "live free workforce");
            Check(HudOverview.AvailableDraft == Mathf.Max(0, Mathf.Min(AntPool.Instance.Free, ColonyPopulation.Instance.MaxSoldiers(EnemyAlert.CrisisActive) - AntPool.Instance.Assigned)), "draft follows policy and free pool");
            var list = HudOverview.HomeCommanders; Check(list.Length >= 2, "multiple commanders");
            selection.SelectOnly(list[0].GetComponent<AntColony.Units.SelectableObject>()); await Frames();
            Check(Rect("SelectedUnitPanel").gameObject.activeSelf && Rect("CivilianCommands").gameObject.activeSelf, "single civilian state");
            Geometry();
            Button("Portrait").onClick.Invoke(); await Frames();
            Check(Object.FindAnyObjectByType<DetailTabs>().Visible, "portrait opens details");
            Button("Portrait").onClick.Invoke();
            typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(selection, new object[] { list[1].GetComponent<AntColony.Units.SelectableObject>() });
            await Frames();
            Check(Rect("MultiCommands").gameObject.activeSelf && !Rect("SelectedUnitPanel").gameObject.activeSelf, "multi state uses summary");
            Check(!Button("Multi Stop").interactable, "multi civilian rest needs individual selection");
            Check(Rect("ColonySummary").GetComponent<Text>().text.Contains("장수 2명 선택") && !Rect("ColonySummary").GetComponent<Text>().text.Contains("()"), "multi summary count and activity");
            Button("Multi Clear").onClick.Invoke(); await Frames(); Check(selection.GetSelectedObjects().Count == 0, "clear command");
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { BuildingKind.Dormitory, UnitRole.Worker });
            made = Object.Instantiate(template, list[0].Position + Vector3.right * 10, Quaternion.identity); made.SetActive(true);
            var dorm = made.GetComponent<Dormitory>(); WorkTargetPanel.Select(dorm); await Frames();
            Check(Rect("TargetCommands").gameObject.activeSelf && Rect("WorkTargetPanel").gameObject.activeSelf, "building state");
            Check(Rect("WorkTargetPanel").GetComponentsInChildren<Text>().Any(t => t.text.Contains("침대 4개")), "real dorm capacity");
            Button("Target Residents").onClick.Invoke(); await Frames(); Check(ToastManager.VisibleLines.Any(t => t.Contains("숙소 배정")), "assignment action");
            Button("Target Build").onClick.Invoke(); await Frames(); Check(BuildScreen.Picking, "dorm build opens builder picker");
            while (BuildScreen.Back()) { } await Frames();
            selection.SelectOnly(list[0].GetComponent<AntColony.Units.SelectableObject>());
            typeof(CommanderAnt).GetMethod("Mobilize", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(list[0], new object[] { 1, list[0].Position });
            await Frames(); Check(Rect("DeployedCommands").gameObject.activeSelf && !Rect("CivilianCommands").gameObject.activeSelf, "combat state");
            Check(Button("Attack Move").gameObject.activeInHierarchy && Button("Return To Post").gameObject.activeInHierarchy, "combat controls retained");
            Geometry();
            bool moved = false; ToastManager.SetCrisis("v4-test", "위치 검사", () => moved = true); await Frames();
            var location = Object.FindObjectsByType<Button>().First(b => b.name == "Location" && b.transform.parent.GetComponentsInChildren<Text>().Any(t => t.text == "위치 검사"));
            location.onClick.Invoke(); Check(moved, "notification location invokes target without dismissing");
            Check(ToastManager.VisibleLines.Contains("위치 검사"), "navigation preserves crisis");
            ToastManager.SetCrisis("v4-test", null);
            await Sizes();
            return "PASS " + checks + " HUD v4 checks";
        }
        finally { if (made != null) Object.Destroy(made); ToastManager.SetCrisis("v4-test", null); SaveStorage.RootOverride = original; Time.timeScale = 0; }
    }

    static async Task Sizes()
    {
        var assembly = typeof(Editor).Assembly;
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group = sizesType.GetMethod("GetGroup").Invoke(singleton, new[] { Enum.Parse(groupType, "Standalone") });
        var viewType = assembly.GetType("UnityEditor.GameView"); var view = EditorWindow.GetWindow(viewType);
        var indexProperty = viewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        int oldIndex = (int)indexProperty.GetValue(view);
        int builtins = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
        var labels = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
        for (int i = labels.Length - 1; i >= builtins; i--) if (labels[i].Contains("HUD v4 check")) group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { i });
        int customs = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var sizeKind = assembly.GetType("UnityEditor.GameViewSizeType");
        bool added = false;
        try
        {
            foreach (var resolution in new[] { new Vector2Int(1440, 900), new Vector2Int(1280, 1024), new Vector2Int(960, 900), new Vector2Int(720, 900), new Vector2Int(1920, 1080) })
            {
                var size = Activator.CreateInstance(sizeType, Enum.Parse(sizeKind, "FixedResolution"), resolution.x, resolution.y, "HUD v4 check");
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size }); added = true;
                indexProperty.SetValue(view, builtins + customs); view.Repaint(); await Frames();
                Object.FindAnyObjectByType<HudResponsiveLayout>().Apply(); await Frames(); Check(Screen.width == resolution.x && Screen.height == resolution.y, "resolution applied " + resolution + " actual " + Screen.width + "x" + Screen.height); Geometry();
                indexProperty.SetValue(view, oldIndex);
                group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { builtins + customs }); added = false;
            }
        }
        finally
        {
            indexProperty.SetValue(view, oldIndex);
            if (added) group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { builtins + customs });
            view.Repaint(); await Frames(); Object.FindAnyObjectByType<HudResponsiveLayout>().Apply();
        }
    }
}






