using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Object=UnityEngine.Object;
public static class AutomaticAnimationChecks
{
 static int checks;
 static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL "+text);checks++;}
 static Vector3[] Pose(GameObject owner){var all=new System.Collections.Generic.List<Vector3>();foreach(var r in owner.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled)){var mesh=new Mesh();r.BakeMesh(mesh);all.AddRange(mesh.vertices.Select(r.transform.TransformPoint));Object.Destroy(mesh);}return all.ToArray();}
 static async Task Motion(GameObject owner,Animator a,string state)
 {
  Check(a.cullingMode==AnimatorCullingMode.AlwaysAnimate,"automatic rig update "+state);a.Play("Base Layer."+state,0,0);await Task.Delay(160);var before=Pose(owner);await Task.Delay(530);var after=Pose(owner);
  Check(before.Length>0&&before.Length==after.Length&&before.Where((v,i)=>(v-after[i]).sqrMagnitude>.000001f).Any(),"real-frame rendered mesh animates "+state);
  System.IO.File.WriteAllText("Temp/automatic-animation-progress.txt",checks+" "+state);
 }
 public static async Task<string> Main()
 {
  checks=0;var time=Time.timeScale;var root=new GameObject("Automatic animation checks");Time.timeScale=1;
  try
  {
   var hero=GameObject.CreatePrimitive(PrimitiveType.Capsule);hero.transform.SetParent(root.transform);AntColony.Units.AntVisual.Attach(hero);hero.GetComponent<AntColony.Units.AntVisual>().enabled=false;
   var animator=AntColony.Units.RigAnimator.Find(hero);
   foreach(var clip in new[]{"Idle_A","Walk","Run","Attack","Hit","Tool_Chop","Tool_Mine","Tool_Harvest","Tool_Dig","Tool_Hammer","BowShoot","Carry","Eat"})await Motion(hero,animator,clip);
   foreach(var name in new[]{"Aphid","Silkworm","Spider","Bee","Wasp","NightBeetle"})
   {
    var model=Object.Instantiate(Resources.Load<GameObject>("Critters/"+name+"/"+name),root.transform);animator=AntColony.Units.RigAnimator.Bind(model,name+"Rig",Resources.Load<RuntimeAnimatorController>("Critters/"+name+"/"+name+"Animator"));await Motion(model,animator,name=="Bee"||name=="Wasp"?"Fly":"Walk");
   }
   var citizen=Object.Instantiate(Resources.Load<GameObject>("Residents/Resident_Combo_A"),root.transform);animator=AntColony.Units.RigAnimator.Bind(citizen,"Combo_A",Resources.Load<RuntimeAnimatorController>("Residents/ResidentAnimator"));await Motion(citizen,animator,"Walk");
   var result="PASS "+checks+" real-frame automatic animation checks; no forced Animator.Update; 13 hero states, 6 wildlife rigs, resident";System.IO.File.WriteAllText("Temp/automatic-animation-result.txt",result);return result;
  }catch(Exception e){System.IO.File.WriteAllText("Temp/automatic-animation-result.txt",e.ToString());throw;}
  finally{Time.timeScale=time;Object.Destroy(root);}
 }
}
