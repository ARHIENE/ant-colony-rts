using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using UnityEngine;
public static class SaveCapturePrep
{
 const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
 public static async Task<string> Main()
 {
  while(SaveSystem.Busy)await Task.Delay(50);
  SaveStorage.RootOverride=System.IO.Path.Combine(Application.temporaryCachePath,"SaveVisual1026");
  var settings=UserSettings.Current.Clone();settings.autoSaveEnabled=false;settings.autoPauseThreat=settings.autoPauseDowned=settings.autoPauseRocket=false;UserSettings.Apply(settings,false);
  StartingCandidate[] team;do{team=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray();}while(StartingCandidate.Missing(team).Length>0);
  SaveSystem.NewGame(new NewGameOptions{seed=261026,biome=MapBiome.Forest,climate=MapClimate.Temperate,mapSize=MapSize.Small,startingCount=3,startingTeam=team});
  while(SaveSystem.Busy)await Task.Delay(50);
  AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;
  foreach(var c in CommanderRoster.Instance.Commanders.Where(c=>c.IsColonyMember)){c.SetJobEnabled(AntColony.Units.CommanderJobs.All,false);c.CommandStop();c.PersonalState.meal=new AntColony.Units.CommanderMealState();c.PersonalState.sleep=new AntColony.Units.CommanderSleepState();c.PersonalState.work.resting=false;c.Traits.values.Remove(AntColony.Units.CommanderTrait.Nocturnal);}
  var c0=CommanderRoster.Instance.Commanders.First(c=>c.IsColonyMember&&c.CanDoJob(AntColony.Units.CommanderJobs.Gathering));
  var node=AntColony.World.ResourceNode.Available.Where(n=>n.ResourceType==AntColony.Data.ResourceType.Leaf&&!n.IsLooseCargo&&n.CanGatherWith(c0)).OrderBy(n=>(n.transform.position-c0.Position).sqrMagnitude).First();
  UnityEngine.AI.NavMesh.SamplePosition(node.transform.position+Vector3.back*2.3f,out var hit,3,UnityEngine.AI.NavMesh.AllAreas);c0.Agent.Warp(hit.position);
  var cam=UnityEngine.Camera.main.GetComponent<AntColony.Camera.IsometricCameraController>();cam.ApplySettings(35,18);typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",Hidden).Invoke(cam,new object[]{node.transform.position,0f,9f});
  var selection=UnityEngine.Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();selection.ClearSelection();typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection",Hidden).Invoke(selection,new object[]{c0.GetComponent<AntColony.Units.SelectableObject>()});
  Time.timeScale=1;await Task.Delay(200);Physics.SyncTransforms();
  var click=(Vector2)UnityEngine.Camera.main.WorldToScreenPoint(node.GetComponent<Collider>().bounds.center);
  typeof(AntColony.Units.UnitSelectionController).GetMethod("IssueCommand",Hidden).Invoke(UnityEngine.Object.FindAnyObjectByType<AntColony.Units.UnitSelectionController>(),new object[]{click});
  var limit=Time.realtimeSinceStartup+10;while(!c0.IsGatheringAnimation&&Time.realtimeSinceStartup<limit)await Task.Delay(100);
  cam.enabled=false;
  var message="Capture ready: "+c0.CommanderName+" gathering="+c0.IsGatheringAnimation+" clip="+string.Join(",",AntColony.Units.RigAnimator.Find(c0.gameObject).GetCurrentAnimatorClipInfo(0).Select(x=>x.clip.name));System.IO.File.WriteAllText("Temp/save-capture-ready.txt",message);return message;
 }
 public static async Task<string> Gather()
 {
  AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;
  // Isolated SAVE fixture: gather in daylight instead of the colony's scheduled sleep phase.
  typeof(GameSession).GetProperty("GameSeconds").SetValue(GameSession.Instance,20f);
  var world=UnityEngine.Object.FindAnyObjectByType<AntColony.UI.WorldMapPanel>();if(world.IsOpen)world.Toggle();
  var c=CommanderRoster.Instance.Commanders.First(c=>c.IsColonyMember&&c.CanDoJob(AntColony.Units.CommanderJobs.Gathering));c.CommandStop();c.PersonalState.meal=new AntColony.Units.CommanderMealState();c.PersonalState.sleep=new AntColony.Units.CommanderSleepState();c.PersonalState.work.resting=false;c.Traits.values.Remove(AntColony.Units.CommanderTrait.Nocturnal);
  var node=AntColony.World.ResourceNode.Available.Where(n=>n.ResourceType==AntColony.Data.ResourceType.Leaf&&!n.IsLooseCargo&&n.CanGatherWith(c)).OrderBy(n=>(n.transform.position-c.Position).sqrMagnitude).First();
  if(!UnityEngine.AI.NavMesh.SamplePosition(node.transform.position+Vector3.back*2,out var hit,4,UnityEngine.AI.NavMesh.AllAreas)||!c.Agent.Warp(hit.position))throw new Exception("Cannot prepare reachable capture position");
  var cam=UnityEngine.Camera.main.GetComponent<AntColony.Camera.IsometricCameraController>();typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",Hidden).Invoke(cam,new object[]{node.transform.position,0f,8f});cam.enabled=false;
  c.CommandGather(node);AntColony.Units.GatherOrderFeedback.Show(node,c);Time.timeScale=1;var end=Time.realtimeSinceStartup+10;while(!c.IsGatheringAnimation&&Time.realtimeSinceStartup<end)await Task.Delay(50);await Task.Delay(250);Time.timeScale=0;
  var selector=UnityEngine.Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();selector.ClearSelection();typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection",Hidden).Invoke(selector,new object[]{c.GetComponent<AntColony.Units.SelectableObject>()});
  return "Paused working pose: "+c.Position+" screen="+UnityEngine.Camera.main.WorldToScreenPoint(c.Position)+" clip="+string.Join(",",AntColony.Units.RigAnimator.Find(c.gameObject).GetCurrentAnimatorClipInfo(0).Select(x=>x.clip.name));
 }
 public static async Task<string> Candidates(){Time.timeScale=0;typeof(AntColony.UI.GameMenuController).GetMethod("NewGameScreen",Hidden).Invoke(AntColony.UI.GameMenuController.Instance,null);await Task.Yield();Canvas.ForceUpdateCanvases();return "Candidate screen ready";}
 public static async Task<string> Roster(){Time.timeScale=0;AntColony.UI.GameMenuController.Instance.Details(CommanderRoster.Instance.Commanders.First(c=>c.IsColonyMember));await Task.Yield();Canvas.ForceUpdateCanvases();return "Commander detail ready";}
 public static async Task<string> World(){AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;var p=UnityEngine.Object.FindAnyObjectByType<AntColony.UI.WorldMapPanel>();if(!p.IsOpen)p.Toggle();await Task.Yield();await Task.Yield();Canvas.ForceUpdateCanvases();return "World/expedition screen ready";}
}
