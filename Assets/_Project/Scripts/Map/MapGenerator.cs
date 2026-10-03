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
        public readonly List<(Material material, Color baseColor)> Foliage = new List<(Material, Color)>();
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
        }

        // 물 수위(월드 y). 물이 없으면 NaN. 물속 이동 감속·낚시터 위치·홍수에서 읽는다.
        public static float WaterLevel { get; private set; } = float.NaN;
        public const float WaterMoveMultiplier = .6f; // 물속은 느리게 지나간다(2026-10-03, 잠정)
        public static bool InWater(Vector3 p) => !float.IsNaN(WaterLevel) && p.y < WaterLevel - .05f;

        // 기준점에서 가까운 물가(물 바로 위 땅) 한 곳. minDistance 안은 건너뛴다(본거지 평지).
        public bool TryFindShore(Vector3 near, float minDistance, out Vector3 shore)
        {
            shore = default;
            if (float.IsNaN(WaterLevel) || mesh == null) return false;
            var best = float.MaxValue; var vertices = mesh.vertices;
            foreach (var v in vertices)
            {
                var p = transform.TransformPoint(v);
                if (p.y < WaterLevel || p.y > WaterLevel + .6f) continue;
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
            GenerateTerrain();
        }

        public void GenerateTerrain()
        {
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
            WaterLevel = generateWater && waterObject != null ? waterObject.transform.position.y : float.NaN;
        }

        private void SpawnObjects()
        {
            foreach (var obj in spawnedObjects)
                ReleaseObject(obj);
            spawnedObjects.Clear();
            // 씬에 구워져 목록에서 빠진 예전 장식도 지운다(물은 따로 관리).
            for (var i = transform.childCount - 1; i >= 0; i--)
                if (transform.GetChild(i).gameObject != waterObject) { var old = transform.GetChild(i).gameObject; old.SetActive(false); ReleaseObject(old); } // 끄고 지워야 같은 프레임 NavMesh에서 빠진다

            var minH = mesh.bounds.min.y;
            var maxH = mesh.bounds.max.y;
            var vertices = mesh.vertices;

            // 간격 검사는 1m 격자로(장식 종류가 많아도 느려지지 않게).
            var occupied = new HashSet<Vector2Int>();
            foreach (var clone in foliageClones.Values) ReleaseObject(clone);
            foliageClones.Clear(); Foliage.Clear();

            for (var z = 0; z <= zSize; z++)
            {
                for (var x = 0; x <= xSize; x++)
                {
                    var index = z * (xSize + 1) + x;
                    var vertex = vertices[index];
                    var heightNormalized = Mathf.InverseLerp(minH, maxH, vertex.y);

                    for (var s = 0; s < spawnObjects.Count; s++)
                    {
                        var spawnObj = spawnObjects[s];
                        if (spawnObj.prefab == null) continue;
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
            clone = new Material(original) { name = original.name + " (season)" };
            foliageClones[original] = clone;
            Foliage.Add((clone, clone.HasProperty("_BaseColor") ? clone.GetColor("_BaseColor") : Color.white));
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

            waterObject = new GameObject("Water");
            waterObject.transform.SetParent(transform, false);

            var minH = mesh.bounds.min.y;
            var maxH = mesh.bounds.max.y;
            var waterY = Mathf.Lerp(minH, maxH, waterHeight);
            // 본거지 평지는 물에 잠기지 않게 그보다 낮게 둔다.
            if (hasFlatZone) waterY = Mathf.Min(waterY, flatHeight - transform.position.y - .5f);

            waterObject.transform.localPosition = new Vector3(
                xSize / 2f,
                waterY,
                zSize / 2f);
            waterObject.transform.localScale = new Vector3(xSize, 1f, zSize);

            var mf = waterObject.AddComponent<MeshFilter>();
            var mr = waterObject.AddComponent<MeshRenderer>();
            mf.mesh = CreatePlaneMesh();
            mr.sharedMaterial = waterMat;
        }

        private Mesh CreatePlaneMesh()
        {
            var planeMesh = new Mesh();
            var vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f)
            };
            var triangles = new[] { 0, 2, 1, 2, 3, 1 };
            var uvs = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            planeMesh.vertices = vertices;
            planeMesh.triangles = triangles;
            planeMesh.uv = uvs;
            planeMesh.RecalculateNormals();
            return planeMesh;
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
            }
            meshRenderer.sharedMaterial = terrainMaterial;
        }

        private void GenerateMesh()
        {
            var vertices = new Vector3[(xSize + 1) * (zSize + 1)];
            var i = 0;
            for (var z = 0; z <= zSize; z++)
            {
                for (var x = 0; x <= xSize; x++)
                {
                    float yPos = 0f;
                    for (var o = 0; o < octavesCount; o++)
                    {
                        var frequency = Mathf.Pow(lacunarity, o);
                        var amplitude = Mathf.Pow(persistance, o);
                        yPos += Mathf.PerlinNoise((x + xOffset) * noiseScale * frequency, (z + zOffset) * noiseScale * frequency) * amplitude;
                    }
                    yPos *= heightMultiplier;
                    // 본거지 평탄화. 반경 안은 원래 높이로 눌러 두고 바깥에서 지형으로 되돌아간다.
                    if (hasFlatZone)
                    {
                        var world = transform.TransformPoint(new Vector3(x, 0f, z));
                        var planar = new Vector2(world.x - flatCenter.x, world.z - flatCenter.z).magnitude;
                        var blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(flatRadius, flatRadius * 1.5f, planar));
                        yPos = Mathf.Lerp(flatHeight - transform.position.y, yPos, blend);
                    }
                    vertices[i] = new Vector3(x, yPos, z);
                    i++;
                }
            }

            var triangles = new int[xSize * zSize * 6];
            var vertex = 0;
            var triangleIndex = 0;
            for (var z = 0; z < zSize; z++)
            {
                for (var x = 0; x < xSize; x++)
                {
                    triangles[triangleIndex + 0] = vertex + 0;
                    triangles[triangleIndex + 1] = vertex + xSize + 1;
                    triangles[triangleIndex + 2] = vertex + 1;
                    triangles[triangleIndex + 3] = vertex + 1;
                    triangles[triangleIndex + 4] = vertex + xSize + 1;
                    triangles[triangleIndex + 5] = vertex + xSize + 2;
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
        public void SetTint(Color tint) { if (terrainMaterial != null) terrainMaterial.SetColor("_Tint", tint); }

        private void GenerateTexture()
        {
            if (terrainMaterial == null) return;
            var minTerrainHeight = mesh.bounds.min.y + transform.position.y - 0.1f;
            var maxTerrainHeight = mesh.bounds.max.y + transform.position.y + 0.1f;

            terrainMaterial.SetFloat("minTerrainHeight", minTerrainHeight);
            terrainMaterial.SetFloat("maxTerrainHeight", maxTerrainHeight);

            var layersCount = Mathf.Min(terrainLayers.Count, 32);
            terrainMaterial.SetInt("numTextures", layersCount);
            ReleaseObject(terrainTextures);
            terrainTextures = null;
            if (layersCount == 0) return;

            var heights = new float[32];
            for (var i = 0; i < layersCount; i++) heights[i] = terrainLayers[i].startHeight;
            terrainMaterial.SetFloatArray("terrainHeights", heights);

            terrainTextures = new Texture2DArray(512, 512, layersCount, TextureFormat.RGBA32, true);
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
            terrainMaterial.SetTexture("terrainTextures", terrainTextures);
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
