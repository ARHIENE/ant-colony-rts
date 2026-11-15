using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AntColony.UI;
using UnityEngine;
using UnityEngine.UI;

public static class CombatReadability1115Checks
{
    const string Log = "work/colony-polish-1115/combat-readability.txt";
    static void Check(bool ok, string message)
    {
        File.AppendAllText(Log, (ok ? "PASS " : "FAIL ") + message + "\n");
        if (!ok) throw new Exception(message);
    }
    public static string Main()
    {
        File.WriteAllText(Log, "Separate notification instance; original notifications and campaign preserved.\n");
        var singleton = typeof(ToastManager).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        var original = singleton.GetValue(null);
        var host = new GameObject("Readability notification fixture");
        try
        {
            var manager = host.AddComponent<ToastManager>();
            for (int i = 0; i < 20; i++) ToastManager.ShowLatest("mood:test", "기분 주의 " + i, ToastKind.Warning);
            Check(ToastManager.Count == 1 && ToastManager.VisibleLines.Single() == "기분 주의 19", "Same commander warning keeps latest state once");
            for (int i = 0; i < 8; i++) ToastManager.Show("완료 " + i);
            ToastManager.SetCrisis("enemy", "적 접근");
            Check(ToastManager.VisibleLines.Count == 6 && ToastManager.VisibleLines[0] == "적 접근", "Default shows five priority notifications and overflow");
            ToastManager.Expand(true);
            Check(ToastManager.VisibleLines.Count == 10, "Expand exposes retained notifications");
            typeof(ToastManager).GetMethod("Rebuild", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            var collapse = host.GetComponentsInChildren<Button>().First(b => b.transform.parent.name == "Toast Less");
            collapse.onClick.Invoke();
            Check(ToastManager.VisibleLines.Count == 6, "Actual collapse button restores compact rail");
            foreach (var rain in UnityEngine.Object.FindObjectsByType<AntColony.Map.Precipitation>())
                Check(rain.GetComponent<ParticleSystemRenderer>().maxParticleSize <= .0071f, "Weather particle screen size is bounded");
            return "PASS notification deduplication, overflow, priority, collapse and particle cap";
        }
        finally { host.SetActive(false); UnityEngine.Object.Destroy(host); singleton.SetValue(null, original); }
    }
}
