using System;using System.Linq;using System.IO;using System.Threading.Tasks;using UnityEngine;using UnityEditor;using AntColony.Units;using AntColony.Core;
public static class Appearance1112Views {
 public static async Task<string> Run(){
  if(!Application.isPlaying)throw new Exception("Isolated Play required");
  var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember);c.CommandStop();
  var p=c.GetComponent<MeshyNativeMotionPlayer>();var original=c.transform.Find("Kemono");var a=original.GetComponent<KemonoAppearance>();
  var oldHair=original.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.enabled&&s.name.StartsWith("Part_Hair_")).name.Split('_')[2];
  Directory.CreateDirectory("work/appearance-overlap-1112/unity-fixed");
  EditorApplication.isPaused=false;AntColony.UI.SimulationPause.UserSpeed(0);p.PreviewMotion="Idle_3";p.PreviewPhase=.3f;
  var controller=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();if(controller)controller.enabled=false;
  EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
  try{
   foreach(var hair in a.GetVariants("Hair")){
    a.SetVariant("Hair",hair);a.SetPaletteColor(new Color(.65f,.31f,.36f));await Task.Delay(100);
    var head=p.NativeModel.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:Head");
    var focus=head.position+Vector3.up*.1f;var cam=Camera.main;cam.orthographicSize=.7f;cam.nearClipPlane=.01f;
    cam.transform.position=focus+p.transform.forward*3+Vector3.up*.15f;cam.transform.LookAt(focus);
    await Task.Delay(120);ScreenCapture.CaptureScreenshot("work/appearance-overlap-1112/unity-fixed/hair-"+hair+".png");await Task.Delay(120);
   }
   var face=original.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.name.StartsWith("Part_Face_"));
   for(int sh=0;sh<face.sharedMesh.blendShapeCount;sh++){
    for(int j=0;j<face.sharedMesh.blendShapeCount;j++)face.SetBlendShapeWeight(j,j==sh?100:0);
    p.NativeModel.GetComponent<MeshyNativeAppearance>().Sync();await Task.Delay(100);
    ScreenCapture.CaptureScreenshot("work/appearance-overlap-1112/unity-fixed/expression-"+sh+".png");await Task.Delay(100);
   }
   return "Actual commander captures: six hair variants and eight facial shape channels; inspect each image";
  }finally{a.SetVariant("Hair",oldHair);p.PreviewMotion=null;p.PreviewPhase=-1;}
 }
}
