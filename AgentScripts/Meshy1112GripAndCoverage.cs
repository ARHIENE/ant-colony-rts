using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AntColony.Units;
public static class Meshy1112GripAndCoverage
{
 public static string Run()
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");
  var runtime=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");
  Directory.CreateDirectory("work/animation-1112");
  var rows=new List<string>{"motion,rig,runtime_actions,visual_review"};
  foreach(var m in lib.motions)
  {
   var actions=string.Join(";",runtime.actions.Where(a=>a.motion==m.id).Select(a=>a.action));
   rows.Add(m.id+","+m.rig+","+(actions==""?"UNBOUND":actions)+",PENDING_FULL_CYCLE_REVIEW");
  }
  File.WriteAllLines("work/animation-1112/all-motion-coverage.csv",rows);
  var report=new List<string>();
  foreach(var rig in Enumerable.Range(0,lib.characters.Length))
  {
   var pivot=new GameObject("One-handed grip regression");
   try
   {
    var model=UnityEngine.Object.Instantiate(lib.characters[rig],pivot.transform,false);
    var m=lib.motions.First(x=>x.rig==rig);
    m.clip.SampleAnimation(m.armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model,m.clip.length*.3f);
    var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
    var fingers=bones.Values.Where(t=>t.name.StartsWith("mixamorig:RightHand")&&t.name!="mixamorig:RightHand").ToArray();
    var authored=fingers.Select(t=>t.localRotation).ToArray();
    var arm=bones["mixamorig:RightForeArm"].localRotation;
    var grip=pivot.AddComponent<MeshyNativeEquipment>();grip.Bind(model.transform);
    grip.Pose("Tool_Hammer_Stone","Walk");
    float delta=fingers.Select((t,i)=>Quaternion.Angle(authored[i],t.localRotation)).Max();
    if(delta<.1f||Quaternion.Angle(arm,bones["mixamorig:RightForeArm"].localRotation)>.01f)throw new Exception("One-hand grip failed rig "+rig);
    grip.Pose(null,"Idle");
    if(grip.FingerContactError!=0||grip.SupportShaftError!=0||grip.BowHeadClearance!=0)throw new Exception("Stale grip diagnostics");
    report.Add("rig="+rig+" one-hand fingers="+delta.ToString("F2")+"deg; arm unchanged; unequip diagnostics clear PASS");
   }finally{UnityEngine.Object.DestroyImmediate(pivot);}
  }
  report.Add("Coverage="+lib.motions.Length+" clips; runtime="+runtime.motions.Length+" clips / "+runtime.actions.Length+" bindings. Unbound clips and full visual reviews remain explicit in CSV.");
  File.WriteAllLines("work/animation-1112/grip-regression.txt",report);return string.Join("\n",report);
 }
}
