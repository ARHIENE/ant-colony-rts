using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // HUD v2 콘솔 위 좌하단 림월드식 상세 탭 창(개요·기분·특성·관계·장비·건강). 장수 1명 선택 시에만 보인다.
    public sealed class DetailTabs : MonoBehaviour
    {
        public static readonly string[] Tabs = { "개요", "기분", "특성", "관계", "장비", "건강" };
        private const float Width = 300f;
        private RectTransform root;
        private Text body;
        private readonly Button[] tabs = new Button[6];
        private SelectionManager selection;
        public int Tab { get; set; } = 1;
        public string Body => body != null ? body.text : "";
        public bool Visible => root != null && root.gameObject.activeSelf;

        public static DetailTabs Create(Transform canvas)
        {
            var root = MenuTheme.Panel(canvas, "DetailTabs", new Vector2(0, 0), new Vector2(Width, 160), new Vector2(8, HudConsole.CenterHeight + 8));
            // 판을 끄고 켜므로 컴포넌트는 늘 켜져 있는 캔버스에 둔다.
            var d = canvas.gameObject.AddComponent<DetailTabs>(); d.root = root;
            for (var i = 0; i < Tabs.Length; i++)
            {
                var index = i;
                var rect = MenuTheme.Rect("Tab " + Tabs[i], root);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.sizeDelta = new Vector2(Width / 6, 28); rect.anchoredPosition = new Vector2(i * Width / 6, -2);
                rect.gameObject.AddComponent<Image>();
                var button = rect.gameObject.AddComponent<Button>(); MenuTheme.StyleButton(button);
                button.onClick.AddListener(() => d.Tab = index);
                var text = MenuTheme.Text(rect, Tabs[i], 12); MenuTheme.Stretch(text.rectTransform); text.alignment = TextAnchor.MiddleCenter;
                d.tabs[i] = button;
            }
            d.body = MenuTheme.Text(root, "", 12);
            var b = d.body.rectTransform; b.anchorMin = Vector2.zero; b.anchorMax = Vector2.one;
            b.offsetMin = new Vector2(10, 8); b.offsetMax = new Vector2(-10, -34);
            d.body.alignment = TextAnchor.UpperLeft; d.body.supportRichText = true; d.body.lineSpacing = 1.1f;
            return d;
        }

        private void LateUpdate()
        {
            if (selection == null) selection = FindFirstObjectByType<SelectionManager>();
            var c = SelectedUnitPanel.FindSingleSelectedCommander(selection);
            var visible = c != null && !GameMenuController.BlocksInput && !BuildScreen.Picking;
            root.gameObject.SetActive(visible);
            if (!visible) return;
            for (var i = 0; i < tabs.Length; i++)
                tabs[i].GetComponentInChildren<Text>().color = i == Tab ? MenuTheme.Accent : MenuTheme.Muted;
            body.text = Describe(c, Tab);
        }

        private static string Row(string label, float value) =>
            $"{label}  <color=#{ColorUtility.ToHtmlStringRGB(value < 0 ? MenuTheme.DangerInk : MenuTheme.Hp)}>{value:+0;−0}</color>";

        public static string Describe(CommanderAnt c, int tab)
        {
            var p = c.PersonalState;
            switch (tab)
            {
                case 0:
                    return $"<b>{c.CommanderName}</b>  {c.WeaponLabel}\n하는 일: {(CommanderOverhead.Activity(c) is var a && a.Length > 0 ? a : "대기")}\n"
                        + $"체력 {Mathf.CeilToInt(c.PersonalHealth)}/{GameBalance.CommanderHealth:0} · 충성심 {c.Traits.Loyalty}\n"
                        + (c.IsDeployed ? $"병력 {Mathf.CeilToInt(c.TroopHealth)}/{c.CommandLimit}" : "평시 병력 0 (출전 시 징집소에서 편성)");
                case 1:
                    var rows = p.moodFactors.OrderBy(f => f.value).Take(6).Select(f => Row(f.reason, f.value));
                    return string.Join("\n", rows) + $"\n<b>기분 {c.Mood:0}</b>  <color=#968976>/ 붕괴 위험 {CommanderOverhead.MoodWarning:0} 이하</color>";
                case 2:
                    return c.Traits.values.Count == 0 ? "특성 없음" : string.Join("\n", c.Traits.values.Select(t => t.ToString()));
                case 3:
                    var names = CommanderRoster.Instance != null ? CommanderRoster.Instance.Commanders.ToDictionary(x => x.PersonalState.id, x => x.CommanderName) : null;
                    var rel = p.relations.Where(r => r.spouse || r.family || Mathf.Abs(r.value) >= 20).OrderByDescending(r => Mathf.Abs(r.value)).Take(6)
                        .Select(r => $"{(names != null && names.TryGetValue(r.otherId, out var n) ? n : "?")}  {(r.spouse ? "배우자" : r.family ? "가족" : r.value > 0 ? "친구" : "라이벌")} {r.value:+0;−0}");
                    return rel.Any() ? string.Join("\n", rel) : "눈에 띄는 관계 없음";
                case 4:
                    return p.equipment.Count == 0 ? "장비 없음" : string.Join("\n", p.equipment.Select(e => $"{e.slot}: {e.Label}"));
                default:
                    var health = p.injuries.Select(i => $"{i.part} · {i.severity}" + (i.severity == InjurySeverity.Permanent ? "" : $" ({Mathf.CeilToInt(i.remaining)}s)"));
                    var extra = p.infected ? "\n곰팡이 감염" : "";
                    return (p.injuries.Count == 0 ? "부상 없음" : string.Join("\n", health)) + extra;
            }
        }
    }
}
