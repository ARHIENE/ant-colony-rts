using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using AntColony.UI;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
public static class AnimeSettlerGameplayCapture
{
 public static async Task<string> Main()
 {
  var units=Object.FindObjectsByType<CommanderAnt>(FindObjectsSortMode.None).OrderBy(c=>c.name).ToArray();
  if(units.Length<1)throw new Exception("Need live game units");
  var cam=Camera.main;var control=cam.GetComponent<AntColony.Camera.IsometricCameraController>();
  var oldEnabled=control.enabled;var oldPosition=cam.transform.position;var oldRotation=cam.transform.rotation;var oldSize=cam.orthographicSize;
  var pause=SimulationPause.Capture();GameObject hut=null;
  var folder="art/anime-settler-1105/gameplay-frames";Directory.CreateDirectory(folder);
  var start=units.Select(c=>c.Position).ToArray();
  try{
   var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{BuildingKind.Hut,UnitRole.Worker});
   var hutPosition=start[0]+new Vector3(6,0,-2);
   if(NavMesh.SamplePosition(hutPosition,out var hutGround,4,NavMesh.AllAreas))hutPosition=hutGround.position;
   hut=Object.Instantiate(template,hutPosition,Quaternion.identity);hut.name="Temporary gameplay hut preview";hut.SetActive(true);
   control.enabled=false;cam.orthographicSize=3.5f;
   var focus=start[0]+new Vector3(3,.8f,2);
   cam.transform.position=focus+new Vector3(0,8,9);cam.transform.LookAt(focus);
   for(var i=0;i<units.Length;i++){
    var ap=units[i].GetComponentInChildren<KemonoAppearance>();
    ap.SetVariant("Hair",new[]{"BOB","LONG","TWIN"}[i%3]);ap.SetVariant("Outfit",new[]{"CITIZEN","FARMER","GUARD"}[i%3]);
    if(!NavMesh.SamplePosition(start[i]+Vector3.forward*6,out var hit,4,NavMesh.AllAreas))throw new Exception("No walk destination");
    units[i].CommandMove(hit.position);
   }
   SimulationPause.UserSpeed(1);Time.timeScale=1;
   await Task.Delay(300);
   for(var frame=0;frame<45;frame++){
    var center=units.Aggregate(Vector3.zero,(p,u)=>p+u.Position)/units.Length+Vector3.up*.8f;
    cam.transform.position=center+new Vector3(0,7,9);cam.transform.LookAt(center);
    ScreenCapture.CaptureScreenshot(Path.GetFullPath(folder+"/frame-"+frame.ToString("D3")+".png"));
    await Task.Delay(100);
   }
   await Task.Delay(250);
   var distances=units.Select((c,i)=>Vector3.Distance(start[i],c.Position)).ToArray();
   if(!distances.Any(d=>d>.5f))throw new Exception("Game did not move; capture is not valid");
   var result="Actual saved-game commanders moved: "+string.Join(", ",distances.Select(d=>d.ToString("F2")+"m"))+"; 45 screen frames requested; temporary functional hut included.";
   File.WriteAllText("art/anime-settler-1105/gameplay-capture.txt",result);return result;
  }finally{
   Time.timeScale=0;SimulationPause.Restore(pause);Time.timeScale=0;
   if(hut!=null)Object.Destroy(hut);
   cam.transform.position=oldPosition;cam.transform.rotation=oldRotation;cam.orthographicSize=oldSize;control.enabled=oldEnabled;
  }
 }
}
