using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using AntColony.Buildings;
using UnityEngine;
using UnityEngine.AI;
public static class FunEntry1106Checks {
 static string log=""; static void Note(string s){log+=s+"\n";File.WriteAllText("Temp/fun-entry-1106.txt",log);}
 static async Task Wait(Func<bool> condition){var end=DateTime.UtcNow.AddSeconds(60);while(!condition()&&DateTime.UtcNow<end)await Task.Delay(50);if(!condition())throw new Exception("scene timeout");}
 public static async Task<string> Main(){
 if(!Application.isPlaying)throw new Exception("Play required"); log="";
 var settings=UserSettings.Current.Clone();var root=SaveStorage.RootOverride;
 SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"FunEntry-"+Guid.NewGuid().ToString("N"));
 try{var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseDowned=quiet.autoPauseThreat=false;UserSettings.Apply(quiet,false);
 foreach(var biome in new[]{MapBiome.Forest,MapBiome.Waterside,MapBiome.Desert,MapBiome.City})foreach(var seed in new[]{261106,261107}){
 await Wait(()=>!SaveSystem.Busy);UnityEngine.Random.InitState(seed);
 SaveSystem.NewGame(new NewGameOptions{seed=seed,mapSize=MapSize.Small,biome=biome,startingCount=3,startingTeam=Enumerable.Range(0,3).Select(i=>StartingCandidate.Roll(i)).ToArray()});
 await Wait(()=>!SaveSystem.Busy&&WaveDefense.Instance!=null);GameMenuController.Instance.Resume();Time.timeScale=0;await Task.Delay(200);
 var local=SceneObjects.Any<LocalIncursions>();int ok=0;int partial=0;
 for(int i=0;i<12;i++) {if(local.TrySpawnWaveGroup(1,false)){ok++;var v=local.Visitors.Last();var home=GameManager.Instance.FindNearestPlayerBuilding(Vector3.zero);var path=new NavMeshPath();NavMesh.CalculatePath(v.Position,home.Position,NavMesh.AllAreas,path);if(path.status!=NavMeshPathStatus.PathComplete)partial++;} foreach(var v in local.Visitors.ToArray())if(v!=null&&!v.IsDead)v.Wound(1e9f);}
 Note($"{biome} seed={seed}: spawn {ok}/12 partial={partial}");
 if(biome==MapBiome.Forest&&seed==261106){var buildings=SceneObjects.All<BuildingBase>().Where(b=>b!=null&&b.isActiveAndEnabled).ToArray();try{foreach(var b in buildings)b.gameObject.SetActive(false);await Task.Delay(100);var spawned=local.TrySpawnWaveGroup(1,false);Note("NO BUILDINGS spawn="+spawned);if(!spawned)throw new Exception("no-building spawn failed");
var visitor=local.Visitors.Last();var start=Vector3.Distance(visitor.Position,HomeReturnPoint.Anchor);Time.timeScale=4;await Task.Delay(5000);Time.timeScale=0;
var finish=visitor==null||visitor.IsDead?0:Vector3.Distance(visitor.Position,HomeReturnPoint.Anchor);Note($"NO BUILDINGS approach={start:0.0}->{finish:0.0}");if(finish>=start-5)throw new Exception("no-building approach stalled");
foreach(var v in local.Visitors.ToArray())if(v!=null&&!v.IsDead)v.Wound(1e9f);}finally{foreach(var b in buildings)if(b!=null)b.gameObject.SetActive(true);} }
 }
 var last=SceneObjects.Any<LocalIncursions>(); if(!last.TrySpawnWaveGroup(1,true))throw new Exception("leader spawn failed"); if(!SaveSystem.TrySave(false,0,out var error))throw new Exception("wave leader save failed: "+error); Note("PASS actual save with independent wave commander"); return log;
 }finally{Time.timeScale=0;UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;}
 }
}