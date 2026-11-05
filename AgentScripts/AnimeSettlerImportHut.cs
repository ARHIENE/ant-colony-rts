using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class AnimeSettlerImportHut
{
    public static string Main()
    {
        if (Application.isPlaying) throw new Exception("Import in edit mode.");
        const string source = "art/anime-settler-1105/";
        const string path = "Assets/Resources/Facilities/CC_Hut.fbx";
        const string folder = "Assets/Resources/MeadowMaterials";
        Directory.CreateDirectory(folder);
        if (!File.Exists(source + "before-anime-CC_Hut.fbx")) File.Copy(path, source + "before-anime-CC_Hut.fbx");
        File.Copy(source + "CC_Hut.fbx", path + ".anime-import", true);
        File.Replace(path + ".anime-import", path, null);
        var texturePath = folder + "/Meadow_Hut_BaseColor.png";
        File.Copy(source + "Meadow_Hut_BaseColor.png", texturePath, true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        importer.SaveAndReimport();
        importer = (ModelImporter)AssetImporter.GetAtPath(path);
        foreach (var name in new[] { "Meadow_Hut", "Story_CreamPlaster_MeadowHut" })
        {
            var materialPath = folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, materialPath); }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", .12f);
            material.SetFloat("_Metallic", 0);
            material.SetFloat("_Cull", 0); // Thin shingles and window trim have visible backs.
            EditorUtility.SetDirty(material);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), material);
        }
        EditorUtility.SetDirty(importer);
        AssetDatabase.WriteImportSettingsIfDirty(path);
        importer.SaveAndReimport(); AssetDatabase.SaveAssets();
        var hut = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var renderer = hut.GetComponentInChildren<Renderer>();
        if (renderer.sharedMaterials.Any(m => m == null || m.GetTexture("_BaseMap") == null)) throw new Exception("Missing hut texture.");
        if (!hut.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("CC_Hut__WorkPoint_0"))) throw new Exception("Missing work point.");
        return "Hut imported: textured URP materials, work point retained; triangles=" + hut.GetComponentInChildren<MeshFilter>().sharedMesh.triangles.Length / 3;
    }
}
