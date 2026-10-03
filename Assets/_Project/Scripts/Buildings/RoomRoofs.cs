using System.Collections.Generic;
using UnityEngine;

namespace AntColony.Buildings
{
    // 방마다 칸 단위 납작한 지붕 메시(림월드식 자동 지붕). 카메라가 다가가면(줌 인) 투명해져 안이 보인다.
    public sealed class RoomRoofs : MonoBehaviour
    {
        public const float Height = 2.6f, FadeOrthoSize = 14f;
        private static RoomRoofs instance;
        private readonly List<MeshRenderer> roofs = new List<MeshRenderer>();
        private Material material;
        public static int Count => instance == null ? 0 : instance.roofs.Count;

        public static void Rebuild(IReadOnlyList<Room> rooms)
        {
            if (instance == null)
            {
                if (rooms.Count == 0) return;
                instance = new GameObject("RoomRoofs").AddComponent<RoomRoofs>();
            }
            instance.Build(rooms);
        }
        private void OnDestroy()
        {
            ClearRoofs();
            if (material != null) Destroy(material);
            if (instance == this) instance = null;
        }

        private void ClearRoofs()
        {
            foreach (var r in roofs) if (r != null)
            {
                Destroy(r.GetComponent<MeshFilter>().sharedMesh);
                Destroy(r.gameObject);
            }
            roofs.Clear();
        }

        private void Build(IReadOnlyList<Room> rooms)
        {
            ClearRoofs();
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                material = new Material(shader);
                material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
                material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            foreach (var room in rooms)
            {
                var vertices = new List<Vector3>(); var triangles = new List<int>();
                foreach (var c in room.Cells)
                {
                    var i = vertices.Count;
                    vertices.Add(new Vector3(c.x, Height, c.y)); vertices.Add(new Vector3(c.x + 1, Height, c.y));
                    vertices.Add(new Vector3(c.x + 1, Height, c.y + 1)); vertices.Add(new Vector3(c.x, Height, c.y + 1));
                    triangles.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
                }
                var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
                var go = new GameObject("Roof " + room.KindName); go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = material;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                roofs.Add(mr);
            }
        }

        private void LateUpdate()
        {
            var cam = UnityEngine.Camera.main; if (cam == null || material == null) return;
            var alpha = cam.orthographic && cam.orthographicSize < FadeOrthoSize ? .15f : .85f;
            var color = new Color(.42f, .33f, .22f, alpha);
            material.color = color; if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        }
    }
}
