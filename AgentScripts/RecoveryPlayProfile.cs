using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class RecoveryPlayProfile
{
    public static async Task<string> Main()
    {
        if (!Application.isPlaying || !AntColony.Core.GameSession.Instance.GameStarted) throw new Exception("Start a game first");
        var directions = AntColony.World.WorldMapManager.Instance.Sites.Select(s => AntColony.UI.WorldPlanet.Direction(s.MapPosition)).ToArray();
        if (!directions.Any(d => d.z > .2f) || !directions.Any(d => d.y > .9f) || !directions.Any(d => d.y < -.9f))
            throw new Exception("World sites must include the back and both polar regions");
        var menu = AntColony.UI.GameMenuController.Instance;
        menu.Resume(); menu.SetSpeed(1);
        var samples = new List<float>();
        var frame = Time.frameCount;
        try
        {
            await Task.Delay(1000);
            var until = Time.realtimeSinceStartup + 10;
            while (Time.realtimeSinceStartup < until)
            {
                await Task.Yield();
                if (Time.frameCount == frame) continue;
                frame = Time.frameCount; samples.Add(Time.unscaledDeltaTime);
            }
            var sorted = samples.OrderBy(t => t).ToArray();
            return $"World coverage PASS ({directions.Length} sites); normal play {samples.Count} frames, avg {1 / samples.Average():0.0} fps, p95 {sorted[Mathf.Min(sorted.Length - 1, Mathf.FloorToInt(sorted.Length * .95f))] * 1000:0.0} ms, triangles {UnityEditor.UnityStats.triangles}";
        }
        finally { menu.SetSpeed(0); }
    }
}
