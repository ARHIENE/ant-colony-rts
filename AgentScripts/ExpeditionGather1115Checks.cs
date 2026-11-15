using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
public static class ExpeditionGather1115Checks
{
 const string Log="work/colony-polish-1115/expedition-gather.txt";
 static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool ok,string note){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+note+"\n");if(!ok)throw new Exception(note);}
 static async Task Wait(Func<bool> ready,int seconds=90){var end=DateTime.UtcNow.AddSeconds(seconds);while(!ready()&&DateTime.UtcNow<end)await Task.Delay(100);if(!ready())throw new Exception("Readiness timeout");}
 public static string Main(){_=Run();return "Started isolated expedition gathering fixture; natural progression remains separately preserved";}
 public static async Task<string> Review(){
  if(!SaveSystem.TryLoad(Path.GetFullPath("work/colony-polish-1115/expedition-save/saves/manual1.json"),out var error))throw new Exception(error);
  try{
   await Wait(()=>!SaveSystem.Busy&&GameSession.Exists&&GameSession.Instance.GameStarted);SimulationPause.UserSpeed(0);
   var ship=WorldMapManager.Instance.Transports.Single(t=>t!=null&&t.State==ExpeditionState.Deployed);
   var c=ship.Crew.Single();WorldMapManager.Instance.ViewSite(ship.Site);
   var rootRenderer=ship.GetComponent<Renderer>();var visible=ship.GetComponentsInChildren<Renderer>().Where(r=>r!=rootRenderer&&r.enabled&&!r.forceRenderingOff).ToArray();
   Check(rootRenderer.forceRenderingOff&&visible.Length>0,"Loaded walking party replaces yellow placeholder with Meshy pack");
   Check(visible.All(r=>r.sharedMaterials.All(m=>m&&m.shader.name=="Universal Render Pipeline/Lit"&&m.GetTexture("_BaseMap"))),"Pack uses mapped URP texture");
   var bounds=visible[0].bounds;foreach(var r in visible.Skip(1))bounds.Encapsulate(r.bounds);
   Check(Mathf.Abs(Mathf.Max(bounds.size.x,bounds.size.z)-1.5f)<.02f&&Mathf.Abs(bounds.min.y-ship.Position.y)<.02f,"Pack keeps 1.5m footprint and grounded bottom under scaled vehicle parent");
   Check(ship.GetCargo(ResourceType.Soil)==14&&c.TryWorkApproach(ship.Position,out _),"Loaded cargo and transport approach remain available");
   var camera=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
   typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",Hidden).Invoke(camera,new object[]{c.Position+Vector3.up,0f,10f});
   await Task.Delay(1200);ScreenCapture.CaptureScreenshot("work/colony-polish-1115/expedition-gather-close.png");await Task.Delay(500);
   return $"Captured landed commander and transport, soil cargo={ship.GetCargo(ResourceType.Soil)}";
  }finally{
   if(!SaveSystem.TryLoad(Path.GetFullPath("work/natural-campaign-1115/checkpoint-save.json"),out error))throw new Exception(error);
   await Wait(()=>!SaveSystem.Busy&&GameSession.Exists&&GameSession.Instance.GameStarted);SimulationPause.UserSpeed(0);
  }
 }
 static async Task Run(){
  File.WriteAllText(Log,"RUNNING isolated prepared expedition; placement and later full-cargo injection are fixture only\n");
  var root=SaveStorage.RootOverride;
  try{
   if(string.IsNullOrEmpty(root)||Time.timeScale!=0)throw new Exception("Paused isolated campaign required");
   SaveStorage.RootOverride=Path.GetFullPath("work/colony-polish-1115/expedition-save");
   var world=WorldMapManager.Instance;var site=world.Sites.First(s=>WorldMapManager.SiteRevealed(s)&&s.Kind==ExpeditionSiteKind.ResourceSite);
   var c=CommanderRoster.Instance.Commanders[0];c.CommandStop();typeof(WorkerAnt).GetMethod("DropCargo",Hidden).Invoke(c,null);
   var ship=world.CreateTransport(false,site.transform.position,true);
   typeof(ExpeditionTransport).GetMethod("RestoreState",Hidden).Invoke(ship,new object[]{ExpeditionState.Deployed,0f,site,site.transform.position,world.HomePosition,0,0,0});
   typeof(ExpeditionTransport).GetMethod("RestoreCrew",Hidden).Invoke(ship,new object[]{new List<CommanderAnt>{c}});
   await Task.Delay(500);
   var soil=ResourceNode.Available.Where(n=>n.ResourceType==ResourceType.Soil&&!n.IsDepleted&&(n.transform.position-site.transform.position).sqrMagnitude<70*70).OrderBy(n=>(n.transform.position-c.Position).sqrMagnitude).First();
   var homeNode=ResourceNode.Available.First(n=>ProductionStock.AtHome(n.transform.position)&&n.CanGather);
   Check(!c.TryDirectGather(homeNode,out var reason)&&reason.Contains("현재 원정 지역"),"Remote home resource rejected before changing commander work");
   bool accepted=c.TryDirectGather(soil,out reason);
   Check(accepted,"Arrived expedition accepts actual local soil gathering: "+reason);
   Check(!c.TryDirectGather(homeNode,out reason)&&c.CurrentResourceNode==soil,"Rejected cross-region command preserves accepted gathering order");
   typeof(ExpeditionTransport).GetProperty("State").SetValue(ship,ExpeditionState.Outbound);
   Check(!c.TryDirectGather(soil,out reason),"Travel state rejects field gathering");
   typeof(ExpeditionTransport).GetProperty("State").SetValue(ship,ExpeditionState.Deployed);
   world.ViewSite(site);SimulationPause.UserSpeed(3);
   await Wait(()=>ship.GetCargo(ResourceType.Soil)>0,55);SimulationPause.UserSpeed(0);
   int cargo=ship.GetCargo(ResourceType.Soil);Check(cargo>0,"Actual movement, extraction and deposit into expedition cargo: "+cargo);
   ScreenCapture.CaptureScreenshot("work/colony-polish-1115/expedition-gather.png");await Task.Delay(300);
   if(!SaveSystem.TrySave(false,0,out var error))throw new Exception(error);
   if(!SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error))throw new Exception(error);
   await Wait(()=>!SaveSystem.Busy&&GameSession.Exists&&GameSession.Instance.GameStarted);SimulationPause.UserSpeed(0);
   ship=WorldMapManager.Instance.Transports.Single(t=>t!=null&&t.State==ExpeditionState.Deployed);c=ship.Crew.Single();
   Check(ship.GetCargo(ResourceType.Soil)==cargo&&!c.IsEmbarked,"Full reload preserves real collected cargo and landed crew");
   var local=ResourceNode.Available.First(n=>n.ResourceType==ResourceType.Soil&&!n.IsDepleted&&(n.transform.position-ship.Site.transform.position).sqrMagnitude<70*70);
   ship.AddCargo(ResourceType.Soil,ship.ReturnCargoSpace);
   Check(!c.TryDirectGather(local,out reason)&&reason.Contains("화물 공간"),"Full expedition cargo rejects a new gathering order");
  }catch(Exception e){File.AppendAllText(Log,"ERROR "+e+"\n");}
  finally{
   SimulationPause.UserSpeed(0);SaveStorage.RootOverride=root;
   if(SaveSystem.TryLoad(Path.GetFullPath("work/natural-campaign-1115/checkpoint-save.json"),out var error)){
    await Wait(()=>!SaveSystem.Busy&&GameSession.Exists&&GameSession.Instance.GameStarted);SimulationPause.UserSpeed(0);
    File.AppendAllText(Log,"RESTORED natural checkpoint; expedition fixture discarded\n");
   }else File.AppendAllText(Log,"RESTORE ERROR "+error+"\n");
  }
 }
}


