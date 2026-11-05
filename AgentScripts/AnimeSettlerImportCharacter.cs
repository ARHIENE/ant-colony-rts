using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class AnimeSettlerImportCharacter
{
    public static string Main()
    {
        if (Application.isPlaying) throw new Exception("Import in edit mode.");
        const string source = "art/anime-settler-1105/";
        const string path = "Assets/Resources/Kemono/Kemono_Character.fbx";
        const string folder = "Assets/Resources/MeadowMaterials";
        if (!File.Exists(source + "before-anime-Kemono_Character.fbx")) File.Copy(path, source + "before-anime-Kemono_Character.fbx");
        File.Copy(source + "Kemono_Character.fbx", path + ".anime-import", true); File.Replace(path + ".anime-import", path, null);
        var texturePath = folder + "/Meadow_Character_BaseColor.png";
        File.Copy(source + "Meadow_Character_BaseColor.png", texturePath, true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.importBlendShapes = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        importer.SaveAndReimport();
        importer = (ModelImporter)AssetImporter.GetAtPath(path);
        foreach (var original in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToArray())
        {
            if (!original.name.StartsWith("Meadow_")) continue;
            var materialPath = folder + "/" + original.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, materialPath); }
            var textured = original.name.StartsWith("Meadow_Character") || original.name.StartsWith("Meadow_TexturedHair");
            material.SetTexture("_BaseMap", textured ? AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath) : null);
            material.SetColor("_BaseColor", textured ? Color.white : original.color);
            material.SetFloat("_Smoothness", .08f); material.SetFloat("_Metallic", 0);
            material.SetFloat("_SpecularHighlights", 0); material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            EditorUtility.SetDirty(material);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), original.name), material);
        }
        EditorUtility.SetDirty(importer); AssetDatabase.WriteImportSettingsIfDirty(path);
        importer.SaveAndReimport(); AssetDatabase.SaveAssets();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        var face = renderers.First(r => r.name.StartsWith("Part_Face_"));
        if (face.sharedMesh.blendShapeCount != 8) throw new Exception("Missing expressions");
        var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid) throw new Exception("Invalid animation avatar");
        return "Character imported; renderers=" + renderers.Length + ", expressions=" + face.sharedMesh.blendShapeCount + ", avatar valid.";
    }
}
