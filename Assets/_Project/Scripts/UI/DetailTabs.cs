using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 콘솔 위 좌하단 림월드식 상세 탭 창(개요·기분·특성·관계·장비·건강).
    // HUD v3: 항상 띄우지 않고 선택 정보의 초상을 누르면 열린다. 포상·연구소 강화·장수 관리도 여기서.
    public sealed class DetailTabs : MonoBehaviour
    {
        public static readonly string[] Tabs = { "개요", "기분", "특성", "관계", "장비", "건강", "이야기" };
        private const float Width = 300f;
        private RectTransform root;
        private Text body;
        private readonly Button[] tabs = new Button[7];
        private SelectionManager selection;
        private CommanderAnt opened;
        private CommanderAnt described;
        private int describedTab = -1;
        private float refresh;
        public int Tab { get; set; } = 1;
        public string Body => body != null ? body.text : "";
        public bool Visible => root != null && root.gameObject.activeSelf;

        // 초상 클릭: 같은 장수면 닫고, 아니면 그 장수로 연다. 선택이 바뀌면 저절로 닫힌다.
        public void Toggle(CommanderAnt c) => opened = opened == c ? null : c;

        public static void Reward(CommanderAnt c)
        {
            if (c != null && !c.TryReward()) ToastManager.Show("본거지의 명령 가능한 장수만 포상할 수 있습니다. 식량 30 · 재사용 대기 300초(게임 2일).");
        }

        public static DetailTabs Create(Transform canvas)
        {
            var root = MenuTheme.Panel(canvas, "DetailTabs", new Vector2(0, 0), new Vector2(Width, 196), new Vector2(8, HudConsole.CenterHeight + 8));
            // 판을 끄고 켜므로 컴포넌트는 늘 켜져 있는 캔버스에 둔다.
            var d = canvas.gameObject.AddComponent<DetailTabs>(); d.root = root;
            for (var i = 0; i < Tabs.Length; i++)
            {
                var index = i;
                var rect = MenuTheme.Rect("Tab " + Tabs[i], root);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.sizeDelta = new Vector2(Width / Tabs.Length, 28); rect.anchoredPosition = new Vector2(i * Width / Tabs.Length, -2);
                rect.gameObject.AddComponent<Image>();
                var button = rect.gameObject.AddComponent<Button>(); MenuTheme.StyleButton(button);
                button.onClick.AddListener(() => d.Tab = index);
                var text = MenuTheme.Text(rect, Tabs[i], 12); MenuTheme.Stretch(text.rectTransform); text.alignment = TextAnchor.MiddleCenter;
                d.tabs[i] = button;
            }
            d.body = MenuTheme.Text(root, "", 12);
            var b = d.body.rectTransform; b.anchorMin = Vector2.zero; b.anchorMax = Vector2.one;
            b.offsetMin = new Vector2(10, 40); b.offsetMax = new Vector2(-10, -34);
            d.body.alignment = TextAnchor.UpperLeft; d.body.supportRichText = true; d.body.lineSpacing = 1.1f;
            HUDController Hud() => AntColony.Core.SceneObjects.Any<HUDController>();
            d.Action(root, 0, "Reward", "포상", () => Reward(d.opened), () => "식량 30 소비 · 기분 보통 +8, 탐욕/야심 +15, 금욕 +3. 효과·재사용 대기 각 300초(게임 2일). 본거지에서 명령 가능한 장수만 포상.");
            d.Action(root, 1, "Attack Research", "공격 연구", () => Hud()?.TryLabResearch(true),
                () => Hud()?.LabResearchLabel(d.opened, true) + "\n무기와 같은 종류의 연구소에서 이 장수의 공격을 올립니다.");
            d.Action(root, 2, "Armor Research", "방어 연구", () => Hud()?.TryLabResearch(false),
                () => Hud()?.LabResearchLabel(d.opened, false) + "\n무기와 같은 종류의 연구소에서 이 장수의 방어를 올립니다.");
            d.Action(root, 3, "Details", "관리", () => { if (d.opened != null) GameMenuController.Instance?.Details(d.opened); },
                () => "장수 관리: 기술 13종·열정·장비 전체.");
            return d;
        }

        // 아래쪽 버튼 4칸. 도움말은 장수에 따라 바뀌므로 열 때마다 갱신한다.
        private readonly System.Collections.Generic.List<(MenuTooltip tip, System.Func<string> help)> helps = new System.Collections.Generic.List<(MenuTooltip, System.Func<string>)>();
        private void Action(RectTransform root, int index, string name, string label, System.Action action, System.Func<string> help)
        {
            var rect = MenuTheme.Rect(name, root);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(67, 26); rect.anchoredPosition = new Vector2(10 + index * 71, 8);
            rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>(); MenuTheme.StyleButton(button);
            button.onClick.AddListener(() => action());
            var text = MenuTheme.Text(rect, label, 12); MenuTheme.Stretch(text.rectTransform); text.alignment = TextAnchor.MiddleCenter;
            helps.Add((rect.gameObject.AddComponent<MenuTooltip>(), help));
        }

        private void LateUpdate()
        {
            if (selection == null) selection = AntColony.Core.SceneObjects.Any<SelectionManager>();
            var c = SelectedUnitPanel.FindSingleSelectedCommander(selection);
            if (c != opened) opened = null;
            var visible = c != null && opened == c && !GameMenuController.BlocksInput && !BuildScreen.Picking;
            root.gameObject.SetActive(visible);
            if (!visible) return;
            root.sizeDelta = new Vector2(Width, Tab == 6 ? 282 : 196);
            for (var i = 0; i < tabs.Length; i++)
                tabs[i].GetComponentInChildren<Text>().color = i == Tab ? MenuTheme.Accent : MenuTheme.Muted;
            if (c != described || Tab != describedTab || Time.unscaledTime >= refresh)
            {
                described = c; describedTab = Tab; refresh = Time.unscaledTime + .5f;
                body.text = Describe(c, Tab);
                foreach (var (tip, help) in helps) tip.Message = help();
            }
        }

        private static string Row(string label, float value) =>
            $"{label}  <color=#{ColorUtility.ToHtmlStringRGB(value < 0 ? MenuTheme.DangerInk : MenuTheme.Hp)}>{value:+0;−0}</color>";

        public static string Describe(CommanderAnt c, int tab)
        {
            var p = c.PersonalState;
            switch (tab)
            {
                case 0:
                    return $"<b>{c.CommanderName}</b>  {c.WeaponName}\n하는 일: {(CommanderOverhead.Activity(c) is var a && a.Length > 0 ? a : "대기")}\n"
                        + $"체력 {Mathf.CeilToInt(c.PersonalHealth)}/{GameBalance.CommanderHealth:0} · 포만 {c.Satiety:0} · 피로 {c.Fatigue:0} · 오락 {c.Joy:0} · 위생 {c.Hygiene:0}\n"
                        + (c.IsDeployed ? $"병력 {Mathf.CeilToInt(c.TroopHealth)}/{c.CommandLimit}" : "평시 병력 0 (출전 시 징집소에서 편성)");
                case 1:
                    var rows = p.moodFactors.OrderBy(f => f.value).Take(6).Select(f => Row(SpeechBubbles.MoodReason(c, f.reason), f.value));
                    return string.Join("\n", rows) + $"\n<b>기분 {c.Mood:0}</b>  <color=#b1b3a7>/ 붕괴 위험 {CommanderOverhead.MoodWarning:0} 이하</color>";
                case 2:
                    return c.Traits.values.Count == 0 ? "특성 없음" : string.Join("\n", c.Traits.values.Select(CommanderTraits.DisplayName));
                case 3:
                    var names = CommanderRoster.Instance != null ? CommanderRoster.Instance.Commanders.ToDictionary(x => x.PersonalState.id, x => x.CommanderName) : null;
                    var rel = p.relations.Where(r => r.spouse || r.family || Mathf.Abs(r.value) >= 20).OrderByDescending(r => Mathf.Abs(r.value)).Take(6)
                        .Select(r => $"{(names != null && names.TryGetValue(r.otherId, out var n) ? n : "?")}  {(r.spouse ? "배우자" : r.family ? "가족" : r.value > 0 ? "친구" : "라이벌")} {r.value:+0;−0}");
                    return rel.Any() ? string.Join("\n", rel) : "눈에 띄는 관계 없음";
                case 4:
                    return p.equipment.Count == 0 ? "장비 없음" : string.Join("\n", p.equipment.Select(e => $"{PersonalDisplayNames.Slot(e.slot)}: {e.Label}"));
                case 5:
                    return GameMenuController.HealthLines(p.health);
                default:
                    return "<b>이야기 · 최근 주요 사건</b>\n" + CampaignHistory.StoryLines(CampaignHistory.Instance?.Data, p.id, c.CommanderName);
            }
        }
    }
}
