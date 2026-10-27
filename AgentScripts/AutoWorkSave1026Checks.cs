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
public static class AutoWorkSave1026Checks
{
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL "+checks+" "+label);checks++;System.IO.File.WriteAllText("Temp/autowork-save1026-progress.txt",checks+" "+label);}
 static T Build<T>(AntColony.Data.BuildingKind kind,Vector3 position) where T:BuildingBase
 {
  var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{kind,AntColony.Data.UnitRole.Worker});
  var go=Object.Instantiate(template,position,Quaternion.identity);go.name=kind.ToString();go.SetActive(true);return go.GetComponent<T>();
 }
 static float Floor(R t)=>ResourceNode.Available.Where(n=>n.IsLooseCargo&&n.ResourceType==t).Sum(n=>n.AmountRemaining);
 public static async Task<string> Main()
 {
  checks=0;var old=UserSettings.Current.Clone();var root=AntColony.Save.SaveStorage.RootOverride;Time.timeScale=0;
  AntColony.Save.SaveStorage.RootOverride=System.IO.Path.Combine(Application.temporaryCachePath,"AutoWorkSave1026-"+Guid.NewGuid().ToString("N"));
  try
  {
   var settings=old.Clone();settings.autoSaveEnabled=false;UserSettings.Apply(settings,false);
   StartingCandidate[] team;do{team=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray();}while(StartingCandidate.Missing(team).Length>0);
   AntColony.Save.SaveSystem.NewGame(new NewGameOptions{seed=261026,mapSize=MapSize.Small,startingCount=3,startingTeam=team});
   var ready=DateTime.UtcNow.AddSeconds(90);while(AntColony.Save.SaveSystem.Busy&&DateTime.UtcNow<ready)await Task.Delay(50);Time.timeScale=0;Check(!AntColony.Save.SaveSystem.Busy,"isolated game ready");
   foreach(var c in AntColony.Core.CommanderRoster.Instance.Commanders){c.CommandStop();c.SetJobEnabled(CommanderJobs.All,false);}
   var actor=AntColony.Core.CommanderRoster.Instance.Commanders.First();
   var node=ResourceNode.Available.First(n=>n.NeedsDesignation&&!n.GatheringForbidden&&!n.IsDepleted&&n.IsUnlocked);
   Check(node.RequestWork(),"natural request");node.PauseWork(true);var at=node.transform.position;var order=node.WorkRequestOrder;
   var p=Build<Processor>(AntColony.Data.BuildingKind.Carpentry,actor.Position+Vector3.left*6);p.AddOrder(0,Workshop.OrderMode.Count,2);
   var courier=new object();ResourceManager.Instance.AddTo(ResourceManager.Instance.BaseBin,R.Wood,2);Check(p.BeginPickup(courier,R.Wood,2),"production material reserved");ResourceManager.Instance.TakeFrom(ResourceManager.Instance.BaseBin,R.Wood,2);
   Check(p.Deliver(courier,R.Wood,2)==2&&p.NeedsWork,"delivered stock ready before save");p.Paused=true;
   var kitchen=Build<Kitchen>(AntColony.Data.BuildingKind.Kitchen,actor.Position+Vector3.forward*6);kitchen.Orders.Clear();kitchen.AddOrder(0,Workshop.OrderMode.Count,1);
   var food=new FoodLots();food.Add(2,2);Check(kitchen.DeliverFood(new object(),R.Food,2,food)==2,"freshness delivered to kitchen");kitchen.Paused=true;
   Check(AntColony.Save.SaveSystem.TrySave(false,0,out var error),"new save valid: "+error);
   Check(AntColony.Save.SaveSystem.TryLoad(AntColony.Save.SaveSlots.PathFor(false,0),out error),"new save accepted: "+error);
   var deadline=DateTime.UtcNow.AddSeconds(90);while(AntColony.Save.SaveSystem.Busy&&DateTime.UtcNow<deadline)await Task.Delay(50);Time.timeScale=0;Check(!AntColony.Save.SaveSystem.Busy,"load complete");
   node=ResourceNode.Available.FirstOrDefault(n=>(n.transform.position-at).sqrMagnitude<.01f);
   Check(node!=null&&node.WorkRequested&&node.WorkPaused&&node.WorkRequestOrder==order,"natural designation, pause and age survive scene reload");
   p=SceneObjects.All<Processor>().First(x=>x.Data.kind==AntColony.Data.BuildingKind.Carpentry);Check(p.Paused&&p.Orders.Count==1,"processor order and pause restored");p.Paused=false;Check(p.NeedsWork,"delivered materials restored without remote deduction");
   var before=Floor(R.Wood);p.CancelCurrent();Check(Mathf.Abs(Floor(R.Wood)-before-2)<.01f,"unconsumed ingredients returned exactly once");p.CancelCurrent();Check(Mathf.Abs(Floor(R.Wood)-before-2)<.01f,"repeat cancellation cannot duplicate refund");
   kitchen=SceneObjects.All<Kitchen>().First(x=>x.Data.kind==AntColony.Data.BuildingKind.Kitchen);Check(kitchen.Paused&&kitchen.Staged==2&&kitchen.Meals.stagedLots.Stalest<=2,"staged freshness and pause restored");
   var rotten=Floor(R.RottenFood);ResourceManager.Instance.Spoil(3);Check(kitchen.Staged==0&&Floor(R.RottenFood)>=rotten+2,"paused ingredients spoil instead of freezing time");
   var inputs=new ProductionInputs();var need=new[]{(R.Honeydew,2)};var lots=new FoodLots();lots.Add(1,1);lots.Add(2,50);
   Check(inputs.Deliver(new object(),R.Honeydew,2,need,lots)==2,"partial food delivery clamped");inputs.Age(2,kitchen.Position);Check(inputs.Amount(R.Honeydew)==1,"partial delivery preserves oldest lots");
   p.Orders.Clear();p.AddOrder(0,Workshop.OrderMode.Count,1);ResourceManager.Instance.AddTo(ResourceManager.Instance.BaseBin,R.Wood,2);
   Check(p.BeginPickup(courier,R.Wood,2),"new production delivery reserved");ResourceManager.Instance.TakeFrom(ResourceManager.Instance.BaseBin,R.Wood,2);Check(p.Deliver(courier,R.Wood,2)==2,"new ingredients delivered");
   var refund=Floor(R.Wood);p.RemoveOrder(p.Orders[0]);Check(Mathf.Abs(Floor(R.Wood)-refund-2)<.01f&&!p.NeedsWork,"deleting pending order returns staged inputs and unlocks next recipe");
   var result="PASS "+checks+" save/load, ingredient freshness and cancellation checks";System.IO.File.WriteAllText("Temp/autowork-save1026-result.txt",result);return result;
  }
  catch(Exception e){System.IO.File.WriteAllText("Temp/autowork-save1026-result.txt",e.ToString());throw;}
  finally{Time.timeScale=0;AntColony.Save.SaveStorage.RootOverride=root;UserSettings.Apply(old,false);}
 }
}
