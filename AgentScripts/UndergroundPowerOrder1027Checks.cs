using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using AntColony.Save;
using UnityEngine;
using B=AntColony.Data.BuildingKind;
public static class UndergroundPowerOrder1027Checks
{
 static int checks;
 static void Check(bool yes,string message){if(!yes)throw new Exception("Power/order "+checks+" "+message);checks++;File.WriteAllText("Temp/underground-power1027-progress.txt",checks+" "+message);}
 public static async Task<string> Main()
 {
  checks=0;Time.timeScale=0;var root=SaveStorage.RootOverride;SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"UndergroundPower1027-"+Guid.NewGuid().ToString("N"));
  try
  {
   var drill=SupplyFacility.All.First(s=>s.Kind==B.DeepDrill&&UndergroundProduction.LayerAt(s.Kind,s.Position)?.type==(int)AntColony.Data.ResourceType.CopperOre);
   foreach(var old in PowerNode.All.Where(n=>n.IsBattery&&(n.Position-drill.Position).sqrMagnitude<9).ToArray()){old.gameObject.SetActive(false);UnityEngine.Object.Destroy(old.gameObject);}
   var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{B.Battery,AntColony.Data.UnitRole.Worker});
   var go=UnityEngine.Object.Instantiate(template,drill.Position+Vector3.left,Quaternion.identity);go.name="Battery";go.SetActive(true);var battery=go.GetComponent<PowerNode>();
   var charge=typeof(PowerNode).GetProperty("Charge");charge.SetValue(battery,200f);PowerGrid.MarkDirty();PowerGrid.Tick(.5f);
   Check(drill.Powered&&battery.Charge<200,"real grid powers drill and consumes battery");
   charge.SetValue(battery,0f);PowerGrid.Tick(.5f);Check(!drill.Powered,"actual empty battery cuts power");
   charge.SetValue(battery,GameBalance.BatteryCapacity);PowerGrid.Tick(.5f);Check(drill.Powered,"replenished battery resumes power");
   foreach(var person in CommanderRoster.Instance.Commanders){person.CommandStop();person.SetJobEnabled(CommanderJobs.All,false);person.PersonalState.meal=new CommanderMealState();person.PersonalState.sleep=new CommanderSleepState();person.PersonalState.hygiene=new CommanderHygieneState();person.PersonalState.joy=new CommanderJoyState();}
   var c=CommanderRoster.Instance.Commanders.First();c.SetJobEnabled(CommanderJobs.Gathering,true);
   Check(c.StartService(drill,CommanderJobs.Gathering),"powered drill assigned");c.Agent.Warp(c.Agent.destination);c.Agent.ResetPath();
   drill.Supply.work=2.5f;var position=drill.Position;var id=c.PersonalState.id;
   Check(SaveSystem.TrySave(false,0,out var error),"active drill order save valid: "+error);
   Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"active drill order load accepted: "+error);
   var end=DateTime.UtcNow.AddSeconds(90);while(SaveSystem.Busy&&DateTime.UtcNow<end)await Task.Delay(60);Time.timeScale=0;Check(!SaveSystem.Busy,"active order scene restore complete");
   drill=SupplyFacility.All.First(s=>s.Kind==B.DeepDrill&&(s.Position-position).sqrMagnitude<1);c=CommanderRoster.Instance.Commanders.First(p=>p.PersonalState.id==id);
   Check(c.ServiceTarget==drill&&c.ServiceJob==CommanderJobs.Gathering,"same worker target and job survive reload");Check(Mathf.Abs(drill.Supply.work-2.5f)<.001f,"partial drilling cycle survives reload");
   PowerGrid.Tick(.5f);Check(drill.Powered,"restored battery reconnects automatically");
   AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=1;var deadline=DateTime.UtcNow.AddSeconds(15);
   while(drill.Supply.work<=2.5f&&DateTime.UtcNow<deadline)await Task.Delay(80);Time.timeScale=0;
   Check(drill.Supply.work>2.5f,"restored active work resumes in real frames without reassignment");
   var layer=UndergroundProduction.LayerAt(drill.Kind,drill.Position);var before=layer.remaining;c.CommandStop();
   foreach(var n in ResourceNode.Available.Where(n=>n.IsLooseCargo&&(n.transform.position-drill.Position).sqrMagnitude<16).ToArray()){n.gameObject.SetActive(false);UnityEngine.Object.Destroy(n.gameObject);}
   Time.timeScale=1;c.StartService(drill,CommanderJobs.Gathering);c.Agent.Warp(c.Agent.destination);c.Agent.ResetPath();
   for(var cycle=0;cycle<10&&layer.remaining>=1;cycle++){drill.WorkDrill(c,10000);foreach(var n in ResourceNode.Available.Where(n=>n.IsLooseCargo&&(n.transform.position-drill.Position).sqrMagnitude<16).ToArray()){n.gameObject.SetActive(false);UnityEngine.Object.Destroy(n.gameObject);}}
   Check(before>0&&layer.remaining==0&&layer.exhaustionNotified,"actual machine exhausts finite underground reserve");Check(!drill.WorkDrill(c,10),"exhausted drill cannot produce again");c.CommandStop();Time.timeScale=0;
   drill.gameObject.SetActive(false);Check(UndergroundProduction.PlacementReason(B.DeepDrill,position)!=null,"exhausted deposit cannot be rebuilt for new stock");drill.gameObject.SetActive(true);
   Check(SaveSystem.TrySave(false,1,out error),"exhausted machine and shared reserve remain save-valid: "+error);
   var result="PASS "+checks+" actual power network, active drill save/load, resumed production and machine exhaustion checks";File.WriteAllText("Temp/underground-power1027-result.txt",result);return result;
  }
  catch(Exception e){File.WriteAllText("Temp/underground-power1027-result.txt",e.ToString());throw;}
  finally{Time.timeScale=0;SaveStorage.RootOverride=root;}
 }
}
