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
using UnityEngine.AI;
using R=AntColony.Data.ResourceType;
using B=AntColony.Data.BuildingKind;
using Object=UnityEngine.Object;
public static class UndergroundProduction1027Checks
{
 static int checks;
 static void Check(bool ok,string text){if(!ok)throw new Exception("Underground FAIL "+checks+" "+text);checks++;File.WriteAllText("Temp/underground-production1027-progress.txt",checks+" "+text);}
 static void Near(float a,float b,string text)=>Check(Mathf.Abs(a-b)<.02f,text+" "+a+" / "+b);
 static async Task Wait(Func<bool> condition,int seconds=90){var deadline=DateTime.UtcNow.AddSeconds(seconds);while(!condition()&&DateTime.UtcNow<deadline)await Task.Delay(60);}
 static T Put<T>(B kind,Vector3 at) where T:BuildingBase
 {var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{kind,AntColony.Data.UnitRole.Worker});var go=Object.Instantiate(template,at,Quaternion.identity);go.name=kind.ToString();go.SetActive(true);return go.GetComponent<T>();}
 static float Floor(R type)=>ResourceNode.Available.Where(n=>n.IsLooseCargo&&n.ResourceType==type).Sum(n=>n.AmountRemaining);
 static void ClearFloor(SupplyFacility drill){foreach(var n in ResourceNode.Available.Where(n=>n.IsLooseCargo&&(n.transform.position-drill.Position).sqrMagnitude<=16).ToArray()){n.gameObject.SetActive(false);Object.Destroy(n.gameObject);}}
 static void Complete(R type){CampaignResearch.Instance.RecordSampleDelivery(type,1);CampaignResearch.Instance.TryCompleteResourceResearch(type);}
 static void At(CommanderAnt c,SupplyFacility drill){c.CommandStop();c.SetJobEnabled(CommanderJobs.Gathering,true);Check(c.StartService(drill,CommanderJobs.Gathering),"direct drill assignment");c.Agent.Warp(c.Agent.destination);c.Agent.ResetPath();}
 public static async Task<string> Main()
 {
  checks=0;var settings=UserSettings.Current.Clone();var root=SaveStorage.RootOverride;Time.timeScale=0;
  SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"Underground1027-"+Guid.NewGuid().ToString("N"));
  try
  {
   var config=settings.Clone();config.autoSaveEnabled=false;config.autoPauseThreat=config.autoPauseDowned=config.autoPauseRocket=false;UserSettings.Apply(config,false);
   StartingCandidate[] team;do{team=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray();}while(StartingCandidate.Missing(team).Length>0);
   await Wait(()=>!SaveSystem.Busy);
   SaveSystem.NewGame(new NewGameOptions{seed=261027,mapSize=MapSize.Small,startingCount=3,startingTeam=team});await Wait(()=>!SaveSystem.Busy&&CommanderRoster.Instance.Commanders.Count>=3);Time.timeScale=0;Check(!SaveSystem.Busy&&CommanderRoster.Instance.Commanders.Count>=3,"new game ready");
   AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;GameSession.Instance.MarkStarted(gameSeconds:20);
   foreach(var p in CommanderRoster.Instance.Commanders){p.CommandStop();p.SetJobEnabled(CommanderJobs.All,false);p.Traits.values.Clear();p.PersonalState.sleep=new CommanderSleepState();p.PersonalState.meal=new CommanderMealState();p.PersonalState.hygiene=new CommanderHygieneState();p.PersonalState.joy=new CommanderJoyState();}
   var actor=CommanderRoster.Instance.Commanders.First();var home=actor.Position;var storage=Put<Storage>(B.Storage,home+Vector3.left*3);
   Check(NavMesh.SamplePosition(home+Vector3.right*10,out var ground,8,NavMesh.AllAreas),"reachable drill point");
   var survey=UndergroundSurvey.Instance;var state=survey.Capture();var layers=state.layers.Where(l=>l.map==0&&l.type>=0).Take(4).ToArray();var types=new[]{R.CopperOre,R.CrudeOil,R.NaturalGas,R.StellarOre};
   for(int i=0;i<4;i++){layers[i].type=(int)types[i];layers[i].total=UndergroundSurvey.Amount(types[i]);layers[i].remaining=layers[i].total;layers[i].position=new Vector3(Mathf.Floor(ground.position.x)+.5f,ground.position.y,Mathf.Floor(ground.position.z)+.5f)+Vector3.forward*20*i;layers[i].work=300;layers[i].discovered=true;layers[i].discoveryPending=false;}
   survey.Restore(state);await Task.Yield();Time.timeScale=0;layers=survey.Layers.Where(l=>l.map==0&&l.discovered&&l.type>=0).Take(4).ToArray();
   var research=CampaignResearch.Instance;var rs=research.CaptureState();rs.completed.Add((int)ScienceTechnology.UndergroundSurvey);research.RestoreState(rs);
   Check(UndergroundProduction.PlacementReason(B.DeepDrill,home+Vector3.left*80)!=null,"outside deposit placement refused");
   Check(UndergroundProduction.PlacementReason(B.GasExtractor,layers[1].position)!=null,"wrong deposit placement refused");
   var drill=Put<SupplyFacility>(B.DeepDrill,layers[0].position);drill.Powered=true;
   Check(UndergroundProduction.PlacementReason(B.DeepDrill,drill.Position)!=null,"second drill blocked");
   Check(UndergroundProduction.PlacementReason(B.DeepDrill,drill.Position,drill)==null,"own binding valid");
   Check(drill.Demand==100&&FacilityVisual.ModelFor(B.DeepDrill)!=null&&Resources.Load<GameObject>("Facilities/"+FacilityVisual.ModelFor(B.GasExtractor))!=null,"power and real facility model connected");
   Check(!actor.StartService(drill,CommanderJobs.Gathering),"job zero prevents direct drill work");
   actor.SetJobEnabled(CommanderJobs.Gathering,true);actor.TickDuty(2);Check(actor.ServiceTarget==drill,"automatic work chooses built drill; ready="+actor.CivilianWorkReady+" powered="+drill.Powered+" batch="+UndergroundProduction.Batch(drill)+" path="+actor.Agent.hasPath+" service="+actor.ServiceJob);
   actor.Agent.Warp(actor.Agent.destination);actor.Agent.ResetPath();Time.timeScale=1;
   var floor=Floor(R.CopperOre);Check(drill.WorkDrill(actor,100),"worker extracts research sample");Near(Floor(R.CopperOre)-floor,1,"only one whole sample on floor; rate="+actor.WorkRate(CommanderActivity.Gathering)+" work="+drill.Supply.work);Near(layers[0].remaining,399,"sample deducted from shared layer");
   Check(!drill.WorkDrill(actor,20)&&!research.HasSample(R.CopperOre),"pending floor sample stops more production");
   actor.CommandStop();actor.SetJobEnabled(CommanderJobs.Gathering,false);actor.SetJobEnabled(CommanderJobs.Hauling,true);
   var sample=ResourceNode.Available.First(n=>n.IsLooseCargo&&n.ResourceType==R.CopperOre);actor.CommandGather(sample);await Wait(()=>research.HasSample(R.CopperOre),20);Time.timeScale=0;
   Check(research.HasSample(R.CopperOre),"sample physically hauled home for research; cargo="+actor.CarriedAmount+" working="+actor.IsWorking+" moving="+actor.Agent.hasPath+" position="+actor.Position+" stock="+ResourceManager.Instance.GetAmount(R.CopperOre));Check(research.TryCompleteResourceResearch(R.CopperOre),"sample unlocks resource study");actor.CommandStop();actor.SetJobEnabled(CommanderJobs.Hauling,false);
   drill.Powered=true;At(actor,drill);var before=layers[0].remaining;Check(!drill.WorkDrill(actor,20),"paused work does not advance");Near(layers[0].remaining,before,"pause preserves stock");
   Time.timeScale=1;drill.Powered=false;Check(!drill.WorkDrill(actor,20),"outage does not work");drill.Powered=true;actor.PersonalState.meal.eatSeconds=2;Check(!drill.WorkDrill(actor,20),"eating does not work");actor.PersonalState.meal.eatSeconds=0;
   actor.SetJobEnabled(CommanderJobs.Gathering,false);Check(!drill.WorkDrill(actor,20),"job zero stops active drilling");actor.SetJobEnabled(CommanderJobs.Gathering,true);
   floor=Floor(R.CopperOre);Check(drill.WorkDrill(actor,10000),"researched ore production");Near(Floor(R.CopperOre)-floor,100,"bounded floor output full");Near(before-layers[0].remaining,100,"one unit output per shared unit removed");before=layers[0].remaining;Check(!drill.WorkDrill(actor,20),"output saturation pauses");Near(layers[0].remaining,before,"full output discards no resource");
   ClearFloor(drill);Check(drill.WorkDrill(actor,10),"removed output automatically permits work");actor.CommandStop();
   var high=Put<SupplyFacility>(B.DeepDrill,layers[3].position);high.Powered=true;Complete(R.StellarOre);Check(UndergroundProduction.Batch(high)==0&&UndergroundProduction.Status(high).Contains("등급"),"steel machine cannot bypass titanium alloy requirement");
   var gas=Put<SupplyFacility>(B.GasExtractor,layers[2].position);var gp=Put<SupplyFacility>(B.GasPipe,gas.Position+Vector3.right);var gt=Put<SupplyFacility>(B.GasTank,gas.Position+Vector3.right*2);var lt=Put<SupplyFacility>(B.FluidTank,gas.Position+Vector3.left);
   gas.Powered=true;gas.Tick(5);Check(gt.Supply.amount==1&&gas.Supply.amount==0&&research.HasSample(R.NaturalGas),"gas sample enters actual gas tank and registers at home");gas.Tick(5);Near(layers[2].remaining,599,"gas does not repeat sample before research");Check(research.TryCompleteResourceResearch(R.NaturalGas),"gas sample research ready");
   Check(!lt.Fill(R.NaturalGas,1)&&!gt.Fill(R.CrudeOil,1)&&!drill.Fill(R.CrudeOil,1),"liquid/gas/solid ports reject mismatched phase");
   gas.Tick(5);Check(gt.Supply.amount==11&&lt.Supply.amount==0,"gas flows only through gas network");var gasBefore=layers[2].remaining;gas.Powered=false;gas.Tick(20);Near(layers[2].remaining,gasBefore,"gas power loss preserves reserve");gas.Powered=true;
   Time.timeScale=0;gas.Tick(20);Near(layers[2].remaining,gasBefore,"paused gas extractor does not work");Time.timeScale=1;gas.Tick(10000);Check(gt.Supply.amount==91&&gas.Supply.amount==100,"tank and extractor output buffers filled without mixing");gasBefore=layers[2].remaining;gas.Tick(100);Near(layers[2].remaining,gasBefore,"full gas output pauses without deletion");
   gp.gameObject.SetActive(false);gt.RestoreSupply(new SupplyFacility.State());gas.Tick(5);Check(gt.Supply.amount==0&&lt.Supply.amount==0,"disconnected gas tank and adjacent liquid tank cannot receive");gp.gameObject.SetActive(true);gas.Tick(5);Check(gt.Supply.amount==10,"pipe reconnection resumes gas extraction");
   var oil=Put<SupplyFacility>(B.OilDrill,layers[1].position);var op=Put<SupplyFacility>(B.FluidPipe,oil.Position+Vector3.right);var ot=Put<SupplyFacility>(B.FluidTank,oil.Position+Vector3.right*2);oil.Powered=true;oil.Tick(5);
   Check(ot.Supply.amount==1&&research.HasSample(R.CrudeOil),"oil research sample delivered through liquid pipe");Check(research.TryCompleteResourceResearch(R.CrudeOil),"oil resource study");oil.Tick(5);Check(ot.Supply.amount==11,"oil comes from finite underground layer");Near(layers[1].remaining,589,"oil reserve decrements actual output");
   var refinery=Put<SupplyFacility>(B.OilRefinery,ot.Position+Vector3.right);refinery.Powered=true;refinery.Tick(5);Check(ot.Supply.amount==11&&refinery.Supply.amount==0,"refinery cannot run without worker");
   actor.CommandStop();actor.SetJobEnabled(CommanderJobs.Crafting,true);Check(actor.StartService(refinery,CommanderJobs.Crafting),"refinery crafting assignment");actor.Agent.Warp(actor.Agent.destination);actor.Agent.ResetPath();refinery.WorkRefinery(actor,SupplyFacility.ProcessSeconds/actor.WorkRate(CommanderActivity.Crafting)+.001f);actor.CommandStop();actor.SetJobEnabled(CommanderJobs.Crafting,false);
   Check(ot.Supply.amount==10&&refinery.Supply.amount==1&&refinery.Supply.fluid==(int)R.Petroleum,"worker refinery consumes real piped crude");
   var beforeRebuild=layers[1].remaining;oil.gameObject.SetActive(false);Object.Destroy(oil.gameObject);oil=Put<SupplyFacility>(B.OilDrill,layers[1].position);oil.Powered=true;oil.Tick(5);Near(layers[1].remaining,beforeRebuild-10,"rebuild shares original reserve without reset");
   var surface=Put<SupplyFacility>(B.OilDeposit,home+Vector3.left*80);var old=Put<SupplyFacility>(B.OilDrill,surface.Position+Vector3.right);old.Powered=true;old.Tick(5);Check(surface.Supply.depositRemaining==590&&old.Supply.amount==10,"existing surface oil source remains usable and finite");
   surface.Supply.depositRemaining=10;old.Tick(5);old.Tick(50);Check(surface.Supply.depositRemaining==0&&old.Supply.amount==20,"surface oil stops at exhaustion");
   Check(!SupplyFacility.ValidForKind(B.FluidTank,new SupplyFacility.State{fluid=(int)R.NaturalGas,amount=1})&&!SupplyFacility.ValidForKind(B.GasTank,new SupplyFacility.State{fluid=(int)R.CrudeOil,amount=1}),"save rejects phase mismatch");
   Time.timeScale=0;var reserve=layers[2].remaining;var gasAt=gas.Position;var copperAt=drill.Position;gas.Supply.work=2.5f;var gasAmount=gt.Supply.amount;
   Check(SaveSystem.TrySave(false,0,out var error),"new production save valid: "+error);Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"production save load accepted: "+error);await Wait(()=>!SaveSystem.Busy);Time.timeScale=0;Check(!SaveSystem.Busy,"production restore completed");
   survey=UndergroundSurvey.Instance;Near(survey.Layers.First(l=>l.type==(int)R.NaturalGas&&l.map==0&&l.discovered).remaining,reserve,"finite shared gas reserve survives real scene reload");
   gas=SupplyFacility.All.First(s=>s.Kind==B.GasExtractor&&(s.Position-gasAt).sqrMagnitude<1);Near(gas.Supply.work,2.5f,"partial cycle restored");Check(SupplyFacility.All.Any(s=>s.Kind==B.GasTank&&s.Supply.amount==gasAmount),"stored gas restored");Check(SupplyFacility.All.Any(s=>s.Kind==B.OilDeposit&&s.Supply.depositRemaining==0),"exhausted surface oil not regenerated after load");
   Check(UndergroundProduction.PlacementReason(B.DeepDrill,copperAt)!=null,"restored extractor still owns deposit");
   AntColony.UI.GameMenuController.Instance.ShowSurvey();Check(AntColony.UI.GameMenuController.Instance.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("채취 시설 배치")),"discovery menu offers actual production placement");AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;
   var result="PASS "+checks+" underground production, physical hauling, finite reserve, phase separation and real save/load checks";File.WriteAllText("Temp/underground-production1027-result.txt",result);return result;
  }
  catch(Exception e){File.WriteAllText("Temp/underground-production1027-result.txt",e.ToString());throw;}
  finally{Time.timeScale=0;SaveStorage.RootOverride=root;UserSettings.Apply(settings,false);}
 }
}
