using System;using System.Linq;using System.IO;using System.Threading.Tasks;using UnityEngine;using UnityEditor;using AntColony.Units;using AntColony.Core;
public static class Appearance1112LegacyView {
 public static async Task<string> Run(){
  var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember);var p=c.GetComponent<MeshyNativeMotionPlayer>();var original=c.transform.Find("Kemono");var a=original.GetComponent<KemonoAppearance>();
  p.enabled=false;AntColony.UI.SimulationPause.UserSpeed(0);EditorApplication.isPaused=false;
  try{
   a.SetVariant("Hair","BOB");a.SetPaletteColor(new Color(.65f,.31f,.36f));await Task.Delay(120);
   var face=original.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.enabled&&s.name.StartsWith("Part_Face_"));
   var cam=Camera.main;cam.orthographicSize=.7f;cam.nearClipPlane=.01f;var focus=face.bounds.center;
   cam.transform.position=focus+original.forward*3+Vector3.up*.15f;cam.transform.LookAt(focus);
   await Task.Delay(120);Directory.CreateDirectory("work/appearance-overlap-1112/unity");ScreenCapture.CaptureScreenshot("work/appearance-overlap-1112/unity/legacy-face.png");await Task.Delay(150);
   return "Legacy fallback actual renderer captured; one enabled face and "+original.GetComponentsInChildren<SkinnedMeshRenderer>().Count(s=>s.enabled&&s.name.StartsWith("Part_Hair_"))+" hair";
  }finally{p.enabled=true;}
 }
}
