using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;
using AntColony.Core;using AntColony.Buildings;using AntColony.Data;using AntColony.Units;using AntColony.World;using AntColony.Save;using AntColony.UI;using UnityEngine;using UnityEngine.AI;
public static class Handcart1115Checks
{
 const string Log="work/colony-polish-1115/handcart-checks.txt";
 static void Check(bool ok,string text){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new Exception(text);}
 static async Task Wait(Func<bool> done,int seconds=120){var end=DateTime.UtcNow.AddSeconds(seconds);while(!done()&&DateTime.UtcNow<end)await Task.Delay(100);Check(done(),"Wait condition");}
 static GameObject Template(BuildingKind kind)=>(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{kind,UnitRole.Worker});
 public static string Main(){File.WriteAllText(Log,"Isolated supplied-stock fixture: real walking, construction work, boarding and pulling; not natural campaign proof.\n");_=Run();return "Handcart regression started";}
 static async Task Run(){var root=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"Handcart1115-"+Guid.NewGuid().ToString("N"));
 try{
  var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false;UserSettings.Apply(quiet,false);
  await Wait(()=>!SaveSystem.Busy);SaveSystem.NewGame(new NewGameOptions{seed=261115,mapSize=MapSize.Small});await Wait(()=>!SaveSystem.Busy);GameMenuController.Instance.Resume();SimulationPause.UserSpeed(0);
  foreach(var p in CommanderRoster.Instance.Commanders){p.CommandStop();p.SetJobEnabled(CommanderJobs.All,false);}
  var c=CommanderRoster.Instance.Commanders.First();c.PersonalState.toolTier=2;c.SetJobEnabled(CommanderJobs.Building|CommanderJobs.Hauling,true);
  Check(EarlyConstruction.HasFixedCost(BuildingKind.Handcart)&&EarlyConstruction.Cost(BuildingKind.Handcart).Sum(n=>n.amount)==35,"Fixed wood/plank/fiber costs");
  Check(ScienceEffects.BuildingResearch(BuildingKind.Handcart).Contains(ScienceTechnology.ManagedResources),"Early technology gate");
  var storeGo=UnityEngine.Object.Instantiate(Template(BuildingKind.Storage),c.Position+Vector3.left*4,Quaternion.identity);storeGo.SetActive(true);var store=storeGo.GetComponent<Storage>();
  foreach(var need in EarlyConstruction.Cost(BuildingKind.Handcart))ResourceManager.Instance.AddTo(store.Bin,need.type,need.amount);
  Check(NavMesh.SamplePosition(c.Position+Vector3.forward*6,out var hit,8,NavMesh.AllAreas),"Construction ground");
  var built=UnityEngine.Object.Instantiate(Template(BuildingKind.Handcart),hit.position,Quaternion.identity);
  var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.transform.position=hit.position;marker.GetComponent<Collider>().isTrigger=true;
  var site=marker.AddComponent<BuildingConstructionSite>();site.Initialize(built,30);foreach(var n in EarlyConstruction.Cost(BuildingKind.Handcart))site.Require(n.type,n.amount);
  c.CommandBuild(site);SimulationPause.UserSpeed(3);
  var end=DateTime.UtcNow.AddSeconds(150);while(site&&DateTime.UtcNow<end){if(c.CanStartConstruction)c.CommandBuild(site);await Task.Delay(200);}
  Check(!site,"Real material delivery and builder completed assembly");await Task.Delay(300);SimulationPause.UserSpeed(0);
  var world=WorldMapManager.Instance;var cart=world.Transports.Single(t=>t.Handcart);Check(cart.CargoCapacity>=80&&cart.CommanderCapacity==4&&cart.MaxHealth==180,"Handcart stats and model");
  c.CommandStop();c.SetJobEnabled(CommanderJobs.All,false);Check(cart.TryBoard(new[]{c}),"Board queued");SimulationPause.UserSpeed(3);await Wait(()=>cart.Crew.Contains(c));SimulationPause.UserSpeed(0);
  var target=world.Sites.FirstOrDefault(s=>WorldMapManager.SiteRevealed(s)&&s.Disposition!=ConquestDisposition.Abandoned);Check(target!=null,"Known destination");
  Check(cart.DepartureBlock(target)=="","Departure allowed: "+cart.DepartureBlock(target));var start=cart.Position;Check(cart.TryDepart(target)&&cart.PullingHandcart&&!c.IsEmbarked,"Departure exposes actual puller");
  SimulationPause.UserSpeed(3);await Wait(()=>Vector3.Distance(cart.Position,start)>4);SimulationPause.UserSpeed(0);Check(cart.PullingHandcart,"Actual local movement before world travel");
  c.CommandStop();cart.Tick(.01f);var pos=cart.Position;Check(SaveSystem.TrySave(false,0,out var error),"Save during paused pull: "+error);Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"Reload during pull: "+error);await Wait(()=>!SaveSystem.Busy);SimulationPause.UserSpeed(0);
  world=WorldMapManager.Instance;cart=world.Transports.Single(t=>t.Handcart);c=cart.Crew.First();Check(cart.PullingHandcart&&!c.IsEmbarked&&Vector3.Distance(cart.Position,pos)<.1f,"Pull state and position restored");
  Check(!c.IsFollowingMoveOrder,"Paused pull remains paused after reload");Check(cart.ResumeHandcartPull(),"Resume pull using player action");
  SimulationPause.UserSpeed(3);await Wait(()=>!cart.PullingHandcart,150);SimulationPause.UserSpeed(0);Check(cart.State==ExpeditionState.Outbound&&c.IsEmbarked,"Gate reached before world travel");
  Check(SaveSystem.TrySave(false,1,out error),"Save outbound cart: "+error);
 }catch(Exception e){File.AppendAllText(Log,"ERROR "+e+"\n");}
 finally{SimulationPause.UserSpeed(0);UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;File.AppendAllText(Log,"RESTORED settings and save root\n");}}
}
