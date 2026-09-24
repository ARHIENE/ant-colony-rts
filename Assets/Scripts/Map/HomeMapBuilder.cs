using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Map
{
    // 새 게임 옵션(맵 크기 / 시드)을 씬에 실제로 들어 있는 지형에 적용한다.
    // 씬 지형 루트는 임포트된 TerrainGenerator이고, 포팅본 MapGenerator를 쓰는 씬도 있어 둘 다 지원한다.
    //
    // 중요한 순서: 지형 생성 -> NavMesh 재구축 -> 그 뒤에야 장수/적 소굴 배치가 돌아야 한다.
    // 그래서 이 컴포넌트는 Awake(실행 순서 -10000)에서 전부 끝내고, 지형 컴포넌트의 Start는 꺼 버린다.
    //
    // 새 게임/불러오기로 시작한 세션이 아니면 아무것도 하지 않는다.
    // (기존 씬을 그대로 Play 하는 기존 검사·작업 흐름의 지형과 NavMesh를 건드리지 않기 위해서다.)
    [DefaultExecutionOrder(-10000)]
    public sealed class HomeMapBuilder : MonoBehaviour
    {
        public const float HomeFlatRadius = 30f;

        public static HomeMapBuilder Instance { get; private set; }

        // 본거지를 중심으로 한 이동 가능 영역. 카메라 한계와 검사에서 읽는다.
        public Bounds WorldBounds { get; private set; }
        public bool Rebuilt { get; private set; }
        public string Status { get; private set; } = "Untouched (scene defaults)";

        private NavMeshDataInstance navInstance;
        private NavMeshData navData;

        private void Awake()
        {
            Instance = this;
            WorldBounds = DefaultBounds();
        }

        // 부트스트랩이 sceneLoaded 시점(씬 Awake 뒤, 첫 Start 앞)에 한 번 호출한다.
        // 새 게임/불러오기로 시작한 세션이 아니면 지형과 NavMesh를 건드리지 않는다.
        public void ApplyFromSession()
        {
            if (!GameSession.Exists || !GameSession.Instance.GameStarted)
            {
                Status = "Untouched (scene defaults)";
                return;
            }
            Apply(GameSession.Instance.Options);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (navInstance.valid) navInstance.Remove();
            if (navData != null) Destroy(navData);
        }

        private static Bounds DefaultBounds()
        {
            // 씬 기본값(기존 카메라 한계와 같은 400 정사각형).
            return new Bounds(new Vector3(200f, 0f, 200f), new Vector3(400f, 100f, 400f));
        }

        // 본거지 중심. 여왕방이 있으면 그 자리를, 없으면 플레이어 건물 평균을 쓴다.
        private static bool TryFindHome(out Vector3 center)
        {
            var queen = FindFirstObjectByType<QueenChamber>();
            if (queen != null) { center = queen.Position; return true; }
            var sum = Vector3.zero;
            var count = 0;
            foreach (var building in FindObjectsByType<BuildingBase>(FindObjectsSortMode.None))
            {
                if (!building.CountsTowardPlayerDefeat) continue;
                sum += building.Position;
                count++;
            }
            center = count > 0 ? sum / count : new Vector3(200f, 0f, 200f);
            return count > 0;
        }

        public void Apply(NewGameOptions options)
        {
            if (options == null) return;
            TryFindHome(out var home);
            var scale = MapSizes.Scale(options.mapSize);

            
            var ported = FindFirstObjectByType<MapGenerator>();
            if (ported == null)
            {
                WorldBounds = ResizeAround(home, DefaultBounds().size.x * scale);
                Status = "No terrain component found; only camera bounds were resized.";
                ApplyCameraBounds();
                return;
            }

            var baseExtent = Mathf.Max(ported.BaseXSize, ported.BaseZSize);
            if (baseExtent < 2) baseExtent = Mathf.RoundToInt(DefaultBounds().size.x);
            var extent = Mathf.Max(40, Mathf.RoundToInt(baseExtent * scale));

            // 시드는 Perlin 샘플 오프셋과 장식물 배치 시드로 들어간다. 같은 시드 + 같은 크기 = 같은 지형.
            var xOffset = Mathf.Abs(options.seed % 9973);
            var zOffset = Mathf.Abs((options.seed / 7 + 1237) % 9973);

            // 지형 원점을 본거지 중심에 맞춰 옮긴다. 작은 맵을 골라도 본거지가 항상 한가운데 남는다.
            var root = ported.transform;
            root.position = new Vector3(home.x - extent * .5f, root.position.y, home.z - extent * .5f);

            {
                ported.Configure(extent, extent, xOffset, zOffset, options.seed);
                ported.SetFlatZone(home, HomeFlatRadius, home.y);
                ported.enabled = false;
                ported.GenerateTerrain();
            }

            WorldBounds = ResizeAround(home, extent);
            Physics.SyncTransforms();
            RebuildNavMesh();
            PlaceHomeThreats(home);
            ApplyCameraBounds();
            Rebuilt = true;
            Status = $"{options.mapSize} ({extent} units) seed {options.seed}; NavMesh rebuilt.";
        }

        private static Bounds ResizeAround(Vector3 home, float extent)
            => new Bounds(new Vector3(home.x, 0f, home.z), new Vector3(extent, 100f, extent));

        private static void PlaceHomeThreats(Vector3 home)
        {
            // Keep the prototype's nearby monster out of the starting worker formation.
            foreach (var monster in FindObjectsByType<AntColony.World.WildMonster>())
            {
                if (Vector3.Distance(monster.Position, home) >= HomeFlatRadius) continue;
                var agent = monster.GetComponent<NavMeshAgent>();
                for (var i = 0; i < 16; i++)
                {
                    var candidate = home + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * 38f;
                    if (!NavMesh.SamplePosition(candidate, out var hit, 8f, NavMesh.AllAreas)
                        || Vector3.Distance(hit.position, home) < HomeFlatRadius) continue;
                    var path = new NavMeshPath();
                    if (!NavMesh.CalculatePath(home, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                    if (agent != null && agent.enabled) agent.Warp(hit.position);
                    else monster.transform.position = hit.position;
                    break;
                }
            }
        }

        // 씬에 구워 둔 NavMesh는 새 지형과 맞지 않으므로 걷어내고 현재 콜라이더로 다시 만든다.
        // 원정 거점은 이 시점 이후(WorldMapManager.Start)에 각자 NavMesh를 더하므로 영향을 받지 않는다.
        private void RebuildNavMesh()
        {
            NavMesh.RemoveAllNavMeshData();
            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();
            var collectBounds = new Bounds(WorldBounds.center, WorldBounds.size + new Vector3(20f, 200f, 20f));
            NavMeshBuilder.CollectSources(collectBounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (!(source.sourceObject is Mesh mesh) || mesh.isReadable || !(source.component is Collider collider)) continue;
                // ponytail: unreadable obstacle meshes use conservative bounds; enable mesh read access if exact outlines are needed.
                var bounds = collider.bounds;
                sources[i] = new NavMeshBuildSource {
                    shape = NavMeshBuildSourceShape.Box, size = bounds.size,
                    transform = Matrix4x4.TRS(bounds.center, Quaternion.identity, Vector3.one),
                    area = 1 // Not Walkable
                };
            }
            navData = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), sources,
                collectBounds, Vector3.zero, Quaternion.identity);
            if (navData != null) navInstance = NavMesh.AddNavMeshData(navData);
        }

        private void ApplyCameraBounds()
        {
            var main = UnityEngine.Camera.main;
            var controller = main != null ? main.GetComponent<AntColony.Camera.IsometricCameraController>() : null;
            if (controller == null) return;
            controller.SetRegion(new Vector3(WorldBounds.center.x, controller.FocusPoint.y, WorldBounds.center.z), WorldBounds);
        }

        // 본거지 화면으로 돌아갈 때 쓰는 기본 영역.
        public static Bounds CurrentWorldBounds => Instance != null ? Instance.WorldBounds : DefaultBounds();
    }
}

