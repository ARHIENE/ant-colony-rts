using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.Profiling;

// Disposable new game only. Returns immediately; report records completion and restored settings.
// This measures Editor frame time, not campaign balance or 4000 independent agents.
public static class OrganicScale1114Probe
{
    const string Report = "work/organic-combat-1114/scale-report.txt";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> Log = new List<string>();
    static void Note(string line) { Log.Add(line); File.WriteAllLines(Report, Log); }
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); Note("PASS " + label); }
    static async Task Frame()
    {
        int frame = Time.frameCount;
        do { await Task.Yield(); if (!Application.isPlaying) throw new Exception("Play stopped during probe"); }
        while (Time.frameCount == frame);
    }
    static async Task Wait(Func<bool> ready)
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (!ready()) { if (DateTime.UtcNow > end) throw new Exception("Initialization timeout"); await Frame(); }
    }
    static async Task Measure(string label, WildMonster[] hosts, float seconds = 6)
    {
        var samples = new List<float>();
        var before = hosts.Select(h => h.Position).ToArray();
        int minBodies = int.MaxValue, maxBodies = 0;
        var end = Time.realtimeSinceStartupAsDouble + seconds;
        while (Time.realtimeSinceStartupAsDouble < end)
        {
            await Frame(); samples.Add(Time.unscaledDeltaTime * 1000);
            int bodies = hosts.Where(h => h != null && !h.IsDead).Sum(h => h.HordeCount);
            minBodies = Math.Min(minBodies, bodies); maxBodies = Math.Max(maxBodies, bodies);
        }
        samples.Sort();
        int moved = hosts.Where((h, i) => h != null && Vector3.Distance(h.Position, before[i]) > .1f).Count();
        Note($"MEASURE {label}: frames={samples.Count} meanMs={samples.Average():F2} p95Ms={samples[Math.Min(samples.Count - 1, (int)Math.Ceiling(samples.Count * .95) - 1)]:F2} maxMs={samples.Last():F2} bodies={minBodies}..{maxBodies} movedHosts={moved} allocatedMB={Profiler.GetTotalAllocatedMemoryLong() / 1048576}");
        if (hosts.Length > 0) Check(minBodies == 4000 && maxBodies == 4000, label + " maintains 4000 logical bodies");
    }
    public static string Main()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        if (GameObject.Find("OrganicScale1114Running") != null) throw new Exception("Probe already running");
        var marker = new GameObject("OrganicScale1114Running");
        UnityEngine.Object.DontDestroyOnLoad(marker);
        Log.Clear(); Note("RUNNING " + DateTime.UtcNow.ToString("O"));
        _ = Run(marker);
        return "Started; poll work/organic-combat-1114/scale-report.txt until RESTORED. Do not stop Play before that.";
    }
    static async Task Run(GameObject marker)
    {
        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        var random = UnityEngine.Random.state;
        bool passed = false;
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false;
            UserSettings.Apply(quiet, false);
            SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "OrganicScale-" + Guid.NewGuid().ToString("N"));
            await Wait(() => !SaveSystem.Busy);
            SaveSystem.NewGame(new NewGameOptions { seed = 261114, mapSize = MapSize.Small, startingCount = 3,
                startingTeam = Enumerable.Range(0, 3).Select(i => StartingCandidate.Roll(i)).ToArray() });
            await Wait(() => !SaveSystem.Busy && WaveDefense.Instance != null);
            GameMenuController.Instance.Resume(); Time.timeScale = 1;
            foreach (var c in CommanderRoster.Instance.Commanders) if (c != null) c.enabled = false;
            var wave = WaveDefense.Instance; wave.enabled = false;
            var camera = SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
            typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView", Private)
                .Invoke(camera, new object[] { HomeReturnPoint.Anchor, 0f, 45f });
            await Measure("baseline", Array.Empty<WildMonster>());
            Time.timeScale = 0;
            UnityEngine.Random.InitState(261114);
            var state = (WaveDefense.State)typeof(WaveDefense).GetField("s", Private).GetValue(wave);
            state.wave = 28;
            Check(wave.StartNow(), "wave29 starts through production API");
            Check(state.total == 4000, "wave29 target is 4000");
            var spawn = typeof(WaveDefense).GetMethod("SpawnGroup", Private);
            var local = SceneObjects.Any<LocalIncursions>();
            int attempts = 0;
            double spawnStart = Time.realtimeSinceStartupAsDouble;
            while (state.pending > 0 && attempts++ < 180)
            {
                spawn.Invoke(wave, null);
                foreach (var h in local.Visitors) if (h != null) h.enabled = false;
                await Frame();
            }
            Check(state.pending == 0, "all pending bodies spawned without dropping failures");
            var hosts = local.Visitors.Where(h => h != null && !h.IsDead).ToArray();
            Check(hosts.Sum(h => h.HordeCount) == 4000, "production spawner totals 4000 bodies");
            Check(hosts.Count(h => h is EnemyCommander) == 1, "one original commander");
            Check(hosts.Where(h => h.IsOrganic).Select(h => h.OrganicKind).Distinct().Count() == 17, "all 17 species present in wave29");
            Note($"SPAWN hosts={hosts.Length} seconds={Time.realtimeSinceStartupAsDouble - spawnStart:F2} groups={attempts}");
            var damage = typeof(WildMonster).GetField("attackDamage", Private);
            var organic = typeof(WildMonster).GetField("organic", Private);
            foreach (var h in hosts)
            {
                damage.SetValue(h, 0f);
                if (h.IsOrganic) ((OrganicCombatState)organic.GetValue(h)).spawnCharges = 0;
                h.enabled = true;
            }
            Time.timeScale = 1;
            double warmup = Time.realtimeSinceStartupAsDouble + 2;
            while (Time.realtimeSinceStartupAsDouble < warmup) await Frame();
            var visuals = hosts.Select(h => h.GetComponent<HordeVisual>()).Where(h => h != null).ToArray();
            Check(visuals.All(h => h.enabled), "all horde visuals prepared");
            Check(visuals.Sum(h => h.ExtraBodies) + hosts.Length == 4000, "visible body accounting reaches 4000");
            Note("SPECIES " + string.Join(", ", hosts.Where(h => h.IsOrganic).GroupBy(h => h.OrganicKind).Select(g => g.Key + ":" + g.Sum(h => h.HordeCount))));
            await Measure("all enabled A", hosts);
            foreach (var h in visuals) h.enabled = false;
            await Measure("horde rendering off", hosts);
            foreach (var h in visuals) h.enabled = true;
            await Measure("all enabled B", hosts);
            foreach (var h in hosts) h.enabled = false;
            await Measure("enemy AI off; NavMesh on", hosts);
            foreach (var h in hosts) h.Agent.enabled = false;
            await Measure("enemy AI and NavMesh off", hosts);
            passed = true; Note("PASS stress measurements completed; zero damage and no brood births used to hold count fixed");
        }
        catch (Exception error) { Note("FAILED " + error); }
        finally
        {
            Time.timeScale = 0; UserSettings.Apply(settings, false); SaveStorage.RootOverride = root;
            UnityEngine.Random.state = random;
            if (marker != null) UnityEngine.Object.Destroy(marker);
            Note("RESTORED settings/save root; result=" + (passed ? "PASS" : "FAIL") + "; stop Play to discard scene");
        }
    }
}
