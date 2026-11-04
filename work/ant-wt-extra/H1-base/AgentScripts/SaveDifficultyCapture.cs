using System;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// SAVE 전용 실행 상태 준비. 각 Prepare는 새 Play 세션에서 실행하고 종료로 복구한다.
public static class SaveDifficultyCapture
{
    public static async Task<string> Capture(string name)
    {
        var view = UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
        view.Show();
        view.Focus();
        view.Repaint();
        await Task.Delay(300);
        var path = System.IO.Path.GetFullPath(".unity/" + name + ".png");
        ScreenCapture.CaptureScreenshot(path);
        for (var i = 0; i < 100 && !System.IO.File.Exists(path); i++)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            view.Repaint();
            await Task.Delay(100);
        }
        if (!System.IO.File.Exists(path)) throw new Exception("Screenshot was not written");
        return path;
    }

    public static Task<string> Boss() => Prepare(ExpeditionSiteKind.BossNest);
    public static Task<string> Resources() => Prepare(ExpeditionSiteKind.ResourceSite);

    public static async Task<string> Settlement()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
        var world = WorldMapManager.Instance;
        var site = world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement && s.Difficulty == 3);
        var commander = CommanderRoster.Instance.Commanders.First(c => c.HasTroops);
        var ship = world.CreateTransport(false, commander.Position + Vector3.right * 3);
        if (!ship.TryBoard(new[] { commander }) || !ship.TryDepart(site)) throw new Exception("Capture expedition refused");
        ship.Tick(ship.TravelSeconds);
        foreach (var building in site.Colony.GetComponentsInChildren<AntColony.Buildings.BuildingBase>())
            building.TakeDamage(float.MaxValue);
        foreach (var guard in site.GetComponentsInChildren<WildMonster>()) guard.TakeDamage(float.MaxValue);
        for (var i = 0; i < 100 && !site.Cleared; i++) await Task.Delay(50);
        if (!site.TryResolveConquest(ConquestDisposition.Annexed)) throw new Exception("Cannot annex capture site");
        site.Settlement.Tick(6000);
        world.ViewSite(site);
        var panel = Object.FindAnyObjectByType<AntColony.UI.WorldMapPanel>();
        typeof(AntColony.UI.WorldMapPanel).GetField("selectedSite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(panel, site);
        typeof(AntColony.UI.WorldMapPanel).GetField("selectedShip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(panel, ship);
        if (!panel.IsOpen) panel.Toggle();
        Time.timeScale = 0;
        var view = UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
        view.Show(); view.Focus(); view.Repaint();
        await Task.Delay(300);
        return site.Title + " / difficulty 3 / local stock "
            + site.Colony.GetStock(AntColony.Data.ResourceType.Food) + "F / "
            + site.Colony.GetStock(AntColony.Data.ResourceType.Soil) + "S";
    }

    private static async Task<string> Prepare(ExpeditionSiteKind kind)
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
        var world = WorldMapManager.Instance;
        var site = world.Sites.First(s => s.Kind == kind && s.Difficulty == 3);
        var commander = CommanderRoster.Instance.Commanders.First(c => c.HasTroops);
        var ship = world.CreateTransport(false, commander.Position + Vector3.right * 3);
        if (!ship.TryBoard(new[] { commander }) || !ship.TryDepart(site))
            throw new Exception("Capture expedition refused");
        ship.Tick(ship.TravelSeconds);
        if (!world.ViewSite(site)) throw new Exception("Cannot view capture site");
        await Task.Delay(200);
        var panel = Object.FindAnyObjectByType<AntColony.UI.WorldMapPanel>();
        if (panel.IsOpen) panel.Toggle();
        Focus(site.Boss != null ? site.Boss.Position : site.transform.position);
        Time.timeScale = 0;
        await Task.Delay(200);
        return site.Title + " / difficulty " + site.Difficulty
            + (site.Boss != null ? " / HP " + site.Boss.CurrentHp + "/" + site.Boss.MaxHp
                : " / " + string.Join(", ", site.GetComponentsInChildren<ResourceNode>()
                    .Select(n => n.ResourceType + " " + n.AmountRemaining)));
    }

    public static async Task<string> Loot()
    {
        var site = WorldMapManager.Instance.ViewedSite;
        if (site == null || site.Boss == null) throw new Exception("Prepare Boss first");
        site.Boss.TakeDamage(site.Boss.MaxHp);
        await Task.Delay(100);
        var nodes = Object.FindObjectsByType<ResourceNode>().Where(n => n.name.StartsWith("BossLoot ")).ToArray();
        if (nodes.Length != 2) throw new Exception("Expected two loot nodes");
        Focus((nodes[0].transform.position + nodes[1].transform.position) * .5f);
        await Task.Delay(200);
        return string.Join(", ", nodes.Select(n => n.ResourceType + " " + n.AmountRemaining));
    }

    private static void Focus(Vector3 point)
    {
        var camera = Camera.main;
        var controller = camera.GetComponent<AntColony.Camera.IsometricCameraController>();
        controller.SetRegion(point, new Bounds(point, Vector3.one * 70));
        controller.enabled = false;
        camera.orthographicSize = 12;
    }
}
