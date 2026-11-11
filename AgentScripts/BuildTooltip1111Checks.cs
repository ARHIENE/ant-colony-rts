using System;
using System.IO;
using System.Collections.Generic;
using AntColony.Units;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;

// BUILD-TOOLTIP-1111: 건설 메뉴 초반 건물 툴팁의 내용과 실제 표시를 격리된 새 게임에서 확인한다.
// 사용자 저장 슬롯은 쓰지 않는다. 단일 Run 진입점으로 실행한다(설정/저장루트/해상도 finally 복원).
public static class BuildTooltip1111Checks
{
    static int passed;
    static UserSettingsData original;
    static string originalRoot;
    static string resolution = "current";
    static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool value, string name) { if (!value) throw new Exception("FAIL " + name); passed++; }
    static GameMenuController Menu => GameMenuController.Instance;
    static Button Find(string name) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include).First(b => b.name == name);

    public static async Task<string> Prepare()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        for (int i = 0; i < 600 && (SaveSystem.Busy || Menu == null || !GameSession.Exists); i++) await Task.Delay(100);
        if (SaveSystem.Busy || Menu == null || !GameSession.Exists) throw new Exception("Initial editor scene still loading");
        originalRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "BuildTooltip1111-" + Guid.NewGuid().ToString("N"));
        original = UserSettings.Current.Clone();
        var quiet = UserSettings.Current.Clone(); quiet.autoSaveEnabled = false; UserSettings.Apply(quiet, false);
        SaveSystem.NewGame(new NewGameOptions { seed = 261111, mapSize = MapSize.Small, biome = MapBiome.Forest, startingCount = 3,
            startingTeam = Enumerable.Range(0, 3).Select(StartingCandidate.Roll).ToArray() });
        for (var i = 0; i < 300 && (SaveSystem.Busy || Menu == null || !GameSession.Exists || !GameSession.Instance.GameStarted); i++) await Task.Delay(100);
        if (SaveSystem.Busy || Menu == null || !GameSession.Exists || !GameSession.Instance.GameStarted)
            throw new Exception("Isolated world did not become ready within 30 seconds; verification aborted.");
        Menu.Resume(); SimulationPause.UserSpeed(0); await Task.Delay(300);
        return "Isolated build-tooltip world ready, autosave disabled; user slots untouched.";
    }

    // 대상: 탭 이름, 버튼 이름, 반드시 들어가야 하는 문구.
    static readonly (string tab, string button, string[] must)[] Targets =
    {
        ("저장", "Build Storage", new[] { "<b>창고</b>", "보관 한도", "운반" }),
        ("생활", "Build SleepingMat", new[] { "<b>자리</b>", "1명 잠자리", "잠자리" }),
        ("생활", "Build SingleBed", new[] { "<b>침대</b>", "1명 잠자리" }),
        ("생활", "Build Dormitory", new[] { "<b>숙소</b>", "4명 잠자리" }),
        ("생활", "Build Toilet", new[] { "<b>화장실</b>", "위생" }),
        ("식량", "Build Farm", new[] { "<b>밭</b>", "농사", "종자" }),
        ("작업", "Build ScienceLab", new[] { "<b>정착지 연구대</b>", "연구 메뉴에서 담당", "연구비" }),
        ("방어", "Build AcidTower", new[] { "<b>산성탑</b>", "사거리", "2웨이브" }),
        ("타일", "Build SoilWall", new[] { "<b>흙벽</b>", "통행", "연구 미완료: 흙벽 공법" }),
        ("타일", "Build LeafWall", new[] { "<b>나뭇잎 벽</b>", "불에 탐", "선행 연구 없음" }),
        ("타일", "Build CapWall", new[] { "<b>병뚜껑 벽</b>", "통행", "흙벽 공법" }),
        ("방어", "Build CastleWall", new[] { "<b>성벽</b>", "방어", "흙벽 공법" }),
        ("타일", "Build Door", new[] { "<b>문</b>", "아군은 지나가고" }),
        ("방어", "Build TrapPit", new[] { "<b>끈끈이 함정</b>", "함정 공학", "← 흙벽 공법" }),
        ("방어", "Build SpikeTrap", new[] { "<b>가시 함정</b>", "함정 공학" }),
        ("작업", "Build Workshop", new[] { "<b>공방</b>", "근접 무기", "제작" })
    };

    public static async Task<string> Main()
    {
        passed = 0;
        var log = new StringBuilder();
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "build-tooltip-1111"); Directory.CreateDirectory(dir);
        Menu.Resume(); await Task.Delay(100);
        BuildScreen.Open(); await Task.Delay(200);
        Check(BuildScreen.IsOpen, "build screen open");
        var panel = (RectTransform)typeof(GameMenuController).GetField("tooltipPanel", Flags).GetValue(Menu);
        var tip = (Text)typeof(GameMenuController).GetField("tip", Flags).GetValue(Menu);
        foreach (var (tab, name, must) in Targets)
        {
            Find("Tab " + tab).onClick.Invoke(); await Task.Delay(700); // 툴팁 문자열 캐시(0.5초)와 화면 갱신을 기다린다.
            var button = Find(name);
            Check(button.gameObject.activeInHierarchy, name + " visible (not hidden)");
            Canvas.ForceUpdateCanvases();
            var hitPoint = ((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = hitPoint }, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, name + " pointer can reach button");
            var message = button.GetComponent<MenuTooltip>().Message;
            log.AppendLine("[" + name + "]").AppendLine(message).AppendLine();
            foreach (var word in must) Check(message.Contains(word), name + " contains '" + word + "'");
            Check(message.Contains("언제") && message.Contains("운영"), name + " has when/operation lines");
            Check(message.Contains("부족 · ") || message.Contains("지금 부족한 조건 없음"), name + " has missing-condition line");
            // 실제 표시: 포인터 진입과 같은 경로로 띄우고, 패널이 켜지고 글이 잘리지 않는지 본다.
            button.GetComponent<MenuTooltip>().OnPointerEnter(new PointerEventData(EventSystem.current));
            await Task.Delay(150);
            Check(panel.gameObject.activeInHierarchy && tip.text == message, name + " tooltip shown");
            var tipCanvas = panel.GetComponent<Canvas>();
            Check(tipCanvas != null && tipCanvas.overrideSorting && tipCanvas.sortingOrder > button.GetComponentInParent<Canvas>().sortingOrder, name + " tooltip draws above build window");
            var area = (RectTransform)panel.parent;
            Canvas.ForceUpdateCanvases();
            Check(tip.rectTransform.rect.height + 1 >= tip.preferredHeight && panel.rect.height <= area.rect.height - 24 + 1, name + " tooltip text not clipped");
            var corners = new Vector3[4]; panel.GetWorldCorners(corners);
            Check(corners[0].x >= -1 && corners[0].y >= -1 && corners[2].x <= Screen.width + 1 && corners[2].y <= Screen.height + 1, name + " tooltip within viewport");
            Rect ScreenRect(RectTransform rect)
            {
                var c = new Vector3[4]; rect.GetWorldCorners(c);
                return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
            }
            var helpRect = ScreenRect(panel);
            foreach (var control in button.transform.root.GetComponentsInChildren<Button>())
            {
                if (!control.gameObject.activeInHierarchy) continue;
                Check(!helpRect.Overlaps(ScreenRect((RectTransform)control.transform)), name + " does not cover " + control.name);
            }
            Check(BuildScreen.TooltipArea((RectTransform)button.transform, out var description), name + " uses description column");
            var column = ScreenRect(description);
            Check(Mathf.Abs(column.xMin-helpRect.xMin)<1 && Mathf.Abs(column.yMax-helpRect.yMax)<1, name + " inside right column");
            log.AppendLine($"  size {panel.rect.width:0}x{panel.rect.height:0} (text {tip.preferredHeight:0})").AppendLine();
            if (name == "Build Storage" || name == "Build ScienceLab" || name == "Build SoilWall" || name == "Build Farm" || name == "Build Toilet" || name == "Build AcidTower" || name == "Build Workshop")
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, name.Replace("Build ", "tooltip-") + "-" + resolution + ".png"));
                await Task.Delay(500);
            }
            button.GetComponent<MenuTooltip>().OnPointerExit(new PointerEventData(EventSystem.current));
        }
        // 대상이 아닌 건물은 기존 툴팁(이름: 비용) 그대로.
        Find("Tab 저장").onClick.Invoke(); await Task.Delay(700);
        var jar = Find("Build Jar").GetComponent<MenuTooltip>().Message;
        Check(jar.StartsWith("항아리: ") && !jar.Contains("언제"), "non-target keeps existing tooltip");
        // 현재 부족 조건이 캐시 이후 실제 작업표 변경을 반영하는지 확인한다.
        var team = CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead).ToArray();
        var haul = team.Select(c => c.WorkState.Priority(CommanderJobs.Hauling)).ToArray();
        try
        {
            foreach (var c in team) c.SetJobEnabled(CommanderJobs.Hauling, false);
            await Task.Delay(750);
            Check(Find("Build Storage").GetComponent<MenuTooltip>().Message.Contains("운반 작업을 켠 장수 없음"), "storage shortage reflects disabled hauling");
        }
        finally { for (int i = 0; i < team.Length; i++) team[i].SetJobPriority(CommanderJobs.Hauling, haul[i]); }
        // 잠긴 건물도 누를 수 있는 칸으로 남아 있고(숨김 없음) 잠금은 기존 규칙 그대로.
        Check(AntColony.Buildings.BuildingPlacementController.LockReason(BuildingKind.SoilWall) != null, "soil wall still research-locked");
        Check(AntColony.Buildings.BuildingPlacementController.LockReason(BuildingKind.AcidTower) == null, "acid tower still unlocked");
        Find("Tab 저장").onClick.Invoke(); await Task.Delay(700);
        var storageButton = Find("Build Storage");
        storageButton.GetComponent<MenuTooltip>().OnPointerEnter(new PointerEventData(EventSystem.current));
        storageButton.onClick.Invoke(); await Task.Delay(150);
        Check(BuildScreen.Picking && !panel.gameObject.activeSelf, "builder picker replaces help");
        storageButton.GetComponent<MenuTooltip>().OnPointerEnter(new PointerEventData(EventSystem.current));
        Check(!panel.gameObject.activeSelf, "grid hover never covers builder picker");
        BuildScreen.Back();
        BuildScreen.Back(); await Task.Delay(100);
        Check(!panel.gameObject.activeSelf, "closing build clears help");
        File.WriteAllText(Path.Combine(dir, "tooltips-" + resolution + ".txt"), log.ToString(), new UTF8Encoding(false));
        return "BuildTooltip1111 PASS " + passed + " checks · " + dir;
    }

    // One run_script call keeps the snapshot in the same dynamically compiled assembly.
    // Independent Prepare/Main/Finish calls cannot safely share static fields.
    public static async Task<string> Run()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        var asm = typeof(Editor).Assembly; var sizes = asm.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizes).GetProperty("instance").GetValue(null);
        var group = sizes.GetMethod("GetGroup").Invoke(singleton, new[] { Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone") });
        var viewType = asm.GetType("UnityEditor.GameView"); var view = EditorWindow.GetWindow(viewType);
        var prop = viewType.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        int old = (int)prop.GetValue(view);
        int slot = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null) + (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
        bool added = false; var result = new StringBuilder();
        Directory.CreateDirectory("Temp/build-tooltip-1111");
        try
        {
            await Prepare();
            foreach (var size in new[] { new Vector2Int(1440, 900), new Vector2Int(760, 600) })
            {
                resolution = size.x + "x" + size.y;
                var entry = Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"), Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), size.x, size.y, "Build tooltip check");
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { entry }); added = true;
                prop.SetValue(view, slot); view.Focus(); view.Repaint(); await Task.Delay(900);
                if (Screen.width != size.x || Screen.height != size.y) throw new Exception("resolution mismatch " + resolution);
                result.AppendLine(resolution + ": " + await Main());
                File.WriteAllText("Temp/build-tooltip-1111/checks.txt", result.ToString());
                prop.SetValue(view, old);
                group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { slot }); added = false;
            }
            return result.ToString();
        }
        catch (Exception e) { File.WriteAllText("Temp/build-tooltip-1111/checks.txt", result + "FAIL " + e); throw; }
        finally
        {
            if (BuildScreen.IsOpen) BuildScreen.Back();
            Time.timeScale = 0; UserSettings.Apply(settings, false); SaveStorage.RootOverride = root;
            prop.SetValue(view, old);
            if (added) group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { slot });
        }
    }

    public static string Finish()
    {
        if (original != null) UserSettings.Apply(original, false);
        SaveStorage.RootOverride = originalRoot;
        return "settings and save root restored";
    }
}
