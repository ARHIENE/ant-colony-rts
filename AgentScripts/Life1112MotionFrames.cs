using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using UnityEditor;using AntColony.Units;using AntColony.Core;
public static class Life1112MotionFrames {
 public static async Task<string> Run(){
 var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember);c.CommandStop();c.WorkState.jobs=CommanderJobs.None;
 var t=typeof(CommanderAnt);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
 var cam=Camera.main;cam.orthographicSize=2f;var focus=c.Position+Vector3.up*.8f;cam.transform.position=focus+new Vector3(2,1.1f,3);cam.transform.LookAt(focus);
 EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();Time.timeScale=1;string report="";
 foreach(var action in new[]{"IdleTalk","IdleRest","IdleLook"}){
 c.CommandStop();t.GetField("idleLeisureMotion",flags).SetValue(c,action);t.GetField("idleLeisureUntil",flags).SetValue(c,Time.time+30);await Task.Delay(250);
 var p=c.GetComponent<MeshyNativeMotionPlayer>();var before=p.PlayingMotion;var bones=c.GetComponentsInChildren<Transform>();var q=bones.Select(b=>b.localRotation).ToArray();int frame=Time.frameCount;await Task.Delay(500);
 float delta=Enumerable.Range(0,bones.Length).Max(i=>Quaternion.Angle(q[i],bones[i].localRotation));if(Time.frameCount<=frame||delta<.01f)throw new Exception(action+" frozen");
 report+=action+" clip="+before+" frames="+(Time.frameCount-frame)+" maxJointDegrees="+delta+"\n";
 ScreenCapture.CaptureScreenshot("work/life-assets-1112/"+action+".png");await Task.Delay(200);
 }File.WriteAllText("work/life-assets-1112/motion-frames.txt",report);return report;
 }
}
