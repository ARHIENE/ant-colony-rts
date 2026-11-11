using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using AntColony.Save;
using AntColony.UI;
public static class CombatOrder1112Checks {
 public static async Task<string> Run(){
 var settings=UserSettings.Current.Clone();var root=SaveStorage.RootOverride;string log="";
 try {
 var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false;UserSettings.Apply(quiet,false);SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"CombatOrder1112-"+Guid.NewGuid().ToString("N"));
 for(int i=0;i<600&&(SaveSystem.Busy||!GameSession.Exists||GameMenuController.Instance==null);i++)await Task.Delay(50);
 SaveSystem.NewGame(new NewGameOptions{seed=261106,mapSize=MapSize.Small,biome=MapBiome.Forest,startingCount=3});
 for(int i=0;i<600&&SaveSystem.Busy;i++)await Task.Delay(50);
 GameMenuController.Instance.Resume();Time.timeScale=0;await Task.Delay(200);
 var team=CommanderRoster.Instance.Commanders.Where(c=>c!=null&&!c.IsDead).ToArray();var c=team[0];
 var field=typeof(SoldierAnt).GetField("currentTarget",BindingFlags.Instance|BindingFlags.NonPublic);
 var dummy=new GameObject("Destroyed target regression").AddComponent<WildMonster>();field.SetValue(c,dummy);
 UnityEngine.Object.DestroyImmediate(dummy.gameObject);
 typeof(CommanderAnt).GetMethod("TickFormation",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{.02f});
 typeof(SoldierAnt).GetMethod("TickCombat",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
 c.CommandStop();log+="Destroyed target formation update: PASS\n";
 WaveDefense.Instance.StartNow();SimulationPause.UserSpeed(1);
 var local=SceneObjects.Any<LocalIncursions>();var camera=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();camera.enabled=false;
 var home=team.Select(x=>x.Position).Aggregate(Vector3.zero,(a,b)=>a+b)/team.Length;
 var until=DateTime.UtcNow.AddSeconds(110);float start=GameCalendar.GameSeconds;bool ordered=false;float next=0;
 while(WaveDefense.Instance.InWave&&DateTime.UtcNow<until&&GameMenuController.Instance.ScreenName!="COLONY LOST"){
 var alive=local.Visitors.Where(v=>v!=null&&!v.IsDead&&!v.Retreating).ToArray();
 if(!ordered&&alive.Any(v=>Vector3.Distance(v.Position,home)<20)){
  var point=alive.OrderBy(v=>Vector3.Distance(v.Position,home)).First().Position;
  foreach(var fighter in team)fighter.CommandAttackMove(point);ordered=true;camera.FocusOn(point);
  SceneObjects.Any<SelectionManager>().SelectOnly(team[0].GetComponent<SelectableObject>());
  await Task.Delay(100);
  var button=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude).First(b=>b.name=="Priority Work");button.onClick.Invoke();
  if(!SceneObjects.Any<AttackMoveController>().IsAttackMode)throw new Exception("Civilian attack button did not activate");
  log+="Civilian attack button: PASS; one attack-move order each\n";
 }
 if(GameCalendar.GameSeconds-start>=next){next+=15;log+=$"{GameCalendar.GameSeconds-start:0}s remaining={WaveDefense.Instance.RemainingEnemies} hp="+string.Join(",",team.Select(x=>x.PersonalHealth.ToString("0")))+"\n";ScreenCapture.CaptureScreenshot("Temp/first-defense-1112/combat-order-"+(int)next+".png");File.WriteAllText("Temp/first-defense-1112/combat-order.txt",log);}
 await Task.Delay(250);
 }
 log+=$"Result cleared={!WaveDefense.Instance.InWave} elapsed={GameCalendar.GameSeconds-start:0}s\n";
 File.WriteAllText("Temp/first-defense-1112/combat-order.txt",log);
 if(WaveDefense.Instance.InWave)throw new Exception(log);return log;
 }finally{Time.timeScale=0;UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;}
 }
}
