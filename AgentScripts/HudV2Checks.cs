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
            Check(HudClock.DayLabel.EndsWith("· 밤"), "clock label shows night: " + HudClock.DayLabel);

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

            // 상세 탭: 선택 장수 기분 사유 목록.
            await Frames(2);
            var tabs = Object.FindAnyObjectByType<DetailTabs>();
            Check(tabs != null && tabs.Visible, "detail tabs visible for one commander");
            list[0].PersonalState.AddMood("검사 노숙", -15, 60);
            tabs.Tab = 1; await Frames(2);
            Check(tabs.Body.Contains("검사 노숙") && tabs.Body.Contains("붕괴 위험"), "mood tab lists reasons: " + tabs.Body);
            for (var i = 0; i < DetailTabs.Tabs.Length; i++) { tabs.Tab = i; await Frames(1); Check(tabs.Body.Length > 0, "tab " + DetailTabs.Tabs[i] + " has text"); }

            // 커맨드 카드: 평시 배치 / 출전 전용 숨김.
            await Frames(2);
            Check(Find("Skill").gameObject.activeSelf && !Find("Skill").interactable, "Q locked in peace");
            Check(!Find("Attack Move").gameObject.activeSelf && !Find("Stop").gameObject.activeSelf && !Find("Return To Post").gameObject.activeSelf, "combat buttons hidden in peace");
            Check(Find("Send To Treatment").gameObject.activeSelf && Find("Priority Work").gameObject.activeSelf && Find("Reward").gameObject.activeSelf, "peace buttons visible");

            // 미니맵 필터: 끄면 점이 줄어든다.
            Minimap.Filters[0] = Minimap.Filters[1] = Minimap.Filters[2] = true;
            await Task.Delay(400); var all = Minimap.DotCount;
            Find("Minimap Filter Resources").onClick.Invoke(); await Task.Delay(400);
            Check(all > 0 && Minimap.DotCount < all, $"resource filter hides dots {all}->{Minimap.DotCount}");
            Find("Minimap Filter Resources").onClick.Invoke();

            // 인구수 = 현재 일반개미 수.
            var ants = Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude).FirstOrDefault(t => t.text.StartsWith("<color=#968976>개미</color>"));
            Check(ants != null && ants.text.Contains("<b>" + AntPool.Instance.Total + "</b>"), "ant count shown");
            return "PASS " + checks + " HUD v2 checks";
        }
        finally { session.MarkStarted(real, game); SaveStorage.RootOverride = root; Time.timeScale = 0; Minimap.Filters[0] = Minimap.Filters[1] = Minimap.Filters[2] = true; }
    }
}
