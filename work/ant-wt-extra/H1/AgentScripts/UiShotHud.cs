using System;
using System.IO;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using UnityEngine;

// 2026-10-24 깨끗한 새 게임 HUD 캡처(UI 점검용). 임시 저장 경로 사용.
public static class UiShotHud
{
    public static async Task<string> Main()
    {
        var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "UiShot-" + Guid.NewGuid().ToString("N"));
        try
        {
            SaveSystem.NewGame(new NewGameOptions { seed = 241024, mapSize = MapSize.Small });
            var until = DateTime.UtcNow.AddSeconds(90);
            await Task.Delay(500); while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
            await Task.Delay(3000);
            Directory.CreateDirectory("Temp/ui-shots"); ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/ui-shots/hud_clean.png"));
            await Task.Delay(500);
            return "HUD shot";
        }
        finally { SaveStorage.RootOverride = root; }
    }
}
