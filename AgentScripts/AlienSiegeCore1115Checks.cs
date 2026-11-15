using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Boss;
using AntColony.Core;
using AntColony.Save;
using UnityEngine;
public static class AlienSiegeCore1115Checks
{
 public static async Task<string> View(){
  var boss=SceneObjects.All<BossHealth>().First(b=>b.GetComponent<CampaignPlaceholderBody>()&&!b.GetComponent<MothershipBossMarker>());
  var camera=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
  typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(camera,new object[]{boss.Position,0f,10f});
  await Task.Delay(500);ScreenCapture.CaptureScreenshot("work/colony-polish-1115/siege-core-game.png");await Task.Delay(500);return "Captured real boss with HUD";
 }
 const string Log="work/colony-polish-1115/siege-core.txt";
 static void Check(bool ok,string message){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new Exception(message);}
 static string Stats(BossHealth b)=>b.name+":"+b.CurrentHp+":"+b.MaxHp+":"+JsonUtility.ToJson(b.GetComponent<BossEncounter>()?.Capture());
 static void Inspect(BossHealth b){
  var hp=Stats(b);var hits=b.GetComponentsInChildren<Collider>().Where(c=>c.enabled).ToArray();
  Check(b.GetComponentsInChildren<Renderer>().Where(r=>r.name=="지휘체 핵"||r.name=="지휘체 하부"||r.name=="방열 기둥").All(r=>!r.enabled||r.forceRenderingOff),"Spawn/restore hides old body before test calls Ensure");
  CampaignBossBody.Ensure(b);CampaignBossBody.Ensure(b);
  var model=b.transform.Find("침공 지휘체 · Meshy");
  Check(model&&b.GetComponentsInChildren<Transform>().Count(t=>t.name=="침공 지휘체 · Meshy")==1,"Meshy body exists once: "+b.name);
  Check(model.GetComponentsInChildren<Renderer>().All(r=>r.enabled&&!r.forceRenderingOff&&r.sharedMaterials.All(m=>m&&m.GetTexture("_BaseMap"))),"Visible textured body");
  var old=b.GetComponentsInChildren<Renderer>().Where(r=>!r.transform.IsChildOf(model)).ToArray();
  File.AppendAllText(Log,string.Join("\n",old.Select(r=>r.name+" enabled="+r.enabled+" hidden="+r.forceRenderingOff))+"\n");
  Check(old.Where(r=>r.name=="지휘체 핵"||r.name=="지휘체 하부"||r.name=="방열 기둥").All(r=>!r.enabled||r.forceRenderingOff),"Primitive body renderers hidden; UI and telegraphs retained");
  Check(hp==Stats(b)&&hits.SequenceEqual(b.GetComponentsInChildren<Collider>().Where(c=>c.enabled)),"Health, encounter state and colliders unchanged");
  Check(hits.Any(c=>c.Raycast(new Ray(c.bounds.center+Vector3.up*20,Vector3.down),out var hit,40)&&hit.collider.GetComponentInParent<BossHealth>()==b),"Existing boss click hitbox works");
 }
 public static async Task<string> Main(){
  if(Time.timeScale!=0||string.IsNullOrEmpty(SaveStorage.RootOverride))throw new Exception("Paused isolated run required");
  File.WriteAllText(Log,"");
  var bosses=SceneObjects.All<BossHealth>().Where(b=>b.GetComponent<CampaignPlaceholderBody>()&&!b.GetComponent<MothershipBossMarker>()).ToArray();
  Check(bosses.Length>0,"Natural campaign placeholder bosses found");foreach(var b in bosses)Inspect(b);
  var before=bosses.Select(Stats).OrderBy(s=>s).ToArray();
  Check(SaveSystem.TrySave(false,0,out var error),"Normal save: "+error);var path=SaveSlots.PathFor(false,0);
  if(!SaveSystem.TryLoad(path,out error))throw new Exception(error);
  var until=DateTime.UtcNow.AddSeconds(100);while((SaveSystem.Busy||!GameSession.Exists||!GameSession.Instance.GameStarted)&&DateTime.UtcNow<until)await Task.Delay(100);
  AntColony.UI.SimulationPause.UserSpeed(0);
  bosses=SceneObjects.All<BossHealth>().Where(b=>b.GetComponent<CampaignPlaceholderBody>()&&!b.GetComponent<MothershipBossMarker>()).ToArray();
  Check(before.SequenceEqual(bosses.Select(Stats).OrderBy(s=>s)),"Full reload preserves boss health and encounter state");foreach(var b in bosses)Inspect(b);
  return File.ReadAllText(Log);
 }
}
