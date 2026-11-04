using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// HUD v2 (2026-09-28): 낮밤 달력, 속도 1·2·3·5배, 장수 바, 상세 탭, 미니맵 필터, 커맨드 카드 배치.
public static class HudV2Checks
{
    static int checks;
    static void Check(bool ok, string text) { if (!ok) throw new Exception("FAIL " + text); checks++; }
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static async Task Frames(int n) { var f = Time.frameCount; for (var i = 0; i < 200 && Time.frameCount < f + n; i++) await Task.Delay(20); }
    static Button Find(string name) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include).FirstOrDefault(b => b.name == name);

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode"); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "HudV2-" + Guid.NewGuid().ToString("N"));
        var session = GameSession.Instance; float real = 0, game = 0;
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 260928, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            var menu = GameMenuController.Instance; menu.Resume(); Time.timeScale = 1; await Frames(2);
            real = session.PlaySeconds; game = session.GameSeconds;

            // 달력: 한 계절 = 하루 15분, 낮 10분 · 밤 5분.
            session.MarkStarted(real, 0); Check(!GameCalendar.IsNight && GameCalendar.SecondsUntilPhaseChange == 600, "day starts, night in 10 min");
            session.MarkStarted(real, 599); Check(!GameCalendar.IsNight, "still day at 9:59");
            session.MarkStarted(real, 600); Check(GameCalendar.IsNight && GameCalendar.SecondsUntilPhaseChange == 300, "night at 10 min");
            session.MarkStarted(real, 900); Check(!GameCalendar.IsNight && GameCalendar.Day == 1 && GameCalendar.CurrentSeason == Season.Summer, "next day = next season");
            session.MarkStarted(real, 610); await Frames(2);
            Check(HudClock.DayLabel.Contains("· 밤 · "), "clock label shows night and weather: " + HudClock.DayLabel);

            // 속도: 일시정지 + 1·2·3·5배. 버튼이 같은 동작.
            menu.SetSpeed(5); Check(Time.timeScale == 5, "five times speed");
            menu.SetSpeed(4); Check(Time.timeScale == 3 || Time.timeScale == 5, "only allowed speeds");
            Find("2x").onClick.Invoke(); Check(Time.timeScale == 2, "2x button");
            Find("Pause / Play").onClick.Invoke(); Check(Time.timeScale == 0, "pause button");
            Find("Pause / Play").onClick.Invoke(); Check(Time.timeScale == 2, "resume restores 2x");
            Check(KeyBindings.Get(GameAction.WorkSchedule) == UnityEngine.InputSystem.Key.T, "T opens work schedule by default");
            Check(!KeyBindings.CanBind(UnityEngine.InputSystem.Key.Space) && !KeyBindings.CanBind(UnityEngine.InputSystem.Key.F8), "space/F keys reserved");
            Time.timeScale = 1;

            // 장수 바: 12칸, 클릭 = 선택, 스크롤.
            var bar = Object.FindAnyObjectByType<RosterBar>();
            var list = RosterBar.Commanders;
            Check(bar != null && list.Length > 0, "roster bar with commanders");
            await Frames(2);
            var shown = Enumerable.Range(0, RosterBar.Visible).Count(i => Find("Roster " + i).gameObject.activeSelf);
            Check(shown == Mathf.Min(RosterBar.Visible, list.Length), $"roster shows {shown} of {list.Length}");
            bar.Click(0);
            var selection = Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();
            Check(SelectedUnitPanel.FindSingleSelectedCommander(selection) == list[0], "portrait click selects commander");
            if (list.Length > RosterBar.Visible) { bar.Scroll(RosterBar.Visible); Check(bar.Offset > 0, "roster scrolls"); bar.Scroll(-99); }

            // 상세 탭(HUD v3): 항상 띄우지 않고 초상 클릭으로 연다.
            await Frames(2);
            var tabs = Object.FindAnyObjectByType<DetailTabs>();
            Check(tabs != null && !tabs.Visible, "detail tabs hidden until portrait click");
            Find("Portrait").onClick.Invoke(); await Frames(2);
            Check(tabs.Visible, "portrait click opens detail tabs");
            Check(Find("Reward") != null && Find("Attack Research") != null && Find("Details") != null, "reward/research/details moved into detail window");
            list[0].PersonalState.AddMood("검사 노숙", -15, 60);
            tabs.Tab = 1; await Frames(2);
            Check(tabs.Body.Contains("검사 노숙") && tabs.Body.Contains("붕괴 위험"), "mood tab lists reasons: " + tabs.Body);
            for (var i = 0; i < DetailTabs.Tabs.Length; i++) { tabs.Tab = i; await Frames(1); Check(tabs.Body.Length > 0, "tab " + DetailTabs.Tabs[i] + " has text"); }

            // 커맨드 카드(HUD v3): 평시 = 우선·휴식·징집소·건설 2×2만, 출전 명령은 출전 중 판에.
            await Frames(2);
            var civilian = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include).First(r => r.name == "CivilianCommands");
            Check(civilian.gameObject.activeInHierarchy, "civilian card active");
            var civilianButtons = civilian.GetComponentsInChildren<Button>().Select(b => b.name).ToArray();
            Check(civilianButtons.SequenceEqual(new[] { "Priority Work", "Send To Rest", "Build", "Civilian Work Schedule", "Civilian Details", "Civilian Cycle Weapon", "Civilian Science", "Civilian Stop" }), "civilian 3x3: " + string.Join(",", civilianButtons));
            Check(!Find("Skill").gameObject.activeInHierarchy && !Find("Attack Move").gameObject.activeInHierarchy && !Find("Return To Post").gameObject.activeInHierarchy, "combat buttons hidden in peace");
            Check(civilian.GetComponentsInChildren<Text>().All(t => t.text.Length <= 3), "button names 2~3 letters");

            // 상단 메뉴(HUD v3): ≡ · 작업표 · 과학 · 월드맵 · 기록. 장수·외교는 다른 곳으로.
            var toolbar = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include).First(r => r.name == "MenuToolbar");
            var top = toolbar.GetComponentsInChildren<Button>().Select(b => b.GetComponentInChildren<Text>().text).ToArray();
            Check(top.SequenceEqual(new[] { "≡", "작업표", "과학", "월드맵", "기록" }), "top menu: " + string.Join(",", top));
            Find("Roster Count").onClick.Invoke(); await Frames(1);
            Check(GameMenuController.BlocksInput, "roster count opens commander management"); menu.Resume(); Time.timeScale = 1;
            Check(Find("Diplomacy") != null, "diplomacy inside world map");
            Find("Materials").onClick.Invoke(); await Frames(1);
            var materials = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include).First(r => r.name == "MaterialsList");
            Check(materials.gameObject.activeSelf && materials.GetComponentInChildren<Text>().text.Contains("특수"), "materials button opens list");
            var clockRect = Object.FindAnyObjectByType<HudClock>().GetComponent<RectTransform>();
            var materialCorners = new Vector3[4]; var clockCorners = new Vector3[4];
            materials.GetWorldCorners(materialCorners); clockRect.GetWorldCorners(clockCorners);
            // HUD v4: 재료 목록은 달력 왼쪽에 붙는다.
            Check(!Rect.MinMaxRect(materialCorners[0].x, materialCorners[0].y, materialCorners[2].x, materialCorners[2].y).Overlaps(Rect.MinMaxRect(clockCorners[0].x, clockCorners[0].y, clockCorners[2].x, clockCorners[2].y)), "materials list clears clock controls");
            Find("Materials").onClick.Invoke();

            // 미니맵 필터: 끄면 점이 줄어든다.
            Minimap.Filters[0] = Minimap.Filters[1] = Minimap.Filters[2] = true;
            await Task.Delay(400); var all = Minimap.DotCount;
            Find("Minimap Filter Resources").onClick.Invoke(); await Task.Delay(400);
            Check(all > 0 && Minimap.DotCount < all, $"resource filter hides dots {all}->{Minimap.DotCount}");
            Find("Minimap Filter Resources").onClick.Invoke();

            // 인구 = 현재 일반개미 수.
            var ants = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude).First(r => r.name == "Population").GetComponentInChildren<Text>();
            Check(ants.text.Contains("인구") && ants.text.Contains(">" + ColonyPopulation.Instance.Total + "</color></b>"), "population shown (Phase 4 total, sentiment color): " + ants.text);
            return "PASS " + checks + " HUD v3 checks";
        }
        finally { session.MarkStarted(real, game); SaveStorage.RootOverride = root; Time.timeScale = 0; Minimap.Filters[0] = Minimap.Filters[1] = Minimap.Filters[2] = true; }
    }
}
