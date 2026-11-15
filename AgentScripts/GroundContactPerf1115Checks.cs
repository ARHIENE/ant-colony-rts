using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using AntColony.World;
public static class GroundContactPerf1115Checks
{
 public static string Main(){
  if(!Application.isPlaying || Time.timeScale!=0)throw new Exception("Paused Play required");
  var report=new StringBuilder();int count=0;float maxError=0;
  foreach(OrganicEnemyKind kind in Enum.GetValues(typeof(OrganicEnemyKind))){
   if(kind==OrganicEnemyKind.None)continue;
   var root=OrganicEnemyVisual.Create(kind);var a=root.GetComponentInChildren<Animator>();
   var s=root.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=s.sharedMesh;
   var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;var bones=s.bones;
   var contact=new OrganicGroundContact(root);try{
    foreach(string state in new[]{"Idle","Move","Attack","Hit","Death"})
    foreach(float phase in new[]{0f,.23f,.61f,.97f}){
     root.transform.position=new Vector3(211,2.3f,197);root.transform.rotation=Quaternion.Euler(0,73,0);
     a.Play("Base Layer."+state,0,phase);a.Update(0);
     var matrices=new Matrix4x4[bones.Length];for(int b=0;b<bones.Length;b++)matrices[b]=bones[b].localToWorldMatrix*bind[b];
     float lowest=float.PositiveInfinity;
     for(int i=0;i<vertices.Length;i++){
      var v=vertices[i];var w=weights[i];
      var p=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
      lowest=Mathf.Min(lowest,p.y);
     }
     var anchor=root.transform.position;
     float ground=NavMesh.SamplePosition(anchor,out var hit,.5f,NavMesh.AllAreas)?hit.position.y:anchor.y;
     contact.Apply(root,anchor);
     float error=Mathf.Abs(contact.LastCorrection-(ground-lowest));maxError=Mathf.Max(maxError,error);
     if(error>.003f)throw new Exception(kind+" "+state+" "+phase+" error="+error);
     count++;
    }
    report.AppendLine("PASS "+kind+" 20 poses");
   }finally{contact.Dispose();contact.Dispose();UnityEngine.Object.DestroyImmediate(root);}
  }
  report.AppendLine("PASS "+count+" poses; max difference="+maxError+" metres");
  File.WriteAllText("work/colony-polish-1115/ground-contact-checks.txt",report.ToString());return report.ToString();
 }
}
