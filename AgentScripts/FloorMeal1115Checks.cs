using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;
using AntColony.Core;using AntColony.Data;using AntColony.Units;using AntColony.World;using AntColony.Save;using AntColony.UI;using UnityEngine;
public static class FloorMeal1115Checks {
 const string Log="work/colony-polish-1115/floor-meal.txt";
 static void Check(bool ok,string text){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new Exception(text);}
 public static string Main(){File.WriteAllText(Log,"Isolated supplied floor-food fixture; normal walking, pickup, eating and full save/load.\n");_=Run();return "Floor meal checks started";}
 static async Task Ready(){var end=DateTime.UtcNow.AddSeconds(120);while(SaveSystem.Busy&&DateTime.UtcNow<end)await Task.Delay(100);Check(!SaveSystem.Busy,"Scene ready");}
 static bool Available(CommanderAnt c)=>(bool)typeof(CommanderAnt).GetProperty("HasAccessibleMeal",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);
 static ResourceNode Drop(ResourceType t,int amount,Vector3 pos){var before=ResourceNode.Available.ToArray();typeof(DiplomacyManager).GetMethod("DropFloor",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(ResourceType),typeof(int),typeof(Vector3)},null).Invoke(null,new object[]{t,amount,pos});return ResourceNode.Available.First(n=>!before.Contains(n));}
 static async Task Run(){var root=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();try{
 var quiet=settings.Clone();quiet.autoSaveEnabled=false;quiet.autoPauseThreat=false;UserSettings.Apply(quiet,false);SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"FloorMeal1115-"+Guid.NewGuid().ToString("N"));
 SaveSystem.NewGame(new NewGameOptions{seed=261115,mapSize=MapSize.Small});await Ready();GameMenuController.Instance.Resume();SimulationPause.UserSpeed(0);
 foreach(var p in CommanderRoster.Instance.Commanders){p.CommandStop();p.SetJobEnabled(CommanderJobs.All,false);}
 foreach(var n in ResourceNode.Available.Where(n=>n.IsLooseCargo&&CookedFoods.MealTypes.Contains(n.ResourceType)).ToArray())n.gameObject.SetActive(false);
 foreach(var t in CookedFoods.MealTypes)ResourceManager.Instance.TrySpend(t,ResourceManager.Instance.GetAmount(t));
 var c=CommanderRoster.Instance.Commanders.First();c.PersonalState.carriedRations=new FoodLots();c.PersonalState.meal.satiety=0;c.PersonalState.meal.diet=CommanderAnt.DietSimple;c.PersonalState.AddMood("fixture",-35,90);
 Drop(ResourceType.Food,10,c.Position+Vector3.left*2);Check(!Available(c),"Raw floor food respects cooked-only diet");
 var meal=Drop(ResourceType.SimpleMeal,2,c.Position+Vector3.forward*7);var lots=meal.GetComponent<FloorFood>().Lots;lots.lots.Clear();lots.Add(2,300,14,true);
 Check(Available(c),"Accessible cooked floor food is a meal source");var upkeep=SceneObjects.Any<UpkeepManager>();typeof(UpkeepManager).GetMethod("RunCycle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(upkeep,null);
 Check(c.IsColonyMember&&!c.IsDeparting&&upkeep.ConsecutiveFailures==0,"Empty raw stock does not cause departure while a meal is reachable");
 var start=c.Position;c.TickDuty(.1f);Check(meal.AmountRemaining==2&&!c.IsEating,"Distant meal is not consumed remotely");
 SimulationPause.UserSpeed(1);var end=DateTime.UtcNow.AddSeconds(50);while(!c.IsEating&&DateTime.UtcNow<end)await Task.Delay(30);SimulationPause.UserSpeed(0);
 Check(c.IsEating&&Vector3.Distance(start,c.Position)>2&&meal.AmountRemaining==1,"Normal movement reaches and picks one floor meal");
 Check(c.PersonalState.meal.quality==1&&c.PersonalState.meal.cookSkill==13&&c.PersonalState.meal.inspired,"Cooking quality and inspiration survive floor pickup");
 string id=c.PersonalState.id;Check(SaveSystem.TrySave(false,0,out var error),"Save during eating: "+error);Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"Load eating save: "+error);await Ready();GameMenuController.Instance.Resume();SimulationPause.UserSpeed(0);
 c=CommanderRoster.Instance.Commanders.First(p=>p.PersonalState.id==id);Check(c.IsEating&&c.PersonalState.meal.cookSkill==13,"Picked meal survives full reload");
 SimulationPause.UserSpeed(2);end=DateTime.UtcNow.AddSeconds(25);while(c.Satiety<90&&DateTime.UtcNow<end)await Task.Delay(100);SimulationPause.UserSpeed(0);Check(c.Satiety>=90&&!c.IsDeparting,"Floor meal completes and restores satiety");
 }catch(Exception e){File.AppendAllText(Log,"ERROR "+e+"\n");}finally{SimulationPause.UserSpeed(0);UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;File.AppendAllText(Log,"RESTORED\n");}}
}
