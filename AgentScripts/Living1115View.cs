using System;using System.IO;using System.Linq;using System.Reflection;using AntColony.Core;using AntColony.Buildings;using AntColony.Data;using AntColony.Units;using UnityEngine;
public static class Living1115View {
 public static string Main(){var bed=SceneObjects.All<Dormitory>().First(b=>b.isActiveAndEnabled&&b.IsMat);var cam=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
 typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(cam,new object[]{bed.Position,0f,8f});
 var text="";foreach(var b in SceneObjects.All<Dormitory>().Where(b=>b.isActiveAndEnabled&&b.IsMat)){
 text+="MAT size="+b.transform.lossyScale+" pos="+b.Position+"\n";foreach(var r in b.GetComponentsInChildren<Renderer>())text+=r.name+" "+r.bounds+"\n";
 foreach(var c in b.Residents){var pose=c.TrySleepingPose(out var support,out var head);text+=c.CommanderName+" sleeping="+c.IsAsleep+" pose="+pose+" support="+support+" head="+head+"\n";}}
 File.WriteAllText("work/natural-campaign-1115/living-view.txt",text);ScreenCapture.CaptureScreenshot("work/natural-campaign-1115/screens/meshy-mat-close.png");return text;}
}

