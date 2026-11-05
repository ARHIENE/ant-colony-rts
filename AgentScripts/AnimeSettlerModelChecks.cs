using System;
using System.IO;
using System.Linq;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;
public static class AnimeSettlerModelChecks
{
 public static string Main()
 {
  var root=new GameObject("Temporary imported character checks");root.transform.position=new Vector3(500,500,500);
  var count=0;
  void Check(bool ok,string why){if(!ok)throw new Exception(why);count++;}
  try {
   var model=KemonoModel.Create(root.transform,1105);var ap=model.GetComponent<KemonoAppearance>();
   var hairs=ap.GetVariants("Hair");var outfits=ap.GetVariants("Outfit");Check(hairs.Length==6,"hair count");Check(outfits.Length==4,"outfit count");
   var animator=model.GetComponentsInChildren<Animator>().First(a=>a.runtimeAnimatorController!=null);
   var skin=model.GetComponentsInChildren<SkinnedMeshRenderer>();
   foreach(var hair in hairs)foreach(var outfit in outfits){
    Check(ap.SetVariant("Hair",hair)&&ap.SetVariant("Outfit",outfit),"variant switch");
    Check(skin.Where(r=>r.enabled&&r.name.StartsWith("Part_Hair_")).All(r=>r.name.StartsWith("Part_Hair_"+hair+"_")),"hair overlap");
    Check(skin.Where(r=>r.enabled&&r.name.StartsWith("Part_Outfit_")).All(r=>r.name.StartsWith("Part_Outfit_"+outfit+"_")),"outfit overlap");
    foreach(var time in new[]{0f,.25f,.5f,.75f}){
     animator.Play("Base Layer.Walk",0,time);animator.Update(0);
     var bounds=skin.Where(r=>r.enabled).Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
     Check(bounds.size.y>.8f&&bounds.size.y<2.8f&&bounds.size.x<3,"invalid animated bounds "+hair+" "+outfit+" "+bounds);
    }
   }
   var face=skin.First(r=>r.name.StartsWith("Part_Face_"));Check(face.sharedMesh.blendShapeCount==8,"expressions count");
   var baked=new Mesh();try{for(var i=0;i<face.sharedMesh.blendShapeCount;i++){face.SetBlendShapeWeight(i,100);face.BakeMesh(baked);Check(baked.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsInfinity(v.y)),"invalid face vertices");face.SetBlendShapeWeight(i,0);}}finally{Object.DestroyImmediate(baked);}
   Check(model.GetComponentsInChildren<Transform>().Any(t=>t.name=="Socket.Tool.R"),"tool socket");
   var report=count+" imported-model checks passed: 24 combinations, 4 walk phases each, 8 facial shapes, tool socket.";
   File.WriteAllText("art/anime-settler-1105/unity-validation.txt",report);return report;
  }finally{Object.DestroyImmediate(root);}
 }
}
