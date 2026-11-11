using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.Data;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
public static class Animation1110Game
{
 public static Task<string> Chop()=>Gather("Wood");
 public static Task<string> Dig()=>Gather("Clay");
 public static async Task<string> PrepareAndGather(){await Prepare();return await Gather();}
 public static async Task<string> Prepare(){
  if(!Application.isPlaying)throw new Exception("Play required"); EditorApplication.isPaused=false; for(int i=0;i<200&&(SaveSystem.Busy||GameMenuController.Instance==null||!GameSession.Exists);i++)await Task.Delay(100);
  SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"ANIMATION1110-"+Guid.NewGuid().ToString("N"));
  var settings=UserSettings.Current.Clone();settings.autoSaveEnabled=false;UserSettings.Apply(settings,false);
  SaveSystem.NewGame(new NewGameOptions{seed=1110,mapSize=MapSize.Small,startingCount=3,startingTeam=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray()});
  for(int i=0;i<300&&(SaveSystem.Busy||GameMenuController.Instance==null||!GameSession.Exists||!GameSession.Instance.GameStarted);i++)await Task.Delay(100);
  GameMenuController.Instance.Resume();SimulationPause.UserSpeed(1);
  return "Isolated live game, autosave off";
 }
 public static async Task<string> Gather(string resource="Stone"){
  var type=(ResourceType)Enum.Parse(typeof(ResourceType),resource);
  var c=CommanderRoster.Instance.Commanders.First(x=>x!=null&&x.IsColonyMember);
  EditorApplication.isPaused=false;foreach(var previous in UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude))if(previous.WorkRequested)previous.CancelWork(); c.CommandStop();typeof(WorkerAnt).GetMethod("DropCargo",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(c,null);c.PersonalState.toolTier=2;
  var node=UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude).Where(n=>n.ResourceType==type&&!n.IsLooseCargo&&n.CanGather).OrderBy(n=>(n.transform.position-c.Position).sqrMagnitude).First();
  var nav=c.GetComponent<NavMeshAgent>();
  for(int i=0;i<12;i++){var p=node.transform.position+Quaternion.Euler(0,i*30,0)*Vector3.forward*2;if(NavMesh.SamplePosition(p,out var hit,3,NavMesh.AllAreas)){nav.Warp(hit.position);break;}}
  c.CommandGather(node);SimulationPause.UserSpeed(1);
  for(int i=0;i<160&&!c.IsGatheringAnimation;i++)await Task.Delay(100);
  if(!c.IsGatheringAnimation||c.CurrentResourceNode!=node)throw new Exception("Gather did not start: "+resource);
  var v=c.GetComponent<AntVisual>();var a=c.GetComponentsInChildren<Animator>().First(x=>x.runtimeAnimatorController!=null);
  var controller=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();if(controller)controller.enabled=false;
  var cam=Camera.main;cam.orthographic=true;cam.orthographicSize=1.45f;var focus=c.Position+Vector3.up*.65f;cam.transform.position=focus+new Vector3(-2,1.2f,3);cam.transform.LookAt(focus);
  EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
  await Task.Delay(300);
  var hand=c.GetComponentsInChildren<Transform>().First(t=>t.name=="hand.R");var before=hand.position;float t0=a.GetCurrentAnimatorStateInfo(0).normalizedTime;await Task.Delay(700);var moved=Vector3.Distance(before,hand.position);var t1=a.GetCurrentAnimatorStateInfo(0).normalizedTime;
  var report=resource+" target="+c.CurrentResourceNode?.ResourceType+" actualGather="+c.IsGatheringAnimation+" state="+v.StateName+" animator="+a.GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip?.name+" time="+t0+" -> "+t1+" handDelta="+moved;
  Focus();EditorApplication.isPaused=true;File.AppendAllText("work/animation-1110/game-audit.txt",report+"\n");return report;
 }
 public static string Focus(){
  var c=CommanderRoster.Instance.Commanders.First(x=>x!=null&&x.IsColonyMember);var model=c.GetComponentInChildren<MeshyEquipmentGrip>().transform;var cam=Camera.main;var ts=model.GetComponentsInChildren<Transform>();var focus=(ts.First(t=>t.name=="head").position+ts.First(t=>t.name=="foot.L").position+ts.First(t=>t.name=="foot.R").position)/3;
  cam.orthographicSize=1.05f;cam.transform.position=focus+model.right*1.8f-model.forward*.8f+Vector3.up*.65f;cam.transform.LookAt(focus);cam.nearClipPlane=.02f;
  return "close side view "+c.Position+" modelForward="+model.forward+" target="+c.CurrentResourceNode?.transform.position;
 }
 public static async Task<string> Move(){
  EditorApplication.isPaused=false;
  var c=CommanderRoster.Instance.Commanders.First(x=>x!=null&&x.IsColonyMember);foreach(var n in UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude))if(n.WorkRequested)n.CancelWork();c.CommandStop();typeof(WorkerAnt).GetMethod("DropCargo",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(c,null);SimulationPause.UserSpeed(1);var start=c.Position;
  for(int i=0;i<12;i++){var p=start+Quaternion.Euler(0,i*30,0)*Vector3.forward*8;if(NavMesh.SamplePosition(p,out var hit,2,NavMesh.AllAreas)&&c.CanReach(hit.position)){c.CommandMove(hit.position);break;}}
  await Task.Delay(2000);Focus();var a=RigAnimator.Find(c.gameObject);var result="agent="+c.GetComponent<NavMeshAgent>().velocity+" scale="+Time.timeScale+" move distance="+Vector3.Distance(start,c.Position)+" clip="+a.GetCurrentAnimatorClipInfo(0).First().clip.name;EditorApplication.isPaused=true;File.AppendAllText("work/animation-1110/game-audit.txt",result+"\n");return result;
 }
 public static async Task<string> Sleep(){
  EditorApplication.isPaused=false; foreach(var c in CommanderRoster.Instance.Commanders.Where(x=>x!=null&&x.IsColonyMember)){c.CommandStop();typeof(WorkerAnt).GetMethod("DropCargo",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(c,null);}
  GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds,110);SimulationPause.UserSpeed(1);
  await Task.Delay(2500);Focus();EditorApplication.isPaused=true;
  var unit=CommanderRoster.Instance.Commanders.First(x=>x!=null&&x.IsColonyMember);var a=RigAnimator.Find(unit.gameObject);
  var result="night="+GameCalendar.IsNight+" asleep="+unit.IsAsleep+" visual="+unit.GetComponent<AntVisual>().StateName+" clip="+a.GetCurrentAnimatorClipInfo(0).First().clip.name;
  File.AppendAllText("work/animation-1110/game-audit.txt",result+"\n");return result;
 }
}
