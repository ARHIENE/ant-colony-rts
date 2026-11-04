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
        private Text identity, mapLabel, brand;
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
            selection = AntColony.Core.SceneObjects.Any<SelectionManager>();
            var overlays = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            notifications = overlays.FirstOrDefault(r => r.name == "Toasts");
            objective = overlays.FirstOrDefault(r => r.name == "Objective");
            toolbar = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(r => r.name == "MenuToolbar");
            var dock = MenuTheme.Rect("V6Dock", transform);
            dock.anchorMin = Vector2.zero; dock.anchorMax = new Vector2(1, 0); dock.pivot = new Vector2(.5f, 0);
            dock.sizeDelta = new Vector2(0, 52); dock.gameObject.AddComponent<Image>().color = MenuTheme.Plate;
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
            identity = MenuTheme.Text(summary, "", 16); MenuTheme.Stretch(identity.rectTransform);
            identity.rectTransform.offsetMin = new Vector2(12, 0); identity.rectTransform.offsetMax = new Vector2(-130, 0);
            identity.alignment = TextAnchor.MiddleLeft;
            var details = MenuLayout.Button(summary, "V6 Details", "상세 ↗", 0, 0, 80, 32, () => {
                var commander = SelectedUnitPanel.FindSingleSelectedCommander(selection);
                if (commander != null) GameMenuController.Instance?.Details(commander);
                else targetDetail = !targetDetail;
            });
            var dr = (RectTransform)details.transform; dr.anchorMin = dr.anchorMax = dr.pivot = new Vector2(1, 1); dr.anchoredPosition = new Vector2(-46, -10);
            var clear = MenuLayout.Button(summary, "V6 Clear Selection", "×", 0, 0, 32, 32, () => { selection?.ClearSelection(); WorkTargetPanel.Clear(); targetDetail = false; });
            var cr = (RectTransform)clear.transform; cr.anchorMin = cr.anchorMax = cr.pivot = Vector2.one; cr.anchoredPosition = new Vector2(-8, -10);
            mapToggle = MenuTheme.Rect("V6MapToggle", HudConsole.Left);
            mapToggle.gameObject.AddComponent<Image>().color = MenuTheme.Plate2;
            mapToggle.gameObject.AddComponent<Button>().onClick.AddListener(() => mapOpen = !mapOpen);
            mapLabel = MenuTheme.Text(mapToggle, "지도 접기", 12); MenuTheme.Stretch(mapLabel.rectTransform); mapLabel.alignment = TextAnchor.MiddleCenter;
            var legend = MenuTheme.Text(HudConsole.Left, "<color=#52634a>■</color> 낮음  <color=#c5bb8d>■</color> 높음  <color=#568cab>■</color> 물", 12);
            legend.name = "MapLegend"; legend.alignment = TextAnchor.MiddleCenter;
            // 지도는 선택 콘솔과 독립적으로 유지한다.
            HudConsole.Left.SetParent(transform, false);
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
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = MenuTheme.UiScale;
            float width = canvas.rect.width;
            var top = (RectTransform)transform.Find("ResourceBar");
            TopHeight = width < 960 ? 116 : 66; top.sizeDelta = new Vector2(0, TopHeight);
            var roster = (RectTransform)transform.Find("RosterBar");
            // V6: 넓은 화면은 장수 바를 상단 가운데, 좁으면 왼쪽(브랜드 뒤)·둘째 줄.
            bool centered = width >= 1280;
            roster.anchorMin = roster.anchorMax = roster.pivot = centered ? new Vector2(.5f, 1) : new Vector2(0, 1);
            roster.anchoredPosition = centered ? new Vector2(-40, -4) : new Vector2(width >= 1100 ? 140 : 16, width < 960 ? -56 : -4);
            brand.gameObject.SetActive(width >= 1100);
            if (notifications != null) notifications.anchoredPosition = new Vector2(-10, -TopHeight - 112);
            if (objective != null) objective.anchoredPosition = new Vector2(10, -TopHeight - 10);
            var clock = (RectTransform)transform.Find("HudClock"); clock.anchoredPosition = new Vector2(-10, -TopHeight - 10);
            var materials = (RectTransform)transform.Find("MaterialsList"); materials.anchoredPosition = new Vector2(-266, -TopHeight - 10);
            var target = WorkTargetPanel.Target;
            if (target != lastTarget) { targetDetail = false; lastTarget = target; }
            bool hasTarget = target != null && target.gameObject.activeInHierarchy;
            var units = selection == null ? new SelectableObject[0] : selection.GetSelectedObjects().Where(s => s != null && s.IsSelected && s.isActiveAndEnabled).ToArray();
            bool visible = !BuildScreen.IsOpen && (hasTarget || units.Length > 0);
            console.gameObject.SetActive(visible);
            float mapWidth = width < 1100 ? 224 : 272, mapSide = mapWidth - 12;
            float left = mapWidth + 32, cardWidth = Mathf.Min(790, width - left - 16);
            Place(console, left, 66, cardWidth, 132);
            Place(summary, 0, 76, cardWidth, 56);
            Place(HudConsole.Right, 0, 0, cardWidth, 76);
            var commander = SelectedUnitPanel.FindSingleSelectedCommander(selection);
            var title = hasTarget ? target is AntColony.Buildings.BuildingBase building && building.Data != null ? building.Data.displayName : target.name : commander != null ? commander.CommanderName : $"{units.Length}명 선택";
            var status = hasTarget ? "대상 관리" : commander != null ? CommanderOverhead.Activity(commander) + $" · 체력 {commander.PersonalHealth:0}/{AntColony.Core.GameBalance.CommanderHealth:0} · 기분 {commander.Mood:0}" : "공통으로 가능한 명령";
            identity.text = $"<b>{title}</b>\n<size=12>{status}</size>";
            Place(HudConsole.Center, left, 220, Mathf.Min(808, width - left - 16), 224);
            HudConsole.Center.gameObject.SetActive(visible && targetDetail);
            Place(HudConsole.Left, 16, 66, mapWidth, mapOpen ? mapSide + 80 : 28);
            Place(mapToggle, 0, mapOpen ? mapSide + 52 : 0, mapWidth, 28); mapLabel.text = mapOpen ? "지도 접기" : "지도 펼치기";
            var map = (RectTransform)HudConsole.Left.Find("MinimapScreen");
            map.gameObject.SetActive(mapOpen); Place(map, 6, 48, mapSide, mapSide);
            var legend = (RectTransform)HudConsole.Left.Find("MapLegend"); legend.gameObject.SetActive(mapOpen); Place(legend, 4, 27, mapWidth - 8, 18);
            for (int i = 0; i < 3; i++)
            {
                var filter = (RectTransform)HudConsole.Left.Find("Minimap Filter " + (Minimap.Filter)i);
                float filterWidth = (mapWidth - 16) / 3;
                filter.gameObject.SetActive(mapOpen); Place(filter, 4 + i * (filterWidth + 4), 3, filterWidth, 22);
            }
            var tabs = (RectTransform)transform.Find("DetailTabs"); if (tabs != null) tabs.anchoredPosition = new Vector2(left, 210);
            occupied.Clear();
            foreach (var rect in new[] { top, roster, clock, materials, notifications, objective, console, HudConsole.Left, HudConsole.Center, tabs, (RectTransform)transform.Find("V6Dock") }) Reserve(rect);
        }
    }
}
