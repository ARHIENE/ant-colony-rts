using System;
using System.Linq;
using UnityEngine;
using AntColony.Units;
public static class HandGrip1106Invariant
{
 public static string Main(){var root=new GameObject("Hand invariance");try{
 var m=KemonoModel.Create(root.transform,1105);var anim=RigAnimator.Find(m);anim.enabled=false;var grip=m.GetComponent<MeshyEquipmentGrip>();KemonoModel.Hold(m,"Tool_Shovel_Stone");
 var body=m.GetComponentsInChildren<SkinnedMeshRenderer>().First(x=>x.name.StartsWith("Part_Body"));var mesh=body.sharedMesh;var vs=mesh.vertices;var ws=mesh.boneWeights;var bp=mesh.bindposes;
 Func<int,Vector3> skin=i=>{var w=ws[i];return (body.bones[w.boneIndex0].localToWorldMatrix*bp[w.boneIndex0]).MultiplyPoint3x4(vs[i])*w.weight0+(body.bones[w.boneIndex1].localToWorldMatrix*bp[w.boneIndex1]).MultiplyPoint3x4(vs[i])*w.weight1+(body.bones[w.boneIndex2].localToWorldMatrix*bp[w.boneIndex2]).MultiplyPoint3x4(vs[i])*w.weight2+(body.bones[w.boneIndex3].localToWorldMatrix*bp[w.boneIndex3]).MultiplyPoint3x4(vs[i])*w.weight3;};
 var ts=m.GetComponentsInChildren<Transform>();string result="";
 foreach(var side in new[]{"L","R"}){
 var socket=ts.First(t=>t.name=="Socket.Tool."+side);Func<int,bool> isHand=j=>body.bones[j].name.EndsWith("."+side)&&(body.bones[j].name.StartsWith("hand.")||body.bones[j].name.StartsWith("f_")||body.bones[j].name.StartsWith("thumb."));
 var ids=Enumerable.Range(0,vs.Length).Where(i=>{var w=ws[i];return (isHand(w.boneIndex0)?w.weight0:0)+(isHand(w.boneIndex1)?w.weight1:0)+(isHand(w.boneIndex2)?w.weight2:0)+(isHand(w.boneIndex3)?w.weight3:0)>.999f;}).ToArray();
 anim.Play("Base Layer.Idle_A",0,.2f);anim.Update(0);grip.Pose(true);var before=ids.ToDictionary(i=>i,i=>socket.InverseTransformPoint(skin(i)));
 anim.Play("Base Layer.Tool_Chop",0,.5f);anim.Update(0);grip.Pose(false);
 var diffs=ids.Select(i=>(i,delta:(socket.InverseTransformPoint(skin(i))-before[i]).magnitude)).OrderByDescending(v=>v.delta).Take(5);
 result+=side+" pure hand "+ids.Length+" drift "+string.Join(", ",diffs.Select(d=>d.i+":"+d.delta.ToString("F5")))+"\n";
 foreach(var n in new[]{"hand.","f_middle.01.","f_middle.03."}){var b=ts.First(t=>t.name==n+side);result+=b.name+" "+socket.InverseTransformPoint(b.position).ToString("F4")+"\n";}
 }
 return result;
 }finally{UnityEngine.Object.DestroyImmediate(root);}}
}
