using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AntColony.Buildings
{
    // 직사각형 방은 Meshy 초가지붕, 불규칙 방은 칸별 지붕. 가까이 보면 내부가 드러난다.
    public sealed class RoomRoofs : MonoBehaviour
    {
        public const float Height = 2.6f, FadeOrthoSize = 14f;
        private static RoomRoofs instance;
        private readonly List<MeshRenderer> roofs = new List<MeshRenderer>();
        private readonly List<GameObject> models = new List<GameObject>();
        private readonly Dictionary<Material, Material> modelMaterials = new Dictionary<Material, Material>();
        private Material material;
        public static int Count => instance == null ? 0 : instance.roofs.Count + instance.models.Count;

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
            foreach (var value in modelMaterials.Values) if (value != null) Destroy(value);
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
            foreach (var model in models) if (model != null) Destroy(model);
            models.Clear();
        }

        private static float RoofHeight(Room room)
        {
            // Room cells contain only X/Z. Their Y must come from the terrain/walls,
            // not a world-space constant (home terrain is commonly around Y=10).
            var floor = room.Walls.Where(w => w != null).Select(w => w.Position.y - Mathf.Abs(w.transform.lossyScale.y) * .5f).DefaultIfEmpty(0).Max();
            foreach (var cell in room.Cells)
                if (Physics.Raycast(new Vector3(cell.x + .5f, floor + 100, cell.y + .5f), Vector3.down,
                    out var hit, 200, 1 << 8, QueryTriggerInteraction.Ignore)) floor = Mathf.Max(floor, hit.point.y);
            var ceiling = room.Walls.Where(w => w != null).Select(WallTop).DefaultIfEmpty(floor + Height).Max();
            return Mathf.Max(floor + .1f, ceiling);
        }

        private static float WallTop(BuildingBase wall)
        {
            var model = wall.GetComponent<FacilityVisual>()?.Model;
            var top = wall.Position.y + Mathf.Abs(wall.transform.lossyScale.y) * .5f;
            return model == null ? top : model.GetComponentsInChildren<Renderer>().Select(r => r.bounds.max.y).DefaultIfEmpty(top).Max();
        }

        private bool TryThatch(Room room, float y)
        {
            if (room.Cells.Count == 0) return false;
            int x0 = room.Cells.Min(c => c.x), x1 = room.Cells.Max(c => c.x) + 1;
            int z0 = room.Cells.Min(c => c.y), z1 = room.Cells.Max(c => c.y) + 1;
            // Do not stretch a rectangular model across courtyards or L-shaped gaps.
            if ((x1 - x0) * (z1 - z0) != room.Cells.Count) return false;
            var source = Resources.Load<GameObject>("TribalFinish1115/ThatchRoof");
            if (source == null) return false;
            var model = new GameObject("Thatch roof " + room.KindName);
            model.transform.SetParent(transform, false);
            // Preserve the FBX's axis/scale conversion under a world-aligned sizing root.
            Instantiate(source, model.transform);
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { Destroy(model); return false; }
            var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            var size = bounds.size;
            if (size.x <= 0 || size.y <= 0 || size.z <= 0) { Destroy(model); return false; }
            float rise = Mathf.Clamp((z1 - z0) * .2f, .35f, 1.5f);
            // Interior cells stop at the inner wall edge; eaves must reach over the boundary.
            var scale = new Vector3((x1 - x0 + 1.2f) / size.x, rise / size.y, (z1 - z0 + 1.2f) / size.z);
            model.transform.localScale = scale;
            model.transform.position = new Vector3((x0 + x1) * .5f, y, (z0 + z1) * .5f)
                - Vector3.Scale(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), scale);
            foreach (var r in renderers)
            {
                r.sharedMaterials = r.sharedMaterials.Select(original =>
                {
                    if (original == null) return original;
                    if (!modelMaterials.TryGetValue(original, out var fade))
                    {
                        fade = new Material(original) { name = original.name + " Roof fade" };
                        MakeTransparent(fade); modelMaterials.Add(original, fade);
                    }
                    return fade;
                }).ToArray();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            foreach (var child in model.GetComponentsInChildren<Transform>()) child.gameObject.layer = 2;
            foreach (var collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;
            models.Add(model);
            return true;
        }

        private static void MakeTransparent(Material value)
        {
            value.SetFloat("_Surface", 1); value.SetFloat("_Blend", 0);
            value.SetOverrideTag("RenderType", "Transparent"); value.renderQueue = 3000;
            value.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            value.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            value.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
            value.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            value.SetInt("_ZWrite", 0); value.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private void Build(IReadOnlyList<Room> rooms)
        {
            ClearRoofs();
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                material = new Material(shader);
                MakeTransparent(material);
            }
            foreach (var room in rooms)
            {
                var height = RoofHeight(room);
                if (TryThatch(room, height)) continue;
                var vertices = new List<Vector3>(); var triangles = new List<int>();
                foreach (var c in room.Cells)
                {
                    var i = vertices.Count;
                    vertices.Add(new Vector3(c.x, height, c.y)); vertices.Add(new Vector3(c.x + 1, height, c.y));
                    vertices.Add(new Vector3(c.x + 1, height, c.y + 1)); vertices.Add(new Vector3(c.x, height, c.y + 1));
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
            foreach (var value in modelMaterials.Values)
                value.SetColor("_BaseColor", new Color(1, 1, 1, alpha));
        }
    }
}
