using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;
using AntColony.Core;using AntColony.Buildings;using AntColony.Data;using AntColony.Units;using AntColony.World;using AntColony.Save;using AntColony.UI;using UnityEngine;
public static class HandcartRepair1115 {
 const string Log="work/colony-polish-1115/handcart-repair.txt";
 static void Check(bool ok,string text){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new Exception(text);}
 public static string Main(){File.WriteAllText(Log,"Isolated damaged-cart and supplied-stock repair fixture.\n");_=Run();return "Repair check started";}
 static async Task Run(){var root=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();try{
  var quiet=settings.Clone();quiet.autoSaveEnabled=false;UserSettings.Apply(quiet,false);SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"CartRepair1115");
  var deadline=DateTime.UtcNow.AddSeconds(120);while(SaveSystem.Busy&&DateTime.UtcNow<deadline)await Task.Delay(100);
  Check(!SaveSystem.Busy,"Ready");SaveSystem.NewGame(new NewGameOptions{seed=261115,mapSize=MapSize.Small});
  deadline=DateTime.UtcNow.AddSeconds(120);while(SaveSystem.Busy&&DateTime.UtcNow<deadline)await Task.Delay(100);Check(!SaveSystem.Busy,"New isolated game");
  GameMenuController.Instance.Resume();SimulationPause.UserSpeed(0);foreach(var p in CommanderRoster.Instance.Commanders){p.CommandStop();p.SetJobEnabled(CommanderJobs.All,false);}
  var c=CommanderRoster.Instance.Commanders.First();c.SetJobEnabled(CommanderJobs.Repair,true);
  var cart=WorldMapManager.Instance.CreateTransport(false,c.Position+Vector3.right*2,handcart:true);
  var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{BuildingKind.Storage,UnitRole.Worker});
  var go=UnityEngine.Object.Instantiate(template,c.Position+Vector3.left*5,Quaternion.identity);go.SetActive(true);var store=go.GetComponent<Storage>();
  var rm=ResourceManager.Instance;rm.TrySpend(ResourceType.Wood,rm.GetAmount(ResourceType.Wood));rm.AddTo(store.Bin,ResourceType.Soil,30);
  cart.TakeDamage(45);float hp=cart.CurrentHealth;int soil=rm.GetAmount(ResourceType.Soil);
  Check(cart.WorkRepair(c,1)&&cart.CurrentHealth==hp&&rm.GetAmount(ResourceType.Soil)==soil,"Soil cannot repair a wooden handcart");
  rm.AddTo(store.Bin,ResourceType.Wood,20);int wood=rm.GetAmount(ResourceType.Wood);
  Check(cart.TryRepair(c),"Normal repair command accepted");SimulationPause.UserSpeed(3);
  deadline=DateTime.UtcNow.AddSeconds(60);while(cart.CurrentHealth<cart.MaxHealth&&DateTime.UtcNow<deadline)await Task.Delay(100);SimulationPause.UserSpeed(0);
  Check(cart.CurrentHealth==cart.MaxHealth&&rm.GetAmount(ResourceType.Wood)<wood&&rm.GetAmount(ResourceType.Soil)==soil,"Live repair consumes wood only");
  Check(cart.DepartureBlock(null).Contains("끌 수 있는 장수"),"Empty intact cart reports missing puller");
 }catch(Exception e){File.AppendAllText(Log,"ERROR "+e+"\n");}finally{SimulationPause.UserSpeed(0);UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;File.AppendAllText(Log,"RESTORED\n");}}
}
