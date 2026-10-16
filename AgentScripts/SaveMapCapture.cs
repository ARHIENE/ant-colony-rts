using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// SAVE 전용: 실제 게임 화면을 준비한다. 캡처는 공식 CLI screenshot 명령으로 수행한다.
public static class SaveMapCapture
{
    static async Task<string> Shot(string name)
    {
        Canvas.ForceUpdateCanvases();
        await Task.Delay(300);
        var path = Path.GetFullPath(".unity/save-2026-10-03/" + name + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        ScreenCapture.CaptureScreenshot(path);
        await Task.Delay(1000);
        if (!File.Exists(path)) throw new Exception("Capture not written: " + path);
        return path;
    }
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        if (SaveSystem.Busy) throw new Exception("Scene load timeout");
        GameMenuController.Instance.Resume(); Time.timeScale = 0;
    }
    static async Task<string> Prepare(MapBiome biome)
    {
        await Ready();
        SaveSystem.NewGame(new NewGameOptions { seed = 261003, mapSize = MapSize.Small, biome = biome });
        await Ready();
        var camera = Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();
        var focus = WorldMapManager.Instance.HomePosition;
        if (biome == MapBiome.Waterside)
            focus = Object.FindObjectsByType<ResourceNode>().First(n => n.RequiresFishing && n.GetComponentInParent<ExpeditionSite>() == null).transform.position;
        camera.FocusOn(focus); camera.GetComponent<Camera>().orthographicSize = 27; camera.enabled = false;
        await Task.Delay(400);
        if (biome == MapBiome.Waterside)
        {
            WeatherSystem.Instance.Set(WeatherKind.Rain);
            await Task.Yield();
            foreach (var particles in Object.FindObjectsByType<ParticleSystem>()) particles.Simulate(1.5f, true, true);
        }
        Canvas.ForceUpdateCanvases();
        return biome + " ready; time paused; capture game view with HUD";
    }
    public static async Task<string> Desert() { await Prepare(MapBiome.Desert); return await Shot("desert-hud"); }
    public static async Task<string> City() { await Prepare(MapBiome.City); return await Shot("city-hud"); }
    public static async Task<string> Cave() { await Prepare(MapBiome.Cave); return await Shot("cave-hud"); }
    public static async Task<string> Waterside() { await Prepare(MapBiome.Waterside); return await Shot("waterside-rain-hud"); }
    static GameObject Put(BuildingKind kind, Vector3 position)
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = Object.Instantiate(template, position, Quaternion.identity); go.SetActive(true); return go;
    }
    public static async Task<string> Scout()
    {
        await Prepare(MapBiome.Garden);
        var point = WorldMapManager.Instance.HomePosition + Vector3.right * 8;
        if (NavMesh.SamplePosition(point, out var hit, 8, NavMesh.AllAreas)) point = hit.position;
        var post = Put(BuildingKind.ScoutPost, point).GetComponent<ScoutPost>();
        WorkTargetPanel.Select(post.GetComponent<BuildingBase>());
        GameMenuController.Instance.ShowScout(post);
        await Task.Delay(300); Canvas.ForceUpdateCanvases();
        return await Shot("scout-dispatch");
    }
    public static async Task<string> Walls()
    {
        await Prepare(MapBiome.Garden);
        foreach (var commander in CommanderRoster.Instance.Commanders)
        { commander.SetJobEnabled(CommanderJobs.All, false); commander.CommandStop(); }
        var point = WorldMapManager.Instance.HomePosition + new Vector3(14, 0, -14);
        if (!NavMesh.SamplePosition(point, out var hit, 10, NavMesh.AllAreas)) throw new Exception("No wall staging ground");
        point = hit.position;
        var placement = Object.FindAnyObjectByType<BuildingPlacementController>();
        ResourceManager.Instance.Add(AntColony.Data.ResourceType.Soil, 1000);
        var builder = CommanderRoster.Instance.Commanders.First(c => c.CanStartConstruction);
        if (!placement.BeginPlacement(BuildingKind.LeafWall, UnitRole.Worker, builder)) throw new Exception("Cannot begin wall placement");
        var cells = placement.LineCells(point, point + Vector3.right * 9);
        var count = placement.PlaceLine(cells, cells.ConvertAll(_ => true));
        for (var i = 0; i < 10; i++)
        {
            var wall = Put(BuildingKind.LeafWall, point + new Vector3(i, .75f, 5)).GetComponent<Wall>();
            if (i >= 4 && i <= 6) WallFire.Ignite(wall);
        }
        var camera = Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();
        camera.FocusOn(point + new Vector3(4, 0, 2)); camera.GetComponent<Camera>().orthographicSize = 12;
        if (count != 10) throw new Exception("Expected ten wall sites, got " + count);
        return await Shot("wall-line-fire");
    }
    public static async Task<string> Hud()
    {
        await Prepare(MapBiome.Garden);
        var commanders = CommanderRoster.Instance.Commanders;
        foreach (var commander in commanders) { commander.SetJobEnabled(CommanderJobs.All, false); commander.CommandStop(); }
        var selection = Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();
        typeof(AntColony.Units.SelectionManager).GetMethod("SelectOnly", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
            .Invoke(selection, new object[] { commanders[0].GetComponent<SelectableObject>() });
        typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(selection, new object[] { commanders[1].GetComponent<SelectableObject>() });
        return await Shot("hud-v4-fixed-multi");
    }
}
