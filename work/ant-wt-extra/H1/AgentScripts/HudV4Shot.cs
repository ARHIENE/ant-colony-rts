using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

// HUD v4 SAVE 캡처: 선택 없음(+알림 위치 버튼) / 다중 선택 / 숙소 선택을 .unity/save-2026-10-03에 저장.
public static class HudV4Shot
{
    static async Task Shot(string name)
    {
        await Task.Delay(400);
        var dir = Path.GetFullPath(".unity/save-2026-10-03"); Directory.CreateDirectory(dir);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
        await Task.Delay(600);
    }

    public static async Task<string> Main()
    {
        string original = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "HudV4Shot-" + Guid.NewGuid().ToString("N"));
        GameObject made = null;
        try
        {
            for (int i = 0; i < 600 && SaveSystem.Busy; i++) await Task.Delay(50);
            SaveSystem.NewGame(new NewGameOptions { seed = 261003, mapSize = MapSize.Small });
            await Task.Delay(500); for (int i = 0; i < 600 && SaveSystem.Busy; i++) await Task.Delay(50);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var selection = Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();
            var list = HudOverview.HomeCommanders;

            selection.ClearSelection();
            ToastManager.SetCrisis("v4-shot", "적 발견: 북쪽 입구", () => { });
            await Shot("hud-v4-empty");
            ToastManager.SetCrisis("v4-shot", null);

            selection.SelectOnly(list[0].GetComponent<AntColony.Units.SelectableObject>());
            typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(selection, new object[] { list[1].GetComponent<AntColony.Units.SelectableObject>() });
            await Shot("hud-v4-multi");

            selection.ClearSelection();
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { BuildingKind.Dormitory, UnitRole.Worker });
            made = Object.Instantiate(template, list[0].Position + Vector3.right * 6, Quaternion.identity); made.SetActive(true);
            WorkTargetPanel.Select(made.GetComponent<Dormitory>());
            await Shot("hud-v4-dorm");
            return "shots saved";
        }
        finally { if (made != null) Object.Destroy(made); ToastManager.SetCrisis("v4-shot", null); SaveStorage.RootOverride = original; }
    }
}
