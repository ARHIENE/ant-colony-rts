using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
public static class Motion1113Setup
{
    public static string Run()
    {
        if (Application.isPlaying) throw new Exception("Edit mode required");
        foreach (var path in new[] { "Assets/Resources/Facilities/CC_FarmTile.fbx", "Assets/Resources/ResourceModels/CC_ConstructionPlan.fbx" })
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (!importer) throw new Exception("Missing model: " + path);
            if (!importer.isReadable) { importer.isReadable = true; importer.SaveAndReimport(); }
        }
        return "Farm and construction geometry readable for surface contact";
    }
    public static string Coverage()
    {
        var all = Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");
        var live = Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");
        var lines = all.motions.Select(m => m.id + "\t" + string.Join(",", live.actions.Where(a=>a.motion==m.id).Select(a=>a.action)));
        System.IO.File.WriteAllLines("work/motion-actions-1113/motion-bindings.tsv", lines);
        return live.actions.Length + " action bindings / " + live.motions.Length + " source clips; library " + all.motions.Length;
    }
}
