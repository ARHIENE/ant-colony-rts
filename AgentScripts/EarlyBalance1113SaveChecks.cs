using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

public static class EarlyBalance1113SaveChecks
{
    public static string Run()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SaveStorage.RootOverride) || Time.timeScale != 0)
            throw new Exception("Paused isolated game required");
        var worker = CommanderRoster.Instance.Commanders.First();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var field = typeof(WorkerAnt).GetField("targetNode", flags);
        var capture = typeof(WorkerAnt).GetMethod("CaptureWorkerOrder", flags);
        var previous = field.GetValue(worker);
        var stateField = typeof(WorkerAnt).GetField("state", flags);
        var previousState = stateField.GetValue(worker);
        var cargo = worker.CarriedAmount;
        var obj = new GameObject("Depleted loose cargo save regression");
        obj.SetActive(false);
        var node = obj.AddComponent<ResourceNode>();
        node.ConfigureLoot(ResourceType.Wood, 0);
        try
        {
            // Populate normal save references before isolating the stale-target edge case.
            SaveSnapshot.Capture();
            field.SetValue(worker, node);
            stateField.SetValue(worker, Enum.Parse(stateField.FieldType, "Gathering"));
            var order = (WorkerOrderDto)capture.Invoke(worker, null);
            if (order.node != null || order.state != 0 || worker.CarriedAmount != cargo) throw new Exception("Depleted reference, state, or cargo changed");
            if (!SaveSystem.TrySave(false, 2, out var error)) throw new Exception(error);
            // A live unregistered target must still be rejected, rather than silently discarded.
            node.ConfigureLoot(ResourceType.Wood, 2);
            obj.SetActive(true);
            bool rejected = false;
            try { capture.Invoke(worker, null); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException) { rejected = true; }
            if (!rejected) throw new Exception("Live unregistered target was silently lost");
        }
        finally { field.SetValue(worker, previous); stateField.SetValue(worker, previousState); UnityEngine.Object.DestroyImmediate(obj); }
        var result = "PASS depleted loose-node reference omitted; cargo preserved; normal save succeeds; live missing reference still rejected; fixture restored.";
        File.WriteAllText("work/early-balance-1113/depleted-node-save-checks.txt", result);
        return result;
    }
}
