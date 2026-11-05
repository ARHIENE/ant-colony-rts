using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using AntColony.Buildings;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;
public static class FunFloor1106Checks {
 static string log="";static int count;
 static void Check(bool ok,string msg){log+=(ok?"PASS ":"FAIL ")+msg+"\n";File.WriteAllText("Temp/fun-floor-1106.txt",log);if(!ok)throw new Exception(log);count++;}
 static async Task Wait(Func<bool> condition){var end=DateTime.UtcNow.AddSeconds(70);while(!condition()&&DateTime.UtcNow<end)await Task.Delay(50);if(!condition())throw new Exception("timeout");}
 public static async Task<string> Main(){
 if(!Application.isPlaying)throw new Exception("Play required");log="";count=0;
 var settings=UserSettings.Current.Clone();var root=SaveStorage.RootOverride;SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"FunFloor-"+Guid.NewGuid().ToString("N"));
 try{var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false;UserSettings.Apply(quiet,false);
 await Wait(()=>!SaveSystem.Busy);SaveSystem.NewGame(new NewGameOptions{seed=261106,mapSize=MapSize.Small});await Wait(()=>!SaveSystem.Busy);GameMenuController.Instance.Resume();Time.timeScale=0;
 var crew=CommanderRoster.Instance.Commanders.ToArray();foreach(var unit in crew){unit.CommandStop();unit.SetJobEnabled(CommanderJobs.All,false);}var c=crew.First(x=>x.CanDoJob(CommanderJobs.Building));c.SetJobEnabled(CommanderJobs.Building,true);
 var home=WorldMapManager.Instance.HomePosition;
 var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{BuildingKind.Storage,UnitRole.Worker});
 var building=UnityEngine.Object.Instantiate(template);building.SetActive(false);var siteGO=GameObject.CreatePrimitive(PrimitiveType.Cube);siteGO.name="FloorDeliveryCheck";siteGO.GetComponent<Collider>().isTrigger=true;siteGO.transform.position=home;
 var site=siteGO.AddComponent<BuildingConstructionSite>();site.Initialize(building,3);site.Require(ResourceType.Soil,5);
 typeof(DiplomacyManager).GetMethod("DropFloor",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(ResourceType),typeof(int),typeof(Vector3)},null).Invoke(null,new object[]{ResourceType.Soil,8,c.Position});var node=ResourceNode.Available.Where(n=>n.IsLooseCargo&&n.ResourceType==ResourceType.Soil).OrderBy(n=>(n.transform.position-c.Position).sqrMagnitude).First();
 Check(!SceneObjects.All<Storage>().Any(b=>b.isActiveAndEnabled),"no completed warehouse fixture");
 node.GatheringForbidden=true;await Task.Delay(120);BuildingConstructionSite.Allocate();Check(site.Needs[0].reserved==0,"forbidden floor pile not reserved");
 node.GatheringForbidden=false;await Task.Delay(120);BuildingConstructionSite.Allocate();Check(site.Needs[0].reserved==5,"floor stock reserves actual building need");
 Check(c.CommandFetchFor(site),"courier starts floor pickup without warehouse");Check(c.CurrentResourceNode==node,"floor source uses saved worker node");
 Check(SaveSystem.TrySave(false,0,out var error),"save pickup: "+error);Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"load pickup: "+error);await Wait(()=>!SaveSystem.Busy);Time.timeScale=0;
 c=CommanderRoster.Instance.Commanders.First(x=>x.DeliverySite is BuildingConstructionSite);site=(BuildingConstructionSite)c.DeliverySite;node=c.CurrentResourceNode;
 Check(node!=null&&node.IsLooseCargo,"floor source restored after real load");
 var before=node.AmountRemaining;var taken=(int)typeof(WorkerAnt).GetMethod("TakeFloorMaterial",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{null});
 Check(taken==5&&Mathf.Approximately(node.AmountRemaining,before-5),"take once removes exact source amount");
 Check((int)typeof(WorkerAnt).GetMethod("TakeFloorMaterial",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{null})==0,"repeated pickup cannot duplicate resources");
 // TickFetching normally assigns the taken cargo before moving. Exercise the existing delivery stage with that same cargo.
 typeof(WorkerAnt).GetMethod("RestoreCargo",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{(float)taken,ResourceType.Soil,null});
 Check(site.Deliver(c,ResourceType.Soil,taken)==5&&site.Ready,"existing construction delivery accepts floor material");
 typeof(WorkerAnt).GetMethod("RestoreCargo",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{0f,ResourceType.Soil,null});c.CommandStop();
 site.Cancel();Check(ResourceNode.Available.Where(n=>n.IsLooseCargo&&n.ResourceType==ResourceType.Soil).Sum(n=>n.AmountRemaining)>=8,"cancel returns delivered material to floor");
 return $"PASS {count}\n"+log;
 }finally{Time.timeScale=0;UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;}
 }
}