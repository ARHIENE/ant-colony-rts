using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 2026-10-24 사용자 보고 "버튼은 눌리는데 상호작용이 안 됨" 진단: 화면에 보이는 버튼을 하나씩 눌러
// (1) 예외·오류 로그가 나는 버튼, (2) 눌러도 UI·시간·선택이 전혀 안 바뀌는 버튼을 찾는다. 새로 열린 창 안 버튼도 한 단계 더 누른다.
// 게임을 끝내거나 저장을 지우는 버튼은 누르지 않는다. 결과 전체는 Temp/ui-audit.txt.
public static class UiButtonAudit
{
    static readonly string[] Skip = { "Quit", "Exit", "종료", "New Game", "새 게임", "Load", "불러오기", "Delete", "삭제", "Main Menu", "메인", "Restart", "다시 시작", "Save", "저장" };
    static readonly List<string> errors = new List<string>();

    static string Path(Transform t) { var s = t.name; while (t.parent != null && t.parent.GetComponent<Canvas>() == null) { t = t.parent; s = t.name + "/" + s; } return s; }
    static string Label(Button b) => b.GetComponentInChildren<Text>(true)?.text?.Replace("\n", " ") ?? "";

    static string Snapshot()
    {
        var sb = new StringBuilder();
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            foreach (var t in c.GetComponentsInChildren<Transform>(false)) sb.Append(t.GetEntityId().ToString()).Append(',');
        foreach (var t in Object.FindObjectsByType<Text>(FindObjectsSortMode.None)) if (t.isActiveAndEnabled) sb.Append(t.text).Append('|');
        foreach (var t in Object.FindObjectsByType<Toggle>(FindObjectsSortMode.None)) sb.Append(t.isOn);
        sb.Append(Time.timeScale).Append(UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform.position.ToString() : "");
        return sb.ToString();
    }

    static string ActiveSet()
    {
        var sb = new StringBuilder();
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isActiveAndEnabled) foreach (var g in c.GetComponentsInChildren<Graphic>(false)) if (g.isActiveAndEnabled && g.color.a > .01f) sb.Append(g.GetEntityId().ToString()).Append(',');
        return sb.ToString();
    }

    static List<Button> Visible() => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
        .Where(b => b.isActiveAndEnabled && b.interactable && !Skip.Any(k => b.name.Contains(k) || Label(b).Contains(k))).ToList();

    static async Task Frames(int n) { var f = Time.frameCount; for (var i = 0; i < 100 && Time.frameCount < f + n; i++) await Task.Delay(20); }

    public static async Task<string> Main()
    {
        if (!Application.isPlaying) return "FAIL Play mode";
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        // 실제 저장 슬롯을 건드리지 않게 임시 저장 경로에서 새 게임(작은 맵)으로 시작한다.
        var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = System.IO.Path.Combine(Application.temporaryCachePath, "UiAudit-" + Guid.NewGuid().ToString("N"));
        SaveSystem.NewGame(new NewGameOptions { seed = 241024, mapSize = MapSize.Small });
        until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        await Task.Delay(1500); Time.timeScale = 1;
        Application.LogCallback log = (msg, trace, type) => { if (type == LogType.Exception || type == LogType.Error) errors.Add(msg.Split('\n')[0] + " @ " + string.Join(" < ", (trace ?? "").Split('\n').Take(3).Select(l => l.Trim()))); };
        Application.logMessageReceived += log;
        var report = new StringBuilder(); int pressed = 0, broken = 0, silent = 0, flicker = 0;
        var done = new HashSet<EntityId>();
        try
        {
            var queue = new Queue<(Button b, int depth)>(Visible().Select(b => (b, 0)));
            while (queue.Count > 0 && pressed < 400)
            {
                var (button, depth) = queue.Dequeue();
                if (button == null || !button.isActiveAndEnabled || !button.interactable || !done.Add(button.GetEntityId())) continue;
                var before = Snapshot(); var shownBefore = new HashSet<EntityId>(Visible().Select(b => b.GetEntityId()));
                var name = Path(button.transform) + " [" + Label(button) + "]";
                errors.Clear(); pressed++;
                try { button.onClick.Invoke(); } catch (Exception e) { var b = e.GetBaseException(); errors.Add(b.GetType().Name + ": " + b.Message + " @ " + string.Join(" < ", (b.StackTrace ?? "").Split('\n').Take(3).Select(l => l.Trim()))); }
                // 깜빡임: 누른 뒤 15프레임 동안 보이는 UI 집합이 몇 번 바뀌는지(한 번 열리고 끝나면 1~2, 깜빡이면 그 이상).
                var flips = 0; var last = ActiveSet();
                for (var f = 0; f < 15; f++) { await Frames(1); var now = ActiveSet(); if (now != last) flips++; last = now; }
                var after = Snapshot();
                if (flips > 2) { flicker++; report.AppendLine($"FLICK  {name} :: {flips}회 변화/15프레임"); }
                if (errors.Count > 0) { broken++; report.AppendLine("ERROR  " + name + " :: " + string.Join(" / ", errors.Distinct().Take(3))); }
                else if (after == before) { silent++; report.AppendLine("NOCHG  " + name); }
                // 새로 나타난 버튼(열린 창)은 한 단계 더 눌러 본다.
                if (depth < 1) foreach (var b in Visible().Where(b => !shownBefore.Contains(b.GetEntityId()))) queue.Enqueue((b, depth + 1));
                // 열린 창·바뀐 상태를 되돌리기 어렵기 때문에 ESC 대신 새로 켜진 최상위 창만 끈다.
                foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    foreach (Transform w in c.transform)
                        if (w.gameObject.activeSelf && !before.Contains(w.GetEntityId().ToString() + ",") && depth == 0 && w.GetComponentsInChildren<Button>().Length > 0) w.gameObject.SetActive(false);
                Time.timeScale = 1;
            }
        }
        finally { Application.logMessageReceived -= log; SaveStorage.RootOverride = root; }
        File.WriteAllText("Temp/ui-audit.txt", report.ToString());
        return $"AUDIT pressed {pressed} error {broken} nochange {silent} flicker {flicker}\n" + string.Join("\n", report.ToString().Split('\n').Take(60));
    }
}
