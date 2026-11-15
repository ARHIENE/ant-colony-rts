using System;using System.Linq;using System.Reflection;using AntColony.Core;using AntColony.Buildings;using AntColony.Data;using AntColony.UI;using UnityEngine;
public static class IndustryView1115
{
 public static string Main()
 {
  AntColony.UI.GameMenuController.Instance.Resume();SimulationPause.UserSpeed(0);
  var p=SceneObjects.All<Processor>().Single(p=>p.Data.kind==BuildingKind.Quarry);
  var camera=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
  typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(camera,new object[]{p.Position+Vector3.right*3,0f,8f});
  return string.Join("\n",SceneObjects.All<Processor>().Where(p=>p.IsResourceSite).Select(p=>p.Data.kind+" model="+(p.GetComponent<FacilityVisual>()?.Model!=null)));
 }
}
