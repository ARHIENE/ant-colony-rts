using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace AntColony.Units
{
    // 추가 몸체는 표시 전용이다. 체력·충돌·공격 판정은 호스트가 소유한다.
    [DisallowMultipleComponent]
    public sealed class HordeVisual : MonoBehaviour
    {
        // 역행렬도 전송하는 일반 인스턴싱 셰이더의 상수 버퍼 한도를 고려한다.
        private const int BatchSize = 511;
        private static readonly Dictionary<string, SharedBody> Cache = new Dictionary<string, SharedBody>();
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

        private sealed class SharedBody
        {
            public string key;
            public Mesh mesh;
            public Texture2D palette;
            public RenderParams[] draws;
            public int users;
        }

        private struct Part
        {
            public Renderer renderer;
            public Mesh mesh;
            public int submesh;
            public Material material;
            public Color color;
            public Texture texture;
        }

        private Func<int> count;
        private SharedBody body;
        private Vector3 scale, previousPosition;
        private Quaternion facing = Quaternion.identity;
        private Vector3[] offsets = Array.Empty<Vector3>();
        private readonly Matrix4x4[] matrices = new Matrix4x4[BatchSize];
        private float seedAngle;
        public int ExtraBodies { get; private set; }

        public static HordeVisual Attach(GameObject host, Func<int> count)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (count == null) throw new ArgumentNullException(nameof(count));
            var visual = host.GetComponent<HordeVisual>();
            if (visual == null) visual = host.AddComponent<HordeVisual>();
            visual.count = count;
            return visual;
        }

        private void OnEnable()
        {
            previousPosition = transform.position;
            facing = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            seedAngle = Hash(unchecked((uint)GetEntityId().GetHashCode())) * Mathf.PI * 2;
        }

        private static float Hash(uint value)
        {
            unchecked
            {
                value ^= value >> 16; value *= 0x7feb352d;
                value ^= value >> 15; value *= 0x846ca68b; value ^= value >> 16;
            }
            return (value & 0xffffff) / 16777216f;
        }

        private static Mesh SourceMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skin) return skin.sharedMesh;
            if (renderer is MeshRenderer && renderer.TryGetComponent<MeshFilter>(out var filter)) return filter.sharedMesh;
            return null;
        }

        private bool Prepare()
        {
            if (!SystemInfo.supportsInstancing || GetComponent<AntVisual>() == null)
                throw new InvalidOperationException("GPU 인스턴싱과 호스트 AntVisual이 필요합니다.");
            var renderers = GetComponentsInChildren<Renderer>();
            Transform root = null;
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled || renderer.transform == transform || SourceMesh(renderer) == null) continue;
                root = renderer.transform;
                while (root.parent != transform && root.parent != null) root = root.parent;
                break;
            }
            if (root == null) throw new InvalidOperationException("호스트 AntVisual의 활성 메시를 찾지 못했습니다.");
            scale = Vector3.Scale(transform.lossyScale, root.localScale);
            var parts = new List<Part>();
            var key = new StringBuilder(root.name);
            var block = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled || !renderer.transform.IsChildOf(root)) continue;
                var mesh = SourceMesh(renderer);
                if (mesh == null) continue; // 선택 링·파티클은 복제하지 않는다.
                var materials = renderer.sharedMaterials;
                if (materials.Length == 0) continue;
                key.Append('|').Append(mesh.GetEntityId().ToString());
                for (var sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var slot = Mathf.Min(sub, materials.Length - 1);
                    var material = materials[slot];
                    if (material == null) throw new InvalidOperationException("원본 몸체 재질이 비어 있습니다.");
                    block.Clear(); renderer.GetPropertyBlock(block, slot);
                    if (block.isEmpty) renderer.GetPropertyBlock(block);
                    var color = block.HasColor(BaseColor) ? block.GetColor(BaseColor)
                        : material.HasProperty(BaseColor) ? material.GetColor(BaseColor) : Color.white;
                    var texture = block.HasTexture(BaseMap) ? block.GetTexture(BaseMap)
                        : material.HasProperty(BaseMap) ? material.GetTexture(BaseMap) : null;
                    parts.Add(new Part { renderer = renderer, mesh = mesh, submesh = sub, material = material, color = color, texture = texture });
                    // 같은 부품·재질·팔레트 조합은 호스트 위치와 무관하게 한 번만 베이크한다.
                    key.Append(':').Append(material.GetEntityId().ToString()).Append(':').Append(color.r).Append(',')
                        .Append(color.g).Append(',').Append(color.b).Append(',').Append(color.a)
                        .Append(':').Append(texture != null ? texture.GetEntityId().ToString() : "없음");
                }
            }
            var cacheKey = key.ToString();
            if (!Cache.TryGetValue(cacheKey, out body))
            {
                body = Build(cacheKey, parts, root);
                Cache.Add(cacheKey, body);
            }
            body.users++;
            return true;
        }

        private const int TriangleBudget = 1500;

        private static SharedBody Build(string key, List<Part> parts, Transform root)
        {
            var baked = new Dictionary<Renderer, Mesh>();
            var textures = new Dictionary<Texture, Texture2D>();
            var temporary = new List<Mesh>();
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var styles = new List<int>();
            var triangles = new List<int>();
            Mesh combined = null;
            Material material = null;
            Texture2D palette = null;
            try
            {
                for (var style = 0; style < parts.Count; style++)
                {
                    var part = parts[style];
                    if (!baked.TryGetValue(part.renderer, out var mesh))
                    {
                        if (part.renderer is SkinnedMeshRenderer skin)
                        {
                            mesh = new Mesh { name = "무리 몸체 포즈", indexFormat = IndexFormat.UInt32 };
                            temporary.Add(mesh);
                            skin.BakeMesh(mesh, false);
                        }
                        else
                        {
                            mesh = part.mesh;
                            if (!mesh.isReadable)
                                throw new InvalidOperationException("정적 몸체 메시의 Read/Write가 꺼져 있습니다: " + mesh.name);
                        }
                        baked.Add(part.renderer, mesh);
                    }
                    Texture2D texture = null;
                    if (part.texture != null && part.texture != Texture2D.whiteTexture &&
                        !textures.TryGetValue(part.texture, out texture))
                    {
                        texture = ReadTexture(part.texture);
                        textures.Add(part.texture, texture);
                    }
                    var positions = mesh.vertices;
                    var uv = mesh.uv;
                    var indices = mesh.GetTriangles(part.submesh);
                    var remap = new Dictionary<int, int>();
                    var matrix = root.worldToLocalMatrix * part.renderer.localToWorldMatrix;
                    var tint = QualitySettings.activeColorSpace == ColorSpace.Linear ? part.color.linear : part.color;
                    var uvScale = part.material.HasProperty(BaseMap) ? part.material.GetTextureScale("_BaseMap") : Vector2.one;
                    var uvOffset = part.material.HasProperty(BaseMap) ? part.material.GetTextureOffset("_BaseMap") : Vector2.zero;
                    foreach (var index in indices)
                    {
                        if (!remap.TryGetValue(index, out var mapped))
                        {
                            mapped = vertices.Count; remap.Add(index, mapped);
                            vertices.Add(matrix.MultiplyPoint3x4(positions[index]));
                            var color = tint;
                            if (texture != null && uv.Length == positions.Length)
                            {
                                var coord = Vector2.Scale(uv[index], uvScale) + uvOffset;
                                color *= texture.GetPixelBilinear(coord.x, coord.y);
                            }
                            color.a = 1;
                            colors.Add(color); styles.Add(style);
                        }
                        triangles.Add(mapped);
                    }
                }
                if (triangles.Count == 0) throw new InvalidOperationException("합칠 몸체 메시가 없습니다.");
                combined = BuildLowDetail(vertices, colors, styles, triangles);
                // URP Lit는 정점 색을 읽지 않는다. 설치된 셰이더의 일반 인스턴싱 변형을 사용한다.
                var shader = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
                if (shader == null || !shader.isSupported)
                {
                    // 빌드에서 Particles 셰이더가 제거되었으면 기존 Lit가 읽을 면별 색 견본을 만든다.
                    shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null || !shader.isSupported) throw new InvalidOperationException("URP Lit 셰이더가 필요합니다.");
                    palette = BuildPaletteFallback(combined);
                }
                material = new Material(shader) { name = "무리 정점 색", enableInstancing = true };
                material.SetColor(BaseColor, Color.white);
                material.SetTexture(BaseMap, palette != null ? palette : Texture2D.whiteTexture);
                material.SetColor("_SpecColor", Color.black);
                material.SetFloat("_Smoothness", 0);
                material.renderQueue = (int)RenderQueue.Geometry;
                var draw = new RenderParams(material)
                {
                    shadowCastingMode = ShadowCastingMode.Off, // 본체(호스트)만 그림자를 낸다.
                    receiveShadows = parts[0].renderer.receiveShadows,
                    renderingLayerMask = parts[0].renderer.renderingLayerMask,
                    lightProbeUsage = LightProbeUsage.BlendProbes
                };
                return new SharedBody { key = key, mesh = combined, palette = palette, draws = new[] { draw } };
            }
            catch
            {
                if (combined != null) Destroy(combined);
                if (material != null) Destroy(material);
                if (palette != null) Destroy(palette);
                throw;
            }
            finally
            {
                foreach (var mesh in temporary) Destroy(mesh);
                foreach (var texture in textures.Values) Destroy(texture);
            }
        }

        // Read/Write가 꺼진 텍스처도 준비 시에만 작은 색 견본으로 읽는다.
        private static Texture2D ReadTexture(Texture source)
        {
            var target = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            var srgb = GL.sRGBWrite;
            Texture2D copy = null;
            try
            {
                GL.sRGBWrite = false;
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                copy = new Texture2D(64, 64, TextureFormat.RGBA32, false, true) { wrapMode = source.wrapMode };
                copy.ReadPixels(new Rect(0, 0, 64, 64), 0, 0, false);
                copy.Apply(false, false);
                return copy;
            }
            catch { if (copy != null) Destroy(copy); throw; }
            finally
            {
                GL.sRGBWrite = srgb;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static Mesh BuildLowDetail(List<Vector3> source, List<Color> colors, List<int> styles, List<int> indices)
        {
            var bounds = new Bounds(source[0], Vector3.zero);
            foreach (var point in source) bounds.Encapsulate(point);
            var extent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            var cell = Mathf.Max(extent / 80, .00001f);
            var clusters = new Dictionary<(Vector3Int, int), int>();
            var positions = new List<Vector3>();
            var palette = new List<Color>();
            var populations = new List<int>();
            var triangles = new List<int>();
            var unique = new HashSet<(int, int, int)>();
            var remap = new int[source.Count];
            // 색 경계를 유지하고 셀 안의 위치·색을 평균한다. 매번 원본에서 시작한다.
            for (var attempt = 0; attempt < 48; attempt++, cell *= 1.15f)
            {
                clusters.Clear(); positions.Clear(); palette.Clear(); populations.Clear(); triangles.Clear(); unique.Clear();
                for (var i = 0; i < source.Count; i++)
                {
                    var grid = Vector3Int.FloorToInt((source[i] - bounds.min) / cell);
                    var cluster = (grid, styles[i]);
                    if (!clusters.TryGetValue(cluster, out var index))
                    {
                        index = positions.Count; clusters.Add(cluster, index);
                        positions.Add(Vector3.zero); palette.Add(Color.clear); populations.Add(0);
                    }
                    remap[i] = index;
                    positions[index] += source[i]; palette[index] += colors[i]; populations[index]++;
                }
                for (var i = 0; i < positions.Count; i++)
                {
                    positions[i] /= populations[i]; palette[i] /= populations[i];
                }
                for (var i = 0; i < indices.Count; i += 3)
                {
                    var a = remap[indices[i]]; var b = remap[indices[i + 1]]; var c = remap[indices[i + 2]];
                    if (a == b || b == c || c == a) continue;
                    if (Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]).sqrMagnitude <= 1e-16f) continue;
                    // 순환 순서만 정규화해 반대 방향 면은 보존한다.
                    var triangle = a < b && a < c ? (a, b, c) : b < c ? (b, c, a) : (c, a, b);
                    if (unique.Add(triangle)) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
                }
                if (triangles.Count == 0) break;
                if (triangles.Count > TriangleBudget * 3) continue;
                // 미사용 정점을 제거하면 정점 수도 최대 삼각형 수 * 3이다.
                var compact = new List<Vector3>();
                var compactColors = new List<Color>();
                for (var i = 0; i < remap.Length; i++) remap[i] = -1;
                for (var i = 0; i < triangles.Count; i++)
                {
                    var old = triangles[i];
                    if (remap[old] < 0)
                    {
                        remap[old] = compact.Count;
                        compact.Add(positions[old] - Vector3.up * bounds.min.y);
                        compactColors.Add(palette[old]);
                    }
                    triangles[i] = remap[old];
                }
                var mesh = new Mesh { name = "무리 공유 LOD (정점 색)", indexFormat = IndexFormat.UInt16 };
                try
                {
                    mesh.SetVertices(compact); mesh.SetColors(compactColors); mesh.SetTriangles(triangles, 0);
                    mesh.RecalculateNormals(); mesh.RecalculateBounds();
                    return mesh;
                }
                catch { Destroy(mesh); throw; }
            }
            throw new InvalidOperationException("1500삼각형 이하의 유효한 LOD를 만들지 못했습니다.");
        }

        private static Texture2D BuildPaletteFallback(Mesh mesh)
        {
            var indices = mesh.triangles;
            var source = mesh.vertices; var colors = mesh.colors; var normals = mesh.normals;
            var vertices = new Vector3[indices.Length]; var tint = new Color[indices.Length];
            var directions = new Vector3[indices.Length]; var uv = new Vector2[indices.Length];
            var width = Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(indices.Length / 3f)));
            var pixels = new Color[width * width];
            for (var i = 0; i < indices.Length; i += 3)
            {
                var color = (colors[indices[i]] + colors[indices[i + 1]] + colors[indices[i + 2]]) / 3;
                var slot = i / 3;
                pixels[slot] = color;
                var coord = new Vector2((slot % width + .5f) / width, (slot / width + .5f) / width);
                for (var j = 0; j < 3; j++)
                {
                    vertices[i + j] = source[indices[i + j]]; directions[i + j] = normals[indices[i + j]];
                    tint[i + j] = color; uv[i + j] = coord; indices[i + j] = i + j;
                }
            }
            var palette = new Texture2D(width, width, TextureFormat.RGBA32, false, true)
                { name = "무리 면 색 견본", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            try
            {
                palette.SetPixels(pixels); palette.Apply(false, true);
                mesh.Clear(); mesh.vertices = vertices; mesh.colors = tint; mesh.normals = directions;
                mesh.uv = uv; mesh.triangles = indices; mesh.RecalculateBounds();
                return palette;
            }
            catch { Destroy(palette); throw; }
        }

        private void EnsureOffsets(int required)
        {
            if (offsets.Length >= required) return;
            var capacity = Mathf.NextPowerOfTwo(Mathf.Max(64, required));
            Array.Resize(ref offsets, capacity);
            for (var i = 0; i < capacity; i++)
            {
                // 바깥쪽부터 줄어드는 고정 나선. 반경 상한 이후에는 같은 원판을 안정된 순서로 채운다.
                var radius = Mathf.Min(6f, .45f * Mathf.Sqrt(i % 177 + 1));
                var angle = seedAngle + i * 2.39996323f;
                offsets[i] = new Vector3(Mathf.Cos(angle) * radius,
                    Hash(unchecked((uint)i + (uint)GetEntityId().GetHashCode())) * Mathf.PI * 2, Mathf.Sin(angle) * radius);
            }
        }

        private void LateUpdate()
        {
            var position = transform.position;
            var delta = position - previousPosition; previousPosition = position; delta.y = 0;
            var moving = Time.deltaTime > 0 && delta.sqrMagnitude > .000001f;
            if (moving) facing = Quaternion.LookRotation(delta);
            ExtraBodies = count == null ? 0 : Mathf.Max(0, count() - 1);
            if (ExtraBodies == 0) return;
            if (body == null)
            {
                try { Prepare(); }
                catch (Exception error)
                {
                    Debug.LogWarning("무리 표시 준비 실패: " + error.Message, this);
                    enabled = false; ExtraBodies = 0; return;
                }
            }
            EnsureOffsets(ExtraBodies);
            var size = Vector3.Scale(body.mesh.bounds.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            var bounds = new Bounds(position + Vector3.up * (size.y * .5f),
                new Vector3(12 + size.magnitude * 2, size.y + 1, 12 + size.magnitude * 2));
            for (var start = 0; start < ExtraBodies; start += BatchSize)
            {
                var amount = Mathf.Min(BatchSize, ExtraBodies - start);
                for (var i = 0; i < amount; i++)
                {
                    var offset = offsets[start + i];
                    var wave = moving ? Mathf.Sin(Time.time * 9 + offset.y) : 0;
                    var point = position + new Vector3(offset.x, Mathf.Abs(wave) * .045f, offset.z);
                    matrices[i] = Matrix4x4.TRS(point, facing * Quaternion.Euler(0, 0, wave * 3), scale);
                }
                for (var sub = 0; sub < body.draws.Length; sub++)
                {
                    var draw = body.draws[sub];
                    draw.layer = gameObject.layer; draw.worldBounds = bounds;
                    Graphics.RenderMeshInstanced(draw, body.mesh, sub, matrices, amount);
                }
            }
        }

        private void OnDisable() { ExtraBodies = 0; }

        private void OnDestroy()
        {
            if (body == null || --body.users > 0) return;
            Cache.Remove(body.key);
            Destroy(body.mesh);
            if (body.palette != null) Destroy(body.palette);
            foreach (var draw in body.draws) Destroy(draw.material);
            body = null;
        }
    }
}
