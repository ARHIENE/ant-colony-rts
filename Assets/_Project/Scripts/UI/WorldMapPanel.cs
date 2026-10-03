using System.Collections.Generic;
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
        private const float MapW = 672, MapH = 480, MarkerW = 46, MarkerH = 22;
        private GameObject panel;
        private UnityEngine.UI.Text status, siteInfo, scienceStatus, feedback, mapTitle, routeStatus, defenseStatus, worldNotice;
        private UnityEngine.UI.Button annex, abandon, startRoute, stopRoute;
        private readonly List<UnityEngine.UI.Button> markers = new List<UnityEngine.UI.Button>();
        private RectTransform mapRoot, selectionRing;
        private ExpeditionSite selectedSite;
        private ExpeditionTransport selectedShip;
        private bool seenUnlock, cameraWasEnabled;
        private const float PlanetX = (MapW - 390) / 2, PlanetY = 38, PlanetSize = 390;
        private WorldPlanet planetView;
        private RectTransform expeditionList;
        private readonly Dictionary<ExpeditionTransport, UnityEngine.UI.Text> boxes = new Dictionary<ExpeditionTransport, UnityEngine.UI.Text>();
        private ExpeditionTransport expanded;
        public bool IsOpen => panel != null && panel.activeSelf;
        // 월드 위 OnGUI 표시(장수 머리 위·채집 금지)가 전체 화면 월드맵 위에 그려지지 않게 한다.
        public static bool AnyOpen { get; private set; }
        public RectTransform PanelRect => panel != null ? (RectTransform)panel.transform : null;

        private void Start()
        {
            worldNotice = L.Label(transform, "", 14, 300, 70, 840, 28);
            worldNotice.name = "SettlementNotice";
            worldNotice.raycastTarget = false;

            var rect = MenuTheme.Rect("WorldMapPanel", transform);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = new Vector2(0, -40);
            rect.gameObject.AddComponent<UnityEngine.UI.Image>().color = MenuTheme.Hex(0x0f0c09);
            panel = rect.gameObject;

            // 왼쪽: 원정대(수송 수단·승무원).
            var left = L.Plate(rect, "Expeditions", 12, 12, 360, 746);
            L.Label(left, "<b>원정대</b>", 15, 12, 8, 200, 26);
            // 원정 1건 = 박스(목적지·남은 시간·상태·장수·병력·수단/적재·전리품·부대 기분/부상). 클릭 = 행성을 그 목적지로 돌리고 박스를 펼침.
            expeditionList = L.List(rect, 24, 44, 336, 166, 4);
            status = L.Label(rect, "", 13, 24, 48, 336, 160, MenuTheme.Muted, TextAnchor.UpperLeft);
            status.gameObject.SetActive(false);
            Btn(rect, "Next Transport", "다음 수송 수단", 24, 214, NextShip);
            Btn(rect, "Board Selected", "선택 장수 탑승", 196, 214, () => ChangeCrew(false));
            Btn(rect, "Unload Crew", "승무원 내리기", 24, 254, () => Result(selectedShip != null && selectedShip.TryUnloadCrew(), "승무원을 내렸습니다."));
            Btn(rect, "Station Selected", "선택 장수 주둔", 196, 254, () => ChangeCrew(true));
            Btn(rect, "Return Home", "귀환", 24, 294, () => Result(selectedShip != null && selectedShip.TryReturn(), "장수와 화물을 싣고 귀환합니다."));
            Btn(rect, "Collect Equipment / Blueprint", "전리품·설계도 싣기", 196, 294,
                () => Result(selectedShip != null && selectedShip.TryCollectRewards(), "전리품을 실었습니다. 귀환하면 사용할 수 있습니다."));
            L.Line(rect, 24, 340, 336);
            routeStatus = L.Label(rect, "", 12, 24, 348, 336, 80, MenuTheme.Muted, TextAnchor.UpperLeft);
            L.Label(rect, "탑승·주둔·귀환: 8m 이내, 운반 중인 자원은 먼저 내려놓기.\n주둔: 수송 수단이 돌아와도 거점에 남음. 탑승: 선택한 주둔 장수를 다시 태움.\n편입 거점 생산: 60초마다 (식량 10 / 재료 5) × 난이도, 수송으로 집에 옮김.",
                12, 24, 436, 336, 90, MenuTheme.Dim, TextAnchor.UpperLeft);
            feedback = L.Label(rect, "", 13, 24, 680, 336, 70, MenuTheme.Accent, TextAnchor.UpperLeft);

            // 가운데: 행성 지도. 마커는 거점의 MapPosition(0~1)을 지도 안으로 옮긴다.
            var map = L.Box(rect, "WorldMap", 384, 12, MapW, MapH, MenuTheme.Hex(0x16120e), true);
            planetView = WorldPlanet.Create(map, PlanetX, PlanetY, PlanetSize);
            mapTitle = L.Label(map, "", 13, 12, 6, 600, 40, MenuTheme.Muted, TextAnchor.UpperLeft);
            selectionRing = L.Box(map, "SelectionRing", 0, 0, MarkerW + 8, MarkerH + 8, MenuTheme.Accent);
            selectionRing.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            selectionRing.gameObject.SetActive(false);
            L.Button(map, "Home", "소굴로", 12, MapH - 46, 90, 34, () => { WorldMapManager.Instance.ViewSite(null); Toggle(); }, "홈 둥지 화면으로 돌아갑니다.");
            startRoute = L.Button(map, "Start Auto", "자동 수송 시작", 110, MapH - 46, 150, 34, () =>
                Result(selectedShip != null && selectedShip.Route != null && selectedShip.Route.TryStart(selectedSite), "자동 수송을 시작했습니다. 장수가 모아서 돌아옵니다."));
            stopRoute = L.Button(map, "Stop Auto", "자동 수송 중지", 268, MapH - 46, 150, 34, () => {
                selectedShip?.Route?.Stop();
                feedback.text = "자동 수송을 멈췄습니다. 진행 중인 이동·작업은 계속되며, 나가 있다면 직접 귀환시키세요.";
            });
            // HUD v3: 외교는 상단 메뉴에서 빼고 월드맵 안으로.
            L.Button(map, "Diplomacy", "외교", 426, MapH - 46, 90, 34, () => GameMenuController.Instance?.Diplomacy(), "접촉한 문명 · 협정 · 전쟁 · 거래");
            mapRoot = map;
            BuildMarkers();

            // 가운데 아래: 선택 거점 정보와 행동.
            L.Plate(rect, "SiteInfo", 384, 504, MapW, 254);
            siteInfo = L.Label(rect, "", 13, 398, 514, 644, 100, align: TextAnchor.UpperLeft);
            Btn(rect, "Depart", "원정 보내기", 398, 622, () => Result(selectedShip != null && selectedShip.TryDepart(selectedSite), "원정대가 출발했습니다."), 156);
            Btn(rect, "View Battlefield", "전장 보기", 560, 622, () => {
                var world = WorldMapManager.Instance;
                if (world != null && selectedSite != null && world.ViewSite(selectedSite)) Toggle();
                else feedback.text = "수송 수단이 도착해야 전장에 들어갈 수 있습니다.";
            }, 156);
            annex = Btn(rect, "Annex", "편입", 722, 622, () => ResolveConquest(ConquestDisposition.Annexed), 156);
            abandon = Btn(rect, "Abandon", "포기", 884, 622, () => ResolveConquest(ConquestDisposition.Abandoned), 156);
            defenseStatus = L.Label(rect, "", 12, 398, 668, 644, 22, MenuTheme.DangerInk);
            defenseStatus.name = "SettlementDefenseStatus";

            // 오른쪽: 과학·수송 수단 건조 + 범례.
            L.Plate(rect, "Science", 1068, 12, 360, 300);
            L.Label(rect, "<b>과학 · 수송 수단</b>", 15, 1080, 20, 336, 26);
            scienceStatus = L.Label(rect, "", 12, 1080, 50, 336, 110, MenuTheme.Muted, TextAnchor.UpperLeft);
            Btn(rect, "Build Science Lab", "과학 연구소 건설", 1080, 170, () => {
                if (FindFirstObjectByType<BuildingPlacementController>().BeginScienceLabPlacement()) Toggle();
                else feedback.text = "홈 둥지, 개미 60, 낚시, 2단계 병영, 선택한 건설 장수가 필요합니다.";
            });
            Btn(rect, "Science / Researchers", "과학 연구 · 연구원", 1252, 170, () => { Toggle(); GameMenuController.Instance.Science(); });
            Btn(rect, "Build Vehicle 50F/60 재료", "차량 건조 식50 재료60", 1080, 210, () => ScienceAction(false, true));
            Btn(rect, "Build Aircraft 80F/100 재료", "비행기 건조 식80 재료100", 1252, 210, () => ScienceAction(true, true));
            var legend = L.Plate(rect, "Legend", 1068, 324, 360, 220);
            L.Label(legend, "<b>범례</b>", 14, 12, 8, 200, 24);
            L.Label(legend, "거점 종류   C 정착지 · B 보스 둥지 · R 자원지 · T 교역소", 12, 12, 38, 336, 20, MenuTheme.Muted);
            string[] names = { "편입 거점", "공격받는 거점", "포기한 거점", "문명·중립 (세력 색)" };
            Color[] colors = { new Color(.3f, .85f, .5f), new Color(1f, .25f, .15f), Color.gray, MenuTheme.Accent };
            for (var i = 0; i < names.Length; i++)
            {
                L.Box(legend, "Swatch", 12, 68 + i * 26, 14, 14, colors[i] * .75f);
                L.Label(legend, names[i], 12, 34, 64 + i * 26, 300, 22, MenuTheme.Muted);
            }
            L.Button(rect, "Close", "닫기   M", 1318, 724, 110, 34, Toggle);
            panel.SetActive(false);
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
            defenseStatus.text = selectedSite != null && selectedSite.Defense != null ? selectedSite.Defense.Status : "";
            if (selectedShip == null) NextShip();
            var route = selectedShip != null ? selectedShip.Route : null;
            startRoute.interactable = route != null && !route.IsRunning && selectedShip.State == ExpeditionState.Home
                && selectedSite != null && selectedSite.Disposition == ConquestDisposition.Annexed;
            stopRoute.interactable = route != null && route.IsRunning;
            routeStatus.text = route != null && route.IsRunning
                ? $"<b>자동 수송</b>: {route.Destination.Title}\n{route.Status}\n자원·일꾼마다 한 번 싣고, 집에서 {TransportRoute.IntervalSeconds:0}초 뒤 반복."
                : $"<b>자동 수송</b>: {(route != null ? route.Status : "꺼짐")}\n집에서 일꾼 장수를 태우고, 편입 거점을 고른 뒤 자동 수송 시작.";
            var lab = FindLab();
            scienceStatus.text = $"과학 연구소: {(lab != null ? lab.Busy ? $"작업 중 {lab.Remaining:0}초" : "대기" : "없음")}\n"
                + $"차량: {(world.VehicleResearched ? "연구 완료" : "잠김")} · 비행기: {(world.AircraftResearched ? "연구 완료" : "잠김")}\n"
                + "연구소: 식량 100 / 재료 100 / 개미 8. 연구원을 배정하고, 차량은 2단계·비행기는 3단계로 강화. 건조 10초.";
            mapTitle.text = world.Unlocked ? $"<b>월드맵</b>  거점 {world.Sites.Count}" : "<b>월드맵 잠김</b>\n첫 차량이나 비행기를 건조하세요.";
            for (var i = 0; i < markers.Count; i++)
            {
                var site = world.Sites[i];
                var local = Vector2.zero;
                var visible = world.Unlocked && planetView.Project(site.MapPosition, out local);
                markers[i].gameObject.SetActive(visible);
                if (visible) ((RectTransform)markers[i].transform).anchoredPosition = new Vector2(PlanetX + local.x - MarkerW / 2, -(PlanetY + local.y - MarkerH / 2));
                var color = site.Defense != null && (site.Defense.UnderAttack || site.Disposition == ConquestDisposition.Lost)
                    ? new Color(1f, .25f, .15f) : site.Disposition == ConquestDisposition.Annexed ? new Color(.3f, .85f, .5f)
                    : site.Disposition == ConquestDisposition.Abandoned ? Color.gray : DiplomacyManager.Instance?.Faction(site)?.color ?? site.Color;
                markers[i].GetComponent<UnityEngine.UI.Image>().color = new Color(color.r * .65f, color.g * .65f, color.b * .65f, 1);
                if (site == selectedSite)
                {
                    selectionRing.gameObject.SetActive(visible);
                    selectionRing.anchoredPosition = ((RectTransform)markers[i].transform).anchoredPosition + new Vector2(-4, 4);
                }
            }
            if (selectedSite == null) selectionRing.gameObject.SetActive(false);
            annex.interactable = abandon.interactable = selectedSite != null && selectedSite.CanResolveConquest;
            siteInfo.text = SiteText(selectedSite);
            RefreshExpeditions(world);
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
                var height = 58 + (open ? 18 * ship.Crew.Count + 44 : 0);
                box.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
                text.rectTransform.sizeDelta = new Vector2(-16, height - 8);
                var actions = (RectTransform)box.Find("Box Actions");
                actions.gameObject.SetActive(open); actions.anchoredPosition = new Vector2(8, -(height - 38));
                box.GetComponent<UnityEngine.UI.Outline>().effectColor = ship == selectedShip ? MenuTheme.Accent : MenuTheme.Line2;
            }
            foreach (var gone in new List<ExpeditionTransport>(boxes.Keys))
                if (gone == null || !System.Linq.Enumerable.Contains(world.Transports, gone)) { if (boxes[gone] != null) Destroy(boxes[gone].transform.parent.gameObject); boxes.Remove(gone); }
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
            var actions = L.Place(MenuTheme.Rect("Box Actions", rect), 8, 0, 300, 32);
            L.Button(actions, "Box Return", "귀환", 0, 0, 96, 30, () => Result(ship != null && ship.TryReturn(), "장수와 화물을 싣고 귀환합니다."), null, false, 12);
            L.Button(actions, "Box Battlefield", "전장 보기", 104, 0, 110, 30, () => {
                var world = WorldMapManager.Instance;
                if (world != null && ship != null && ship.Site != null && world.ViewSite(ship.Site)) Toggle();
                else feedback.text = "수송 수단이 도착해야 전장에 들어갈 수 있습니다.";
            }, null, false, 12);
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
            foreach (var c in crew) { mood += c.Mood; if (c.PersonalState.injuries.Count > 0) injured++; }
            var names = crew.Count == 0 ? "없음" : string.Join(", ", System.Linq.Enumerable.Select(crew, c => c.CommanderName));
            var text = $"<b>{ship.name}</b> · {(ship.Aircraft ? "비행기" : "차량")} · {StateName(ship.State)}{(ship.Remaining > 0 ? $" {ship.Remaining:0}초" : "")} → {(ship.Site != null ? ship.Site.Title : "소굴")}\n"
                + $"장수 {names} ({ship.CommanderLoad}/{ship.CommanderCapacity}) · 병력 {ship.Load}/{ship.Capacity}\n"
                + $"<color=#968976>화물 {ship.CargoLoad}/{ship.CargoCapacity} · 전리품 장비 {ship.EquipmentCargo.Count}{(ship.BlueprintCargo ? "·설계도" : "")}"
                + $" · 기분 {(crew.Count > 0 ? mood / crew.Count : 0):0} · 부상 {injured}</color>";
            if (!open) return text;
            foreach (var c in crew)
                text += $"\n  {c.CommanderName}  병력 {c.TroopCount}/{c.CommandLimit} · 체력 {c.PersonalHealth:0} · 기분 {c.Mood:0}"
                    + (c.PersonalState.injuries.Count > 0 ? $" · 부상 {c.PersonalState.injuries.Count}" : "");
            return text;
        }

        private static string StateName(ExpeditionState state) => state switch
        {
            ExpeditionState.Home => "대기", ExpeditionState.Outbound => "이동 중", ExpeditionState.Deployed => "도착",
            ExpeditionState.Returning => "귀환 중", _ => state.ToString()
        };

        private static string SiteText(ExpeditionSite site)
        {
            if (site == null) return "<color=#968976>지도에서 목적지를 선택하세요.</color>";
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
                + new string('■', Mathf.Clamp(site.Difficulty, 0, 5)) + new string('□', Mathf.Clamp(5 - site.Difficulty, 0, 5));
            if (site.Settlement != null && site.Colony != null)
                text += $"\n주둔 {site.Settlement.Garrison.Count} · 현지 비축 식량 {site.Colony.GetStock(AntColony.Data.ResourceType.Food):0} / 재료 {site.Colony.GetStock(AntColony.Data.ResourceType.Soil):0}";
            return text;
        }

        // 거점 생성(WorldMapManager.Start)이 이 패널보다 늦게 돌 수 있어 아직 없는 마커만 이어서 만든다.
        private void BuildMarkers()
        {
            var world = WorldMapManager.Instance;
            if (world == null || mapRoot == null) return;
            for (var i = markers.Count; i < world.Sites.Count; i++)
            {
                var site = world.Sites[i];
                var symbol = site.Kind == ExpeditionSiteKind.Settlement ? "C" : site.Kind == ExpeditionSiteKind.BossNest ? "B" : site.Kind == ExpeditionSiteKind.TradePost ? "T" : "R";
                var marker = L.Button(mapRoot, site.Title, $"{symbol}{i + 1:00}", 0, 0, MarkerW, MarkerH, () => selectedSite = site, site.Title, false, 11);
                marker.GetComponent<UnityEngine.UI.Image>().color = new Color(site.Color.r * .65f, site.Color.g * .65f, site.Color.b * .65f, 1);
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
            foreach (var lab in FindObjectsByType<ScienceLab>(FindObjectsSortMode.None))
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
            foreach (var selectable in FindFirstObjectByType<SelectionManager>().GetSelectedObjects())
            { var commander = selectable != null ? selectable.GetComponent<CommanderAnt>() : null; if (commander != null) passengers.Add(commander); }
            Result(selectedShip != null && (station ? selectedShip.TryStation(passengers) : selectedShip.TryBoard(passengers)),
                station ? "선택한 장수가 주둔합니다. 수송 수단이 돌아가도 남습니다." : "선택한 장수를 승무원에 추가했습니다.");
        }
    }
}
