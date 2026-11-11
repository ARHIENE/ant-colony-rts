using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
public static class Meshy1111LibraryChecks
{
 public static string Facing()
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");
  return string.Join("\n",lib.characters.Select(c=>
  {
   var bones=c.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);var right=(bones["mixamorig:RightArm"].position-bones["mixamorig:LeftArm"].position).normalized;
   return c.name+" rootRotation="+c.transform.eulerAngles+" shoulderRight="+right+" anatomicalForward="+Vector3.Cross(right,Vector3.up);
  }));
 }
 public static string Run(int start=0,int count=20)
 {
  var library=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");
  var report=new List<string>();int errors=0;
  for(int i=start;i<Math.Min(start+count,library.motions.Length);i++)
  {
   var motion=library.motions[i];var model=UnityEngine.Object.Instantiate(library.characters[motion.rig]);
   try
   {
    var bones=model.GetComponentsInChildren<Transform>();
    var tracked=bones.Where(t=>t.name=="mixamorig:RightArm"||t.name=="mixamorig:RightForeArm"||t.name=="mixamorig:LeftLeg"||t.name=="mixamorig:RightLeg").ToArray();
    motion.clip.SampleAnimation(motion.armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model,0);var initial=tracked.Select(t=>t.localRotation).ToArray();float activity=0,limbVariation=0;
    var lengths=tracked.Select(t=>t.localPosition.magnitude).ToArray();
    for(int f=1;f<=20;f++)
    {
     motion.clip.SampleAnimation(motion.armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model,motion.clip.length*f/20);
     for(int j=0;j<tracked.Length;j++)
     {
      activity=Mathf.Max(activity,Quaternion.Angle(initial[j],tracked[j].localRotation));
      limbVariation=Mathf.Max(limbVariation,Mathf.Abs(lengths[j]-tracked[j].localPosition.magnitude));
      var p=tracked[j].position;if(float.IsNaN(p.x)||float.IsInfinity(p.x))throw new Exception("Nonfinite bone");
     }
    }
    if(activity<.1f||limbVariation>.005f)errors++;
    report.Add(motion.id+" rig="+motion.rig+" jointMotion="+activity.ToString("F2")+" lengthVariation="+limbVariation.ToString("F6")+(activity<.1f||limbVariation>.005f?" REVIEW":" PASS"));
   }
   finally{UnityEngine.Object.DestroyImmediate(model);}
  }
  Directory.CreateDirectory("work/animation-1111");File.WriteAllLines("work/animation-1111/library-check-"+start+".txt",report);
  return string.Join("\n",report)+"\nReview flags="+errors;
 }
 public static string Tools()
 {
  return string.Join("\n",new[]{"Tool_Pickaxe_Stone","Tool_Axe_Stone","Tool_Hammer_Stone","Weapon_Sword_Copper","Weapon_ShortBow","Ammo_Arrow"}.Select(name=>
  {
   var go=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Kemono/Handheld/"+name));
   var bounds=go.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
   string result=name+" rot="+go.transform.eulerAngles+" scale="+go.transform.localScale+" bounds="+bounds;
   UnityEngine.Object.DestroyImmediate(go);return result;
  }));
 }
 public static string PickaxeAxis()
 {
  var go=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Kemono/Handheld/Tool_Pickaxe_Stone"));
  try
  {
   var points=go.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToArray();
   float min=points.Min(v=>v.z),max=points.Max(v=>v.z);
   return string.Join("\n",Enumerable.Range(0,8).Select(i=>{float a=Mathf.Lerp(min,max,i/8f),b=Mathf.Lerp(min,max,(i+1)/8f);var p=points.Where(v=>v.z>=a&&v.z<=b).ToArray();return a.ToString("F3")+".."+b.ToString("F3")+" width="+(p.Length==0?0:p.Max(v=>v.x)-p.Min(v=>v.x));}));
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 public static string RigidGrip()
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");var motion=lib.motions.First(m=>m.id=="Heavy_Hammer_Swing");var go=UnityEngine.Object.Instantiate(lib.characters[motion.rig]);
  try
  {
   var bones=go.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);var right=bones["mixamorig:RightHand"];
   Vector3 Palm(string side)=>Vector3.Lerp(bones["mixamorig:"+side+"Hand"].position,bones["mixamorig:"+side+"HandMiddle1"].position,.65f);
   motion.clip.SampleAnimation(go,motion.clip.length*.2f);var axis=(Palm("Left")-Palm("Right")).normalized;var local=right.InverseTransformDirection(axis);
   return string.Join("\n",Enumerable.Range(0,21).Select(i=>{motion.clip.SampleAnimation(go,motion.clip.length*i/20f);var offset=Palm("Left")-Palm("Right");return (i/20f).ToString("F2")+" gap="+Vector3.Cross(offset,right.TransformDirection(local)).magnitude.ToString("F3")+" along="+Vector3.Dot(offset,right.TransformDirection(local)).ToString("F3");}));
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}
