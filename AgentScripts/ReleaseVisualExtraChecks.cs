using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using AntColony.UI;
using AntColony.Camera;
public static class ReleaseVisualExtraChecks
{
 public static async Task<string> Run()
 {
  var cam=Camera.main; var control=cam.GetComponent<IsometricCameraController>();
  var focus=control.FocusPoint;var yaw=control.Yaw;var size=control.ViewSize;
  var restore=typeof(IsometricCameraController).GetMethod("RestoreView",BindingFlags.Instance|BindingFlags.NonPublic);
  try {
   restore.Invoke(control,new object[]{focus,yaw,9f}); await Task.Delay(350);
   ScreenCapture.CaptureScreenshot("Temp/release-visual-pass/commander-close.png");await Task.Delay(350);
   restore.Invoke(control,new object[]{focus+new Vector3(18,0,18),yaw,size});await Task.Delay(350);
   ScreenCapture.CaptureScreenshot("Temp/release-visual-pass/landscape-edge.png");await Task.Delay(350);
   return "close and edge captures complete; restored original camera";
  } finally {restore.Invoke(control,new object[]{focus,yaw,size});}
 }
 public static string InspectMissing()
 {
  if(Application.isPlaying)throw new Exception("Edit only");
  var all=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
  var missing=all.Where(t=>PrefabUtility.GetPrefabInstanceStatus(t.gameObject)==PrefabInstanceStatus.MissingAsset).ToArray();
  return string.Join("\n",missing.GroupBy(t=>t.name).Select(g=>g.Key+" count="+g.Count()+" parent="+g.First().parent?.name+" components="+string.Join(",",g.First().GetComponents<Component>().Select(c=>c==null?"MISSING":c.GetType().Name))));
 }
}
