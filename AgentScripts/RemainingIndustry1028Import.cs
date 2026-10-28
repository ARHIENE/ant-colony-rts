using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class RemainingIndustry1028Import
{
    public static string Main()
    {
        if (Application.isPlaying) throw new Exception("Stop Play before importing");
        AssetDatabase.Refresh();
        var method = typeof(AntColony.EditorTools.KemonoImport).GetMethod("ImportProp", BindingFlags.Static | BindingFlags.NonPublic);
        var paths = new[] { "Facilities/CharcoalKiln", "Facilities/CokeOven", "ResourceModels/Proc_Charcoal", "ResourceModels/Proc_Coke", "ResourceModels/Proc_Pottery" };
        foreach (var id in paths)
        {
            var path = "Assets/Resources/" + id + ".fbx";
            method.Invoke(null, new object[] { path, true });
            var renderers = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0 || renderers.Any(r => r.sharedMaterials.Any(m => m == null || !m.shader.name.StartsWith("Universal Render Pipeline/"))))
                throw new Exception("Missing mesh/material " + path);
        }
        AssetDatabase.SaveAssets();
        return "PASS 5 production FBX imports and URP material sets";
    }
}
