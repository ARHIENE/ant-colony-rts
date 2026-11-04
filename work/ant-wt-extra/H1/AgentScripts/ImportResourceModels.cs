using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Data;
using AntColony.World;
using UnityEditor;
using UnityEngine;

public static class ImportResourceModels
{
    public static async Task<string> Main(int start, int count)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before importing resources.");
        var ids = Enum.GetValues(typeof(AntColony.Data.ResourceType)).Cast<AntColony.Data.ResourceType>()
            .SelectMany(t => new[] { ResourceVisual.ModelFor(t, false), ResourceVisual.ModelFor(t, true) })
            .Where(id => id != null).Distinct().OrderBy(id => id).ToArray();
        Directory.CreateDirectory("Assets/Resources/ResourceModels");
        var batch = ids.Skip(start).Take(count).ToArray();
        var import = typeof(AntColony.EditorTools.KemonoImport).GetMethod("ImportProp", BindingFlags.Static | BindingFlags.NonPublic);
        foreach (var id in batch)
        {
            var source = id.StartsWith("CC_") ? "art/resident-kit/exports/facilities/" + id + ".fbx"
                : "art/production-kit-20261019/exports/resources/" + id + ".fbx";
            if (!File.Exists(source) && id.StartsWith("CC_")) source = "art/colony-core/exports/" + id + ".fbx";
            if (!File.Exists(source)) throw new FileNotFoundException("Missing authored resource model", source);
            var target = "Assets/Resources/ResourceModels/" + id + ".fbx";
            if (!File.Exists(target)) File.Copy(source, target);
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            import.Invoke(null, new object[] { target, false });
            if (AssetDatabase.LoadAssetAtPath<GameObject>(target) == null) throw new Exception("Import failed " + id);
            File.WriteAllText("Temp/resource-model-import-progress.txt", id);
            await Task.Yield();
        }
        AssetDatabase.SaveAssets();
        return "Imported " + batch.Length + " URP resource models; next=" + Math.Min(start + count, ids.Length) + "/" + ids.Length;
    }
}
