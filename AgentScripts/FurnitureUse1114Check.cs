using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using UnityEngine.AI;using AntColony.Units;using AntColony.Core;using AntColony.Buildings;using AntColony.Data;using AntColony.UI;
public static class FurnitureUse1114Check
{
 static BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
 public static Task<string> Shelf()=>Run(BuildingKind.Bookshelf);
 public static Task<string> Bench()=>Run(BuildingKind.Carpentry);
 static GameObject Build(BuildingKind kind,CommanderAnt c)
 {
  var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).Invoke(null,new object[]{kind,UnitRole.Worker});
  var pc=UnityEngine.Object.FindAnyObjectByType<BuildingPlacementController>();pc.BeginPlacement(kind,UnitRole.Worker,null);
  try{for(int radius=5;radius<16;radius+=2)for(int i=0;i<16;i++){
   var desired=c.Position+Quaternion.Euler(0,i*22.5f,0)*Vector3.forward*radius;
   if(!NavMesh.SamplePosition(desired,out var floor,1,NavMesh.AllAreas))continue;
   var pos=(Vector3)typeof(BuildingPlacementController).GetMethod("GetPlacementPosition",Hidden).Invoke(pc,new object[]{template,floor.position});
   if(!(bool)typeof(BuildingPlacementController).GetMethod("CellValid",Hidden).Invoke(pc,new object[]{pos}))continue;
   var go=UnityEngine.Object.Instantiate(template,pos,Quaternion.identity);go.name="USE1114_"+kind;go.SetActive(true);FacilityVisual.Attach(go.GetComponent<BuildingBase>());return go;
  }}finally{pc.CancelPlacement();}throw new Exception("No valid position");
 }
 static async Task<string> Run(BuildingKind kind)
 {
  UnityEditor.EditorApplication.isPaused=false; if(string.IsNullOrEmpty(AntColony.Save.SaveStorage.RootOverride))throw new Exception("Isolated root required");
  var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember&&!x.IsNocturnal);c.CommandMove(c.Agent.nextPosition);c.CommandStop();
  typeof(WorkerAnt).GetMethod("DropCargo",Hidden).Invoke(c,null);c.PersonalState.meal.eatSeconds=0;c.PersonalState.meal.satiety=100;c.PersonalState.joy.joy=100;
  GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds,0);
  var go=Build(kind,c);var b=go.GetComponent<BuildingBase>();SimulationPause.UserSpeed(1);await Task.Delay(500);
  if(!FurnitureUseSurface.TryPoint(b,out var stand,out var surface))throw new Exception("No furniture surface");
  var start=c.Position;Processor.State state=null;
  if(kind==BuildingKind.Bookshelf){c.PersonalState.joy.joy=0;c.PersonalState.joy.retrySeconds=0;}
  else {var bench=go.GetComponent<Processor>();state=new Processor.State{current=0};typeof(Processor).GetMethod("RestoreState",Hidden).Invoke(bench,new object[]{state});c.SetJobEnabled(CommanderJobs.Crafting,true);if(!c.StartService(bench,CommanderJobs.Crafting))throw new Exception("StartService refused");}
  string expected=kind==BuildingKind.Bookshelf?"ReadBook":"Saw";
  for(int i=0;i<180;i++){if(c.GetComponent<AntVisual>().MotionName==expected)break;if(state!=null)state.progress=0;await Task.Delay(100);}
  var v=c.GetComponent<AntVisual>();var p=c.GetComponent<MeshyNativeMotionPlayer>();if(v.MotionName!=expected||p.PlayingMotion!="Idle_3")throw new Exception("Use not playing: "+v.MotionName+" native="+p.PlayingMotion+" position="+c.Position+" stand="+stand+" service="+c.ServiceTarget+" play="+c.PlaySpot);
  await Task.Delay(400);SimulationPause.UserSpeed(0);
  var ctl=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();if(ctl)ctl.enabled=false;
  var focus=Vector3.Lerp(c.Position,b.Position,.5f)+Vector3.up*.3f;Camera.main.orthographicSize=2.2f;Camera.main.transform.position=focus+(kind==BuildingKind.Bookshelf?new Vector3(3,1.6f,4):new Vector3(-3,1.6f,-4));Camera.main.transform.LookAt(focus);
  ScreenCapture.CaptureScreenshot("work/furniture-wall-1114/"+kind+"-use.png");await Task.Delay(300);
  var result=kind+" PASS actual action="+v.MotionName+" native="+p.PlayingMotion+" moved="+Vector3.Distance(start,c.Position)+" position="+c.Position+" standing="+stand+" surface="+surface+" distance="+Vector3.ProjectOnPlane(c.Position-stand,Vector3.up).magnitude;
  File.WriteAllText("work/furniture-wall-1114/"+kind+"-use.txt",result);return result;
 }
}
