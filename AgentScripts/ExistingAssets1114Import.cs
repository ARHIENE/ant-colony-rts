using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ExistingAssets1114Import
{
    public static string Main()
    {
        var names = new[] { "SewingTable", "Prospector", "DeepDrill", "AdvancedDrill", "GasExtractor", "GasGenerator", "GasTank" };
        var importer = typeof(AntColony.EditorTools.KemonoImport).GetMethod("ImportProp", BindingFlags.Static | BindingFlags.NonPublic);
        foreach (var name in names)
        {
            var path = "Assets/Resources/Facilities/" + name + ".fbx";
            if (!File.Exists(path)) File.Copy("art/production-kit-20261019/exports/facilities/" + name + ".fbx", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            importer.Invoke(null, new object[] { path, true });
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!model || model.GetComponentsInChildren<Renderer>(true).Length == 0) throw new Exception("Empty model: " + name);
        }
        AssetDatabase.SaveAssets();
        return "Imported 7 existing models without generation/download.";
    }
}
