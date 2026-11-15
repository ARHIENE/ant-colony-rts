using System;using System.IO;using System.Linq;using System.Threading.Tasks;
using AntColony.Core;using AntColony.Boss;using AntColony.Units;using AntColony.Save;using AntColony.UI;using UnityEngine;using UnityEngine.AI;
public static class MothershipFight1115
{
 const string Log="work/campaign-complete-1115/ordinary-fight.txt";
 public static string Main(){File.WriteAllText(Log,"Loaded existing checkpoint. No health, damage, equipment or position injection. Ordinary attack and dodge movement orders only.\n");_=Run();return "Ordinary final-boss combat started";}
 static async Task Ready(){var end=DateTime.UtcNow.AddSeconds(90);while(SaveSystem.Busy&&DateTime.UtcNow<end)await Task.Delay(100);if(SaveSystem.Busy)throw new Exception("Load timed out");}
 static float EscapeDistance(MothershipEncounter.State s,Vector3 from,Vector3 to)
 {for(int step=1;step<=20;step++)if(!MothershipEncounter.Contains(s,Vector3.Lerp(from,to,step/20f)))return Vector3.Distance(from,to)*step/20f;return 100;}
 public static string Resume(){File.AppendAllText(Log,"RESUME current encounter unchanged after wall-clock deadline.\n");_=Run(true);return "Resumed existing encounter";}
 public static string Prepared(){File.WriteAllText(Log,"Prepared equipment fixture: standard quality-2 sword and armor. Checkpoint health and boss HP unchanged. Ordinary attacks and movement only after setup.\n");_=Run(false,true);return "Prepared equipment combat started";}
 static async Task Run(bool resume=false,bool standardGear=false)
 {
  var root=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"OrdinaryFinal1115-"+Guid.NewGuid().ToString("N"));
  try {
   var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false;UserSettings.Apply(quiet,false);
   string error=null; await Ready();if(!resume && !SaveSystem.TryLoad(Path.GetFullPath("work/campaign-complete-1115/mothership-save.json"),out error))throw new Exception(error);await Ready();
   var campaign=RocketCampaign.Instance;var boss=campaign.MothershipBoss;var crew=campaign.MothershipCrew.ToArray();
   if(standardGear)foreach(var c in crew){c.PersonalState.equipment.RemoveAll(e=>e.slot==EquipmentSlot.Weapon||e.slot==EquipmentSlot.Armor);c.PersonalState.equipment.Add(EquipmentRecipes.Create(EquipmentRecipe.Sword,2));c.PersonalState.equipment.Add(EquipmentRecipes.Create(EquipmentRecipe.Coating,2));}
   GameMenuController.Instance.Resume();UnityEditor.EditorApplication.isPaused=false;SimulationPause.UserSpeed(3);
   var encounter=boss.GetComponent<MothershipEncounter>();int lastPattern=-2;float lastReport=-30;var end=DateTime.UtcNow.AddMinutes(12);
   File.AppendAllText(Log,$"START boss={boss.CurrentHp}/{boss.MaxHp}; crew={string.Join(",",crew.Select(c=>c.CommanderName+":"+c.PersonalHealth+" attack="+c.AttackDamage))}\n");
   while(!campaign.MothershipVictory && crew.Any(c=>c&&!c.IsDead&&c.PersonalHealth>0)&&DateTime.UtcNow<end)
   {
    var state=encounter.Capture();
    foreach(var c in crew.Where(c=>c&&!c.IsDead&&c.PersonalHealth>0&&c.CanReceiveOrders))
    {
     if(state.casting>=0 && MothershipEncounter.Contains(state,c.Position))
     {
      if(state.casting!=lastPattern || !c.IsFollowingMoveOrder)
      {
       var options=Enumerable.Range(1,24).SelectMany(step=>Enumerable.Range(0,24).Select(i=>c.Position+new Vector3(Mathf.Cos(i*Mathf.PI/12),0,Mathf.Sin(i*Mathf.PI/12))*step*.5f))
        .Where(p=>RocketCampaign.ShipBounds.Contains(p)&&!MothershipEncounter.Contains(state,p)&&!MothershipEncounter.Contains(state,Vector3.MoveTowards(p,state.origin,.4f)))
        .OrderBy(p=>(p-c.Position).sqrMagnitude).ThenBy(p=>(p-boss.Position).sqrMagnitude);
       bool moved=false;
       foreach(var p in options)if(NavMesh.SamplePosition(p,out var hit,2,NavMesh.AllAreas)&&!MothershipEncounter.Contains(state,hit.position)&&c.CanReach(hit.position)){c.CommandMove(hit.position);moved=true;File.AppendAllText(Log,$"DODGE {state.casting} from={c.Position} to={hit.position} speed={c.Agent.speed} stop={c.Agent.stoppingDistance} ready={c.CanReceiveOrders} constructing={c.IsConstructing}\n");break;}
       if(!moved)File.AppendAllText(Log,$"NO DODGE PATH {state.casting} at {c.Position} options={options.Count()}\n");
      }
     }
     else if(state.casting<0 && (lastPattern>=0||!c.IsInCombat))c.CommandAttack(boss);
    }
    lastPattern=state.casting;
    if(GameCalendar.GameSeconds-lastReport>=30){lastReport=GameCalendar.GameSeconds;File.AppendAllText(Log,$"time={lastReport:0} boss={boss.CurrentHp:0} crew={string.Join(",",crew.Select(c=>c?c.PersonalHealth.ToString("0"):"gone"))}\n");}
    await Task.Delay(75);
   }
   SimulationPause.UserSpeed(0);
   if(!campaign.MothershipVictory)throw new Exception("Ordinary combat did not win. Boss remaining="+boss.CurrentHp+" crew="+string.Join(";",crew.Select(c=>c?JsonUtility.ToJson(c.PersonalState):"gone"))+" deadline="+(DateTime.UtcNow>=end));
   if(!SaveSystem.TrySave(false,0,out error))throw new Exception(error);
   File.Copy(SaveSlots.PathFor(false,0),"work/campaign-complete-1115/ordinary-victory-save.json",true);
   File.AppendAllText(Log,"PASS final boss defeated only through normal attacks and dodge orders; victory saved.\n");
  }catch(Exception e){File.AppendAllText(Log,"FAIL "+e+"\n");}
  finally{SimulationPause.UserSpeed(0);UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;File.AppendAllText(Log,"RESTORED settings and save root\n");}
 }
}
