using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AntColony.Map
{
    // SIMUL-TeaamProject(hyeonyeop 브랜치) TerrainGenerator.cs 참고 포팅.
    // 텍스처 블렌딩 셰이더는 Assets/Shaders/TerrainBlend.shader로 함께 포팅.
    // 물(Water)은 원본의 전용 노멀맵 셰이더 대신 임의의 반투명 Material을 그대로 사용하는 방식으로 단순화함.
    public class MapGenerator : MonoBehaviour
    {
        [SerializeField][Range(2, 200)] private int xSize = 10;
        [SerializeField][Range(2, 200)] private int zSize = 10;

        [SerializeField] private int xOffset;
        [SerializeField] private int zOffset;

        [SerializeField][Range(0.001f, 1f)] private float noiseScale = 0.03f;
        // 계단식 지형(2026-10-19) 뒤로는 본거지 평지가 없을 때(씬 그대로 Play) 기준 지면 높이 = heightMultiplier / 2(옛 평균 높이).
        [SerializeField][Range(0f, 50f)] private float heightMultiplier = 7f;

        // lacunarity^octavesCount는 정점 사이 노이즈 주파수 차이를 지수적으로 키운다.
        // 범위를 벗어나면 인접 정점의 높이가 폭주해 500유닛 이상 벌어진 삼각형(PhysX 콜라이더 에러)이 생긴다.
        [SerializeField][Range(1, 8)] private int octavesCount = 1;
        [SerializeField][Range(1f, 4f)] private float lacunarity = 2f;
        [SerializeField][Range(0f, 1f)] private float persistance = 0.5f;

        [SerializeField] private List<Layer> terrainLayers = new List<Layer> { new Layer() };
        [SerializeField] private Material mat;

        [Header("Water")]
        [SerializeField] private bool generateWater;
        [SerializeField] private Material waterMat;
        [SerializeField][Range(0, 1)] private float waterHeight = 0.3f;

        [Header("Objects")]
        [SerializeField] private List<SpawnObject> spawnObjects = new List<SpawnObject>();

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        [SerializeField, HideInInspector] private Mesh mesh;

        [SerializeField, HideInInspector] private GameObject waterObject;
        [SerializeField, HideInInspector] private List<GameObject> spawnedObjects = new List<GameObject>();
        [SerializeField, HideInInspector] private Material terrainMaterial;
        [SerializeField, HideInInspector] private Material materialSource;
        [SerializeField, HideInInspector] private Texture2DArray terrainTextures;

        private static void ReleaseObject(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
            ReleaseObject(mesh);
            ReleaseObject(terrainMaterial);
            ReleaseObject(terrainTextures);
            foreach (var clone in foliageClones.Values) ReleaseObject(clone);
            ReleaseWater();
        }

        private void ReleaseWater()
        {
            if (waterObject == null) return;
            var filter = waterObject.GetComponent<MeshFilter>();
            if (filter != null) ReleaseObject(filter.sharedMesh);
            ReleaseObject(waterObject);
            waterObject = null;
        }

        // 새 게임 옵션을 받는 창구. 임포트된 TerrainGenerator와 같은 규약을 쓴다.
        public int BaseXSize => xSize;
        public int BaseZSize => zSize;

        // 홍수 시작/복원 시에만 조회한다. 물 평면 아래 지형 정점으로 물가를 판정한다.
        public bool NearWater(Vector3 point, float radius)
        {
            if (!generateWater || waterObject == null || mesh == null) return false;
            var waterY = waterObject.transform.position.y;
            foreach (var vertex in mesh.vertices)
            {
                var p = transform.TransformPoint(vertex);
                if (p.y <= waterY && new Vector2(p.x - point.x, p.z - point.z).sqrMagnitude <= radius * radius) return true;
            }
            return false;
        }

        // 바이옴 스타일(2026-10-03): 바닥·장식·물 설정을 갈아끼운다. 에셋이 빠진 칸은 버리고, 다 비면 씬 기본값을 유지한다.
        private bool styled;
        // 계절 색: 잎·풀 재질은 원본 대신 복제본을 쓰고(원본 에셋은 그대로), 복제본 색만 바꾼다. 재질 수가 적어 매 프레임 바꿔도 싸다.
        private readonly Dictionary<Material, Material> foliageClones = new Dictionary<Material, Material>();
        public void ApplyStyle(BiomeMapStyle style)
        {
            if (style == null) return;
            var layers = style.layers.FindAll(l => l.texture != null);
            var spawns = style.spawns.FindAll(s => s.prefab != null);
            if (layers.Count > 0) terrainLayers = layers;
            if (spawns.Count > 0) { spawnObjects = spawns; styled = true; }
            generateWater = style.water && (style.waterMaterial != null || waterMat != null);
            waterHeight = style.waterHeight;
            if (style.waterMaterial != null) waterMat = style.waterMaterial;
            snowGround = style.snowGround; leafGround = style.leafGround;
        }
        private Texture2D snowGround, leafGround;

        // 물 수위(월드 y). 물이 없으면 NaN. 물속 이동 감속·낚시터 위치·홍수에서 읽는다.
        public static float WaterLevel { get; private set; } = float.NaN;
        public const float WaterMoveMultiplier = .6f; // 물속은 느리게 지나간다(2026-10-03, 잠정)
        public static bool InWater(Vector3 p) => !float.IsNaN(WaterLevel) && p.y < WaterLevel - .05f;

        // 기준점에서 가까운 물가(웅덩이에서 2칸 안의 평지) 한 곳. minDistance 안은 건너뛴다(본거지 평지).
        public bool TryFindShore(Vector3 near, float minDistance, out Vector3 shore)
        {
            shore = default;
            if (float.IsNaN(WaterLevel) || mesh == null || terrace == null) return false;
            var best = float.MaxValue; var vertices = mesh.vertices; var w = xSize + 1;
            for (var i = 0; i < vertices.Length; i++)
            {
                if (terrace.clear[i]) continue;
                int x = i % w, z = i / w; var nearPit = false;
                for (var dz = -2; dz <= 2 && !nearPit; dz++)
                    for (var dx = -2; dx <= 2 && !nearPit; dx++)
                    {
                        int px = x + dx, pz = z + dz;
                        nearPit = px >= 0 && pz >= 0 && px <= xSize && pz <= zSize && terrace.heights[pz * w + px] < 0;
                    }
                if (!nearPit) continue;
                var p = transform.TransformPoint(vertices[i]);
                var d = new Vector2(p.x - near.x, p.z - near.z).sqrMagnitude;
                if (d < minDistance * minDistance || d >= best) continue;
                best = d; shore = p;
            }
            return best < float.MaxValue;
        }

        private bool hasFlatZone;
        private Vector3 flatCenter;
        private float flatRadius;
        private float flatHeight;
        private int spawnSeedBase;
        private float[] noise01;               // 정점별 정규화 노이즈(장식 분포)
        private TerraceHeights.Result terrace; // 정점별 단계 높이(건설 평지 판정·물가·수면)
        private float baseY;                   // 기준 단계(저지대)의 로컬 높이

        public void Configure(int newXSize, int newZSize, int newXOffset, int newZOffset, int seed)
        {
            xSize = Mathf.Clamp(newXSize, 2, 640);
            zSize = Mathf.Clamp(newZSize, 2, 640);
            xOffset = newXOffset;
            zOffset = newZOffset;
            spawnSeedBase = seed;
        }

        public void SetFlatZone(Vector3 worldCenter, float radius, float worldHeight)
        {
            hasFlatZone = radius > 0f;
            flatCenter = worldCenter;
            flatRadius = radius;
            flatHeight = worldHeight;
        }

        private void Start()
        {
            if (SelectSingleTerrain() != this) return;
            GenerateTerrain();
        }

        // 사본의 렌더러뿐 아니라 자식 장식과 콜라이더도 NavMesh 수집 전에 제외한다.
        public static MapGenerator SelectSingleTerrain()
        {
            var maps = FindObjectsByType<MapGenerator>(FindObjectsSortMode.InstanceID);
            if (maps.Length == 0) return null;
            var selected = Active != null && Active.gameObject.activeInHierarchy ? Active : maps[0];
            foreach (var map in maps)
                if (map != selected) map.gameObject.SetActive(false);
            return selected;
        }

        public void GenerateTerrain()
        {
            // 새 게임으로 만든 본거지 지형(평지 있음)이 기준이다. 씬에 켜진 다른 지형이 나중에 Start로 생성돼도 빼앗지 않는다.
            var authoritative = Active == null || Active == this || hasFlatZone || !Active.hasFlatZone;
            if (authoritative) Active = this;
            CreateMesh();
            GenerateMesh();
            GenerateTexture();
            var randomState = Random.state;
            try { SpawnObjects(); }
            finally { Random.state = randomState; }

            if (generateWater)
                GenerateWater();
            else if (waterObject != null)
                ReleaseWater();
            if (authoritative) WaterLevel = generateWater && waterObject != null ? waterObject.transform.position.y : float.NaN;
        }

        private void SpawnObjects()
        {
            foreach (var obj in spawnedObjects)
                ReleaseObject(obj);
            spawnedObjects.Clear();
            // 씬에 구워져 목록에서 빠진 예전 장식(Tree_01 등 렌더러가 있는 자식)도 지운다. 물·렌더러 없는 관리용 자식은 둔다.
            // ponytail: 렌더러 유무로 장식을 가린다. 지형 밑에 보이는 소품을 직접 붙일 일이 생기면 장식에 표식 컴포넌트를 단다.
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject;
                if (old != waterObject && old.GetComponentInChildren<Renderer>(true) != null) { old.SetActive(false); ReleaseObject(old); } // 끄고 지워야 같은 프레임 NavMesh에서 빠진다
            }

            var vertices = mesh.vertices;

            // 간격 검사는 1m 격자로(장식 종류가 많아도 느려지지 않게).
            var occupied = new HashSet<Vector2Int>();
            foreach (var clone in foliageClones.Values) ReleaseObject(clone);
            foliageClones.Clear();

            for (var z = 0; z <= zSize; z++)
            {
                for (var x = 0; x <= xSize; x++)
                {
                    var index = z * (xSize + 1) + x;
                    var vertex = vertices[index];
                    if (terrace.clear[index]) continue; // 웅덩이·절벽·경사로는 비워 둔다(경사로가 나무로 막히지 않게)
                    // 계단식 지형은 높이가 몇 단계뿐이라 장식 분포는 높이 대신 원래 노이즈 값으로 고른다.
                    var heightNormalized = noise01[index];

                    for (var s = 0; s < spawnObjects.Count; s++)
                    {
                        var spawnObj = spawnObjects[s];
                        if (spawnObj.prefab == null) continue;
                        if ((!styled || spawnObj.solid) && !terrace.open[index]) continue; // 콜라이더가 남는 장식은 탁 트인 평지에만
                        if (heightNormalized < spawnObj.minHeight || heightNormalized > spawnObj.maxHeight) continue;

                        // prefab 인스턴스 ID는 재실행마다 달라져 같은 시드에서도 배치가 흔들린다.
                        // 목록 순번과 맵 시드만 써서 재현 가능하게 한다.
                        var seed = (x + xOffset) * 73856093 ^ (z + zOffset) * 19349663 ^ (s + 1) * 83492791 ^ spawnSeedBase;
                        Random.InitState(seed);

                        if (Random.value > spawnObj.spawnChance) continue;

                        var worldPos = transform.TransformPoint(vertex);
                        if (hasFlatZone
                            && new Vector2(worldPos.x - flatCenter.x, worldPos.z - flatCenter.z).magnitude < flatRadius) continue;

                        if (TooClose(occupied, worldPos, spawnObj.minDistanceBetween)) continue;

#if UNITY_EDITOR
                        var obj = !Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(spawnObj.prefab)
                            ? (GameObject)PrefabUtility.InstantiatePrefab(spawnObj.prefab)
                            : Instantiate(spawnObj.prefab);
                        obj.transform.position = worldPos;
#else
                        var obj = Instantiate(spawnObj.prefab, worldPos, Quaternion.identity);
#endif
                        obj.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        var randomScale = Random.Range(spawnObj.minScale, spawnObj.maxScale);
                        obj.transform.localScale = Vector3.one * randomScale;
                        obj.transform.parent = transform;
                        var renderers = obj.GetComponentsInChildren<MeshRenderer>();
                        if (renderers.Length > 0)
                        {
                            var bottom = renderers.Min(r => r.bounds.min.y);
                            obj.transform.position += Vector3.up * (worldPos.y - bottom);
                        }
                        spawnedObjects.Add(obj);
                        occupied.Add(new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.z)));
                        if (styled && !spawnObj.solid) foreach (var c in obj.GetComponentsInChildren<Collider>()) DestroyImmediate(c); // 같은 프레임 NavMesh 재구축 전에 빠져야 한다.
                        if (spawnObj.foliage) foreach (var r in obj.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(FoliageClone).ToArray();
                    }
                }
            }
        }

        private Material FoliageClone(Material original)
        {
            if (original == null) return null;
            if (foliageClones.TryGetValue(original, out var clone)) return clone;
            // 계절 나무 셰이더(잎만 계절색, 겨울 윗면 눈)로 바꾼다. 원래 텍스처·색은 그대로 옮긴다.
            var season = Resources.Load<Material>("SeasonFoliage");
            if (season == null) return original;
            clone = new Material(season) { name = original.name + " (season)" };
            clone.SetTexture("_BaseMap", original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : original.HasProperty("_MainTex") ? original.GetTexture("_MainTex") : null);
            clone.SetColor("_BaseColor", original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : original.HasProperty("_Color") ? original.GetColor("_Color") : Color.white);
            clone.SetFloat("_Evergreen", original.name.Contains("Pine") ? 1 : 0); // 소나무는 가을에도 초록
            foliageClones[original] = clone;
            return clone;
        }

        private static bool TooClose(HashSet<Vector2Int> occupied, Vector3 p, float minDistance)
        {
            var r = Mathf.CeilToInt(minDistance); var cell = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z));
            for (var dx = -r; dx <= r; dx++) for (var dz = -r; dz <= r; dz++)
                if (occupied.Contains(new Vector2Int(cell.x + dx, cell.y + dz)) && new Vector2(dx, dz).magnitude < minDistance) return true;
            return false;
        }

        private void GenerateWater()
        {
            ReleaseWater();

            // 웅덩이 칸(꼭짓점 하나라도 웅덩이)만 수면을 깐다. 웅덩이가 없으면 물도 없다.
            var w = xSize + 1;
            var vertices = new List<Vector3>(); var uvs = new List<Vector2>(); var triangles = new List<int>();
            for (var z = 0; z < zSize; z++)
                for (var x = 0; x < xSize; x++)
                {
                    var i = z * w + x;
                    if (terrace.heights[i] >= 0 && terrace.heights[i + 1] >= 0 && terrace.heights[i + w] >= 0 && terrace.heights[i + w + 1] >= 0) continue;
                    var b = vertices.Count;
                    foreach (var (cx, cz) in new[] { (x, z), (x + 1, z), (x, z + 1), (x + 1, z + 1) })
                    {
                        vertices.Add(new Vector3(cx, 0f, cz));
                        uvs.Add(new Vector2((float)cx / xSize, (float)cz / zSize)); // 옛 전체 수면과 같은 UV
                    }
                    triangles.AddRange(new[] { b, b + 2, b + 1, b + 2, b + 3, b + 1 });
                }
            if (vertices.Count == 0) return;

            waterObject = new GameObject("Water");
            waterObject.transform.SetParent(transform, false);
            waterObject.transform.localPosition = new Vector3(0f, baseY - TerraceHeights.WaterDepth, 0f);

            var waterMesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            waterMesh.SetVertices(vertices); waterMesh.SetUVs(0, uvs); waterMesh.SetTriangles(triangles, 0);
            waterMesh.RecalculateNormals();
            waterObject.AddComponent<MeshFilter>().mesh = waterMesh;
            waterObject.AddComponent<MeshRenderer>().sharedMaterial = waterMat;
        }

        // NavMesh 재구축에서 수면을 '못 걷는 곳'으로 넣는다(웅덩이 바닥도 높이 여유가 없어 함께 빠진다).
        public Mesh WaterMesh => waterObject != null ? waterObject.GetComponent<MeshFilter>().sharedMesh : null;
        public Matrix4x4 WaterMatrix => waterObject != null ? waterObject.transform.localToWorldMatrix : Matrix4x4.identity;

        public static MapGenerator Active { get; private set; }
        // 건물 바닥 밑 정점이 모두 같은 단계의 평지인가(절벽·경사로·웅덩이 위 건설 금지). 지형이 없으면 막지 않는다.
        public static bool FlatAt(Vector3 center, Vector3 extents)
        {
            var t = Active;
            if (t == null || t.terrace == null) return true;
            var min = t.transform.InverseTransformPoint(center - extents);
            var max = t.transform.InverseTransformPoint(center + extents);
            int x0 = Mathf.Clamp(Mathf.FloorToInt(min.x + .01f), 0, t.xSize), x1 = Mathf.Clamp(Mathf.CeilToInt(max.x - .01f), 0, t.xSize);
            int z0 = Mathf.Clamp(Mathf.FloorToInt(min.z + .01f), 0, t.zSize), z1 = Mathf.Clamp(Mathf.CeilToInt(max.z - .01f), 0, t.zSize);
            var first = t.terrace.heights[z0 * (t.xSize + 1) + x0];
            if (first < 0 || first != Mathf.Round(first)) return false;
            for (var z = z0; z <= z1; z++)
                for (var x = x0; x <= x1; x++)
                    if (t.terrace.heights[z * (t.xSize + 1) + x] != first) return false;
            return true;
        }

        // 먼 길 경유지(NavRoute): 지형 격자 대략 경로에서 every칸마다 한 점(월드 좌표). 출발·도착점은 넣지 않는다.
        public bool CoarseRoute(Vector3 from, Vector3 to, int stride, int every, List<Vector3> points)
        {
            points.Clear();
            if (terrace == null) return false;
            var w = xSize + 1;
            int Index(Vector3 p)
            {
                var local = transform.InverseTransformPoint(p);
                return Mathf.Clamp(Mathf.RoundToInt(local.z), 0, zSize) * w + Mathf.Clamp(Mathf.RoundToInt(local.x), 0, xSize);
            }
            var cells = new List<int>();
            if (!TerraceHeights.Route(terrace.heights, w, Index(from), Index(to), stride, cells)) return false;
            for (var k = every; k < cells.Count - 1; k += every)
            {
                var i = cells[k];
                points.Add(transform.TransformPoint(new Vector3(i % w, baseY + terrace.heights[i] * TerraceHeights.LevelHeight, i / w)));
            }
            return true;
        }

        private void CreateMesh()
        {
            if (GetComponent<MeshFilter>() == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();
            if (GetComponent<MeshRenderer>() == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            if (GetComponent<MeshCollider>() == null)
                meshCollider = gameObject.AddComponent<MeshCollider>();

            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();

            ReleaseObject(mesh);
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // 65535 버텍스 제한 해제
            meshFilter.mesh = mesh;
            if (terrainMaterial == null || materialSource != mat)
            {
                ReleaseObject(terrainMaterial);
                materialSource = mat;
                terrainMaterial = mat != null ? new Material(mat) : null;
                if (terrainMaterial != null && terrainMaterial.shader.name != "AntColony/TerrainBlend")
                {
                    terrainMaterial.shader = Shader.Find("AntColony/TerrainBlend");
                    terrainMaterial.SetFloat("_textureScale", 8f);
                }
            }
            meshRenderer.sharedMaterial = terrainMaterial;
        }

        private void GenerateMesh()
        {
            var vertices = new Vector3[(xSize + 1) * (zSize + 1)];
            noise01 = new float[vertices.Length];
            var i = 0;
            for (var z = 0; z <= zSize; z++)
            {
                for (var x = 0; x <= xSize; x++)
                {
                    float value = 0f;
                    for (var o = 0; o < octavesCount; o++)
                    {
                        var frequency = Mathf.Pow(lacunarity, o);
                        var amplitude = Mathf.Pow(persistance, o);
                        value += Mathf.PerlinNoise((x + xOffset) * noiseScale * frequency, (z + zOffset) * noiseScale * frequency) * amplitude;
                    }
                    noise01[i++] = value;
                }
            }
            // 0~1로 펴서 물 높이(waterHeight)·장식 높이 범위를 옛 지형과 같은 뜻으로 쓴다.
            float lo = Mathf.Min(noise01), hi = Mathf.Max(noise01);
            for (i = 0; i < noise01.Length; i++) noise01[i] = Mathf.InverseLerp(lo, hi, noise01[i]);

            // 본거지 평지: 반경 안은 기준 단계로 고정, 1.3배까지는 웅덩이 금지.
            baseY = hasFlatZone ? flatHeight - transform.position.y : heightMultiplier * .5f;
            terrace = TerraceHeights.Build(noise01, xSize, zSize, waterHeight, generateWater, (x, z) =>
            {
                if (!hasFlatZone) return 0;
                var world = transform.TransformPoint(new Vector3(x, 0f, z));
                var planar = new Vector2(world.x - flatCenter.x, world.z - flatCenter.z).magnitude;
                return planar < flatRadius ? 2 : planar < flatRadius * 1.3f ? 1 : 0;
            }, spawnSeedBase ^ xOffset * 31 ^ zOffset);
            i = 0;
            for (var z = 0; z <= zSize; z++)
                for (var x = 0; x <= xSize; x++, i++)
                    vertices[i] = new Vector3(x, baseY + terrace.heights[i] * TerraceHeights.LevelHeight, z);

            var triangles = new int[xSize * zSize * 6];
            var vertex = 0;
            var triangleIndex = 0;
            for (var z = 0; z < zSize; z++)
            {
                for (var x = 0; x < xSize; x++)
                {
                    int a = vertex, b = vertex + 1, c = vertex + xSize + 1, d = vertex + xSize + 2;
                    // 높이 차가 작은 쪽 대각선으로 나눈다. 한 모서리만 다른 칸(대각선 절벽)이 톱니 대신 45° 선이 된다(2026-10-22).
                    if (Mathf.Abs(vertices[a].y - vertices[d].y) < Mathf.Abs(vertices[b].y - vertices[c].y))
                    {
                        triangles[triangleIndex + 0] = a; triangles[triangleIndex + 1] = c; triangles[triangleIndex + 2] = d;
                        triangles[triangleIndex + 3] = a; triangles[triangleIndex + 4] = d; triangles[triangleIndex + 5] = b;
                    }
                    else
                    {
                        triangles[triangleIndex + 0] = a; triangles[triangleIndex + 1] = c; triangles[triangleIndex + 2] = b;
                        triangles[triangleIndex + 3] = b; triangles[triangleIndex + 4] = c; triangles[triangleIndex + 5] = d;
                    }
                    vertex++;
                    triangleIndex += 6;
                }
                vertex++;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            meshCollider.sharedMesh = mesh;
        }

        // 바이옴 바닥 색조(GenerateTerrain 뒤에 부른다).
        public void SetTint(Color tint)
        {
            if (terrainMaterial == null) return;
            RestoreTerrainLayers();
            terrainMaterial.SetColor("_Tint", tint);
        }
        // 계절 바닥 덮임(0~1): 겨울 눈, 가을 낙엽. 텍스처는 바이옴 스타일에서 오며, 없으면 덮지 않는다.
        public void SetSeasonGround(float snow, float leaves)
        {
            if (terrainMaterial == null) return;
            terrainMaterial.SetTexture("_SnowTex", snowGround); terrainMaterial.SetTexture("_LeafTex", leafGround);
            terrainMaterial.SetFloat("_Snow", snowGround != null ? snow : 0); terrainMaterial.SetFloat("_Leaves", leafGround != null ? leaves : 0);
        }

        // SetTint을 거치지 않는 경로(메뉴 배경 지형 등)도 그리기 직전에 층 정보를 다시 연결한다.
        // 층 정보가 지워진 머티리얼에서 값을 읽으면 오류가 나므로 확인 없이 프레임당 한 번 연결한다.
        private int boundFrame = -1;
        private void OnWillRenderObject()
        {
            if (boundFrame == Time.frameCount || terrainTextures == null) return;
            boundFrame = Time.frameCount; BindTerrainLayers();
        }

        private void RestoreTerrainLayers()
        {
            // 셰이더 재임포트는 Properties 밖의 층 정보를 지운다. 텍스처를 재생성하지 않고 복구한다.
            // 지워진 값을 읽으면 오류가 나므로 확인 없이 다시 연결한다(텍스처는 재생성하지 않음).
            if (terrainMaterial != null && terrainTextures != null) BindTerrainLayers();
        }

        private void BindTerrainLayers()
        {
            if (terrainMaterial == null) return;
            // 텍스처 높이 범위는 웅덩이 바닥~언덕2로 고정한다. 웅덩이 없는 맵에서도 저지대가 맨 아래(물가) 층으로 칠해지지 않고
            // 단계마다 다른 지면층이 깔린다(저지대 ⅓, 언덕1 ⅔, 언덕2 꼭대기).
            var minTerrainHeight = baseY - TerraceHeights.LevelHeight + transform.position.y - 0.1f;
            var maxTerrainHeight = baseY + 2 * TerraceHeights.LevelHeight + transform.position.y + 0.1f;

            terrainMaterial.SetFloat("minTerrainHeight", minTerrainHeight);
            terrainMaterial.SetFloat("maxTerrainHeight", maxTerrainHeight);
            terrainMaterial.SetFloat("terraceBase", baseY + transform.position.y);
            terrainMaterial.SetFloat("terraceStep", TerraceHeights.LevelHeight);

            var layersCount = Mathf.Min(terrainLayers.Count, 32);
            terrainMaterial.SetInt("numTextures", layersCount);
            var heights = new float[32];
            for (var i = 0; i < layersCount; i++) heights[i] = terrainLayers[i].startHeight;
            terrainMaterial.SetFloatArray("terrainHeights", heights);
            terrainMaterial.SetTexture("terrainTextures", terrainTextures);
        }

        private void GenerateTexture()
        {
            if (terrainMaterial == null) return;
            var layersCount = Mathf.Min(terrainLayers.Count, 32);
            ReleaseObject(terrainTextures);
            terrainTextures = null;
            if (layersCount == 0) { BindTerrainLayers(); return; }
            terrainTextures = new Texture2DArray(512, 512, layersCount, TextureFormat.RGBA32, true);
            terrainTextures.filterMode = FilterMode.Trilinear;
            terrainTextures.anisoLevel = 8;
            terrainTextures.wrapMode = TextureWrapMode.Repeat;
            var resized = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            var target = RenderTexture.GetTemporary(512, 512);
            var previous = RenderTexture.active;
            try
            {
                for (var i = 0; i < layersCount; i++)
                {
                    Graphics.Blit(terrainLayers[i].texture != null ? terrainLayers[i].texture : Texture2D.whiteTexture, target);
                    RenderTexture.active = target;
                    resized.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                    terrainTextures.SetPixels(resized.GetPixels(), i);
                }
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                ReleaseObject(resized);
            }
            terrainTextures.Apply();
            BindTerrainLayers();
        }

        [System.Serializable]
        public class Layer
        {
            public Texture2D texture;
            [Range(0, 1)] public float startHeight;
        }

        [System.Serializable]
        public class SpawnObject
        {
            public GameObject prefab;
            [Range(0, 1)] public float minHeight = 0f;
            [Range(0, 1)] public float maxHeight = 1f;
            [Range(0, 1)] public float spawnChance = 0.05f;
            public float minScale = 0.8f;
            public float maxScale = 1.2f;
            public float minDistanceBetween = 1.5f;
            public bool foliage; // 계절 색을 입히는 잎·풀(2026-10-03)
            public bool solid;   // 콜라이더를 남겨 길을 막는 큰 장식(나무·바위). 아니면 콜라이더를 지워 지나다닌다
        }
    }
}
