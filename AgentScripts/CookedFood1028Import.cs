using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using AntColony.Data;
public static class CookedFood1028Import
{
    public static string Main()
    {
        if (Application.isPlaying) throw new Exception("Stop Play before import");
        AssetDatabase.Refresh();
        var method = typeof(AntColony.EditorTools.KemonoImport).GetMethod("ImportProp", BindingFlags.Static | BindingFlags.NonPublic);
        var names = new[] { "Dish_Rice", "Bread_Flat", "Dish_FriedRice" };
        foreach (var name in names)
        {
            var path = "Assets/Resources/ResourceModels/" + name + ".fbx"; method.Invoke(null, new object[] { path, true });
            var renderers = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0 || renderers.Any(r => r.sharedMaterials.Any(m => m == null || !m.shader.name.StartsWith("Universal Render Pipeline/")))) throw new Exception(path);
        }
        AssetDatabase.SaveAssets(); return "PASS 3 cooked food models/materials";
    }
}
