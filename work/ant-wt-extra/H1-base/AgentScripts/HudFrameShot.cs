using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Save;
using AntColony.Core;
using AntColony.Map;
using AntColony.UI;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// HUD v4.1 프레임 확인용 캡처(2026-10-04). Play 모드에서 실행. select = 장수 1명 선택.
public static class HudFrameShot
{
    public static async Task<string> Main(string tag = "hud", bool select = true)
    {
        while (SaveSystem.Busy) await Task.Delay(50);
        if (!AntColony.Core.GameSession.Exists || !AntColony.Core.GameSession.Instance.GameStarted)
        {
            SaveSystem.NewGame(new NewGameOptions { seed = 261003, mapSize = MapSize.Small, biome = MapBiome.Garden });
            while (SaveSystem.Busy) await Task.Delay(50);
        }
        GameMenuController.Instance.Resume(); Time.timeScale = 0;
        var cam = Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();
        cam.FocusOn(WorldMapManager.Instance.HomePosition);
        if (select) Object.FindAnyObjectByType<RosterBar>().Click(0);
        else Object.FindAnyObjectByType<AntColony.Units.SelectionManager>()?.ClearSelection();
        await Task.Delay(600);
        var path = Path.GetFullPath($".unity/floor/{tag}-{Screen.width}x{Screen.height}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        ScreenCapture.CaptureScreenshot(path);
        await Task.Delay(1000);
        return path;
    }
}
