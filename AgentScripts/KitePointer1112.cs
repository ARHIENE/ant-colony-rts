using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
public static class KitePointer1112
{
 static Mouse mouse;static string log="";static int pass,fail;
 static void Note(string s){log+=s+"\n";File.WriteAllText("work/commander-only-1112/pointer-replay.txt",log);}
 static void Check(bool ok,string s){if(ok)pass++;else fail++;Note((ok?"PASS ":"FAIL ")+s);}
 static async Task Frames(int n){int end=Time.frameCount+n;var timeout=DateTime.UtcNow.AddSeconds(10);while(Time.frameCount<end){if(DateTime.UtcNow>timeout)throw new Exception("frames stalled");await Task.Delay(8);}}
 static async Task Click(Vector3 world,MouseButton button,Component follow=null){var p=(Vector2)UnityEngine.Camera.main.WorldToScreenPoint(world);mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,new MouseState{position=p});await Frames(2);await Task.Delay(100);if(follow!=null)p=(Vector2)UnityEngine.Camera.main.WorldToScreenPoint(follow.transform.position+Vector3.up*.6f);mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,new MouseState{position=p}.WithButton(button));await Frames(2);await Task.Delay(100);mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,new MouseState{position=p});await Frames(2);await Task.Delay(100);Physics.SyncTransforms(); Physics.Raycast(UnityEngine.Camera.main.ScreenPointToRay(p),out var hit,500f); Note($"click {button} screen={p} t={Time.time:F3} hit={hit.collider?.name} current={Mouse.current?.name} ui={UnityEngine.EventSystems.EventSystem.current?.IsPointerOverGameObject()}");}
 static async Task<bool> Until(Func<bool> f,float seconds){float end=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(30);while(Time.time<end&&!f()){if(DateTime.UtcNow>deadline)throw new Exception("simulation stalled");await Task.Delay(8);}return f();}
 public static async Task<string> Main(){
  log="";pass=fail=0;var t=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("KiteUser1112")).Where(t=>t!=null).Last(t=>t.GetField("me",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null) is CommanderAnt c&&c!=null);
  CommanderAnt me=(CommanderAnt)t.GetField("me",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);WildMonster enemy=(WildMonster)t.GetField("enemy",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);WildMonster other=(WildMonster)t.GetField("otherEnemy",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
  var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode; var oldMouse=Mouse.current;var background=InputSystem.settings.backgroundBehavior;
  try{
   InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;mouse=InputSystem.AddDevice<Mouse>("KitePointerTest");mouse.MakeCurrent();
   Note("Unity Input System pointer replay at 1x; no direct combat/move method calls; visible CLI-driven input trial.");
   await Click(me.Position+Vector3.up*.6f,MouseButton.Left);
   Check(me.GetComponent<SelectableObject>().IsSelected,"left click selects bow commander");
   if(!me.GetComponent<SelectableObject>().IsSelected)return $"FAIL selection\n{log}";
   float otherBefore=other.CurrentHealth;Time.timeScale=1;float initial=me.PersonalHealth;int cycles=0;
   for(int i=0;i<3&&enemy!=null&&!enemy.IsDead;i++){
    float hp=enemy.CurrentHealth;await Click(enemy.Position+Vector3.up*.6f,MouseButton.Right,enemy); Check(me.IsAttacking(enemy),$"cycle {i+1}: explicit enemy attack target retained");
    bool hit=await Until(()=>enemy==null||enemy.CurrentHealth<hp-.01f,7);Check(hit,$"cycle {i+1}: right-click enemy causes damage");if(!hit||enemy==null||enemy.IsDead)break;
    var start=me.Position;float cooldown=me.AttackCooldownLeft;var away=(me.Position-enemy.Position);away.y=0;away.Normalize();
    Vector3 goal=start+away*4; var screen=UnityEngine.Camera.main.WorldToScreenPoint(goal); Check(screen.x>=0&&screen.x<UnityEngine.Screen.width&&screen.y>=0&&screen.y<UnityEngine.Screen.height,$"cycle {i+1}: retreat click inside visible Game view");
    await Click(goal,MouseButton.Right);await Until(()=>Vector3.Distance(me.Position,start)>1.5f,3);
    Check(Vector3.Distance(me.Position,start)>1.5f,$"cycle {i+1}: ground right click retreats {Vector3.Distance(me.Position,start):F2}m");
    Check(!me.IsAttacking(enemy),$"cycle {i+1}: move keeps priority over automatic attack");
    Note($"cycle {i+1}: hp={me.PersonalHealth:F1} enemy={enemy.CurrentHealth:F1} cooldown={cooldown:F2}->{me.AttackCooldownLeft:F2}");if(Vector3.Distance(me.Position,start)>1.5f)cycles++;
   }
   Check(cycles>=2,"at least two attack/retreat cycles through actual input controllers");
   Check(other==null||other.CurrentHealth<otherBefore,"unselected commander keeps fighting automatically");
   Check(!me.IsDead,$"controlled commander survives {initial:F1}->{me.PersonalHealth:F1}");
   return $"DONE pass={pass} fail={fail}\n{log}";
  }finally{Time.timeScale=0;if(mouse!=null)InputSystem.RemoveDevice(mouse);if(oldMouse!=null)oldMouse.MakeCurrent();InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;}
 }
}
