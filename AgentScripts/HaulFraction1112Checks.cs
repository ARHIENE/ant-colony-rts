using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using AntColony.Units;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;
using Object=UnityEngine.Object;
public static class HaulFraction1112Checks {
 public static string Run(){
  var c=CommanderRoster.Instance.Commanders.First(x=>x!=null&&!x.IsDead&&!x.Health.Downed);c.CommandStop();
  ResourceNode Make(float amount){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.SetActive(false);go.transform.position=c.Position+c.transform.right*2;go.transform.localScale=Vector3.one*.6f;var n=go.AddComponent<ResourceNode>();n.ConfigureLoot(ResourceType.Soil,amount);go.SetActive(true);return n;}
  var tiny=Make(.25f);var whole=Make(10f);
  try {
   Func<ResourceNode,bool> start=n=>((IEnumerable<(Component target,Func<bool> start)>)typeof(CommanderAnt).GetMethod("AutoCandidates",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(c,new object[]{CommanderJobs.Hauling})).First(x=>x.target==n).start();
   if(start(tiny))throw new Exception("Fraction scheduled an endless storage round trip");
   if(!start(whole))throw new Exception("Whole-unit hauling stopped working");
   c.CommandStop();c.CommandGather(tiny);
   if(c.CurrentResourceNode!=tiny)throw new Exception("Manual fraction collection was removed");
   if(Mathf.Abs(tiny.AmountRemaining-.25f)>.0001f)throw new Exception("Fraction was destroyed or rounded away");
   return "PASS: automatic fraction rejected, whole load accepted, manual fraction retained, resource amount unchanged";
  } finally {c.CommandStop();Object.Destroy(tiny.gameObject);Object.Destroy(whole.gameObject);}
 }
}
