using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AntColony.Units;
public static class Native1112Transitions
{
 static readonly string[] Once={"Arise","Dead","falling_down","Hit_Reaction","Shot_and_Blown_Back","Ladder_Climb_Finish","Ladder_Mount_Start","Sit_to_Stand_Transition_M","Stand_to_Sit_Transition_M","Walk_to_Sit","Stand_Clap_and_Sit_Down","Sit_Lie_Bed","sleep","Regular_Jump","Jump_Over_Obstacle_1","Roll_Dodge"};
 public static string Inspect()
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");
  return string.Join("\n",lib.motions.Where(m=>Once.Contains(m.id)).Select(m=>$"{m.id}: loop={m.loop}"));
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
  int count=0;
  foreach(var path in new[]{"Assets/Resources/MeshyNative/Library.asset","Assets/Resources/MeshyNative/Runtime.asset"}) {
   var lib=AssetDatabase.LoadAssetAtPath<MeshyNativeLibrary>(path);
   foreach(var m in lib.motions)if(Once.Contains(m.id)&&m.loop){m.loop=false;count++;}
   EditorUtility.SetDirty(lib);
  }
  AssetDatabase.SaveAssets();return "Marked one-shot transitions (not cyclic locomotion/work): "+count;
 }
}
