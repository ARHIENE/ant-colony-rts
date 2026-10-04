using System;
using System.IO;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;

// SAVE 전용 준비. 실제 캡처는 공식 CLI capture_game_view(source=screen)로 한다.
public static class SaveHudCapture
{
    public static async Task<string> Main()
    {
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "SaveCapture-" + Guid.NewGuid().ToString("N"));
        var settings = UserSettings.Current.Clone(); settings.autoSaveEnabled = false; UserSettings.Apply(settings, false);
        while (SaveSystem.Busy) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261003, mapSize = MapSize.Small, biome = MapBiome.Garden });
        while (SaveSystem.Busy) await Task.Delay(50);
        GameMenuController.Instance.Resume(); Time.timeScale = 0;
        var camera = UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();
        camera.FocusOn(WorldMapManager.Instance.HomePosition);
        UnityEngine.Object.FindAnyObjectByType<RosterBar>().Click(0);
        Canvas.ForceUpdateCanvases();
        await Task.Delay(500);
        return "Garden HUD ready; temporary save root; capture before stopping Play";
    }
}
