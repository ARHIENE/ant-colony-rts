using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Storybook1105Import
{
    public static string Main()
    {
        if (Application.isPlaying) throw new Exception("Import in edit mode.");
        var files = new[] { "Kemono_Character", "CC_Hut", "CC_House", "CC_Storage", "CC_Watchtower" };
        const string directory = "Assets/Resources/StorybookMaterials";
        Directory.CreateDirectory(directory);
        foreach (var file in files)
        {
            var path = "Assets/Resources/" + (file == "Kemono_Character" ? "Kemono/" : "Facilities/") + file + ".fbx";
            var sourcePath = "art/storybook-1105/" + file + ".fbx";
            if (!File.ReadAllBytes(sourcePath).SequenceEqual(File.ReadAllBytes(path)))
            {
                var temporary = path + ".storybook-import";
                File.Copy(sourcePath, temporary, true);
                File.Replace(temporary, path, null);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.SaveAndReimport();
            foreach (var original in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
            {
                if (!original.name.StartsWith("Story_")) continue;
                var materialPath = directory + "/" + original.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.SetColor("_BaseColor", original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : original.color);
                    material.SetFloat("_Smoothness", .08f);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), original.name), material);
            }
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        var character = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Kemono/Kemono_Character.fbx");
        var avatar = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Kemono/Kemono_Character.fbx").OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid) throw new Exception("Character avatar is invalid.");
        return "Imported 5 FBX; avatar valid; character renderers=" + character.GetComponentsInChildren<Renderer>(true).Length;
    }
}
