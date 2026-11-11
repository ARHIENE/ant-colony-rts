using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
public static class Meshy1111PlaybackAudit
{
 public static async Task<string> Run()
 {
  if(!Application.isPlaying)throw new Exception("Isolated Play required");
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();if(!p)throw new Exception("Native player required");
  var library=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");var runtime=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");
  var oldPreview=p.PreviewMotion;float oldPhase=p.PreviewPhase;var report=new List<string>();
  AntColony.UI.SimulationPause.UserSpeed(0);EditorApplication.isPaused=false;
  try
  {
   p.Configure(library);
   foreach(var entry in library.motions)
   {
    p.PreviewMotion=entry.id;p.PreviewPhase=.35f;
    int frame=Time.frameCount;var end=DateTime.UtcNow.AddSeconds(3);
    while((p.PlayingMotion!=entry.id||Time.frameCount<frame+2)&&DateTime.UtcNow<end)await Task.Delay(20);
    if(p.PlayingMotion!=entry.id)throw new Exception("Playback selection failed: "+entry.id);
    var model=p.NativeModel;var hips=model.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:Hips");
    var skin=model.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>Array.IndexOf(s.bones,hips)>=0);
    int index=Array.IndexOf(skin.bones,hips);
    var expected=hips.parent.InverseTransformPoint((skin.transform.localToWorldMatrix*skin.sharedMesh.bindposes[index].inverse).MultiplyPoint3x4(Vector3.zero));
    float error=Vector2.Distance(new Vector2(hips.localPosition.x,hips.localPosition.z),new Vector2(expected.x,expected.z));
    if(error>.001f||float.IsNaN(error))throw new Exception("Horizontal root drift "+entry.id+" "+error);
    if(!model.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&!r.forceRenderingOff))throw new Exception("Invisible native model "+entry.id);
    report.Add(entry.id+" PASS live playable/root, rig="+entry.rig+", rootError="+error.ToString("F6"));
   }
   File.WriteAllLines("work/animation-1111/live-playback-audit.txt",report);
   return report.Count+" native clips selected by the live player, visible and horizontally anchored; source pose aesthetics still require visual review";
  }
  finally{p.Configure(runtime);p.PreviewMotion=oldPreview;p.PreviewPhase=oldPhase;}
 }
}
