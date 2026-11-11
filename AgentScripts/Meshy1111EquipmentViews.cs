using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
public static class Meshy1111EquipmentViews
{
 public static async Task<string> Hands(string label="hands")
 {
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();var original=p.transform.Find("Kemono");p.PreviewMotion=p.PlayingMotion;p.PreviewPhase=.25f;await Task.Delay(120);
  var bones=p.NativeModel.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);var focus=(bones["mixamorig:LeftHand"].position+bones["mixamorig:RightHand"].position)*.5f;
  Camera.main.transform.position=focus-original.forward*.4f-original.right*.8f+Vector3.up*.4f;Camera.main.transform.LookAt(focus);Camera.main.orthographicSize=.35f;
  Render(Camera.main,Path.GetFullPath("work/animation-1111/views/"+label+".png"));return label;
 }
 public static async Task<string> Front(string label="front")
 {
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();var original=p.transform.Find("Kemono");var focus=p.transform.position+original.localPosition+Vector3.up*.85f;
  Camera.main.transform.position=focus+original.forward*3+original.right*1.2f+Vector3.up*.3f;Camera.main.transform.LookAt(focus);Camera.main.orthographicSize=1.1f;
  p.PreviewPhase=.05f;await Task.Delay(120);Render(Camera.main,Path.GetFullPath("work/animation-1111/views/"+label+".png"));return label;
 }
 public static async Task<string> Live()
 {
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();
  p.PreviewMotion=p.PlayingMotion;p.PreviewPhase=-1;
  AntColony.UI.SimulationPause.UserSpeed(0);EditorApplication.isPaused=false;
  int start=Time.frameCount;var arm=p.NativeModel.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:RightArm");var rotation=arm.localRotation;
  await Task.Delay(500);
  return $"Live isolated preview {p.PlayingMotion}: frames={Time.frameCount-start}, arm rotation delta={Quaternion.Angle(rotation,arm.localRotation)}";
 }
 public static async Task<string> Capture(string label="equipment",bool compareFlip=false)
 {
  var player=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();
  if(!player||!player.NativeModel)throw new Exception("Active native player required");
  var original=player.transform.Find("Kemono");var cam=Camera.main;
  var controller=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();if(controller)controller.enabled=false;
  var focus=player.transform.position+original.localPosition+Vector3.up*.8f;
  cam.orthographicSize=1.3f;cam.nearClipPlane=.02f;
  cam.transform.position=focus-original.right*3-original.forward*.5f+Vector3.up*.4f;cam.transform.LookAt(focus);
  AntColony.UI.SimulationPause.UserSpeed(0);EditorApplication.isPaused=false;
  var path=Path.GetFullPath("work/animation-1111/views");Directory.CreateDirectory(path);
  string old=player.PreviewMotion;float phase=player.PreviewPhase;player.PreviewMotion=player.PlayingMotion;
  try
  {
   for(int i=0;i<5;i++)
   {
    player.PreviewPhase=.05f+.2f*i;await Task.Delay(120);
    Render(cam,Path.Combine(path,label+"-"+i+".png"));
    if(compareFlip)
    {
     var grip=player.GetComponentsInChildren<Transform>().First(t=>t.name=="Native equipment grip");
     var rotation=grip.rotation;grip.rotation*=Quaternion.AngleAxis(180,Vector3.forward);
     Render(cam,Path.Combine(path,label+"-flip-"+i+".png"));grip.rotation=rotation;
    }
   }
  }finally{player.PreviewMotion=old;player.PreviewPhase=phase;}
  return "Captured five phases of "+label+" at "+path;
 }
 static void Render(Camera camera,string path)
 {
  var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1000,800,24);var tex=new Texture2D(1000,800,TextureFormat.RGB24,false);
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1000,800),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
  finally{camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
}
