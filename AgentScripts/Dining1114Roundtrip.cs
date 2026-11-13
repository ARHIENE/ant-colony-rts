using System;using System.IO;using System.Linq;using System.Threading.Tasks;using UnityEngine;using AntColony.Core;using AntColony.Units;using AntColony.Buildings;using AntColony.Save;using AntColony.UI;
public static class Dining1114Roundtrip {
 static CommanderAnt[] Actors()=>CommanderRoster.Instance.Commanders.Where(c=>c&&c.IsColonyMember).ToArray();
 static void CameraAt(Kitchen table){var ctl=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();if(ctl)ctl.enabled=false;Camera.main.orthographicSize=3.4f;Camera.main.transform.position=table.Position+new Vector3(3,2.6f,-4);Camera.main.transform.LookAt(table.Position+Vector3.up*.3f);}
 public static async Task<string> Run(){if(string.IsNullOrEmpty(SaveStorage.RootOverride))throw new Exception("Isolated test root required");var table=UnityEngine.Object.FindObjectsByType<Kitchen>(FindObjectsInactive.Exclude).Single(k=>k.IsTable);var actors=Actors();foreach(var c in actors){if(!c.IsEating){c.CommandStop();c.PersonalState.meal.satiety=0;c.PersonalState.meal.served=true;c.PersonalState.meal.retrySeconds=0;}else c.PersonalState.meal.eatSeconds=100;}
 SimulationPause.UserSpeed(1);for(int i=0;i<180;i++){foreach(var c in actors)if(c.IsEating)c.PersonalState.meal.eatSeconds=100;if(actors.All(c=>c.IsEating&&c.MealTable==table&&c.MealSeatSlot>=0))break;await Task.Delay(100);}await Task.Delay(300);SimulationPause.UserSpeed(0);
 if(!actors.All(c=>c.IsEating&&c.MealTable==table&&c.MealSeatSlot>=0))throw new Exception("Not all actors reached dining seats");
 var ids=actors.Select(c=>c.PersonalState.id).ToArray();var slots=actors.Select(c=>c.MealSeatSlot).ToArray();if(slots.Distinct().Count()!=actors.Length)throw new Exception("Duplicate seat");float minimum=99;for(int i=0;i<actors.Length;i++)for(int j=i+1;j<actors.Length;j++)minimum=Mathf.Min(minimum,Vector3.Distance(actors[i].Position,actors[j].Position));if(minimum<1)throw new Exception("Seated bodies too close: "+minimum);
 CameraAt(table);ScreenCapture.CaptureScreenshot("work/furniture-wall-1114/dining-three.png");await Task.Delay(300);
 Camera.main.orthographicSize=12; if(!SaveSystem.TrySave(false,0,out var error))throw new Exception(error);var path=SaveSlots.PathFor(false,0);File.Copy(path,"work/furniture-wall-1114/dining-save.json",true);
 if(!SaveSystem.TryLoad(path,out error))throw new Exception(error);for(int i=0;i<300&&SaveSystem.Busy;i++)await Task.Delay(100);if(SaveSystem.Busy)throw new Exception("Load timeout");SimulationPause.UserSpeed(0);await Task.Delay(500);
 actors=ids.Select(id=>Actors().Single(c=>c.PersonalState.id==id)).ToArray();table=actors[0].MealTable;
 for(int i=0;i<actors.Length;i++)if(!actors[i].IsEating||!table||actors[i].MealTable!=table||actors[i].MealSeatSlot!=slots[i])throw new Exception("Seat persistence failed for "+i);
 CameraAt(table);ScreenCapture.CaptureScreenshot("work/furniture-wall-1114/dining-reloaded.png");await Task.Delay(300);
 var text="PASS occupied seats="+string.Join(",",slots)+" minimum actor spacing="+minimum+"; TrySave/TryLoad preserved table, seats and eating while paused; sources="+string.Join(",",actors.Select(c=>c.GetComponent<MeshyNativeMotionPlayer>().PlayingMotion));File.WriteAllText("work/furniture-wall-1114/dining-roundtrip.txt",text);return text;}
}


