using System;
using System.Linq;
using System.Reflection;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
public static class EmptyHome1112Checks
{
 public static string Main(){
  var wave=WaveDefense.Instance;var state=wave.Capture();var local=SceneObjects.Any<LocalIncursions>();
  var before=local.Visitors.ToArray();var team=CommanderRoster.Instance.Commanders.Where(c=>c!=null&&c.isActiveAndEnabled).ToArray();
  try{
   foreach(var c in team)c.gameObject.SetActive(false);
   if(ExpeditionRules.HomeCommanders!=0)throw new Exception("fixture still has home commanders");
   if(!WaveDefense.CanStart||!wave.StartNow())throw new Exception("wave blocked without commanders");
   typeof(WaveDefense).GetMethod("SpawnGroup",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(wave,null);
   if(local.Visitors.Count(v=>v!=null&&!before.Contains(v))==0)throw new Exception("no enemies spawned");
   return "PASS zero-home-commander wave starts and spawns actual enemy group; no defeat rules changed";
  }finally{
   foreach(var v in local.Visitors.Where(v=>v!=null&&!before.Contains(v)).ToArray())v.Wound(1e9f);
   wave.Restore(state);foreach(var c in team)if(c!=null)c.gameObject.SetActive(true);
  }
 }
}
