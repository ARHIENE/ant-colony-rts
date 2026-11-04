using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 2026-10-05 기획 반영 캡처: 선택 없음 명령 카드 / 장수 명령 카드 / 건설 탭(생활·전력) / 징집소 '출전'. 결과 .unity/save-2026-10-05b/
public static class Hud1005Shot
{
    static GameObject Put(BuildingKind k, Vector3 p)
    {
        var t = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { k, UnitRole.Worker });
        var go = Object.Instantiate(t, p, Quaternion.identity); go.SetActive(true); return go;
    }
    static async Task<string> Shot(string name)
    {
        await Task.Delay(700);
        var path = Path.GetFullPath($".unity/save-2026-10-05b/{name}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        ScreenCapture.CaptureScreenshot(path);
        await Task.Delay(1000);
        return path;
    }

    public static async Task<string> Main()
    {
        while (SaveSystem.Busy) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261005, mapSize = MapSize.Small, biome = MapBiome.Garden });
        while (SaveSystem.Busy) await Task.Delay(50);
        GameMenuController.Instance.Resume(); Time.timeScale = 0;
        var home = WorldMapManager.Instance.HomePosition;
        Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>().FocusOn(home);
        // 화면에 보이게 위생·전력 가구와 징집소를 둔다.
        var o = new Vector3(Mathf.Floor(home.x) + 4.5f, home.y, Mathf.Floor(home.z) - 3.5f);
        Put(BuildingKind.WoodGenerator, o + new Vector3(.5f, 0, .5f));
        for (var i = 2; i <= 4; i++) Put(BuildingKind.PowerWire, o + new Vector3(i, 0, 0));
        Put(BuildingKind.ElectricLamp, o + new Vector3(5, 0, 0)); Put(BuildingKind.Battery, o + new Vector3(2, 0, 1));
        Put(BuildingKind.Toilet, o + new Vector3(-3, 0, 2)); Put(BuildingKind.Washbasin, o + new Vector3(-4, 0, 2)); Put(BuildingKind.Shower, o + new Vector3(-5, 0, 2));
        var post = Put(BuildingKind.ConscriptionPost, o + new Vector3(-4, 0, -4)).GetComponent<ConscriptionPost>();
        var shots = new List<string>();
        var selection = Object.FindAnyObjectByType<SelectionManager>();

        selection?.ClearSelection(); WorkTargetPanel.Clear(); shots.Add(await Shot("1-idle-card"));
        Object.FindAnyObjectByType<RosterBar>().Click(0); shots.Add(await Shot("2-commander-card"));
        selection?.ClearSelection();
        BuildScreen.Open(); shots.Add(await Shot("3-build-living"));
        Object.FindObjectsByType<Button>(FindObjectsInactive.Include).First(b => b.name == "Tab 생활").onClick.Invoke(); shots.Add(await Shot("3-build-living"));
        Object.FindObjectsByType<Button>(FindObjectsInactive.Include).First(b => b.name == "Tab 전력").onClick.Invoke(); shots.Add(await Shot("4-build-power"));
        while (BuildScreen.Back()) { }
        WorkTargetPanel.Select(post); shots.Add(await Shot("5-conscription-card"));
        WorkTargetPanel.Clear();
        return string.Join("\n", shots.Distinct());
    }
}
