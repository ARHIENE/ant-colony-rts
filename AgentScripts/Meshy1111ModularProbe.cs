using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class Meshy1111ModularProbe
{
 public static string Run()
 {
  var old=Resources.Load<GameObject>("Kemono/Kemono_Character");
  string Key(Vector2 uv)=>Mathf.RoundToInt(uv.x*100000)+":"+Mathf.RoundToInt(uv.y*100000);
  var uvParts=new Dictionary<string,HashSet<string>>();
  foreach(var skin in old.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s=>s.name.StartsWith("Part_")))
  {
   string category=skin.name.Split('_')[1];
   foreach(var uv in skin.sharedMesh.uv){var key=Key(uv);if(!uvParts.TryGetValue(key,out var categories))uvParts[key]=categories=new HashSet<string>();categories.Add(category);}
  }
  var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/MeshyAnimation1106/Heavy_Hammer_Swing.fbx");
  var native=model.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;int matched=0,ambiguous=0;var counts=new Dictionary<string,int>();
  foreach(var uv in native.uv)if(uvParts.TryGetValue(Key(uv),out var categories)){matched++;if(categories.Count>1)ambiguous++;foreach(var c in categories){counts.TryGetValue(c,out var count);counts[c]=count+1;}}
  return "Native vertices="+native.vertexCount+" exact UV matches="+matched+" ambiguous categories="+ambiguous+"\n"+string.Join("\n",counts.Select(p=>p.Key+"="+p.Value));
 }
}
