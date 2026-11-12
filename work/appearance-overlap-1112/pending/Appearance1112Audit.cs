using System;using System.Linq;using System.IO;using System.Threading.Tasks;using UnityEngine;using AntColony.Units;using AntColony.Core;
public static class Appearance1112Audit {
 static void Check(bool value,string text){if(!value)throw new Exception(text);}
 public static async Task<string> Run(){
  Check(Application.isPlaying,"Isolated Play required");
  var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember);c.CommandStop();c.WorkState.jobs=CommanderJobs.None;
  var p=c.GetComponent<MeshyNativeMotionPlayer>();var original=c.transform.Find("Kemono");var a=original.GetComponent<KemonoAppearance>();
  var oldEquipment=c.PersonalState.equipment.ToArray();string oldHair=original.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.enabled&&s.name.StartsWith("Part_Hair_")).name.Split('_')[2];
  int combinations=0,swaps=0;var report="";
  try{
   AntColony.UI.SimulationPause.UserSpeed(0);UnityEditor.EditorApplication.isPaused=false;
   foreach(var motion in new[]{"Idle_3","Heavy_Hammer_Swing"})foreach(var hair in a.GetVariants("Hair")){
    a.SetVariant("Hair",hair);p.PreviewMotion=motion;p.PreviewPhase=.3f;await Task.Delay(100);
    var skins=p.NativeModel.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled&&!s.forceRenderingOff).ToArray();
    foreach(var skin in skins){var mesh=skin.sharedMesh;Check(mesh.triangles.All(i=>i>=0&&i<mesh.vertexCount),skin.name+" invalid triangle indices");Check(mesh.boneWeights.Length==mesh.vertexCount&&mesh.uv.Length==mesh.vertexCount&&mesh.normals.Length==mesh.vertexCount,skin.name+" vertex stream mismatch");}
    Check(skins.Count(s=>s.name.StartsWith("Part_Hair_"))==1,"Multiple visible hairs");
    var face=skins.Single(s=>s.name.StartsWith("Part_Face_"));Check(face.sharedMesh.triangles.Length/3==4741,"Face partition not rebuilt");
    Check(skins.Single(s=>s.name.StartsWith("Part_Hair_")).sharedMesh.blendShapeCount==0,"Hair has facial expressions");
    Check(original.GetComponentsInChildren<Renderer>(true).All(r=>r.forceRenderingOff),"Legacy renderer visible during native playback");
    foreach(var expression in new[]{"NEUTRAL","SMILE","PAIN","SLEEP"}){a.SetExpression(expression);await Task.Delay(40);Check(p.NativeModel.GetComponentsInChildren<SkinnedMeshRenderer>().Count(s=>s.enabled&&s.name.StartsWith("Part_Face_"))==1,"Duplicate expression faces");}
    combinations++;
   }
   // A fixed preview keeps the native model active while only equipment changes.
   p.PreviewMotion="Idle_3";p.PreviewPhase=.3f;
   foreach(var effect in new[]{TrinketEffect.Gather,TrinketEffect.Mood,TrinketEffect.Move}){
    int motionRevision=c.GetComponent<AntVisual>().MotionRevision; c.PersonalState.equipment.RemoveAll(e=>e.slot==EquipmentSlot.Trinket);
    c.PersonalState.equipment.Add(new EquipmentItem{slot=EquipmentSlot.Trinket,effect=effect});c.RefreshEquipment();await Task.Delay(150);
    Check(c.GetComponent<AntVisual>().MotionRevision==motionRevision,"Fixture changed motion during accessory-only swap"); Check(original.GetComponentsInChildren<Renderer>(true).All(r=>r.forceRenderingOff),"New legacy equipment escaped hiding: "+effect);swaps++;
   }
   report=combinations+" rig/hair combinations: one hair/face, no facial morphs on hair; "+swaps+" accessory swaps kept legacy hidden";
   File.WriteAllText("work/appearance-overlap-1112/runtime-audit.txt",report);return report;
  }finally{c.PersonalState.equipment.Clear();c.PersonalState.equipment.AddRange(oldEquipment);c.RefreshEquipment();a.SetVariant("Hair",oldHair);a.SetExpression("NEUTRAL");p.PreviewMotion=null;p.PreviewPhase=-1;}
 }
}
