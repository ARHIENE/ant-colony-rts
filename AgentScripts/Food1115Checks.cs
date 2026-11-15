using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;
using AntColony.Core;using AntColony.Buildings;using AntColony.Data;using AntColony.Units;using AntColony.World;using AntColony.Save;using AntColony.UI;using UnityEngine;
public static class Food1115Checks
{
 const string Log="work/colony-polish-1115/food-checks.txt";
 static void Check(bool ok,string text){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new Exception(text);}
 static async Task Ready(){var end=DateTime.UtcNow.AddSeconds(100);while(SaveSystem.Busy&&DateTime.UtcNow<end)await Task.Delay(80);Check(!SaveSystem.Busy,"Scene ready");}
 static T Put<T>(BuildingKind kind,Vector3 pos) where T:BuildingBase {var t=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{kind,UnitRole.Worker});var go=UnityEngine.Object.Instantiate(t,pos,Quaternion.identity);go.SetActive(true);return go.GetComponent<T>();}
 public static string Main(){File.WriteAllText(Log,"Isolated fixture: physical stock and deliveries, normal Kitchen.Work, meal consumption, save compatibility. Not natural campaign proof.\n");_=Run();return "Food regression started";}
 static async Task Run()
 {
  var root=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"Food1115-"+Guid.NewGuid().ToString("N"));
  try{
   var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false;UserSettings.Apply(quiet,false);
   await Ready();SaveSystem.NewGame(new NewGameOptions{seed=261115,mapSize=MapSize.Small});await Ready();GameMenuController.Instance.Resume();SimulationPause.UserSpeed(0);
   foreach(var person in CommanderRoster.Instance.Commanders){person.CommandStop();person.SetJobEnabled(CommanderJobs.All,false);}
   var c=CommanderRoster.Instance.Commanders.First();var store=Put<Storage>(BuildingKind.Storage,c.Position+Vector3.left*3);var kitchen=Put<Kitchen>(BuildingKind.Hearth,c.Position+Vector3.forward*6);kitchen.Orders.Clear();
   Check((int)ResourceType.Spirits==74&&(int)ResourceType.BarleyPorridge==82&&Kitchen.Recipes[5].output==ResourceType.SmokedFood,"Existing resource and recipe IDs preserved");
   foreach(var t in new[]{ResourceType.BarleyPorridge,ResourceType.Soup,ResourceType.HoneyBread}){
    int recipe=Array.FindIndex(Kitchen.Recipes,r=>r.output==t);var r=Kitchen.Recipes[recipe];
    Check(t.IsEdible()&&ColdStorage.Holds(t)&&FoodLots.ShelfLife(t)==GameCalendar.SecondsPerDay*3,"Diet, cold storage and spoilage "+t);
    var model=Resources.Load<GameObject>("ResourceModels/"+CookedFoods.Model(t));Check(model&&model.GetComponentsInChildren<Renderer>().All(x=>x.sharedMaterials.All(m=>m&&m.shader.name.StartsWith("Universal Render Pipeline/"))),"Distinct model and URP materials "+t);
    Check(kitchen.AddOrder(recipe,Workshop.OrderMode.Count,1),"Queue "+t);
    foreach(var need in r.inputs)ResourceManager.Instance.AddTo(store.Bin,need.type,need.amount);
    Check(!kitchen.NeedsCook,"Cannot cook before delivery "+t);
    foreach(var need in r.inputs){var courier=new object();Check(kitchen.BeginPickup(courier,need.type,need.amount),"Reserve "+need.type);int taken=ResourceManager.Instance.TakeFrom(store.Bin,need.type,need.amount,out var lots);Check(taken==need.amount&&kitchen.DeliverFood(courier,need.type,taken,lots)==taken,"Consume and deliver exact input "+need.type);}
    Check(kitchen.Work(c,.1f)&&kitchen.Current!=null,"Start "+t);Check(Kitchen.Valid(kitchen.Meals),"Partial cooking validates");
    if(t==ResourceType.BarleyPorridge){float progress=kitchen.Meals.progress;Check(SaveSystem.TrySave(false,1,out var partialError),"Save active cooking: "+partialError);Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,1),out partialError),"Reload active cooking: "+partialError);await Ready();SimulationPause.UserSpeed(0);c=CommanderRoster.Instance.Commanders.First();store=SceneObjects.All<Storage>().First();kitchen=SceneObjects.All<Kitchen>().First(k=>!k.IsTable);Check(kitchen.Current?.output==t&&Mathf.Abs(kitchen.Meals.progress-progress)<.0001f&&kitchen.Staged==0,"Active recipe and consumed ingredients survive reload");}
    bool worked=kitchen.Work(c,1000);Check(worked&&kitchen.Current==null&&kitchen.Orders.Count==0,"Finish exact queued meal "+t+" progress="+kitchen.Meals.progress.ToString("R")+" rate="+c.WorkRate(CommanderActivity.Cooking));
    await Task.Delay(150);var cargo=ResourceNode.Available.Single(n=>n.IsLooseCargo&&n.ResourceType==t);Check(cargo.AmountRemaining==1&&cargo.transform.Find("ResourceModel"),"Real floor output/model "+t);
   }
   Check(SaveSystem.TrySave(false,0,out var error),"Save cooked outputs: "+error);Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"Reload: "+error);await Ready();SimulationPause.UserSpeed(0);
   c=CommanderRoster.Instance.Commanders.First();store=SceneObjects.All<Storage>().First();
   foreach(var t in new[]{ResourceType.BarleyPorridge,ResourceType.Soup,ResourceType.HoneyBread})Check(ResourceNode.Available.Any(n=>n.IsLooseCargo&&n.ResourceType==t&&n.AmountRemaining==1),"Saved output survives "+t);
   foreach(var bin in ResourceManager.Instance.Bins.ToArray())foreach(var type in CookedFoods.MealTypes)ResourceManager.Instance.TakeFrom(bin,type,bin.Amount(type));
   foreach(var t in new[]{ResourceType.BarleyPorridge,ResourceType.Soup,ResourceType.HoneyBread}){
    ResourceManager.Instance.AddTo(store.Bin,t,1);var meal=c.PersonalState.meal;meal.diet=CommanderAnt.AllDiet;meal.served=false;meal.pickedFood=0;meal.retrySeconds=meal.eatSeconds=meal.poisonSeconds=0;meal.satiety=5;
    typeof(CommanderAnt).GetMethod("StartMeal",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(c,new object[]{null});Check(meal.eatSeconds>0&&meal.quality==CookedFoods.Quality(t)&&ResourceManager.Instance.GetAmount(t)==0,"Consumes correct meal grade "+t);
    typeof(CommanderAnt).GetMethod("FinishMeal",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(c,null);meal.eatSeconds=0;Check(c.Satiety==100,"Satisfies hunger "+t);
   }
   Check(SaveValidator.TryParse(File.ReadAllText("work/campaign-complete-1115/mothership-save.json"),out var old,out error)&&old.version==SaveFileV1.CurrentVersion,"Original v24 campaign checkpoint migration: "+error);
   Check(old.world.transports.All(t=>t.cargoAmounts==null||t.cargoAmounts.Length==StoreBin.TypeCount),"v24 transport cargo arrays expanded");
   GameMenuController.Instance.ShowKitchen(SceneObjects.All<Kitchen>().First(k=>!k.IsTable));
  }catch(Exception e){File.AppendAllText(Log,"ERROR "+e+"\n");}
  finally{SimulationPause.UserSpeed(0);UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;File.AppendAllText(Log,"RESTORED settings and save root\n");}
 }
}
