using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using AntColony.Buildings;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;
public static class Pacing1111Probe {
 static string label="before"; static string log=""; static void Note(string s){log+=$"[{GameCalendar.GameSeconds:0}s] {s}\n";File.WriteAllText("Temp/pacing-1111-"+label+".txt",log);}
 static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
 static async Task Wait(Func<bool> condition){var end=DateTime.UtcNow.AddSeconds(60);while(!condition()&&DateTime.UtcNow<end)await Task.Delay(50);if(!condition())throw new Exception("scene timeout");}
 static bool Place(BuildingKind kind,Vector3 home){
 Physics.SyncTransforms(); var pc=SceneObjects.Any<BuildingPlacementController>();if(!pc.BeginPlacement(kind,UnitRole.Worker,null))return false;
 var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{pc.PendingKind,UnitRole.Worker});
 for(int radius=4;radius<=22;radius+=3)for(int i=0;i<24;i++){
 var a=i*Mathf.PI/12;var p=home+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;
 if(!NavMesh.SamplePosition(p,out var hit,2,NavMesh.AllAreas))continue;
 var pos=(Vector3)typeof(BuildingPlacementController).GetMethod("GetPlacementPosition",Hidden).Invoke(pc,new object[]{template,hit.position});
 if(!(bool)typeof(BuildingPlacementController).GetMethod("CellValid",Hidden).Invoke(pc,new object[]{pos}))continue;
 var result=pc.PlaceLine(new List<Vector3>{pos},new List<bool>{true});Note($"PLACE {kind}: {result} at {pos}");return result>0;
 }
 pc.CancelPlacement();Note("NO VALID PLACEMENT "+kind);return false;
 }
 public static async Task<string> Main(string runLabel, int startingCount, bool focused){ label=runLabel;
 if(!Application.isPlaying)throw new Exception("Play required"); log="";
 var settings=UserSettings.Current.Clone();var root=SaveStorage.RootOverride;
 SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"Preparation1111-"+Guid.NewGuid().ToString("N"));
 try{var quiet=settings.Clone();quiet.autoSaveEnabled=quiet.autoPauseDowned=quiet.autoPauseThreat=false;UserSettings.Apply(quiet,false);
 foreach(int count in new[]{startingCount}){
 await Wait(()=>!SaveSystem.Busy);UnityEngine.Random.InitState(261106);
 StartingCandidate[] team;int rolls=0;do{team=Enumerable.Range(0,count).Select(i=>StartingCandidate.Roll(i)).ToArray();rolls++;}while(StartingCandidate.Missing(team).Length>0&&rolls<100);
 SaveSystem.NewGame(new NewGameOptions{seed=261106,mapSize=MapSize.Small,biome=MapBiome.Forest,startingCount=count,startingTeam=team});
 await Wait(()=>!SaveSystem.Busy&&WaveDefense.Instance!=null);GameMenuController.Instance.Resume();Time.timeScale=0;await Task.Delay(150);
 var home=(Vector3)typeof(ResourceManager).GetProperty("HomePosition",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
 Note($"START {count} commanders rolls={rolls}; buildings="+string.Join(",",SceneObjects.All<BuildingBase>().Where(b=>b.isActiveAndEnabled&&b.Data!=null).Select(b=>b.name)));
 foreach(var type in new[]{ResourceType.Food,ResourceType.Wood,ResourceType.Soil}){
 var nodes=ResourceNode.Available.Where(n=>n.NeedsDesignation&&n.ResourceType==type&&!n.IsDepleted&&Vector3.Distance(n.transform.position,home)<50).OrderBy(n=>Vector3.Distance(n.transform.position,home)).Take(3).ToArray();foreach(var n in nodes)n.RequestWork();Note($"designated {type}={nodes.Length}");}
 foreach(var wreck in SceneObjects.All<CrashWreck>().Where(w=>w.isActiveAndEnabled).ToArray())Demolition.OrderDemolish(wreck);
 for(int i=0;i<count;i++)Place(BuildingKind.SleepingMat,home);
 Place(BuildingKind.Toilet,home);Place(BuildingKind.Storage,home);Place(BuildingKind.MushroomFarm,home);
 var w=WaveDefense.Instance;float previous=0;var times=new Dictionary<string,float>();float next=0;bool tower=false;bool prioritySet=false;bool foodPriority=false;bool labPlanned=false;bool researchStarted=false;bool assigned=false;var seen=new HashSet<BuildingBase>();var end=DateTime.UtcNow.AddMinutes(5);Time.timeScale=5;
 while(!w.InWave&&GameCalendar.GameSeconds<900&&DateTime.UtcNow<end){
 float dt=Mathf.Max(0,GameCalendar.GameSeconds-previous);previous=GameCalendar.GameSeconds; foreach(var c in CommanderRoster.Instance.Commanders.Where(c=>c!=null&&!c.IsDead)){string key=c.IsAsleep?"sleep":c.IsEating||c.IsWashing||c.IsPlaying||c.PersonalState.treating?"needs":c.IsGatheringAnimation&&c.CurrentResourceNode!=null&&c.CurrentResourceNode.IsRegrowing?"growth_wait":c.IsWorking||c.ServiceTarget!=null||c.ScienceAssignment!=null?"work":c.Agent!=null&&(c.Agent.hasPath||c.Agent.pathPending)?"move":"idle";times[key]=(times.TryGetValue(key,out var t)?t:0)+dt;}
if(GameCalendar.GameSeconds>=300&&!tower){tower=Place(BuildingKind.AcidTower,home);}
 foreach(var b in SceneObjects.All<BuildingBase>().Where(b=>b.isActiveAndEnabled&&!b.IsDead&&b.Data!=null))if(seen.Add(b))Note("COMPLETED "+b.Data.kind);
if(GameCalendar.GameSeconds>=30&&!prioritySet){prioritySet=true; foreach(var c in CommanderRoster.Instance.Commanders){c.SetJobPriority(CommanderJobs.Building,8);c.SetJobPriority(CommanderJobs.Hauling,7);}Note("PLAYER raises building=8 hauling=7 after initial gathering"); if(focused){foreach(var n in ResourceNode.Available.Where(n=>n.NeedsDesignation&&n.WorkRequested&&n.ResourceType==ResourceType.Soil))WorkPriorities.Set(n,8,false);Note("PLAYER prioritizes designated soil for queued construction");}}
if(focused){
 var rm=ResourceManager.Instance;
 if(!foodPriority&&rm.GetAmount(ResourceType.Soil)>=40){foodPriority=true;foreach(var n in ResourceNode.Available.Where(n=>n.NeedsDesignation&&n.WorkRequested)){if(n.ResourceType==ResourceType.Soil)WorkPriorities.Set(n,5,false);if(n.ResourceType==ResourceType.Food)WorkPriorities.Set(n,8,false);}Note("PLAYER switches designated gathering to food for research");}
 if(foodPriority&&!labPlanned&&rm.GetAmount(ResourceType.Food)>=50&&rm.GetAmount(ResourceType.Soil)>=50){labPlanned=Place(BuildingKind.ScienceLab,home);Note("PLAYER invests surplus in research building"); if(labPlanned) { foreach(var n in ResourceNode.Available.Where(n=>n.NeedsDesignation&&n.WorkRequested)){if(n.ResourceType==ResourceType.Food)WorkPriorities.Set(n,5,false);if(n.ResourceType==ResourceType.Wood)WorkPriorities.Set(n,8,false);}Note("PLAYER prioritizes wood missing from research construction"); }}
 var lab=SceneObjects.All<ScienceLab>().FirstOrDefault(l=>l.isActiveAndEnabled&&!l.IsDead);
 if(lab!=null&&!researchStarted&&CampaignResearch.Instance.TryStart(ScienceTechnology.Resin)){researchStarted=true;Note("PLAYER selects earth wall research using earned stock");}
 if(researchStarted&&!assigned&&lab!=null)foreach(var c in CommanderRoster.Instance.Commanders){if(c.SendToResearch(lab)){assigned=true;Note("PLAYER assigns researcher "+c.CommanderName);break;}}
}
if(GameCalendar.GameSeconds>=next){next+=60;
 Note($"team={count} food={ResourceManager.Instance.GetAmount(ResourceType.Food)} soil={ResourceManager.Instance.GetAmount(ResourceType.Soil)} wood={ResourceManager.Instance.GetAmount(ResourceType.Wood)} buildings="+string.Join(",",SceneObjects.All<BuildingBase>().Where(b=>b.isActiveAndEnabled&&!b.IsDead&&b.Data!=null).Select(b=>b.Data?.kind.ToString())));
 foreach(var c in CommanderRoster.Instance.Commanders.Where(c=>c!=null&&!c.IsDead))Note($"{c.name} hp={c.PersonalHealth:0} mood={c.Mood:0} down={c.Health.Downed} sleep={c.IsAsleep} activity={c.CurrentActivity} node={c.CurrentResourceNode?.name} left={c.CurrentResourceNode?.AmountRemaining:0} delivery={c.DeliverySite!=null} build={c.ConstructionTarget?.BuildingKind}");
 foreach(var site in SceneObjects.All<BuildingConstructionSite>().Where(s=>s.isActiveAndEnabled))Note($"site={site.BuildingKind} wait={site.WaitReason} work={site.RemainingWork:0}");
 }
 if(GameMenuController.Instance.ScreenName=="COLONY LOST")break;
 await Task.Delay(250);
 }
 Note("COMMANDER_SECONDS "+string.Join(", ",times.Select(p=>p.Key+"="+p.Value.ToString("0"))));
Note($"END team={count} wave={w.Wave} remaining={w.RemainingEnemies} screen={GameMenuController.Instance.ScreenName}");Time.timeScale=0;
 }
 return log;
 }finally{Time.timeScale=0;UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;}
 }
}
