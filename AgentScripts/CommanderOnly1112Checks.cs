using System;
using System.Linq;
using System.Reflection;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
public static class CommanderOnly1112Checks
{
 public static string Main(){
  var c=CommanderRoster.Instance.Commanders.First(x=>x!=null&&!x.IsDead&&x.IsColonyMember);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  int pass=0;void Check(bool good,string label){if(!good)throw new Exception("FAIL "+label);pass++;}
  float hp=c.CurrentHealth,attack=c.AttackDamage;int free=AntPool.Instance.Free;
  typeof(CommanderAnt).GetMethod("RestoreTroops",flags).Invoke(c,new object[]{20,.5f});
  Check(c.TroopCount==0&&!c.HasTroops,"legacy troop load discards troops");
  Check(Mathf.Approximately(c.CurrentHealth,hp)&&Mathf.Approximately(c.MaxHealth,GameBalance.CommanderHealth),"personal HP unchanged by old troops");
  Check(Mathf.Approximately(c.AttackDamage,attack),"attack unchanged by old troops");
  Check(!c.TryAssign(10)&&!c.TryAssign(int.MinValue),"all legacy assignment inputs refused");
  Check(AntPool.Instance.Free==free,"assignment consumes no population");
  Check(c.CanFight,"commander can fight with zero troops");
  Check(c.DeploymentPortion==c.RationPortion,"deployment food is personal portion");
  return "PASS "+pass+" commander-only checks";
 }
}
