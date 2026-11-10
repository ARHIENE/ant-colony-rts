using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AntColony.World;
using AntColony.Map;
using AntColony.Data;
using AntColony.Buildings;
using Object = UnityEngine.Object;

public static class Environment1110Checks
{
    public static string Live()
    {
        if(!Application.isPlaying)throw new Exception("Live game required");
        var rows=Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude)
            .Select(n=>new{node=n,model=n.transform.Find("ResourceModel")})
            .Where(x=>x.model!=null).Select(x=>new{x.node,mesh=x.model.GetComponentInChildren<MeshFilter>()?.sharedMesh})
            .Where(x=>x.mesh!=null&&AssetDatabase.GetAssetPath(x.mesh).Contains("Environment1110"))
            .GroupBy(x=>x.node.ResourceType).Select(g=>g.Key+": "+g.Count()).ToArray();
        if(rows.Length==0)throw new Exception("No improved resource nodes in live scene");
        var text="Live scene (read only): "+string.Join(", ",rows);
        File.WriteAllText("work/environment-1110/live-validation.txt",text);return text;
    }
    public static string Inventory()
    {
        var report=new List<string>();
        foreach(var biome in new[]{"Forest","Garden","Waterside","Desert","Cave","City"})
        {
            var style=Resources.Load<BiomeMapStyle>("Biomes/"+biome);
            report.Add(biome+": "+string.Join(", ",style.spawns.Select(s=>s.prefab!=null?s.prefab.name+" nodes="+s.prefab.GetComponentsInChildren<ResourceNode>(true).Length:"MISSING")));
        }
        File.WriteAllLines("work/environment-1110/spawn-inventory.txt",report);
        return string.Join("\n",report);
    }
    public static string Main()
    {
        int checks=0;var report=new List<string>();
        foreach(var id in new[]{"Hut","Storage","Watchtower","Oak","Pine","Bush","Rock","CopperDeposit","IronDeposit","Mushrooms"})
        {
            var model=Resources.Load<GameObject>("Environment1110/Models/"+id+"/"+id);
            if(model==null)throw new Exception("Missing "+id);checks++;
            long triangles=model.GetComponentsInChildren<MeshFilter>(true).Sum(m=>Enumerable.Range(0,m.sharedMesh.subMeshCount).Sum(i=>(long)m.sharedMesh.GetIndexCount(i)/3));
            if(triangles<100||triangles>12000)throw new Exception("Triangle budget "+id+": "+triangles);checks++;
            foreach(var mat in model.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials))
            {if(mat==null||mat.shader.name!="Universal Render Pipeline/Lit"||mat.GetTexture("_BaseMap")==null)throw new Exception("Material "+id);checks++;}
            report.Add(id+": "+triangles+" triangles");
        }
        var root=new GameObject("Environment checks temporary");root.transform.position=new Vector3(900,900,900);
        try
        {
            foreach(var type in new[]{ResourceType.Food,ResourceType.Wood,ResourceType.Resin,ResourceType.Leaf,ResourceType.Stone,ResourceType.CopperOre,ResourceType.IronOre})
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.transform.SetParent(root.transform,false);
                var node=go.AddComponent<ResourceNode>();typeof(ResourceNode).GetField("resourceType",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(node,type);
                var amount=node.AmountRemaining;ResourceVisual.Attach(node);
                var visual=go.transform.Find("ResourceModel");
                if(visual==null||!go.GetComponent<Renderer>().forceRenderingOff||node.AmountRemaining!=amount)throw new Exception("Node visual/stock "+type);checks++;
                var collider=go.GetComponent<BoxCollider>();if(collider==null||!collider.isTrigger||collider.size.x<=0)throw new Exception("Hitbox "+type);checks++;
                var path=AssetDatabase.GetAssetPath(visual.GetComponentInChildren<MeshFilter>().sharedMesh);
                if(!path.Contains("Environment1110"))throw new Exception("Old fallback "+type);checks++;
            }
        }
        finally{Object.DestroyImmediate(root);}
        foreach(var biome in new[]{"Forest","Garden","Waterside","Desert","Cave","City"})
        {
            var style=Resources.Load<BiomeMapStyle>("Biomes/"+biome);
            if(style==null||style.layers.Any(l=>l.texture==null||!AssetDatabase.GetAssetPath(l.texture).Contains("Environment1110"))||style.snowGround==null||style.leafGround==null)throw new Exception("Ground style "+biome);checks++;
            var old=File.ReadAllLines("work/environment-1110/biomes-before/"+biome+".asset").Where(l=>!l.TrimStart().StartsWith("- texture:")&&!l.TrimStart().StartsWith("snowGround:")&&!l.TrimStart().StartsWith("leafGround:")).ToArray();
            var current=File.ReadAllLines(AssetDatabase.GetAssetPath(style)).Where(l=>!l.TrimStart().StartsWith("- texture:")&&!l.TrimStart().StartsWith("snowGround:")&&!l.TrimStart().StartsWith("leafGround:")).ToArray();
            if(!old.SequenceEqual(current))throw new Exception("Biome nontexture data changed: "+biome);checks++;
        }
        foreach(var kind in new[]{BuildingKind.Hut,BuildingKind.Storage,BuildingKind.Watchtower})
        {
            var loader=typeof(FacilityVisual).GetMethod("LoadModel",BindingFlags.NonPublic|BindingFlags.Static);
            var model=(GameObject)loader.Invoke(null,new object[]{kind,ResourceType.Soil});
            if(model==null||!AssetDatabase.GetAssetPath(model).Contains("Environment1110"))throw new Exception("Building loader "+kind);checks++;
            var stone=(GameObject)loader.Invoke(null,new object[]{kind,ResourceType.Stone});
            if(stone==null||AssetDatabase.GetAssetPath(stone).Contains("Environment1110"))throw new Exception("Renovation fallback "+kind);checks++;
        }
        report.Add("PASS "+checks+" checks: models, URP textures, node attachment/stock/hitboxes, building loaders, six biome data preservation.");
        File.WriteAllLines("work/environment-1110/validation.txt",report);return string.Join("\n",report);
    }
}
