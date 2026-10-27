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
public static class Save1028Views
{
 static async Task Wait(Func<bool> ready,int seconds=90){var end=DateTime.UtcNow.AddSeconds(seconds);while(!ready()&&DateTime.UtcNow<end)await Task.Delay(80);}
 static T Put<T>(B kind,Vector3 at)where T:BuildingBase{var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{kind,AntColony.Data.UnitRole.Worker});var go=Object.Instantiate(template,at,Quaternion.identity);go.name=kind.ToString();go.SetActive(true);return go.GetComponent<T>();}
 static ResourceNode Node(R type,Vector3 at,bool loose){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.SetActive(false);go.name="SAVE review "+type;go.transform.position=at+Vector3.up*.3f;go.transform.localScale=Vector3.one*.6f;go.GetComponent<Collider>().isTrigger=true;var n=go.AddComponent<ResourceNode>();if(loose)n.ConfigureLoot(type,7);else n.ConfigureNatural(type,150,0);go.SetActive(true);return n;}
 static void View(Vector3 at,float size=8){var cam=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();cam.enabled=false;typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(cam,new object[]{at,0f,size});UnityEditor.EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();}
 public static async Task<string> Prepare()
 {
  Directory.CreateDirectory("Temp/save-20261028");File.WriteAllText("Temp/save-20261028/settings-before.json",JsonUtility.ToJson(UserSettings.Current));File.WriteAllText("Temp/save-20261028/root-before.txt",SaveStorage.RootOverride??"");
  SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"SAVE28-review-"+Guid.NewGuid().ToString("N"));var config=UserSettings.Current.Clone();config.autoSaveEnabled=false;config.autoPauseThreat=config.autoPauseDowned=config.autoPauseRocket=false;UserSettings.Apply(config,false);
  await Wait(()=>!SaveSystem.Busy);StartingCandidate[] team;do{team=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray();}while(StartingCandidate.Missing(team).Length>0);foreach(var t in team){t.ageYears=20;t.traits.values.Clear();}
  SaveSystem.NewGame(new NewGameOptions{seed=281028,mapSize=MapSize.Small,startingCount=3,startingTeam=team});await Wait(()=>!SaveSystem.Busy&&CommanderRoster.Instance.Commanders.Count==3);AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;GameSession.Instance.MarkStarted(gameSeconds:20);
  var crew=CommanderRoster.Instance.Commanders;foreach(var c in crew){c.CommandStop();c.SetJobEnabled(CommanderJobs.All,false);c.PersonalState.sleep=new CommanderSleepState();c.PersonalState.meal=new CommanderMealState();c.PersonalState.hygiene=new CommanderHygieneState();c.PersonalState.joy=new CommanderJoyState();}
  var home=crew[0].Position;var at=new Vector3(Mathf.Floor(home.x)+.5f,home.y,Mathf.Floor(home.z)+.5f);var tech=CampaignResearch.Instance.CaptureState();tech.completed.Add((int)ScienceTechnology.UndergroundSurvey);CampaignResearch.Instance.RestoreState(tech);
  foreach(var n in ResourceNode.Available.Where(n=>(n.transform.position-at).sqrMagnitude<150&&!n.IsLooseCargo).ToArray()){n.gameObject.SetActive(false);Object.Destroy(n.gameObject);}
  var chem=Put<Processor>(B.ChemistryBench,at+Vector3.left*3);Put<SupplyFacility>(B.FluidPipe,chem.Position+Vector3.right*2);Put<SupplyFacility>(B.FluidTank,chem.Position+Vector3.right*3).Fill(R.CrudeOil,70);
  var store=Put<Storage>(B.Storage,chem.Position-Vector3.forward*3);ResourceManager.Instance.AddTo(store.Bin,R.Coal,30);ResourceManager.Instance.AddTo(store.Bin,R.Drum,3);ResourceManager.Instance.AddTo(store.Bin,R.GasCylinder,3);
  var steam=Put<PowerNode>(B.SteamGenerator,chem.Position+Vector3.forward*2);steam.RestoreFuel(10);Put<PowerNode>(B.PowerWire,chem.Position+Vector3.forward);PowerGrid.Tick(.5f);chem.AddOrder(0,Workshop.OrderMode.KeepStock,10);crew[0].SetJobEnabled(CommanderJobs.Crafting,true);crew[0].StartService(chem,CommanderJobs.Crafting);crew[0].Agent.Warp(chem.Position-Vector3.forward*1.4f);crew[0].Agent.ResetPath();Time.timeScale=1;chem.Powered=true;chem.Work(crew[0],1);Time.timeScale=0;
  var charger=Put<SupplyFacility>(B.FuelCharger,at+Vector3.right*6+Vector3.forward*4);charger.SetContainerMode(2);Put<SupplyFacility>(B.GasTank,charger.Position+Vector3.right).Fill(R.NaturalGas,60);charger.AddContainerOrder(5);charger.Deliver(new object(),R.GasCylinder,1);crew[1].SetJobEnabled(CommanderJobs.Crafting,true);crew[1].StartService(charger,CommanderJobs.Crafting);crew[1].Agent.Warp(charger.Position-Vector3.forward);crew[1].Agent.ResetPath();Time.timeScale=1;charger.WorkContainers(crew[1],1);Time.timeScale=0;
  var refinery=Put<SupplyFacility>(B.OilRefinery,chem.Position+Vector3.left*4);Put<SupplyFacility>(B.FluidTank,refinery.Position+Vector3.left).Fill(R.CrudeOil,40);Put<PowerNode>(B.PowerWire,refinery.Position+Vector3.right);Put<PowerNode>(B.PowerWire,refinery.Position+Vector3.right*2);Put<PowerNode>(B.PowerWire,refinery.Position+Vector3.right*3);PowerGrid.Tick(.5f);
  var types=new[]{R.Chitin,R.Wax,R.Component,R.Water};for(int i=0;i<types.Length;i++)Node(types[i],at+new Vector3(1+i*1.6f,0,-1),true);Node(R.Chitin,at+new Vector3(2,0,3),false);Node(R.Soil,at+new Vector3(4,0,3),false);
  View(at+Vector3.right*1.2f,8);await Task.Delay(300);Time.timeScale=0;return "Prepared isolated SAVE28 real game view "+Screen.width+"x"+Screen.height;
 }
 public static async Task<string> Show(string screen)
 {
  var menu=AntColony.UI.GameMenuController.Instance;
  if(screen=="chemical")menu.ShowProcessor(SceneObjects.All<Processor>().First(p=>p.Chemical));
  else if(screen=="containers")menu.ShowContainers(SupplyFacility.All.First(s=>s.Kind==B.FuelCharger));
  else if(screen=="work")menu.ShowTasks(AntColony.UI.GameMenuController.TaskCandidates().ToList());
  else if(screen=="refinery"){menu.Resume();Time.timeScale=0;var b=SupplyFacility.All.First(s=>s.Kind==B.OilRefinery);AntColony.UI.WorkTargetPanel.Select(b);View(b.Position+Vector3.right*2,8);await Task.Delay(150);var detail=Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(x=>x.name=="V6 Details");detail?.onClick.Invoke();}
  else {menu.Resume();Time.timeScale=0;AntColony.UI.WorkTargetPanel.Clear();SceneObjects.Any<SelectionManager>()?.ClearSelection();var water=ResourceNode.Available.First(n=>n.name.StartsWith("SAVE review")&&n.ResourceType==R.Water);var c=CommanderRoster.Instance.Commanders.Last();c.CommandStop();c.Agent.Warp(water.transform.position-Vector3.forward*3);c.Agent.ResetPath();View(water.transform.position+Vector3.left*3+Vector3.forward*1.5f,8);}
  Canvas.ForceUpdateCanvases();UnityEditor.EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();await Task.Delay(250);return screen+" ready";
 }
 public static async Task<string> Capture(string name)
 {
  var path=Path.GetFullPath("Temp/save-20261028/"+name+".png");Directory.CreateDirectory(Path.GetDirectoryName(path));if(File.Exists(path))File.Delete(path);UnityEditor.EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();Canvas.ForceUpdateCanvases();await Task.Delay(200);ScreenCapture.CaptureScreenshot(path);await Wait(()=>File.Exists(path)&&new FileInfo(path).Length>0,10);return path+" "+new FileInfo(path).Length;
 }
 public static string Finish(){Time.timeScale=0;var root=File.ReadAllText("Temp/save-20261028/root-before.txt");SaveStorage.RootOverride=string.IsNullOrEmpty(root)?null:root;UserSettings.Apply(JsonUtility.FromJson<UserSettingsData>(File.ReadAllText("Temp/save-20261028/settings-before.json")),false);SceneObjects.Any<AntColony.Camera.IsometricCameraController>().enabled=true;return "SAVE28 settings/root restored";}
}
