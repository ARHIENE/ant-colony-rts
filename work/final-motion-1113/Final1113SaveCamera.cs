using System.Reflection;using UnityEngine;using AntColony.UI;
public static class Final1113SaveCamera {
 public static string Run(){SimulationPause.UserSpeed(0);var c=Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();c.enabled=true;typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{Camera.main.transform.position,45f,18f});return "Normal camera size restored for save regression";}
}
