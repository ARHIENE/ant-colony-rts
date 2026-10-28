using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MoveMarker1028Checks
{
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        var speed = Time.timeScale; var checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("Move marker FAIL " + label); checks++; }
        try
        {
            foreach (var scale in new[] { 0f, 1f, 5f })
            {
                Time.timeScale = scale;
                MoveMarker.Spawn(Vector3.zero, Color.green);
                var marker = Object.FindAnyObjectByType<MoveMarker>();
                var material = marker.GetComponent<Renderer>().sharedMaterial;
                await Task.Delay(150);
                Check(marker == null || marker.transform.localScale.x < 1.4f, "shrinks or already expires at scale " + scale);
                await Task.Delay(400);
                Check(marker == null && material == null, "visual and material expire at scale " + scale);
            }
            Time.timeScale = 0;
            for (var i = 0; i < 32; i++) MoveMarker.Spawn(Vector3.right * i, Color.green);
            Check(Object.FindObjectsByType<MoveMarker>().Length == 32, "rapid click markers spawned");
            await Task.Delay(650);
            Check(Object.FindObjectsByType<MoveMarker>().Length == 0, "rapid paused clicks leave no traces");
            var result = "PASS " + checks + " paused/normal/fast marker expiry, material cleanup and 32 rapid clicks";
            File.WriteAllText("Temp/move-marker1028-result.txt", result); return result;
        }
        finally
        {
            foreach (var marker in Object.FindObjectsByType<MoveMarker>()) Object.Destroy(marker.gameObject);
            Time.timeScale = speed;
        }
    }
}
