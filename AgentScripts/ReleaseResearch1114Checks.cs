using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using AntColony.Buildings;
using UnityEngine;

public static class ReleaseResearch1114Checks
{
    public static string Run()
    {
        if (!Application.isPlaying || Time.timeScale != 0 || !(SaveStorage.RootOverride ?? "").Contains("ReleaseFlow1114"))
            throw new Exception("Paused isolated checkpoint fixture required");
        int count = 0;
        void Check(bool pass, string reason) { if (!pass) throw new Exception(reason); count++; }
        GameSession.Instance.MarkStarted(10, 30);
        var c = CommanderRoster.Instance.Commanders.First();
        c.CommandStop(); c.PersonalState.sleep = new CommanderSleepState(); c.PersonalState.meal = new CommanderMealState();
        c.SetJobPriority(CommanderJobs.Gathering, 5); c.SetJobPriority(CommanderJobs.Research, 5);
        var lab = SceneObjects.All<ScienceLab>().First(l => l.isActiveAndEnabled);
        lab.ReleaseResearcher();
        var go = new GameObject("FIXTURE research interrupted food"); go.transform.position = c.Position + Vector3.left * 3;
        go.AddComponent<SphereCollider>().radius = .6f;
        var node = go.AddComponent<ResourceNode>(); node.ConfigureNatural(ResourceType.Food, 100, 0);
        Check(c.TryDirectGather(node, out var reason), "Start gather: " + reason);
        typeof(WorkerAnt).GetMethod("RestoreCargo", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, new object[] { 8f, ResourceType.Food, null });
        var position = lab.transform.position;
        try {
            lab.transform.position = new Vector3(3000, 0, 3000);
            Check(!c.SendToResearch(lab), "Unreachable lab rejected");
            Check(c.CurrentResourceNode == node && c.CarriedAmount == 8, "Unreachable assignment preserves gather and cargo");
        } finally { lab.transform.position = position; }
        c.SetJobPriority(CommanderJobs.Research, 0);
        Check(!c.SendToResearch(lab) && c.CarriedAmount == 8 && c.CurrentResourceNode == node, "Forbidden research preserves current task");
        c.SetJobPriority(CommanderJobs.Research, 5);
        var before = ResourceNode.Available.Where(n => n.IsLooseCargo).Sum(n => n.AmountRemaining);
        Check(c.SendToResearch(lab), "Research directly interrupts ordinary gathering/carrying");
        Check(c.CarriedAmount == 0 && c.CurrentResourceNode == null, "Old task released");
        Check(Math.Abs(ResourceNode.Available.Where(n => n.IsLooseCargo).Sum(n => n.AmountRemaining) - before - 8) < .01f,
            "Research transition preserves exactly eight cargo on floor");
        var result = "PASS " + count + " research assignment assertions; injected disposable checkpoint, not natural play.";
        File.WriteAllText("work/release-flow-1114/research-regression.txt", result); return result;
    }
}
