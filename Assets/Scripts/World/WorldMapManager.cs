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
        public string SettlementNotice { get; internal set; } = "";
        private Vector3 homeFocus;

        private void Awake() => Instance = this;

        private void Start()
        {
            var queen = FindFirstObjectByType<QueenChamber>();
            HomePosition = queen != null ? queen.Position : Vector3.zero;
            var camera = UnityEngine.Camera.main.GetComponent<AntColony.Camera.IsometricCameraController>();
            homeFocus = camera.FocusPoint;
            // 문명들은 월드맵 해금 전에도 존재하고 성장한다. 본거지와는 NavMesh가 연결되지 않는다.
            // ponytail: 30곳의 구성/격자 배치는 임시값이다. 생성 규칙 확정 시 교체한다.
            for (var i = 0; i < 30; i++)
            {
                var kind = i % 5 == 2 ? ExpeditionSiteKind.BossNest
                    : i % 5 == 4 ? ExpeditionSiteKind.ResourceSite : ExpeditionSiteKind.Settlement;
                var faction = i % 5 == 0 ? "Amber" : i % 5 == 1 ? "Azure" : $"Colony {i + 1:00}";
                var color = i % 5 == 0 ? new Color(.9f, .6f, .15f)
                    : i % 5 == 1 ? new Color(.25f, .65f, 1f) : Color.HSVToRGB(.3f + i / 150f, .55f, .85f);
                var title = i < 2 ? faction + " Colony" : faction + $" Outpost {i + 1:00}";
                if (kind == ExpeditionSiteKind.BossNest)
                { title = i == 2 ? "MiniBird Nest" : $"MiniBird Nest {i + 1:00}"; faction = "Wildlife"; color = new Color(.85f, .3f, .4f); }
                else if (kind == ExpeditionSiteKind.ResourceSite)
                { title = $"Resource Field {i + 1:00}"; faction = "Neutral"; color = new Color(.35f, .8f, .45f); }
                // 난이도는 생성 시 고정된다. 본거지와의 거리나 진행도에 따라 올라가지 않는다.
                CreateSite(title, faction, color, new Vector2(i % 6 / 5f, 1 - i / 6 / 4f), kind, 1 + i % 3);
            }
        }

        private void CreateSite(string title, string faction, Color color, Vector2 mapPosition, ExpeditionSiteKind kind, int difficulty)
        {
            var go = new GameObject(title);
            go.transform.position = new Vector3(5000 + sites.Count * 200, 0, 5000);
            var site = go.AddComponent<ExpeditionSite>();
            site.Initialize(title, faction, color, mapPosition, kind, difficulty, colonyTemplate, bossTemplate, commanderTemplate);
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
            if (site != null && (!Unlocked || (site.Disposition != ConquestDisposition.Annexed
                && (site.Visitor == null || site.Visitor.State != ExpeditionState.Deployed)))) return false;
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
