using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using AntColony.Buildings;
using UnityEngine;

// Disposable fixture. Injected state is never counted as natural gameplay evidence.
public static class ReleaseDirect1114Checks
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static int count;
    static void Check(bool pass, string reason) { if (!pass) throw new Exception(reason); count++; }
    static ResourceNode Node(ResourceType type, Vector3 pos)
    {
        var go = new GameObject("FIXTURE direct " + type); go.transform.position = pos;
        go.AddComponent<SphereCollider>().radius = .6f;
        var n = go.AddComponent<ResourceNode>(); n.ConfigureNatural(type, 100, 0); return n;
    }
    public static async Task<string> Run()
    {
        if (!Application.isPlaying || Time.timeScale != 0 || !(SaveStorage.RootOverride ?? "").Contains("ReleaseFlow1114"))
            throw new Exception("Paused isolated fixture required");
        count = 0;
        GameSession.Instance.MarkStarted(10, 30);
        var c = CommanderRoster.Instance.Commanders.First(x => !x.IsSleepTime && x.CanDoJob(CommanderJobs.Gathering) && x.CanDoJob(CommanderJobs.Building));
        c.CommandStop(); c.PersonalState.sleep = new CommanderSleepState();
        c.PersonalState.meal = new CommanderMealState(); c.WorkState.health = 100;
        c.SetJobPriority(CommanderJobs.Gathering, 5); c.SetJobPriority(CommanderJobs.Building, 5);
        var a = Node(ResourceType.Food, c.Position + Vector3.left * 4);
        var b = Node(ResourceType.Wood, c.Position + Vector3.forward * 4);
        var stone = Node(ResourceType.Stone, c.Position + Vector3.right * 4);
        Physics.SyncTransforms(); await Task.Delay(100);
        Check(c.TryDirectGather(a, out var why), "Direct gathering starts: " + why);
        Check(a.WorkRequested && !b.WorkRequested, "Only clicked natural resource becomes designated");
        Check(c.TryDirectGather(a, out why) && c.CurrentResourceNode == a, "Repeated target remains stable");
        c.SetJobPriority(CommanderJobs.Gathering, 0);
        typeof(WorkerAnt).GetMethod("RestoreCargo", Hidden).Invoke(c, new object[] { 8f, ResourceType.Food, null });
        Check(!c.TryDirectGather(b, out why) && why.Contains("금지"), "Forbidden work reports reason");
        Check(c.CurrentResourceNode == a && c.CarriedAmount == 8 && !b.WorkRequested, "Rejected command preserves previous target/cargo/designations");
        c.SetJobPriority(CommanderJobs.Gathering, 5); c.PersonalState.toolTier = 0;
        Check(!c.TryDirectGather(stone, out why) && why.Contains("도구"), "Mining tool is checked before designation");
        Check(!stone.WorkRequested && c.CarriedAmount == 8, "Missing tool does not alter world or cargo");
        var unreachable = Node(ResourceType.Food, new Vector3(3000, 0, 3000));
        Check(!c.TryDirectGather(unreachable, out why), "Unreachable direct target rejected");
        Check(c.CurrentResourceNode == a && !unreachable.WorkRequested && c.CarriedAmount == 8, "Unreachable target preserves order and cargo");
        var pilesBefore = ResourceNode.Available.Where(n => n.IsLooseCargo).Sum(n => n.AmountRemaining);
        Check(c.TryDirectGather(b, out why), "Valid replacement accepted: " + why);
        Check(c.CurrentResourceNode == b && c.CarriedAmount == 0, "Replacement switches active work");
        Check(Math.Abs(ResourceNode.Available.Where(n => n.IsLooseCargo).Sum(n => n.AmountRemaining) - pilesBefore - 8) < .01f,
            "Interrupted cargo is dropped exactly once");
        var site = SceneObjects.All<BuildingConstructionSite>().First();
        site.SetPaused(true);
        Check(!c.TryDirectBuild(site, out why) && c.CurrentResourceNode == b, "Paused blueprint preserves current gathering");
        site.SetPaused(false);
        c.SetJobPriority(CommanderJobs.Building, 0);
        Check(!c.TryDirectBuild(site, out why) && c.CurrentResourceNode == b, "Direct construction respects job prohibition");
        c.SetJobPriority(CommanderJobs.Building, 5);
        foreach (var n in site.Needs) n.delivered = n.need;
        Check(c.TryDirectBuild(site, out why) && c.ConstructionTarget == site, "Gathering can switch directly to ready construction: " + why);
        Check(c.TryDirectGather(a, out why) && c.ConstructionTarget == null, "Construction can safely switch to gathering: " + why);
        Check(site.Ready, "Switch preserves delivered construction materials");
        SceneObjects.Any<SelectionManager>().SelectOnly(c.GetComponent<SelectableObject>());
        var camera = UnityEngine.Camera.main;
        var screen = camera.WorldToScreenPoint(b.transform.position);
        typeof(UnitSelectionController).GetMethod("IssueCommand", Hidden).Invoke(SceneObjects.Any<UnitSelectionController>(), new object[] { (Vector2)screen });
        Check(c.CurrentResourceNode == b, "Actual right-click raycast route reaches direct command");
        await Task.Delay(1000);
        ScreenCapture.CaptureScreenshot("F:/UnityProjects/ant-colony-rts/work/release-flow-1114/screens/direct-command-fixture.png");
        var result = "PASS " + count + " direct-order assertions; disposable injected fixture, not natural play.";
        File.WriteAllText("work/release-flow-1114/direct-regression.txt", result);
        return result;
    }
}
