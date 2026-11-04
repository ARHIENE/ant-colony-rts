using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;
public static class SaveV5Capture
{
 public static async Task<string> Prepare()
 {
  var previous=SaveStorage.RootOverride;
  SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"SaveV5-"+Guid.NewGuid().ToString("N"));
  try {
   SaveSystem.NewGame(new NewGameOptions { seed=261012, mapSize=MapSize.Small });
   var until=DateTime.UtcNow.AddSeconds(90);
   while(SaveSystem.Busy && DateTime.UtcNow<until) await Task.Delay(100);
   if(SaveSystem.Busy) throw new Exception("Scene not ready");
   await Task.Delay(400); Time.timeScale=0;
   var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{BuildingKind.MedicineBench,UnitRole.Worker});
   var bench=Object.Instantiate(template,new Vector3(10,0,10),Quaternion.identity); bench.SetActive(true);
   var animal=Critter.Spawn(Species.Aphid,new Vector3(12,0,12),true);
   GameMenuController.Instance.OpenCritterTab(animal,true);
   Canvas.ForceUpdateCanvases(); return "medical ready; isolated save root restored";
  } finally {SaveStorage.RootOverride=previous;}
 }
 public static string Production() { GameMenuController.Instance.ShowProcessor(Object.FindAnyObjectByType<Processor>()); Canvas.ForceUpdateCanvases(); return "production ready"; }
 public static string Build() {GameMenuController.Instance.Resume(); BuildScreen.OpenDormitory(); Canvas.ForceUpdateCanvases();return "build ready";}
}
