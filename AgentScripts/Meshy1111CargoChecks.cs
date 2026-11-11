using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using AntColony.Data;
public static class Meshy1111CargoChecks
{
 public static string Run()
 {
  var unit=CommanderRoster.Instance.Commanders.First(c=>c&&c.IsColonyMember);
  var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  var drop=typeof(WorkerAnt).GetMethod("DropCargo",flags);
  AntColony.UI.SimulationPause.UserSpeed(0);unit.CommandStop();drop.Invoke(unit,null);
  var before=UnityEngine.Object.FindObjectsByType<ResourceNode>().ToHashSet();
  typeof(WorkerAnt).GetMethod("RestoreCargo",flags).Invoke(unit,new object[]{12f,ResourceType.Stone,null});drop.Invoke(unit,null);
  var node=UnityEngine.Object.FindObjectsByType<ResourceNode>().Single(n=>!before.Contains(n)&&n.IsLooseCargo);
  var bounds=node.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!r.forceRenderingOff).Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
  var offset=node.transform.position-unit.transform.position;offset.y=0;
  float width=Mathf.Max(bounds.size.x,bounds.size.z);
  if(node.AmountRemaining!=12||unit.IsCarrying||width>.651f||offset.magnitude<.8f)throw new Exception("Drop regression: amount="+node.AmountRemaining+" width="+width+" distance="+offset.magnitude);
  if(Physics.Raycast(bounds.center+Vector3.up*4,Vector3.down,out var ground,12,1<<8,QueryTriggerInteraction.Ignore)&&Mathf.Abs(bounds.min.y-ground.point.y)>.03f)throw new Exception("Pile floats above ground: "+(bounds.min.y-ground.point.y));
  return "PASS: 12 stone preserved, cargo cleared, pile width="+width+"m, horizontal clearance="+offset.magnitude+"m, grounded.";
 }
}
