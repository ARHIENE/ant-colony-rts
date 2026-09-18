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
        private UnityEngine.UI.Text status, scienceStatus, feedback, mapTitle;
        private readonly List<UnityEngine.UI.Button> markers = new List<UnityEngine.UI.Button>();
        private ExpeditionSite selectedSite;
        private ExpeditionTransport selectedShip;
        private bool seenUnlock, cameraWasEnabled;
        public bool IsOpen => panel != null && panel.activeSelf;
        public RectTransform PanelRect => panel != null ? (RectTransform)panel.transform : null;

        private void Start()
        {
            Button(transform, "World / Science", new Vector2(1090, -60), new Vector2(180, 34), Toggle).name = "WorldMapToggle";
            panel = new GameObject("WorldMapPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(.035f, .14f);
            rect.anchorMax = new Vector2(.965f, .91f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<UnityEngine.UI.Image>().color = new Color(.055f, .075f, .1f, .99f);
            Label(rect, "Science & Expeditions", new Vector2(20, -12), new Vector2(700, 28), 20);
            Button(rect, "Close", new Vector2(1050, -12), new Vector2(110, 30), Toggle);
            scienceStatus = Label(rect, "", new Vector2(20, -50), new Vector2(1140, 42), 14);
            Button(rect, "Build Science Lab", new Vector2(20, -100), new Vector2(190, 36), () => {
                if (FindFirstObjectByType<BuildingPlacementController>().BeginScienceLabPlacement()) Toggle();
                else feedback.text = "Requires home, 60 ants, fishing, a T2 barracks and a selected builder.";
            });
            Button(rect, "Research Vehicle 60F/50S", new Vector2(220, -100), new Vector2(220, 36), () => ScienceAction(false, false));
            Button(rect, "Research Aircraft 100F/80S", new Vector2(450, -100), new Vector2(230, 36), () => ScienceAction(true, false));
            Button(rect, "Build Vehicle 50F/60S", new Vector2(690, -100), new Vector2(220, 36), () => ScienceAction(false, true));
            Button(rect, "Build Aircraft 80F/100S", new Vector2(920, -100), new Vector2(230, 36), () => ScienceAction(true, true));
            var map = new GameObject("WorldMap", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            map.transform.SetParent(rect, false);
            Place((RectTransform)map.transform, new Vector2(20, -156), new Vector2(500, 310));
            map.GetComponent<UnityEngine.UI.Image>().color = new Color(.12f, .2f, .24f);
            mapTitle = Label(map.transform, "", new Vector2(12, -8), new Vector2(478, 42), 15);
            Button(map.transform, "Home", new Vector2(20, -245), new Vector2(90, 38), () => {
                WorldMapManager.Instance.ViewSite(null); Toggle();
            });
            var world = WorldMapManager.Instance;
            if (world != null)
                foreach (var site in world.Sites)
                {
                    var target = site;
                    var marker = Button(map.transform, site.Title, new Vector2(25 + site.MapPosition.x * 340,
                        -65 - (1 - site.MapPosition.y) * 190), new Vector2(145, 42), () => selectedSite = target);
                    marker.GetComponent<UnityEngine.UI.Image>().color = new Color(site.Color.r * .65f, site.Color.g * .65f, site.Color.b * .65f, 1);
                    markers.Add(marker);
                }
            status = Label(rect, "", new Vector2(545, -155), new Vector2(590, 115), 15);
            Button(rect, "Next Transport", new Vector2(545, -285), new Vector2(180, 34), NextShip);
            Button(rect, "Board Selected", new Vector2(735, -285), new Vector2(180, 34), Board);
            Button(rect, "Unload Crew", new Vector2(925, -285), new Vector2(180, 34), () => Result(selectedShip != null && selectedShip.TryUnloadCrew(), "Crew unloaded."));
            Button(rect, "Depart", new Vector2(545, -330), new Vector2(180, 34), () => Result(selectedShip != null && selectedShip.TryDepart(selectedSite), "Expedition departed."));
            Button(rect, "View Battlefield", new Vector2(735, -330), new Vector2(180, 34), () => {
                if (world != null && selectedShip != null && selectedShip.Site != null && world.ViewSite(selectedShip.Site)) Toggle();
                else feedback.text = "The transport must arrive before entering the battlefield.";
            });
            Button(rect, "Return Home", new Vector2(925, -330), new Vector2(180, 34), () => Result(selectedShip != null && selectedShip.TryReturn(), "Returning with crew and cargo."));
            Label(rect, "Board: select commanders near the transport (8m).\nReturn: bring all crew within 8m and deposit carried resources first.\nCargo enters home storage only after returning. Home continues running.",
                new Vector2(545, -385), new Vector2(590, 76), 14);
            feedback = Label(rect, "", new Vector2(20, -480), new Vector2(1130, 45), 14);
            panel.SetActive(false);
        }

        private void Update()
        {
            var world = WorldMapManager.Instance;
            if (world == null || panel == null) return;
            if (world.Unlocked && !seenUnlock) { seenUnlock = true; if (!IsOpen) Toggle(); }
            if (!IsOpen) return;
            if (selectedShip == null) NextShip();
            var lab = FindLab();
            scienceStatus.text = $"Science Lab: {(lab != null ? lab.Busy ? $"Working {lab.Remaining:0}s" : "Ready" : "Not built")} | "
                + $"Vehicle: {(world.VehicleResearched ? "Researched" : "Locked")} | Aircraft: {(world.AircraftResearched ? "Researched" : "Locked")}\n"
                + "Lab: 100F / 100S / 8 ants. Requires population 60 + Fishing + Barracks T2. Research 15s; construction 10s.";
            mapTitle.text = world.Unlocked ? "World Map — select a colored settlement" : "World Map locked\nConstruct your first vehicle or aircraft.";
            foreach (var marker in markers) marker.gameObject.SetActive(world.Unlocked);
            var target = selectedSite != null ? $"Target: {selectedSite.Title} ({(selectedSite.Cleared ? "Cleared" : "Hostile")})" : "Select a destination on the map.";
            status.text = selectedShip == null ? "No transport. Research and construct one.\n" + target
                : $"{selectedShip.name} | {selectedShip.State} {selectedShip.Remaining:0}s | Load {selectedShip.Load}/{selectedShip.Capacity}\n"
                    + $"Crew {selectedShip.Crew.Count} | Cargo {selectedShip.GetCargo(AntColony.Data.ResourceType.Food)}F / "
                    + $"{selectedShip.GetCargo(AntColony.Data.ResourceType.Soil)}S / {selectedShip.GetCargo(AntColony.Data.ResourceType.Special)} Special\n" + target;
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
        private void NextShip()
        {
            var ships = WorldMapManager.Instance.Transports;
            var index = -1;
            for (var i = 0; i < ships.Count; i++) if (ships[i] == selectedShip) index = i;
            for (var n = 1; n <= ships.Count; n++)
                if (ships[(index + n) % ships.Count] != null) { selectedShip = ships[(index + n) % ships.Count]; return; }
            selectedShip = null;
        }
        private void Board()
        {
            var passengers = new List<CommanderAnt>();
            foreach (var selectable in FindFirstObjectByType<SelectionManager>().GetSelectedObjects())
            { var commander = selectable != null ? selectable.GetComponent<CommanderAnt>() : null; if (commander != null) passengers.Add(commander); }
            Result(selectedShip != null && selectedShip.TryBoard(passengers), "Selected commanders boarded.");
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
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
            return label;
        }
        private static UnityEngine.UI.Button Button(Transform parent, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, position, size);
            go.GetComponent<UnityEngine.UI.Image>().color = new Color(.2f, .26f, .32f);
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = go.GetComponent<UnityEngine.UI.Image>();
            button.onClick.AddListener(click);
            Label(go.transform, text, Vector2.zero, size, 13).alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
