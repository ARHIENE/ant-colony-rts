using System.Linq;
using System.Collections.Generic;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // V6: 하단 메뉴, 선택 카드와 접이식 지도. 게임 명령은 기존 컴포넌트를 사용한다.
    [DefaultExecutionOrder(600)]
    public sealed class HudResponsiveLayout : MonoBehaviour
    {
        private RectTransform canvas, console, toolbar, summary, mapToggle;
        private SelectionManager selection;
        private Text identity, mapLabel, brand, activityLabel, healthLabel, moodLabel;
        private RectTransform healthMeter, moodMeter;
        private RawImage selectedPortrait;
        private CommanderAnt portraitCommander;
        private bool mapOpen = true, targetDetail;
        private Component lastTarget;
        private RectTransform notifications, objective;
        public static float TopHeight { get; private set; } = 66;
        private static readonly List<Rect> occupied = new List<Rect>();
        private static readonly List<Rect> worldLabels = new List<Rect>();
        private static int labelFrame = -1;
        private readonly Vector3[] corners = new Vector3[4];

        public static bool OverlapsHud(Rect guiRect) => occupied.Any(r => r.Overlaps(guiRect));
        public static bool ReserveWorldLabel(Rect guiRect)
        {
            if (labelFrame != Time.frameCount) { worldLabels.Clear(); labelFrame = Time.frameCount; }
            if (OverlapsHud(guiRect) || worldLabels.Any(r => r.Overlaps(guiRect))) return false;
            worldLabels.Add(guiRect); return true;
        }
        // 선택 장수 한눈 요약(2026-11-01): 하는 일이 비면 "대기", 욕구 3종을 둘째 줄에. 30 이하/70 이상은 경고색.
        private static string CommanderStatusLine(AntColony.Units.CommanderAnt c)
        {
            var activity = CommanderOverhead.Activity(c);
            string Need(string name, float value) => value >= 70 ? $"<color=#e0705a>{name} {value:0}</color>" : $"{name} {value:0}";
            return $"{(activity.Length > 0 ? activity : "대기")} · 체력 {c.PersonalHealth:0}/{AntColony.Core.GameBalance.CommanderHealth:0} · 기분 {c.Mood:0}\n"
                + $"{Need("배고픔", 100f - c.Satiety)} · {Need("피로", c.Fatigue)} · {(c.Hygiene < 30 ? $"<color=#e0705a>위생 {c.Hygiene:0}</color>" : $"위생 {c.Hygiene:0}")}";
        }

        private void Reserve(RectTransform rect)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy) return;
            rect.GetWorldCorners(corners);
            var c = rect.GetComponentInParent<Canvas>();
            var camera = c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;
            var lo = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var hi = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            occupied.Add(Rect.MinMaxRect(lo.x - 4, Screen.height - hi.y - 4, hi.x + 4, Screen.height - lo.y + 4));
        }
        private void OnDestroy() => occupied.Clear();

        private void Start()
        {
            canvas = (RectTransform)transform;
            console = (RectTransform)transform.Find("CommandConsole");
            console.gameObject.AddComponent<SelectionPortraitStrip>();
            console.GetComponent<Image>().color = Color.clear;
            HudConsole.Right.GetComponent<Image>().color = Color.clear;
            selection = AntColony.Core.SceneObjects.Any<SelectionManager>();
            var overlays = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            notifications = overlays.FirstOrDefault(r => r.name == "Toasts");
            objective = overlays.FirstOrDefault(r => r.name == "Objective");
            toolbar = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(r => r.name == "MenuToolbar");
            var dock = MenuTheme.Rect("V6Dock", transform);
            dock.anchorMin = Vector2.zero; dock.anchorMax = new Vector2(1, 0); dock.pivot = new Vector2(.5f, 0);
            dock.SetAsFirstSibling();
            dock.sizeDelta = new Vector2(0, 36); dock.gameObject.AddComponent<Image>().color = MenuTheme.Hex(0x2c2b29);
            brand = MenuTheme.Text(transform.Find("ResourceBar"), "작은 정착지", 15);
            MenuLayout.Place(brand.rectTransform, 16, 16, 120, 30);
            brand.fontStyle = FontStyle.Bold;
            if (toolbar != null)
            {
                var layout = toolbar.GetComponent<HorizontalLayoutGroup>();
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
                // 메뉴 캔버스는 씬 전환 후에도 유지하므로 소유권을 유지한다.
                toolbar.anchorMin = toolbar.anchorMax = toolbar.pivot = Vector2.zero;
                toolbar.anchoredPosition = new Vector2(12, 9); toolbar.sizeDelta = new Vector2(900, 34);
                var build = toolbar.Find("V6 Build")?.GetComponent<Button>() ?? MenuTheme.Button(toolbar, "V6 Build", BuildScreen.Toggle); build.GetComponentInChildren<Text>().text = "건설";
                build.transform.SetSiblingIndex(1);
                var roster = toolbar.Find("V6 Roster")?.GetComponent<Button>() ?? MenuTheme.Button(toolbar, "V6 Roster", () => GameMenuController.Instance?.Roster()); roster.GetComponentInChildren<Text>().text = "장수";
                foreach (var b in toolbar.GetComponentsInChildren<Button>())
                {
                    var label = b.GetComponentInChildren<Text>(); label.fontSize = 13; label.resizeTextForBestFit = false;
                    var element = b.GetComponent<LayoutElement>(); element.preferredHeight = 34;
                    element.preferredWidth = Mathf.Max(52, b.GetComponentInChildren<Text>().preferredWidth + 20);
                }
            }
            summary = MenuTheme.Rect("V6SelectionSummary", console);
            summary.gameObject.AddComponent<Image>().color = MenuTheme.Hex(0x3d3a35);
            HudChrome.Round(summary.GetComponent<Image>(), 5, MenuTheme.Hex(0x797267));
            var portraitBacking = MenuLayout.Place(MenuTheme.Rect("Portrait Backing", summary), 8, 8, 64, 72);
            portraitBacking.gameObject.AddComponent<Image>().color = MenuTheme.Hex(0xd3cec3);
            HudChrome.Round(portraitBacking.GetComponent<Image>(), 2, MenuTheme.Hex(0x9a9283));
            var portraitRect = MenuLayout.Place(MenuTheme.Rect("Selected Commander Portrait", summary), 8, 8, 64, 72);
            selectedPortrait = portraitRect.gameObject.AddComponent<RawImage>();
            selectedPortrait.raycastTarget = false;
            identity = MenuTheme.Text(summary, "", 13); MenuTheme.Stretch(identity.rectTransform);
            identity.rectTransform.offsetMin = new Vector2(8, 0); identity.rectTransform.offsetMax = new Vector2(-28, 0);
            identity.horizontalOverflow = HorizontalWrapMode.Overflow; identity.verticalOverflow = VerticalWrapMode.Overflow;
            identity.alignment = TextAnchor.MiddleLeft;
            activityLabel = MenuLayout.Label(summary, "", 11, 84, 34, 250, 14, MenuTheme.Hex(0xcec5b7));
            healthLabel = MenuLayout.Label(summary, "", 11, 84, 55, 51, 18, MenuTheme.Hex(0xcec5b7));
            moodLabel = MenuLayout.Label(summary, "", 11, 209, 55, 46, 18, MenuTheme.Hex(0xcec5b7));
            healthMeter = MenuLayout.Meter(summary, 135, 63, 67, 3, 1, MenuTheme.Hex(0xa9ba8d));
            moodMeter = MenuLayout.Meter(summary, 255, 63, 72, 3, 1, MenuTheme.Hex(0xc1ad83));
            var details = MenuLayout.Button(summary, "V6 Details", "상세 ↗", 0, 0, 80, 32, () => {
                var commander = SelectedUnitPanel.FindSingleSelectedCommander(selection);
                if (commander != null) GameMenuController.Instance?.Details(commander);
                else if (HudOverview.Selected(selection).Length > 1) GameMenuController.Instance?.Roster();
                else targetDetail = !targetDetail;
            });
            var dr = (RectTransform)details.transform; dr.anchorMin = dr.anchorMax = dr.pivot = new Vector2(1, 1); dr.anchoredPosition = new Vector2(-28, -2); dr.sizeDelta = new Vector2(42,24);
            MenuTheme.Stretch(details.GetComponentInChildren<Text>().rectTransform); details.GetComponentInChildren<Text>().text = "상세";
            var clear = MenuLayout.Button(summary, "V6 Clear Selection", "×", 0, 0, 32, 32, () => { selection?.ClearSelection(); WorkTargetPanel.Clear(); targetDetail = false; });
            var cr = (RectTransform)clear.transform; cr.anchorMin = cr.anchorMax = cr.pivot = Vector2.one; cr.anchoredPosition = new Vector2(-2, -2); cr.sizeDelta = new Vector2(24,24);
            MenuTheme.Stretch(clear.GetComponentInChildren<Text>().rectTransform);
            mapToggle = MenuTheme.Rect("V6MapToggle", HudConsole.Left);
            mapToggle.gameObject.AddComponent<Image>().color = MenuTheme.Hex(0x302e2b);
            mapToggle.gameObject.AddComponent<Button>().onClick.AddListener(() => mapOpen = !mapOpen);
            mapLabel = MenuTheme.Text(mapToggle, "지도 접기", 12); MenuTheme.Stretch(mapLabel.rectTransform); mapLabel.alignment = TextAnchor.MiddleCenter;
            var legend = MenuTheme.Text(HudConsole.Left, "<color=#52634a>■</color> 낮음  <color=#c5bb8d>■</color> 높음  <color=#568cab>■</color> 물", 12);
            legend.name = "MapLegend"; legend.alignment = TextAnchor.MiddleCenter;
            // 지도는 선택 콘솔과 독립적으로 유지한다.
            HudConsole.Left.SetParent(transform, false);
            HudChrome.Round(HudConsole.Left.GetComponent<Image>(), 6, MenuTheme.Hex(0x797267));
            HudChrome.Clip(HudConsole.Left.GetComponent<Image>(), 6);
            HudConsole.Center.SetParent(transform, false);
            foreach (var root in new[] { console, HudConsole.Left, HudConsole.Right })
                foreach (var decoration in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Bronze" || t.name == "WingEdge" || t.name == "Rivet").ToArray()) decoration.gameObject.SetActive(false);
        }

        private static void Place(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
            r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h);
        }

        private void LateUpdate()
        {
            if (canvas == null) return;
            // These overlays can be created after the HUD's Start during scene loading.
            if (objective == null) objective = GameObject.Find("Objective")?.GetComponent<RectTransform>();
            if (notifications == null) notifications = GameObject.Find("Toasts")?.GetComponent<RectTransform>();
            var scaler = GetComponent<CanvasScaler>();
            MenuTheme.ConfigureScale(scaler);
            float width = canvas.rect.width;
            var top = (RectTransform)transform.Find("ResourceBar");
            TopHeight = 44; top.gameObject.SetActive(false);
            var roster = (RectTransform)transform.Find("RosterBar");
            // V6: 넓은 화면은 장수 바를 상단 가운데, 좁으면 왼쪽(브랜드 뒤)·둘째 줄.
            bool centered = width >= 1280;
            roster.anchorMin = roster.anchorMax = roster.pivot = centered ? new Vector2(.5f, 1) : new Vector2(0, 1);
            roster.anchoredPosition = centered ? new Vector2(-40, -4) : new Vector2(width > 760 ? 140 : 16, width < 960 ? -56 : -4);
            brand.gameObject.SetActive(false); roster.gameObject.SetActive(false);
            transform.Find("V6Dock")?.gameObject.SetActive(true);
            if (notifications != null) notifications.anchoredPosition = new Vector2(width <= 760 ? -12 : -18, -92);
            if (objective != null) objective.gameObject.SetActive(false);
            var clock = (RectTransform)transform.Find("HudClock"); clock.anchorMin = clock.anchorMax = clock.pivot = Vector2.zero; clock.anchoredPosition = new Vector2(width <= 760 ? 6 : 12, 0); clock.gameObject.SetActive(!BuildScreen.IsOpen);
            if (transform.Find("HudWave") is RectTransform wave) { wave.anchoredPosition = new Vector2(width <= 760 ? -12 : -18, -20); wave.sizeDelta = new Vector2(width <= 760 ? 210 : 220, 60); wave.gameObject.SetActive(!BuildScreen.IsOpen); }
            var materials = (RectTransform)transform.Find("MaterialsList"); materials.anchoredPosition = new Vector2(-266, -TopHeight - 10);
            var target = WorkTargetPanel.Target;
            if (target != lastTarget) { targetDetail = false; lastTarget = target; }
            bool hasTarget = target != null && target.gameObject.activeInHierarchy;
            var groups = transform.Find("ControlGroups"); if (groups != null) groups.gameObject.SetActive(false);
            var units = selection == null ? new SelectableObject[0] : selection.GetSelectedObjects().Where(s => s != null && s.IsSelected && s.isActiveAndEnabled).ToArray();
            bool visible = !GameMenuController.MenuOpen && !BuildScreen.IsOpen && (hasTarget || units.Length > 0);
            console.gameObject.SetActive(visible);
            float mapWidth = width <= 760 ? 160 : 190;
            float mapHeight = width <= 760 ? 150 : 168;
            float cardWidth = Mathf.Min(width <= 760 ? 320 : 344, width - mapWidth - 40);
            float left = width > 760 ? (width - cardWidth) * .5f : width - cardWidth - 16;
            bool multi = units.Length > 1;
            bool singlePeace = units.Length == 1 && !hasTarget && SelectedUnitPanel.FindSingleSelectedCommander(selection) is CommanderAnt peaceCommander && !peaceCommander.IsDeployed;
            float commandHeight = singlePeace || multi ? 36 : 96;
            float summaryHeight = width <= 760 ? 84 : 88;
            Place(console, left, 52, cardWidth, multi ? 126 : commandHeight + summaryHeight + 4);
            Place(summary, 0, commandHeight + 4, cardWidth, multi ? 86 : summaryHeight);
            Place(HudConsole.Right, 0, 0, singlePeace && width <= 760 ? cardWidth - 18 : cardWidth, commandHeight);
            var commander = SelectedUnitPanel.FindSingleSelectedCommander(selection);
            selectedPortrait.gameObject.SetActive(commander != null && !hasTarget);
            summary.Find("Portrait Backing").gameObject.SetActive(commander != null && !hasTarget);
            if (portraitCommander != commander)
            {
                portraitCommander = commander;
                selectedPortrait.texture = commander != null ? CharacterPortraits.Get(commander) : null;
            }
            bool showCommander = commander != null && !hasTarget;
            bool small = width <= 760;
            float textX = small ? 80 : 84, titleY = small ? 10 : 11;
            var title = hasTarget ? target is AntColony.Buildings.BuildingBase building && building.Data != null ? building.Data.displayName : target.name : commander != null ? commander.CommanderName : $"{units.Length}명 선택";
            identity.text = title; identity.font = MenuTheme.NumberFont; identity.fontStyle = FontStyle.Normal;
            identity.fontSize = showCommander ? (small ? 18 : 19) : 14;
            identity.color = MenuTheme.Hex(0xf2e7d0);
            MenuLayout.Place(identity.rectTransform, showCommander ? textX : 10, showCommander ? titleY : 2, cardWidth - (showCommander ? textX + 36 : 80), showCommander ? 22 : 24);
            foreach (var item in new[] {activityLabel.gameObject, healthLabel.gameObject, moodLabel.gameObject, healthMeter.gameObject, moodMeter.gameObject}) item.SetActive(showCommander);
            if (showCommander)
            {
                MenuLayout.Place((RectTransform)selectedPortrait.transform, 9, 8, small ? 60 : 64, small ? 68 : 72);
                MenuLayout.Place((RectTransform)summary.Find("Portrait Backing"), 9, 8, small ? 60 : 64, small ? 68 : 72);
                activityLabel.text = string.IsNullOrEmpty(CommanderOverhead.Activity(commander)) ? "대기" : CommanderOverhead.Activity(commander);
                MenuLayout.Place(activityLabel.rectTransform, textX, small ? 32 : 34, cardWidth - textX - 9, 14);
                healthLabel.text = $"체력  {commander.PersonalHealth:0}"; moodLabel.text = $"기분  {commander.Mood:0}";
                MenuLayout.Place(healthLabel.rectTransform, textX, small ? 54 : 55, 51, 18);
                MenuLayout.Place(moodLabel.rectTransform, textX + (small ? 115 : 125), small ? 54 : 55, 46, 18);
                float hpWidth = small ? 57 : 67, moodWidth = small ? 62 : 72;
                MenuLayout.Place(healthMeter, textX + 51, small ? 61 : 63, hpWidth, 3);
                MenuLayout.Place(moodMeter, textX + (small ? 161 : 171), small ? 61 : 63, moodWidth, 3);
                healthMeter.Find("Fill").GetComponent<RectTransform>().sizeDelta = new Vector2(hpWidth * Mathf.Clamp01(commander.PersonalHealth / AntColony.Core.GameBalance.CommanderHealth), 3);
                moodMeter.Find("Fill").GetComponent<RectTransform>().sizeDelta = new Vector2(moodWidth * Mathf.Clamp01(commander.Mood / 100f), 3);
            }
            // Keep details accessible by clicking the name, without adding a button absent from the approved HUD.
            var detailsRect = (RectTransform)summary.Find("V6 Details");
            detailsRect.GetComponent<Image>().color = Color.clear;
            var detailsButton = detailsRect.GetComponent<Button>(); detailsButton.transition = Selectable.Transition.None; detailsRect.GetComponent<Image>().canvasRenderer.SetColor(Color.white);
            detailsRect.GetComponentInChildren<Text>().text = "";
            MenuLayout.Place(detailsRect, showCommander ? textX : 10, showCommander ? titleY : 2, cardWidth - (showCommander ? textX + 38 : 80), 22);
            var clearRect = (RectTransform)summary.Find("V6 Clear Selection");
            MenuLayout.Place(clearRect, cardWidth - 29, 11, 18, 18);
            clearRect.GetComponent<Image>().color = Color.clear; clearRect.GetComponent<Button>().transition = Selectable.Transition.None;
            Place(HudConsole.Center, Mathf.Max(12, width - 620), 260, Mathf.Min(608, width - 24), 224);
            HudConsole.Center.gameObject.SetActive(visible && targetDetail);
            HudConsole.Left.gameObject.SetActive(!GameMenuController.MenuOpen && !BuildScreen.IsOpen);
            Place(HudConsole.Left, 12, 48, mapWidth, mapOpen ? mapHeight : 28);
            HudConsole.Left.GetComponent<Image>().color = MenuTheme.Hex(0x302e2b);
            Place(mapToggle, 0, 0, mapWidth, 28); mapLabel.text = "주변 지도"; mapLabel.fontSize = 11; mapLabel.alignment = TextAnchor.MiddleLeft; mapLabel.rectTransform.offsetMin = new Vector2(30, 0);
            var pin = HudChrome.Icon(mapToggle, "Map Pin", "MapPin", MenuTheme.Hex(0xcec5b7));
            MenuLayout.Place(pin.rectTransform, 10, 8, 12, 12);
            var chevron = HudChrome.Icon(mapToggle, "Map Chevron", mapOpen ? "ChevronDown" : "ChevronUp", MenuTheme.Hex(0xcec5b7));
            MenuLayout.Place(chevron.rectTransform, mapWidth - 24, 8, 12, 12);
            var map = (RectTransform)HudConsole.Left.Find("MinimapScreen");
            map.gameObject.SetActive(mapOpen); Place(map, 1, 28, mapWidth - 2, mapHeight - 29);
            var legend = (RectTransform)HudConsole.Left.Find("MapLegend"); legend.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++)
            {
                var filter = (RectTransform)HudConsole.Left.Find("Minimap Filter " + (Minimap.Filter)i);
                float filterWidth = (mapWidth - 16) / 3;
                filter.gameObject.SetActive(false); Place(filter, 4 + i * (filterWidth + 4), 2, filterWidth, 20);
            }
            var tabs = (RectTransform)transform.Find("DetailTabs"); if (tabs != null) tabs.anchoredPosition = new Vector2(left, 250);
            occupied.Clear();
            foreach (var rect in new[] { top, roster, clock, materials, notifications, objective, console, HudConsole.Left, HudConsole.Center, tabs, (RectTransform)transform.Find("HudWave"), (RectTransform)transform.Find("V6Dock") }) Reserve(rect);
        }
    }
}
