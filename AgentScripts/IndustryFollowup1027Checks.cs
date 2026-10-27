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
public static class IndustryFollowup1027Checks
{
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception("Followup FAIL "+checks+" "+label);checks++;File.WriteAllText("Temp/industry-followup-progress.txt",checks+" "+label);}
 static void Near(float a,float b,string label)=>Check(Mathf.Abs(a-b)<.02f,label+" "+a+"/"+b);
 static async Task Wait(Func<bool> ready,int seconds=90){var end=DateTime.UtcNow.AddSeconds(seconds);while(!ready()&&DateTime.UtcNow<end)await Task.Delay(70);}
 static T Put<T>(B kind,Vector3 at)where T:BuildingBase{var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{kind,AntColony.Data.UnitRole.Worker});var go=Object.Instantiate(template,at,Quaternion.identity);go.name=kind.ToString();go.SetActive(true);return go.GetComponent<T>();}
 static void Park(CommanderAnt c,SupplyFacility s){c.CommandStop();Check(c.StartService(s,CommanderJobs.Crafting),"crafting assignment");c.Agent.Warp(c.Agent.destination);c.Agent.ResetPath();}
 public static async Task<string> Main()
 {
  checks=0;Time.timeScale=0;var old=UserSettings.Current.Clone();var root=SaveStorage.RootOverride;SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"IndustryFollowup-"+Guid.NewGuid().ToString("N"));
  try
  {
   var config=old.Clone();config.autoSaveEnabled=false;config.autoPauseThreat=config.autoPauseDowned=config.autoPauseRocket=false;UserSettings.Apply(config,false);
   await Wait(()=>!SaveSystem.Busy);StartingCandidate[] team;do{team=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray();}while(StartingCandidate.Missing(team).Length>0);foreach(var t in team){t.ageYears=20;t.traits.values.Clear();}
   SaveSystem.NewGame(new NewGameOptions{seed=271027,mapSize=MapSize.Small,startingCount=3,startingTeam=team});await Wait(()=>!SaveSystem.Busy&&CommanderRoster.Instance.Commanders.Count==3);AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;GameSession.Instance.MarkStarted(gameSeconds:20);
   foreach(var p in CommanderRoster.Instance.Commanders){p.CommandStop();p.SetJobEnabled(CommanderJobs.All,false);p.PersonalState.sleep=new CommanderSleepState();p.PersonalState.meal=new CommanderMealState();p.PersonalState.hygiene=new CommanderHygieneState();p.PersonalState.joy=new CommanderJoyState();}
   var c=CommanderRoster.Instance.Commanders.First();var id=c.PersonalState.id;Check(NavMesh.SamplePosition(c.Position+Vector3.right*6,out var hit,5,NavMesh.AllAreas),"reachable refinery");var at=new Vector3(Mathf.Floor(hit.position.x)+.5f,hit.position.y,Mathf.Floor(hit.position.z)+.5f);
   var refinery=Put<SupplyFacility>(B.OilRefinery,at);var raw=Put<SupplyFacility>(B.FluidTank,at+Vector3.right);raw.Fill(R.CrudeOil,40);var output=Put<SupplyFacility>(B.FluidTank,at+Vector3.left);output.Fill(R.Petroleum,1);
   var battery=Put<PowerNode>(B.Battery,at+Vector3.forward);typeof(PowerNode).GetProperty("Charge").SetValue(battery,GameBalance.BatteryCapacity);PowerGrid.Tick(.5f);Check(refinery.Powered,"real grid supplies 200W refinery");Time.timeScale=1;refinery.Tick(20);Check(raw.Supply.amount==40&&output.Supply.amount==1,"power alone does not refine");
   Check(!c.StartService(refinery,CommanderJobs.Crafting),"job zero blocks direct refinery order");c.SetJobEnabled(CommanderJobs.Crafting,true);Check(c.StartService(refinery,CommanderJobs.Crafting),"worker accepts refinery order");Check(!refinery.WorkRefinery(c,1),"cannot refine before reaching workstation");
   var visual=c.GetComponent<AntVisual>();Check(visual!=null&&!(bool)typeof(AntVisual).GetProperty("HasStationaryWork",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(visual),"approaching station keeps movement animation instead of premature work");
   await Wait(()=>c.ServiceTarget==refinery&&refinery.Supply.work>0,25);Time.timeScale=0;Check(c.ServiceTarget==refinery&&refinery.Supply.work>0,"worker really walks and starts refining");
   refinery.Supply.work=2;var before=raw.Supply.amount;Time.timeScale=1;refinery.Powered=false;Check(!refinery.WorkRefinery(c,10),"power loss blocks refining");Near(refinery.Supply.work,2,"power loss preserves progress");Check(raw.Supply.amount==before,"power loss preserves raw oil");refinery.Powered=true;
   Time.timeScale=0;Check(!refinery.WorkRefinery(c,10),"pause blocks work");Time.timeScale=1;refinery.Supply.work=0;before=raw.Supply.amount;var produced=output.Supply.amount;Check(refinery.WorkRefinery(c,SupplyFacility.ProcessSeconds/c.WorkRate(CommanderActivity.Crafting)+.001f),"onsite worker refines a batch");Check(raw.Supply.amount==before-1&&output.Supply.amount==produced+1,"exact one piped input and output");
   output.RestoreSupply(new SupplyFacility.State{fluid=(int)R.Petroleum,amount=100});refinery.RestoreSupply(new SupplyFacility.State{fluid=(int)R.Petroleum,amount=100,work=2});Check(!refinery.WorkRefinery(c,20)&&refinery.RefineryStatus.Contains("출력"),"full output pauses");Near(refinery.Supply.work,2,"full output preserves progress");
   output.RestoreSupply(new SupplyFacility.State());refinery.RestoreSupply(new SupplyFacility.State{work=2});raw.RestoreSupply(new SupplyFacility.State());Check(!refinery.WorkRefinery(c,20)&&refinery.RefineryStatus.Contains("원유"),"missing raw input pauses");raw.Fill(R.CrudeOil,30);Park(c,refinery);
   var survey=UndergroundSurvey.Instance;var state=survey.Capture();var layers=state.layers.Where(l=>l.map==0&&l.type>=0).Take(3).ToArray();for(int i=0;i<3;i++){layers[i].type=(int)R.CopperOre;layers[i].total=400;layers[i].remaining=i==0?0:400;layers[i].exhaustionNotified=i==0;layers[i].work=300;layers[i].discovered=true;layers[i].discoveryPending=false;layers[i].position=at+Vector3.forward*(12+12*i);}survey.Restore(state);layers=survey.Layers.Where(l=>l.map==0&&l.discovered&&l.type==(int)R.CopperOre).ToArray();
   var drill=Put<SupplyFacility>(B.DeepDrill,layers[0].position);var active=Put<SupplyFacility>(B.AdvancedDeepDrill,layers[2].position);Check(!Demolition.CanMove(active),"live deposit drill cannot relocate");Check(Demolition.CanMove(drill),"exhausted drill can relocate");Check(Demolition.OrderMove(drill,at)==null,"relocation rejects outside deposits");Check(Demolition.OrderMove(drill,layers[2].position)==null,"relocation rejects occupied deposit");
   var dest=layers[1].position;var site=Demolition.OrderMove(drill,dest);Check(site!=null&&Demolition.Pending(drill)&&!Demolition.CanMove(drill),"one relocation per target");Check(UndergroundProduction.Occupied(layers[1])&&UndergroundProduction.PlacementReason(B.DeepDrill,dest)!=null,"destination reserved before packing");
   site.BeginWork();site.Complete();Check(!drill.enabled&&Demolition.IsPacked(drill)&&site.GetComponent<DemolitionSite>().Packed,"existing pack phase disables drill");Time.timeScale=0;refinery.Supply.work=2;c.CommandStop();Park(c,refinery);
   Check(SaveSystem.TrySave(false,0,out var error),"packed move and refinery work save: "+error);Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"load accepted: "+error);await Wait(()=>!SaveSystem.Busy);Time.timeScale=0;
   refinery=SupplyFacility.All.First(s=>s.Kind==B.OilRefinery);c=CommanderRoster.Instance.Commanders.First(p=>p.PersonalState.id==id);Near(refinery.Supply.work,2,"refining progress survives scene reload");Check(c.ServiceTarget==refinery&&c.ServiceJob==CommanderJobs.Crafting,"same refining worker restored");
   site=SceneObjects.All<BuildingConstructionSite>().First(s=>s.GetComponent<DemolitionSite>()?.Move==true);drill=(SupplyFacility)site.GetComponent<DemolitionSite>().Target;var targetLayer=UndergroundProduction.LayerAt(drill.Kind,dest);Check(Demolition.IsPacked(drill)&&UndergroundProduction.Occupied(targetLayer),"packed drill and destination reservation survive reload");
   var total=Enum.GetValues(typeof(R)).Cast<R>().Sum(ResourceManager.Instance.GetAmount);site.Complete();await Task.Delay(80);Near((drill.Position-dest).sqrMagnitude,0,"drill installed at new deposit");Check(drill.enabled&&!Demolition.IsPacked(drill),"drill unpacks and re-enables");Check(UndergroundProduction.LayerAt(drill.Kind,drill.Position).remaining==400,"move preserves new deposit stock");Check(Enum.GetValues(typeof(R)).Cast<R>().Sum(ResourceManager.Instance.GetAmount)==total,"relocation costs no new resources");
   survey=UndergroundSurvey.Instance;Check(survey.Layers.Any(l=>l.map==0&&l.discovered&&l.remaining==0),"old exhausted reserve stays exhausted");
   Check(SaveSystem.TrySave(false,1,out error),"completed relocation remains save valid: "+error);
   var result="PASS "+checks+" refinery worker/power/input/output and drill relocation/reservation/save checks";File.WriteAllText("Temp/industry-followup-result.txt",result);return result;
  }
  catch(Exception e){File.WriteAllText("Temp/industry-followup-result.txt",e.ToString());throw;}
  finally{Time.timeScale=0;SaveStorage.RootOverride=root;UserSettings.Apply(old,false);}
 }
}
