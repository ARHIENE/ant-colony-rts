using System.Linq;
using UnityEngine;
using AntColony.Units;
public static class HandGrip1106Diagnostic
{
 public static string Main(){var root=new GameObject("Hand diagnostic");try{
 var m=KemonoModel.Create(root.transform,1105);RigAnimator.Find(m).enabled=false;KemonoModel.Hold(m,"Tool_Axe_Stone");m.GetComponent<MeshyEquipmentGrip>().Pose(true);
 var body=m.GetComponentsInChildren<SkinnedMeshRenderer>().First(x=>x.name.StartsWith("Part_Body"));var mesh=body.sharedMesh;var vs=mesh.vertices;var ws=mesh.boneWeights;var bp=mesh.bindposes;
 System.Func<int,Vector3> skin=i=>{var w=ws[i];return (body.bones[w.boneIndex0].localToWorldMatrix*bp[w.boneIndex0]).MultiplyPoint3x4(vs[i])*w.weight0+(body.bones[w.boneIndex1].localToWorldMatrix*bp[w.boneIndex1]).MultiplyPoint3x4(vs[i])*w.weight1+(body.bones[w.boneIndex2].localToWorldMatrix*bp[w.boneIndex2]).MultiplyPoint3x4(vs[i])*w.weight2+(body.bones[w.boneIndex3].localToWorldMatrix*bp[w.boneIndex3]).MultiplyPoint3x4(vs[i])*w.weight3;};
 var ts=m.GetComponentsInChildren<Transform>();var socket=ts.First(t=>t.name=="Socket.Tool.R");var hand=ts.First(t=>t.name=="hand.R");string result="socket in hand "+socket.localPosition.ToString("F4")+" scale="+hand.lossyScale+"\n";
 foreach(var n in new[]{"hand.R","f_index.01.R","f_index.03.R","f_middle.01.R","f_middle.03.R","f_pinky.01.R","thumb.01.R"}){
 var b=ts.First(t=>t.name==n);var ids=Enumerable.Range(0,vs.Length).Where(i=>body.bones[ws[i].boneIndex0]==b).ToArray();var center=ids.Aggregate(Vector3.zero,(sum,i)=>sum+skin(i))/Mathf.Max(1,ids.Length);
 result+=n+" bone in socket="+socket.InverseTransformPoint(b.position).ToString("F4")+" skin="+socket.InverseTransformPoint(center).ToString("F4")+" n="+ids.Length+"\n";
 }
 foreach(var mf in socket.GetComponentsInChildren<MeshFilter>()){
 var ps=mf.sharedMesh.vertices.Select(v=>socket.InverseTransformPoint(mf.transform.TransformPoint(v))).ToArray();
 result+="tool "+mf.name+" range="+string.Join(" / ",Enumerable.Range(0,3).Select(k=>ps.Min(v=>v[k]).ToString("F4")+".."+ps.Max(v=>v[k]).ToString("F4")))+"\n";
 var near=ps.Where(v=>Mathf.Abs(v.y)<.035f).ToArray();if(near.Length>0)result+="handle="+near.Aggregate(Vector3.zero,(a,b)=>a+b)/near.Length+" range="+string.Join(" / ",Enumerable.Range(0,3).Select(k=>near.Min(v=>v[k]).ToString("F4")+".."+near.Max(v=>v[k]).ToString("F4")))+"\n";
 }
 return result;
 }finally{Object.DestroyImmediate(root);}}
}
