using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;

public static class FireReadability1115Checks
{
 const string Log="work/colony-polish-1115/fire-readability.txt";
 static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool ok,string note){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+note+"\n");if(!ok)throw new Exception(note);}
 static async Task Wait(Func<bool> ready){var end=DateTime.UtcNow.AddSeconds(120);while(!ready()&&DateTime.UtcNow<end)await Task.Delay(100);if(!ready())throw new Exception("Readiness timeout");}
 public static string Main(){_=Run();return "Started isolated fire fixture; natural checkpoint is preserved";}
 static async Task Run(){
  File.WriteAllText(Log,"RUNNING isolated Meshy fire, no natural campaign damage or resource changes\n");
  string root=SaveStorage.RootOverride;
  try{
   if(string.IsNullOrEmpty(root)||Time.timeScale!=0)throw new Exception("Paused isolated campaign required");
   SaveStorage.RootOverride=Path.GetFullPath("work/colony-polish-1115/fire-save");
   var home=(Vector3)typeof(ResourceManager).GetProperty("HomePosition",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
   var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{BuildingKind.Carpentry,UnitRole.Worker});
   var go=UnityEngine.Object.Instantiate(template,home+new Vector3(30,0,0),Quaternion.identity);go.SetActive(true);
   var building=go.GetComponent<BuildingBase>();building.MainMaterial=ResourceType.Wood;
   await Wait(()=>building.GetComponent<FacilityVisual>()?.Model!=null);
   typeof(BuildingBase).GetMethod("RestoreHealth",Hidden).Invoke(building,new object[]{building.MaxHealth});
   Check(go.GetComponent<Renderer>().forceRenderingOff,"Imported Meshy model hides original renderer");
   Check(WallFire.Ignite(building),"Normal ignition accepts wooden facility");
   var fire=go.GetComponent<WallFire>();float hp=building.CurrentHealth,remaining=fire.CaptureState().remaining;
   await Task.Delay(1800);
   Check(go.GetComponents<BuildingFireVisual>().Length==1&&go.GetComponentInChildren<ParticleSystem>().particleCount>0,"One fire effect remains visible while simulation is paused");
   Check(building.CurrentHealth==hp&&fire.CaptureState().remaining==remaining,"Paused presentation does not advance damage or duration");
   ThreatIncidents.Scan(true);int sequence=SimulationPause.Capture().sequence;ThreatIncidents.Scan(true);
   Check(SimulationPause.Capture().active.Contains("fire:home")&&SimulationPause.Capture().sequence==sequence,"Managed fire warning is active and deduplicated");
   Check(CampaignNextSteps.RecoverySummary(false).Contains("화재"),"Recovery summary prioritizes firefighting");
   var camera=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
   typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",Hidden).Invoke(camera,new object[]{building.Position+Vector3.up,0f,7f});
   await Task.Delay(1200);ScreenCapture.CaptureScreenshot("work/colony-polish-1115/fire-active.png");await Task.Delay(400);
   var position=building.Position;
   if(!SaveSystem.TrySave(false,0,out var error))throw new Exception(error);
   if(!SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error))throw new Exception(error);
   await Wait(()=>!SaveSystem.Busy&&GameSession.Exists&&GameSession.Instance.GameStarted);SimulationPause.UserSpeed(0);
   building=SceneObjects.All<BuildingBase>().Single(b=>b.isActiveAndEnabled&&b.Data?.kind==BuildingKind.Carpentry&&(b.Position-position).sqrMagnitude<.01f);
   fire=building.GetComponent<WallFire>();await Task.Delay(1500);
   File.AppendAllText(Log,$"RELOAD burning={fire.Burning} remaining={fire.CaptureState().remaining:R}/{remaining:R} hp={building.CurrentHealth:R}/{hp:R} particles={building.GetComponentInChildren<ParticleSystem>()?.particleCount}\n");
   Check(fire.Burning&&Mathf.Abs(fire.CaptureState().remaining-remaining)<.001f&&Mathf.Abs(building.CurrentHealth-hp)<.001f&&building.GetComponentInChildren<ParticleSystem>().particleCount>0,"Validated full reload preserves fire and restores particles without changing damage");
   fire.Extinguish(WallFire.ExtinguishSeconds);await Task.Delay(300);ThreatIncidents.Scan(true);
   Check(!fire.Burning&&building.GetComponentInChildren<ParticleSystem>().particleCount==0&&!SimulationPause.Capture().active.Contains("fire:home"),"Extinguishing clears particles and active fire warning");
  }catch(Exception e){File.AppendAllText(Log,"ERROR "+e+"\n");}
  finally{
   SaveStorage.RootOverride=root;
   if(SaveSystem.TryLoad(Path.GetFullPath("work/natural-campaign-1115/checkpoint-save.json"),out var error)){
    await Wait(()=>!SaveSystem.Busy&&GameSession.Exists&&GameSession.Instance.GameStarted);SimulationPause.UserSpeed(0);
    File.AppendAllText(Log,"RESTORED natural checkpoint; fixture discarded\n");
   }else File.AppendAllText(Log,"RESTORE ERROR "+error+"\n");
  }
 }
}
