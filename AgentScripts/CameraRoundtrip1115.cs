using System;using System.IO;using System.Reflection;using System.Threading.Tasks;using AntColony.Core;using AntColony.Save;using AntColony.UI;
public static class CameraRoundtrip1115 {
 const string Log="work/colony-polish-1115/camera-roundtrip.txt";
 public static string Main(){if(string.IsNullOrEmpty(SaveStorage.RootOverride)||SaveSystem.Busy)throw new Exception("Ready isolated session required");File.WriteAllText(Log,"Started normal minimum-zoom save/load\n");_=Run();return "Camera roundtrip started";}
 static async Task Run(){try{SimulationPause.UserSpeed(0);var cam=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(cam,new object[]{cam.FocusPoint,cam.Yaw,5f});
 if(!SaveSystem.TrySave(false,2,out var error))throw new Exception(error);if(!SaveSystem.TryLoad(SaveSlots.PathFor(false,2),out error))throw new Exception(error);
 var end=DateTime.UtcNow.AddSeconds(120);while(SaveSystem.Busy&&DateTime.UtcNow<end)await Task.Delay(100);if(SaveSystem.Busy)throw new Exception("Load timeout");GameMenuController.Instance.Resume();SimulationPause.UserSpeed(0);
 cam=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();if(Math.Abs(cam.ViewSize-5f)>.001f)throw new Exception("Zoom not restored: "+cam.ViewSize);
 File.AppendAllText(Log,"PASS minimum zoom5 saved and fully reloaded; progress="+GameCalendar.GameSeconds+"\n");
 }catch(Exception e){File.AppendAllText(Log,"ERROR "+e+"\n");}}
}
