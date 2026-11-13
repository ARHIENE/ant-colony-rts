using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
using AntColony.Core;
public static class Meshy1111NativeActions
{
 public static async Task<string> SourcePreview(string id)
 {
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();p.Configure(Resources.Load<MeshyNativeLibrary>("MeshyNative/Library"));
  p.PreviewMotion=id;p.PreviewPhase=.3f;await Task.Delay(150);Focus();return p.PlayingMotion;
 }
 public static string CaptureSettings()
 {
  SessionState.SetString("Meshy1111.settings",JsonUtility.ToJson(UserSettings.Current));
  SessionState.SetString("Meshy1111.root",AntColony.Save.SaveStorage.RootOverride??"");
  return "Captured user settings and save root for restoration";
 }
 public static string RestoreSettings()
 {
  var root=SessionState.GetString("Meshy1111.root","");AntColony.Save.SaveStorage.RootOverride=root==""?null:root;
  var json=SessionState.GetString("Meshy1111.settings","");
  if(json!="")UserSettings.Apply(JsonUtility.FromJson<UserSettingsData>(json),false);else UserSettings.Load();
  return "Restored user settings and save root without writing saves";
 }
 public static async Task<string> PreviewWeapon(string weapon="ShortBow")
 {
  var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember);c.CommandStop();
  c.PersonalState.equipment.RemoveAll(e=>e.slot==EquipmentSlot.Weapon);
  c.PersonalState.equipment.Add(new EquipmentItem{slot=EquipmentSlot.Weapon,weapon=(WeaponKind)Enum.Parse(typeof(WeaponKind),weapon)});c.RefreshEquipment();
  var v=c.GetComponent<AntVisual>();v.Attack(c.Position+Vector3.forward*4);
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");var binding=lib.actions.FirstOrDefault(a=>a.action==v.MotionName);
  if(binding==null)throw new Exception("This weapon uses the legacy rig: "+weapon);
  var p=c.GetComponent<MeshyNativeMotionPlayer>();p.PreviewMotion=binding.motion;p.PreviewPhase=-1;
  AntColony.UI.SimulationPause.UserSpeed(0);EditorApplication.isPaused=false;await Task.Delay(300);Focus();
  return weapon+" action="+v.MotionName+" native="+p.PlayingMotion+"; isolated preview loops without advancing simulation";
 }
 public static async Task<string> Phase(float phase)
 {
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();p.PreviewPhase=phase;
  EditorApplication.isPaused=false;await Task.Delay(100);
  return "Fixed preview phase "+phase+" of "+p.PlayingMotion;
 }
 public static async Task<string> CheckPickaxe()
 {
   EditorApplication.isPaused=false;
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();
  var bones=p.NativeModel.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
  var item=p.GetComponentsInChildren<Transform>().First(t=>t.name=="Tool_Pickaxe_Stone(Clone)"&&t.parent.name=="Native equipment grip");
  Vector3 Palm(string side)=>Vector3.Lerp(bones["mixamorig:"+side+"Hand"].position,bones["mixamorig:"+side+"HandMiddle1"].position,.65f);
  float maxError=0,minSupport=10,maxSupport=-10,minHeadDirection=1;
  for(int i=0;i<50;i++)
  {
   var axis=item.parent.forward;var right=Palm("Right");var left=Palm("Left");
   float scale=item.lossyScale.y;
   var lead=item.position-axis*(.20f*scale);
   maxError=Mathf.Max(maxError,Vector3.Distance(lead,right));
   float support=Vector3.Dot(left-item.position,axis)/scale;minSupport=Mathf.Min(minSupport,support);maxSupport=Mathf.Max(maxSupport,support);
   minHeadDirection=Mathf.Min(minHeadDirection,Vector3.Dot(-axis,(right-left).normalized));await Task.Delay(50);
  }
  if(maxError>.01f||minSupport<-.21f||maxSupport>.131f||minHeadDirection<.99f)throw new Exception("Grip failed: primary="+maxError+" support="+minSupport+".."+maxSupport+" head="+minHeadDirection);
  return "PASS over full swing: lead palm error="+maxError+"m; support on usable shaft="+minSupport+".."+maxSupport+" (allowed -.20..+.13); head points away from lower hand="+minHeadDirection;
 }
 public static string MoveExistingDrops()
 {
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();int count=0;float amount=0;
  foreach(var node in UnityEngine.Object.FindObjectsByType<AntColony.World.ResourceNode>().Where(n=>n.IsLooseCargo&&Vector3.Distance(n.transform.position,p.transform.position)<2).ToArray())
  {
   amount+=node.AmountRemaining;var destination=p.transform.position+new Vector3(2+count*.8f,0,1.5f);count++;
   if(Physics.Raycast(destination+Vector3.up*4,Vector3.down,out var hit,12,1<<8,QueryTriggerInteraction.Ignore))destination.y=hit.point.y;
   var renders=node.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!r.forceRenderingOff).ToArray();
   var bounds=renders.Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
   node.transform.localScale*=.6f/Mathf.Max(bounds.size.x,bounds.size.z);
   bounds=renders.Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
   node.transform.position+=destination-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
  }
  return "Moved "+count+" overlapping dropped piles beside character, fitted to .6m; preserved resources="+amount;
 }
 public static string InspectVisible()
 {
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();var v=p.GetComponent<AntVisual>();
  return "unit="+p.transform.position+" nativeRoot="+p.NativeModel.position+" hips="+p.NativeModel.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:Hips").localPosition+" scale="+p.NativeModel.lossyScale+" action="+v.MotionName+" native="+p.PlayingMotion+" tool="+v.HeldTool+"\n"+string.Join("\n",p.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!r.forceRenderingOff).Select(r=>AnimationUtility.CalculateTransformPath(r.transform,p.transform)+" bounds="+r.bounds));
 }
 public static string Nearby()
 {
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();
  return string.Join("\n",UnityEngine.Object.FindObjectsByType<Renderer>().Where(r=>r.enabled&&!r.forceRenderingOff&&Vector3.Distance(r.bounds.center,p.transform.position)<2.5f).Select(r=>AnimationUtility.CalculateTransformPath(r.transform,null)+" bounds="+r.bounds+" components="+string.Join(",",r.GetComponentsInParent<Component>().Select(c=>c?c.GetType().Name:"missing"))));
 }
 public static string Apply()
 {
  if(!Application.isPlaying)throw new Exception("Play required");
  var unit=CommanderRoster.Instance.Commanders.First(c=>c&&c.IsColonyMember);
  var old=unit.GetComponent<MeshyNativePilot>();if(old)UnityEngine.Object.DestroyImmediate(old);
  var player=unit.GetComponent<MeshyNativeMotionPlayer>();if(!player)player=unit.gameObject.AddComponent<MeshyNativeMotionPlayer>();
  player.Configure(Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime"));
  return "Native library attached to actual commander "+unit.name;
 }
 public static string Focus()
 {
  var controller=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();if(controller)controller.enabled=false;
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();var original=p.transform.Find("Kemono");
  var center=p.transform.position+original.localPosition+Vector3.up*.8f;
  var cam=Camera.main;cam.orthographicSize=1.4f;cam.nearClipPlane=.02f;cam.transform.position=center+original.right*2-original.forward*.9f+Vector3.up*.5f;cam.transform.LookAt(center);
  EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();return "Native actions in Game view";
 }
 public static async Task<string> Preview(string id="Heavy_Hammer_Swing")
 {
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();p.PreviewMotion=id;p.PreviewPhase=-1;
  AntColony.UI.SimulationPause.UserSpeed(0);EditorApplication.isPaused=false;await Task.Delay(300);Focus();
  var arm=p.NativeModel.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:RightArm");var before=arm.localRotation;
  await Task.Delay(500);return "Preview="+p.PlayingMotion+" jointDelta="+Quaternion.Angle(before,arm.localRotation)+"; simulation paused, native motion running";
 }
 public static async Task<string> Actual()
 {
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();p.PreviewMotion=null;AntColony.UI.SimulationPause.UserSpeed(1);EditorApplication.isPaused=false;
  await Task.Delay(300);Focus();var arm=p.NativeModel.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:RightArm");var before=arm.localRotation;
  await Task.Delay(500);return "Game action="+p.GetComponent<AntVisual>().MotionName+" native="+p.PlayingMotion+" tool="+p.GetComponent<AntVisual>().HeldTool+" jointDelta="+Quaternion.Angle(before,arm.localRotation);
 }
}
