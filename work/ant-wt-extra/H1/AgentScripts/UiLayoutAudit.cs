using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 2026-10-24 UI 배치 점검: 주요 화면마다 (1) 잘리거나 칸 밖으로 넘치는 글씨, (2) 다른 UI에 가려 실제 클릭이 안 닿는 버튼을 찾는다.
public static class UiLayoutAudit
{
    static async Task Frames(int n) { var f = Time.frameCount; for (var i = 0; i < 200 && Time.frameCount < f + n; i++) await Task.Delay(20); }
    static string Path(Transform t) { var s = t.name; for (var i = 0; i < 3 && t.parent != null; i++) { t = t.parent; s = t.name + "/" + s; } return s; }

    static void Audit(string screen, StringBuilder report, HashSet<string> seen)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var t in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
        {
            if (!t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text) || t.color.a < .05f) continue;
            var r = t.rectTransform.rect; if (r.width < 4 || r.height < 4) continue;
            var gen = t.GetGenerationSettings(r.size);
            var h = t.cachedTextGeneratorForLayout.GetPreferredHeight(t.text, gen) / t.pixelsPerUnit;
            var w = t.cachedTextGeneratorForLayout.GetPreferredWidth(t.text, t.GetGenerationSettings(Vector2.zero)) / t.pixelsPerUnit;
            string issue = null;
            if (t.verticalOverflow == VerticalWrapMode.Truncate && h > r.height + 2) issue = $"세로 잘림 {h:0}>{r.height:0}";
            else if (t.verticalOverflow == VerticalWrapMode.Overflow && h > r.height + 6 && t.GetComponent<LayoutElement>() == null && t.GetComponentInParent<LayoutGroup>() == null) issue = $"세로 넘침 {h:0}>{r.height:0}";
            else if (t.horizontalOverflow == HorizontalWrapMode.Overflow && w > r.width + 4) issue = $"가로 넘침 {w:0}>{r.width:0}";
            if (issue == null) continue;
            var key = Path(t.transform) + issue.Split(' ')[0];
            if (seen.Add(key)) report.AppendLine($"TEXT [{screen}] {Path(t.transform)} :: {issue} :: \"{t.text.Replace("\n", "⏎").Substring(0, Math.Min(40, t.text.Length))}\"");
        }
        var es = EventSystem.current; if (es == null) return;
        foreach (var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            if (!b.isActiveAndEnabled || !b.interactable) continue;
            var rt = (RectTransform)b.transform; var center = rt.TransformPoint(rt.rect.center);
            if (center.x < 0 || center.y < 0 || center.x > Screen.width || center.y > Screen.height) { if (seen.Add("off" + Path(b.transform))) report.AppendLine($"OFFSCREEN [{screen}] {Path(b.transform)}"); continue; }
            var hits = new List<RaycastResult>(); es.RaycastAll(new PointerEventData(es) { position = center }, hits);
            var top = hits.FirstOrDefault().gameObject;
            if (top == null || top.transform.IsChildOf(b.transform)) continue;
            if (seen.Add("blk" + Path(b.transform))) report.AppendLine($"BLOCKED [{screen}] {Path(b.transform)} <- {Path(top.transform)}");
        }
    }

    public static async Task<string> Main()
    {
        var menu = GameMenuController.Instance; var c = CommanderRoster.Instance.Commanders.FirstOrDefault();
        var map = Object.FindFirstObjectByType<WorldMapPanel>();
        var screens = new (string name, Action open)[]
        {
            ("hud", () => { }), ("pause", menu.Pause), ("roster", menu.Roster), ("population", menu.Population),
            ("science", menu.Science), ("diplomacy", menu.Diplomacy), ("work", menu.WorkSchedule), ("events", menu.EventLog),
            ("guide", menu.Guide), ("civic", menu.CivicServices), ("crafting", menu.GroundCrafting), ("conscription", menu.OpenConscription),
            ("health", () => menu.ShowHealth(c)), ("equipment", () => menu.ShowEquipment(c)), ("supplies", menu.CarriedSupplies),
            ("logistics", () => menu.Management("물류·식품")), ("colony", () => menu.Management("의료·관리")), ("production", () => menu.Management("생산·자원")),
            ("build", BuildScreen.Toggle), ("worldmap", () => map.Toggle()),
        };
        var report = new StringBuilder(); var seen = new HashSet<string>();
        foreach (var (name, open) in screens)
        {
            try { open(); } catch (Exception e) { report.AppendLine($"OPEN-ERROR [{name}] {e.GetType().Name}: {e.Message}"); }
            await Frames(4);
            Audit(name, report, seen);
            if (name == "worldmap") map.Toggle(); else if (name == "build") BuildScreen.Toggle(); else menu.Resume();
            await Frames(2);
        }
        System.IO.File.WriteAllText("Temp/ui-layout-audit.txt", report.ToString());
        var lines = report.ToString().Split('\n').Where(l => l.Length > 0).ToArray();
        return $"LAYOUT issues {lines.Length} (text {lines.Count(l => l.StartsWith("TEXT"))}, blocked {lines.Count(l => l.StartsWith("BLOCKED"))}, offscreen {lines.Count(l => l.StartsWith("OFF"))})";
    }
}
