using System;
using System.Linq;
using System.Threading.Tasks;
using AntColony.UI;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using AntColony.Buildings;
using UnityEngine;
using UnityEngine.UI;
public static class V6Checks {
static int checks;
static void Check(bool ok,string s){if(!ok)throw new Exception(s);checks++;}
static Button Button(string s)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(b=>b.name==s);
public static async Task<string> Main(){
var original=AntColony.Save.SaveStorage.RootOverride;AntColony.Save.SaveStorage.RootOverride=System.IO.Path.Combine(Application.temporaryCachePath,"V6-"+Guid.NewGuid());
try {
for(int i=0;i<1800 && AntColony.Save.SaveSystem.Busy;i++)await Task.Delay(50);
Check(!AntColony.Save.SaveSystem.Busy,"initial scene ready");
AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions{seed=77,mapSize=AntColony.Core.MapSize.Small,biome=AntColony.Core.MapBiome.Forest});
for(int i=0;i<1800 && AntColony.Save.SaveSystem.Busy;i++)await Task.Delay(50);
Check(!AntColony.Save.SaveSystem.Busy,"new game completes");AntColony.UI.GameMenuController.Instance.Resume();await Task.Delay(300);Check(AntColony.Core.GameSession.Instance.GameStarted,"real game started");
var canvas=GameObject.Find("HUDCanvas").transform;var console=canvas.Find("CommandConsole");var selection=UnityEngine.Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();
selection.ClearSelection();WorkTargetPanel.Clear();await Task.Delay(150);
Check(!console.gameObject.activeSelf,"no selection hides console");
Check(canvas.Find("V6Dock").GetComponent<RectTransform>().sizeDelta.y==52,"52px dock");
var commander=UnityEngine.Object.FindObjectsByType<AntColony.Units.CommanderAnt>(FindObjectsSortMode.None).First(c=>!c.IsDead && c.Data!=null);selection.SelectOnly(commander.GetComponent<AntColony.Units.SelectableObject>());await Task.Delay(200);
Check(console.gameObject.activeSelf,"selected commander shows console");Check(console.Find("CommandCard/CivilianCommands").gameObject.activeInHierarchy,"civilian commands visible");
Check(console.Find("V6SelectionSummary").GetComponentInChildren<Text>().text.Contains(commander.CommanderName),"real name in summary");
Button("V6 Details").onClick.Invoke();await Task.Delay(100);Check(AntColony.UI.GameMenuController.Instance.ScreenName!="Game","commander details opens");AntColony.UI.GameMenuController.Instance.Resume();
Button("V6 Clear Selection").onClick.Invoke();await Task.Delay(100);Check(!console.gameObject.activeSelf,"clear hides card");
var map=canvas.Find("ConsoleLeft");map.Find("V6MapToggle").GetComponent<Button>().onClick.Invoke();await Task.Delay(100);Check(!map.Find("MinimapScreen").gameObject.activeSelf,"map collapses");map.Find("V6MapToggle").GetComponent<Button>().onClick.Invoke();await Task.Delay(100);Check(map.Find("MinimapScreen").gameObject.activeInHierarchy,"map expands");
var building=UnityEngine.Object.FindAnyObjectByType<AntColony.Buildings.Stockpile>();WorkTargetPanel.Select(building);await Task.Delay(150);Check(console.gameObject.activeSelf,"building card visible");Button("V6 Details").onClick.Invoke();await Task.Delay(100);Check(canvas.Find("ConsoleCenter").gameObject.activeInHierarchy,"target details visible");
Button("V6 Build").onClick.Invoke();await Task.Delay(150);Check(BuildScreen.IsOpen,"build connected");Check(!console.gameObject.activeSelf,"build hides selection");BuildScreen.Toggle();
Button("V6 Roster").onClick.Invoke();await Task.Delay(100);Check(AntColony.UI.GameMenuController.Instance.ScreenName!="Game","roster opens");AntColony.UI.GameMenuController.Instance.Resume();
return "PASS "+checks+" V6 UI checks";
}finally{AntColony.Save.SaveStorage.RootOverride=original;}
}}
