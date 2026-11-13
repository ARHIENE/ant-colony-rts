using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Units;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Data;
public static class Motion1113BedCheck
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
  var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).Invoke(null,new object[]{BuildingKind.SingleBed,UnitRole.Worker});
  var placement=UnityEngine.Object.FindAnyObjectByType<BuildingPlacementController>();
  placement.BeginPlacement(BuildingKind.SingleBed,UnitRole.Worker,null);
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
  go.name="MOTION1113 bed";go.SetActive(true);FacilityVisual.Attach(go.GetComponent<BuildingBase>());
  Dormitory.Assign(c);
  GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds,105);
  c.PersonalState.sleep.fatigue=80;
  AntColony.UI.SimulationPause.UserSpeed(1);
  Vector3 start=c.Position,support=default,head=default;
  for(int i=0;i<100&&!c.TrySleepingPose(out support,out head);i++)await Task.Delay(100);
  await Task.Delay(600);
  AntColony.UI.SimulationPause.UserSpeed(0);
  var p=c.GetComponent<MeshyNativeMotionPlayer>();
  var result="arrived="+c.TrySleepingPose(out support,out head)+" traveled="+Vector3.Distance(start,c.Position)+" support="+support+" original="+p.PlayingMotion+" bedSeconds="+c.PersonalState.sleep.bedSeconds;
  var controller=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();if(controller)controller.enabled=false;
  Camera.main.orthographicSize=2.4f;Camera.main.transform.position=go.transform.position+new Vector3(3,3,-4);Camera.main.transform.LookAt(go.transform.position+Vector3.up*.5f);
  System.IO.File.WriteAllText("work/motion-actions-1113/bed-check.txt",result);
  return result;
 }
}
