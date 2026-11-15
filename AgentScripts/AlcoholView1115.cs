using System;using System.Linq;using System.Reflection;using System.Threading.Tasks;
using AntColony.Core;using AntColony.Data;using AntColony.Units;using AntColony.UI;using UnityEngine;
public static class AlcoholView1115
{
 public static async Task<string> Main()
 {
  var c=CommanderRoster.Instance.Commanders.First();c.CommandStop();c.WorkState.duty=CommanderDuty.Civilian;c.Alcohol.allowed=true;c.Alcohol.intoxication=c.Alcohol.hangover=0;c.Alcohol.sinceDrink=450;
  ResourceManager.Instance.Add(ResourceType.Beer,1);if(!c.RequestDrink())return "Drink blocked: "+c.DrinkBlock();
  SimulationPause.UserSpeed(1);var until=DateTime.UtcNow.AddSeconds(30);while(!c.IsDrinking&&DateTime.UtcNow<until)await Task.Delay(25);SimulationPause.UserSpeed(0);
  var camera=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(camera,new object[]{c.Position,0f,5f});
  return c.AlcoholStatus;
 }
 public static string Menu(){GameMenuController.Instance.ShowAlcohol();return "Alcohol policy menu open";}
}
