using System;
using System.IO;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
public static class ReviewLoad1112
{
    public static async Task<string> Run()
    {
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        try
        {
            while (SaveSystem.Busy) await Task.Delay(100);
            var quiet = settings.Clone(); quiet.autoSaveEnabled = false; UserSettings.Apply(quiet, false);
            SaveStorage.RootOverride = Path.Combine(Path.GetTempPath(), "ReviewLoad1112-" + Guid.NewGuid().ToString("N"));
            if (!SaveSystem.TryLoad(Path.GetFullPath("work/three-waves-1112/after-wave3-raw.json"), out var error)) throw new Exception(error);
            var end = DateTime.UtcNow.AddSeconds(60);
            while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(100);
            if (SaveSystem.Busy) throw new Exception("Load timeout");
            GameMenuController.Instance.Resume(); SimulationPause.UserSpeed(0);
            return "Loaded original snapshot; paused";
        }
        finally { SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
