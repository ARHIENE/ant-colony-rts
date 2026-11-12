using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using AntColony.Units;
public static class Appearance1112LegacyInspect {
 static string V(Vector2 v)=>Mathf.RoundToInt(v.x*100000)+","+Mathf.RoundToInt(v.y*100000);
 static string Key(Vector2 a,Vector2 b,Vector2 c)=>string.Join("|",new[]{V(a),V(b),V(c)}.OrderBy(x=>x,StringComparer.Ordinal));
 public static string Run(){
  var keys=new HashSet<string>(File.ReadAllLines("work/appearance-overlap-1112/hair-uv-triangle-keys.txt"));
  var model=Resources.Load<GameObject>("Kemono/Kemono_Character");string report="";
  foreach(var s in model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s=>s.name.StartsWith("Part_Face_")||s.name.StartsWith("Part_Hair_BOB_"))){var m=s.sharedMesh;var t=m.triangles;var uv=m.uv;int count=0;for(int i=0;i<t.Length;i+=3)if(keys.Contains(Key(uv[t[i]],uv[t[i+1]],uv[t[i+2]])))count++;report+=s.name+" triangles="+t.Length/3+" matching misplaced hair="+count+" bones="+string.Join(",",s.bones.Select(b=>b.name))+"\n";}
  File.WriteAllText("work/appearance-overlap-1112/legacy-inspect.txt",report);return report;
 }
}
