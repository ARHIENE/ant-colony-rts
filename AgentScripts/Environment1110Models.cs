using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Environment1110Models
{
    const string Root = "Assets/Resources/Environment1110/Models";
    public static string Import()
    {
        var reports = new List<string>();
        var sources = Directory.GetFiles("art/environment-1110/models", "*.fbx", SearchOption.AllDirectories);
        foreach (var source in sources)
        {
            var id = Path.GetRelativePath("art/environment-1110/models", source).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            var folder = Root + "/" + id;
            Directory.CreateDirectory(folder);
            var baseName = Path.Combine(Path.GetDirectoryName(source), Path.GetFileNameWithoutExtension(source));
            if (!File.Exists(baseName + ".png")) throw new Exception(id + " missing base color");
            File.Copy(baseName + ".png", folder + "/BaseColor.png", true);
            if (File.Exists(baseName + "_normal.png")) File.Copy(baseName + "_normal.png", folder + "/Normal.png", true);
            var path = folder + "/" + id + ".fbx";
            File.Copy(source, path, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.isReadable = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            importer.ExtractTextures(folder);
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
        Directory.CreateDirectory("work/environment-1110");
        File.WriteAllLines("work/environment-1110/model-import.txt", reports);
        return string.Join("\n", reports);
    }
    static string Safe(string value) => string.Concat(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
