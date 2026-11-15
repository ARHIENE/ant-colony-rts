using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;using AntColony.Core;using AntColony.World;using AntColony.Save;using AntColony.UI;using UnityEngine;
public static class Handcart1115ReloadView
{
 const string Log="work/colony-polish-1115/handcart-view.txt";
 public static string Main(){File.WriteAllText(Log,"Reload latest isolated outbound cart for visual review\n");_=Run();return "Cart visual review started";}
 static async Task Run(){var root=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();
 try{
  var ready=DateTime.UtcNow.AddSeconds(120);while(SaveSystem.Busy&&DateTime.UtcNow<ready)await Task.Delay(100);
  var quiet=settings.Clone();quiet.autoSaveEnabled=false;UserSettings.Apply(quiet,false);SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"CartView1115");
  var source=Directory.GetDirectories(Application.temporaryCachePath,"Handcart1115-*").SelectMany(d=>Directory.GetFiles(d,"*.json",SearchOption.AllDirectories)).OrderByDescending(File.GetLastWriteTimeUtc).First(p=>{try{return File.ReadAllText(p).Contains("\"handcart\":true")||File.ReadAllText(p).Contains("\"handcart\": true");}catch{return false;}});
  if(!SaveSystem.TryLoad(source,out var error))throw new Exception(error);var until=DateTime.UtcNow.AddSeconds(120);while(SaveSystem.Busy&&DateTime.UtcNow<until)await Task.Delay(100);if(SaveSystem.Busy)throw new Exception("Load timeout");
  GameMenuController.Instance.Resume();SimulationPause.UserSpeed(0);var cart=WorldMapManager.Instance.Transports.First(t=>t.Handcart);
  var cam=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(cam,new object[]{cart.Position,0f,7f});await Task.Delay(300);
  foreach(var r in cart.GetComponentsInChildren<Renderer>()){var p=UnityEngine.Camera.main.WorldToViewportPoint(r.bounds.center);File.AppendAllText(Log,r.name+" visible="+r.enabled+" forcedOff="+r.forceRenderingOff+" bounds="+r.bounds+" viewport="+p+"\n");}
  ScreenCapture.CaptureScreenshot("Assets/work/colony-polish-1115/handcart-edge-fixed.png");await Task.Delay(500);File.AppendAllText(Log,"CAPTURED\n");
 }catch(Exception e){File.AppendAllText(Log,"ERROR "+e+"\n");}finally{UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;File.AppendAllText(Log,"RESTORED\n");}}
}
