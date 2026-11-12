using System;using System.Linq;using System.IO;using UnityEngine;using UnityEngine.Animations;using UnityEngine.Playables;using AntColony.Units;
public static class Appearance1112OverlapProbe {
 public static string Run(){
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");string report="";
  foreach(var id in new[]{"Idle_3","Heavy_Hammer_Swing"}){
   var m=lib.motions.First(x=>x.id==id);var host=new GameObject("Temporary appearance probe");var model=UnityEngine.Object.Instantiate(lib.characters[m.rig],host.transform,false);var original=KemonoModel.Create(host.transform,42);
   var graph=PlayableGraph.Create("Temporary appearance probe");
   try{
    foreach(var a in model.GetComponentsInChildren<Animator>())a.enabled=false;
    var root=m.armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model;
    var anim=root.GetComponent<Animator>();if(!anim)anim=root.AddComponent<Animator>();anim.enabled=true;anim.runtimeAnimatorController=null;
    var body=model.GetComponentsInChildren<SkinnedMeshRenderer>().Single();
    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var clip=AnimationClipPlayable.Create(graph,m.clip);AnimationPlayableOutput.Create(graph,"probe",anim).SetSourcePlayable(clip);graph.Play();
    var appearance=MeshyNativeAppearance.Attach(model.transform,original.transform,m.rig);
    report+=id+" original enabled after attach="+body.enabled;
    clip.SetTime(m.clip.length*.3f);graph.Evaluate(0);appearance.Sync();
    report+=" after graph="+body.enabled+" visible skins="+string.Join(",",model.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).Select(s=>s.name))+"\n";
   }finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(host);}
  }File.WriteAllText("work/appearance-overlap-1112/overlap-probe.txt",report);return report;
 }
}
