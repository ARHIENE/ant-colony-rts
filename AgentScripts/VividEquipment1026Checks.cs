using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Object=UnityEngine.Object;
public static class VividEquipment1026Checks
{
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL "+checks+" "+label);checks++;File.WriteAllText("Temp/vivid1026-progress.txt",checks+" "+label);}
 static Vector3[] Pose(GameObject model)
 {
  var r=model.GetComponentsInChildren<SkinnedMeshRenderer>().First(x=>x.enabled&&x.name.StartsWith("Part_Body_"));var mesh=new Mesh();r.BakeMesh(mesh);var vertices=mesh.vertices;Object.Destroy(mesh);return vertices;
 }
 static Vector3[] ToolBounds(Transform item,Transform socket) => item.GetComponentsInChildren<MeshFilter>(true).SelectMany(m=>
  Enumerable.Range(0,8).Select(i=>{var b=m.sharedMesh.bounds;var v=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));return socket.InverseTransformPoint(m.transform.TransformPoint(v));})).ToArray();
 public static async Task<string> Main(bool motion=true)
 {
  checks=0;var scale=Time.timeScale;GameObject owner=null;
  try
  {
   Check(Application.isPlaying&&AntColony.Core.GameSession.Instance.GameStarted,"started game in Play mode");AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=1;owner=new GameObject("Vivid animation verification");var model=AntColony.Units.KemonoModel.Create(owner.transform,37);var animator=AntColony.Units.RigAnimator.Find(model);
   var controller=(AnimatorController)AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Resources/Kemono/KemonoAnimator.controller");
   var states=controller.layers[0].stateMachine.states.Select(x=>x.state).Where(s=>s.motion is AnimationClip).GroupBy(s=>s.motion).Select(g=>g.First()).ToArray();
   Check(states.Length==88,"all 88 character clips connected");
   model.GetComponent<AntColony.Units.KemonoAppearance>().SetArmor(true);
   var armor=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.StartsWith("Part_Equipment_Armor_")).ToArray();
   Check(armor.Length>0&&armor.All(r=>r.enabled&&r.bones.All(b=>b!=null)&&r.sharedMaterials.All(m=>m!=null)),"worn armor parts use real rig bones and materials");
   if(motion) foreach(var s in states)
   {
    var clip=(AnimationClip)s.motion;Check(clip.length>0&&clip.isHumanMotion==false,"valid Generic clip "+s.name);
    animator.Play("Base Layer."+s.name,0,0);await Task.Delay(50);var before=Pose(model);await Task.Delay(Mathf.Clamp((int)(clip.length*420),100,450));var after=Pose(model);
    var movement=before.Select((v,i)=>(v-after[i]).sqrMagnitude).Max();
    Check(after.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),"finite mesh "+s.name);
    Check(after.All(v=>v.magnitude<3),"mesh stays within character scale "+s.name);
    Check(movement>.0000001f,"visible real-frame movement "+s.name+" delta="+Mathf.Sqrt(movement));
   }
   animator.Play("Base Layer.Tool_Chop",0,0);await Task.Delay(80);
   var socket=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Socket.Tool.R");
   foreach(var path in Directory.GetFiles("Assets/Resources/Kemono/Handheld","*.fbx"))
   {
    var name=Path.GetFileNameWithoutExtension(path);AntColony.Units.KemonoModel.Hold(model,name);await Task.Yield();
    var grip=name=="Weapon_ShortBow"||name.StartsWith("Weapon_Shield_") ? model.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Socket.Tool.L") : socket;
    var item=grip.Cast<Transform>().FirstOrDefault(t=>t.gameObject.activeSelf&&t.name==name+"(Clone)");
    Check(item!=null,"handheld loaded "+name);Check(item.localPosition.sqrMagnitude<.0000001f&&Quaternion.Angle(item.localRotation,Quaternion.identity)<.01,"grip origin "+name);
    var vertices=ToolBounds(item,grip);
    Check(vertices.Length>0&&vertices.All(v=>v.magnitude<2),"no preview-grid displacement "+name+" vertices="+vertices.Length+" max="+(vertices.Length>0?vertices.Max(v=>v.magnitude):0)+" components="+string.Join(",",item.GetComponents<Component>().Select(c=>c.GetType().FullName)));
   }
   AntColony.Units.KemonoModel.Hold(model,"Tool_Axe_Stone");var axe=socket.Cast<Transform>().First(t=>t.gameObject.activeSelf&&t.name=="Tool_Axe_Stone(Clone)");var axis=ToolBounds(axe,socket);
   Check(axis.Max(v=>v.magnitude)>.25&&axis.Max(v=>v.magnitude)<.5,"axe shaft extends from grip to head without library offset");
   var result="PASS "+checks+" checks: "+(motion?"88 real-frame deformed character clips and ":"")+"72 handheld grip/material models";File.WriteAllText("Temp/vivid1026-result.txt",result);return result;
  }
  catch(Exception e){File.WriteAllText("Temp/vivid1026-result.txt",e.ToString());throw;}
  finally{if(owner!=null)Object.Destroy(owner);Time.timeScale=scale;}
 }
}
