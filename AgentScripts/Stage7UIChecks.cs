using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// 4번 7단계 UI 마감: 토스트 규칙, 첫 등장 힌트, 붕괴 경고·오라·필터, 엔딩 기록, 3D 행성·원정 박스.
public static class Stage7UIChecks
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static int checks;
    static void Check(bool ok, string text) { if (!ok) throw new Exception("FAIL " + text); checks++; }
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static async Task Frames(int n) { var f = Time.frameCount; for (var i = 0; i < 200 && Time.frameCount < f + n; i++) await Task.Delay(20); }
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode"); var root = SaveStorage.RootOverride;
        var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Stage7UI-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 260927, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            var s = UserSettings.Current.Clone(); s.firstHints = true; s.shownHints.Clear(); UserSettings.Apply(s, false);

            // 토스트: 같은 알림 합치기, 5개 초과는 +N건, 클릭 = 닫기, 위기 고정.
            var toastManager = Object.FindAnyObjectByType<ToastManager>();
            void ClearToasts() { var list = typeof(ToastManager).GetField("toasts", Private).GetValue(toastManager); list.GetType().GetMethod("Clear").Invoke(list, null); }
            ClearToasts();
            ToastManager.Show("테스트 A"); ToastManager.Show("테스트 A"); ToastManager.Show("테스트 A");
            Check(ToastManager.Count == 1 && ToastManager.VisibleLines.Any(l => l == "테스트 A ×3"), "same toast merges x3");
            for (var i = 0; i < 6; i++) ToastManager.Show("테스트 B" + i, ToastKind.Warning);
            Check(ToastManager.Count == 7, "seven toasts tracked");
            var lines = ToastManager.VisibleLines;
            Check(lines.Count(l => !l.StartsWith("+")) <= ToastManager.MaxVisible && lines.Last().StartsWith("+") && lines.Last().EndsWith("건"), "max five visible plus overflow line");
            ToastManager.SetCrisis("test", "위기 테스트");
            Check(ToastManager.VisibleLines[0] == "위기 테스트", "crisis pinned first");
            ToastManager.Dismiss(0); Check(!ToastManager.VisibleLines.Contains("위기 테스트"), "crisis dismissed by click");
            ToastManager.SetCrisis("test", "위기 테스트 2"); Check(ToastManager.VisibleLines[0] == "위기 테스트 2", "changed crisis reappears");
            ToastManager.SetCrisis("test", null);
            var before = ToastManager.Count; ToastManager.Dismiss(0); Check(ToastManager.Count == before - 1, "click closes toast");
            typeof(ToastManager).GetField("hovered", Private).SetValue(toastManager, true);
            var remaining = ToastManager.Count; await Frames(3);
            Check(ToastManager.Paused && ToastManager.Count == remaining, "hover pauses timers");
            typeof(ToastManager).GetField("hovered", Private).SetValue(toastManager, false);

            // 첫 등장 힌트: 1회, 설정으로 끄기.
            ClearToasts(); FirstHints.Trigger("contact");
            Check(FirstHints.Seen("contact") && ToastManager.VisibleLines.Any(l => l.Contains("첫 문명 접촉")), "hint shown once and remembered");
            var count = ToastManager.Count; FirstHints.Trigger("contact"); Check(ToastManager.Count == count, "hint not repeated");
            s = UserSettings.Current.Clone(); s.firstHints = false; UserSettings.Apply(s, false);
            FirstHints.Trigger("prisoner"); Check(!FirstHints.Seen("prisoner"), "hints can be turned off");
            s = UserSettings.Current.Clone(); s.firstHints = true; UserSettings.Apply(s, false);
            CampaignHistory.Record("포로", "테스트", "적 장수 포획"); Check(FirstHints.Seen("prisoner"), "history record triggers prisoner hint");

            // 붕괴 경고: 위험 구간 진입 시 경고 토스트 + 힌트, 붕괴 중 오라, 장수 관리 필터.
            var c = CommanderRoster.Instance.Commanders[0];
            var watch = Object.FindAnyObjectByType<MoodWatch>();
            ClearToasts(); c.PersonalState.AddMood("Test grief", -200, 60); watch.Scan();
            Check(ToastManager.VisibleLines.Any(l => l.Contains("기분 경고: " + c.CommanderName)), "mood warning toast");
            Check(FirstHints.Seen("collapse"), "collapse hint");
            Check((bool)typeof(GameMenuController).GetMethod("InFilter", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { c, 4 }), "mood warning roster filter includes commander");
            c.StartMentalBreak(MentalBreak.Idle); watch.Scan();
            Check(MoodWatch.HasAura(c), "break aura shown");
            typeof(CommanderAnt).GetMethod("EndMentalBreak", Private)?.Invoke(c, null);
            c.PersonalState.moodFactors.RemoveAll(f => f.reason == "Test grief"); watch.Scan();
            if (c.PersonalState.mentalBreak == MentalBreak.None) Check(!MoodWatch.HasAura(c), "aura cleared after break");
            Check(!(bool)typeof(GameMenuController).GetMethod("InFilter", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { c, 4 }) || c.Mood <= CommanderOverhead.MoodWarning, "filter excludes recovered commander");
            GameMenuController.Instance.Roster(); await Frames(2);
            Check(GameMenuController.Instance.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b => b.name == "Filter 기분 경고"), "roster has mood warning filter");
            GameMenuController.Instance.Resume();

            // 엔딩 기록: 누적 기록 항목이 모두 있다(점수·등급 없음).
            var research = CampaignResearch.Instance;
            CampaignHistory.Record("사망", "테스트 장수", "전사"); CampaignHistory.Record("보스 처치", "MiniBird", "");
            var record = GameMenuController.EndingRecord(research);
            foreach (var key in new[] { "기간", "실제 플레이", "Food", "Soil", "Special", "일반개미", "탑승", "남겨진 장수", "사망", "포로", "합류", "떠난 장수", "처치한 보스", "거점", "전쟁·동맹", "주요 이벤트" })
                Check(record.Any(l => l.Contains(key)), "ending record has " + key);
            Check(record.Any(l => l.Contains("테스트 장수(전사)")) && record.Any(l => l.Contains("MiniBird")), "ending record lists deaths and bosses");
            Check(!record.Any(l => l.Contains("점수") || l.Contains("등급")), "no score or grade");

            // 월드맵 3D 행성: 초점 이동과 뒷면 숨김, 원정 박스 펼침.
            if (WorldMapManager.Instance.Transports.Count == 0) WorldMapManager.Instance.CreateTransport(false, c.Position + Vector3.right * 3);
            await Frames(3);
            var ui = Object.FindAnyObjectByType<WorldMapPanel>();
            if (!ui.IsOpen) ui.Toggle();
            var sites = WorldMapManager.Instance.Sites;
            var far = sites.OrderBy(x => x.MapPosition.x).First(); var near = sites.OrderBy(x => x.MapPosition.x).Last();
            ui.Planet.Focus(near.MapPosition); ui.Planet.SnapToTarget();
            Check(ui.Planet.Project(near.MapPosition, out var p) && Mathf.Abs(p.x - ui.Planet.Rect.sizeDelta.x / 2) < 20 && Mathf.Abs(p.y - ui.Planet.Rect.sizeDelta.y / 2) < 20, "focused site at planet center");
            if (Mathf.Abs(far.MapPosition.x - near.MapPosition.x) > .6f) Check(!ui.Planet.Project(far.MapPosition, out _), "opposite site hidden on back side");
            var drag = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { delta = new Vector2(100, 0) };
            ui.Planet.Project(near.MapPosition, out var before2); ui.Planet.OnDrag(drag); ui.Planet.SnapToTarget(); ui.Planet.Project(near.MapPosition, out var after2);
            Check(after2.x > before2.x + 10, "dragging right turns planet right");
            var ships = WorldMapManager.Instance.Transports;
            Check(ships.Count > 0 && ships[0] != null, "transport exists");
            {
                typeof(WorldMapPanel).GetMethod("Update", Private).Invoke(ui, null);
                ui.OpenExpedition(ships[0]);
                typeof(WorldMapPanel).GetMethod("Update", Private).Invoke(ui, null);
                Check(ui.ExpandedExpedition == ships[0] && ui.PanelRect.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b => b.name == "Box Return"), "expedition box expands with actions");
                ui.OpenExpedition(ships[0]); Check(ui.ExpandedExpedition == null, "second click collapses");
            }
            ui.Toggle();
            return "PASS Stage7UIChecks " + checks;
        }
        finally { UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; Time.timeScale = 0; }
    }
}
