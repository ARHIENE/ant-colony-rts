using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Data;
using UnityEngine;
using UnityEngine.UI;
public static class UiPrototype1106Checks
{
 static int passed;
 static void Check(bool value,string name){if(!value)throw new Exception(name);passed++;}
 static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
 static GameMenuController Menu=>GameMenuController.Instance;
 static Button Button(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).First(b=>b.name==name);
 public static async Task<string> Prepare()
 {
  if(!Application.isPlaying)throw new Exception("Play required");
  SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"UI1106-"+Guid.NewGuid().ToString("N"));
  var quiet=UserSettings.Current.Clone();quiet.autoSaveEnabled=false;UserSettings.Apply(quiet,false);
  SaveSystem.NewGame(new NewGameOptions{seed=1106,mapSize=MapSize.Small,biome=MapBiome.Forest,startingCount=3,startingTeam=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray()});
  for(int i=0;i<300&& (SaveSystem.Busy||Menu==null||!GameSession.Exists||!GameSession.Instance.GameStarted);i++)await Task.Delay(100);
  if(SaveSystem.Busy||Menu==null||!GameSession.Exists||!GameSession.Instance.GameStarted)
   throw new Exception("Isolated UI world did not become ready within 30 seconds; verification aborted.");
  Menu.Resume();SimulationPause.UserSpeed(0);await Task.Delay(300);
  return "Isolated UI test world ready, autosave disabled; user slots untouched.";
 }
 public static async Task<string> Main()
 {
  passed=0;Menu.Resume();await Task.Delay(100);
  Check(!GameObject.Find("ResourceBar"),"legacy bar hidden");Check(!GameObject.Find("RosterBar"),"roster notch hidden");
  Check(GameObject.Find("PinnedResources"),"resource rail visible");Check(Button("Prototype Settings"),"separate settings button");
  var icons=UnityEngine.Object.FindObjectsByType<PrototypeResourceIcon>(FindObjectsInactive.Exclude);
  Check(icons.Length==PrototypeResourceHud.Pinned.Count&&icons.All(i=>i.GetComponent<CanvasRenderer>()!=null),"pinned resource icons have renderers");
  Menu.PrototypeResources();await Task.Delay(100);
  Check(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Count(b=>b.name.StartsWith("Pin "))==Enum.GetValues(typeof(ResourceType)).Length,"all resource types accessible");
  bool before=PrototypeResourceHud.Pinned.Contains(ResourceType.Iron);Button("Pin Iron").onClick.Invoke();await Task.Delay(50);
  Check(PrototypeResourceHud.Pinned.Contains(ResourceType.Iron)!=before,"pin toggles");Button("Pin Iron").onClick.Invoke();
  Menu.PrototypeOperations();Check(Button("Operations 생산"),"production route");Check(Button("Operations 치료"),"medical route");Check(Button("Operations 목장"),"ranch route");
  Menu.Science();await Task.Delay(100);
  Check(GameObject.Find("ResearchGraph"),"tree graph created");Check(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Count(b=>b.name.StartsWith("Tree "))==CampaignResearch.Technologies.Length,"all real technologies represented");
  Check(GameObject.Find("Prerequisite"),"prerequisite edges present");Check(Button("Research Assignments"),"researcher assignment accessible");
  var tree=GameObject.Find("ResearchTree").GetComponent<ScrollRect>();var startX=tree.horizontalNormalizedPosition;
  Button("Research Right").onClick.Invoke();Check(tree.horizontalNormalizedPosition>startX,"later research columns reachable");
  Button("Research Left").onClick.Invoke();Check(Math.Abs(tree.horizontalNormalizedPosition-startX)<.001f,"research navigation returns to starting column");
  Menu.Resume();await Task.Delay(100);Button("Prototype Settings").onClick.Invoke();await Task.Delay(100);
  var draftField=typeof(GameMenuController).GetField("settingsDraft",flags);var original=UserSettings.Current.masterVolume;
  var draft=(UserSettingsData)draftField.GetValue(Menu);draft.masterVolume=.2f;
  Check(UserSettings.Current.masterVolume==original,"draft does not apply immediately");
  Button("Cancel Settings").onClick.Invoke();Check(((UserSettingsData)draftField.GetValue(Menu)).masterVolume==original,"cancel restores current settings");
  Check(Button("Settings Tab 4"),"interface settings tab");Check(Button("Apply Settings"),"explicit apply action");
  Menu.Resume();await Task.Delay(100);Check(GameObject.Find("PinnedResources"),"HUD restored after closing");
  BuildScreen.Open();await Task.Delay(100);
  Check(!GameObject.Find("PinnedResources")&&!GameObject.Find("HudWave"),"construction screen hides overlapping HUD");
  BuildScreen.Back();await Task.Delay(100);Check(GameObject.Find("PinnedResources"),"HUD restored after construction");
  return "UI1106 PASS "+passed+" checks";
 }
 public static string Show(string page){switch(page){case "research":Menu.Science();break;case "operations":Menu.PrototypeOperations();break;case "resources":Menu.PrototypeResources();break;case "settings":typeof(GameMenuController).GetMethod("Settings",flags).Invoke(Menu,null);break;case "build":Menu.Resume();BuildScreen.Toggle();break;case "roster":Menu.Roster();break;default:Menu.Resume();break;}return page;}
}
