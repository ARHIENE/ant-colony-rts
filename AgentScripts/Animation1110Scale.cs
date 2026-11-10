using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
public static class Animation1110Scale {
 public static string Main(){
 var owner=new GameObject("Scale audit");var model=KemonoModel.Create(owner.transform,42);var animator=RigAnimator.Find(model);animator.enabled=false;
 var lines=new System.Collections.Generic.List<string>();
 foreach(var phase in new[]{"idle","new"}){
 if(phase=="new")Resources.Load<AnimationClip>("Kemono/MeshyMotions/Tool_Mine").SampleAnimation(animator.gameObject,.4f);
 lines.Add(phase);foreach(var t in model.GetComponentsInChildren<Transform>())if(new[]{"Upright","KemonoRig","Root","pevis","spine","head","upper_arm.R","forearm.R","hand.R"}.Contains(t.name))lines.Add(t.name+" local="+t.localPosition.ToString("F5")+" scale="+t.localScale+" worldScale="+t.lossyScale+" world="+t.position+" rot="+t.rotation.eulerAngles);
 foreach(var s in model.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled)){var mesh=new Mesh();s.BakeMesh(mesh);lines.Add(s.name+" bounds="+s.bounds+" bake="+mesh.bounds+" scale="+s.transform.lossyScale);Object.DestroyImmediate(mesh);}
 }
 Object.DestroyImmediate(owner);File.WriteAllLines("work/animation-1110/scale.txt",lines);return string.Join("\n",lines);
 }
}
