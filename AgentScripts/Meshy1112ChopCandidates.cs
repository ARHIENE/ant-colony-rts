using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AntColony.Units;
public static class Meshy1112ChopCandidates
{
 [Serializable] public class Sample {public float phase,time,span;public Vector3 head,leftFoot,rightFoot;public Quaternion torso;}
 [Serializable] public class Trace {public string id;public float duration;public List<Sample> frames=new List<Sample>();}
 public static string Run()
 {
  var library=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");var report=new List<string>();
  Directory.CreateDirectory("work/animation-1112");
  foreach(string id in new[]{"Charged_Axe_Chop","Axe_Spin_Attack","Left_Slash","Right_Hand_Sword_Slash","Heavy_Hammer_Swing"})
  {
   var entry=library.motions.First(m=>m.id==id);var pivot=new GameObject("Chop candidate "+id);
   try
   {
    var model=UnityEngine.Object.Instantiate(library.characters[entry.rig],pivot.transform,false);
    var bounds=model.GetComponentsInChildren<SkinnedMeshRenderer>().Select(s=>s.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});pivot.transform.localScale=Vector3.one*1.6f/bounds.size.y;
    var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);var hips=bones["mixamorig:Hips"];var origin=hips.localPosition;
    Vector3 Palm(string side)=>Vector3.Lerp(bones["mixamorig:"+side+"Hand"].position,bones["mixamorig:"+side+"HandMiddle1"].position,.65f);
    var trace=new Trace{id=id,duration=entry.clip.length};
    for(int i=0;i<=120;i++)
    {
     entry.clip.SampleAnimation(entry.armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model,entry.clip.length*i/120f);
     var position=hips.localPosition;position.x=origin.x;position.z=origin.z;hips.localPosition=position;
     var right=Palm("Right");var left=Palm("Left");
     trace.frames.Add(new Sample{phase=i/120f,time=entry.clip.length*i/120f,span=Vector3.Distance(right,left),head=right+(right-left).normalized*.10f,leftFoot=bones["mixamorig:LeftFoot"].position,rightFoot=bones["mixamorig:RightFoot"].position,torso=bones["mixamorig:Spine2"].rotation});
    }
    File.WriteAllText("work/animation-1112/"+id+".json",JsonUtility.ToJson(trace,true));
    var windows=new List<string>();
    for(int i=0;i<120;i+=12)
    {
     var a=trace.frames[i];var b=trace.frames[i+12];var delta=b.head-a.head;
     windows.Add($"{a.phase:F1}-{b.phase:F1}: xz={new Vector2(delta.x,delta.z).magnitude:F2} y={Mathf.Abs(delta.y):F2} turn={Quaternion.Angle(a.torso,b.torso):F0} foot={Mathf.Max(Vector3.Distance(a.leftFoot,b.leftFoot),Vector3.Distance(a.rightFoot,b.rightFoot)):F2}");
    }
    report.Add($"{id} {entry.clip.length:F2}s handSpan={trace.frames.Min(f=>f.span):F2}..{trace.frames.Max(f=>f.span):F2}\n"+string.Join("; ",windows));
   }finally{UnityEngine.Object.DestroyImmediate(pivot);}
  }
  File.WriteAllLines("work/animation-1112/chop-candidates.txt",report);return string.Join("\n",report);
 }
}
