using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;

// Paused A/B of the obsolete prototype mesh only. The imported facility and attack line stay intact.
public static class ThreeWaves1112TowerVisual
{
    const string Dir = "work/three-waves-1112/";
    public static async Task<string> Compare()
    {
        if (!Application.isPlaying || Time.timeScale != 0) throw new Exception("Paused isolated loaded game required");
        var towers = SceneObjects.All<AcidTower>().Where(t => t.isActiveAndEnabled && !t.IsDead).ToArray();
        if (towers.Length == 0) throw new Exception("No actual built tower");
        var old = towers.Select(t => t.transform.Find("AcidReservoir")?.GetComponent<Renderer>()).ToArray();
        var report = string.Join("\n", towers.Select(t => $"{t.Position} range={t.Range} damage={t.Damage} interval={t.AttackInterval} hp={t.CurrentHealth}"));
        for (int i = 0; i < towers.Length; i++)
        {
            if (old[i] == null || !old[i].forceRenderingOff) throw new Exception("Prototype cylinder was not hidden by facility model setup");
            var visual = towers[i].GetComponent<FacilityVisual>();
            if (visual == null || visual.Model == null || !visual.Model.GetComponentsInChildren<Renderer>().Any(r => r.enabled && !r.forceRenderingOff)) throw new Exception("Imported tower invisible");
            var spray = towers[i].GetComponentInChildren<LineRenderer>(true);
            if (spray == null || spray.forceRenderingOff) throw new Exception("Attack spray was hidden");
        }
        try
        {
            foreach (var r in old) r.forceRenderingOff = false;
            await Task.Delay(1100); ScreenCapture.CaptureScreenshot(Dir + "screens/tower-before.png"); await Task.Delay(300);
            foreach (var r in old) r.forceRenderingOff = true;
            await Task.Delay(1100); ScreenCapture.CaptureScreenshot(Dir + "screens/tower-after.png"); await Task.Delay(300);
            File.WriteAllText(Dir + "tower-visual-checks.txt", "PASS " + towers.Length + " imported towers visible, prototype cylinders hidden, attack lines preserved\n" + report);
            return report;
        }
        finally { foreach (var r in old) r.forceRenderingOff = true; }
    }
}
