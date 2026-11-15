using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;
using AntColony.Buildings;using AntColony.Core;using AntColony.Data;using AntColony.Save;using AntColony.UI;using AntColony.Units;using UnityEngine;
public static class LateIndustry1115Checks
{
 const string Log="work/colony-polish-1115/late-industry-checks.txt";
 static void Check(bool ok,string message){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new Exception(message);}
 static async Task Wait(Func<bool> done,int seconds=90){var until=DateTime.UtcNow.AddSeconds(seconds);while(!done()&&DateTime.UtcNow<until)await Task.Delay(100);Check(done(),"Wait completed");}
 static GameObject Build(BuildingKind k,Vector3 p)
 {var t=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{k,UnitRole.Worker});var g=UnityEngine.Object.Instantiate(t,p,Quaternion.identity);g.name="LateIndustry"+k;g.SetActive(true);return g;}
 public static async Task<string> Main()
 {
  var mine=SceneObjects.All<Processor>().Single(p=>p.Data.kind==BuildingKind.DeepMine);
  File.WriteAllText(Log,"Injected unlocked research/coal supply; actual power grid, worker travel and production.\n");
  var root=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"LateIndustry1115-"+Guid.NewGuid().ToString("N"));
  try
  {
   var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false;UserSettings.Apply(quiet,false);
   var c=CommanderRoster.Instance.Commanders.First();c.CommandStop();c.SetJobEnabled(CommanderJobs.Gathering,true);
   var campaign=CampaignResearch.Instance;campaign.RecordSampleDelivery(ResourceType.TitaniumOre,1);campaign.TryCompleteResourceResearch(ResourceType.TitaniumOre);
   Check(mine.AddOrder(0,Workshop.OrderMode.Count,1),"Researched titanium production available");
   Check(!mine.AddOrder(1,Workshop.OrderMode.Count,1),"Stellar extraction remains locked");
   Check(!mine.NeedsWork,"Unpowered deep mine waits");
   var generator=Build(BuildingKind.SteamGenerator,mine.Position+Vector3.right*3).GetComponent<PowerNode>();ResourceManager.Instance.Add(ResourceType.Coal,200);
   var initial=ProductionStock.Count(ResourceType.TitaniumOre);Check(c.StartService(mine,CommanderJobs.Gathering),"Assign actual miner");SimulationPause.UserSpeed(3);
   await Wait(()=>mine.Current!=null && mine.Progress>.03f);SimulationPause.UserSpeed(0);
   Check(mine.Powered&&generator.FuelSeconds>0,"Solid fuel generator supplies real grid");
   var progress=mine.Progress;generator.gameObject.SetActive(false);PowerGrid.Tick(.5f);
   Check(!mine.Powered&&!mine.Work(c,30)&&Mathf.Approximately(progress,mine.Progress),"Power loss preserves unpaid work and prevents output");
   generator.gameObject.SetActive(true);SimulationPause.UserSpeed(3);
   await Wait(()=>ProductionStock.Count(ResourceType.TitaniumOre)>=initial+3,180);SimulationPause.UserSpeed(0);
   Check(mine.Current==null&&mine.Orders.Count==0,"One titanium batch completed after power restoration");
   Check(mine.GetComponent<FacilityVisual>()?.Model!=null,"Deep mining model visible in live scene");
   var state=campaign.CaptureState();state.completed.Add((int)ScienceTechnology.Vehicle);state.completed.Add((int)ScienceTechnology.SignalSurvey);campaign.RestoreState(state);
   campaign.ToggleSignalSearch();Check(campaign.HasResearchTask,"Radio search creates assignable research task");
   c.CommandStop();c.SetJobEnabled(CommanderJobs.Research,true);var lab=Build(BuildingKind.ScienceLab,c.Position+Vector3.forward*4).GetComponent<ScienceLab>();
   Check(c.SendToResearch(lab),"Researcher can take radio assignment without active science topic");
   var known=campaign.CaptureState().knownRegions.Count;SimulationPause.UserSpeed(5);
   await Wait(()=>!campaign.SearchingSignals,240);SimulationPause.UserSpeed(0);
   Check(campaign.CaptureState().knownRegions.Count==known+2,"Real researcher receives two distant coordinates");
   Check(SaveSystem.TrySave(false,0,out var error),"Late production and discoveries save: "+error);
   return File.ReadAllText(Log);
  }
  finally{SimulationPause.UserSpeed(0);UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;}
 }
 public static async Task<string> ResumeSignals()
 {
  var root=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"Signal1115-"+Guid.NewGuid().ToString("N"));
  try {
   var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false;UserSettings.Apply(quiet,false);
   var campaign=CampaignResearch.Instance;var known=campaign.CaptureState().knownRegions.Count;
   Check(campaign.SearchingSignals,"Continue interrupted verification with preserved radio work");
   SimulationPause.UserSpeed(5);await Wait(()=>!campaign.SearchingSignals,240);SimulationPause.UserSpeed(0);
   Check(campaign.CaptureState().knownRegions.Count==known+2,"Real researcher receives two distant coordinates");
   Check(SaveSystem.TrySave(false,0,out var error),"Late production and discoveries save: "+error);
   return File.ReadAllText(Log);
  } finally {SimulationPause.UserSpeed(0);UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;}
 }
}
