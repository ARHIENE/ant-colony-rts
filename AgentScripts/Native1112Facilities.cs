using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using AntColony.Core;
using AntColony.Data;
using AntColony.Buildings;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using Object = UnityEngine.Object;

// Deliberately isolated visual fixture, not evidence of natural construction speed.
public static class Native1112Facilities
{
 static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static async Task<string> Run()
 {
  var settings=UserSettings.Current.Clone();var root=SaveStorage.RootOverride;
  var asm=typeof(Editor).Assembly;var sizes=asm.GetType("UnityEditor.GameViewSizes");
  var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizes).GetProperty("instance").GetValue(null);
  var group=sizes.GetMethod("GetGroup").Invoke(singleton,new[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});
  var viewType=asm.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(viewType);
  var prop=viewType.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
  int old=(int)prop.GetValue(view);int slot=(int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null)+(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null);bool added=false;
  var spawned=new List<GameObject>();string result="";
  Directory.CreateDirectory("Temp/animation-facilities-1112");
  try{
   var quiet=settings.Clone();quiet.autoSaveEnabled=false;UserSettings.Apply(quiet,false);
   SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"Facility1112-"+Guid.NewGuid().ToString("N"));
   GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds,45);Time.timeScale=0;GameMenuController.Instance.Resume();Time.timeScale=0;
   var entry=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),1440,900,"Facility review");
   group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{entry});added=true;prop.SetValue(view,slot);view.Focus();view.Repaint();await Task.Delay(700);
   var home=(Vector3)typeof(ResourceManager).GetProperty("HomePosition",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
   var cam=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();cam.enabled=false;
   var center=home+new Vector3(0,0,-12);
   typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",Hidden).Invoke(cam,new object[]{center,45f,10f});
   SceneObjects.Any<SelectionManager>()?.ClearSelection();
   SceneObjects.Any<WeatherSystem>().Set(WeatherKind.Clear,600);
   var kinds=new[]{BuildingKind.Campfire,BuildingKind.Carpentry,BuildingKind.Kitchen,BuildingKind.ScienceLab,BuildingKind.SpikeTrap,BuildingKind.QueenChamber,BuildingKind.Workshop,BuildingKind.Smelter};
   var names=new[]{"Campfire","Carpentry","Kitchen","ResearchDesk","SpikeTrap","Stockpile","Workshop","Smelter"};
   var get=typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);
   for(int i=0;i<kinds.Length;i++){
    var pos=center+new Vector3((i%4-1.5f)*5,0,(i/4-.5f)*6);
    if(Physics.Raycast(pos+Vector3.up*50,Vector3.down,out var hit,100))pos.y=hit.point.y;
    var template=(GameObject)get.Invoke(null,new object[]{kinds[i],UnitRole.Worker});
    GameObject go;
    if(template==null && kinds[i]==BuildingKind.QueenChamber){go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.transform.position=pos+Vector3.up*.5f;go.transform.localScale=new Vector3(3,1,3);go.AddComponent<Stockpile>();}
    else {Check(template!=null,"template "+kinds[i]);go=Object.Instantiate(template,pos,Quaternion.identity);}
    spawned.Add(go);go.name="Visual review "+names[i];go.SetActive(true);
    FacilityVisual.Attach(go.GetComponent<BuildingBase>());await Task.Delay(100);
    var visual=go.GetComponent<FacilityVisual>();Check(visual?.Model!=null,"missing runtime model "+names[i]);
    Check(visual.Model.name.StartsWith(names[i]),"wrong release model "+visual.Model.name);
    var box=go.GetComponent<Renderer>().bounds;var bounds=visual.Model.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
    Check(bounds.size.x<=box.size.x+.05f&&bounds.size.z<=box.size.z+.05f,"footprint "+names[i]);
    Check(Mathf.Abs(bounds.min.y-box.min.y)<.05f,"ground "+names[i]);
    Check(visual.Model.GetComponentsInChildren<Collider>().Length==0,"extra colliders "+names[i]);
    var bld=go.GetComponent<BuildingBase>();bld.MainMaterial=ResourceType.Wood;await Task.Delay(100);
    Check(visual.Model!=null&&!visual.Model.name.StartsWith(names[i]),"renovation fallback "+names[i]);
    bld.MainMaterial=ResourceType.Soil;await Task.Delay(100);Check(visual.Model.name.StartsWith(names[i]),"default model restoration "+names[i]);
    result+=names[i]+" runtime bounds="+bounds.size+" floor/collider/material fallback PASS\n";
   }
   ScreenCapture.CaptureScreenshot("Temp/animation-facilities-1112/facilities-1440.png");await Task.Delay(500);
   SceneObjects.Any<WeatherSystem>().Set(WeatherKind.Storm,600);await Task.Delay(1800);
   var particles=SceneObjects.All<ParticleSystem>().Sum(p=>p.particleCount);Check(particles>0,"storm not emitting");
   ScreenCapture.CaptureScreenshot("Temp/animation-facilities-1112/storm-1440.png");await Task.Delay(500);
   result+="Storm live particles="+particles+"; inspect actual capture. Visual fixture, not pacing evidence.\n";
   File.WriteAllText("Temp/animation-facilities-1112/facility-checks.txt",result);return result;
  } finally {foreach(var go in spawned)if(go!=null)Object.Destroy(go);Time.timeScale=0;UserSettings.Apply(settings,false);SaveStorage.RootOverride=root;prop.SetValue(view,old);if(added)group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{slot});}
 }
}
