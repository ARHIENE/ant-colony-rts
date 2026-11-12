using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

public static class WaveSaveHerbs1112
{
    const string Dir = "work/three-waves-review-1112/";
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(100);
        if (SaveSystem.Busy) throw new Exception("Save/load did not finish");
        GameMenuController.Instance?.Resume();
        SimulationPause.UserSpeed(0);
        await Task.Delay(100);
    }
    public static async Task<string> Run()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        var report = new System.Text.StringBuilder(); int count = 0;
        void Check(bool ok, string label)
        { report.AppendLine((ok ? "PASS " : "FAIL ") + label); if (!ok) throw new Exception(label); count++; }
        try
        {
            await Ready();
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false;
            UserSettings.Apply(quiet, false);
            SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "WaveSaveHerbs1112-" + Guid.NewGuid().ToString("N"));
            var rawPath = Path.GetFullPath("work/three-waves-1112/after-wave3-raw.json");
            Check(SaveValidator.TryParse(File.ReadAllText(rawPath), out var raw, out var error), "original three-wave snapshot parses: " + error);
            Check(SavePreflight.Validate(raw, out error), "original expired enemy remains validate: " + error);
            var body = raw.corpses.First(c => c.kind == CorpseKind.EnemyCommander && c.remaining == 0);
            var id = body.commanderId;
            body.commanderId = "";
            Check(!SavePreflight.Validate(raw, out _), "expired enemy without identity remains rejected");
            body.commanderId = id; body.kind = CorpseKind.Ant;
            Check(!SavePreflight.Validate(raw, out _), "expired ordinary corpse remains rejected");
            body.kind = CorpseKind.EnemyCommander; body.remaining = -1;
            Check(!SavePreflight.Validate(raw, out _), "negative decay time remains rejected");
            body.remaining = 0;
            Check(SaveSystem.TryLoad(rawPath, out error), "actual original snapshot load: " + error);
            await Ready();
            var current = SaveSnapshot.Capture();
            foreach (var prior in raw.nodes.Where(n => n.key.StartsWith("mat:")))
            {
                var restored = current.nodes.Single(n => n.key == prior.key);
                Check(restored.exists == prior.exists && (!prior.exists || restored.type == prior.type
                    && Vector3.Distance(restored.position.ToVector3(), prior.position.ToVector3()) < .01f
                    && Mathf.Abs(restored.amount - prior.amount) < .01f), "old material key preserved " + prior.key);
            }
            var herbs = MaterialDeposits.Spawned.Where(n => n != null && n.ResourceType == ResourceType.Herb).ToArray();
            Check(herbs.Length == 2 && herbs.All(n => n.AmountRemaining > 0 && n.RegrowSeconds > 0), "two naturally spawned regrowing herb nodes");
            var team = CommanderRoster.Instance.Commanders.Where(c => c != null && c.IsColonyMember && !c.IsDead).ToArray();
            Check(herbs.Any(n => team.Any(c => c.TryWorkApproach(n.transform.position, out _))), "herbs reachable from surviving colony");
            Check(herbs.All(n => n.NeedsDesignation && n.WorkKind == NaturalWorkKind.Harvesting), "herbs use normal designated harvesting");
            Check(Corpse.All.Count(c => c.Data.kind == CorpseKind.EnemyCommander && c.Data.remaining == 0) >= 2, "actual expired enemy remains restored");
            Check(SaveSystem.TrySave(false, 0, out error), "actual post-combat save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "actual saved slot load: " + error);
            await Ready();
            Check(WaveDefense.Instance.Wave == 3 && !WaveDefense.Instance.InWave, "completed third wave preserved");
            Check(Corpse.All.Count(c => c.Data.kind == CorpseKind.EnemyCommander && c.Data.remaining == 0) >= 2, "expired enemy remains survive roundtrip");
            Check(MaterialDeposits.Spawned.Count(n => n != null && n.ResourceType == ResourceType.Herb) == 2, "new herb nodes survive roundtrip");
            SaveSystem.NewGame(new NewGameOptions { seed = 910273, mapSize = MapSize.Small, biome = MapBiome.Desert });
            await Ready();
            Check(SaveSnapshot.Capture().options.seed == 910273, "different map created before loading");
            Check(SaveSystem.TryLoad(rawPath, out error), "different map accepts original snapshot: " + error);
            await Ready();
            Check(SaveSnapshot.Capture().options.seed == raw.options.seed && WaveDefense.Instance.Wave == 3, "different map loads saved map and wave");
            report.AppendLine("TOTAL PASS " + count);
            return report.ToString();
        }
        finally
        {
            Time.timeScale = 0; UserSettings.Apply(settings, false); SaveStorage.RootOverride = root;
            File.WriteAllText(Dir + "save-herbs-results.txt", report.ToString());
        }
    }
}
