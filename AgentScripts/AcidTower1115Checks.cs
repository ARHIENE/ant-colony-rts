using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AntColony.Buildings;
using UnityEngine;
public static class AcidTower1115Checks
{
 public static string WatchShot(){_=Watch();return "Watching natural combat spray; no combat state changed";}
 static async System.Threading.Tasks.Task Watch(){
  var until=DateTime.UtcNow.AddSeconds(50);
  while(DateTime.UtcNow<until&&Application.isPlaying){
   foreach(var tower in UnityEngine.Object.FindObjectsByType<AcidTower>().Where(t=>t.isActiveAndEnabled&&!t.IsDead)){
    var spray=(LineRenderer)typeof(AcidTower).GetField("spray",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tower);
    if(!spray||!spray.enabled)continue;
    var model=tower.GetComponent<FacilityVisual>()?.Model;if(!model)continue;
    var muzzle=model.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="AcidMuzzle");
    if(!muzzle)continue;
    var distance=Vector3.Distance(spray.GetPosition(0),muzzle.position);
    File.AppendAllText("work/colony-polish-1115/acid-tower.txt",$"\n{(distance<.01f?"PASS":"FAIL")} actual natural combat spray at nozzle, delta={distance}, target={spray.GetPosition(1)}");return;
   }
   await System.Threading.Tasks.Task.Delay(20);
  }
  File.AppendAllText("work/colony-polish-1115/acid-tower.txt","\nUNVERIFIED no shot observed during bounded watch");
 }
 public static string Main()
 {
  var rows=new System.Collections.Generic.List<string>();
  var towers=UnityEngine.Object.FindObjectsByType<AcidTower>().Where(t=>t.isActiveAndEnabled&&!t.IsDead).ToArray();
  if(towers.Length==0)throw new Exception("No live tower");
  foreach(var tower in towers)
  {
   var model=tower.GetComponent<FacilityVisual>().Model;
   var muzzle=model.GetComponentsInChildren<Transform>().Single(t=>t.name=="AcidMuzzle");
   var position=model.position;var rotation=model.rotation;float cooldown=tower.Cooldown;
   var renderer=model.GetComponentsInChildren<Renderer>().First();
   if(renderer.sharedMaterial.shader.name!="Universal Render Pipeline/Lit"||!renderer.sharedMaterial.GetTexture("_BaseMap"))throw new Exception("Tower material missing");
   try {
    foreach(var direction in new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left})
    {
     var actual=(Vector3)typeof(AcidTower).GetMethod("AimVisual",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tower,new object[]{tower.Position+direction*8});
     var facing=actual-tower.Position;facing.y=0;
     if(Vector3.Angle(facing,direction)>.1f||Vector3.Distance(actual,muzzle.position)>.001f)throw new Exception("Muzzle does not follow aim");
     if(actual.y<tower.Position.y+.3f||actual.y>tower.Position.y+5f||facing.magnitude>4)throw new Exception("Muzzle outside tower bounds "+actual);
    }
    if(tower.Cooldown!=cooldown)throw new Exception("Visual changes gameplay cooldown");
    rows.Add("PASS live Meshy tower: textured, four aiming directions, actual nozzle origin, cooldown unchanged at "+tower.Position);
   } finally {model.SetPositionAndRotation(position,rotation);}
  }
  string result=string.Join("\n",rows);File.WriteAllText("work/colony-polish-1115/acid-tower.txt",result);return result;
 }
}
