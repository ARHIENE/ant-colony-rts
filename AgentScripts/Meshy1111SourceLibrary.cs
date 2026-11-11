using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AntColony.Units;
public static class Meshy1111SourceLibrary
{
 public static string Migrate()
 {
  int count=0;
  foreach(var asset in new[]{"Assets/Resources/MeshyNative/Library.asset","Assets/Resources/MeshyNative/Runtime.asset"})
  {
   var lib=AssetDatabase.LoadAssetAtPath<MeshyNativeLibrary>(asset);
   foreach(var motion in lib.motions)
   {
    var path="Assets/_Project/Art/MeshyNativeSources/"+motion.id+".fbx";
    var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
    var bindings=AnimationUtility.GetCurveBindings(clip);
    bool armature=bindings.Any(b=>b.path.StartsWith("mixamorig:"));
    string prefix=armature?"SmartRigArmature/":"";
    foreach(var b in bindings.Where(b=>b.type==typeof(Transform)&&b.path!=""))
     if(!lib.characters[motion.rig].transform.Find(prefix+b.path))throw new Exception(motion.id+" unmatched "+prefix+b.path);
    motion.clip=clip;
    typeof(MeshyNativeLibrary.Motion).GetField("armatureRoot").SetValue(motion,armature);
    if(motion.id=="Heavy_Hammer_Swing"||motion.id=="Charged_Axe_Chop")
    {
     var model=UnityEngine.Object.Instantiate(lib.characters[motion.rig]);
     try
     {
      var root=armature?model.transform.Find("SmartRigArmature").gameObject:model;
      var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
      Vector3 Palm(string s)=>Vector3.Lerp(bones["mixamorig:"+s+"Hand"].position,bones["mixamorig:"+s+"HandMiddle1"].position,.65f);
      float span=0;
      for(int i=0;i<=60;i++){clip.SampleAnimation(root,clip.length*i/60f);span=Mathf.Max(span,Vector3.Distance(Palm("Left"),Palm("Right")));}
      typeof(MeshyNativeLibrary.Motion).GetField("maxHandSpan").SetValue(motion,span);
     }finally{UnityEngine.Object.DestroyImmediate(model);}
    }
    count++;
   }
   EditorUtility.SetDirty(lib);
  }
  AssetDatabase.SaveAssets();return "Direct original FBX clips bound: "+count+" entries across full/runtime libraries. No pose/curve retarget. Root paths verified; work grip spans sampled.";
 }
}
