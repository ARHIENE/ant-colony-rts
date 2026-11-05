using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AntColony.Units;
public static class MeshyRigDiagnostic {
 public static string Main(){var o=Object.Instantiate(Resources.Load<GameObject>("Kemono/Kemono_Character"));try{
 var ts=o.GetComponentsInChildren<Transform>();var positions=ts.ToDictionary(t=>t,t=>t.localPosition);var scales=ts.ToDictionary(t=>t,t=>t.localScale);
 var a=RigAnimator.Bind(o,"KemonoRig",Resources.Load<RuntimeAnimatorController>("Kemono/KemonoAnimator"));a.Rebind();a.Play("Base Layer.Idle_A",0,.2f);a.Update(.01f);
 return string.Join("\n",ts.Where(t=>(t.localPosition-positions[t]).magnitude>.001f||(t.localScale-scales[t]).magnitude>.001f).Select(t=>t.name+" pos "+positions[t].ToString("F4")+" -> "+t.localPosition.ToString("F4")+" scale "+scales[t]+" -> "+t.localScale));
 }finally{Object.DestroyImmediate(o);}}
}
