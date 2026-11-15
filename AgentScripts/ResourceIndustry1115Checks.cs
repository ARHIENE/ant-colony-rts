using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;
using AntColony.Buildings;using AntColony.Core;using AntColony.Data;using AntColony.Save;using AntColony.UI;using AntColony.Units;using UnityEngine;
public static class ResourceIndustry1115Checks
{
 const string Log="work/colony-polish-1115/industry-checks.txt";
 static void Check(bool ok,string message){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new Exception(message);}
 static async Task Wait(Func<bool> done,int seconds=80){var until=DateTime.UtcNow.AddSeconds(seconds);while(!done()&&DateTime.UtcNow<until)await Task.Delay(100);Check(done(),"Wait completed");}
 public static async Task<string> Main()
 {
  if(!Application.isPlaying)throw new Exception("Play required");
  File.WriteAllText(Log,"Injected facility regression. Actual worker movement/production; not a natural economy campaign.\n");
  var root=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();
  SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"Industry1115-"+Guid.NewGuid().ToString("N"));
  try
  {
   var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false;UserSettings.Apply(quiet,false);
   await Wait(()=>!SaveSystem.Busy);SaveSystem.NewGame(new NewGameOptions{seed=261115,mapSize=MapSize.Small});await Wait(()=>!SaveSystem.Busy);
   GameMenuController.Instance.Resume();SimulationPause.UserSpeed(0);
   foreach(var worker in CommanderRoster.Instance.Commanders){worker.CommandStop();worker.SetJobEnabled(CommanderJobs.All,false);}
   var c=CommanderRoster.Instance.Commanders.First();c.PersonalState.toolTier=2;c.SetJobEnabled(CommanderJobs.Gathering,true);
   var kinds=new[]{BuildingKind.Quarry,BuildingKind.LoggingCamp,BuildingKind.SandPit,BuildingKind.ClayPit,BuildingKind.CultivationGarden,BuildingKind.OreMine,BuildingKind.DeepMine};
   var method=typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic);
   for(int i=0;i<kinds.Length;i++)
   {
    var k=kinds[i];var template=(GameObject)method.Invoke(null,new object[]{k,UnitRole.Worker});Check(template!=null,"Build template "+k);
    var go=UnityEngine.Object.Instantiate(template,c.Position+new Vector3(6+i*6,0,0),Quaternion.identity);go.name="IndustryCheck"+k;go.SetActive(true);
    var p=go.GetComponent<Processor>();Check(p&&p.Demand==(k==BuildingKind.DeepMine?200:0)&&p.IsResourceSite,"Correct manual/deep power requirement "+k);
    var model=Resources.Load<GameObject>("Industry1115/"+k);Check(model&&model.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m&&m.shader.name=="Universal Render Pipeline/Lit")),"Model/material "+k);
    Check(!p.NeedsWork,"No undesignated production "+k);
    if(k!=BuildingKind.OreMine && k!=BuildingKind.DeepMine)Check(p.AddOrder(0,Workshop.OrderMode.Count,1),"Explicit bill "+k);
    else Check(!p.AddOrder(0,Workshop.OrderMode.Count,1),"Ore research gate retained");
   }
   var quarry=SceneObjects.All<Processor>().Single(p=>p.name=="IndustryCheckQuarry");
   var garden=SceneObjects.All<Processor>().Single(p=>p.name=="IndustryCheckCultivationGarden");
   Check(!garden.NeedsWork,"Garden waits for physical water delivery");
   Check(!quarry.Work(c,1000),"Remote work cannot generate stone");
   var campaign=CampaignResearch.Instance;
   Check(campaign.CaptureState().regionalDiscovery && !AntColony.World.WorldMapManager.Instance.Sites.Any(AntColony.World.WorldMapManager.SiteRevealed),"New game regions and first boss hidden");
   var region=AntColony.World.WorldMapManager.Instance.Sites.First(s=>s.Kind==AntColony.World.ExpeditionSiteKind.ResourceSite);
   typeof(CampaignResearch).GetMethod("RevealRegion",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(campaign,new object[]{region,"Fixture clue"});
   Check(AntColony.World.WorldMapManager.SiteRevealed(region),"Found clue reveals one destination");
   var regionTitle=region.Title;
   var before=ProductionStock.Count(ResourceType.Stone);var start=c.Position;
   Check(CampaignResearch.Technologies.Select((d,i)=>(int)d.Technology==i).All(v=>v),"Research IDs preserve existing saves and prerequisites");
   Check(c.StartService(quarry,CommanderJobs.Gathering),"Assign quarry through normal service order");SimulationPause.UserSpeed(3);
   await Wait(()=>quarry.Orders.Count==0 && quarry.Current==null,100);SimulationPause.UserSpeed(0);
   Check(ProductionStock.Count(ResourceType.Stone)>=before+5,"Quarry batch adds five actual stone");
   Check(Vector3.Distance(start,c.Position)>1,"Worker walks to quarry");
   Check(quarry.Orders.Count==0&&quarry.Current==null,"Exactly one batch consumed");
   quarry.AddOrder(0,Workshop.OrderMode.KeepStock,30);
   Check(SaveSystem.TrySave(false,0,out var error),"Save facilities: "+error);
   Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"Reload facilities: "+error);await Wait(()=>!SaveSystem.Busy);SimulationPause.UserSpeed(0);
   Check(SceneObjects.All<Processor>().Count(p=>p.IsResourceSite)==7,"Seven new building kinds restored");
   quarry=SceneObjects.All<Processor>().Single(p=>p.Data.kind==BuildingKind.Quarry);
   Check(quarry.Orders.Count==1&&quarry.Orders[0].amount==30,"Production goal persists");
   Check(CampaignResearch.Instance.CaptureState().knownRegions.Contains(regionTitle),"Region discovery persists through full scene reload");
   return File.ReadAllText(Log);
  }
  finally{SimulationPause.UserSpeed(0);UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;}
 }
 public static string VerifyRestored()
 {
  var quarry=SceneObjects.All<Processor>().Single(p=>p.Data.kind==BuildingKind.Quarry);
  Check(quarry.Orders.Count==1&&quarry.Orders[0].amount==30,"Restored production goal matches allowed maximum 30 (previous test incorrectly expected unclamped 50)");
  Check(CampaignResearch.Instance.CaptureState().knownRegions.Count==1,"Discovered region persisted after full reload");
  return File.ReadAllText(Log);
 }
}
