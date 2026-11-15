using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;

public static class ConstructionOrder1115Checks
{
    const string Log = "work/colony-polish-1115/construction-order.txt";
    static readonly BindingFlags Hidden = BindingFlags.Static | BindingFlags.NonPublic;
    public static string RepairLiveIdentifiers()
    {
        var sites = SceneObjects.All<BuildingConstructionSite>().Where(s => s.isActiveAndEnabled).ToArray();
        var sequence = typeof(BuildingConstructionSite).GetField("orderSeq", Hidden);
        long next = Math.Max((long)sequence.GetValue(null), sites.Max(s => s.Order));
        File.WriteAllText(Log, "Repair duplicate identity metadata only; work, materials, workers and progression preserved.\n");
        foreach (var group in sites.GroupBy(s => s.Order)) foreach (var duplicate in group.Skip(1))
        {
            long before = duplicate.Order;
            typeof(BuildingConstructionSite).GetProperty("Order").SetValue(duplicate, ++next);
            File.AppendAllText(Log, $"REPAIR {duplicate.BuildingKind} order {before}->{next}, remaining work {duplicate.RemainingWork}\n");
        }
        sequence.SetValue(null, next);
        return "Repaired duplicate construction identities; capture validated checkpoint next";
    }
    public static string VerifySequence()
    {
        var sequence = typeof(BuildingConstructionSite).GetField("orderSeq", Hidden);
        long before = (long)sequence.GetValue(null);
        var a = new GameObject("Order restore fixture"); a.SetActive(false);
        var b = new GameObject("New order fixture"); b.SetActive(false);
        try
        {
            var restored = a.AddComponent<BuildingConstructionSite>();
            typeof(BuildingConstructionSite).GetProperty("Order").SetValue(restored, before + 100);
            var fresh = b.AddComponent<BuildingConstructionSite>();
            if (fresh.Order <= restored.Order) throw new Exception("New blueprint repeats restored sequence");
            File.AppendAllText(Log, "PASS restoring a high construction order advances newly allocated identities\n");
            typeof(BuildingConstructionSite).GetProperty("Order").SetValue(restored, before);
            if ((long)sequence.GetValue(null) < fresh.Order) throw new Exception("Low restored order rewound sequence");
            File.AppendAllText(Log, "PASS restoring lower order does not rewind identity sequence\n");
            return "PASS construction sequence after reload";
        }
        finally { UnityEngine.Object.Destroy(a); UnityEngine.Object.Destroy(b); }
    }
}
