using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class ImportGraphicsProps
{
    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        foreach (var id in new[] { "CC_ConstructionPlan", "CC_DemolitionRubble", "CC_OreOutcrop" })
        {
            var target = "Assets/Resources/ResourceModels/" + id + ".fbx";
            if (!File.Exists(target)) File.Copy("art/colony-core/exports/" + id + ".fbx", target);
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            typeof(AntColony.EditorTools.KemonoImport).GetMethod("ImportProp", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { target, false });
        }
        AssetDatabase.SaveAssets(); return "Imported 3 authored construction/deposit props";
    }
}
