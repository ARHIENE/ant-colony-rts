using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AntColony.Units;
public static class Meshy1111BowProbe
{
 public static string Run()
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");var report=new List<string>();
  foreach(var entry in lib.motions.Where(m=>m.id.StartsWith("Archery_Shot")||m.id=="Female_Bow_Charge_Left_Hand"))
  {
   var pivot=new GameObject("Bow clearance probe");var model=UnityEngine.Object.Instantiate(lib.characters[entry.rig],pivot.transform,false);
   try
   {
    var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
    var grip=pivot.AddComponent<MeshyNativeEquipment>();grip.Bind(model.transform);
    float near=100,minArrow=100;var frames=new List<string>();
    for(int f=0;f<=30;f++)
    {
     entry.clip.SampleAnimation(entry.armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model,entry.clip.length*f/30f);
     grip.Pose("Weapon_ShortBow","BowShoot");
     var bow=pivot.GetComponentsInChildren<MeshFilter>().First(m=>m.transform.IsChildOf(pivot.transform.Find("Native equipment grip")));
     var head=bones["mixamorig:Head"].position;
     float distance=bow.sharedMesh.vertices.Min(v=>Vector3.Distance(bow.transform.TransformPoint(v),head));near=Mathf.Min(near,distance);
     Vector3 Palm(string s)=>Vector3.Lerp(bones["mixamorig:"+s+"Hand"].position,bones["mixamorig:"+s+"HandMiddle1"].position,.65f);
     var a=Palm("Right");var b=Palm("Left");var axis=(b-a).normalized;
     var closest=a+axis*Mathf.Clamp(Vector3.Dot(head-a,axis),0,Vector3.Distance(a,b));
     float arrow=Vector3.Distance(head,closest);minArrow=Mathf.Min(minArrow,arrow);
     if(f%5==0)frames.Add($"{f/30f:F2}:bowHead={distance:F3},stringHead={arrow:F3},draw={Vector3.Distance(a,b):F3}");
    }
    report.Add(entry.id+" minBowHead="+near+" minDrawSegmentHead="+minArrow+" "+string.Join("; ",frames));
   }finally{UnityEngine.Object.DestroyImmediate(pivot);}
  }
  foreach(var name in new[]{"Ammo_Arrow","Weapon_BossC_Quakehammer","Weapon_Spear_Copper","Weapon_ShortBow"})
  {
   var go=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Kemono/Handheld/"+name));
   try{var p=go.GetComponentsInChildren<MeshFilter>().SelectMany(m=>m.sharedMesh.vertices.Select(v=>m.transform.TransformPoint(v))).ToArray();report.Add(name+" xyz="+p.Min(v=>v.x)+".."+p.Max(v=>v.x)+", "+p.Min(v=>v.y)+".."+p.Max(v=>v.y)+", "+p.Min(v=>v.z)+".."+p.Max(v=>v.z));}
   finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  File.WriteAllLines("work/animation-1111/bow-probe.txt",report);return string.Join("\n",report);
 }
}
