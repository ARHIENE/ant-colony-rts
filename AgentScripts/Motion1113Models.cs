using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Motion1113Models
{
    const string Root = "Assets/Resources/Furniture1113/Models";
    public static string Bed() => Import("SingleBed");
    public static string Bowl() => Import("Bowl");
    public static string Book() => Import("OpenBook");
    public static string Table() => Import("DiningTable");
    public static string Shelf() => Import("Bookshelf");
    public static string Bench() => Import("Workbench");
    public static string Wall() => Import("Palisade");
    public static string Major() => Table()+"\n"+Shelf()+"\n"+Bench()+"\n"+Wall();
    public static string Import(string only = null)
    {
        if (Application.isPlaying) throw new Exception("Edit mode required");
        var reports = new List<string>();
        var sources = Directory.GetFiles("art/meshy-1113/staged", "*.fbx", SearchOption.AllDirectories);
        foreach (var source in sources)
        {
            var id = Path.GetRelativePath("art/meshy-1113/staged", source).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            if (only != null && id != only) continue;
            var folder = Root + "/" + id;
            Directory.CreateDirectory(folder);
            var baseName = Path.Combine(Path.GetDirectoryName(source), Path.GetFileNameWithoutExtension(source));
            var sourceColor = Path.Combine(Path.GetDirectoryName(source), "BaseColor.png");
            if (!File.Exists(sourceColor)) throw new Exception(id + " missing base color");
            File.Copy(sourceColor, folder + "/BaseColor.png", true);
            var sourceNormal = Path.Combine(Path.GetDirectoryName(source), "Normal.png");
            if (File.Exists(sourceNormal)) File.Copy(sourceNormal, folder + "/Normal.png", true);
            var path = folder + "/" + id + ".fbx";
            File.Copy(source, path, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.isReadable = true; // Usage surfaces and tool contacts read source geometry.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            // The downloaded base/normal files are already staged; do not extract duplicate embedded maps.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var textures = Directory.GetFiles(folder).Where(f => new[]{".png",".jpg",".jpeg"}.Contains(Path.GetExtension(f).ToLowerInvariant())).ToArray();
            foreach (var texture in textures)
            {
                var ti = AssetImporter.GetAtPath(texture.Replace('\\','/')) as TextureImporter;
                if (ti == null) continue;
                ti.textureType = Path.GetFileName(texture) == "Normal.png" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = ti.textureType != TextureImporterType.NormalMap;
                ti.mipmapEnabled = true; ti.maxTextureSize = 2048; ti.anisoLevel = 4;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.SaveAndReimport();
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var mats = model.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToArray();
            foreach (var original in mats)
            {
                var matPath = folder + "/" + Safe(original.name) + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
                Texture albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/BaseColor.png");
                if (albedo == null)
                {
                    var candidate = textures.FirstOrDefault(f => f.IndexOf("base", StringComparison.OrdinalIgnoreCase) >= 0 || f.IndexOf("albedo", StringComparison.OrdinalIgnoreCase) >= 0 || f.IndexOf("diffuse", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (candidate != null) albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(candidate.Replace('\\','/'));
                }
                if (albedo == null) throw new Exception(id + " has no albedo texture; refusing untextured import");
                mat.SetTexture("_BaseMap", albedo); mat.SetColor("_BaseColor", Color.white);
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Normal.png");
                if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.SetFloat("_BumpScale", .5f); mat.EnableKeyword("_NORMALMAP"); }
                mat.SetFloat("_Smoothness", .15f); mat.SetFloat("_Metallic", 0);
                mat.enableInstancing = true; EditorUtility.SetDirty(mat);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), original.name), mat);
            }
            importer.SaveAndReimport();
            model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var triangles = model.GetComponentsInChildren<MeshFilter>(true).Sum(m => m.sharedMesh != null ? Enumerable.Range(0, m.sharedMesh.subMeshCount).Sum(i => (long)m.sharedMesh.GetIndexCount(i) / 3) : 0);
            reports.Add(id + ": " + triangles + " triangles, " + textures.Length + " textures, " + mats.Length + " materials");
        }
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("work/motion-actions-1113");
        File.WriteAllLines("work/motion-actions-1113/" + (only == null ? "model-import" : only + "-import") + ".txt", reports);
        return string.Join("\n", reports);
    }
    static string Safe(string value) => string.Concat(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
