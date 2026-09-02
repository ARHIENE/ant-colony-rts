using UnityEngine;

namespace AntColony.Boss.Telegraph
{
    // SIMUL-TeaamProject(hyeonyeop 브랜치) GroundTelegraph/GroundTelegraphCircle.cs 참고 포팅.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GroundTelegraphCircle : MonoBehaviour
    {
        [Header("Shape")]
        [Min(0.1f)] public float radius = 5f;
        [Range(12, 128)] public int segmentCount = 48;
        [Range(1, 24)] public int ringCount = 10;

        [Header("Ground Sampling")]
        public LayerMask groundMask;
        [Min(1f)] public float castHeight = 50f;
        [Min(0f)] public float surfaceOffset = 0.05f;

        [Header("Debug")]
        public bool rebuildEveryFrame;

        private MeshFilter meshFilter;
        private Mesh mesh;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();

            mesh = new Mesh { name = "GroundTelegraphCircle" };
            mesh.MarkDynamic();

            meshFilter.sharedMesh = mesh;
            Build();
        }

        private void Update()
        {
            if (rebuildEveryFrame) Build();
        }

        public void SetCenterAndRadius(Vector3 center, float newRadius)
        {
            transform.position = center;
            radius = newRadius;
            Build();
        }

        public void Build()
        {
            if (mesh == null)
            {
                mesh = new Mesh { name = "GroundTelegraphCircle" };
                mesh.MarkDynamic();

                if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
            }

            var vertsPerRing = segmentCount + 1;
            var vertexCount = (ringCount + 1) * vertsPerRing;

            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var triangles = new int[ringCount * segmentCount * 6];

            var v = 0;

            for (var r = 0; r <= ringCount; r++)
            {
                var r01 = r / (float)ringCount;
                var currentRadius = radius * r01;

                for (var s = 0; s <= segmentCount; s++)
                {
                    var angle = s / (float)segmentCount * Mathf.PI * 2f;

                    var flatOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * currentRadius;
                    var sampleWorldPos = transform.position + flatOffset;
                    var vertexWorldPos = SampleGround(sampleWorldPos);

                    vertices[v] = transform.InverseTransformPoint(vertexWorldPos);
                    uvs[v] = new Vector2(flatOffset.x / (radius * 2f) + 0.5f, flatOffset.z / (radius * 2f) + 0.5f);
                    v++;
                }
            }

            var t = 0;
            for (var r = 0; r < ringCount; r++)
            {
                var ringStart = r * vertsPerRing;
                var nextRingStart = (r + 1) * vertsPerRing;

                for (var s = 0; s < segmentCount; s++)
                {
                    var a = ringStart + s;
                    var b = nextRingStart + s;
                    var c = ringStart + s + 1;
                    var d = nextRingStart + s + 1;

                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = c;

                    triangles[t++] = c;
                    triangles[t++] = b;
                    triangles[t++] = d;
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }

        private Vector3 SampleGround(Vector3 sampleWorldPos)
        {
            var rayOrigin = sampleWorldPos + Vector3.up * castHeight;
            var rayDistance = castHeight * 2f;

            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
                return hit.point + hit.normal * surfaceOffset;

            return sampleWorldPos + Vector3.up * surfaceOffset;
        }
    }
}
