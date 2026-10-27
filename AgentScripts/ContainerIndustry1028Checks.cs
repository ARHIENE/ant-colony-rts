using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using AntColony.Save;
using UnityEngine;
using UnityEngine.AI;
using R=AntColony.Data.ResourceType;
using B=AntColony.Data.BuildingKind;
using Object=UnityEngine.Object;
public static class ContainerIndustry1028Checks
{
 static int count;
 static void Check(bool ok,string label){if(!ok)throw new Exception("Container FAIL "+count+" "+label);count++;File.WriteAllText("Temp/container-industry-progress.txt",count+" "+label);}
 static async Task Wait(Func<bool> ready,int seconds=90){var end=DateTime.UtcNow.AddSeconds(seconds);while(!ready()&&DateTime.UtcNow<end)await Task.Delay(70);}
 static T Put<T>(B kind,Vector3 at)where T:BuildingBase{var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{kind,AntColony.Data.UnitRole.Worker});var go=Object.Instantiate(template,at,Quaternion.identity);go.name=kind.ToString();go.SetActive(true);return go.GetComponent<T>();}
 static float Floor(R type)=>ResourceNode.Available.Where(n=>n.IsLooseCargo&&n.ResourceType==type).Sum(n=>n.AmountRemaining);
 static void ConsumeFloor(R type){var node=ResourceNode.Available.First(n=>n.IsLooseCargo&&n.ResourceType==type&&n.AmountRemaining>=1);Check(node.TryConsumeStock(1),"actual floor item transferred for unloading");}
 static void Park(CommanderAnt c,SupplyFacility station){c.CommandStop();Check(c.StartService(station,CommanderJobs.Crafting),"onsite container worker assigned");c.Agent.Warp(c.Agent.destination);c.Agent.ResetPath();}
 static void Finish(CommanderAnt c,SupplyFacility station){Time.timeScale=1;Check(station.WorkContainers(c,SupplyFacility.ProcessSeconds/c.WorkRate(CommanderActivity.Crafting)+.01f),"container batch performed");Time.timeScale=0;}
 public static async Task<string> Main()
 {
  count=0;Time.timeScale=0;var root=SaveStorage.RootOverride;var old=UserSettings.Current.Clone();SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"Containers1028-"+Guid.NewGuid().ToString("N"));
  try
  {
   var config=old.Clone();config.autoSaveEnabled=false;config.autoPauseThreat=config.autoPauseDowned=config.autoPauseRocket=false;UserSettings.Apply(config,false);await Wait(()=>!SaveSystem.Busy);
   StartingCandidate[] team;do{team=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray();}while(StartingCandidate.Missing(team).Length>0);foreach(var t in team){t.ageYears=20;t.traits.values.Clear();}
   SaveSystem.NewGame(new NewGameOptions{seed=281028,mapSize=MapSize.Small,startingCount=3,startingTeam=team});await Wait(()=>!SaveSystem.Busy&&CommanderRoster.Instance.Commanders.Count==3);AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;GameSession.Instance.MarkStarted(gameSeconds:20);
   foreach(var p in CommanderRoster.Instance.Commanders){p.CommandStop();p.SetJobEnabled(CommanderJobs.All,false);p.PersonalState.sleep=new CommanderSleepState();p.PersonalState.meal=new CommanderMealState();p.PersonalState.hygiene=new CommanderHygieneState();p.PersonalState.joy=new CommanderJoyState();}
   var c=CommanderRoster.Instance.Commanders.First();var id=c.PersonalState.id;Check(NavMesh.SamplePosition(c.Position+Vector3.right*8,out var hit,5,NavMesh.AllAreas),"reachable filling station");var at=new Vector3(Mathf.Floor(hit.position.x)+.5f,hit.position.y,Mathf.Floor(hit.position.z)+.5f);var station=Put<SupplyFacility>(B.FuelCharger,at);var liquid=Put<SupplyFacility>(B.FluidTank,at+Vector3.right);liquid.Fill(R.CrudeOil,40);var gas=Put<SupplyFacility>(B.GasTank,at+Vector3.forward);gas.Fill(R.NaturalGas,40);
   var store=Put<Storage>(B.Storage,c.Position+Vector3.left*3);ResourceManager.Instance.AddTo(store.Bin,R.Drum,1);station.SetContainerMode(1);station.AddContainerOrder(1);WorkPriorities.Set(station,9,false);
   Check(!station.NeedsContainerWorker&&station.ContainerNeedsIngredients,"order waits for physical container");Check(!c.StartService(station,CommanderJobs.Crafting),"job zero cannot start packing");c.SetJobEnabled(CommanderJobs.Crafting,true);Check(c.CommandFetchFor(station)&&c.DeliverySite==station,"existing delivery route accepts station");
   Check(SaveSystem.TrySave(false,0,out var error),"container delivery order save: "+error);Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"delivery load accepted: "+error);await Wait(()=>!SaveSystem.Busy);Time.timeScale=0;station=SupplyFacility.All.First(s=>s.Kind==B.FuelCharger);c=CommanderRoster.Instance.Commanders.First(p=>p.PersonalState.id==id);liquid=SupplyFacility.All.First(s=>s.Kind==B.FluidTank);gas=SupplyFacility.All.First(s=>s.Kind==B.GasTank);
   Check(c.DeliverySite==station&&station.Supply.containerOrders==1,"delivery and order restored");AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=1;await Wait(()=>station.Supply.containerOrders==0,30);Time.timeScale=0;
   Check(station.Supply.containerOrders==0&&Floor(R.FilledCrudeDrum)==1,"actual autonomous fetch walk pack and floor output");Check(liquid.Supply.amount==30&&ResourceManager.Instance.GetAmount(R.Drum)==0,"one physical drum and ten crude consumed");c.CommandStop();
   foreach(var mode in new[]{0,1,2})
   {
    station.RestoreSupply(new SupplyFacility.State());Check(station.SetContainerMode(mode),"select packing mode "+mode);var fluid=station.ContainerFluid;var tank=mode==2?gas:liquid;tank.RestoreSupply(new SupplyFacility.State{fluid=(int)fluid,amount=20});Check(station.AddContainerOrder(1),"packing order "+mode);Check(station.Deliver(new object(),station.ContainerInput,1)==1,"physical input staged "+mode);var filled=station.FilledContainer;var before=Floor(filled);Park(c,station);Finish(c,station);Check(tank.Supply.amount==10&&Floor(filled)==before+1&&station.Supply.containerOrders==0,"exact packed amount and one output "+mode);c.CommandStop();
    ConsumeFloor(filled);Check(station.SetContainerMode(mode+3),"select unloading "+mode);Check(station.AddContainerOrder(1)&&station.Deliver(new object(),filled,1)==1,"filled cargo staged for unpacking "+mode);var empty=station.EmptyContainer;var empties=Floor(empty);Park(c,station);Finish(c,station);Check(tank.Supply.amount==20&&Floor(empty)==empties+1,"fluid returned to correct network and empty container recovered "+mode);c.CommandStop();
   }
   Check(!liquid.Fill(R.NaturalGas,1)&&!gas.Fill(R.CrudeOil,1),"container ports retain gas liquid separation");
   station.RestoreSupply(new SupplyFacility.State());station.SetContainerMode(2);station.AddContainerOrder(1);station.Deliver(new object(),R.GasCylinder,1);Park(c,station);Time.timeScale=1;station.WorkContainers(c,.25f);Time.timeScale=0;var progress=station.Supply.work;var fuel=gas.Supply.amount;Check(progress>0&&fuel==20,"partial work consumes no fluid yet");station.PauseContainers(true);Time.timeScale=1;Check(!station.WorkContainers(c,20)&&station.Supply.work==progress,"pause keeps partial work");Time.timeScale=0;
   Check(SaveSystem.TrySave(false,1,out error),"paused staged container save: "+error);Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,1),out error),"paused container load: "+error);await Wait(()=>!SaveSystem.Busy);Time.timeScale=0;station=SupplyFacility.All.First(s=>s.Kind==B.FuelCharger);c=CommanderRoster.Instance.Commanders.First(p=>p.PersonalState.id==id);gas=SupplyFacility.All.First(s=>s.Kind==B.GasTank);
   Check(station.Supply.containerPaused&&Mathf.Abs(station.Supply.work-progress)<.01f&&station.Supply.containerStock.Single().amount==1,"pause progress and delivered cylinder restored");var returned=Floor(R.GasCylinder);station.CancelContainerWork();Check(Floor(R.GasCylinder)==returned+1&&gas.Supply.amount==fuel,"cancel refunds delivered cylinder and preserves gas");station.CancelContainerWork();Check(Floor(R.GasCylinder)==returned+1,"cancel cannot duplicate refund");
   Check(!SupplyFacility.ValidForKind(B.FuelCharger,new SupplyFacility.State{containerMode=6})&&!SupplyFacility.ValidForKind(B.FluidTank,new SupplyFacility.State{containerOrders=1}),"invalid and wrong kind container saves rejected");Check(!SupplyFacility.ValidForKind(B.FuelCharger,new SupplyFacility.State{containerStock=new System.Collections.Generic.List<ProductionInputs.Stock>{new ProductionInputs.Stock{type=R.FilledGasCylinder,amount=50}}}),"wrong delivered input rejected");
   Check(Resources.Load<GameObject>("Kemono/Props/"+ProductionProp.ForResource(R.FilledCrudeDrum))!=null&&Resources.Load<GameObject>("Kemono/Props/"+ProductionProp.ForResource(R.FilledGasCylinder))!=null,"filled containers use existing real models");
   AntColony.UI.GameMenuController.Instance.ShowContainers(station);var menu=AntColony.UI.GameMenuController.Instance;var text=menu.GetComponentsInChildren<UnityEngine.UI.Text>().Select(t=>t.text).ToArray();Check(text.Any(t=>t.Contains("가스 배출"))&&text.Any(t=>t.Contains("원유 충전")),"all modes visible in menu");var add=menu.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.GetComponentInChildren<UnityEngine.UI.Text>()?.text=="1개 지시 추가");add.onClick.Invoke();Check(station.Supply.containerOrders==1,"menu order button actually changes count");Check(AntColony.UI.GameMenuController.TaskCandidates().Contains(station),"container request joins work management");menu.Resume();Time.timeScale=0;
   Check(SaveSystem.TrySave(false,2,out error),"final container state valid: "+error);var result="PASS "+count+" container real delivery, 3 fluid round trips, reuse, pause/cancel, save and UI checks";File.WriteAllText("Temp/container-industry-result.txt",result);return result;
  }
  catch(Exception e){File.WriteAllText("Temp/container-industry-result.txt",e.ToString());throw;}
  finally{Time.timeScale=0;SaveStorage.RootOverride=root;UserSettings.Apply(old,false);}
 }
}
