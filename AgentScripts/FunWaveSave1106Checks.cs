using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;
public static class FunWaveSave1106Checks {
 static async Task Wait(Func<bool> done){var end=DateTime.UtcNow.AddSeconds(60);while(!done()&&DateTime.UtcNow<end)await Task.Delay(80);if(!done())throw new Exception("wait timeout");}
 public static async Task<string> Main(){
 if(!Application.isPlaying)throw new Exception("Play required");var root=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();
 SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"FunWaveSave-"+Guid.NewGuid().ToString("N"));
 try{var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false;UserSettings.Apply(quiet,false);
 await Wait(()=>!SaveSystem.Busy);SaveSystem.NewGame(new NewGameOptions{seed=261106,mapSize=MapSize.Small});await Wait(()=>!SaveSystem.Busy);GameMenuController.Instance.Resume();Time.timeScale=4;
 if(!WaveDefense.Instance.StartNow())throw new Exception("wave start failed");await Wait(()=>SceneObjects.Any<LocalIncursions>().Visitors.OfType<EnemyCommander>().Any());Time.timeScale=0;
 var leader=SceneObjects.Any<LocalIncursions>().Visitors.OfType<EnemyCommander>().Single();if(!leader.Talents.Validate())throw new Exception("invalid leader talents");
 if(!SaveSystem.TrySave(false,0,out var error))throw new Exception("save: "+error);
 if(!SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error))throw new Exception("load: "+error);await Wait(()=>!SaveSystem.Busy);Time.timeScale=0;
 leader=SceneObjects.Any<LocalIncursions>().Visitors.OfType<EnemyCommander>().Single();if(!leader.Talents.Validate()||!WaveDefense.Instance.InWave||WaveDefense.Instance.RemainingEnemies!=15)throw new Exception("wave/leader roundtrip failed");
 return "PASS first wave leader initialization, actual save, actual load, valid restored leader, active wave with 15 enemies";
 }finally{Time.timeScale=0;UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;}
 }
}