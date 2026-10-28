using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

public static class Performance1028Probe
{
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Performance1028-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = false; quiet.autoPauseThreat = false; quiet.vSync = false; quiet.frameLimit = 0;
            UserSettings.Apply(quiet, false);
            var end = DateTime.UtcNow.AddSeconds(90);
            while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(60);
            var team = Enumerable.Range(0, 3).Select(i => { var c = StartingCandidate.Roll(i); c.traits.values.Clear(); return c; }).ToArray();
            SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Medium, biome = MapBiome.Forest, startingCount = 3, startingTeam = team });
            end = DateTime.UtcNow.AddSeconds(90);
            while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(60);
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
            var camera = UnityEngine.Camera.main.GetComponent<AntColony.Camera.IsometricCameraController>(); camera.enabled = false;
            try
            {
                await Task.Delay(3000);
                ProfilerDriver.ClearAllFrames(); ProfilerDriver.enabled = true; Profiler.enabled = true;
                var start = Time.frameCount; var samples = new List<float>();
                while (Time.frameCount < start + 90) { await Task.Yield(); if (Time.frameCount != start) samples.Add(Time.unscaledDeltaTime); }
                ProfilerDriver.enabled = false; Profiler.enabled = false;
                var totals = new Dictionary<string, float>(); var frames = 0;
                for (var f = ProfilerDriver.firstFrameIndex; f <= ProfilerDriver.lastFrameIndex; f++)
                {
                    using var view = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName | HierarchyFrameDataView.ViewModes.InvertHierarchy, HierarchyFrameDataView.columnSelfTime, false);
                    if (view == null || !view.valid) continue;
                    frames++; var kids = new List<int>(); view.GetItemChildren(view.GetRootItemID(), kids);
                    foreach (var id in kids) { var name = view.GetItemName(id); totals[name] = (totals.TryGetValue(name, out var t) ? t : 0) + view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnSelfTime); }
                }
                var renderers = UnityEngine.Object.FindObjectsByType<Renderer>();
                var result = new StringBuilder("Medium Forest seed281028, 3 commanders, 1x\n");
                result.AppendLine($"frames={frames} meanMs={samples.Average() * 1000:0.00} p95Ms={samples.OrderBy(x => x).ElementAt((int)(samples.Count * .95)) * 1000:0.00}");
                result.AppendLine($"renderers={renderers.Length} visible={renderers.Count(r=>r.isVisible)} animators={UnityEngine.Object.FindObjectsByType<Animator>().Count(a=>a.enabled)} skinned={UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>().Length} drawCalls={UnityEditor.UnityStats.drawCalls} triangles={UnityEditor.UnityStats.triangles}");
                foreach (var pair in totals.OrderByDescending(p=>p.Value).Take(25)) result.AppendLine($"{pair.Value / Math.Max(1, frames):0.000}ms {pair.Key}");
                File.WriteAllText("Temp/performance1028-editor.txt", result.ToString()); return result.ToString();
            }
            finally { camera.enabled = true; }
        }
        finally { ProfilerDriver.enabled = false; Profiler.enabled = false; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); Time.timeScale = 0; }
    }
}
