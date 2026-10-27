using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using R=AntColony.Data.ResourceType;
using Object=UnityEngine.Object;
public static class AutoWork1026Checks
{
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL "+checks+" "+label);checks++;System.IO.File.WriteAllText("Temp/autowork1026-progress.txt",checks+" "+label);}
 static T Build<T>(AntColony.Data.BuildingKind kind,Vector3 at) where T:BuildingBase
 {
  var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{kind,AntColony.Data.UnitRole.Worker});
  var go=Object.Instantiate(template,at,Quaternion.identity);go.SetActive(true);return go.GetComponent<T>();
 }
 static void Needs(CommanderAnt c)
 {
  c.Traits.values.Clear();c.PersonalState.sleep=new CommanderSleepState();c.PersonalState.meal=new CommanderMealState();c.PersonalState.hygiene.hygiene=100;c.PersonalState.joy.joy=100;
 }
 static async Task Wait(Func<bool> done,int seconds=20){var until=DateTime.UtcNow.AddSeconds(seconds);while(!done()&&DateTime.UtcNow<until)await Task.Delay(80);}
 public static async Task<string> Main()
 {
  checks=0;var old=UserSettings.Current.Clone();var oldRoot=AntColony.Save.SaveStorage.RootOverride;GameObject test=null,siteGo=null;Processor bench=null;Storage store=null;
  AntColony.Save.SaveStorage.RootOverride=System.IO.Path.Combine(Application.temporaryCachePath,"AutoWork1026-"+Guid.NewGuid().ToString("N"));
  try
  {
   Check(Application.isPlaying,"Play mode");var settings=old.Clone();settings.autoSaveEnabled=false;settings.autoPauseThreat=settings.autoPauseDowned=settings.autoPauseRocket=false;UserSettings.Apply(settings,false);
   await Wait(()=>!AntColony.Save.SaveSystem.Busy,90);
   StartingCandidate[] team;do{team=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray();}while(StartingCandidate.Missing(team).Length>0);
   AntColony.Save.SaveSystem.NewGame(new NewGameOptions{seed=261026,mapSize=MapSize.Small,startingCount=3,startingTeam=team});await Wait(()=>!AntColony.Save.SaveSystem.Busy,90);Check(!AntColony.Save.SaveSystem.Busy,"new game ready");
   AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;
   foreach(var x in AntColony.Core.CommanderRoster.Instance.Commanders){x.CommandStop();x.SetJobEnabled(CommanderJobs.All,false);Needs(x);}
   var c=AntColony.Core.CommanderRoster.Instance.Commanders.First();
   Check(UnityEngine.AI.NavMesh.SamplePosition(c.Position+Vector3.right*4,out var hit,3,UnityEngine.AI.NavMesh.AllAreas),"work ground");
   test=GameObject.CreatePrimitive(PrimitiveType.Cylinder);test.SetActive(false);test.transform.position=hit.position+Vector3.up*.5f;var n=test.AddComponent<ResourceNode>();n.ConfigureNatural(R.Wood,4,0);test.SetActive(true);await Task.Yield();
   c.SetJobEnabled(CommanderJobs.Gathering,true);c.TickDuty(2);Check(!n.WorkRequested&&c.CurrentResourceNode!=n,"unmarked natural resource is not automatic work");
   Check(n.RequestWork()&&n.WorkRequested,"designation accepted");var age=WorkPriorities.RequestedAt(n);n.RequestWork();Check(WorkPriorities.RequestedAt(n)==age,"repeated designation keeps one request");
   c.TickDuty(2);Check(c.CurrentResourceNode==n,"idle allowed worker claims designated wood");Check(n.GetComponent<GatherOrderFeedback>()!=null,"automatic gather has target feedback");
   n.PauseWork(true);Check(n.WorkPaused&&c.CurrentResourceNode!=n,"pause releases worker");c.TickDuty(2);Check(c.CurrentResourceNode!=n,"paused work not reclaimed");
   n.PauseWork(false);c.SetJobEnabled(CommanderJobs.Gathering,false);c.CommandGather(n);Check(c.CurrentResourceNode!=n,"direct gather respects job zero");
   c.SetJobEnabled(CommanderJobs.Gathering,true);c.CommandGather(n);Check(c.CurrentResourceNode==n,"direct gather resumes designated work");
   var warehouse=ResourceManager.Instance.GetAmount(R.Wood);Time.timeScale=1;await Wait(()=>n.IsDepleted,25);Time.timeScale=0;
   Check(n.IsDepleted,"real-frame gather completes");Check(!n.WorkRequested,"depletion consumes request");
   Check(ResourceManager.Instance.GetAmount(R.Wood)==warehouse,"gather does not teleport output to storage");
   Check(ResourceNode.Available.Any(x=>x.IsLooseCargo&&x.ResourceType==R.Wood&&x.AmountRemaining>0),"harvest output remains as haulable floor goods");
   c.CommandStop();c.SetJobEnabled(CommanderJobs.All,false);Object.Destroy(test);test=null;await Task.Yield();
   var actual=ResourceNode.Available.First(x=>x.NeedsDesignation&&!x.GatheringForbidden&&!x.IsDepleted&&x.GetComponentInParent<AntColony.World.ExpeditionSite>()==null);
   Check(actual.RequestWork(),"real map request");actual.PauseWork(true);var sequence=actual.WorkRequestOrder;
   Check(AntColony.Save.SaveSystem.TrySave(false,0,out var saveError),"save requests: "+saveError);
   var oldDto=JsonUtility.FromJson<AntColony.Save.ResourceNodeDto>("{}");Check(!oldDto.workRequested&&!oldDto.workPaused,"old node save defaults to unmarked");
   actual.CancelWork();Check(!actual.WorkRequested&&actual.WorkRequestOrder==0,"cancel clears request and age");
   var inputs=new ProductionInputs();var required=new[]{(R.Wood,2)};var courier1=new object();var courier2=new object();
   Check(inputs.Reserve(courier1,R.Wood,2,required)&&!inputs.Reserve(courier2,R.Wood,1,required),"ingredient reservations prevent duplicate deliveries");
   Check(inputs.Deliver(courier1,R.Wood,2,required)==2&&inputs.Ready(required),"only physically delivered stock is ready");
   Check(inputs.Consume(required)&&!inputs.Consume(required),"ingredients consumed exactly once");
   var rm=ResourceManager.Instance;store=Build<Storage>(AntColony.Data.BuildingKind.Storage,c.Position+Vector3.forward*4);rm.AddTo(store.Bin,R.Wood,12);bench=Build<Processor>(AntColony.Data.BuildingKind.Carpentry,c.Position+Vector3.left*4);
   Check(bench.AddOrder(0,Workshop.OrderMode.Count,1),"carpentry count order queued");Check(!bench.NeedsWork&&bench.NeedsIngredients,"stored materials require delivery before production");
   c.SetJobEnabled(CommanderJobs.Crafting,true);c.TickDuty(2);Time.timeScale=1;await Wait(()=>c.DeliverySite==bench||bench.Current!=null,12);Time.timeScale=0;Check(c.DeliverySite==bench||bench.Current!=null,"worker takes actual production delivery; ready="+c.CivilianWorkReady+" allow="+c.AllowsJob(CommanderJobs.Crafting)+" approach="+c.TryWorkApproach(bench.Position,out _)+" stock="+rm.GetAmount(R.Wood)+" pickup="+bench.NextPickup(out _,out _)+" carrying="+c.CarriedAmount+" bins="+string.Join(";",rm.Bins.Where(b=>b.Amount(R.Wood)>0).Select(b=>b.Host+"/"+b.IsBase+"/"+b.Spendable)));
   Time.timeScale=1;await Wait(()=>bench.Current!=null||bench.Orders.Count==0,35);Time.timeScale=0;
   Check(bench.Current!=null||bench.Orders.Count==0,"delivery leads to automatic production");
   Time.timeScale=1;await Wait(()=>bench.Current==null&&bench.Orders.Count==0,25);Time.timeScale=0;
   Check(bench.Current==null&&bench.Orders.Count==0,"count production stops at requested batch");
   Check(ResourceNode.Available.Any(x=>x.IsLooseCargo&&x.ResourceType==R.Plank),"processed goods are floor output");
   c.CommandStop();c.SetJobEnabled(CommanderJobs.All,false);Object.Destroy(bench.gameObject);bench=null;Needs(c);
   siteGo=GameObject.CreatePrimitive(PrimitiveType.Cube);siteGo.transform.position=c.Position+Vector3.forward*3;var site=siteGo.AddComponent<BuildingConstructionSite>();site.Initialize(null,100);
   c.SetJobEnabled(CommanderJobs.Building,true);c.CommandBuild(site);Time.timeScale=1;
   var seen=new System.Collections.Generic.HashSet<int>();var positions=new System.Collections.Generic.List<Vector3>();var deadline=DateTime.UtcNow.AddSeconds(32);
   while(seen.Count<3&&DateTime.UtcNow<deadline){if(c.IsBuildingAnimation){if(seen.Add(c.ConstructionStep))positions.Add(c.Position);}await Task.Delay(80);}
   Time.timeScale=0;Check(seen.Count==3,"construction visits three work stations");Check(positions.Any(p=>Vector3.Distance(p,positions[0])>1),"builder physically walks between stations");
   Check(c.ConstructionClip==(c.ConstructionStep==1?"Saw":"Tool_Hammer"),"construction selects hammer and saw clips");c.CommandStop();
   var result="PASS "+checks+" designation, physical harvesting/production and construction checks";System.IO.File.WriteAllText("Temp/autowork1026-result.txt",result);return result;
  }
  catch(Exception e){System.IO.File.WriteAllText("Temp/autowork1026-result.txt",e.ToString());throw;}
  finally{Time.timeScale=0;if(test!=null)Object.Destroy(test);if(siteGo!=null)Object.Destroy(siteGo);if(bench!=null)Object.Destroy(bench.gameObject);if(store!=null)Object.Destroy(store.gameObject);AntColony.Save.SaveStorage.RootOverride=oldRoot;UserSettings.Apply(old,false);}
 }
}
