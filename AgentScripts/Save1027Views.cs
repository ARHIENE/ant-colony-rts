using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using AntColony.Save;
using UnityEngine;
using UnityEngine.AI;
using R=AntColony.Data.ResourceType;
using B=AntColony.Data.BuildingKind;
using Object=UnityEngine.Object;
public static class Save1027Views
{
 static async Task Wait(Func<bool> ready,int seconds=90){var until=DateTime.UtcNow.AddSeconds(seconds);while(!ready()&&DateTime.UtcNow<until)await Task.Delay(80);}
 static T Put<T>(B kind,Vector3 at)where T:BuildingBase{var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{kind,AntColony.Data.UnitRole.Worker});var go=Object.Instantiate(template,at,Quaternion.identity);go.name=kind.ToString();go.SetActive(true);return go.GetComponent<T>();}
 public static async Task<string> Prepare()
 {
  Directory.CreateDirectory("Temp/save-20261027");File.WriteAllText("Temp/save-20261027/settings-before.json",JsonUtility.ToJson(UserSettings.Current));File.WriteAllText("Temp/save-20261027/root-before.txt",SaveStorage.RootOverride??"");SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"Save27Showcase-"+Guid.NewGuid().ToString("N"));var config=UserSettings.Current.Clone();config.autoSaveEnabled=false;config.autoPauseThreat=config.autoPauseDowned=config.autoPauseRocket=false;UserSettings.Apply(config,false);
  await Wait(()=>!SaveSystem.Busy);StartingCandidate[] team;do{team=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray();}while(StartingCandidate.Missing(team).Length>0);foreach(var t in team){t.traits.values.Clear();t.ageYears=20;}
  SaveSystem.NewGame(new NewGameOptions{seed=271027,mapSize=MapSize.Small,startingCount=3,startingTeam=team});await Wait(()=>!SaveSystem.Busy&&CommanderRoster.Instance.Commanders.Count==3);Time.timeScale=0;AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;GameSession.Instance.MarkStarted(gameSeconds:20);
  var crew=CommanderRoster.Instance.Commanders;foreach(var c in crew){c.CommandStop();c.SetJobEnabled(CommanderJobs.All,false);c.PersonalState.meal=new CommanderMealState();c.PersonalState.sleep=new CommanderSleepState();c.PersonalState.hygiene=new CommanderHygieneState();c.PersonalState.joy=new CommanderJoyState();}
  var home=crew[0].Position;var tech=CampaignResearch.Instance.CaptureState();tech.completed.Add((int)ScienceTechnology.UndergroundSurvey);CampaignResearch.Instance.RestoreState(tech);
  var world=WorldMapManager.Instance;world.Assessment.AssignEvaluator(crew[0]);world.Assessment.Tick(30);
  NavMesh.SamplePosition(home+Vector3.right*8,out var hit,6,NavMesh.AllAreas);var at=new Vector3(Mathf.Floor(hit.position.x)+.5f,hit.position.y,Mathf.Floor(hit.position.z)+.5f);
  var smelter=Put<Processor>(B.GasSmelter,at);var store=Put<Storage>(B.Storage,at+Vector3.left*3);ResourceManager.Instance.AddTo(store.Bin,R.TitaniumOre,4);ResourceManager.Instance.AddTo(store.Bin,R.Coal,20);smelter.AddOrder(0,Workshop.OrderMode.KeepStock,10);smelter.Deliver(new object(),R.TitaniumOre,2);ResourceManager.Instance.TakeFrom(store.Bin,R.TitaniumOre,2);
  Put<SupplyFacility>(B.GasPipe,at+Vector3.right);Put<SupplyFacility>(B.GasTank,at+Vector3.right*2).Fill(R.NaturalGas,70);var generator=Put<PowerNode>(B.GasGenerator,at+Vector3.forward);Put<PowerNode>(B.PowerWire,at+Vector3.forward*2);Put<PowerNode>(B.Battery,at+Vector3.forward*3);Put<SupplyFacility>(B.GasPipe,at+Vector3.right+Vector3.forward);
  smelter.Powered=true;crew[1].SetJobEnabled(CommanderJobs.Crafting,true);Time.timeScale=1;smelter.Work(crew[1],1);Time.timeScale=0;
  var survey=UndergroundSurvey.Instance;var data=survey.Capture();var layer=data.layers.First(l=>l.map==0&&l.type>=0);layer.type=(int)R.StellarOre;layer.total=80;layer.remaining=80;layer.work=300;layer.discovered=true;layer.discoveryPending=false;layer.position=at+Vector3.forward*8;survey.Restore(data);
  var scanner=Put<UndergroundScanner>(B.UndergroundScanner,at+Vector3.left*5);crew[2].SetJobEnabled(CommanderJobs.Research,true);scanner.RequestAssignment(crew[2]);
  foreach(var n in ResourceNode.Available.Where(n=>n.NeedsDesignation&&!n.IsDepleted&&!n.GatheringForbidden&&n.IsUnlocked).Take(3))n.RequestWork();
  SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.FocusOn(at);
  var gameView=UnityEditor.EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));gameView.Focus();
  return "Prepared isolated real runtime showcase; "+Screen.width+"x"+Screen.height;
 }
 public static string Show(string screen)
 {
  var menu=AntColony.UI.GameMenuController.Instance;
  if(screen=="assessment")menu.ShowAssessment();
  else if(screen=="investigation")menu.ShowInvestigation();
  else if(screen=="survey")menu.ShowSurvey(SceneObjects.All<UndergroundScanner>().First());
  else if(screen=="industry")menu.ShowProcessor(SceneObjects.All<Processor>().First(p=>p.Data.kind==B.GasSmelter));
  else if(screen=="work")menu.ShowTasks(AntColony.UI.GameMenuController.TaskCandidates().ToList());
  else if(screen=="world"){menu.Resume();Time.timeScale=1;}
  Canvas.ForceUpdateCanvases();return screen+" ready";
 }
 public static async Task<string> Capture(string name)
 {
  var path=Path.GetFullPath("Temp/save-20261027/"+name+".png");Directory.CreateDirectory(Path.GetDirectoryName(path));if(File.Exists(path))File.Delete(path);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(path);await Wait(()=>File.Exists(path)&&new FileInfo(path).Length>0,10);return path+" "+new FileInfo(path).Length;
 }
 public static async Task<string> WorkDemo()
 {
  var menu=AntColony.UI.GameMenuController.Instance;menu.Resume();Time.timeScale=0;
  var c=CommanderRoster.Instance.Commanders.First();c.CommandStop();c.SetJobEnabled(CommanderJobs.All,false);c.SetJobEnabled(CommanderJobs.Building,true);c.PersonalState.toolTier=2;
  NavMesh.SamplePosition(c.Position+Vector3.left*5,out var ground,5,NavMesh.AllAreas);
  var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{B.Carpentry,AntColony.Data.UnitRole.Worker});var completed=Object.Instantiate(template,ground.position,Quaternion.identity);completed.name="Carpentry";
  var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Construction capture";go.transform.position=ground.position;go.transform.localScale=new Vector3(2,.2f,2);go.GetComponent<Collider>().isTrigger=true;var site=go.AddComponent<BuildingConstructionSite>();site.Initialize(completed,180);c.CommandBuild(site);
  var cam=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(cam,new object[]{ground.position,0f,8f});
  Time.timeScale=1;await Wait(()=>c.IsConstructing&&site.RemainingWork<178,20);Time.timeScale=0;return "Real construction work="+c.IsConstructing+" remaining="+site.RemainingWork;
 }
 public static string Finish(){Time.timeScale=0;var root=File.ReadAllText("Temp/save-20261027/root-before.txt");SaveStorage.RootOverride=string.IsNullOrEmpty(root)?null:root;UserSettings.Apply(JsonUtility.FromJson<UserSettingsData>(File.ReadAllText("Temp/save-20261027/settings-before.json")),false);return "Capture sandbox restored";}
}
