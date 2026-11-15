using System.Linq;using System.Reflection;using AntColony.Core;using UnityEngine;
public static class OrdinaryFightView1115
{
 public static string Main()
 {
  var r=RocketCampaign.Instance;var c=r.MothershipCrew.First();var camera=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
  typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(camera,new object[]{(c.Position+r.MothershipBoss.Position)*.5f,0f,12f});
  return "Boss "+r.MothershipBoss.CurrentHp+" crew "+c.PersonalHealth;
 }
}
