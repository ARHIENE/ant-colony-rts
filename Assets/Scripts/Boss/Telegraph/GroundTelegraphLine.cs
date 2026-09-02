using UnityEngine;

namespace AntColony.Boss.Telegraph
{
    // SIMUL-TeaamProject(hyeonyeop 브랜치) GroundTelegraph/GroundTelegraphLine.cs 참고 포팅.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GroundTelegraphLine : MonoBehaviour
    {
        [Header("Shape")]
        [Min(0.1f)] public float length = 8f;
        [Min(0.1f)] public float width = 3f;
        [Range(1, 40)] public int lengthSegments = 16;
        [Range(1, 20)] public int widthSegments = 6;

        [Header("Ground Sampling")]
        public LayerMask groundMask;
        [Min(1f)] public float castHeight = 50f;
        [Min(0f)] public float surfaceOffset = 0.05f;

        private MeshFilter meshFilter;
        private Mesh mesh;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();

            mesh = new Mesh { name = "GroundTelegraphLine" };
            mesh.MarkDynamic();

            meshFilter.sharedMesh = mesh;
        }

        public void SetData(Vector3 origin, Quaternion rotation, float newLength, float newWidth)
        {
            transform.SetPositionAndRotation(origin, rotation);
            length = newLength;
            width = newWidth;
            Build();
        }

        public void Build()
        {
            if (mesh == null)
            {
                mesh = new Mesh { name = "GroundTelegraphLine" };
                mesh.MarkDynamic();
                meshFilter.sharedMesh = mesh;
            }

            var xCount = widthSegments + 1;
            var zCount = lengthSegments + 1;
            var vertexCount = xCount * zCount;

            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var triangles = new int[widthSegments * lengthSegments * 6];

            var v = 0;
            for (var z = 0; z <= lengthSegments; z++)
            {
                var z01 = z / (float)lengthSegments;
                var localZ = z01 * length;

                for (var x = 0; x <= widthSegments; x++)
                {
                    var x01 = x / (float)widthSegments;
                    var localX = Mathf.Lerp(-width * 0.5f, width * 0.5f, x01);

                    var localOffset = new Vector3(localX, 0f, localZ);
                    var sampleWorldPos = transform.TransformPoint(localOffset);
                    var vertexWorldPos = SampleGround(sampleWorldPos);

                    vertices[v] = transform.InverseTransformPoint(vertexWorldPos);
                    uvs[v] = new Vector2(x01, z01);
                    v++;
                }
            }

            var t = 0;
            for (var z = 0; z < lengthSegments; z++)
            {
                var rowStart = z * xCount;
                var nextRowStart = (z + 1) * xCount;

                for (var x = 0; x < widthSegments; x++)
                {
                    var a = rowStart + x;
                    var b = nextRowStart + x;
                    var c = rowStart + x + 1;
                    var d = nextRowStart + x + 1;

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
