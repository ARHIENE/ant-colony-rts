using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
public static class Meshy1111GripProfileProbe
{
 public static string Run()
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");var report=new List<string>();
  foreach(var id in new[]{"Heavy_Hammer_Swing","Charged_Axe_Chop","Right_Hand_Sword_Slash","Archery_Shot_2","Shield_Push_Left"})
  {
   var entry=lib.motions.First(m=>m.id==id);var model=UnityEngine.Object.Instantiate(lib.characters[entry.rig]);
   try
   {
    var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
    Vector3 Palm(string s)=>Vector3.Lerp(bones["mixamorig:"+s+"Hand"].position,bones["mixamorig:"+s+"HandMiddle1"].position,.65f);
    float max=0,min=100;Vector3 previousR=Vector3.zero,previousL=Vector3.zero;float rightTravel=0,leftTravel=0;
    for(int frame=0;frame<=60;frame++)
    {
     entry.clip.SampleAnimation(model,entry.clip.length*frame/60f);var r=Palm("Right");var l=Palm("Left");
     max=Mathf.Max(max,Vector3.Distance(r,l));min=Mathf.Min(min,Vector3.Distance(r,l));
     if(frame>0){rightTravel+=Vector3.Distance(r,previousR);leftTravel+=Vector3.Distance(l,previousL);}previousR=r;previousL=l;
    }
    report.Add(id+" palmSpan="+min+".."+max+" rightTravel="+rightTravel+" leftTravel="+leftTravel);
   }finally{UnityEngine.Object.DestroyImmediate(model);}
  }
  File.WriteAllLines("work/animation-1111/grip-profiles.txt",report);return string.Join("\n",report);
 }
}
