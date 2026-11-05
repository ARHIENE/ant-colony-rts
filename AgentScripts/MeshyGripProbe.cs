using System.Linq;
using AntColony.Units;
using UnityEngine;
public static class MeshyGripProbe {
 public static string Main(){var root=new GameObject("Grip probe");try{
 var model=KemonoModel.Create(root.transform,1105);var anim=RigAnimator.Find(model);anim.Update(.01f);anim.enabled=false;
 KemonoModel.Hold(model,"Weapon_SingleRifle");model.GetComponent<MeshyEquipmentGrip>().Pose(true);
 var body=model.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name.StartsWith("Part_Body"));var baked=new Mesh();body.BakeMesh(baked);var vs=baked.vertices;var ws=body.sharedMesh.boneWeights;
 string result="";foreach(var side in new[]{"L","R"}){var socket=model.GetComponentsInChildren<Transform>().First(t=>t.name=="Socket.Tool."+side);Vector3 sum=Vector3.zero;int n=0;
 for(int i=0;i<ws.Length;i++)if(body.bones[ws[i].boneIndex0].name=="hand."+side&&ws[i].weight0>.5f){sum+=body.transform.TransformPoint(vs[i]);n++;}
 result+=side+" palm="+(sum/Mathf.Max(1,n)).ToString("F4")+" socket="+socket.position.ToString("F4")+" deltaLocal="+socket.InverseTransformVector(sum/Mathf.Max(1,n)-socket.position).ToString("F4")+" vertices="+n+"; ";}
 Object.DestroyImmediate(baked);return result;
 }finally{Object.DestroyImmediate(root);}}
}
