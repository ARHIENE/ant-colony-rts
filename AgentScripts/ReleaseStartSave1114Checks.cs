using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.World;
using AntColony.UI;
using UnityEngine;

public static class ReleaseStartSave1114Checks
{
    public static async Task<string> Run()
    {
        if (!Application.isPlaying || Time.timeScale != 0 || !(SaveStorage.RootOverride ?? "").Contains("ReleaseFlow1114"))
            throw new Exception("Paused isolated natural game required");
        int count = 0;
        void Check(bool pass, string reason) { if (!pass) throw new Exception(reason); count++; }
        var initial = GameSession.Instance.Options.startingCount;
        var waves = Enumerable.Range(1, 3).Select(WaveDefense.Count).ToArray();
        Check(SaveSystem.TrySave(false, 0, out var error), "Normal save: " + error);
        var file = SaveSlots.PathFor(false, 0);
        var json = File.ReadAllText(file);
        Check(SaveValidator.TryParse(json, out var saved, out error) && saved.options.startingCount == initial, "Starting count captured: " + error);
        Check(SaveSystem.TryLoad(file, out error), "Normal load: " + error);
        var end = DateTime.UtcNow.AddSeconds(45);
        while ((SaveSystem.Busy || !GameSession.Instance.GameStarted) && DateTime.UtcNow < end) await Task.Delay(100);
        SimulationPause.UserSpeed(0);
        Check(!SaveSystem.Busy && GameSession.Instance.Options.startingCount == initial, "Starting count survives scene reload");
        Check(waves.SequenceEqual(Enumerable.Range(1, 3).Select(WaveDefense.Count)), "First three wave counts survive reload");
        foreach (int value in new[] { 1, 2, 3, 4, -1 })
        {
            var edited = Regex.Replace(json, "\"startingCount\"\\s*:\\s*\\d+", "\"startingCount\":" + value);
            bool valid = SaveValidator.TryParse(edited, out var parsed, out error);
            Check(valid == (value >= 1 && value <= 3) && (!valid || parsed.options.startingCount == value), "Count validation " + value);
        }
        var legacy = Regex.Replace(json, "\"startingCount\"\\s*:\\s*\\d+\\s*,", "");
        Check(SaveValidator.TryParse(legacy, out var old, out error) && old.options.startingCount == 0, "Missing field remains compatible: " + error);
        var result = "PASS " + count + "; normal start=" + initial + " save/reload; wave counts=" + string.Join(",", waves) + "; missing/invalid JSON fixture checks separate from natural play.";
        File.WriteAllText("work/release-flow-1114/start-save-regression.txt", result); return result;
    }
}
