using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using R=AntColony.Data.ResourceType;
using Object=UnityEngine.Object;
public static class ResearchCraft1026Checks
{
 static int checks;static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL "+checks+" "+label);checks++;}
 static T Build<T>(AntColony.Data.BuildingKind kind,Vector3 position) where T:BuildingBase
 {
  var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{kind,AntColony.Data.UnitRole.Worker});
  var go=Object.Instantiate(template,position,Quaternion.identity);go.SetActive(true);return go.GetComponent<T>();
 }
 public static async Task<string> Main()
 {
  checks=0;ScienceLab lab=null;Workshop shop=null;Time.timeScale=0;
  try
  {
   var day=Mathf.Floor(GameCalendar.GameSeconds/GameCalendar.SecondsPerDay)*GameCalendar.SecondsPerDay;
   if(GameCalendar.TimeOfDay>20)day+=GameCalendar.SecondsPerDay;
   GameSession.Instance.MarkStarted(gameSeconds:day+20);
   foreach(var x in AntColony.Core.CommanderRoster.Instance.Commanders){x.CommandStop();x.SetJobEnabled(CommanderJobs.All,false);x.Traits.values.Clear();x.PersonalState.sleep=new CommanderSleepState();x.PersonalState.meal=new CommanderMealState();x.PersonalState.hygiene=new CommanderHygieneState();x.PersonalState.joy=new CommanderJoyState();}
   var team=AntColony.Core.CommanderRoster.Instance.Commanders;var c=team[0];var other=team[1];
   lab=Build<ScienceLab>(AntColony.Data.BuildingKind.ScienceLab,c.Position+Vector3.left*3);
   var research=CampaignResearch.Instance;var state=research.CaptureState();state.active=(int)ScienceTechnology.Resin;state.progress=0;state.paused=false;research.RestoreState(state);
   Check(!c.SendToResearch(lab),"research job zero rejects direct assignment");c.SetJobEnabled(CommanderJobs.Research,true);await Task.Yield();
   Check(c.SendToResearch(lab)&&lab.Target==c,"player chooses fixed researcher");
   c.PersonalState.meal.satiety=0;c.TickDuty(.1f);Check(c.IsEating&&lab.Target==c&&c.ScienceAssignment==lab,"real meal interruption keeps research assignment");
   var progress=research.Progress;research.Tick(1);Check(research.Progress==progress,"eating does not simultaneously research");
   c.PersonalState.meal=new CommanderMealState();GameSession.Instance.MarkStarted(gameSeconds:day+110);c.TickDuty(.1f);Check(c.IsAsleep&&lab.Target==c,"night sleep preserves fixed researcher");research.Tick(1);Check(research.Progress==progress,"sleeping researcher contributes no work");
   GameSession.Instance.MarkStarted(gameSeconds:day+170);c.TickDuty(.1f);Check(!c.IsAsleep&&lab.Target==c,"researcher returns after waking");
   c.PersonalState.hygiene.washSeconds=2;research.Tick(1);Check(research.Progress==progress,"washing contributes no research");c.PersonalState.hygiene.washSeconds=0;
   c.PersonalState.joy.playSeconds=2;c.PersonalState.joy.freeWalk=true;research.Tick(1);Check(research.Progress==progress,"recreation contributes no research");c.PersonalState.joy=new CommanderJoyState();
   c.TickDuty(.1f);research.Tick(1);Check(research.Progress>progress,"fixed research resumes after needs");c.CommandStop();Check(lab.Target==null,"explicit stop releases assignment");
   state=research.CaptureState();state.completed.Add((int)ScienceTechnology.Blades);research.RestoreState(state);
   shop=Build<Workshop>(AntColony.Data.BuildingKind.Workshop,c.Position+Vector3.right*2);Check(shop.TryEnqueue(EquipmentRecipe.Mandible),"equipment order waits without warehouse materials");
   Check(!shop.InputsReady,"craft cannot start before physical delivery");shop.Deliver(new object(),R.Soil,40);shop.Deliver(new object(),R.Special,5);Check(shop.InputsReady,"delivered crafting ingredients ready");
   c.SetJobEnabled(CommanderJobs.Crafting,true);other.SetJobEnabled(CommanderJobs.Crafting,true);Check(shop.TryAssign(c),"maker assigned");shop.Tick(.5f);shop.Release();
   Check(!shop.TryAssign(other)&&shop.Jobs.Count==1,"unfinished equipment retains original maker");Check(shop.TryAssign(c),"original maker can resume");
   var inventory=EquipmentInventory.Instance.Items.Count;shop.Tick(10000);Check(shop.Jobs.Count==0&&EquipmentInventory.Instance.Items.Count==inventory,"crafted equipment not teleported into inventory");
   var loot=SceneObjects.All<EquipmentLoot>().FirstOrDefault(l=>l.Items.Any(i=>i.weapon==WeaponKind.Mandible));Check(loot!=null&&Workshop.Stock(EquipmentRecipe.Mandible)>=1,"floor equipment counted toward stock goal");
   shop.AddOrder(EquipmentRecipe.Mandible,Workshop.OrderMode.KeepStock,1);shop.Refill();Check(shop.Jobs.Count==0,"floor output prevents surplus craft");
   Check(!loot.TryCollect(c),"job zero blocks direct floor equipment haul");c.SetJobEnabled(CommanderJobs.Hauling,true);Check(loot.TryCollect(c),"hauling allows physical equipment pickup");
   var result="PASS "+checks+" fixed research/needs, original maker and floor equipment checks";System.IO.File.WriteAllText("Temp/research-craft1026-result.txt",result);return result;
  }
  catch(Exception e){System.IO.File.WriteAllText("Temp/research-craft1026-result.txt",e.ToString());throw;}
  finally{Time.timeScale=0;if(lab!=null)Object.Destroy(lab.gameObject);if(shop!=null)Object.Destroy(shop.gameObject);}
 }
}
