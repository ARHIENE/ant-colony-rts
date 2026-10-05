using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 2026-10-05 건설 분류 16종: 탭 이름·순서와 기존 가구의 새 분류 위치를 확인한다.
public static class BuildCategoryChecks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); checks++; }
    static async Task Frames() { var frame = Time.frameCount; for (int i = 0; i < 200 && Time.frameCount < frame + 5; i++) await Task.Delay(20); Canvas.ForceUpdateCanvases(); }
    static async Task Ready() { for (int i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50); Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0; }
    static Transform Find(string name) => Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include).FirstOrDefault(r => r.name == name);

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        string original = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "BuildCategory-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261005, mapSize = MapSize.Small }); await Ready();
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            BuildScreen.Open(); await Frames();
            var tabs = new[] { "타일", "생활", "저장", "환경", "전력", "자동화", "배관", "식량", "작업", "의료", "방어", "휴게", "장식", "군사", "마을", "이동" };
            foreach (var t in tabs) Check(Find("Tab " + t) != null && Find("Page " + t) != null, "tab " + t);
            Check(Find("Tab 오락") == null && Find("Tab 벽문") == null && Find("Tab 특수") == null, "old tabs removed");
            foreach (var t in tabs) Check(t.Length >= 2 && t.Length <= 3, "tab name 2~3 chars " + t);
            void In(string tab, params string[] buttons) { foreach (var b in buttons) Check(Find("Page " + tab).GetComponentsInChildren<Button>(true).Any(x => x.name == "Build " + b), b + " in " + tab); }
            In("타일", "SoilWall", "LeafWall", "CapWall", "Door");
            In("방어", "CastleWall", "Gate", "TrapPit", "MineField", "AcidTower", "Watchtower");
            In("군사", "Barracks Melee", "Barracks Flying", "ConscriptionPost");
            In("마을", "Hut", "House", "Apartment");
            In("휴게", "RestRoom", "Campfire", "GamblingDen");
            In("장식", "FlowerPot", "FireflyLamp");
            In("생활", "Dormitory", "Kitchen", "Nursery");
            In("저장", "Storage");
            var tabRect = (RectTransform)Find("Tab 타일"); var second = (RectTransform)Find("Tab 작업");
            Check(second.anchoredPosition.y < tabRect.anchoredPosition.y - 20, "tabs wrap to second row " + tabRect.anchoredPosition + " " + second.anchoredPosition);
            while (BuildScreen.Back()) { }
            return "PASS " + checks + " build category checks";
        }
        finally { SaveStorage.RootOverride = original; }
    }
}
