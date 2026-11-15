using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;
using AntColony.Core;using AntColony.Buildings;using AntColony.Data;using AntColony.Units;using AntColony.World;using AntColony.Save;using AntColony.UI;using UnityEngine;
public static class HaulNeeds1115Checks {
 const string Log="work/colony-polish-1115/haul-needs.txt";
 static void Check(bool ok,string text){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new Exception(text);}
 public static string Main(){File.WriteAllText(Log,"Isolated supplied-cargo and research-assignment fixture; not natural progression.\n");_=Run();return "Hauling checks started";}
 static GameObject Spawn(BuildingKind kind,Vector3 p){var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{kind,UnitRole.Worker});var go=UnityEngine.Object.Instantiate(template,p,Quaternion.identity);go.SetActive(true);return go;}
 static async Task Run(){var root=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();try{
  var quiet=settings.Clone();quiet.autoSaveEnabled=false;UserSettings.Apply(quiet,false);SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"HaulNeeds1115");
  SaveSystem.NewGame(new NewGameOptions{seed=261115,mapSize=MapSize.Small});var deadline=DateTime.UtcNow.AddSeconds(120);while(SaveSystem.Busy&&DateTime.UtcNow<deadline)await Task.Delay(100);Check(!SaveSystem.Busy,"New isolated game ready");
  GameMenuController.Instance.Resume();SimulationPause.UserSpeed(0);foreach(var p in CommanderRoster.Instance.Commanders){p.CommandStop();p.SetJobEnabled(CommanderJobs.All,false);}
  var c=CommanderRoster.Instance.Commanders.First();c.SetJobEnabled(CommanderJobs.Hauling,true);
  var store=Spawn(BuildingKind.Storage,c.Position+Vector3.left*5).GetComponent<Storage>();var lab=Spawn(BuildingKind.ScienceLab,c.Position+Vector3.right*6).GetComponent<ScienceLab>();
  typeof(DiplomacyManager).GetMethod("DropFloor",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(ResourceType),typeof(int),typeof(Vector3)},null).Invoke(null,new object[]{ResourceType.PlantingMaterial,5,c.Position+Vector3.forward*3});var seed=ResourceNode.Available.First(n=>n.IsLooseCargo&&n.ResourceType==ResourceType.PlantingMaterial);
  lab.RestoreAssignment(1,c);Check(!c.TryDirectGather(null,out _)&&lab.Target==c&&c.ScienceAssignment==lab,"Rejected command preserves research assignment");
  Check(c.TryDirectGather(seed,out var reason)&&c.ScienceAssignment==null&&lab.Target==null&&c.CurrentResourceNode==seed,"Valid direct hauling replaces ordinary research: "+reason);
  c.CommandStop();typeof(WorkerAnt).GetMethod("RestoreCargo",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{5f,ResourceType.PlantingMaterial,null});
  c.PersonalState.meal.satiety=100;c.PersonalState.hygiene.hygiene=0;c.PersonalState.hygiene.retrySeconds=0;c.PersonalState.joy.joy=100;
  int before=ResourceManager.Instance.GetAmount(ResourceType.PlantingMaterial);c.TickDuty(.1f);
  Check(c.CarriedAmount==5&&!c.IsWashing,"Low hygiene preserves cargo until delivery");
  SimulationPause.UserSpeed(2);deadline=DateTime.UtcNow.AddSeconds(45);while(c.IsCarrying&&DateTime.UtcNow<deadline)await Task.Delay(100);SimulationPause.UserSpeed(0);
  Check(!c.IsCarrying&&ResourceManager.Instance.GetAmount(ResourceType.PlantingMaterial)>=before+5,"Normal movement deposits carried seed instead of dropping it");
  c.CommandStop();c.TickDuty(.1f);Check(c.IsWashing||c.Hygiene>=GameBalanceNeeds.TemporaryCap,"Hygiene still runs after delivery");
 }catch(Exception e){File.AppendAllText(Log,"ERROR "+e+"\n");}finally{SimulationPause.UserSpeed(0);UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;File.AppendAllText(Log,"RESTORED\n");}}
}
