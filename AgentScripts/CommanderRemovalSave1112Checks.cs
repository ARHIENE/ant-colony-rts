using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Units;
using System.Reflection;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;
public static class CommanderRemovalSave1112Checks {
 static async Task Wait(Func<bool> done){var end=DateTime.UtcNow.AddSeconds(60);while(!done()&&DateTime.UtcNow<end)await Task.Delay(80);if(!done())throw new Exception("wait timeout");}
 public static async Task<string> Main(){
 if(!Application.isPlaying)throw new Exception("Play required");var root=SaveStorage.RootOverride;var settings=UserSettings.Current.Clone();
 SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"FunWaveSave-"+Guid.NewGuid().ToString("N"));
 try{var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false;UserSettings.Apply(quiet,false);
 await Wait(()=>!SaveSystem.Busy);SaveSystem.NewGame(new NewGameOptions{seed=261106,mapSize=MapSize.Small});await Wait(()=>!SaveSystem.Busy);GameMenuController.Instance.Resume();Time.timeScale=4;
 if(!WaveDefense.Instance.StartNow())throw new Exception("wave start failed");await Wait(()=>SceneObjects.Any<LocalIncursions>().Visitors.OfType<EnemyCommander>().Any());Time.timeScale=0;
 var leader=SceneObjects.Any<LocalIncursions>().Visitors.OfType<EnemyCommander>().Single();if(!leader.Talents.Validate())throw new Exception("invalid leader talents");
 var commander=CommanderRoster.Instance.Commanders.First(c=>c.IsColonyMember);
 commander.Traits.values.Clear();commander.Traits.values.Add(CommanderTrait.Legion);
 commander.Traits.specialties=null;
 commander.Talents.levels[(int)CommanderActivity.Command]=7;
 commander.PersonalState.formation.selected=Formation.Arrow;
 ColonyPopulation.Instance.S.taxFood=123;
 typeof(AntPool).GetMethod("RestoreCounts",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(AntPool.Instance,new object[]{70,20,10});
 var rs=CampaignResearch.Instance.CaptureState();rs.active=(int)ScienceTechnology.FormationFocus;rs.progress=1;CampaignResearch.Instance.RestoreState(rs);
 if(!SaveSystem.TrySave(false,0,out var error))throw new Exception("save: "+error);
 if(!SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error))throw new Exception("load: "+error);await Wait(()=>!SaveSystem.Busy);Time.timeScale=0;
 leader=SceneObjects.Any<LocalIncursions>().Visitors.OfType<EnemyCommander>().Single();if(!leader.Talents.Validate()||!WaveDefense.Instance.InWave||WaveDefense.Instance.RemainingEnemies!=15)throw new Exception("wave/leader roundtrip failed");
 commander=CommanderRoster.Instance.Commanders.First(c=>c.IsColonyMember);
 if(!commander.Traits.values.Contains(CommanderTrait.Legion)||commander.HasLegion||commander.Talents.levels[8]!=7||commander.SelectedFormation!=Formation.Plain||commander.PersonalState.formation.selected!=Formation.Arrow)throw new Exception("legacy commander archive mismatch");
 if(ColonyPopulation.Instance.Total!=0||ColonyPopulation.Instance.S.taxFood!=123||AntPool.Instance.Free!=0||CampaignResearch.Instance.Active!=null||!CampaignResearch.Instance.CaptureState().partialIds.Contains((int)ScienceTechnology.FormationFocus))throw new Exception("legacy economy/research archive mismatch");
 if((int)typeof(AntPool).GetField("LegacyAssigned",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(AntPool.Instance)!=20)throw new Exception("archived assigned count lost");
 return "PASS legacy command/legion/formation/citizen counts/tax/research actual save-load; first wave leader initialization, actual save, actual load, valid restored leader, active wave with 15 enemies";
 }finally{Time.timeScale=0;UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;}
 }
}