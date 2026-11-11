using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Units;
public static class Native1112AllActors
{
 public static async Task<string> Run()
 {
  if(!Application.isPlaying)throw new Exception("Play required");
  var units=UnityEngine.Object.FindObjectsByType<CommanderAnt>(FindObjectsSortMode.None).Where(c=>c.IsColonyMember).ToArray();
  await Task.Delay(750);var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");var report=new System.Collections.Generic.List<string>();
  foreach(var c in units){
   var p=c.GetComponent<MeshyNativeMotionPlayer>();if(!p)throw new Exception("No original playback "+c.name);
   foreach(var id in new[]{"Walking","Running","Heavy_Hammer_Swing","Archery_Shot_3","Sleep_Normally"}){
    p.PreviewMotion=id;p.PreviewPhase=.3f;await Task.Delay(60);
    if(p.PlayingMotion!=id)throw new Exception("Selection failure "+id);
    var skins=p.NativeModel.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
    if(!skins.Any(s=>s.name.StartsWith("Part_Hair_"))||!skins.Any(s=>s.name.StartsWith("Part_Face_")))throw new Exception("Cosmetics absent "+id);
    var baseSkin=p.NativeModel.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>!s.name.StartsWith("Part_"));
    float height=MeshyNativeBounds.Rest(new[]{baseSkin}).size.y;
    if(Mathf.Abs(height-1.6f)>.005f)throw new Exception("Body normalization "+id+"="+height);
    report.Add(c.name+" | "+id+" | native skin parts="+skins.Length+" | restHeight="+height.ToString("F4"));
   }
   p.PreviewMotion=null;p.PreviewPhase=-1;
  }
  File.WriteAllLines("work/animation-1112/all-actors.txt",report);return units.Length+" characters / "+report.Count+" source playback+cosmetics+scale checks PASS";
 }
}
