using System;
using System.Linq;
using System.IO;
using UnityEngine;
using AntColony.Units;
public static class MeshySkinDiagnostic {
 public static string Main(){var root=new GameObject("Skin diagnostic");try{
 var m=KemonoModel.Create(root.transform,1105);RigAnimator.Find(m).enabled=false;KemonoModel.Hold(m,"Weapon_SingleRifle");m.GetComponent<MeshyEquipmentGrip>().Pose(true);
 var s=m.GetComponentsInChildren<SkinnedMeshRenderer>().First(x=>x.name.StartsWith("Part_Body"));var mesh=s.sharedMesh;var vs=mesh.vertices;var ws=mesh.boneWeights;var bp=mesh.bindposes;
 Func<int,Vector3> skin=i=>{var w=ws[i];return (s.bones[w.boneIndex0].localToWorldMatrix*bp[w.boneIndex0]).MultiplyPoint3x4(vs[i])*w.weight0+(s.bones[w.boneIndex1].localToWorldMatrix*bp[w.boneIndex1]).MultiplyPoint3x4(vs[i])*w.weight1+(s.bones[w.boneIndex2].localToWorldMatrix*bp[w.boneIndex2]).MultiplyPoint3x4(vs[i])*w.weight2+(s.bones[w.boneIndex3].localToWorldMatrix*bp[w.boneIndex3]).MultiplyPoint3x4(vs[i])*w.weight3;};
 Func<int,string> desc=i=>i+" rest="+vs[i].ToString("F4")+" world="+skin(i).ToString("F4")+" bones="+s.bones[ws[i].boneIndex0].name+":"+ws[i].weight0+","+s.bones[ws[i].boneIndex1].name+":"+ws[i].weight1;
 var tri=mesh.triangles;var lines=new System.Collections.Generic.List<string>();
 for(int j=0;j<tri.Length;j+=3)for(int k=0;k<3;k++){int a=tri[j+k],b=tri[j+(k+1)%3];float rest=s.transform.TransformVector(vs[a]-vs[b]).magnitude*100;float actual=(skin(a)-skin(b)).magnitude;if(actual>.04f&&actual>rest*4)lines.Add((actual/Mathf.Max(rest,.00001f)).ToString("F1")+" "+desc(a)+" / "+desc(b));}
 File.WriteAllLines("art/anime-settler-1105/skin-stretch.txt",lines);
 File.WriteAllLines("art/anime-settler-1105/posed-body.obj",vs.Select((v,i)=>{var p=skin(i);return FormattableString.Invariant($"v {p.x} {p.z} {p.y}");}).Concat(Enumerable.Range(0,tri.Length/3).Select(i=>$"f {tri[i*3]+1} {tri[i*3+2]+1} {tri[i*3+1]+1}")));
 string palms="";
 foreach(var side in new[]{"L","R"}){var socket=m.GetComponentsInChildren<Transform>().First(t=>t.name=="Socket.Tool."+side);var hand=m.GetComponentsInChildren<Transform>().First(t=>t.name=="hand."+side);var ids=Enumerable.Range(0,vs.Length).Where(i=>s.bones[ws[i].boneIndex0]==hand&&ws[i].weight0>.8f).ToArray();var p=ids.Aggregate(Vector3.zero,(sum,i)=>sum+skin(i))/Mathf.Max(1,ids.Length);palms+=side+" hand="+hand.position.ToString("F4")+" socket="+socket.position.ToString("F4")+" meshPalm="+p.ToString("F4")+" count="+ids.Length+"\n";}
 return palms+"Stretched edges "+lines.Count+"\n"+string.Join("\n",lines.Take(3));
 }finally{UnityEngine.Object.DestroyImmediate(root);}}
}

