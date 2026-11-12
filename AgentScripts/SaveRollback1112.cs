using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;

public static class SaveRollback1112
{
    static string Json(SaveFileV1 f) => (string)typeof(SaveSystem).Assembly.GetType("AntColony.Save.SaveOptionalStates").GetMethod("ToJson", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { f, false });
    public static async Task<string> Run()
    {
        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        var report = new System.Text.StringBuilder();
        void Check(bool ok, string name) { report.AppendLine((ok ? "PASS " : "FAIL ") + name); if (!ok) throw new Exception(name); }
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = false; UserSettings.Apply(quiet, false);
            SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "SaveRollback1112-" + Guid.NewGuid().ToString("N"));
            SimulationPause.UserSpeed(0);
            var before = SaveSnapshot.Capture();
            var text = Json(before);
            Check(SaveValidator.TryParse(text, out var broken, out var error), "baseline validates");
            broken.monsters.First(m => m.key.StartsWith("monster:")).key = "monster:999999";
            Check(SaveValidator.Validate(broken, out error), "wrong scene index remains structurally valid: " + error);
            Check(!SavePreflight.ForScene(broken, out _), "wrong scene index fails scene validation");
            var path = Path.Combine(SaveStorage.Root, "invalid-scene.json");
            SaveStorage.WriteAtomic(path, Json(broken));
            Check(SaveSystem.TryLoad(path, out error), "load queues target-scene validation: " + error);
            var end = DateTime.UtcNow.AddSeconds(60);
            while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(100);
            Check(!SaveSystem.Busy && GameSession.Instance.GameStarted, "failed target load rolls back to playable game");
            GameMenuController.Instance.Resume(); SimulationPause.UserSpeed(0);
            var restored = SaveSnapshot.Capture();
            Check(restored.options.seed == before.options.seed, "rollback preserves seed");
            Check(JsonUtility.ToJson(restored.wave) == JsonUtility.ToJson(before.wave), "rollback preserves wave state");
            Check(restored.monsters.All(m => m.key != "monster:999999"), "invalid scene data never applied");
            Check(SaveSystem.TrySave(false, 0, out error), "rollback game remains saveable: " + error);
            return report.ToString();
        }
        finally { UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; File.WriteAllText("work/three-waves-review-1112/save-rollback-results.txt", report.ToString()); }
    }
}
