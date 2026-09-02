using UnityEngine;

namespace AntColony.Boss.Telegraph
{
    // SIMUL-TeaamProject(hyeonyeop 브랜치) GroundTelegraph/GroundTelegraphSector.cs 참고 포팅.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GroundTelegraphSector : MonoBehaviour
    {
        [Header("Shape")]
        [Min(0.1f)] public float radius = 6f;
        [Range(1f, 360f)] public float angleDeg = 90f;
        [Range(6, 128)] public int segmentCount = 36;
        [Range(1, 24)] public int ringCount = 10;

        [Header("Ground Sampling")]
        public LayerMask groundMask;
        [Min(1f)] public float castHeight = 50f;
        [Min(0f)] public float surfaceOffset = 0.05f;

        private MeshFilter meshFilter;
        private Mesh mesh;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();

            mesh = new Mesh { name = "GroundTelegraphSector" };
            mesh.MarkDynamic();

            meshFilter.sharedMesh = mesh;
        }

        public void SetData(Vector3 center, Quaternion rotation, float newRadius, float newAngleDeg)
        {
            transform.SetPositionAndRotation(center, rotation);
            radius = newRadius;
            angleDeg = newAngleDeg;
            Build();
        }

        public void Build()
        {
            if (mesh == null)
            {
                mesh = new Mesh { name = "GroundTelegraphSector" };
                mesh.MarkDynamic();
                meshFilter.sharedMesh = mesh;
            }

            var vertsPerRing = segmentCount + 1;
            var vertexCount = (ringCount + 1) * vertsPerRing;

            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var triangles = new int[ringCount * segmentCount * 6];

            var halfAngleRad = angleDeg * 0.5f * Mathf.Deg2Rad;

            var v = 0;
            for (var r = 0; r <= ringCount; r++)
            {
                var r01 = r / (float)ringCount;
                var currentRadius = radius * r01;

                for (var s = 0; s <= segmentCount; s++)
                {
                    var s01 = s / (float)segmentCount;
                    var angleRad = Mathf.Lerp(-halfAngleRad, halfAngleRad, s01);

                    var localOffset = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad)) * currentRadius;
                    var sampleWorldPos = transform.TransformPoint(localOffset);
                    var vertexWorldPos = SampleGround(sampleWorldPos);

                    vertices[v] = transform.InverseTransformPoint(vertexWorldPos);
                    uvs[v] = new Vector2(localOffset.x / (radius * 2f) + 0.5f, localOffset.z / (radius * 2f) + 0.5f);
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
