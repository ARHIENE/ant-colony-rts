using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
public static class Meshy1111LifeActions
{
 static async Task Until(Func<bool> ready,string context,int milliseconds=12000)
 {
  var deadline=DateTime.UtcNow.AddMilliseconds(milliseconds);
  while(!ready()&&DateTime.UtcNow<deadline)await Task.Delay(50);
  if(!ready())throw new Exception(context+" timed out");
 }
 public static async Task<string> Run()
 {
  if(!Application.isPlaying)throw new Exception("Play required");
  var settings=UserSettings.Current.Clone();var root=SaveStorage.RootOverride;var report=new List<string>();
  try
  {
   EditorApplication.isPaused=false;await Until(()=>!SaveSystem.Busy&&GameMenuController.Instance!=null,"initial load",30000);
   SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"Life1111-"+Guid.NewGuid().ToString("N"));
   var isolated=settings.Clone();isolated.autoSaveEnabled=false;UserSettings.Apply(isolated,false);
   SaveSystem.NewGame(new NewGameOptions{seed=1110,mapSize=MapSize.Small,startingCount=3,startingTeam=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray()});
   await Until(()=>!SaveSystem.Busy&&GameSession.Instance!=null&&GameSession.Instance.GameStarted,"new game",30000);
   GameMenuController.Instance.Resume();SimulationPause.UserSpeed(1);
   var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember);var v=c.GetComponent<AntVisual>();
   await Until(()=>c.GetComponent<MeshyNativeMotionPlayer>()!=null,"native player");var p=c.GetComponent<MeshyNativeMotionPlayer>();
   foreach(var n in ResourceNode.Available.Where(n=>n.WorkRequested).ToArray())n.CancelWork();c.CommandStop();
   var start=c.Position;bool placed=false;
   for(int i=0;i<12&&!placed;i++)if(NavMesh.SamplePosition(start+Quaternion.Euler(0,i*30,0)*Vector3.forward*6,out var hit,1,NavMesh.AllAreas)&&c.CanReach(hit.position)){c.CommandMove(hit.position);placed=true;}
   if(!placed)throw new Exception("No reachable movement target");
   await Until(()=>v.MotionName=="Walk"||v.MotionName=="Run","actual movement");
   await Task.Delay(300);var distance=Vector3.Distance(start,c.Position);
   if(distance<.1f||p.NativeModel==null)throw new Exception("Movement did not advance");
   report.Add("Actual navigation PASS "+v.MotionName+" / "+p.PlayingMotion+" distance="+distance);
   c.CommandStop();
   var food=ResourceNode.Available.Where(n=>n.ResourceType==ResourceType.Food&&!n.IsLooseCargo&&n.CanGather&&!n.NeedsSowing).OrderBy(n=>(n.transform.position-c.Position).sqrMagnitude).First();
   if(!c.TryGatherApproach(food,out var approach))throw new Exception("Harvest approach unreachable");
   c.GetComponent<NavMeshAgent>().Warp(approach);c.CommandGather(food);
   await Until(()=>c.IsGatheringAnimation&&v.MotionName=="Tool_Harvest","actual harvest");
   await Task.Delay(120);
   if(p.PlayingMotion!="Pull_Radish")throw new Exception("Harvest clip mismatch "+p.PlayingMotion);
   report.Add("Actual harvesting PASS "+food.name+" / "+v.MotionName+" / "+p.PlayingMotion);
   foreach(var n in ResourceNode.Available.Where(n=>n.WorkRequested).ToArray())n.CancelWork();c.CommandStop();
   GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds,110);
   await Until(()=>c.IsAsleep&&v.MotionName=="Sleep","actual night sleep");await Task.Delay(100);
   if(p.PlayingMotion!="Sleep_Normally")throw new Exception("Sleep clip mismatch "+p.PlayingMotion);
   report.Add("Actual night sleep PASS IsAsleep=true / "+p.PlayingMotion);
   if(Physics.Raycast(c.Position+Vector3.up*3,Vector3.down,out var ground,10,1<<8,QueryTriggerInteraction.Ignore))
   {
    float bottom=float.PositiveInfinity;
    foreach(var skin in p.NativeModel.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled))
    {
     var baked=new Mesh();skin.BakeMesh(baked);
     foreach(var vertex in baked.vertices)bottom=Mathf.Min(bottom,skin.transform.TransformPoint(vertex).y);
     UnityEngine.Object.Destroy(baked);
    }
    float clearance=bottom-ground.point.y;
    report.Add("Sleep ground clearance="+clearance.ToString("F4")+"m "+(Mathf.Abs(clearance)>.15f?"REVIEW":"PASS"));
   }
   File.WriteAllLines("work/animation-1111/life-actions.txt",report);return string.Join("\n",report);
  }
  finally{SimulationPause.UserSpeed(0);UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;}
 }
}
