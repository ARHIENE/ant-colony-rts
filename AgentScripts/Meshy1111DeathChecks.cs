using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Units;
public static class Meshy1111DeathChecks
{
 public static async Task<string> Run()
 {
  if(!Application.isPlaying)throw new Exception("Play required");
  var library=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");var motion=library.motions.First(m=>m.id=="Dead");
  var body=MeshyNativeCorpse.Create(library,motion,new Vector3(0,2,0),Quaternion.identity,true);
  try
  {
   var arm=body.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:RightArm");var before=arm.localRotation;
   await Task.Delay(800);float moved=Quaternion.Angle(before,arm.localRotation);
   if(moved<.1f)throw new Exception("Death joint did not advance: "+moved);
   await Task.Delay((int)(motion.clip.length*1000)+100);
   var last=arm.localRotation;await Task.Delay(300);float drift=Quaternion.Angle(last,arm.localRotation);
   if(drift>.1f)throw new Exception("Corpse loops after death: "+drift);
   if(!body.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&!r.forceRenderingOff))throw new Exception("Corpse invisible");
   var component=body.GetComponent<MeshyNativeCorpse>();component.enabled=false;component.enabled=true;await Task.Delay(100);
   if(Quaternion.Angle(last,arm.localRotation)>.1f)throw new Exception("Re-enable resets corpse");
   return "PASS native death: joint movement="+moved+"deg; end hold drift="+drift+"deg; visible and stable after re-enable";
  }finally{UnityEngine.Object.Destroy(body);}
 }
}
