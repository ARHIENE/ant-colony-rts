using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Units;
using AntColony.Core;
using AntColony.Data;
using AntColony.Buildings;
public static class Motion1113FarmCheck
{
 public static async Task<string> Run()
 {
  GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds,0);
  var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember);
  c.CommandStop(); c.CommandMove(c.Agent.nextPosition); c.PersonalState.meal.satiety=100;
  typeof(WorkerAnt).GetMethod("DropCargo",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);c.PersonalState.toolTier=2;
  var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).Invoke(null,new object[]{BuildingKind.Farm,UnitRole.Worker});
  UnityEngine.AI.NavMesh.SamplePosition(c.Position+Vector3.right*5,out var ground,2,UnityEngine.AI.NavMesh.AllAreas);
  var go=UnityEngine.Object.Instantiate(template,ground.position,Quaternion.identity);go.transform.position+=Vector3.up*(ground.position.y-go.GetComponent<Renderer>().bounds.min.y);go.name="MOTION1113 Farm";go.SetActive(true);
  FacilityVisual.Attach(go.GetComponent<BuildingBase>());var farm=go.GetComponent<FarmPlot>()??go.AddComponent<FarmPlot>();farm.Configure(FarmCrop.Fungus,false);FarmSeeds.Add(FarmCrop.Fungus,2);
  AntColony.UI.SimulationPause.UserSpeed(1);await Task.Delay(200);c.CommandGather(farm.Node);
  for(int i=0;i<120&&!c.IsGatheringAnimation;i++)await Task.Delay(100);
  await Task.Delay(200);AntColony.UI.SimulationPause.UserSpeed(0);
  var v=c.GetComponent<AntVisual>();var outward=Vector3.ProjectOnPlane(c.Position-go.transform.position,Vector3.up).normalized;
  if (!Physics.Raycast(c.Position+Vector3.up*8,Vector3.down,out var floor,24,1<<8)) throw new Exception("No terrain below farmer");
  bool fitted=MeshyWorkContact.TryRadius(farm.Node,"Tool_Till",outward,floor.point.y,out float radius);
  var result="gather="+c.IsGatheringAnimation+" sow="+farm.Node.NeedsSowing+" visual="+v.MotionName+" tool="+v.HeldTool+" native="+c.GetComponent<MeshyNativeMotionPlayer>().PlayingMotion+" fitted="+fitted+" radius="+radius+" actual="+Vector3.ProjectOnPlane(c.Position-go.transform.position,Vector3.up).magnitude;
  var controller=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();if(controller)controller.enabled=false;
  Camera.main.orthographicSize=2.6f;Camera.main.transform.position=go.transform.position+new Vector3(4,3,-4);Camera.main.transform.LookAt(go.transform.position+Vector3.up*.4f);
  System.IO.File.WriteAllText("work/motion-actions-1113/farm-check.txt",result);return result;
 }
}
