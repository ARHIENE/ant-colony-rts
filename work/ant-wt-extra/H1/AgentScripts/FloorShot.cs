using System;
using System.IO;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// 바닥 텍스처 확인용 캡처(2026-10-04). Play 모드에서 실행. season 0 봄 · 1 여름 · 2 가을 · 3 겨울(계절 중간).
public static class FloorShot
{
    public static async Task<string> Main(string biome = "Garden", string tag = "now", float zoom = 27, int season = 0)
    {
        while (SaveSystem.Busy) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261003, mapSize = MapSize.Small, biome = (MapBiome)Enum.Parse(typeof(MapBiome), biome) });
        while (SaveSystem.Busy) await Task.Delay(50);
        GameMenuController.Instance.Resume(); Time.timeScale = 0;
        GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, GameCalendar.SecondsPerDay * (season + .5f));
        WeatherSystem.Instance.Set(WeatherKind.Clear);
        var camera = Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();
        camera.FocusOn(WorldMapManager.Instance.HomePosition + new Vector3(12, 0, -12));
        camera.GetComponent<Camera>().orthographicSize = zoom;
        await Task.Delay(500);
        var path = Path.GetFullPath($".unity/floor/{biome}-{tag}-s{season}-{zoom}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        ScreenCapture.CaptureScreenshot(path);
        await Task.Delay(1000);
        return path;
    }
}
