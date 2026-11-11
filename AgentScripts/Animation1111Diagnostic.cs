using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
public static class Animation1111Diagnostic
{
 public static string Front(){var c=AntColony.Core.CommanderRoster.Instance.Commanders.First(x=>x!=null&&x.IsColonyMember);var m=c.GetComponentInChildren<MeshyEquipmentGrip>().transform;var focus=m.GetComponentsInChildren<Transform>().First(t=>t.name=="head").position-Vector3.up*.15f;var cam=Camera.main;cam.orthographicSize=.9f;cam.transform.position=focus+m.forward*2+m.right*.6f+Vector3.up*.25f;cam.transform.LookAt(focus);return "Front three-quarter view";}
 public static string HeadSkin(){var c=AntColony.Core.CommanderRoster.Instance.Commanders.First(x=>x!=null&&x.IsColonyMember);var m=c.GetComponentInChildren<MeshyEquipmentGrip>().transform;var s=m.GetComponentsInChildren<SkinnedMeshRenderer>().First(x=>x.name.StartsWith("Part_Face_NEUTRAL"));var h=s.bones.First(t=>t.name=="head");int index=Array.IndexOf(s.bones,h);var ws=s.sharedMesh.boneWeights;var mesh=new Mesh();s.BakeMesh(mesh);var vs=mesh.vertices;var b=new Bounds();bool first=true;for(int i=0;i<vs.Length;i++)if(ws[i].boneIndex0==index&&ws[i].weight0>.5f){var p=h.InverseTransformPoint(s.transform.TransformPoint(vs[i]));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}UnityEngine.Object.DestroyImmediate(mesh);return "headLocalBounds="+b+" headWorldScale="+h.lossyScale+" headInModel="+m.InverseTransformPoint(h.position);}
 public static async Task<string> Step(int milliseconds=450){EditorApplication.isPaused=false;await Task.Delay(milliseconds);EditorApplication.isPaused=true;return "Advanced actual gameplay "+milliseconds+"ms";}
 public static string HeadBounds(){var c=AntColony.Core.CommanderRoster.Instance.Commanders.First(x=>x!=null&&x.IsColonyMember);var m=c.GetComponentInChildren<MeshyEquipmentGrip>().transform;return string.Join("\n",m.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&(r.name.Contains("Face")||r.name.Contains("Hair")||r.name.Contains("Body"))).Select(r=>r.name+" center="+m.InverseTransformPoint(r.bounds.center)+" size="+r.bounds.size));}
 public static string Pose(float phase){EditorApplication.isPaused=true;var c=AntColony.Core.CommanderRoster.Instance.Commanders.First(x=>x!=null&&x.IsColonyMember);var a=RigAnimator.Find(c.gameObject);a.Play("Base Layer.Tool_Mine",0,phase);a.Update(0);var g=c.GetComponentInChildren<MeshyEquipmentGrip>();g.Pose(false);return "Mining phase "+phase;}
 public static string BakeWork(){foreach(var n in new[]{"Heavy_Hammer_Swing","Charged_Axe_Chop"})AntColony.EditorTools.MeshyMotionImport.Bake("Assets/_Project/Art/MeshyAnimation1110/"+n+".fbx",n,true);AssetDatabase.SaveAssets();return LoopAudit();}
 public static string LoopAudit(){var owner=new GameObject("Loop audit");try{var m=KemonoModel.Create(owner.transform,1111);var a=RigAnimator.Find(m);a.enabled=false;var ts=a.GetComponentsInChildren<Transform>();var sb=new StringBuilder();foreach(var name in new[]{"Heavy_Hammer_Swing","Charged_Axe_Chop","Running"}){var clip=Resources.Load<AnimationClip>("Kemono/MeshyMotions/"+name);clip.SampleAnimation(a.gameObject,0);var q=ts.Select(t=>t.localRotation).ToArray();clip.SampleAnimation(a.gameObject,clip.length-.0001f);sb.AppendLine(name+" duration="+clip.length+" seam="+string.Join(",",ts.Select((t,i)=>(name:t.name,angle:Quaternion.Angle(q[i],t.localRotation))).OrderByDescending(x=>x.angle).Take(5).Select(x=>x.name+":"+x.angle.ToString("F1"))));}return sb.ToString();}finally{UnityEngine.Object.DestroyImmediate(owner);}}
 public static string SourceHands() {
  var owner=new GameObject("Source hands");try{var m=KemonoModel.Create(owner.transform,1111);var a=RigAnimator.Find(m);a.enabled=false;var ts=m.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);var clip=Resources.Load<AnimationClip>("Kemono/MeshyMotions/Heavy_Hammer_Swing");var sb=new StringBuilder();
  for(int i=0;i<12;i++){clip.SampleAnimation(a.gameObject,clip.length*i/12);sb.AppendLine(i+" "+string.Join(" ",new[]{"head","upper_arm.R","hand.R","hand.L","Socket.Tool.R"}.Select(n=>n+"="+m.transform.InverseTransformPoint(ts[n].position).ToString("F2")))+" shaft="+m.transform.InverseTransformDirection(ts["Socket.Tool.R"].forward).ToString("F2"));}return sb.ToString();}finally{UnityEngine.Object.DestroyImmediate(owner);}
 }
 public static async Task<string> Live()
 {
  var list=UnityEngine.Object.FindObjectsByType<AntVisual>(FindObjectsInactive.Exclude);
  var sb=new StringBuilder();
  foreach(var v in list.Take(6)) {
   var a=RigAnimator.Find(v.gameObject);if(!a){sb.AppendLine(v.name+" NO ANIMATOR");continue;}
   var bones=a.GetComponentsInChildren<Transform>().Where(t=>new[]{"thigh.L","shin.L","upper_arm.R","forearm.R","spine"}.Contains(t.name)).ToArray();
   var q=bones.Select(t=>t.localRotation).ToArray();var t0=a.GetCurrentAnimatorStateInfo(0).normalizedTime;var p0=v.transform.position;
   await Task.Delay(650);
   sb.AppendLine(v.name+" state="+v.StateName+" clip="+a.GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip?.name+" enabled="+a.enabled+" speed="+a.speed+" timescale="+Time.timeScale+" time="+t0+"->"+a.GetCurrentAnimatorStateInfo(0).normalizedTime+" travel="+Vector3.Distance(p0,v.transform.position)+" rotations="+string.Join(",",bones.Select((t,i)=>t.name+":"+Quaternion.Angle(q[i],t.localRotation).ToString("F2"))));
  }
  return sb.ToString();
 }
 public static string Probe()
 {
  var root=new GameObject("Animation1111Diagnostic");
  try {
   var model=KemonoModel.Create(root.transform,1111);var a=RigAnimator.Find(model);
   var bones=model.GetComponentsInChildren<Transform>(true).Where(t=>new[]{"thigh.L","shin.L","upper_arm.R","forearm.R","spine"}.Contains(t.name)).ToArray();
   var sb=new StringBuilder();sb.AppendLine($"playing={Application.isPlaying} animator={a.name} enabled={a.enabled} initialized={a.isInitialized} avatar={a.avatar?.name} valid={a.avatar?.isValid} controller={a.runtimeAnimatorController.name}");
   foreach(var state in new[]{"Idle_A","IdleVariant0","Run","Walk","Tool_Mine","Tool_Chop","Sleep"}) {
    a.Play("Base Layer."+state,0,0);a.Update(0);var before=bones.Select(t=>t.localRotation).ToArray();
    var clip=a.GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip;
    a.Update(.37f);sb.AppendLine(state+" clip="+clip?.name+" time="+a.GetCurrentAnimatorStateInfo(0).normalizedTime+" rotations="+string.Join(",",bones.Select((t,i)=>t.name+":"+Quaternion.Angle(before[i],t.localRotation).ToString("F2"))));
    if(clip!=null){var bindings=AnimationUtility.GetCurveBindings(clip).Where(b=>b.type==typeof(Transform)).ToArray();sb.AppendLine(" curves="+bindings.Length+" unresolved="+bindings.Count(b=>b.path!=""&&a.transform.Find(b.path)==null));}
   }
   return sb.ToString();
  } finally {UnityEngine.Object.DestroyImmediate(root);}
 }
}
