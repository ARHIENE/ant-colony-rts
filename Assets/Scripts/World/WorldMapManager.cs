using System.Collections.Generic;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    public class WorldMapManager : MonoBehaviour
    {
        public static WorldMapManager Instance { get; private set; }
        [SerializeField] private EnemyColony colonyTemplate;
        [SerializeField] private BossHealth bossTemplate;
        [SerializeField] private EnemyCommander commanderTemplate;
        [SerializeField] private ExpeditionTransport transportTemplate;
        private readonly List<ExpeditionSite> sites = new List<ExpeditionSite>();
        private readonly List<ExpeditionTransport> transports = new List<ExpeditionTransport>();
        public IReadOnlyList<ExpeditionSite> Sites => sites;
        public IReadOnlyList<ExpeditionTransport> Transports => transports;
        public bool VehicleResearched { get; internal set; }
        public bool AircraftResearched { get; internal set; }
        public bool Unlocked { get; private set; }
        public ScienceLab Researcher { get; internal set; }
        public ExpeditionSite ViewedSite { get; private set; }
        public Vector3 HomePosition { get; private set; }
        private Vector3 homeFocus;

        private void Awake() => Instance = this;

        private void Start()
        {
            var queen = FindFirstObjectByType<QueenChamber>();
            HomePosition = queen != null ? queen.Position : Vector3.zero;
            var camera = UnityEngine.Camera.main.GetComponent<AntColony.Camera.IsometricCameraController>();
            homeFocus = camera.FocusPoint;
            // 문명들은 월드맵 해금 전에도 존재하고 성장한다. 본거지와는 NavMesh가 연결되지 않는다.
            CreateSite("Amber Colony", new Color(.9f, .6f, .15f), new Vector2(.25f, .65f), false);
            CreateSite("Azure Colony", new Color(.25f, .65f, 1f), new Vector2(.68f, .72f), false);
            CreateSite("MiniBird Nest", new Color(.85f, .3f, .4f), new Vector2(.55f, .25f), true);
        }

        private void CreateSite(string title, Color color, Vector2 mapPosition, bool boss)
        {
            var go = new GameObject(title);
            go.transform.position = new Vector3(5000 + sites.Count * 200, 0, 5000);
            var site = go.AddComponent<ExpeditionSite>();
            site.Initialize(title, color, mapPosition, boss ? null : colonyTemplate, boss ? bossTemplate : null, commanderTemplate);
            sites.Add(site);
        }

        public bool CanCreateTransport(Vector3 origin, out Vector3 position)
        {
            // 기존 출구 주변에서 걸을 수 있고 다른 수단과 겹치지 않는 위치를 찾는다.
            for (var i = 0; i < 16; i++)
            {
                var candidate = origin + Quaternion.Euler(0, i * 45f, 0) * Vector3.right * (5 + i / 8 * 4);
                if (!NavMesh.SamplePosition(candidate, out var hit, 4, NavMesh.AllAreas)) continue;
                if (Physics.CheckBox(hit.position + Vector3.up, new Vector3(1.6f, .5f, 2f), Quaternion.identity, ~(1 << 8))) continue;
                position = hit.position;
                return transportTemplate != null;
            }
            position = default;
            return false;
        }

        public ExpeditionTransport CreateTransport(bool aircraft, Vector3 position)
        {
            var ship = Instantiate(transportTemplate, position, Quaternion.identity);
            ship.Initialize(aircraft);
            ship.gameObject.SetActive(true);
            transports.Add(ship);
            Unlocked = true;
            return ship;
        }

        public bool ViewSite(ExpeditionSite site)
        {
            if (site != null && (!Unlocked || site.Visitor == null || site.Visitor.State != ExpeditionState.Deployed)) return false;
            var camera = UnityEngine.Camera.main.GetComponent<AntColony.Camera.IsometricCameraController>();
            if (ViewedSite == null) homeFocus = camera.FocusPoint;
            ViewedSite = site;
            FindFirstObjectByType<SelectionManager>()?.ClearSelection();
            FindFirstObjectByType<BuildingPlacementController>()?.CancelPlacement();
            camera.SetRegion(site == null ? homeFocus : site.Landing + Vector3.forward * 15,
                site == null ? new Bounds(new Vector3(200, 0, 200), new Vector3(400, 100, 400))
                    : new Bounds(site.transform.position, new Vector3(65, 100, 65)));
            return true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var site in sites) if (site != null) Destroy(site.gameObject);
        }
    }
}
