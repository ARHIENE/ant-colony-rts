using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Save;
using UnityEngine;
public static class FermentationClock1028Checks
{
    public static async Task<string> Main()
    {
        var barrel = SceneObjects.All<Processor>().First(p => p.Brewing);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var capture = typeof(Processor).GetMethod("CaptureState", flags); var restore = typeof(Processor).GetMethod("RestoreState", flags);
        var original = (Processor.State)capture.Invoke(barrel, null); var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride; var scale = Time.timeScale;
        try
        {
            SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "FermentationClock-" + Guid.NewGuid().ToString("N"));
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            var state = new Processor.State { current = 0, fermenting = true }; restore.Invoke(barrel, new object[] { state });
            if (barrel.NeedsWork) throw new Exception("Fermenting barrel requests a worker");
            Time.timeScale = 1; await Task.Delay(300); Time.timeScale = 0;
            var elapsed = ((Processor.State)capture.Invoke(barrel, null)).fermentation;
            if (!(elapsed > 0 && elapsed < FoodProcessingRecipes.FermentSeconds) || barrel.NeedsWork) throw new Exception("Real frame unattended fermentation did not advance");
            await Task.Delay(150);
            if (((Processor.State)capture.Invoke(barrel, null)).fermentation != elapsed) throw new Exception("Game pause advances fermentation");
            var result = "PASS 3 real-frame unattended fermentation and game-pause checks"; File.WriteAllText("Temp/fermentation-clock1028-result.txt", result); return result;
        }
        finally { restore.Invoke(barrel, new object[] { original }); Time.timeScale = scale; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
