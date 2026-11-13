using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Units;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Data;
public static class Table1114Check
{
 public static async Task<string> Run()
 {
  var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember&&!x.IsNocturnal);
  GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds,0);
  c.CommandMove(c.Agent.nextPosition);
  c.PersonalState.meal.satiety=100;
  AntColony.UI.SimulationPause.UserSpeed(1);
  await Task.Delay(500);
  c.CommandStop();typeof(WorkerAnt).GetMethod("DropCargo",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
  var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).Invoke(null,new object[]{BuildingKind.BigTable,UnitRole.Worker});
  var placement=UnityEngine.Object.FindAnyObjectByType<BuildingPlacementController>();
  placement.BeginPlacement(BuildingKind.BigTable,UnitRole.Worker,null);
  Vector3 bedPoint=default; bool found=false;
  for(int radius=5;radius<15&&!found;radius+=2) for(int i=0;i<16&&!found;i++)
  {
   var desired=c.Position+Quaternion.Euler(0,i*22.5f,0)*Vector3.forward*radius;
   if(!UnityEngine.AI.NavMesh.SamplePosition(desired,out var floor,1,UnityEngine.AI.NavMesh.AllAreas))continue;
   var pos=(Vector3)typeof(BuildingPlacementController).GetMethod("GetPlacementPosition",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(placement,new object[]{template,floor.position});
   if(!(bool)typeof(BuildingPlacementController).GetMethod("CellValid",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(placement,new object[]{pos}))continue;
   bedPoint=pos;found=true;
  }
  placement.CancelPlacement();
  if(!found)throw new Exception("No valid bed placement");
  var go=UnityEngine.Object.Instantiate(template,bedPoint,Quaternion.identity);
  go.name="TABLE1114";go.SetActive(true);FacilityVisual.Attach(go.GetComponent<BuildingBase>());
  c.PersonalState.meal.satiety=0;c.PersonalState.meal.served=true;c.PersonalState.meal.retrySeconds=0;
  AntColony.UI.SimulationPause.UserSpeed(1);
  Vector3 start=c.Position;
  for(int i=0;i<160&&!c.IsEating;i++)await Task.Delay(100);
  await Task.Delay(250);
  AntColony.UI.SimulationPause.UserSpeed(0);
  if(!c.IsEating||c.MealTable!=go.GetComponent<Kitchen>())throw new Exception("Did not arrive and eat at table");
  if(c.GetComponent<MeshyNativeMotionPlayer>().PlayingMotion!="Chair_Sit_Idle_M")throw new Exception("Native seated motion did not start");
  var result="PASS table eating; traveled="+Vector3.Distance(start,c.Position)+" actor="+c.Position+" table="+go.transform.position+" source="+c.GetComponent<MeshyNativeMotionPlayer>().PlayingMotion;
  var controller=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();if(controller)controller.enabled=false;
  Camera.main.orthographicSize=3.5f;Camera.main.transform.position=go.transform.position+new Vector3(3,3,-4);Camera.main.transform.LookAt(go.transform.position+Vector3.up*.5f);
  System.IO.File.WriteAllText("work/furniture-wall-1114/table-check.txt",result);
  ScreenCapture.CaptureScreenshot("work/furniture-wall-1114/table.png");await Task.Delay(300); return result;
 }
}
