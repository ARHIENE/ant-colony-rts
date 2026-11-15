using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;
using AntColony.Core;using AntColony.Data;using AntColony.Buildings;using AntColony.Units;using AntColony.Save;using AntColony.UI;using UnityEngine;
public static class Alcohol1115Checks
{
 const string Log="work/colony-polish-1115/alcohol-checks.txt";
 static void Check(bool ok,string message){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new Exception(message);}
 static async Task Wait(Func<bool> done,int seconds=100){var end=DateTime.UtcNow.AddSeconds(seconds);while(!done()&&DateTime.UtcNow<end)await Task.Delay(25);Check(done(),"Wait completed");}
 public static string Main(){File.WriteAllText(Log,"Fixture inventory; real walk, pickup, drinking, save/reload.\n");_=Run();return "Started alcohol regression";}
 static async Task Run()
 {
  var root=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"Alcohol1115-"+Guid.NewGuid().ToString("N"));
  try {
   var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false;UserSettings.Apply(quiet,false);
   await Wait(()=>!SaveSystem.Busy);SaveSystem.NewGame(new NewGameOptions{seed=261115,mapSize=MapSize.Small});await Wait(()=>!SaveSystem.Busy);
   GameMenuController.Instance.Resume();SimulationPause.UserSpeed(0);
   foreach(var worker in CommanderRoster.Instance.Commanders){worker.CommandStop();worker.SetJobEnabled(CommanderJobs.All,false);}
   var c=CommanderRoster.Instance.Commanders.First();c.Traits.values.Clear();
   Check(!c.Alcohol.allowed,"Old/default state does not automatically drink");
   c.Alcohol.allowed=true;c.Alcohol.sinceDrink=GameCalendar.SecondsPerDay*3;
   var adult=c.PersonalState.ageMonths;c.PersonalState.ageMonths=12;Check(!c.RequestDrink(),"Children cannot drink");c.PersonalState.ageMonths=adult;
   Check(c.Traits.TryAdd(CommanderTrait.AlcoholWeak)&&!c.Traits.TryAdd(CommanderTrait.AlcoholStrong),"Tolerance traits mutually exclusive");c.Traits.values.Clear();
   var t=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{BuildingKind.Storage,UnitRole.Worker});
   var storage=UnityEngine.Object.Instantiate(t,c.Position+Vector3.right*8,Quaternion.identity);storage.SetActive(true);await Task.Delay(100);
   Check(ResourceManager.Instance.Add(ResourceType.Beer,3)==3,"Physical beer stocked");
   int before=ResourceManager.Instance.GetAmount(ResourceType.Beer);var origin=c.Position;
   Check(c.RequestDrink(),"Explicit drinking begins");Check(ResourceManager.Instance.GetAmount(ResourceType.Beer)==before,"Distant drink does not consume inventory");
   SimulationPause.UserSpeed(1);await Wait(()=>c.IsDrinking);SimulationPause.UserSpeed(0);
   Check(Vector3.Distance(origin,c.Position)>2,"Walks to stockpile before pickup");
   Check(ResourceManager.Instance.GetAmount(ResourceType.Beer)==before-1,"Exactly one portion picked up");
   Check(c.Alcohol.intoxication==0,"Finding or carrying drink does not relieve withdrawal or cause intoxication");
   var id=c.PersonalState.id;var remaining=c.Alcohol.drinking;
   Check(SaveSystem.TrySave(false,0,out var error),"Save held drink: "+error);Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"Reload held drink: "+error);await Wait(()=>!SaveSystem.Busy);
   c=CommanderRoster.Instance.Commanders.Single(x=>x.PersonalState.id==id);
   Check(c.IsDrinking&&Mathf.Abs(c.Alcohol.drinking-remaining)<.01f,"Drink portion and remaining action restored: "+JsonUtility.ToJson(c.Alcohol)+" expected seconds "+remaining);
   SimulationPause.UserSpeed(3);await Wait(()=>c.Alcohol.intoxication>0);SimulationPause.UserSpeed(0);
   Check(!c.IsDrinking&&ResourceManager.Instance.GetAmount(ResourceType.Beer)==before-1,"Loaded drink consumes no duplicate portion");
   Check(c.AlcoholFactor<1&&c.PersonalState.moodFactors.Any(f=>f.reason=="AlcoholRelaxed"),"Actual drinking affects mood and capabilities");
   c.Alcohol.intoxication=110;Check(!c.CanReceiveOrders&&c.AlcoholFactor==0,"Blackout prevents commands and work");c.Alcohol.intoxication=0;
   c.Traits.TryAdd(CommanderTrait.AlcoholDependent);c.Alcohol.sinceDrink=GameCalendar.SecondsPerDay*3;c.Alcohol.allowed=false;
   Check(c.AlcoholWithdrawal&&c.DrinkBlock(true)==""&&c.DrinkBlock()!="","Dependency bypasses prohibition while ordinary policy remains enforced");
   c.WorkState.duty=CommanderDuty.Deployed;
   Check(c.DrinkBlock(true)==""&&c.DrinkBlock()!="","Dependency bypasses draft while ordinary drinking cannot");
   c.Alcohol.retry=0;SimulationPause.UserSpeed(1);await Wait(()=>c.IsDrinking);SimulationPause.UserSpeed(0);
   Check(c.AlcoholWithdrawal,"Pickup alone does not relieve withdrawal");c.CommandStop();
   Check(!c.IsDrinking&&AntColony.World.ResourceNode.Available.Any(n=>n.IsLooseCargo&&n.ResourceType==ResourceType.Beer),"Interrupt returns held portion to real floor cargo");
   c.Alcohol.retry=0;SimulationPause.UserSpeed(1);await Wait(()=>!c.AlcoholWithdrawal);SimulationPause.UserSpeed(0);
   Check(c.IsDeployed&&c.Alcohol.intoxication>0,"Actual forced drinking relieves withdrawal and retains draft");
   c.Alcohol.intoxication=80;c.Alcohol.hangover=150;
   typeof(CommanderAnt).GetMethod("TickAlcohol",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(c,new object[]{75f});
   Check(Mathf.Abs(c.Alcohol.intoxication-60)<.01f&&Mathf.Abs(c.Alcohol.hangover-75)<.01f,"Half day clears 20 intoxication and 75 seconds hangover");
   Check(c.PersonalState.Validate(out error),"New personal state validates: "+error);
   Check(SaveSystem.TrySave(false,1,out error),"Dependency policy save: "+error);
  } catch(Exception e){File.AppendAllText(Log,"ERROR "+e+"\n");}
  finally{SimulationPause.UserSpeed(0);UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;File.AppendAllText(Log,"RESTORED settings and save root\n");}
 }
}
