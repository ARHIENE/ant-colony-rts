using System;
using System.Linq;
using UnityEngine;
using AntColony.Units;
public static class Meshy1111BowRegression
{
 public static string Run()
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");var e=lib.motions.First(m=>m.id==lib.actions.First(a=>a.action=="BowShoot").motion);
  float clearance=100,lengthError=0;int poses=0;
  foreach(var bow in new[]{"Weapon_ShortBow","Weapon_GrandBow"})
  {
   var pivot=new GameObject("Bow regression");var model=UnityEngine.Object.Instantiate(lib.characters[e.rig],pivot.transform,false);
   try
   {
    var bounds=MeshyNativeBounds.Rest(model.GetComponentsInChildren<SkinnedMeshRenderer>());pivot.transform.localScale=Vector3.one*1.6f/bounds.size.y;
    var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);var grip=pivot.AddComponent<MeshyNativeEquipment>();grip.Bind(model.transform);
    for(int i=0;i<=60;i++)
    {
     float phase=i/60f;e.clip.SampleAnimation(e.armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model,e.clip.length*phase);
     var names=new[]{"RightForeArm","RightHand","LeftForeArm","LeftHand"};
     var lengths=names.Select(n=>Vector3.Distance(bones["mixamorig:"+n].position,bones["mixamorig:"+n].parent.position)).ToArray();
     grip.Pose(bow,"BowShoot",phase);clearance=Mathf.Min(clearance,grip.BowHeadClearance);
     for(int j=0;j<names.Length;j++)lengthError=Mathf.Max(lengthError,Mathf.Abs(lengths[j]-Vector3.Distance(bones["mixamorig:"+names[j]].position,bones["mixamorig:"+names[j]].parent.position)));
     var arrow=pivot.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Ammo_Arrow(Clone)");
     if(arrow.gameObject.activeSelf!=(phase<.16f))throw new Exception("Release visibility mismatch");poses++;
    }
   }finally{UnityEngine.Object.DestroyImmediate(pivot);}
  }
  if(clearance<.22f||lengthError>.0001f)throw new Exception($"Bow correction failed clearance={clearance} lengthError={lengthError}");
  return $"PASS {poses} short/grand bow poses; head-to-arrow-line minimum={clearance:F4}m, arm segment length change={lengthError:F6}m; release visibility correct. Mesh/hair clearance requires visual check.";
 }
}
