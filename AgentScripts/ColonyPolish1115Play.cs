using System;using System.IO;using System.Linq;using System.Threading.Tasks;
using AntColony.Core;using AntColony.Save;using AntColony.UI;using AntColony.Units;using AntColony.Buildings;using AntColony.World;using UnityEngine;
public static class ColonyPolish1115Play
{
 const string Dir="work/colony-polish-1115/";
 [Serializable]public class RestoreData{public UserSettingsData settings;public string root;public bool active;}
 public static async Task<string> Load()
 {
  if(!Application.isPlaying)throw new Exception("Play required");UnityEditor.EditorApplication.isPaused=false;
  var deadline=DateTime.UtcNow.AddSeconds(90);while(SaveSystem.Busy&&DateTime.UtcNow<deadline)await Task.Delay(100);
  if(SaveSystem.Busy)throw new Exception("Initial load timeout");Directory.CreateDirectory(Dir);
  if(File.Exists(Dir+"restore.json")&&JsonUtility.FromJson<RestoreData>(File.ReadAllText(Dir+"restore.json")).active)throw new Exception("Restore previous run first");
  var restore=new RestoreData{settings=UserSettings.Current.Clone(),root=SaveStorage.RootOverride,active=true};File.WriteAllText(Dir+"restore.json",JsonUtility.ToJson(restore,true));
  SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"ColonyPolish1115-"+Guid.NewGuid().ToString("N"));
  var settings=restore.settings.Clone();settings.autoSaveEnabled=settings.autoPauseThreat=settings.autoPauseDowned=false;UserSettings.Apply(settings,false);
  return await Reload();
 }
 public static async Task<string> Reload()
 {
  if(string.IsNullOrEmpty(SaveStorage.RootOverride))throw new Exception("Isolation required");
  if(!SaveSystem.TryLoad(Path.GetFullPath("work/release-flow-1114/checkpoint-save.json"),out var error))throw new Exception(error);
  var deadline=DateTime.UtcNow.AddSeconds(90);while(SaveSystem.Busy&&DateTime.UtcNow<deadline)await Task.Delay(100);
  if(SaveSystem.Busy)throw new Exception("Checkpoint load timeout");GameMenuController.Instance.Resume();SimulationPause.UserSpeed(0);
  return Status();
 }
 public static string Status()
 {
  var food=ColonyFoodStatus.Read();return $"time={GameCalendar.GameSeconds} wave={WaveDefense.Instance?.Wave} "+food.Summary+" / "+food.Bottlenecks+"\n"
   +string.Join("\n",CommanderRoster.Instance.Commanders.Select(c=>$"{c.CommanderName} hp={c.PersonalHealth:0} mood={c.Mood:0} satiety={c.Satiety:0} order={CommanderOverhead.Activity(c)}"))
   +"\n"+string.Join("\n",SceneObjects.All<BuildingBase>().Where(b=>b&&b.isActiveAndEnabled&&!b.IsDead&&b.Data).Select(b=>b.Data.kind+" "+b.Position));
 }
 public static string SetFoodPlan()
 {
  var log="Normal player orders; no resource/health grants\n";
  foreach(var bed in SceneObjects.All<StarterCropBed>().Where(b=>b.isActiveAndEnabled&&!b.IsDead))
   log+="Plant existing bed="+bed.Plant()+"\n";
  foreach(var kitchen in SceneObjects.All<Kitchen>().Where(k=>k.isActiveAndEnabled&&!k.IsTable&&!k.IsDead))
   foreach(var order in kitchen.Orders.Where(o=>o.mode==Workshop.OrderMode.KeepStock))order.amount=6;
  log+="Two meals per resident held, seed stock="+FarmSeeds.Count(FarmCrop.Fungus)+"\n";
  log+=string.Join(",",Enum.GetValues(typeof(AntColony.Data.ResourceType)).Cast<AntColony.Data.ResourceType>()
   .Where(t=>ResourceManager.Instance.GetAmount(t)>0).Select(t=>t+"="+ResourceManager.Instance.GetAmount(t)));
  File.AppendAllText(Dir+"play.txt",log+"\n");return log;
 }
 public static string Finish()
 {
  var restore=JsonUtility.FromJson<RestoreData>(File.ReadAllText(Dir+"restore.json"));if(!restore.active)return "Already restored";
  SimulationPause.UserSpeed(0);SaveStorage.RootOverride=restore.root;UserSettings.Apply(restore.settings,false);restore.active=false;
  File.WriteAllText(Dir+"restore.json",JsonUtility.ToJson(restore,true));return "User settings and save root restored";
 }
}
