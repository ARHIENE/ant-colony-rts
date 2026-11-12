using System;using System.IO;using System.Linq;using System.Threading.Tasks;using UnityEngine;using AntColony.Core;using AntColony.Units;
public static class Appearance1112GpuProbe{
 public static async Task<string> Run(){var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember);var p=c.GetComponent<MeshyNativeMotionPlayer>();p.PreviewMotion="Idle_3";p.PreviewPhase=.3f;await Task.Delay(100);string report="";
 var skins=p.NativeModel.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
 foreach(var s in skins){report+=s.name+" quality="+s.quality+" shadow="+s.sharedMaterial.GetFloat("_ReceiveShadows")+"\n";s.quality=SkinQuality.Bone4;s.skinnedMotionVectors=false;}
 await Task.Delay(100);ScreenCapture.CaptureScreenshot("work/appearance-overlap-1112/gpu-bone4.png");await Task.Delay(150);
 var spawned=new System.Collections.Generic.List<GameObject>();
 try{foreach(var s in skins){var mesh=new Mesh();s.BakeMesh(mesh);var go=new GameObject("Diagnostic baked "+s.name);go.transform.SetParent(s.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=s.sharedMaterials;var block=new MaterialPropertyBlock();s.GetPropertyBlock(block,0);r.SetPropertyBlock(block,0);s.forceRenderingOff=true;spawned.Add(go);}
 await Task.Delay(100);ScreenCapture.CaptureScreenshot("work/appearance-overlap-1112/cpu-baked.png");await Task.Delay(150);
 }finally{foreach(var go in spawned){UnityEngine.Object.Destroy(go.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.Destroy(go);}foreach(var s in skins)s.forceRenderingOff=false;}
 File.WriteAllText("work/appearance-overlap-1112/gpu-probe.txt",report);return report;
 }
}
