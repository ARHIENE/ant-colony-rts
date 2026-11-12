using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using UnityEditor;using AntColony.Units;using AntColony.Core;
public static class Life1112Checks {
 public static async Task<string> Run(){
 if(!Application.isPlaying)throw new Exception("Isolated Play required");
 var units=CommanderRoster.Instance.Commanders.Where(c=>c&&c.IsColonyMember).ToArray();
 foreach(var c in units){c.CommandStop();c.WorkState.jobs=CommanderJobs.None;}
 var one=units[0];var t=typeof(CommanderAnt);var timer=t.GetField("idleLeisureUntil",BindingFlags.Instance|BindingFlags.NonPublic);var start=t.GetMethod("TryIdleLeisure",BindingFlags.Instance|BindingFlags.NonPublic);
 var seen=new System.Collections.Generic.HashSet<string>();
 var random=UnityEngine.Random.state;
 try{for(int i=0;i<40;i++){one.CommandStop();timer.SetValue(one,0f);UnityEngine.Random.InitState(i);start.Invoke(one,null);if(one.IdleLeisureMotion!=null)seen.Add(one.IdleLeisureMotion);if(one.ServiceTarget!=null||one.CurrentResourceNode!=null||one.ScienceAssignment!=null||one.ConstructionTarget!=null)throw new Exception("Unrequested work started");}}
 finally{UnityEngine.Random.state=random;}
 if(!seen.Contains("IdleRest")||!seen.Contains("IdleLook"))throw new Exception("No varied rest/look: "+string.Join(",",seen));
 one.CommandStop();one.CommandAttackMove(one.Position+Vector3.forward*6);timer.SetValue(one,0f);start.Invoke(one,null);if(one.IdleLeisureMotion!=null)throw new Exception("Combat order overridden");one.CommandStop();
 one.WorkState.holdDeployment=true;one.WorkState.duty=CommanderDuty.Deployed;start.Invoke(one,null);if(one.IdleLeisureMotion!=null)throw new Exception("Deployed leisure");one.WorkState.duty=CommanderDuty.Civilian;one.WorkState.holdDeployment=false;
 foreach(var action in new[]{"IdleTalk","IdleRest","IdleLook"}){
 one.CommandStop();t.GetField("idleLeisureMotion",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(one,action);timer.SetValue(one,Time.time+20);await Task.Delay(250);
 var p=one.GetComponent<MeshyNativeMotionPlayer>();var binding=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime").actions.First(x=>x.action==action);if(p.PlayingMotion!=binding.motion)throw new Exception(action+" not playing: "+p.PlayingMotion);
 }
 var result="Idle choices="+string.Join(",",seen)+"; 40 choices created no unrequested work; attack-move/deployment blocked; three native actions played on actual commander";
 Directory.CreateDirectory("work/life-assets-1112");File.WriteAllText("work/life-assets-1112/leisure-check.txt",result);return result;
 }
}
