using System;
using System.IO;
using AntColony.Core;
using AntColony.Save;
using AntColony.World;
using UnityEngine;

public static class ReleaseRecovery1114Checks
{
    public static string Run()
    {
        if (!Application.isPlaying || Time.timeScale != 0 || !(SaveStorage.RootOverride ?? "").Contains("ReleaseFlow1114"))
            throw new Exception("Paused isolated fixture required");
        int count = 0;
        void Check(bool pass, string reason) { if (!pass) throw new Exception(reason); count++; }
        var wave = WaveDefense.Instance;
        Storyteller.Reset(); // Reload does not retain the transient last-crisis timestamp.
        foreach (var remaining in new[] { 600f, 421f, 420f, 0f })
        {
            wave.Restore(new WaveDefense.State { saved = true, wave = 1, peaceLeft = remaining });
            Check(Storyteller.Resting == (remaining > 420), "Saved post-wave recovery boundary " + remaining);
        }
        wave.Restore(new WaveDefense.State { saved = true, wave = 1, peaceLeft = 600 });
        int actors = SceneObjects.All<EventActor>().Length;
        Check(!ColonyEvents.Instance.SpawnNightPredators() && SceneObjects.All<EventActor>().Length == actors, "No predators during recovery");
        wave.Restore(new WaveDefense.State { saved = true, wave = 1, inWave = true, total = 15, pending = 15, peaceLeft = 0 });
        Check(!ColonyEvents.Instance.SpawnNightPredators() && SceneObjects.All<EventActor>().Length == actors, "No predators during wave");
        wave.Restore(new WaveDefense.State { saved = true, wave = 0, peaceLeft = 900 });
        Check(!Storyteller.Resting, "First peace is not a post-wave recovery period");
        var result = "PASS " + count + " recovery assertions; restored wave state fixture, discard before natural play.";
        File.WriteAllText("work/release-flow-1114/recovery-regression.txt", result); return result;
    }
}
