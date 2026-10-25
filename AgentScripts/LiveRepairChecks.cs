using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class LiveRepairChecks
{
 static int n;static void Check(bool ok,string s){if(!ok)throw new Exception("FAIL "+s);n++;}
 public static async Task<string> Main()
 {
  n=0;var root=AntColony.Save.SaveStorage.RootOverride;var settings=AntColony.Core.UserSettings.Current.Clone();
  AntColony.Save.SaveStorage.RootOverride=System.IO.Path.Combine(Application.temporaryCachePath,"LiveRepair-"+Guid.NewGuid().ToString("N"));
  try {
   var s=settings.Clone();s.autoSaveEnabled=false;s.autoPauseThreat=s.autoPauseDowned=s.autoPauseRocket=false;AntColony.Core.UserSettings.Apply(s,false);
   while(AntColony.Save.SaveSystem.Busy)await Task.Delay(50);
   AntColony.Core.StartingCandidate[] team;do{team=Enumerable.Range(0,2).Select(AntColony.Core.StartingCandidate.Roll).ToArray();}while(AntColony.Core.StartingCandidate.Missing(team).Length>0);
   AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions{seed=261025,mapSize=AntColony.Core.MapSize.Small,startingCount=2,startingTeam=team});
   while(AntColony.Save.SaveSystem.Busy)await Task.Delay(50);
   AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=1;
   var c=AntColony.Core.CommanderRoster.Instance.Commanders.First(x=>x.IsColonyMember&&x.CanDoJob(AntColony.Units.CommanderJobs.Gathering));
   foreach(var unit in AntColony.Core.CommanderRoster.Instance.Commanders.Where(x=>x.IsColonyMember))unit.CommandStop();
   var a=AntColony.Units.RigAnimator.Find(c.gameObject);var ts=c.GetComponentsInChildren<Transform>();var head=ts.First(t=>t.name=="head");var hip=ts.First(t=>t.name=="pevis");
   await Task.Delay(300);Check(Vector3.Dot((head.position-hip.position).normalized,Vector3.up)>.8f,"character remains upright on real frame");
   var bounds=c.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).Select(r=>r.bounds).Aggregate((x,y)=>{x.Encapsulate(y);return x;});Check(bounds.size.y>1.35f&&bounds.size.y<1.85f,"upright character scale "+bounds.size);
   var pos=c.Position;c.CommandMove(pos+Vector3.right*8);await Task.Delay(400);var first=a.GetCurrentAnimatorStateInfo(0).normalizedTime;var before=ts.Select(t=>t.localRotation).ToArray();await Task.Delay(400);
   Check(a.GetCurrentAnimatorStateInfo(0).normalizedTime>first,"animation advances without forced Update");Check(ts.Where((t,i)=>Quaternion.Angle(before[i],t.localRotation)>.2f).Any(),"bones animate on real frames");Check(Vector3.Distance(pos,c.Position)>.1f,"unit moves");Check(Vector3.Dot((head.position-hip.position).normalized,Vector3.up)>.75f,"walking stays upright");
   var portrait=AntColony.UI.CharacterPortraits.Get(c);Check(portrait!=null&&portrait.width==128,"portrait rendered");
   var cam=UnityEngine.Camera.main.GetComponent<AntColony.Camera.IsometricCameraController>();cam.ApplySettings(s.cameraPanSpeed,18);cam.FocusOn(cam.PanBounds.center.x*Vector3.right+cam.PanBounds.center.y*Vector3.forward);
   var focus=cam.FocusPoint;var mouse=Mouse.current;var old=mouse.position.ReadValue();InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(Screen.width*.5f,Screen.height-2)});await Task.Delay(150);Check(Vector3.Distance(focus,cam.FocusPoint)>.05f,"top edge pans camera during actual frames");InputSystem.QueueStateEvent(mouse,new MouseState{position=old});
   c.CommandStop();var node=AntColony.World.ResourceNode.Available.Where(x=>x.ResourceType==AntColony.Data.ResourceType.Wood&&x.CanGatherWith(c)).OrderBy(x=>(x.transform.position-c.Position).sqrMagnitude).First();
   Check(c.TryWorkApproach(node.transform.position,out var approach),"resource reachable");c.Agent.Warp(approach);cam.FocusOn(node.transform.position);await Task.Delay(200);Physics.SyncTransforms();
   var selection=UnityEngine.Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();selection.ClearSelection();typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(selection,new object[]{c.GetComponent<AntColony.Units.SelectableObject>()});
   var target=UnityEngine.Camera.main.WorldToScreenPoint(node.GetComponent<Collider>().bounds.center);var amount=node.AmountRemaining;
   typeof(AntColony.Units.UnitSelectionController).GetMethod("IssueCommand",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(UnityEngine.Object.FindAnyObjectByType<AntColony.Units.UnitSelectionController>(),new object[]{(Vector2)target});Check(c.CurrentResourceNode==node,"resource right click reaches gather command");
   var deadline=Time.realtimeSinceStartup+8;while(node.AmountRemaining>=amount&&Time.realtimeSinceStartup<deadline)await Task.Delay(100);
   Check(node.AmountRemaining<amount,"resource amount decreases on actual frames");Check(c.CarriedAmount>0,"gathered resource carried");
   var result="PASS "+n+" live upright/animation/portrait/edge/right-click harvesting checks";System.IO.File.WriteAllText("Temp/live-repair-result.txt",result);return result;
  }catch(Exception e){System.IO.File.WriteAllText("Temp/live-repair-result.txt",e.ToString());throw;}finally{Time.timeScale=0;AntColony.Save.SaveStorage.RootOverride=root;AntColony.Core.UserSettings.Apply(settings,false);}
 }
}
