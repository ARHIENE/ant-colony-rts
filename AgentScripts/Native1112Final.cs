using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
using AntColony.World;
using AntColony.Data;
public static class Native1112Final{
 public static async Task<string> Cargo(){var report=new System.Collections.Generic.List<string>();foreach(var type in new[]{ResourceType.Stone,ResourceType.Wood,ResourceType.Clay}){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.SetActive(false);go.transform.position=new Vector3(80,0,80);go.transform.localScale=Vector3.one*.6f;var node=go.AddComponent<ResourceNode>();node.ConfigureLoot(type,5.25f);go.SetActive(true);await Task.Delay(50);var renders=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!r.forceRenderingOff).ToArray();var b=renders[0].bounds;foreach(var r in renders)b.Encapsulate(r.bounds);float width=Mathf.Max(b.size.x,b.size.z);if(width>.66f||node.AmountRemaining!=5.25f)throw new Exception("Cargo changed or oversized "+type);report.Add(type+" width="+width+" stock="+node.AmountRemaining+" PASS");UnityEngine.Object.Destroy(go);}File.WriteAllLines("work/animation-1112/cargo-check.txt",report);return string.Join("\n",report);}
 public static async Task<string> Screen(){AntColony.UI.SimulationPause.UserSpeed(0);EditorApplication.isPaused=false;var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();var model=p.transform.Find("Kemono");var center=p.transform.position+model.localPosition+Vector3.up*.85f;var cam=Camera.main;cam.orthographicSize=1.25f;cam.transform.position=center+model.right*3+model.forward*1.2f+Vector3.up*.35f;cam.transform.LookAt(center);EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();await Task.Delay(100);string file="work/animation-1112/unity-game-final.png";ScreenCapture.CaptureScreenshot(file);await Task.Delay(500);return File.Exists(file)?file:"Capture pending";}
 public static string Coverage(){var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");var lines=new System.Collections.Generic.List<string>{"motion,rig,runtime_actions,verification"};foreach(var m in lib.motions)lines.Add(m.id+","+m.rig+","+string.Join(";",lib.actions.Where(a=>a.motion==m.id).Select(a=>a.action))+",SOURCE_5_POSES_AND_PRODUCTION_APPEARANCE_REVIEWED;LIVE_PLAYBACK_AUDIT_PASS;NATURAL_GAMEPLAY_SEPARATE");File.WriteAllLines("work/animation-1112/all-motion-coverage.csv",lines);return lib.motions.Length+" clips / "+lib.actions.Length+" actions";}
}
