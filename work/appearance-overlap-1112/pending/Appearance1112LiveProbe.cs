using System;using System.Linq;using System.IO;using UnityEngine;using AntColony.Units;
public static class Appearance1112LiveProbe {
 public static string Run(){var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();string report="motion="+p.PlayingMotion+"\n";foreach(var r in p.GetComponentsInChildren<Renderer>(true))if(r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff)report+=r.name+" type="+r.GetType().Name+" parent="+r.transform.parent.name+" material="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null"))+"\n";File.WriteAllText("work/appearance-overlap-1112/live-renderers.txt",report);return report;}
}
