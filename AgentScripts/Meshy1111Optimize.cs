using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class Meshy1111Optimize
{
 public static string Run(int start=0,int count=10)
 {
  var paths=AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/Resources/MeshyNative"}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).Skip(start).Take(count).ToArray();
  long before=0,after=0;int changed=0;
  foreach(var path in paths)
  {
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);var bindings=new List<EditorCurveBinding>();var curves=new List<AnimationCurve>();
   foreach(var binding in AnimationUtility.GetCurveBindings(clip))
   {
    var curve=AnimationUtility.GetEditorCurve(clip,binding);var keys=curve.keys;before+=keys.Length;
    if(keys.Length>2&&keys.All(k=>k.value==keys[0].value&&k.inTangent==0&&k.outTangent==0))
    {
     bindings.Add(binding);curves.Add(AnimationCurve.Constant(keys[0].time,keys[keys.Length-1].time,keys[0].value));after+=2;changed++;
    }
    else after+=keys.Length;
   }
   if(bindings.Count>0){AnimationUtility.SetEditorCurves(clip,bindings.ToArray(),curves.ToArray());EditorUtility.SetDirty(clip);}
  }
  AssetDatabase.SaveAssets();var report="Clips "+start+".."+(start+paths.Length-1)+": exact constant curves="+changed+", keys "+before+" -> "+after+". No changing curves modified.";
  File.AppendAllText("work/animation-1111/constant-optimization.txt",report+"\n");return report;
 }
}
