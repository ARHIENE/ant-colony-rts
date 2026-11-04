using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // 디자인 「월드맵」: 상단 바 아래 전체 화면(16:9 기준 높이 770에 맞춤). 왼쪽 원정대 · 가운데 행성 지도 + 거점 정보 · 오른쪽 과학/범례.
    // 검사가 찾는 이름(WorldMap, Annex, Abandon, Station Selected, Start/Stop Auto, SettlementDefenseStatus, 마커=거점 제목)은 유지한다.
    public class WorldMapPanel : MonoBehaviour
    {
        private const float MarkerSize = 22, Dock = 60; // Dock: 하단 HUD 메뉴바(V6Dock 52) 위 여백
        private GameObject panel;
        private UnityEngine.UI.Text status, siteInfo, scienceStatus, feedback, mapTitle, routeStatus, defenseStatus, worldNotice;
        private UnityEngine.UI.Button annex, abandon, startRoute, stopRoute;
        private readonly List<UnityEngine.UI.Button> markers = new List<UnityEngine.UI.Button>();
        private RectTransform mapRoot, selectionRing, overlay, expeditions, science, legend, siteRect;
        private float uiScale = 1;
        private ExpeditionSite selectedSite;
        private ExpeditionTransport selectedShip;
        private bool seenUnlock = true, cameraWasEnabled;
        private UnityEngine.UI.Text departurePlan;
        private ExpeditionTransport warnedShip;
        private ExpeditionSite warnedSite;
        private WorldPlanet planetView;
        private RectTransform expeditionList, expeditionArea, expeditionControls;
        private readonly Dictionary<ExpeditionTransport, UnityEngine.UI.Text> boxes = new Dictionary<ExpeditionTransport, UnityEngine.UI.Text>();
        private ExpeditionTransport expanded;
        private readonly List<ExpeditionSite> routeStops = new List<ExpeditionSite>();
        public bool IsOpen => panel != null && panel.activeSelf;
        // 월드 위 OnGUI 표시(장수 머리 위·채집 금지)가 전체 화면 월드맵 위에 그려지지 않게 한다.
        public static bool AnyOpen { get; private set; }
        public RectTransform PanelRect => panel != null ? (RectTransform)panel.transform : null;

        private void Start()
        {
            worldNotice = L.Label(transform, "", 14, 300, 70, 840, 28);
            worldNotice.name = "SettlementNotice";
            worldNotice.raycastTarget = false;

            // 2026-10-24 림월드식: 지구가 전체 화면, 패널은 모서리에 작게. HUD 캔버스는 고정 픽셀이라 overlay를 화면 높이에 맞춰 확대한다.
            var rect = MenuTheme.Rect("WorldMapPanel", transform);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = new Vector2(0, -40);
            rect.gameObject.AddComponent<UnityEngine.UI.Image>().color = MenuTheme.Hex(0x0f0c09);
            panel = rect.gameObject;
            planetView = WorldPlanet.Create(rect);
            overlay = MenuTheme.Rect("Overlay", rect); overlay.anchorMin = overlay.anchorMax = overlay.pivot = new Vector2(.5f, .5f);

            // 가운데(전체): 지구 위 마커·제목. 이미지가 없어 클릭이 통과하므로 드래그·휠은 지구가 받는다.
            var map = MenuTheme.Rect("WorldMap", overlay);
            map.anchorMin = Vector2.zero; map.anchorMax = Vector2.one; map.pivot = new Vector2(0, 1); map.offsetMin = map.offsetMax = Vector2.zero;
            mapRoot = map;
            mapTitle = L.Label(map, "", 14, 0, 0, 520, 44, MenuTheme.TextColor, TextAnchor.UpperCenter);
            Corner(mapTitle.rectTransform, new Vector2(.5f, 1), 0, 10);
            selectionRing = L.Box(map, "SelectionRing", 0, 0, MarkerSize + 8, MarkerSize + 8, MenuTheme.Accent);
            selectionRing.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            selectionRing.gameObject.SetActive(false);
            BuildMarkers();

            // 오른쪽 아래 버튼줄. 검사 경로(WorldMap/Start Auto·Stop Auto)를 지키려고 자동 수송 버튼은 WorldMap 아래에 둔다.
            var bar = Corner(MenuTheme.Rect("Map Actions", overlay), new Vector2(1, 0), 12, Dock, 618, 34);
            L.Button(bar, "Home", "정착지로", 0, 0, 90, 34, () => { WorldMapManager.Instance.ViewSite(null); Toggle(); }, "홈 둥지 화면으로 돌아갑니다.");
            L.Button(bar, "Diplomacy", "외교", 368, 0, 76, 34, () => GameMenuController.Instance?.Diplomacy(), "접촉한 문명 · 협정 · 전쟁 · 거래");
            L.Button(bar, "Science Toggle", "과학·수송", 450, 0, 96, 34, () => science.gameObject.SetActive(!science.gameObject.activeSelf));
            L.Button(bar, "Legend Toggle", "범례", 552, 0, 66, 34, () => legend.gameObject.SetActive(!legend.gameObject.activeSelf));
            startRoute = L.Button(map, "Start Auto", "자동 수송 시작", 0, 0, 130, 34, () =>
            {
                // 경유지를 지정했으면 그 순서로, 아니면 선택 거점 하나로 시작한다.
                var route = routeStops.Count > 0 ? (IReadOnlyList<ExpeditionSite>)routeStops.ToArray() : new[] { selectedSite };
                var ok = selectedShip != null && selectedShip.Route != null && selectedShip.Route.TryStart(route);
                Result(ok, "자동 수송을 시작했습니다. 장수가 경유지를 차례로 돌며 모아서 돌아옵니다.");
                if (ok) routeStops.Clear();
            });
            stopRoute = L.Button(map, "Stop Auto", "자동 수송 중지", 0, 0, 130, 34, () => {
                selectedShip?.Route?.Stop();
                feedback.text = "자동 수송을 멈췄습니다. 진행 중인 이동·작업은 계속되며, 나가 있다면 직접 귀환시키세요.";
            });
            Corner((RectTransform)startRoute.transform, new Vector2(1, 0), 12 + 618 - 96 - 130, Dock);
            Corner((RectTransform)stopRoute.transform, new Vector2(1, 0), 12 + 618 - 232 - 130, Dock);

            // 왼쪽 위: 원정대(버튼으로 접기). 원정 1건 = 박스, 클릭 = 지구를 그 목적지로 돌리고 펼침.
            var toggle = L.Button(overlay, "Expeditions Toggle", "원정대 ▾", 0, 0, 120, 32, () => expeditions.gameObject.SetActive(!expeditions.gameObject.activeSelf));
            Corner((RectTransform)toggle.transform, new Vector2(0, 1), 12, 10);
            expeditions = Corner(L.Plate(overlay, "Expeditions", 0, 0, 360, 500), new Vector2(0, 1), 12, 48);
            var e = expeditions;
            // 위: 제목+다음 수송 수단, 목록(내용 높이만큼, LayoutExpeditions), 그 바로 아래 버튼 묶음. 목록 칸이 고정 116이라 위가 비던 문제(2026-10-25).
            L.Label(e, "<b>원정대</b>", 15, 12, 10, 160, 34);
            Btn(e, "Next Transport", "다음 수송 수단", 184, 10, NextShip);
            expeditionList = L.List(e, 12, 50, 336, 0, 4);
            expeditionArea = (RectTransform)expeditionList.parent.parent.parent;
            status = L.Label(e, "", 13, 12, 50, 336, 40, MenuTheme.Muted, TextAnchor.UpperLeft);
            status.gameObject.SetActive(false);
            var k = expeditionControls = L.Place(MenuTheme.Rect("Expedition Controls", e), 0, 50, 360, 320);
            Btn(k, "Board Selected", "선택 장수 탑승", 12, 0, () => ChangeCrew(false));
            Btn(k, "Station Selected", "선택 장수 주둔", 184, 0, () => ChangeCrew(true));
            Btn(k, "Unload Crew", "승무원 내리기", 12, 38, () => Result(selectedShip != null && selectedShip.TryUnloadCrew(), "하차 지시를 내렸습니다."));
            Btn(k, "Return Home", "귀환", 184, 38, () => Result(selectedShip != null && selectedShip.TryReturn(), "철수를 시작합니다. 탑승·적재가 끝나면 출발합니다."));
            Btn(k, "Collect Equipment / Blueprint", "전리품·설계도 싣기", 12, 76,
                () => Result(selectedShip != null && BossRewardTransfer.TryCollect(selectedShip), "전리품을 실었습니다. 귀환하면 사용할 수 있습니다."));
            Btn(k, "Load Expedition Food", "권장 보급 싣기", 184, 76, () => Result(selectedShip != null && selectedShip.TryLoadFood(
                Mathf.Min(selectedShip.ReturnCargoSpace, selectedShip.RecommendedFood(selectedSite) - selectedShip.AvailableFood)), "보급을 실었습니다."));
            Btn(k, "Walking Formation", "도보 편성", 12, 114, () => {
                Toggle(); GameMenuController.Instance.WalkingFormation(party => { selectedShip = party; Toggle(); });
            });
            Btn(k, "Talent Investigation", "인재 조사 파견", 184, 114, () => { Toggle(); GameMenuController.Instance.ShowInvestigation(selectedSite, selectedShip); });
            departurePlan = L.Label(k, "", 12, 12, 154, 336, 74, MenuTheme.Warning, TextAnchor.UpperLeft);
            L.Line(k, 12, 232, 336);
            routeStatus = L.Label(k, "", 12, 12, 238, 336, 40, MenuTheme.Muted, TextAnchor.UpperLeft);
            Btn(k, "Add Route Stop", "경유지 추가", 12, 282, () =>
            {
                if (selectedSite == null || selectedSite.Disposition != ConquestDisposition.Annexed || routeStops.Contains(selectedSite)
                    || routeStops.Count >= TransportRoute.MaxStops) feedback.text = $"편입 거점을 중복 없이 최대 {TransportRoute.MaxStops}곳까지 지정할 수 있습니다.";
                else routeStops.Add(selectedSite);
            });
            Btn(k, "Clear Route Stops", "경유지 비우기", 184, 282, routeStops.Clear);

            // 왼쪽 아래: 선택 거점 정보와 행동. 지구·이름표를 덜 가리게 다른 모서리 패널과 같은 360폭, 선택이 없으면 숨긴다(Update).
            var site = siteRect = Corner(L.Plate(overlay, "SiteInfo", 0, 0, 360, 248), new Vector2(0, 0), 12, Dock);
            siteInfo = L.Label(site, "", 13, 14, 10, 332, 100, align: TextAnchor.UpperLeft);
            Btn(site, "Depart", "원정 보내기", 14, 114, Depart);
            Btn(site, "View Battlefield", "전장 보기", 182, 114, () => {
                var world = WorldMapManager.Instance;
                if (world != null && selectedSite != null && world.ViewSite(selectedSite)) Toggle();
                else feedback.text = "수송 수단이 도착해야 전장에 들어갈 수 있습니다.";
            });
            annex = Btn(site, "Annex", "편입", 14, 152, () => ResolveConquest(ConquestDisposition.Annexed));
            abandon = Btn(site, "Abandon", "포기", 182, 152, () => ResolveConquest(ConquestDisposition.Abandoned));
            defenseStatus = L.Label(site, "", 12, 14, 190, 332, 20, MenuTheme.DangerInk);
            defenseStatus.name = "SettlementDefenseStatus";
            feedback = L.Label(site, "", 12, 14, 212, 332, 32, MenuTheme.Accent, TextAnchor.UpperLeft);

            // 오른쪽 아래: 과학·수송 수단 건조, 범례(버튼으로 펼침, 기본 숨김).
            science = Corner(L.Plate(overlay, "Science", 0, 0, 360, 200), new Vector2(1, 0), 12, Dock + 44);
            L.Label(science, "<b>과학 · 수송 수단</b>", 15, 12, 8, 336, 26);
            scienceStatus = L.Label(science, "", 12, 12, 36, 336, 84, MenuTheme.Muted, TextAnchor.UpperLeft);
            Btn(science, "Build Science Lab", "과학 연구소 건설", 12, 120, () => {
                if (AntColony.Core.SceneObjects.Any<BuildingPlacementController>().BeginScienceLabPlacement()) Toggle();
                else feedback.text = "본거지, 시민 60, 낚시, 2단계 병영, 선택한 건설 장수가 필요합니다.";
            });
            Btn(science, "Science / Researchers", "과학 연구 · 연구원", 184, 120, () => { Toggle(); GameMenuController.Instance.Science(); });
            Btn(science, "Build Vehicle 50F/60 재료", "차량 건조 식50 재료60", 12, 158, () => ScienceAction(false, true));
            Btn(science, "Build Aircraft 80F/100 재료", "비행기 건조 식80 재료100", 184, 158, () => ScienceAction(true, true));
            science.gameObject.SetActive(false);
            legend = Corner(L.Plate(overlay, "Legend", 0, 0, 360, 210), new Vector2(1, 0), 12, Dock + 44);
            L.Label(legend, "<b>범례</b>  C 정착지 · B 보스 · R 자원 · T 교역", 13, 12, 8, 336, 24);
            L.Label(legend, WorldMapSymbols.Legend + "\n" + WorldMapSymbols.SelectionLegend,
                12, 12, 40, 336, 164, MenuTheme.TextColor, TextAnchor.UpperLeft);
            legend.gameObject.SetActive(false);
            var close = L.Button(overlay, "Close", "닫기   M", 0, 0, 110, 34, Toggle);
            Corner((RectTransform)close.transform, new Vector2(1, 1), 12, 10);
            panel.SetActive(false);
        }

        // 모서리 기준 배치: corner(0~1) 쪽에서 안쪽으로 (dx, dy) 띄운다. w·h를 주면 크기도 정한다.
        private static RectTransform Corner(RectTransform r, Vector2 corner, float dx, float dy, float w = -1, float h = -1)
        {
            r.anchorMin = r.anchorMax = r.pivot = corner;
            if (w >= 0) r.sizeDelta = new Vector2(w, h);
            r.anchoredPosition = new Vector2(corner.x < .25f ? dx : corner.x > .75f ? -dx : 0, corner.y < .25f ? dy : corner.y > .75f ? -dy : 0);
            return r;
        }

        // overlay를 패널 크기에 맞춘다: 높이 820 기준으로 확대(1~3배), 크기는 패널/배율 → 모서리 배치가 화면 모서리에 붙는다.
        private void FitOverlay()
        {
            var size = ((RectTransform)panel.transform).rect.size;
            uiScale = Mathf.Clamp(size.y / 820f, 1f, 3f);
            overlay.localScale = Vector3.one * uiScale; overlay.sizeDelta = size / uiScale; overlay.anchoredPosition = Vector2.zero;
        }

        private UnityEngine.UI.Button Btn(Transform parent, string name, string label, float x, float y, System.Action action, float w = 164) =>
            L.Button(parent, name, label, x, y, w, 34, action, null, false, 13);

        private void Update()
        {
            var world = WorldMapManager.Instance;
            if (world == null || panel == null) return;
            worldNotice.text = world.SettlementNotice;
            BuildMarkers();
            if (world.Unlocked && !seenUnlock) { seenUnlock = true; if (!IsOpen) Toggle(); }
            if (!IsOpen) return;
            FitOverlay();
            defenseStatus.text = selectedSite != null && selectedSite.Defense != null ? selectedSite.Defense.Status : "";
            if (selectedShip == null) NextShip();
            departurePlan.text = selectedShip == null ? "도보 편성: 장수 1명·병력 0명부터 가능합니다." :
                $"왕복 {selectedShip.TravelSecondsTo(selectedSite) * 2 / AntColony.Core.GameCalendar.SecondsPerDay:0.0}일 · 권장 식량 {selectedShip.RecommendedFood(selectedSite)} / 보유 {selectedShip.AvailableFood}\n"
                + $"왕복 + 현지 1일 + 예비 1일 · 병력 포함\n귀환 화물 여유 {selectedShip.ReturnCargoSpace}"
                + (selectedShip.LeavesHomeUnattended ? "\n경고: 본거지 대응 장수 없음" : "");
            var route = selectedShip != null ? selectedShip.Route : null;
            startRoute.interactable = route != null && !route.IsRunning && selectedShip.State == ExpeditionState.Home
                && selectedSite != null && selectedSite.Disposition == ConquestDisposition.Annexed;
            stopRoute.interactable = route != null && route.IsRunning;
            routeStatus.text = route != null && route.IsRunning
                ? $"<b>자동 수송</b>: {string.Join(" → ", route.Stops.Select(s => s.Title))}\n{route.Status} · 집에서 {TransportRoute.IntervalSeconds:0}초 뒤 반복"
                : routeStops.Count > 0 ? $"<b>경유지</b>: {string.Join(" → ", routeStops.Select(s => s.Title))}\n일꾼 장수를 태우고 자동 수송 시작."
                : $"<b>자동 수송</b>: {(route != null ? route.Status : "꺼짐")}\n편입 거점을 고르거나 경유지(최대 {TransportRoute.MaxStops}곳)를 추가한 뒤 시작.";
            var lab = FindLab();
            scienceStatus.text = $"과학 연구소: {(lab != null ? lab.Busy ? $"작업 중 {lab.Remaining:0}초" : "대기" : "없음")}\n"
                + $"차량: {(world.VehicleResearched ? "연구 완료" : "잠김")} · 비행기: {(world.AircraftResearched ? "연구 완료" : "잠김")}\n"
                + "연구소: 식량 100 / 재료 100 / 시민 8. 연구원을 배정하고, 차량은 2단계·비행기는 3단계로 강화. 건조 10초.";
            mapTitle.text = world.Unlocked ? $"<b>월드맵</b>  거점 {world.Sites.Count} · 드래그 회전 · 휠 확대" : "<b>월드맵 잠김</b>\n첫 차량이나 비행기를 건조하세요.";
            for (var i = 0; i < markers.Count; i++)
            {
                var site = world.Sites[i];
                var local = Vector2.zero;
                var visible = world.Unlocked && WorldMapManager.SiteRevealed(site) && planetView.Project(site.MapPosition, out local);
                markers[i].gameObject.SetActive(visible);
                if (visible) ((RectTransform)markers[i].transform).anchoredPosition = new Vector2(local.x / uiScale - MarkerSize / 2, -(local.y / uiScale - MarkerSize / 2));
                var color = site.Defense != null && (site.Defense.UnderAttack || site.Disposition == ConquestDisposition.Lost)
                    ? new Color(1f, .25f, .15f) : site.Disposition == ConquestDisposition.Annexed ? new Color(.3f, .85f, .5f)
                    : site.Disposition == ConquestDisposition.Abandoned ? Color.gray : DiplomacyManager.Instance?.Faction(site)?.color ?? site.Color;
                markers[i].GetComponent<WorldMapMarker>().Refresh(site, new Color(color.r * .65f, color.g * .65f, color.b * .65f, 1));
                if (site == selectedSite)
                {
                    selectionRing.gameObject.SetActive(visible);
                    selectionRing.anchoredPosition = ((RectTransform)markers[i].transform).anchoredPosition + new Vector2(-4, 4);
                }
            }
            if (selectedSite == null) selectionRing.gameObject.SetActive(false);
            annex.interactable = abandon.interactable = selectedSite != null && selectedSite.CanResolveConquest;
            siteInfo.text = SiteText(selectedSite);
            // 과학·수송 버튼도 feedback에 결과를 쓰므로 선택이 없어도 안내가 있으면 보인다.
            siteRect.gameObject.SetActive(selectedSite != null || feedback.text != "");
            RefreshExpeditions(world);
            status.gameObject.SetActive(world.Transports.Count == 0);
            status.text = selectedShip == null ? "수송 수단이 없습니다. 연구하고 건조하세요."
                : $"<b>{selectedShip.name}</b>  {StateName(selectedShip.State)} {selectedShip.Remaining:0}초\n"
                    + $"장수 {selectedShip.CommanderLoad}/{selectedShip.CommanderCapacity} · 병력 {selectedShip.Load}/{selectedShip.Capacity} · 화물 {selectedShip.CargoLoad}/{selectedShip.CargoCapacity}\n"
                    + $"승무원 {selectedShip.Crew.Count}명\n운반 중: 식량 {selectedShip.GetCargo(AntColony.Data.ResourceType.Food)} · 재료 {selectedShip.GetCargo(AntColony.Data.ResourceType.Soil)} · 특수 {selectedShip.GetCargo(AntColony.Data.ResourceType.Special)}"
                    + $" · 장비 {selectedShip.EquipmentCargo.Count}{(selectedShip.BlueprintCargo ? " · 설계도" : "")}";
        }

        private void RefreshExpeditions(WorldMapManager world)
        {
            foreach (var ship in world.Transports)
            {
                if (ship == null) continue;
                if (!boxes.TryGetValue(ship, out var text) || text == null) text = boxes[ship] = ExpeditionBox(ship);
                var open = ship == expanded;
                text.text = BoxText(ship, open);
                var box = (RectTransform)text.transform.parent;
                var height = 58 + 18 * (text.text.Split('\n').Length - 3) + (open ? 44 : 0); // 내구·철수 줄 포함
                box.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
                text.rectTransform.sizeDelta = new Vector2(-16, height - 8);
                var actions = (RectTransform)box.Find("Box Actions");
                actions.gameObject.SetActive(open); actions.anchoredPosition = new Vector2(8, -(height - 38));
                box.GetComponent<UnityEngine.UI.Outline>().effectColor = ship == selectedShip ? MenuTheme.Accent : MenuTheme.Line2;
            }
            foreach (var gone in new List<ExpeditionTransport>(boxes.Keys))
                if (gone == null || !System.Linq.Enumerable.Contains(world.Transports, gone)) { if (boxes[gone] != null) Destroy(boxes[gone].transform.parent.gameObject); boxes.Remove(gone); }
            LayoutExpeditions(world.Transports.Count == 0);
        }

        // 목록 칸 높이 = 박스 높이 합(최대 240, 넘치면 스크롤). 수단이 없으면 안내 글 높이. 버튼 묶음은 그 바로 아래, 패널 높이도 맞춘다.
        private void LayoutExpeditions(bool empty)
        {
            float h = 0;
            foreach (var text in boxes.Values)
                if (text != null) h += text.transform.parent.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight + 4;
            h = empty ? 40 : Mathf.Min(240, h + 8);
            expeditionArea.sizeDelta = new Vector2(expeditionArea.sizeDelta.x, h);
            expeditionControls.anchoredPosition = new Vector2(0, -(50 + h + 8));
            expeditions.sizeDelta = new Vector2(expeditions.sizeDelta.x, 50 + h + 8 + 320);
        }

        private UnityEngine.UI.Text ExpeditionBox(ExpeditionTransport ship)
        {
            var rect = MenuTheme.Rect("Expedition " + ship.name, expeditionList);
            rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            rect.gameObject.AddComponent<UnityEngine.UI.Outline>();
            rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 58;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); MenuTheme.StyleButton(button);
            button.onClick.AddListener(() => OpenExpedition(ship));
            var text = MenuTheme.Text(rect, "", 12, 0); Destroy(text.GetComponent<UnityEngine.UI.LayoutElement>());
            text.alignment = TextAnchor.UpperLeft;
            text.rectTransform.anchorMin = new Vector2(0, 1); text.rectTransform.anchorMax = Vector2.one; text.rectTransform.pivot = new Vector2(.5f, 1);
            text.rectTransform.anchoredPosition = new Vector2(0, -4);
            var actions = L.Place(MenuTheme.Rect("Box Actions", rect), 8, 0, 304, 32); // 목록 폭(336) 안에 4개
            // D06: 귀환은 철수 작업 접수(탑승·적재가 끝나야 출발). 철수 중 다시 누르면 철수 취소.
            L.Button(actions, "Box Return", "귀환", 0, 0, 72, 30, () => {
                if (ship != null && ship.IsWithdrawing) { ship.CancelWithdrawal(); feedback.text = "철수를 취소했습니다."; }
                else Result(ship != null && ship.TryReturn(), "철수를 시작합니다. 탑승·적재가 끝나면 출발합니다.");
            }, null, false, 12);
            L.Button(actions, "Box Battlefield", "전장 보기", 76, 0, 76, 30, () => {
                var world = WorldMapManager.Instance;
                if (world != null && ship != null && ship.Site != null && world.ViewSite(ship.Site)) Toggle();
                else feedback.text = "수송 수단이 도착해야 전장에 들어갈 수 있습니다.";
            }, null, false, 12);
            L.Button(actions, "Box Emergency", "긴급 출발", 156, 0, 72, 30, () => {
                var w = ship != null ? ship.WithdrawalProgress : default;
                Result(ship != null && ship.IsWithdrawing && ship.TryEmergencyReturn(), $"남은 인원 {w.People}·짐 {w.Loads}을 두고 출발합니다.");
            }, "철수 중 남은 인원·짐을 두고 탑승한 장수만으로 바로 출발", false, 12);
            // D07: 근처(3m) 장수가 현장 수리(실린 흙 사용) 또는 잔해 해체.
            L.Button(actions, "Box Repair", "수리/해체", 232, 0, 72, 30, () => {
                if (ship == null) return;
                var ok = false;
                foreach (var c in ship.Crew) if (ship.IsWreck ? ship.TrySalvage(c) : ship.TryRepair(c)) { ok = true; break; }
                Result(ok, ship.IsWreck ? "잔해 해체를 시작합니다." : "현장 수리를 시작합니다.");
            }, "수송수단 3m 안 장수가 수리(실린 흙 사용) 또는 잔해 해체", false, 12);
            actions.gameObject.SetActive(false);
            return text;
        }

        // 박스 클릭: 선택 + 펼침/접기 + 행성을 그 수단의 목적지로 돌린다.
        public void OpenExpedition(ExpeditionTransport ship)
        {
            selectedShip = ship;
            expanded = expanded == ship ? null : ship;
            if (ship != null && ship.Site != null) { selectedSite = ship.Site; planetView.Focus(ship.Site.MapPosition); }
        }
        public ExpeditionTransport ExpandedExpedition => expanded;
        public WorldPlanet Planet => planetView;

        private static string BoxText(ExpeditionTransport ship, bool open)
        {
            var crew = ship.Crew;
            var injured = 0; var mood = 0f;
            foreach (var c in crew) { mood += c.Mood; if (c.Health.Injured) injured++; }
            var names = crew.Count == 0 ? "없음" : string.Join(", ", System.Linq.Enumerable.Select(crew, c => c.CommanderName));
            var text = $"<b>{ship.name}</b> · {(ship.Walking ? "도보" : ship.Aircraft ? "비행기" : "차량")} · {StateName(ship.State)}{(ship.Remaining > 0 ? $" {ship.Remaining:0}초" : "")} → {(ship.Site != null ? ship.Site.Title : "정착지")}\n"
                + $"장수 {names} ({ship.CommanderLoad}/{ship.CommanderCapacity}) · 병력 {ship.Load}/{ship.Capacity}\n"
                + $"<color=#b1b3a7>화물 {ship.CargoLoad}/{ship.CargoCapacity} · 전리품 장비 {ship.EquipmentCargo.Count}{(ship.BlueprintCargo ? "·설계도" : "")}"
                + $" · 기분 {(crew.Count > 0 ? mood / crew.Count : 0):0} · 부상 {injured}</color>";
            // D07 내구도·D06 철수 진행.
            if (!ship.Walking)
                text += ship.IsWreck ? $"\n<color=#e06c5a>잔해{(ship.Salvaged ? " · 해체 완료" : $" · 해체 {ship.SalvageRemaining:0}초 남음")}</color>"
                    : $"\n내구 {ship.CurrentHealth:0}/{ship.MaxHealth:0}{(!ship.CanOperate ? " · <color=#e06c5a>운행 불가</color>" : ship.TravelSpeedMultiplier < 1 ? $" · 속도 ×{ship.TravelSpeedMultiplier:0.##}" : "")}"
                        + (ship.FieldWorkActive ? " · 현장 작업 중" : "") + (!ship.HasPilot ? " · <color=#e06c5a>조종 장수 없음</color>" : "");
            if (ship.IsWithdrawing)
            {
                var w = ship.WithdrawalProgress;
                text += $"\n철수 중 · 남은 인원 {w.People} · 짐 {w.Loads} · {(float.IsNaN(w.EstimatedSeconds) ? "이동/구조 대기" : $"약 {w.EstimatedSeconds:0}초")}";
            }
            if (!open) return text;
            text += "\n" + ship.FoodComparison;
            foreach (var c in crew)
                text += $"\n  {c.CommanderName}  병력 {c.TroopCount}/{c.CommandLimit} · 체력 {c.PersonalHealth:0} · 기분 {c.Mood:0}"
                    + (c.Health.Injured ? $" · {c.Health.Summary}" : "");
            return text;
        }

        private static string StateName(ExpeditionState state) => state switch
        {
            ExpeditionState.Home => "대기", ExpeditionState.Outbound => "이동 중", ExpeditionState.Deployed => "도착",
            ExpeditionState.Returning => "귀환 중", _ => state.ToString()
        };

        private static string SiteText(ExpeditionSite site)
        {
            if (site == null) return "<color=#b1b3a7>지도에서 목적지를 선택하세요.</color>";
            var stronghold = site.GetComponent<BossStronghold>();
            if (stronghold != null) return stronghold.Guide;
            var kind = site.Kind switch
            {
                ExpeditionSiteKind.Settlement => "정착지", ExpeditionSiteKind.BossNest => "보스 둥지",
                ExpeditionSiteKind.ResourceSite => "자원지", ExpeditionSiteKind.TradePost => "교역소", _ => "빈 거점"
            };
            var state = site.Disposition == ConquestDisposition.Annexed ? "편입" : site.Disposition == ConquestDisposition.Abandoned ? "포기"
                : site.Disposition == ConquestDisposition.Lost ? "상실"
                : site.Cleared ? site.Kind == ExpeditionSiteKind.ResourceSite ? "고갈" : site.Kind == ExpeditionSiteKind.Settlement ? "정복 결정 대기" : "정리됨"
                : site.Kind == ExpeditionSiteKind.ResourceSite ? "중립" : "적대";
            var text = $"<b><size=16>{site.Title}</size></b>\n{site.Faction} · {kind} · {AntColony.Core.BiomeRules.Name(site.Biome)} · 상태 <b>{state}</b>\n난이도 "
                + DifficultyText(site.Difficulty);
            if (site.Settlement != null && site.Colony != null)
                text += $"\n주둔 {site.Settlement.Garrison.Count} · 현지 비축 식량 {site.Colony.GetStock(AntColony.Data.ResourceType.Food):0} / 재료 {site.Colony.GetStock(AntColony.Data.ResourceType.Soil):0}";
            return text;
        }

        // 거점 난이도 1~3: 쉬움 하늘색 · 보통 노랑 · 어려움 빨강(4 이상은 매우 어려움).
        private static string DifficultyText(int difficulty) => difficulty <= 1 ? "<b><color=#6cc8f0>쉬움</color></b>"
            : difficulty == 2 ? $"<b><color=#{ColorUtility.ToHtmlStringRGB(MenuTheme.HpMid)}>보통</color></b>"
            : $"<b><color=#{ColorUtility.ToHtmlStringRGB(MenuTheme.Danger)}>{(difficulty == 3 ? "어려움" : "매우 어려움")}</color></b>";

        // 거점 생성(WorldMapManager.Start)이 이 패널보다 늦게 돌 수 있어 아직 없는 마커만 이어서 만든다.
        private void BuildMarkers()
        {
            var world = WorldMapManager.Instance;
            if (world == null || mapRoot == null) return;
            for (var i = markers.Count; i < world.Sites.Count; i++)
            {
                var site = world.Sites[i];
                var symbol = site.Kind == ExpeditionSiteKind.Settlement ? "C" : site.Kind == ExpeditionSiteKind.BossNest ? "B" : site.Kind == ExpeditionSiteKind.TradePost ? "T" : "R";
                // 림월드식 거점 표시: 지구 표면을 따라가는 작은 아이콘(종류 글자) + 오른쪽 이름표. 이름표는 클릭을 막지 않는다.
                var marker = L.Button(mapRoot, site.Title, symbol, 0, 0, MarkerSize, MarkerSize, () => selectedSite = site, site.Title, false, 11);
                var tag = L.Label(marker.transform, site.Title, 12, MarkerSize + 4, 0, 160, MarkerSize, MenuTheme.TextColor);
                tag.raycastTarget = false; tag.horizontalOverflow = HorizontalWrapMode.Overflow;
                tag.gameObject.AddComponent<UnityEngine.UI.Shadow>().effectColor = new Color(0, 0, 0, .9f);
                marker.GetComponent<UnityEngine.UI.Image>().color = new Color(site.Color.r * .65f, site.Color.g * .65f, site.Color.b * .65f, 1);
                marker.gameObject.AddComponent<WorldMapMarker>().Initialize(tag);
                marker.gameObject.SetActive(false);
                markers.Add(marker);
            }
        }

        public void Toggle()
        {
            if (panel == null) return;
            var camera = UnityEngine.Camera.main.GetComponent<AntColony.Camera.IsometricCameraController>();
            if (!IsOpen) { cameraWasEnabled = camera.enabled; camera.enabled = false; panel.transform.SetAsLastSibling(); }
            else camera.enabled = cameraWasEnabled;
            panel.SetActive(!IsOpen);
            planetView.SetVisible(IsOpen);
            AnyOpen = IsOpen;
        }
        private static ScienceLab FindLab()
        {
            ScienceLab busy = null;
            foreach (var lab in AntColony.Core.SceneObjects.All<ScienceLab>())
            { if (!lab.isActiveAndEnabled) continue; if (!lab.Busy) return lab; busy = lab; }
            return busy;
        }
        private void ScienceAction(bool air, bool build)
        {
            var lab = FindLab();
            Result(lab != null && (build ? lab.TryConstruct(air) : lab.TryResearch(air)), "시작했습니다.");
        }
        private void Result(bool success, string text) => feedback.text = success ? text : "시작할 수 없습니다: 선택·위치·정원·자원·선행 조건을 확인하세요.";
        private void ResolveConquest(ConquestDisposition disposition)
        {
            Result(selectedSite != null && selectedSite.TryResolveConquest(disposition),
                disposition == ConquestDisposition.Annexed ? "거점을 편입했습니다. 현지 생산 시작 · 방어할 장수는 '선택 장수 주둔'으로 남기세요."
                    : "거점을 포기했습니다. 약탈을 마치고 귀환하세요. 다시 방문할 수 없습니다.");
        }
        private void Depart()
        {
            if (selectedShip != null && selectedShip.LeavesHomeUnattended && (warnedShip != selectedShip || warnedSite != selectedSite))
            {
                warnedShip = selectedShip; warnedSite = selectedSite;
                feedback.text = "본거지에 대응할 장수가 없습니다. 위 왕복 시간을 확인하고 원정 보내기를 다시 누르세요.";
                return;
            }
            // G42: 탑승·하차 지시가 처리되기 전에는 출발하지 않는다.
            if (selectedShip != null && selectedShip.PendingCrewOrderCount > 0) { feedback.text = "승무원 지시 대기 중: " + selectedShip.CrewOrderStatus; return; }
            Result(selectedShip != null && WorldMapManager.SiteRevealed(selectedSite) && selectedShip.TryDepart(selectedSite), "원정대가 출발했습니다.");
            warnedShip = null; warnedSite = null;
        }
        private void NextShip()
        {
            var ships = WorldMapManager.Instance.Transports;
            var index = -1;
            for (var i = 0; i < ships.Count; i++) if (ships[i] == selectedShip) index = i;
            for (var n = 1; n <= ships.Count; n++)
                if (ships[(index + n) % ships.Count] != null) { selectedShip = ships[(index + n) % ships.Count]; return; }
            selectedShip = null;
        }
        private void ChangeCrew(bool station)
        {
            var passengers = new List<CommanderAnt>();
            foreach (var selectable in AntColony.Core.SceneObjects.Any<SelectionManager>().GetSelectedObjects())
            { var commander = selectable != null ? selectable.GetComponent<CommanderAnt>() : null; if (commander != null) passengers.Add(commander); }
            Result(selectedShip != null && (station ? selectedShip.TryStation(passengers) : selectedShip.TryBoard(passengers)),
                station ? "주둔 지시를 내렸습니다. 도착하면 남아 수송 수단이 돌아가도 머뭅니다." : "탑승 지시를 내렸습니다. 수송 수단에 도착하면 승무원이 됩니다.");
        }
    }
}
