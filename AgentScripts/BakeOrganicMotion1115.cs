using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using AntColony.World;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

public static class BakeOrganicMotion
{
    public static string DeathDetailsPreservingScale()
    {
        var report = new List<string>();
        for (int i = 1; i <= 17; i++)
        {
            string name = ((OrganicEnemyKind)i).ToString();
            string asset = "Assets/Resources/OrganicEnemies1114/" + name + "/HordeMotion.asset";
            File.Copy("work/death-weather-1115/before/banks/" + name + ".asset", asset, true);
            AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceUpdate);
            float oldWidth = AssetDatabase.LoadAssetAtPath<OrganicMotionBank>(asset).width;
            report.Add(Main(name));
            var bank = AssetDatabase.LoadAssetAtPath<OrganicMotionBank>(asset);
            bank.width = oldWidth; EditorUtility.SetDirty(bank); AssetDatabase.SaveAssets();
            report.Add("PASS " + name + " preserves living body scale " + oldWidth);
            File.WriteAllLines("work/death-weather-1115/horde-bake.txt", report);
        }
        return string.Join("\n", report);
    }
    public static string All()
    {
        var report = new List<string>();
        for (int i=1;i<=17;i++)
        {
            report.Add(Main(((OrganicEnemyKind)i).ToString()));
            File.WriteAllLines("work/organic-horde-motion-1114/bake-all-1115.txt",report);
        }
        return string.Join("\n",report);
    }
    [Serializable] public class ContactProfile { public float strideSourceUnitsPerSecond, horizontalExtent; }
    static readonly string[] Names = { "Idle", "Move", "Attack", "Hit", "Death", "Heal" };
    public static string Main(string name)
    {
        if (Application.isPlaying) throw new Exception("Bake in Edit mode");
        var kind = (OrganicEnemyKind)Enum.Parse(typeof(OrganicEnemyKind), name);
        string folder = "Assets/Resources/OrganicEnemies1114/" + name + "/";
        var root = OrganicEnemyVisual.Create(kind);
        var animator = root.GetComponentInChildren<Animator>(); animator.enabled = false;
        var skin = root.GetComponentInChildren<SkinnedMeshRenderer>();
        var source = skin.sharedMesh; var baked = new Mesh(); var reduced = new Mesh();
        Texture2D colors = null;
        try
        {
            var idle = AssetDatabase.LoadAllAssetsAtPath(folder + "Idle.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
            idle.SampleAnimation(animator.gameObject, 0);
            var rest = Sample(skin, baked);
            var triangles = source.triangles;
            int[] mapping; List<int[]> clusters; int[] topology;
            Reduce(rest, triangles, out mapping, out clusters, out topology);
            int width = Mathf.NextPowerOfTwo(clusters.Count);
            var descriptors = new List<OrganicMotionBank.Clip>();
            var clips = new List<AnimationClip>();
            int rows = 0;
            for (int i = 0; i < (kind == OrganicEnemyKind.BloomMender ? 6 : 5); i++)
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath(folder + Names[i] + ".fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
                float seconds = i == 4 ? OrganicDeathPose.Duration : clip.length;
                int count = Mathf.CeilToInt(seconds * 24) + 1;
                descriptors.Add(new OrganicMotionBank.Clip { name = Names[i], first = rows, frames = count, seconds = seconds, loop = i < 2 });
                clips.Add(clip); rows += count;
            }
            var positions = new Color[width * rows]; var normals = new Color[width * rows];
            var vertices = new Vector3[clusters.Count];
            var bounds = new Bounds(); bool initial = true;
            reduced.vertices = vertices; reduced.triangles = topology;
            float stride = 0;
            string profilePath = "work/organic-horde-motion-1114/refined/" + name + "/contact-profile.json";
            if (File.Exists(profilePath))
            {
                var profile = JsonUtility.FromJson<ContactProfile>(File.ReadAllText(profilePath));
                float extent = Mathf.Max(rest.Max(v => v.x) - rest.Min(v => v.x), rest.Max(v => v.z) - rest.Min(v => v.z));
                stride = profile.strideSourceUnitsPerSecond * (profile.horizontalExtent > 0 ? extent / profile.horizontalExtent : root.transform.lossyScale.x);
            }
            var stanceSpeeds = new List<float>();
            for (int c = 0; c < clips.Count; c++)
            {
                var descriptor = descriptors[c];
                OrganicDeathPose collapse = null;
                if (c == 4) { idle.SampleAnimation(animator.gameObject, 0); collapse = OrganicDeathPose.Attach(root, kind, 0); }
                Vector3[] previousSample = null; float previousGround = 0;
                for (int f = 0; f < descriptor.frames; f++)
                {
                    if (collapse != null) collapse.Sample(descriptor.seconds * f / (descriptor.frames - 1));
                    else clips[c].SampleAnimation(animator.gameObject, clips[c].length * f / (descriptor.frames - 1));
                    var sample = Sample(skin, baked);
                    float ground = sample.Min(v => v.y);
                    if (c == 1 && stride > 0 && previousSample != null)
                    {
                        float dt = clips[c].length / (descriptor.frames - 1);
                        for (int i = 0; i < sample.Length; i++)
                            if (sample[i].y - ground < .025f && previousSample[i].y - previousGround < .025f)
                            {
                                float speed = (previousSample[i].z - sample[i].z) / dt;
                                if (speed > .01f && speed < 5) stanceSpeeds.Add(speed);
                            }
                    }
                    previousSample = sample; previousGround = ground;
                    for (int i = 0; i < clusters.Count; i++)
                    {
                        var sum = Vector3.zero; foreach (int index in clusters[i]) sum += sample[index];
                        vertices[i] = sum / clusters[i].Length - Vector3.up * ground;

                    }
                    float sole = vertices.Min(v => v.y);
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] -= Vector3.up * sole;
                        if (initial) { bounds = new Bounds(vertices[i], Vector3.zero); initial = false; } else bounds.Encapsulate(vertices[i]);
                    }
                    reduced.vertices = vertices; reduced.RecalculateNormals();
                    var ns = reduced.normals;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        int index = (descriptor.first + f) * width + i; var p = vertices[i]; var n = ns[i];
                        positions[index] = new Color(p.x, p.y, p.z, 1); normals[index] = new Color(n.x, n.y, n.z, 1);
                    }
                }
            }
            // Restore rest frame topology and author texture-based instance animation data.
            for (int i = 0; i < vertices.Length; i++) vertices[i] = new Vector3(positions[i].r, positions[i].g, positions[i].b);
            reduced.vertices = vertices; reduced.RecalculateNormals(); reduced.bounds = bounds;
            var uv = new Vector2[vertices.Length]; for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2((i + .5f) / width, 0);
            reduced.uv2 = uv;
            colors = ReadTexture(skin.sharedMaterial.GetTexture("_BaseMap"));
            var originalUV = source.uv; var tint = skin.sharedMaterial.GetColor("_BaseColor");
            var vertexColors = new Color[vertices.Length];
            for (int i = 0; i < clusters.Count; i++)
            {
                Color color = Color.clear;
                foreach (int v in clusters[i]) color += colors != null && originalUV.Length > v ? colors.GetPixelBilinear(originalUV[v].x, originalUV[v].y) : Color.white;
                vertexColors[i] = color / clusters[i].Length * tint; vertexColors[i].a = 1;
            }
            reduced.colors = vertexColors;
            var bank = AssetDatabase.LoadAssetAtPath<OrganicMotionBank>(folder + "HordeMotion.asset");
            float previousWidth = bank != null ? bank.width : 0;
            if (bank == null) { bank = ScriptableObject.CreateInstance<OrganicMotionBank>(); AssetDatabase.CreateAsset(bank, folder + "HordeMotion.asset"); }
            // Rebuild only assets owned by this baker. GUIDs of the bank remain stable.
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(folder + "HordeMotion.asset")) if (asset != bank) Object.DestroyImmediate(asset, true);
            reduced.name = name + " animated horde mesh"; AssetDatabase.AddObjectToAsset(reduced, bank);
            bank.mesh = reduced; reduced = null;
            bank.positions = Texture(positions, width, rows, "Positions"); bank.normals = Texture(normals, width, rows, "Normals");
            AssetDatabase.AddObjectToAsset(bank.positions, bank); AssetDatabase.AddObjectToAsset(bank.normals, bank);
            var shader = Shader.Find("AntColony/Organic Horde Motion"); if (shader == null) throw new Exception("Animation shader missing");
            bank.material = new Material(shader) { name = name + " horde motion", enableInstancing = true };
            bank.material.SetTexture("_Positions", bank.positions); bank.material.SetTexture("_Normals", bank.normals);
            AssetDatabase.AddObjectToAsset(bank.material, bank);
            bank.clips = descriptors.ToArray(); bank.width = previousWidth > 0 ? previousWidth : Mathf.Max(bounds.size.x, bounds.size.z); bank.animatedBounds = bounds;
            // Authored locomotion speed is refined by the foot-contact authoring pass.
            if (stanceSpeeds.Count > 20) { stanceSpeeds.Sort(); stride = stanceSpeeds[stanceSpeeds.Count / 2]; }
            bank.strideSpeed = stride;
            EditorUtility.SetDirty(bank); AssetDatabase.SaveAssets();
            string result = $"PASS {name}: vertices={vertices.Length} triangles={topology.Length / 3} frames={rows} clips={clips.Count} bounds={bounds.size}";
            File.WriteAllText("work/organic-horde-motion-1114/" + name + "-bake.txt", result);
            return result;
        }
        finally { if (reduced != null) Object.DestroyImmediate(reduced); if (colors != null) Object.DestroyImmediate(colors); Object.DestroyImmediate(baked); Object.DestroyImmediate(root); }
    }
    static Vector3[] Sample(SkinnedMeshRenderer skin, Mesh mesh)
    {
        skin.BakeMesh(mesh, true); var vertices = mesh.vertices; var matrix = skin.localToWorldMatrix;
        for (int i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
        return vertices;
    }
    static Texture2D Texture(Color[] values, int width, int height, string name)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true) { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels(values); texture.Apply(false, true); return texture;
    }
    static Texture2D ReadTexture(Texture source)
    {
        if (source == null) return null;
        var rt = RenderTexture.GetTemporary(256, 256, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var previous = RenderTexture.active;
        try { Graphics.Blit(source, rt); RenderTexture.active = rt; var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false, true); texture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); texture.Apply(); return texture; }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); }
    }
    static void Reduce(Vector3[] source, int[] sourceTriangles, out int[] mapping, out List<int[]> groups, out int[] topology)
    {
        var bounds = new Bounds(source[0], Vector3.zero); foreach (var p in source) bounds.Encapsulate(p);
        float cell = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) / 90;
        mapping = new int[source.Length];
        for (int attempt = 0; attempt < 50; attempt++, cell *= 1.12f)
        {
            var cells = new Dictionary<Vector3Int, int>(); var members = new List<List<int>>();
            for (int i = 0; i < source.Length; i++)
            {
                var key = Vector3Int.FloorToInt((source[i] - bounds.min) / cell);
                if (!cells.TryGetValue(key, out int index)) { index = members.Count; cells.Add(key, index); members.Add(new List<int>()); }
                members[index].Add(i); mapping[i] = index;
            }
            var triangles = new List<int>(); var unique = new HashSet<(int,int,int)>();
            for (int i = 0; i < sourceTriangles.Length; i += 3)
            {
                int a = mapping[sourceTriangles[i]], b = mapping[sourceTriangles[i + 1]], c = mapping[sourceTriangles[i + 2]];
                if (a == b || b == c || c == a) continue;
                var key = a < b && a < c ? (a,b,c) : b < c ? (b,c,a) : (c,a,b);
                if (unique.Add(key)) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
            }
            if (triangles.Count > 2400 * 3) continue;
            if (triangles.Count == 0) throw new Exception("Empty reduced mesh");
            groups = members.Select(m => m.ToArray()).ToList(); topology = triangles.ToArray(); return;
        }
        throw new Exception("Unable to build animation mesh");
    }
}
