using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
// Isolated fixture only. Orders during the observed trial are issued through the Game view.
public static class KiteUser1112
{
 static UserSettingsData saved; static string savedRoot; static CommanderAnt me,other; static WildMonster enemy,otherEnemy;
 static float next; static int frames; static string dir="work/commander-only-1112";
 static void Warp(Component c,Vector3 p){if(!NavMesh.SamplePosition(p,out var h,8,NavMesh.AllAreas))throw new Exception("No navigation position"); c.GetComponent<NavMeshAgent>().Warp(h.position);}
 public static async Task<string> Setup(){
  var deadline=DateTime.UtcNow.AddSeconds(90);
  while(SaveSystem.Busy){if(DateTime.UtcNow>deadline)throw new Exception("initial load timeout");await Task.Delay(50);}
  saved=UserSettings.Current.Clone();savedRoot=SaveStorage.RootOverride;
  var quiet=saved.Clone();quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false;UserSettings.Apply(quiet,false);
  SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"KiteUser1112-"+Guid.NewGuid().ToString("N"));
  SaveSystem.NewGame(new NewGameOptions{seed=261112,mapSize=MapSize.Small,startingCount=3,startingTeam=Enumerable.Range(0,3).Select(i=>StartingCandidate.Roll(i)).ToArray()});
  while(SaveSystem.Busy||WaveDefense.Instance==null||CommanderRoster.Instance==null||CommanderRoster.Instance.Commanders.Count(c=>c!=null&&c.IsColonyMember)<3){if(DateTime.UtcNow>deadline)throw new Exception("new game timeout");await Task.Delay(50);}
  GameMenuController.Instance.Resume();Time.timeScale=0;
  var team=CommanderRoster.Instance.Commanders.Where(c=>c!=null&&!c.IsDead&&c.IsColonyMember).ToArray();me=team[0];other=team[1];
  foreach(var c in team){c.SetJobEnabled(CommanderJobs.All,false);c.CommandStop();}
  var home=(Vector3)typeof(ResourceManager).GetProperty("HomePosition",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
  var local=SceneObjects.Any<LocalIncursions>();var center=home+new Vector3(12,0,-13);
  var spawn=typeof(LocalIncursions).GetMethod("RestoreSpawn",BindingFlags.Instance|BindingFlags.NonPublic);
  enemy=(WildMonster)spawn.Invoke(local,new object[]{false,center+Vector3.right*5});
  otherEnemy=(WildMonster)spawn.Invoke(local,new object[]{false,center+new Vector3(4,0,18)});
  foreach(var e in new[]{enemy,otherEnemy}){e.MakeRaider();e.gameObject.SetActive(true);} typeof(WildMonster).GetMethod("ConfigureForce",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(enemy,new object[]{150f}); enemy.RestoreHealth(150f);
  Warp(me,center+Vector3.left*5);Warp(enemy,center+Vector3.right*5);
  Warp(other,center+new Vector3(0,0,18));Warp(otherEnemy,other.Position+Vector3.right*4);Warp(team[2],home+new Vector3(-25,0,0));
  me.PersonalState.equipment.RemoveAll(e=>e.slot==EquipmentSlot.Weapon);me.PersonalState.equipment.Add(new EquipmentItem{slot=EquipmentSlot.Weapon,weapon=WeaponKind.ShortBow,quality=1});me.RefreshEquipment();
  // A second deployed commander is left to fight automatically without subsequent orders.
  foreach(var c in new[]{me,other}) typeof(CommanderAnt).GetMethod("Mobilize",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{0,c.Position});
  var camera=UnityEngine.Camera.main;var controller=camera.GetComponent<AntColony.Camera.IsometricCameraController>();
  typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,new object[]{center,0f,10f}); controller.ApplySettings(25f,0f);
  Directory.CreateDirectory(dir);File.WriteAllText(dir+"/user-kite.csv","frame,time,actor,health,enemyHealth,x,z,enemyX,enemyZ,windup,cooldown,selected\n");
  EditorApplication.update-=Observe;EditorApplication.update+=Observe;next=0;frames=0;
  return "READY paused; select bow commander and resume using UI. Enemy HP150 fixture; player and both combat/move speeds unchanged. "+Snapshot();
 }
 static void Observe(){if(!EditorApplication.isPlaying||me==null)return;frames++;if(Time.time<next)return;next=Time.time+.05f;Row(me,enemy,"player");Row(other,otherEnemy,"auto");}
 static void Row(CommanderAnt c,WildMonster e,string name){if(c==null||e==null)return;File.AppendAllText(dir+"/user-kite.csv",$"{Time.frameCount},{Time.time:F3},{name},{c.PersonalHealth:F2},{e.CurrentHealth:F2},{c.Position.x:F3},{c.Position.z:F3},{e.Position.x:F3},{e.Position.z:F3},{c.IsWindingUp},{c.AttackCooldownLeft:F3},{c.GetComponent<SelectableObject>().IsSelected}\n");}
 public static string Snapshot()=>me==null?"no fixture":$"frames={frames} t={Time.time:F2} hp={me.PersonalHealth:F2} enemy={enemy?.CurrentHealth:F2} pos={me.Position} enemyPos={enemy?.Position} windup={me.IsWindingUp} cooldown={me.AttackCooldownLeft:F2} otherHp={other?.PersonalHealth:F2} otherEnemy={otherEnemy?.CurrentHealth:F2}";
 public static string Restore(){EditorApplication.update-=Observe;if(saved!=null)UserSettings.Apply(saved,false);SaveStorage.RootOverride=savedRoot;return "settings/root restored";}
}
