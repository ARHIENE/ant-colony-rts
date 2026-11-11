using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class Meshy1111Inventory
{
 public static string Rigs()
 {
  var paths=new[]{"Assets/_Project/Art/MeshyAnimation1106/Heavy_Hammer_Swing.fbx","Assets/_Project/Art/MeshyAnimation1106/Rerig_All.fbx"};
  var poses=paths.Select(path=>{var m=AssetDatabase.LoadAssetAtPath<GameObject>(path);var s=m.GetComponentInChildren<SkinnedMeshRenderer>();return s.bones.Select((b,i)=>(b.name,m:s.transform.localToWorldMatrix*s.sharedMesh.bindposes[i].inverse)).ToDictionary(p=>p.name,p=>p.m);}).ToArray();
  return string.Join("\n",poses[0].Keys.Where(n=>n.Contains("Arm")||n.Contains("Hand")||n.Contains("Leg")||n.Contains("Hips")).Select(n=>n+" rest angle="+Quaternion.Angle(poses[0][n].rotation,poses[1][n].rotation)+" pos="+Vector3.Distance(poses[0][n].GetColumn(3),poses[1][n].GetColumn(3))));
 }
 public static string Inspect()
 {
  var report=new List<string>();
  foreach(var path in AssetDatabase.FindAssets("t:Model",new[]{"Assets/_Project/Art/MeshyAnimation1106","Assets/_Project/Art/MeshyAnimation1110"}).Select(AssetDatabase.GUIDToAssetPath))
  {
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var bones=model.GetComponentsInChildren<Transform>();
   var hand=bones.FirstOrDefault(t=>t.name=="mixamorig:RightHand");var forearm=bones.FirstOrDefault(t=>t.name=="mixamorig:RightForeArm");
   var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview"));
   foreach(var c in clips)report.Add(path+" | "+c.name+" length="+c.length+" skins="+model.GetComponentsInChildren<SkinnedMeshRenderer>().Length+" forearm="+(hand&&forearm?Vector3.Distance(hand.position,forearm.position):0)+" root="+AnimationUtility.GetCurveBindings(c).FirstOrDefault().path);
  }
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Kemono/KemonoAnimator.controller");
  report.Add("\nCONTROLLER");foreach(var s in controller.layers[0].stateMachine.states)report.Add(s.state.name+" => "+AssetDatabase.GetAssetPath(s.state.motion));
  Directory.CreateDirectory("work/animation-1111");File.WriteAllLines("work/animation-1111/native-inventory.txt",report);
  return string.Join("\n",report);
 }
}
