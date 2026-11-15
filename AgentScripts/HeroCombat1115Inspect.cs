using System.Linq;
using System.Reflection;
using AntColony.Units;
using AntColony.Core;
using UnityEngine;
public static class HeroCombat1115Inspect {
 public static string Main()=>string.Join("\n",SceneObjects.All<CommanderAnt>().Where(c=>c.IsColonyMember&&!c.IsDead).Select(c=>{
  var target=(IDamageable)typeof(SoldierAnt).GetField("currentTarget",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);
  var a=c.GetComponent<UnityEngine.AI.NavMeshAgent>();
  return c.CommanderName+" hp="+c.PersonalHealth+" pos="+c.Position+" target="+(target as Component)?.name+" targetpos="+target?.Position+" targetDead="+target?.IsDead+" path="+a.pathStatus+" remaining="+a.remainingDistance+" stopping="+a.stoppingDistance+" destination="+a.destination+" velocity="+a.velocity+" winding="+c.IsWindingUp+" cooldown="+c.AttackCooldownLeft;
 }));
}
