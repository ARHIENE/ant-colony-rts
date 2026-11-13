using AntColony.Core;using System;using System.IO;using System.Linq;using System.Threading.Tasks;using UnityEditor;using UnityEngine;using AntColony.Units;using AntColony.Buildings;using AntColony.UI;
public static class Save1114Media {
 public static async Task<string> Dining(){
 var table=UnityEngine.Object.FindObjectsByType<Kitchen>(FindObjectsInactive.Exclude).Single(k=>k.IsTable);
 var ctl=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();if(ctl)ctl.enabled=false;
 var cam=Camera.main;cam.orthographicSize=3.4f;cam.transform.position=table.Position+new Vector3(3,2.6f,-4);cam.transform.LookAt(table.Position+Vector3.up*.3f);
 return await Capture("dining");}
 public static Task<string> Shelf()=>Capture("bookshelf");
 public static Task<string> Bench()=>Capture("workbench");
 static async Task<string> Capture(string name){
 var dir=Path.GetFullPath("work/save-20261114/media/"+name);Directory.CreateDirectory(dir);EditorApplication.isPaused=false;SimulationPause.UserSpeed(1);
 try{for(int i=0;i<40;i++){await Task.Delay(120);ScreenCapture.CaptureScreenshot(Path.Combine(dir,i.ToString("D3")+".png"));}await Task.Delay(200);}
 finally{SimulationPause.UserSpeed(0);EditorApplication.isPaused=true;}
 var states=CommanderRoster.Instance.Commanders.Where(c=>c&&c.IsColonyMember).Select(c=>c.GetComponent<AntVisual>().MotionName+"/"+c.GetComponent<MeshyNativeMotionPlayer>().PlayingMotion);
 var result=name+": 40 actual Game frames; "+string.Join(",",states);File.WriteAllText(dir+".txt",result);return result;}
}
