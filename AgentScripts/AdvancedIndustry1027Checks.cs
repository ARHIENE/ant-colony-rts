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
public static class AdvancedIndustry1027Checks
{
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception("Industry FAIL "+checks+" "+label);checks++;File.WriteAllText("Temp/advanced-industry1027-progress.txt",checks+" "+label);}
 static void Near(float a,float b,string label)=>Check(Mathf.Abs(a-b)<.01f,label+" "+a+" / "+b);
 static async Task Wait(Func<bool> ready,int seconds=90){var end=DateTime.UtcNow.AddSeconds(seconds);while(!ready()&&DateTime.UtcNow<end)await Task.Delay(70);}
 static T Put<T>(B kind,Vector3 at) where T:BuildingBase{var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{kind,AntColony.Data.UnitRole.Worker});var go=Object.Instantiate(template,at,Quaternion.identity);go.name=kind.ToString();go.SetActive(true);return go.GetComponent<T>();}
 static Processor.State State(Processor p)=>(Processor.State)typeof(Processor).GetMethod("CaptureState",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,null);
 static float Floor(R type)=>ResourceNode.Available.Where(n=>n.IsLooseCargo&&n.ResourceType==type).Sum(n=>n.AmountRemaining);
 static void Research(R type){CampaignResearch.Instance.RecordSampleDelivery(type,1);CampaignResearch.Instance.TryCompleteResourceResearch(type);}
 public static async Task<string> Main()
 {
  checks=0;Time.timeScale=0;var old=UserSettings.Current.Clone();var root=SaveStorage.RootOverride;SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"Industry1027-"+Guid.NewGuid().ToString("N"));
  try
  {
   var settings=old.Clone();settings.autoSaveEnabled=false;settings.autoPauseThreat=settings.autoPauseDowned=settings.autoPauseRocket=false;UserSettings.Apply(settings,false);
   await Wait(()=>!SaveSystem.Busy);StartingCandidate[] team;do{team=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray();}while(StartingCandidate.Missing(team).Length>0);
   SaveSystem.NewGame(new NewGameOptions{seed=271027,mapSize=MapSize.Small,startingCount=3,startingTeam=team});await Wait(()=>!SaveSystem.Busy&&CommanderRoster.Instance.Commanders.Count==3);Time.timeScale=0;Check(!SaveSystem.Busy&&CommanderRoster.Instance.Commanders.Count==3,"isolated game ready");
   AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;GameSession.Instance.MarkStarted(gameSeconds:20);
   foreach(var person in CommanderRoster.Instance.Commanders){person.CommandStop();person.SetJobEnabled(CommanderJobs.All,false);person.Traits.values.Clear();person.PersonalState.meal=new CommanderMealState();person.PersonalState.sleep=new CommanderSleepState();person.PersonalState.hygiene=new CommanderHygieneState();person.PersonalState.joy=new CommanderJoyState();}
   var c=CommanderRoster.Instance.Commanders.First();var home=c.Position;Check(NavMesh.SamplePosition(home+Vector3.right*6,out var hit,5,NavMesh.AllAreas),"reachable industry location");var at=new Vector3(Mathf.Floor(hit.position.x)+.5f,hit.position.y,Mathf.Floor(hit.position.z)+.5f);
   var research=CampaignResearch.Instance;var rs=research.CaptureState();rs.completed.Add((int)ScienceTechnology.UndergroundSurvey);research.RestoreState(rs);Research(R.TitaniumOre);
   Check(StoreBin.TypeCount<64&&StoreBin.TypeCount==Enum.GetValues(typeof(R)).Length,"new resource types fit real storage mask");
   foreach(var type in new[]{R.TitaniumOre,R.StellarOre,R.Titanium,R.TitaniumAlloy}){var model=Resources.Load<GameObject>(ResourceVisual.Folder+ResourceVisual.ModelFor(type,true));Check(model!=null&&model.GetComponentsInChildren<Renderer>(true).Length>0,"real model "+type);}
   var storage=Put<Storage>(B.Storage,home+Vector3.left*3);var smelter=Put<Processor>(B.GasSmelter,at);var pipe=Put<SupplyFacility>(B.GasPipe,at+Vector3.right);var tank=Put<SupplyFacility>(B.GasTank,at+Vector3.right*2);var steam=Put<PowerNode>(B.SteamGenerator,at+Vector3.left);
   ResourceManager.Instance.AddTo(storage.Bin,R.Coal,20);steam.RestoreFuel(10);PowerGrid.Tick(.5f);Check(smelter.Powered&&smelter.Demand==200,"gas smelter receives actual electric supply");
   Check(smelter.AddOrder(0,Workshop.OrderMode.Count,1),"titanium recipe accepted");ResourceManager.Instance.AddTo(storage.Bin,R.TitaniumOre,2);Check(tank.Fill(R.NaturalGas,3),"actual gas tank supply");
   WorkPriorities.Set(smelter,9,false);c.SetJobEnabled(CommanderJobs.Hauling,true);c.TickDuty(2);
   Check(object.ReferenceEquals(c.DeliverySite,smelter),"automatic hauler chooses priority production delivery");
   Time.timeScale=1;await Wait(()=>State(smelter).staged.Any(s=>s.type==R.TitaniumOre&&s.amount==2),30);Time.timeScale=0;
   Check(State(smelter).staged.Any(s=>s.type==R.TitaniumOre&&s.amount==2)&&ResourceManager.Instance.GetAmount(R.TitaniumOre)==0,"hauler physically delivers titanium ore");c.CommandStop();c.SetJobEnabled(CommanderJobs.Hauling,false);c.SetJobEnabled(CommanderJobs.Crafting,true);smelter.Powered=true;
   tank.RestoreSupply(new SupplyFacility.State());Time.timeScale=1;Check(!smelter.Work(c,1)&&smelter.Current==null,"no gas means no material consumption");Check(State(smelter).staged.Sum(s=>s.amount)==2,"ingredients preserved without fuel");tank.Fill(R.NaturalGas,3);smelter.Powered=false;Check(!smelter.Work(c,1),"unpowered gas smelter pauses");smelter.Powered=true;
   Check(smelter.Work(c,.1f)&&smelter.Current?.output==R.Titanium&&State(smelter).gasPaid,"one prepaid gas unit starts batch");Check(tank.Supply.amount==2&&State(smelter).staged.Count==0,"fuel and staged ingredients consumed once");
   pipe.transform.position+=Vector3.forward*10;var progress=smelter.Progress;Check(smelter.Work(c,.1f)&&smelter.Progress>progress&&tank.Supply.amount==2,"paid batch continues across gas disconnection without repayment");smelter.Powered=false;progress=smelter.Progress;Check(!smelter.Work(c,100)&&smelter.Progress==progress,"outage preserves unfinished prepaid batch");smelter.Powered=true;Time.timeScale=0;
   var smelterAt=smelter.Position;Check(SaveSystem.TrySave(false,0,out var error),"paid batch save valid: "+error);
   Check(SaveValidator.TryParse(File.ReadAllText(SaveSlots.PathFor(false,0)),out var saved,out error),"saved paid state parses");var dto=saved.buildings.Single(b=>b.kind=="GasSmelter");dto.processor.gasPaid=false;Check(!SavePreflight.Validate(saved,out error),"save rejects unpaid active gas batch");
   Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"paid batch reload accepted: "+error);await Wait(()=>!SaveSystem.Busy);Time.timeScale=0;
   c=CommanderRoster.Instance.Commanders.First();smelter=SceneObjects.All<Processor>().Single(p=>p.Data.kind==B.GasSmelter);tank=SupplyFacility.All.Single(s=>s.Kind==B.GasTank);pipe=SupplyFacility.All.Single(s=>s.Kind==B.GasPipe);storage=SceneObjects.All<Storage>().First();
   Check(State(smelter).gasPaid&&tank.Supply.amount==2&&smelter.Current?.output==R.Titanium,"prepaid state and disconnected gas stock restored");smelter.Powered=true;Time.timeScale=1;var titanium=Floor(R.Titanium);Check(smelter.Work(c,1000),"restored paid batch completes without new gas");Near(Floor(R.Titanium)-titanium,1,"titanium output on floor");Check(tank.Supply.amount==2&&!State(smelter).gasPaid,"completion resets fuel flag without extra consumption");
   pipe.transform.position=smelterAt+Vector3.right;ResourceManager.Instance.AddTo(storage.Bin,R.Titanium,2);ResourceManager.Instance.AddTo(storage.Bin,R.Steel,1);Check(smelter.AddOrder(1,Workshop.OrderMode.Count,1),"alloy recipe accepted");
   Check(smelter.Deliver(new object(),R.Titanium,2)==2&&smelter.Deliver(new object(),R.Steel,1)==1,"alloy accepts delivered recipe inputs");ResourceManager.Instance.TakeFrom(storage.Bin,R.Titanium,2);ResourceManager.Instance.TakeFrom(storage.Bin,R.Steel,1);
   var alloy=Floor(R.TitaniumAlloy);Check(smelter.Work(c,1000),"titanium alloy batch produced using gas");Near(Floor(R.TitaniumAlloy)-alloy,2,"alloy amount from specified recipe");Check(tank.Supply.amount==1,"one gas for alloy batch");
   var cost=EarlyConstruction.Cost(B.AdvancedDeepDrill);Check(cost.Any(v=>v.type==R.TitaniumAlloy&&v.amount==10)&&cost.Any(v=>v.type==R.Copper&&v.amount==5),"upper drill construction consumes actual alloy");
   var survey=UndergroundSurvey.Instance;var data=survey.Capture();var layer=data.layers.First(l=>l.map==0&&l.type>=0);Check(NavMesh.SamplePosition(home+Vector3.forward*10,out var depositPoint,7,NavMesh.AllAreas),"upper drill deposit reachable");layer.type=(int)R.StellarOre;layer.total=80;layer.remaining=80;layer.work=300;layer.discovered=true;layer.discoveryPending=false;layer.position=depositPoint.position;survey.Restore(data);var id=layer.id;layer=survey.Find(id);Research(R.StellarOre);
   var basic=Put<SupplyFacility>(B.DeepDrill,layer.position);basic.Powered=true;Check(UndergroundProduction.Batch(basic)==0,"steel drill still cannot bypass highest grade");basic.gameObject.SetActive(false);Object.Destroy(basic.gameObject);
   var upper=Put<SupplyFacility>(B.AdvancedDeepDrill,layer.position);upper.Powered=true;Check(UndergroundProduction.Grade(upper.Kind)==5&&UndergroundProduction.Batch(upper)==10,"alloy grade unlocks actual stellar extraction");c.CommandStop();c.SetJobEnabled(CommanderJobs.Crafting,false);c.SetJobEnabled(CommanderJobs.Gathering,true);Check(c.StartService(upper,CommanderJobs.Gathering),"worker accepts upper drill");c.Agent.Warp(c.Agent.destination);c.Agent.ResetPath();var stellar=Floor(R.StellarOre);Check(upper.WorkDrill(c,10000),"upper machine works stellar layer");Near(Floor(R.StellarOre)-stellar,80,"finite stellar output conserved");Check(layer.remaining==0&&layer.exhaustionNotified,"stellar layer exhausts normally");c.CommandStop();Time.timeScale=0;
   var generator=Put<PowerNode>(B.GasGenerator,at+Vector3.forward*50);var gasPipe=Put<SupplyFacility>(B.GasPipe,generator.Position+Vector3.right);var gasTank=Put<SupplyFacility>(B.GasTank,generator.Position+Vector3.right*2);var battery=Put<PowerNode>(B.Battery,generator.Position+Vector3.left);gasTank.Fill(R.NaturalGas,3);
   generator.TickGas(1);Check(gasTank.Supply.amount==3&&generator.Output==0,"paused gas generator cannot consume fuel");Time.timeScale=1;generator.TickGas(1);Check(gasTank.Supply.amount==2&&generator.FuelSeconds==9&&generator.Output==400,"gas enters generation through real gas network");PowerGrid.Tick(.5f);Check(battery.Charge>0,"gas energy charges actual electric network");
   gasPipe.gameObject.SetActive(false);generator.TickGas(2);Check(gasTank.Supply.amount==2&&generator.FuelSeconds==7,"stored fuel burns after pipe break");generator.TickGas(10);Check(generator.FuelSeconds==0&&generator.Output==0&&gasTank.Supply.amount==2,"disconnection prevents new fuel and stops generation at buffer end");
   gasPipe.gameObject.SetActive(true);generator.TickGas(1);Check(gasTank.Supply.amount==1&&generator.Output==400,"gas reconnection resumes generator");typeof(PowerNode).GetProperty("Charge").SetValue(battery,GameBalance.BatteryCapacity);var fuel=generator.FuelSeconds;generator.TickGas(2);Check(gasTank.Supply.amount==1&&generator.FuelSeconds==fuel&&generator.Output==0,"no electricity demand preserves prepaid fuel and reports zero output");Time.timeScale=0;
   Check(SaveSystem.TrySave(false,1,out error),"whole industrial chain save valid: "+error);var gasFuel=generator.FuelSeconds;var generatorAt=generator.Position;Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,1),out error),"industrial chain reload accepted: "+error);await Wait(()=>!SaveSystem.Busy);Time.timeScale=0;
   generator=PowerNode.All.Single(p=>p.Kind==B.GasGenerator);Near(generator.FuelSeconds,gasFuel,"gas generator prepaid fuel survives reload");Check(SupplyFacility.All.Any(s=>s.Kind==B.AdvancedDeepDrill)&&ResourceNode.Available.Any(n=>n.IsLooseCargo&&n.ResourceType==R.TitaniumAlloy),"upper machine and real alloy output survive scene reload");
   AntColony.UI.BuildScreen.Open();Check(Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include).Any(b=>b.name=="Build GasSmelter")&&Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include).Any(b=>b.name=="Build AdvancedDeepDrill"),"industrial construction menu connected");while(AntColony.UI.BuildScreen.Back()){}
   var result="PASS "+checks+" titanium/alloy delivery, paid gas industry, upper drill, generation, save/load and menus checks";File.WriteAllText("Temp/advanced-industry1027-result.txt",result);return result;
  }
  catch(Exception e){File.WriteAllText("Temp/advanced-industry1027-result.txt",e.ToString());throw;}
  finally{Time.timeScale=0;SaveStorage.RootOverride=root;UserSettings.Apply(old,false);}
 }
}
