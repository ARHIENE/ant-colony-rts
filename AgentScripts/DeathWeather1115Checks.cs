using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Core;
using AntColony.World;
using AntColony.Map;
using AntColony.Save;
public static class DeathWeather1115Checks
{
 const string Dir="work/death-weather-1115/";
 static void Check(bool ok,string text){File.AppendAllText(Dir+"checks.txt",(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new Exception(text);}
 public static async Task<string> Main(){
  if(!Application.isPlaying||Time.timeScale!=0)throw new Exception("Paused isolated Play required");
  File.WriteAllText(Dir+"checks.txt","");
  var weather=WeatherSystem.Instance;var prior=weather.CaptureState();
  var random=UnityEngine.Random.state;
  try{
   weather.RestoreState(new WeatherSystem.State{kind=(int)WeatherKind.Snow,remaining=300,surfaceSaved=true});
   weather.TickSurface(45);Check(WeatherSystem.SnowCover>.45f&&WeatherSystem.SnowCover<.55f,"Snow accumulates gradually halfway after 45 game seconds");
   weather.TickSurface(45);Check(WeatherSystem.SnowCover==1,"90 seconds of snow covers exposed surfaces");
   var accumulated=weather.CaptureState();weather.RestoreState(new WeatherSystem.State());weather.RestoreState(JsonUtility.FromJson<WeatherSystem.State>(JsonUtility.ToJson(accumulated)));
   Check(WeatherSystem.SnowCover==1,"Accumulation serialized and restored");
   weather.RestoreState(new WeatherSystem.State{kind=(int)WeatherKind.Rain,remaining=300,surfaceSaved=true,snowCover=1});weather.TickSurface(45);
   Check(WeatherSystem.SnowCover<.6f&&WeatherSystem.Wetness==1,"Rain melts snow and wets natural surfaces");
   Check(JsonUtility.ToJson(random)==JsonUtility.ToJson(UnityEngine.Random.state),"Surface simulation does not consume gameplay random state");
   var surfaces=UnityEngine.Object.FindObjectsByType<NaturalSurface>();
   Check(surfaces.Length>0,"Live natural props have weather materials");
   Check(surfaces.SelectMany(s=>s.GetComponentsInChildren<MeshRenderer>()).SelectMany(r=>r.sharedMaterials).All(m=>m==null||m.shader.name=="AntColony/SeasonFoliage"),"Natural prop materials use the seasonal shader");
   var home=CommanderRosterHome();
   foreach(OrganicEnemyKind kind in Enum.GetValues(typeof(OrganicEnemyKind))){
    if(kind==OrganicEnemyKind.None)continue;
    var corpse=Corpse.Spawn(new Corpse.State{name="Detail check "+kind,kind=CorpseKind.Wildlife,organicKind=(int)kind,position=home+new Vector3(8,0,8),remaining=Corpse.Lifetime,organicYaw=73});
    try{
     var pose=corpse.GetComponentInChildren<OrganicDeathPose>();Check(pose!=null,kind+" has collapse animation");
     var skin=pose.GetComponentInChildren<SkinnedMeshRenderer>();var bones=skin.bones;
     pose.Sample(0);var start=bones.Select(b=>b.localRotation).ToArray();
     pose.Sample(.7f);int moving=bones.Where((b,i)=>Quaternion.Angle(start[i],b.localRotation)>3).Count();
     Check(moving>=3,kind+" moves multiple joints rather than only flipping root: "+moving);
     pose.Sample(2.6f);var end=bones.Select(b=>b.localRotation).ToArray();pose.Sample(2.6f);
     Check(bones.Select((b,i)=>Quaternion.Angle(end[i],b.localRotation)).Max()<.1f,kind+" settled pose does not accumulate rotation");
     Check(corpse.Data.remaining==Corpse.Lifetime&&corpse.Data.count==1,kind+" visual sampling preserves corpse gameplay state");
    }finally{UnityEngine.Object.DestroyImmediate(corpse.gameObject);}
   }
   Check(SaveSystem.TrySave(false,0,out var error),"Normal save after new weather/corpse state: "+error);
  }finally{weather.RestoreState(prior);}
  await Task.Delay(200);return File.ReadAllText(Dir+"checks.txt");
 }
 static Vector3 CommanderRosterHome()=> (Vector3)typeof(ResourceManager).GetProperty("HomePosition",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
 public static async Task<string> FreshDeath(){
  var local=SceneObjects.Any<LocalIncursions>();var spawn=typeof(LocalIncursions).GetMethod("RestoreSpawn",BindingFlags.Instance|BindingFlags.NonPublic);
  var origin=CommanderRosterHome()+new Vector3(0,0,-16);
  var host=(WildMonster)spawn.Invoke(local,new object[]{false,origin});
  host.ConfigureOrganic(OrganicEnemyKind.WallRam);host.MakeRaider();host.name="Fresh death detail check";
  typeof(WildMonster).GetMethod("ConfigureHorde",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(host,new object[]{6});
  host.gameObject.SetActive(true);host.enabled=false;await Task.Delay(250);
  var visual=host.GetComponent<OrganicEnemyVisual>();visual.Act(2,1,.3f);visual.Animator.Update(0);
  host.TakeDamage(100000);await Task.Delay(100);
  var corpse=Corpse.All.Single(c=>c.Data.name=="Fresh death detail check");var pose=corpse.GetComponentInChildren<OrganicDeathPose>();
  Check(pose!=null&&pose.enabled&&pose.Elapsed==0,"Actual lethal damage enters fresh multi-joint collapse while paused");
  Check(corpse.GetComponentInChildren<Animator>().enabled==false,"Legacy flip clip no longer drives actual corpse");
  var remains=UnityEngine.Object.FindObjectsByType<OrganicHordeRemains>();
  Check(remains.Length>0,"Actual horde death emits GPU remains");
  var banks=Enumerable.Range(1,17).Select(i=>Resources.Load<OrganicMotionBank>("OrganicEnemies1114/"+(OrganicEnemyKind)i+"/HordeMotion")).ToArray();
  Check(banks.All(b=>b&&Mathf.Abs(b.clips[4].seconds-OrganicDeathPose.Duration)<.001f&&b.clips[4].frames>=63),"All 17 GPU death clips use the new collapse duration and samples");
  foreach(var r in remains)UnityEngine.Object.DestroyImmediate(r.gameObject);UnityEngine.Object.DestroyImmediate(corpse.gameObject);if(host)UnityEngine.Object.DestroyImmediate(host.gameObject);
  return "Fresh lethal damage and GPU remains checks passed";
 }
 public static async Task<string> Roundtrip(){
  if(Time.timeScale!=0)throw new Exception("Pause first");
  var home=CommanderRosterHome();
  var corpse=Corpse.Spawn(new Corpse.State{name="Detail roundtrip",kind=CorpseKind.Wildlife,organicKind=(int)OrganicEnemyKind.WallRam,position=home+Vector3.right*12,remaining=290,organicYaw=73});
  var pose=corpse.GetComponentInChildren<OrganicDeathPose>();var skin=pose.GetComponentInChildren<SkinnedMeshRenderer>();
  var rotations=skin.bones.Select(b=>b.rotation).ToArray();var position=pose.transform.position;
  WeatherSystem.Instance.RestoreState(new WeatherSystem.State{kind=(int)WeatherKind.Snow,remaining=213,surfaceSaved=true,snowCover=.65f,wetness=.2f});
  Check(SaveSystem.TrySave(false,0,out var error),"Save settled corpse and partial snow: "+error);
  Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"Load detail save: "+error);
  var deadline=DateTime.UtcNow.AddSeconds(90);while(SaveSystem.Busy&&DateTime.UtcNow<deadline)await Task.Delay(100);
  Check(!SaveSystem.Busy,"Detail save finished loading");AntColony.UI.SimulationPause.UserSpeed(0);
  corpse=Corpse.All.Single(c=>c.Data.name=="Detail roundtrip");pose=corpse.GetComponentInChildren<OrganicDeathPose>();skin=pose.GetComponentInChildren<SkinnedMeshRenderer>();
  Check(Mathf.Abs(WeatherSystem.SnowCover-.65f)<.001f&&Mathf.Abs(WeatherSystem.Wetness-.2f)<.001f,"Full reload preserves snow and wetness");
  Check(!pose.enabled&&Vector3.Distance(position,pose.transform.position)<.003f,"Settled corpse stays grounded after full reload");
  Check(skin.bones.Select((b,i)=>Quaternion.Angle(rotations[i],b.rotation)).Max()<.1f,"Full reload preserves corpse direction and articulated pose");
  Check(corpse.Data.count==1&&corpse.Data.remaining==290,"Corpse lifetime and count preserved");
  return File.ReadAllText(Dir+"checks.txt");
 }
 public static async Task<string> DeathView(){
  var home=CommanderRosterHome();var bodies=new System.Collections.Generic.List<Corpse>();
  try{
   var kinds=new[]{OrganicEnemyKind.ClawHunter,OrganicEnemyKind.WallRam,OrganicEnemyKind.BloomMender};
   for(int i=0;i<kinds.Length;i++)bodies.Add(Corpse.Spawn(new Corpse.State{name="Death view",kind=CorpseKind.Wildlife,organicKind=(int)kinds[i],position=home+new Vector3((i-1)*3f,0,-16),remaining=Corpse.Lifetime,organicYaw=30}));
   var camera=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();
   typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(camera,new object[]{home+new Vector3(0,0,-16),0f,4.5f});
   foreach(float t in new[]{0f,.32f,.7f,1.2f,2.6f}){
    foreach(var body in bodies)body.GetComponentInChildren<OrganicDeathPose>().Sample(t);
    await Task.Delay(250);ScreenCapture.CaptureScreenshot(Dir+"death-"+t.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png");await Task.Delay(250);
   }
   return "Captured three body types at five collapse stages";
  }finally{foreach(var body in bodies)UnityEngine.Object.DestroyImmediate(body.gameObject);}
 }
 public static async Task<string> WeatherView(string name){

  float time=Mathf.Floor(GameCalendar.GameSeconds/GameCalendar.SecondsPerYear)*GameCalendar.SecondsPerYear+(name=="autumn"?GameCalendar.SecondsPerSeason*2+150:GameCalendar.SecondsPerSeason+150); if(time<GameCalendar.GameSeconds)time+=GameCalendar.SecondsPerYear;
  typeof(GameSession).GetProperty("GameSeconds").SetValue(GameSession.Instance,time);
  WeatherSystem.Instance.RestoreState(new WeatherSystem.State{kind=(int)(name=="snow"?WeatherKind.Snow:name=="rain"?WeatherKind.Rain:WeatherKind.Clear),remaining=300,surfaceSaved=true,snowCover=name=="snow"?1:0,wetness=name=="rain"?1:0});
  var rock=SceneObjects.All<ResourceNode>().First(n=>!n.IsLooseCargo&&n.ResourceType==AntColony.Data.ResourceType.Stone);
  var camera=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();
  typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(camera,new object[]{rock.transform.position,0f,12f});
  await Task.Delay(600);ScreenCapture.CaptureScreenshot(Dir+"weather-"+name+".png");await Task.Delay(400);return "Captured "+name;
 }
}
