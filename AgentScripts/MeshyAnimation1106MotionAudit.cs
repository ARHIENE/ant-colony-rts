using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class MeshyAnimation1106MotionAudit
{
 public static string Main(string file="Heavy_Hammer_Swing.fbx"){
 var path="Assets/_Project/Art/MeshyAnimation1106/"+file;
 var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
 try{
 foreach(var a in model.GetComponentsInChildren<Animator>())a.enabled=false;
 var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
 var text=new StringBuilder();var bindings=AnimationUtility.GetCurveBindings(clip);
 text.AppendLine("clip="+clip.name+" bindings="+bindings.Length);
 foreach(var binding in bindings.Where(b=>b.path.EndsWith("RightHand")))
 text.AppendLine(binding.path+" "+binding.propertyName+" resolved="+(model.transform.Find(binding.path)!=null));
 var nodes=model.GetComponentsInChildren<Transform>();
 var left=nodes.First(t=>t.name=="mixamorig:LeftHand");var right=nodes.First(t=>t.name=="mixamorig:RightHand");
 var torso=nodes.First(t=>t.name=="mixamorig:Spine2");
 for(int i=0;i<=12;i++){clip.SampleAnimation(model,clip.length*i/12f);text.AppendLine($"phase={i/12f:F2} left={left.position.ToString("F4")} right={right.position.ToString("F4")} handDistance={Vector3.Distance(left.position,right.position):F4} torso={torso.position.ToString("F4")}");}
 text.AppendLine("ROOT "+model.transform.rotation+" child="+model.transform.GetChild(0).localRotation); File.WriteAllText("art/meshy-animation-1106/"+file+"-audit.txt",text.ToString());return text.ToString();
 }finally{UnityEngine.Object.DestroyImmediate(model);}
 }
}

