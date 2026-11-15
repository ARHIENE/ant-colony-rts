using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;
using AntColony.Core;using AntColony.Buildings;using AntColony.Data;using AntColony.Units;using AntColony.Save;using AntColony.UI;using UnityEngine;using UnityEngine.AI;
public static class TribalFinish1115Checks {
 const string Dir="work/colony-polish-1115/";
 static void Check(bool ok,string text){File.AppendAllText(Dir+"tribal-finish.txt",(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new Exception(text);}
 public static string Main(){File.WriteAllText(Dir+"tribal-finish.txt","Visual fixture in prior isolated test world.\n");_=Run();return "Tribal visual checks started";}
 static GameObject Spawn(BuildingKind kind,Vector3 p){var t=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{kind,UnitRole.Worker});var go=UnityEngine.Object.Instantiate(t,p,Quaternion.identity);go.SetActive(true);return go;}
 static async Task Run(){var root=SaveStorage.RootOverride;try{SimulationPause.UserSpeed(0);SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"TribalFinish1115");
 var c=CommanderRoster.Instance.Commanders.First();var centre=c.Position+Vector3.right*15;
 var store=Spawn(BuildingKind.FoodStore,centre+Vector3.left*4);var hearth=Spawn(BuildingKind.Hearth,centre);var gate=Spawn(BuildingKind.Gate,centre+Vector3.right*4).GetComponent<Gate>();gate.SetOpen(false);await Task.Delay(1500);
 foreach(var go in new[]{store,hearth,gate.gameObject}){var visual=go.GetComponent<FacilityVisual>();Check(visual&&visual.Model,"Live model attached "+go.name);Check(visual.Model.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m&&m.shader.name.StartsWith("Universal Render Pipeline/")&&m.GetTexture("_BaseMap"))),"Textured URP materials "+go.name);}
 var transforms=gate.GetComponent<FacilityVisual>().Model.GetComponentsInChildren<Transform>();var l=transforms.First(t=>t.name=="GateLeafLeft");var r=transforms.First(t=>t.name=="GateLeafRight");var lc=l.localRotation;var rc=r.localRotation;
 Check(gate.GetComponent<NavMeshObstacle>().enabled,"Closed gate blocks navigation");
 var cam=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(cam,new object[]{centre,45f,8f});cam.enabled=false;ScreenCapture.CaptureScreenshot(Dir+"tribal-closed.png");await Task.Delay(1000);
 gate.SetOpen(true);await Task.Delay(1000);Check(Quaternion.Angle(lc,l.localRotation)>95&&Quaternion.Angle(rc,r.localRotation)>95&&!gate.GetComponent<NavMeshObstacle>().enabled,"Both leaves open with passable navigation");ScreenCapture.CaptureScreenshot(Dir+"tribal-open.png");await Task.Delay(1000);
 Check(SaveSystem.TrySave(false,0,out var error),"Save open gate: "+error);Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"Load open gate: "+error);var end=DateTime.UtcNow.AddSeconds(100);while(SaveSystem.Busy&&DateTime.UtcNow<end)await Task.Delay(100);Check(!SaveSystem.Busy,"Reload completes");GameMenuController.Instance.Resume();SimulationPause.UserSpeed(0);await Task.Delay(1000);
 gate=SceneObjects.All<Gate>().First(g=>g.isActiveAndEnabled&&!g.IsDead);Check(gate.Open&&gate.GetComponent<FacilityVisual>().Model.GetComponent<MeshyGateVisual>(),"Open state and animated Meshy model survive full reload");
 }catch(Exception e){File.AppendAllText(Dir+"tribal-finish.txt","ERROR "+e+"\n");}finally{SaveStorage.RootOverride=root;SimulationPause.UserSpeed(0);File.AppendAllText(Dir+"tribal-finish.txt","RESTORED\n");}}
}
