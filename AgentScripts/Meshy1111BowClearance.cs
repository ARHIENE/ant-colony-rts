using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AntColony.Units;
public static class Meshy1111BowClearance
{
 public static string Run()
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");var lines=new List<string>();
  foreach(var id in new[]{"Archery_Shot","Archery_Shot_1","Archery_Shot_2","Archery_Shot_3"})
  {
   var e=lib.motions.First(m=>m.id==id);var pivot=new GameObject("Bow head clearance");var model=UnityEngine.Object.Instantiate(lib.characters[e.rig],pivot.transform,false);
   try
   {
    var bs=model.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});pivot.transform.localScale=Vector3.one*(1.6f/bs.size.y);
    var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);var head=bones["mixamorig:Head"];var wrist=bones["mixamorig:RightHand"];
    Vector3 Palm(string s)=>Vector3.Lerp(bones["mixamorig:"+s+"Hand"].position,bones["mixamorig:"+s+"HandMiddle1"].position,.65f);
    float minBefore=100,minAfter=100,maxShift=0,maxLength=0;
    for(int f=0;f<=40;f++)
    {
     e.clip.SampleAnimation(e.armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model,e.clip.length*f/40f);
     var centre=head.position+Vector3.up*.10f;var a=Palm("Right");var b=Palm("Left");var upper=bones["mixamorig:RightArm"];var lower=bones["mixamorig:RightForeArm"];
     float length=Vector3.Distance(upper.position,lower.position)+Vector3.Distance(lower.position,wrist.position);
     minBefore=Mathf.Min(minBefore,Distance(centre,a,b));
     var d=b-a;var closest=a+d*Mathf.Clamp01(Vector3.Dot(centre-a,d)/Mathf.Max(d.sqrMagnitude,1e-8f));
     var shift=(closest-centre).normalized*Mathf.Max(0,.23f-Distance(centre,a,b));
     Solve(upper,lower,wrist,wrist.position+shift,pivot.transform.right);
     Solve(bones["mixamorig:LeftArm"],bones["mixamorig:LeftForeArm"],bones["mixamorig:LeftHand"],bones["mixamorig:LeftHand"].position+shift,-pivot.transform.right);
     minAfter=Mathf.Min(minAfter,Distance(centre,Palm("Right"),Palm("Left")));maxShift=Mathf.Max(maxShift,Vector3.Distance(a,Palm("Right")));
     maxLength=Mathf.Max(maxLength,Mathf.Abs(length-Vector3.Distance(upper.position,lower.position)-Vector3.Distance(lower.position,wrist.position)));
    }
    lines.Add($"{id}: before={minBefore:F3} after={minAfter:F3} maxShift={maxShift:F3} lengthError={maxLength:F6}");
   }finally{UnityEngine.Object.DestroyImmediate(pivot);}
  }
  return string.Join("\n",lines);
 }
 public static float Distance(Vector3 point,Vector3 a,Vector3 b)
 {var d=b-a;return Vector3.Distance(point,a+d*Mathf.Clamp01(Vector3.Dot(point-a,d)/Mathf.Max(d.sqrMagnitude,1e-8f)));}
 public static void Solve(Transform upper,Transform lower,Transform wrist,Vector3 target,Vector3 fallback)
 {
  var rotation=wrist.rotation;var a=upper.position;float l1=Vector3.Distance(a,lower.position),l2=Vector3.Distance(lower.position,wrist.position);
  var axis=(target-a).normalized;float distance=Mathf.Clamp(Vector3.Distance(target,a),Mathf.Abs(l1-l2)+.001f,l1+l2-.001f);target=a+axis*distance;
  var bend=Vector3.ProjectOnPlane(lower.position-a,axis);if(bend.sqrMagnitude<.00001f)bend=Vector3.ProjectOnPlane(fallback,axis);
  float along=(l1*l1+distance*distance-l2*l2)/(2*distance);var elbow=a+axis*along+bend.normalized*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
  upper.rotation=Quaternion.FromToRotation(lower.position-a,elbow-a)*upper.rotation;
  lower.rotation=Quaternion.FromToRotation(wrist.position-lower.position,target-lower.position)*lower.rotation;wrist.rotation=rotation;
 }
}
