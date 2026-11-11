using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AntColony.Units;
public static class Meshy1111FingerProbe
{
 public static string Run()
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");var entry=lib.motions.First(m=>m.id=="Heavy_Hammer_Swing");
  var pivot=new GameObject("Finger probe");var model=UnityEngine.Object.Instantiate(lib.characters[entry.rig],pivot.transform,false);
  try
  {
   var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);entry.clip.SampleAnimation(entry.armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model,entry.clip.length*.4f);
   var grip=pivot.AddComponent<MeshyNativeEquipment>();grip.Bind(model.transform);grip.SetHandSpan(entry.maxHandSpan);grip.Pose("Tool_Pickaxe_Stone","Tool_Mine");
   var mount=pivot.transform.Find("Native equipment grip");var report=new List<string>();
   foreach(var side in new[]{"Left","Right"})foreach(var finger in new[]{"Thumb","Index","Middle","Ring","Pinky"})
   {
    var chain=Enumerable.Range(1,3).Select(i=>bones["mixamorig:"+side+"Hand"+finger+i]).ToArray();
    var end=chain[2].childCount>0?chain[2].GetChild(0).position:chain[2].position+(chain[2].position-chain[1].position)*.7f;
    report.Add(side+finger+" lastChild="+(chain[2].childCount>0?chain[2].GetChild(0).name:"none")+" tipRadius="+Vector3.Cross(end-mount.position,mount.forward).magnitude+" length="+(Vector3.Distance(chain[0].position,chain[1].position)+Vector3.Distance(chain[1].position,chain[2].position))+" local="+chain[2].localPosition+" endLocal="+chain[2].InverseTransformPoint(end));
   }
   return string.Join("\n",report);
  }finally{UnityEngine.Object.DestroyImmediate(pivot);}
 }
}
