using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
public static class SaveV6Prepare {
public static async Task<string> Main() {
var previous=AntColony.Save.SaveStorage.RootOverride;
AntColony.Save.SaveStorage.RootOverride=System.IO.Path.Combine(Application.temporaryCachePath,"SaveV6-"+Guid.NewGuid());
try{
for(int i=0;i<1800 && AntColony.Save.SaveSystem.Busy;i++) await Task.Delay(50);
if(AntColony.Save.SaveSystem.Busy)throw new Exception("initial scene busy");
AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions{seed=77,mapSize=AntColony.Core.MapSize.Small,biome=AntColony.Core.MapBiome.Forest});
for(int i=0;i<1800 && AntColony.Save.SaveSystem.Busy;i++)await Task.Delay(50);
if(AntColony.Save.SaveSystem.Busy)throw new Exception("new scene busy");
AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;
var c=UnityEngine.Object.FindObjectsByType<AntColony.Units.CommanderAnt>(FindObjectsSortMode.None).First(c=>!c.IsDead);
UnityEngine.Object.FindAnyObjectByType<AntColony.Units.SelectionManager>().SelectOnly(c.GetComponent<AntColony.Units.SelectableObject>());
UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>().FocusOn(c.Position);
await Task.Delay(600);
return "ready "+c.CommanderName+" "+Screen.width+"x"+Screen.height;
}finally{AntColony.Save.SaveStorage.RootOverride=previous;}
}}
