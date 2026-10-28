using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using AntColony.Data;
using R = AntColony.Data.ResourceType;
public static class FoodIndustry1028Import
{
    public static string Main()
    {
        if (Application.isPlaying) throw new Exception("Stop Play before importing");
        AssetDatabase.Refresh();
        var method = typeof(AntColony.EditorTools.KemonoImport).GetMethod("ImportProp", BindingFlags.Static | BindingFlags.NonPublic);
        var paths = new[] { "Facilities/Mill", "Facilities/OilPress", "Facilities/BrewingBarrel", "Facilities/Still" }
            .Concat(Enum.GetValues(typeof(R)).Cast<R>().Select(t => ProcessingResources.Model(t)).Where(n => n != null).Select(n => "ResourceModels/" + n)).ToArray();
        foreach (var id in paths)
        {
            var path = "Assets/Resources/" + id + ".fbx"; method.Invoke(null, new object[] { path, true });
            var renderers = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0 || renderers.Any(r => r.sharedMaterials.Any(m => m == null || !m.shader.name.StartsWith("Universal Render Pipeline/")))) throw new Exception(path);
        }
        AssetDatabase.SaveAssets(); return "PASS " + paths.Length + " food industry models/materials";
    }
}
