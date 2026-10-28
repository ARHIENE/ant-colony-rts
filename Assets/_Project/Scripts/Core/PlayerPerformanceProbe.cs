#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AntColony.Save;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace AntColony.Core
{
    // Opt-in development-player measurement. Normal starts never create this object or touch save paths.
    public sealed class PlayerPerformanceProbe : MonoBehaviour
    {
        public static bool Baseline { get; private set; }
        [Serializable] private class Counter { public string name; public bool valid; public double mean; }
        [Serializable] private class Report
        {
            public string unity, gpu, device, phase;
            public bool baseline;
            public string[] qualityLevels;
            public int width, height, frames, renderers, visible, animators, skinned;
            public double meanMs, p50Ms, p95Ms, p99Ms;
            public Counter[] counters;
        }
        private string output;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            var at = Array.IndexOf(args, "-ant-perf");
            if (at < 0 || at + 1 >= args.Length) return;
            Baseline = Array.IndexOf(args, "-ant-perf-baseline") >= 0;
            var path = Path.GetFullPath(args[at + 1]);
            SaveStorage.RootOverride = Path.Combine(Path.GetDirectoryName(path), "isolated-player-save");
            UserSettings.Apply(new UserSettingsData { autoSaveEnabled = false, autoPauseThreat = false, vSync = false, frameLimit = 0 }, false);
            Application.runInBackground = true;
            var host = new GameObject("Player Performance Probe");
            DontDestroyOnLoad(host);
            host.AddComponent<PlayerPerformanceProbe>().output = path;
        }

        private IEnumerator Start()
        {
            yield return null;
            while (SaveSystem.Busy || !GameSession.Exists || AntColony.UI.GameMenuController.Instance == null) yield return null;
            var team = Enumerable.Range(0, 3).Select(i => { var c = StartingCandidate.Roll(i); c.traits.values.Clear(); return c; }).ToArray();
            SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Medium, biome = MapBiome.Forest, startingCount = 3, startingTeam = team });
            while (SaveSystem.Busy) yield return null;
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
            var camera = UnityEngine.Camera.main.GetComponent<AntColony.Camera.IsometricCameraController>(); camera.enabled = false;
            var until = Time.realtimeSinceStartup + 5;
            while (Time.realtimeSinceStartup < until) yield return null;
            var names = new[] { "Main Thread", "Render Thread", "GC Allocated In Frame", "Draw Calls Count", "Batches Count", "Triangles Count", "BehaviourUpdate", "UpdateRendererBoundingVolumes", "MeshSkinning.Update" };
            var handles = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
            var descriptions = handles.Select(ProfilerRecorderHandle.GetDescription).ToArray();
            var recorders = names.Select(name =>
            {
                var desc = descriptions.FirstOrDefault(d => d.Name == name);
                return desc.Name != null ? ProfilerRecorder.StartNew(desc.Category, name, 1) : default;
            }).ToArray();
            var counters = names.Select((name, i) => new Counter { name = name, valid = recorders[i].Valid }).ToArray();
            var frames = new List<double>();
            until = Time.realtimeSinceStartup + 25;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
                frames.Add(Time.unscaledDeltaTime * 1000);
                for (var i = 0; i < recorders.Length; i++) if (recorders[i].Valid) counters[i].mean += recorders[i].LastValue;
            }
            for (var i = 0; i < recorders.Length; i++)
            {
                counters[i].mean /= Math.Max(1, frames.Count);
                recorders[i].Dispose();
            }
            frames.Sort();
            var renderers = FindObjectsByType<Renderer>();
            var report = new Report
            {
                unity = Application.unityVersion, gpu = SystemInfo.graphicsDeviceName, device = SystemInfo.processorType, baseline = Baseline, qualityLevels = QualitySettings.names,
                phase = "Medium Forest seed281028, 3 commanders, 1x, five-second warmup, 25-second sample",
                width = Screen.width, height = Screen.height, frames = frames.Count,
                meanMs = frames.Average(), p50Ms = frames[(int)(frames.Count * .5)], p95Ms = frames[(int)(frames.Count * .95)], p99Ms = frames[(int)(frames.Count * .99)],
                renderers = renderers.Length, visible = renderers.Count(r => r.isVisible),
                animators = FindObjectsByType<Animator>().Count(a => a.enabled), skinned = FindObjectsByType<SkinnedMeshRenderer>().Length, counters = counters
            };
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Application.Quit();
        }
    }
}
#endif
