using System;
using System.Linq;
using System.Threading.Tasks;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ResourceVisualChecks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL " + label); checks++; }

    public static async Task<string> Main()
    {
        checks = 0;
        Check(Application.isPlaying, "play mode");
        var types = Enum.GetValues(typeof(AntColony.Data.ResourceType)).Cast<AntColony.Data.ResourceType>().ToArray();
        var ids = types.SelectMany(t => new[] { ResourceVisual.ModelFor(t, false), ResourceVisual.ModelFor(t, true) })
            .Where(id => id != null).Distinct().ToArray();
        foreach (var id in ids)
        {
            var source = Resources.Load<GameObject>(ResourceVisual.Folder + id);
            Check(source != null, "authored model imported " + id);
            foreach (var material in source.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Distinct())
                Check(material != null && material.shader != null && material.shader.name == "Universal Render Pipeline/Lit", "URP material " + id);
        }
        var owner = new GameObject("Resource visual checks");
        try
        {
            foreach (var type in new[] { AntColony.Data.ResourceType.Leaf, AntColony.Data.ResourceType.Wood, AntColony.Data.ResourceType.Stone, AntColony.Data.ResourceType.IronOre, AntColony.Data.ResourceType.CopperOre, AntColony.Data.ResourceType.Soil })
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.SetActive(false); go.transform.SetParent(owner.transform);
                go.transform.position = Vector3.up * .5f;
                var node = go.AddComponent<AntColony.World.ResourceNode>(); node.ConfigureNatural(type, 80, 0); go.SetActive(true);
                ResourceVisual.Attach(node);
                await Task.Yield();
                Check(go.GetComponent<Renderer>().forceRenderingOff, "primitive hidden " + type);
                if (type == AntColony.Data.ResourceType.Soil) { Check(go.transform.Find("ResourceModel") == null, "soil remains ground"); continue; }
                var model = go.transform.Find("ResourceModel");
                Check(model != null, "model attached " + type);
                Check(model.GetComponentsInChildren<Renderer>().Any(), "authored meshes visible " + type);
                Check(!model.GetComponentsInChildren<Renderer>().Any(r => r.name.Contains("__")), "only one production variant " + type);
                var bounds = model.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                Check(bounds.size.y > .05f && bounds.size.y < 8, "character-relative model scale " + type);
            }
            return "PASS " + checks + " resource visual checks; " + ids.Length + " authored models";
        }
        finally { Object.Destroy(owner); }
    }
}
