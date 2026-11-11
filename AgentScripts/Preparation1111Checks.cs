using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using AntColony.Buildings;
using AntColony.Units;
using UnityEngine;

public static class Preparation1111Checks
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static string log;
    static void Check(bool ok, string name)
    {
        log += (ok ? "PASS " : "FAIL ") + name + "\n";
        if (!ok) throw new Exception(log);
    }
    public static async Task<string> Main(string label)
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        log = "";
        var settings = UserSettings.Current.Clone();
        var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "PreparationChecks-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone();
            quiet.autoSaveEnabled = quiet.autoPauseDowned = quiet.autoPauseThreat = false;
            UserSettings.Apply(quiet, false);
            UnityEngine.Random.InitState(261106);
            StartingCandidate candidate;
            do { candidate = StartingCandidate.Roll(0); } while (StartingCandidate.Missing(new[] { candidate }).Length > 0);
            SaveSystem.NewGame(new NewGameOptions { seed = 261106, mapSize = MapSize.Small, biome = MapBiome.Forest, startingCount = 1, startingTeam = new[] { candidate } });
            var end = DateTime.UtcNow.AddSeconds(60);
            while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(100);
            if (SaveSystem.Busy) throw new Exception("New game timeout");
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var c = CommanderRoster.Instance.Commanders.First(x => !x.IsDead);
            c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false);
            c.SetJobPriority(CommanderJobs.Gathering, 5); c.SetJobPriority(CommanderJobs.Building, 8);
            var node = ResourceNode.Available.Where(n => n.NeedsDesignation && n.ResourceType == ResourceType.Soil && !n.IsDepleted)
                .OrderBy(n => (n.transform.position - c.Position).sqrMagnitude).First(n => { n.RequestWork(); return n.CanGatherWith(c) && c.TryGatherApproach(n, out _); });
            var origin = node.transform.position;
            try
            {
                node.transform.position = origin + Vector3.up * 3;
                Physics.SyncTransforms();
                Check(c.TryGatherApproach(node, out var groundPoint) && groundPoint.y < node.transform.position.y - 2,
                    "elevated resource pivot still yields reachable ground approach");
            }
            finally { node.transform.position = origin; Physics.SyncTransforms(); }
            node.RequestWork(); c.CommandGather(node);
            Check(c.CurrentResourceNode == node, "gather command accepted");
            var capacity = (float)typeof(CommanderAnt).GetProperty("CarryCapacity", Hidden).GetValue(c);
            typeof(WorkerAnt).GetMethod("RestoreCargo", Hidden).Invoke(c, new object[] { capacity, ResourceType.Soil, null });
            var state = typeof(WorkerAnt).GetField("state", Hidden);
            state.SetValue(c, Enum.Parse(state.FieldType, "Gathering"));
            var before = ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == ResourceType.Soil).Sum(n => n.AmountRemaining) + c.CarriedAmount;
            var naturalBefore = node.AmountRemaining;
            typeof(WorkerAnt).GetMethod("TickGathering", Hidden).Invoke(c, null);
            var after = ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == ResourceType.Soil).Sum(n => n.AmountRemaining) + c.CarriedAmount;
            Check(Mathf.Abs(before - after) < .01f, "batch preserves floor plus carried material");
            Check(node.AmountRemaining == naturalBefore && !node.IsDepleted && node.WorkRequested, "unfinished resource designation preserved");
            Check(!c.IsWorking && c.CurrentResourceNode == null, "home civilian releases finished batch for work priority reevaluation");

            // Supply a ready blueprint: this fixture checks reassignment, not natural resource balance.
            var go = new GameObject("Preparation priority fixture");
            go.transform.position = c.Position;
            var site = go.AddComponent<BuildingConstructionSite>(); site.Initialize(null, 5);
            c.Agent.ResetPath();
            typeof(CommanderAnt).GetField("workScan", Hidden).SetValue(c, 0f);
            c.TickDuty(1f);
            Check(c.ConstructionTarget == site, "higher priority building starts before resource depletion");
            c.CommandStop(); UnityEngine.Object.Destroy(go);
            File.WriteAllText("Temp/preparation-1111-checks-" + label + ".txt", log);
            return log;
        }
        finally { Time.timeScale = 0; UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; }
    }
}
