using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using Object=UnityEngine.Object;
public static class LootLoopChecks
{
 static int n;static void Check(bool ok,string s){if(!ok)throw new Exception("FAIL "+s);n++;}
 public static async Task<string> Main(bool fresh)
 {
  n=0;var time=Time.timeScale;var settings=UserSettings.Current.Clone();var root=AntColony.Save.SaveStorage.RootOverride;
  AntColony.Save.SaveStorage.RootOverride=System.IO.Path.Combine(Application.temporaryCachePath,"LootLoop-"+Guid.NewGuid().ToString("N"));GameObject pile=null,warehouse=null;
  try
  {
   var config=settings.Clone();config.autoSaveEnabled=false;config.autoPauseThreat=config.autoPauseDowned=config.autoPauseRocket=false;UserSettings.Apply(config,false);
   while(AntColony.Save.SaveSystem.Busy)await Task.Delay(50);
   if(fresh) {
   var team=Enumerable.Range(0,2).Select(StartingCandidate.Roll).ToArray();foreach(var member in team)member.traits.values.Clear();
   AntColony.Save.SaveSystem.NewGame(new NewGameOptions{seed=261025,mapSize=MapSize.Small,startingCount=2,startingTeam=team});while(AntColony.Save.SaveSystem.Busy)await Task.Delay(50);AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;
   }
   foreach(var unit in CommanderRoster.Instance.Commanders.Where(c=>c.IsColonyMember)){unit.CommandStop();unit.SetJobEnabled(CommanderJobs.All,false);}
   var c=CommanderRoster.Instance.Commanders.First(c=>c.IsColonyMember&&c.CanDoJob(CommanderJobs.Hauling));
   typeof(AntColony.Units.WorkerAnt).GetMethod("DropCargo",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
   var canStore=typeof(AntColony.Units.WorkerAnt).GetMethod("HasCargoDestination",BindingFlags.Instance|BindingFlags.NonPublic);
   Check(ResourceManager.Instance.Room(AntColony.Data.ResourceType.Component,c)>0,"old global capacity exists");Check(!(bool)canStore.Invoke(c,new object[]{AntColony.Data.ResourceType.Component}),"no physical reachable store despite global capacity");
   UnityEngine.AI.NavMesh.SamplePosition(c.Position+Vector3.right*3,out var hit,4,UnityEngine.AI.NavMesh.AllAreas);
   pile=GameObject.CreatePrimitive(PrimitiveType.Cube);pile.SetActive(false);pile.transform.position=hit.position;var node=pile.AddComponent<AntColony.World.ResourceNode>();node.ConfigureLoot(AntColony.Data.ResourceType.Component,3);pile.SetActive(true);await Task.Yield();
   var id=pile.GetEntityId();c.SetJobEnabled(CommanderJobs.Hauling,true);Time.timeScale=1;
   for(var i=0;i<12;i++){c.CommandGather(node);c.TickDuty(1);await Task.Delay(50);}
   Check(c.CurrentResourceNode!=node,"manual and automatic pickup reject missing store");Check(pile.GetEntityId()==id&&node.AmountRemaining==3,"same pile remains instead of repeated recreation");Check(c.CarriedAmount==0,"no pointless pickup/drop");
   c.SetJobEnabled(CommanderJobs.All,false);c.CommandStop();
   var template=(GameObject)typeof(AntColony.Buildings.BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{AntColony.Data.BuildingKind.Storage,AntColony.Data.UnitRole.Worker});
   UnityEngine.AI.NavMesh.SamplePosition(c.Position+Vector3.forward*4,out hit,4,UnityEngine.AI.NavMesh.AllAreas);warehouse=Object.Instantiate(template,hit.position,Quaternion.identity);warehouse.SetActive(true);await Task.Yield();await Task.Yield();
   var storage=warehouse.GetComponent<AntColony.Buildings.Storage>();storage.Bin.allowed=0;Check(!(bool)canStore.Invoke(c,new object[]{AntColony.Data.ResourceType.Component}),"disallowed storage is not destination");
   storage.Bin.allowed=StoreBin.Bit(AntColony.Data.ResourceType.Component);Check((bool)canStore.Invoke(c,new object[]{AntColony.Data.ResourceType.Component}),"new accepting reachable storage enables hauling");
   c.SetJobEnabled(CommanderJobs.Hauling,true);c.CommandGather(node);Check(c.CurrentResourceNode==node,"manual pickup resumes when store available");
   var end=Time.realtimeSinceStartup+18;while(storage.Bin.Amount(AntColony.Data.ResourceType.Component)<3&&Time.realtimeSinceStartup<end)await Task.Delay(100);
   Check(storage.Bin.Amount(AntColony.Data.ResourceType.Component)==3,"three items actually delivered once");Check(c.CarriedAmount==0,"cargo cleared after deposit");
   var result="PASS "+n+" no-store pickup/drop loop and actual storage delivery checks";System.IO.File.WriteAllText("Temp/loot-loop-result.txt",result);return result;
  }catch(Exception e){System.IO.File.WriteAllText("Temp/loot-loop-result.txt",e.ToString());throw;}
  finally{Time.timeScale=time;if(pile!=null)Object.Destroy(pile);if(warehouse!=null)Object.Destroy(warehouse);AntColony.Save.SaveStorage.RootOverride=root;UserSettings.Apply(settings,false);}
 }
}
