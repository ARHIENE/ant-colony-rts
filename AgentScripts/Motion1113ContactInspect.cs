using System.Linq;
using UnityEngine;
using AntColony.Core;
using AntColony.Units;
using AntColony.Buildings;
public static class Motion1113ContactInspect
{
 public static string Run()
 {
  var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsGatheringAnimation);var node=c.CurrentResourceNode;
  Physics.Raycast(c.Position+Vector3.up*8,Vector3.down,out var ground,24,1<<8);
  var dir=Vector3.ProjectOnPlane(c.Position-node.transform.position,Vector3.up).normalized;
  string result="actor="+c.Position.ToString("F4")+" ground="+ground.point.ToString("F4")+" target="+node.transform.position.ToString("F4")+" radiusFit="+MeshyWorkContact.TryRadius(node,"Tool_Till",dir,ground.point.y,out var radius)+" radius="+radius;
  foreach(var r in node.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&!r.forceRenderingOff)) result+="\n"+r.name+" ymin="+(r.bounds.min.y-ground.point.y)+" ymax="+(r.bounds.max.y-ground.point.y)+" readable="+r.GetComponent<MeshFilter>().sharedMesh.isReadable;
  System.IO.File.WriteAllText("work/motion-actions-1113/contact-inspect.txt",result);return result;
 }
}
