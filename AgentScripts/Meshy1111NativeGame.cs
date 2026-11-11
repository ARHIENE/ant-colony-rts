using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using AntColony.Core;
using AntColony.Units;
public static class Meshy1111NativeGame
{
 const string Dir="Assets/_Project/Art/MeshyAnimation1106/";
 public static string CloseCompare(){foreach(var w in Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w=>w.GetType().Name=="Meshy1111DirectCompare"||w.GetType().Name=="MeshyAnimation1106Live").ToArray())w.Close();return "Comparison closed";}
 static AnimationClip Clip(string path,string name=null)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview")&&(name==null||c.name==name));
 public static string Paths(){var p=AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"Heavy_Hammer_Swing.fbx");return string.Join("\n",p.GetComponentsInChildren<Transform>().Take(7).Select(t=>AnimationUtility.CalculateTransformPath(t,p.transform)))+"\n"+string.Join("\n",new[]{Dir+"Heavy_Hammer_Swing.fbx",Dir+"Meshy_AI_Meadow_Sprout_20261106_animated_mixamo_All_animation.fbx","Assets/_Project/Art/MeshyAnimation1110/Idle_3.fbx"}.Select(path=>path+" : "+string.Join(",",AnimationUtility.GetCurveBindings(Clip(path)).Select(b=>b.path).Distinct().Take(4))));}
 public static string Apply()
 {
  if(!Application.isPlaying)throw new Exception("Play required");
  var unit=CommanderRoster.Instance.Commanders.First(c=>c&&c.IsColonyMember);
  if(unit.GetComponent<MeshyNativePilot>())return "Native pilot already attached";
  var character=AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"Heavy_Hammer_Swing.fbx");
  var mining=Clip(Dir+"Heavy_Hammer_Swing.fbx");var moves=Dir+"Meshy_AI_Meadow_Sprout_20261106_animated_mixamo_All_animation.fbx";
  var idle=new AnimationClip{name="Native Ready Pose",frameRate=30};
  var bindings=AnimationUtility.GetCurveBindings(mining);
  AnimationUtility.SetEditorCurves(idle,bindings,bindings.Select(b=>AnimationCurve.Constant(0,1,AnimationUtility.GetEditorCurve(mining,b).Evaluate(0))).ToArray());  var walk=Clip(moves,"Walking");var run=Clip(moves,"Running");
  foreach(var clip in new[]{idle,walk,run,mining})
  {
   var missing=AnimationUtility.GetCurveBindings(clip).Where(b=>b.type==typeof(Transform)&&b.path!=""&&character.transform.Find(b.path)==null).ToArray();
   if(missing.Length>0)throw new Exception(clip.name+" has unmatched native bone paths: "+string.Join(",",missing.Take(3).Select(b=>b.path)));
  }
  var pilot=unit.gameObject.AddComponent<MeshyNativePilot>();
  pilot.Configure(character,idle,walk,run,mining,AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"Meshy_AI_Meadow_Sprout_20261106_animated_mixamo_All_animation.png"));
  return "Attached original Meshy model to actual commander: "+unit.name+". All four clips match its native bone paths. No retarget, no arm IK.";
 }
 public static string Focus()
 {
  var pilot=UnityEngine.Object.FindAnyObjectByType<MeshyNativePilot>();var root=pilot.NativeModel;
  var head=root.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:Head");var feet=root.GetComponentsInChildren<Transform>().Where(t=>t.name=="mixamorig:LeftFoot"||t.name=="mixamorig:RightFoot").ToArray();
  var center=pilot.transform.position+pilot.transform.Find("Kemono").localPosition+Vector3.up*.8f;
  var cam=Camera.main;cam.orthographicSize=1.3f;cam.nearClipPlane=.02f;cam.transform.position=center+pilot.transform.Find("Kemono").right*2-pilot.transform.Find("Kemono").forward*.9f+Vector3.up*.5f;cam.transform.LookAt(center);
  EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();return "Native model Game view focused";
 }
 public static async Task<string> Audit()
 {
  var pilot=UnityEngine.Object.FindAnyObjectByType<MeshyNativePilot>();var bones=pilot.NativeModel.GetComponentsInChildren<Transform>();var arm=bones.First(t=>t.name=="mixamorig:RightArm");var forearm=bones.First(t=>t.name=="mixamorig:RightForeArm");
  var q=arm.localRotation;var r=forearm.localRotation;EditorApplication.isPaused=false;await Task.Delay(650);EditorApplication.isPaused=true;Focus();
  return "Actual game="+pilot.GetComponent<AntVisual>().StateName+" native clip="+pilot.PlayingMotion+" shoulderDelta="+Quaternion.Angle(q,arm.localRotation)+" elbowDelta="+Quaternion.Angle(r,forearm.localRotation);
 }
 public static string Visibility(){var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativePilot>();return string.Join("\n",p.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!r.forceRenderingOff).Select(r=>AnimationUtility.CalculateTransformPath(r.transform,p.transform)+" "+r.GetType().Name));}
 public static string KeepPlaying(){var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativePilot>();p.PreviewMining=true;AntColony.UI.SimulationPause.UserSpeed(0);EditorApplication.isPaused=false;Focus();return "Native mining loop continues at 1x unscaled time; world simulation paused. Camera fixed. No resource depletion.";}
}
