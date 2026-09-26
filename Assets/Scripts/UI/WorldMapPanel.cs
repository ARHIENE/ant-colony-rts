using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.UI
{
    public class WorldMapPanel : MonoBehaviour
    {
        private GameObject panel;
        private UnityEngine.UI.Text status, scienceStatus, feedback, mapTitle, routeStatus, defenseStatus, worldNotice;
        private UnityEngine.UI.Button annex, abandon, startRoute, stopRoute;
        private readonly List<UnityEngine.UI.Button> markers = new List<UnityEngine.UI.Button>();
        private Transform mapRoot;
        private ExpeditionSite selectedSite;
        private ExpeditionTransport selectedShip;
        private bool seenUnlock, cameraWasEnabled;
        public bool IsOpen => panel != null && panel.activeSelf;
        public RectTransform PanelRect => panel != null ? (RectTransform)panel.transform : null;

        private void Start()
        {
            var toggle = Button(transform, "월드맵  M", new Vector2(334, -6), new Vector2(84, 28), Toggle);
            toggle.name = "WorldMapToggle";
            toggle.gameObject.AddComponent<MenuTooltip>().Message = "Open science research, transport construction and world expeditions.";
            worldNotice = Label(transform, "", new Vector2(300, -70), new Vector2(840, 28), 14);
            worldNotice.name = "SettlementNotice";
            panel = new GameObject("WorldMapPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(.035f, .14f);
            rect.anchorMax = new Vector2(.965f, .91f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<UnityEngine.UI.Image>().color = MenuTheme.Background;
            Label(rect, "Science & Expeditions", new Vector2(20, -12), new Vector2(700, 28), 20);
            Button(rect, "Close", new Vector2(1050, -12), new Vector2(110, 30), Toggle);
            scienceStatus = Label(rect, "", new Vector2(20, -50), new Vector2(1140, 42), 14);
            Button(rect, "Build Science Lab", new Vector2(20, -100), new Vector2(190, 36), () => {
                if (FindFirstObjectByType<BuildingPlacementController>().BeginScienceLabPlacement()) Toggle();
                else feedback.text = "Requires home, 60 ants, fishing, a T2 barracks and a selected builder.";
            });
            Button(rect, "Science / Researchers", new Vector2(220, -100), new Vector2(220, 36), () => { Toggle(); GameMenuController.Instance.Science(); });
            Button(rect, "Collect Equipment / Blueprint", new Vector2(450, -100), new Vector2(230, 36), () => Result(selectedShip != null && selectedShip.TryCollectRewards(), "Reward cargo loaded. Return home to use it."));
            Button(rect, "Build Vehicle 50F/60S", new Vector2(690, -100), new Vector2(220, 36), () => ScienceAction(false, true));
            Button(rect, "Build Aircraft 80F/100S", new Vector2(920, -100), new Vector2(230, 36), () => ScienceAction(true, true));
            var map = new GameObject("WorldMap", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            map.transform.SetParent(rect, false);
            Place((RectTransform)map.transform, new Vector2(20, -156), new Vector2(500, 310));
            map.GetComponent<UnityEngine.UI.Image>().color = MenuTheme.Well;
            mapTitle = Label(map.transform, "", new Vector2(12, -8), new Vector2(478, 42), 15);
            Button(map.transform, "Home", new Vector2(20, -245), new Vector2(90, 38), () => {
                WorldMapManager.Instance.ViewSite(null); Toggle();
            });
            startRoute = Button(map.transform, "Start Auto", new Vector2(120, -245), new Vector2(170, 38), () =>
                Result(selectedShip != null && selectedShip.Route != null && selectedShip.Route.TryStart(selectedSite),
                    "Auto route started. Commanders will collect and return."));
            stopRoute = Button(map.transform, "Stop Auto", new Vector2(300, -245), new Vector2(170, 38), () => {
                selectedShip?.Route?.Stop();
                feedback.text = "Auto stopped. Current travel and work continue; return manually if away.";
            });
            mapRoot = map.transform;
            BuildMarkers();
            status = Label(rect, "", new Vector2(545, -155), new Vector2(590, 115), 15);
            Button(rect, "Next Transport", new Vector2(545, -285), new Vector2(180, 34), NextShip);
            Button(rect, "Board Selected", new Vector2(735, -285), new Vector2(180, 34), () => ChangeCrew(false));
            Button(rect, "Unload Crew", new Vector2(925, -285), new Vector2(180, 34), () => Result(selectedShip != null && selectedShip.TryUnloadCrew(), "Crew unloaded."));
            Button(rect, "Depart", new Vector2(545, -330), new Vector2(180, 34), () => Result(selectedShip != null && selectedShip.TryDepart(selectedSite), "Expedition departed."));
            Button(rect, "View Battlefield", new Vector2(735, -330), new Vector2(180, 34), () => {
                var world = WorldMapManager.Instance;
                if (world != null && selectedSite != null && world.ViewSite(selectedSite)) Toggle();
                else feedback.text = "The transport must arrive before entering the battlefield.";
            });
            Button(rect, "Return Home", new Vector2(925, -330), new Vector2(180, 34), () => Result(selectedShip != null && selectedShip.TryReturn(), "Returning with crew and cargo."));
            annex = Button(rect, "Annex", new Vector2(545, -375), new Vector2(180, 34), () => ResolveConquest(ConquestDisposition.Annexed));
            abandon = Button(rect, "Abandon", new Vector2(735, -375), new Vector2(180, 34), () => ResolveConquest(ConquestDisposition.Abandoned));
            Button(rect, "Station Selected", new Vector2(925, -375), new Vector2(180, 34), () => ChangeCrew(true));
            Label(rect, "Board / Station / Return: within 8m; deposit carried resources first.\nStation: leave crew here. Board: recall selected garrison to transport.\nLocal production: (10F / 5S) x difficulty per 60s. Ship cargo home.",
                new Vector2(545, -418), new Vector2(590, 58), 14);
            routeStatus = Label(rect, "", new Vector2(20, -475), new Vector2(500, 50), 14);
            defenseStatus = Label(rect, "", new Vector2(20, -527), new Vector2(1120, 20), 13);
            defenseStatus.name = "SettlementDefenseStatus";
            feedback = Label(rect, "", new Vector2(545, -480), new Vector2(590, 45), 14);
            panel.SetActive(false);
        }

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
                ? $"Auto: {route.Destination.Title}\n{route.Status}\nOne load per resource / worker; repeat after {TransportRoute.IntervalSeconds:0}s at home."
                : $"Auto: {(route != null ? route.Status : "Off")}\nBoard worker crew at home, select an annexed site, Start Auto.";
            var lab = FindLab();
            scienceStatus.text = $"Science Lab: {(lab != null ? lab.Busy ? $"Working {lab.Remaining:0}s" : "Ready" : "Not built")} | "
                + $"Vehicle: {(world.VehicleResearched ? "Researched" : "Locked")} | Aircraft: {(world.AircraftResearched ? "Researched" : "Locked")}\n"
                + "Lab: 100F / 100S / 8 ants. Assign researchers; upgrade to T2 for vehicles / T3 for aircraft. Construction 10s.";
            mapTitle.text = world.Unlocked ? $"World Map — {world.Sites.Count} sites\nC: Colony / B: Boss / R: Resources" : "World Map locked\nConstruct your first vehicle or aircraft.";
            for (var i = 0; i < markers.Count; i++)
            {
                var site = world.Sites[i];
                markers[i].gameObject.SetActive(world.Unlocked);
                var color = site.Defense != null && (site.Defense.UnderAttack || site.Disposition == ConquestDisposition.Lost)
                    ? new Color(1f, .25f, .15f) : site.Disposition == ConquestDisposition.Annexed ? new Color(.3f, .85f, .5f)
                    : site.Disposition == ConquestDisposition.Abandoned ? Color.gray : DiplomacyManager.Instance?.Faction(site)?.color ?? site.Color;
                markers[i].GetComponent<UnityEngine.UI.Image>().color = new Color(color.r * .65f, color.g * .65f, color.b * .65f, 1);
            }
            annex.interactable = abandon.interactable = selectedSite != null && selectedSite.CanResolveConquest;
            var target = selectedSite != null ? $"Target: {selectedSite.Title}\n{selectedSite.Faction} | {selectedSite.Kind}{(selectedSite.Kind == ExpeditionSiteKind.Settlement ? $" | Defenders {selectedSite.Difficulty}" : "")} | "
                + (selectedSite.Disposition != ConquestDisposition.Undecided ? selectedSite.Disposition.ToString()
                    : selectedSite.Cleared ? selectedSite.Kind == ExpeditionSiteKind.ResourceSite ? "Depleted"
                        : selectedSite.Kind == ExpeditionSiteKind.Settlement ? "Conquest undecided" : "Cleared"
                    : selectedSite.Kind == ExpeditionSiteKind.ResourceSite ? "Neutral" : "Hostile") : "Select a destination on the map.";
            if (selectedSite != null && selectedSite.Settlement != null && selectedSite.Colony != null)
                target += $"\nGarrison {selectedSite.Settlement.Garrison.Count} | Local stock "
                    + $"{selectedSite.Colony.GetStock(AntColony.Data.ResourceType.Food):0}F / "
                    + $"{selectedSite.Colony.GetStock(AntColony.Data.ResourceType.Soil):0}S";
            status.text = selectedShip == null ? "No transport. Research and construct one.\n" + target
                : $"{selectedShip.name} | {selectedShip.State} {selectedShip.Remaining:0}s | Commanders {selectedShip.CommanderLoad}/{selectedShip.CommanderCapacity} Troops {selectedShip.Load}/{selectedShip.Capacity} Cargo {selectedShip.CargoLoad}/{selectedShip.CargoCapacity}\n"
                    + $"Crew {selectedShip.Crew.Count} | Cargo {selectedShip.GetCargo(AntColony.Data.ResourceType.Food)}F / "
                    + $"{selectedShip.GetCargo(AntColony.Data.ResourceType.Soil)}S / {selectedShip.GetCargo(AntColony.Data.ResourceType.Special)} Special | {selectedShip.EquipmentCargo.Count} equipment{(selectedShip.BlueprintCargo ? " + blueprint" : "")}\n" + target;
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
                var marker = Button(mapRoot, $"{symbol}{i + 1:00}", new Vector2(20 + i % 7 * 65,
                    -60 - i / 7 * 34), new Vector2(60, 30), () => selectedSite = site);
                marker.name = site.Title;
                marker.GetComponent<UnityEngine.UI.Image>().color = new Color(site.Color.r * .65f, site.Color.g * .65f, site.Color.b * .65f, 1);
                marker.gameObject.SetActive(world.Unlocked);
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
            Result(lab != null && (build ? lab.TryConstruct(air) : lab.TryResearch(air)), "Started.");
        }
        private void Result(bool success, string text) => feedback.text = success ? text : "Cannot start: check selection, location, capacity, resources or prerequisites.";
        private void ResolveConquest(ConquestDisposition disposition)
        {
            Result(selectedSite != null && selectedSite.TryResolveConquest(disposition),
                disposition == ConquestDisposition.Annexed ? "Site annexed. Local production started; Station Selected to leave defenders."
                    : "Site abandoned. Finish looting and return; this site cannot be revisited.");
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
                station ? "Selected commanders stationed. They remain when the transport returns." : "Selected commanders added to transport crew.");
        }
        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
        private static UnityEngine.UI.Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, position, size);
            var label = go.GetComponent<UnityEngine.UI.Text>();
            label.font = MenuTheme.Font;
            label.fontSize = fontSize;
            label.color = MenuTheme.TextColor;
            label.text = text;
            label.raycastTarget = false;
            return label;
        }
        private static UnityEngine.UI.Button Button(Transform parent, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, position, size);
            go.GetComponent<UnityEngine.UI.Image>().color = MenuTheme.Plate2;
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = go.GetComponent<UnityEngine.UI.Image>();
            MenuTheme.StyleButton(button);
            button.onClick.AddListener(click);
            Label(go.transform, text, Vector2.zero, size, 13).alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
