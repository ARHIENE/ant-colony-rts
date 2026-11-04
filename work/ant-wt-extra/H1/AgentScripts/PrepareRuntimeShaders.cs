using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PrepareRuntimeShaders
{
    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before preparing build shaders.");
        const string folder = "Assets/Resources/RuntimeShaders";
        Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        var count = 0;
        foreach (var pair in new[] { ("Terrain", "AntColony/TerrainBlend"), ("WeatherParticles", "Universal Render Pipeline/Particles/Unlit"), ("Roof", "Universal Render Pipeline/Unlit") })
        {
            var shader = Shader.Find(pair.Item2);
            if (shader == null) throw new Exception("Missing runtime shader " + pair.Item2);
            var path = folder + "/" + pair.Item1 + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            if (pair.Item1 == "WeatherParticles")
            {
                material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(material); count++;
        }
        AssetDatabase.SaveAssets();
        return "PASS " + count + " runtime shader references retained through Resources";
    }
}
