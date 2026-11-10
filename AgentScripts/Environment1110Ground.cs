using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AntColony.Map;

public static class Environment1110Ground
{
    const string Root = "Assets/Resources/Environment1110/Ground";
    public static string Import()
    {
        Directory.CreateDirectory(Root);
        foreach (var name in new[] { "ForestFloor", "Grass", "Sand", "Wetland", "Alpine", "Snow", "Soil", "Coast" })
        {
            var source = "art/environment-1110/" + name + ".png";
            if (!File.Exists(source)) continue;
            var path = Root + "/" + name + ".png";
            File.Copy(source, path, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        return "Imported ground textures: " + string.Join(", ", Directory.GetFiles(Root, "*.png").Select(Path.GetFileName));
    }

    static Texture2D Tex(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/" + name + ".png");
    public static string Apply()
    {
        var mapping = new[] {
            ("Forest", new[]{"Wetland", "ForestFloor", "Grass", "Soil", "Alpine"}),
            ("Garden", new[]{"Soil", "Grass", "Grass", "ForestFloor", "Alpine"}),
            ("Waterside", new[]{"Coast", "Sand", "Wetland", "Grass", "Alpine"}),
            ("Desert", new[]{"Sand", "Sand", "Soil", "Alpine", "Alpine"}),
            ("Cave", new[]{"Alpine", "Alpine", "Soil", "Alpine", "Alpine"}),
            ("City", new[]{"Soil", "Soil", "Grass", "Alpine", "Alpine"})
        };
        // Validate every dependency before changing any style. Keep authored heights, water and spawn rules.
        foreach (var pair in mapping)
        {
            if (pair.Item2.Any(n => Tex(n) == null)) throw new Exception("Missing ground texture for " + pair.Item1);
            if (AssetDatabase.LoadAssetAtPath<BiomeMapStyle>("Assets/_Project/Resources/Biomes/" + pair.Item1 + ".asset") == null)
                throw new Exception("Missing style " + pair.Item1);
        }
        Directory.CreateDirectory("work/environment-1110/biomes-before");
        foreach (var pair in mapping)
        {
            var path = "Assets/_Project/Resources/Biomes/" + pair.Item1 + ".asset";
            var backup = "work/environment-1110/biomes-before/" + pair.Item1 + ".asset";
            if (!File.Exists(backup)) File.Copy(path, backup);
            var style = AssetDatabase.LoadAssetAtPath<BiomeMapStyle>(path);
            Undo.RecordObject(style, "Apply Meshy terrain textures");
            for (int i = 0; i < style.layers.Count; i++) style.layers[i].texture = Tex(pair.Item2[Math.Min(i, pair.Item2.Length - 1)]);
            style.snowGround = Tex("Snow");
            style.leafGround = Tex("ForestFloor");
            EditorUtility.SetDirty(style);
        }
        AssetDatabase.SaveAssets();
        return "Updated 6 biome texture sets; preserved layer heights, water, spawns and gameplay.";
    }
}
