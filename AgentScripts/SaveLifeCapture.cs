using System;
using System.IO;
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
using UnityEngine.AI;
using Object = UnityEngine.Object;

// SAVE 전용: 폐기할 Play 세션에서 실제 식사·오락 상태와 HUD를 캡처한다.
public static class SaveLifeCapture
{
    static T Build<T>(BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = Object.Instantiate(template, position, Quaternion.identity); go.SetActive(true); return go.GetComponent<T>();
    }
    public static async Task<string> Prepare()
    {
        var settings = UserSettings.Current.Clone(); settings.autoSaveEnabled = false; UserSettings.Apply(settings, false);
        SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 260930, biome = MapBiome.Garden });
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(50);
        if (SaveSystem.Busy) throw new Exception("Scene not ready");
        GameMenuController.Instance.Resume(); Time.timeScale = 0;
        GameSession.Instance.MarkStarted(0, 20);
        foreach (var c in CommanderRoster.Instance.Commanders) { c.CommandStop(); c.WorkState.jobs = CommanderJobs.None; c.ApplyTraits(new CommanderTraits(CommanderPersonality.Balanced, 50)); }
        var eater = CommanderRoster.Instance.Commanders[0]; var player = CommanderRoster.Instance.Commanders[1];
        var home = eater.Position;
        var kitchen = Build<Kitchen>(BuildingKind.Kitchen, home + Vector3.right * 4);
        kitchen.Meals.meals.Add(new Kitchen.Meal { quality = 3, skill = 20 });
        eater.PersonalState.meal.satiety = 20; eater.TickDuty(1);
        var campfire = Build<RecreationSpot>(BuildingKind.Campfire, home + Vector3.left * 4);
        if (!NavMesh.SamplePosition(campfire.Position, out var hit, 10, NavMesh.AllAreas) || !player.Agent.Warp(hit.position)) throw new Exception("No recreation position");
        player.PersonalState.joy.joy = 20; player.TickDuty(1);
        if (!eater.IsEating || !player.IsPlaying) throw new Exception("Meal/recreation not active");
        var selection = Object.FindAnyObjectByType<AntColony.Units.SelectionManager>(); selection.ClearSelection();
        typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(selection, new object[] { eater.GetComponent<SelectableObject>() });
        selection.enabled = false; // 캡처 준비 중 들어오는 마우스 입력으로 선택이 풀리지 않게 한다.
        Object.FindAnyObjectByType<DetailTabs>().Tab = 0;
        Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>().FocusOn(home);
        Camera.main.orthographicSize = 17;
        return "Garden: meal and campfire active; HUD selected eater.";
    }
    public static string Night()
    {
        GameSession.Instance.MarkStarted(0, GameCalendar.DaySeconds + 10);
        ColonyEvents.Instance.Tick(1);
        var offset = 0;
        foreach (var actor in Object.FindObjectsByType<EventActor>())
            if (actor.Kind == EventActorKind.NightPredator) actor.transform.position = WorldMapManager.Instance.HomePosition + Vector3.forward * 10 + Vector3.right * (offset++ * 3);
        return "Night predators visible near home; paused for capture.";
    }
    public static async Task<string> Capture(string name)
    {
        var path = Path.GetFullPath(Path.Combine(".unity", "save-2026-09-30", name + ".png"));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Canvas.ForceUpdateCanvases();
        var view = UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
        view.Repaint(); await Task.Delay(300);
        ScreenCapture.CaptureScreenshot(path);
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (!File.Exists(path) && DateTime.UtcNow < deadline) { view.Repaint(); await Task.Delay(100); }
        if (!File.Exists(path)) throw new Exception("Screenshot not created");
        return path;
    }
}
