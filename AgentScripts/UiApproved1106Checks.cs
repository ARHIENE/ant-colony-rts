using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using AntColony.Core;
using AntColony.Units;
using AntColony.UI;
using AntColony.World;
public static class UiApproved1106Checks
{
 static int checks;
 static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;}
 static async Task Frames(){int frame=Time.frameCount;for(int i=0;i<150&&Time.frameCount<frame+4;i++)await Task.Delay(30);Canvas.ForceUpdateCanvases();}
 static Rect Bounds(RectTransform r){var c=new Vector3[4];r.GetWorldCorners(c);var box=Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y);Check(box.xMin>=-1&&box.yMin>=-1&&box.xMax<=Screen.width+1&&box.yMax<=Screen.height+1,"out of screen: "+r.name+" "+box);return box;}
 static Button Button(string n)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).First(b=>b.name==n);
 static async Task Shot(string name){await Frames();ScreenCapture.CaptureScreenshot(Path.GetFullPath("work/ui-approved-1106/"+name+".png"));await Frames();}
 public static async Task<string> Run()
 {
  Check(Application.isPlaying&&GameSession.Exists&&GameSession.Instance.GameStarted,"isolated game required");
  var menu=GameMenuController.Instance;menu.Resume();SimulationPause.UserSpeed(0);
  var selection=SceneObjects.Any<SelectionManager>();WorkTargetPanel.Clear();selection.SelectOnly(CommanderRoster.Instance.Commanders[0].GetComponent<SelectableObject>());
  var asm=typeof(Editor).Assembly;var sizesType=asm.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
  var group=sizesType.GetMethod("GetGroup").Invoke(singleton,new[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});
  var viewType=asm.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(viewType);var prop=viewType.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);int old=(int)prop.GetValue(view);
  int slot=(int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null)+(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null);
  bool added=false;var wave=WaveDefense.Instance;var saved=wave.Capture();
  try{
   foreach(var size in new[]{new Vector2Int(1440,900),new Vector2Int(760,600)}){
    var entry=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),size.x,size.y,"Approved HUD check");
    group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{entry});added=true;prop.SetValue(view,slot);view.Focus();view.Repaint();await Frames();
    Check(Screen.width==size.x&&Screen.height==size.y,"resolution applied");
    SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.FocusOn(CommanderRoster.Instance.Commanders[0].transform.position); await Frames();
    var nav=GameObject.Find("MenuToolbar").GetComponent<RectTransform>();var clock=GameObject.Find("HudClock").GetComponent<RectTransform>();
    var nr=Bounds(nav);var cr=Bounds(clock);Check(cr.yMax<=37&&nr.yMax<=37,"clock and menus share bottom line");Check(cr.xMax<=nr.xMin+1,"clock does not overlap menus: clock="+cr+" nav="+nr+" screen="+Screen.width);
    Check(nav.GetComponentsInChildren<Button>().Length==8,"eight navigation items");Bounds(Button("Prototype Settings").GetComponent<RectTransform>());
    Bounds(GameObject.Find("PinnedResources").GetComponent<RectTransform>());Bounds(HudConsole.Left);Bounds(GameObject.Find("HudWave").GetComponent<RectTransform>());Bounds(GameObject.Find("V6SelectionSummary").GetComponent<RectTransform>());
    var face=GameObject.Find("Selected Commander Portrait").GetComponent<RawImage>();Check(face.texture!=null,"real portrait");
    Check(HudConsole.Right.GetComponentsInChildren<Button>().Length==3,"three civilian commands");foreach(var b in HudConsole.Right.GetComponentsInChildren<Button>())Bounds(b.GetComponent<RectTransform>());
    await Shot("hud-"+size.x);
    Button("V6MapToggle").onClick.Invoke();await Frames();Check(!HudConsole.Left.Find("MinimapScreen").gameObject.activeSelf,"map folded");Button("V6MapToggle").onClick.Invoke();await Frames();
    menu.WorkSchedule();await Frames();Check(GameMenuController.MenuOpen,"work schedule opens");menu.Resume();await Frames();
    Button("Civilian Equipment").onClick.Invoke();await Frames();Check(GameMenuController.MenuOpen,"equipment opens");menu.Resume();await Frames();
    var start=wave.Capture();start.saved=true;start.inWave=false;start.wave=0;start.peaceLeft=WaveDefense.FirstPeace;wave.Restore(start);await Frames();var normal=GameObject.Find("HudWave").GetComponent<Image>().color;
    var urgent=wave.Capture();urgent.peaceLeft=30;wave.Restore(urgent);await Frames();var red=GameObject.Find("HudWave").GetComponent<Image>().color;
    Check(red.r>normal.r&&red.g<normal.g,"urgency turns red");Check(GameObject.Find("HudWave").GetComponentsInChildren<Text>().Any(t=>t.text=="00:30"),"actual wave clock");await Shot("urgent-"+size.x);wave.Restore(saved);
    prop.SetValue(view,old);group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{slot});added=false;await Frames();
   }
   return "PASS "+checks+" checks: real HUD screenshots at 1440x900 and 760x600, selection, navigation, map, equipment, wave urgency";
  }finally{wave.Restore(saved);menu.Resume();prop.SetValue(view,old);if(added)group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{slot});view.Repaint();}
 }
}

