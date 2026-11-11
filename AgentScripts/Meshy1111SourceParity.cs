using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
public static class Meshy1111SourceParity
{
 public static string Run(int start=0,int count=20)
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");
  var originals=lib.characters.Select(UnityEngine.Object.Instantiate).ToArray();
  var copies=lib.characters.Select(UnityEngine.Object.Instantiate).ToArray();var report=new List<string>();
  try
  {
   for(int i=start;i<Math.Min(lib.motions.Length,start+count);i++)
   {
    var entry=lib.motions[i];var original=originals[entry.rig];var copy=copies[entry.rig];
    var old=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/MeshyNative/"+entry.id+".anim");
    var a=original.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
    var b=copy.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);float angle=0,position=0;
    foreach(float phase in new[]{.07f,.47f,.89f})
    {
     entry.clip.SampleAnimation(entry.armatureRoot?original.transform.Find("SmartRigArmature").gameObject:original,entry.clip.length*phase);
     old.SampleAnimation(copy,old.length*phase);
     foreach(var pair in a.Where(p=>p.Key.StartsWith("mixamorig:")))
     {
      angle=Mathf.Max(angle,Quaternion.Angle(pair.Value.localRotation,b[pair.Key].localRotation));
      position=Mathf.Max(position,Vector3.Distance(pair.Value.localPosition,b[pair.Key].localPosition));
     }
    }
    report.Add(entry.id+" angle="+angle+" position="+position+(angle<.06f&&position<.00001f?" PASS":" FAIL"));
   }
  }
  finally{foreach(var model in originals.Concat(copies))UnityEngine.Object.DestroyImmediate(model);}
  File.WriteAllLines("work/animation-1111/source-parity-"+start+".txt",report);return string.Join("\n",report);
 }
}
