using System.Linq;
using AntColony.Units;
using UnityEngine;
public static class Storybook1105Inspect
{
 public static string Main()
 {
  var host=new GameObject("inspect");host.transform.position=new Vector3(500,500,500);
  try {
   var m=KemonoModel.Create(host.transform,1105);var a=RigAnimator.Find(m);a.Play("Base Layer.Idle_A",0,.2f);a.Update(.01f);
   var rs=m.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).ToArray();
   return string.Join("\n",m.GetComponentsInChildren<Transform>().Where(t=>new[]{"Kemono","Upright","KemonoRig","Root","head"}.Contains(t.name)).Select(t=>$"{t.name} p={t.position} r={t.eulerAngles} s={t.lossyScale}")) + "\n" +string.Join("\n",rs.Select(r=>$"{r.name}: center={r.bounds.center}, size={r.bounds.size}, scale={r.transform.lossyScale}"));
  } finally {Object.DestroyImmediate(host);}
 }
}
