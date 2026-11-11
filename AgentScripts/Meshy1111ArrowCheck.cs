using System;
using System.Linq;
using UnityEngine;
using AntColony.Units;
public static class Meshy1111ArrowCheck
{
 public static string Run()
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");
  var entry=lib.motions.First(m=>m.id==lib.actions.First(a=>a.action=="BowShoot").motion);
  int count=0;float maxNock=0,maxLength=0;
  foreach(float scale in new[]{.75f,1f,2f})
  {
   var pivot=new GameObject("Arrow world geometry check");pivot.transform.localScale=Vector3.one*scale;
   try
   {
    var model=UnityEngine.Object.Instantiate(lib.characters[entry.rig],pivot.transform,false);
    var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
    var grip=pivot.AddComponent<MeshyNativeEquipment>();grip.Bind(model.transform);
    Vector3 Palm(string s)=>Vector3.Lerp(bones["mixamorig:"+s+"Hand"].position,bones["mixamorig:"+s+"HandMiddle1"].position,.65f);
    foreach(float phase in new[]{.02f,.1f,.5f,.8f})
    {
     entry.clip.SampleAnimation(entry.armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model,entry.clip.length*phase);
     grip.Pose("Weapon_ShortBow","BowShoot",phase);
     var arrow=pivot.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Ammo_Arrow(Clone)");
     bool expected=phase>=0&&phase<.16f;
     if(arrow.gameObject.activeSelf!=expected)throw new Exception("Arrow visibility phase mismatch");
     if(!expected)continue;
     var nock=Palm("Right");var aim=Palm("Left")-nock;var axis=aim.normalized;
     var points=arrow.GetComponentsInChildren<MeshFilter>().SelectMany(m=>m.sharedMesh.vertices.Select(v=>m.transform.TransformPoint(v))).ToArray();
     float min=points.Min(p=>Vector3.Dot(p-nock,axis)),max=points.Max(p=>Vector3.Dot(p-nock,axis));
     maxNock=Mathf.Max(maxNock,Mathf.Abs(min));maxLength=Mathf.Max(maxLength,Mathf.Abs(max-min-(aim.magnitude+.08f)));
     if(Mathf.Abs(min)>.01f||Mathf.Abs(max-min-(aim.magnitude+.08f))>.01f)throw new Exception($"Arrow geometry wrong scale={scale} nock={min} span={max-min} expected={aim.magnitude+.08f}");
     count++;
    }
    grip.Pose(null,"Sleep");
    if(pivot.GetComponentsInChildren<Transform>().Any(t=>t.name=="Ammo_Arrow(Clone)"&&t.gameObject.activeInHierarchy))throw new Exception("Arrow remains after unequip");
   }finally{UnityEngine.Object.DestroyImmediate(pivot);}
  }
  return $"PASS {count} arrow mesh poses across three character scales; max nock error={maxNock:F5}m length error={maxLength:F5}m; release and unequip hide arrow";
 }
}
