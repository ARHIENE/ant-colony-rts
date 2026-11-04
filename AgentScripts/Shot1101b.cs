using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Save;
using AntColony.Core;
using AntColony.Map;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

// 임시(2026-11-01 2차): 장수 3명을 붙여 세워 이름표 겹침 확인. 커밋 전 삭제.
public static class Shot1101b
{
    public static async Task<string> Main()
    {
        while (SaveSystem.Busy || GameMenuController.Instance == null) await Task.Delay(50);
        if (!GameSession.Exists || !GameSession.Instance.GameStarted)
        {
            SaveSystem.NewGame(new NewGameOptions { seed = 261101, mapSize = MapSize.Small, biome = MapBiome.Garden, startingCount = 3 });
            while (SaveSystem.Busy) await Task.Delay(50);
        }
        GameMenuController.Instance.Resume();
        var list = CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead).ToList();
        var basePos = list[0].Position;
        for (int i = 1; i < list.Count; i++) { var a = list[i].GetComponent<UnityEngine.AI.NavMeshAgent>(); a?.Warp(basePos + new Vector3(i * .6f, 0, 0)); }
        Time.timeScale = 0;
        Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>().FocusOn(basePos);
        await Task.Delay(800);
        var path = Path.GetFullPath("Temp/shot-1101/labels.png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        ScreenCapture.CaptureScreenshot(path);
        await Task.Delay(1000);
        return path + " n=" + list.Count;
    }
}
