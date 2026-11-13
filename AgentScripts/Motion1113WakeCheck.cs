using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Core;
using AntColony.Units;
public static class Motion1113WakeCheck
{
 public static async Task<string> Run()
 {
  var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.TrySleepingPose(out _,out _));
  var before=c.Position; var exit=c.Agent.nextPosition;
  GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds,0);
  c.CommandMove(exit+Vector3.forward*3);
  AntColony.UI.SimulationPause.UserSpeed(1);await Task.Delay(1700);AntColony.UI.SimulationPause.UserSpeed(0);
  if(c.IsAsleep || !c.Agent.updatePosition || c.TrySleepingPose(out _,out _) || Vector3.Distance(before,c.Position)<.5f)throw new Exception("Bed exit failed");
  return "PASS wake/move: navigation restored, sleeping placement cleared, distance="+Vector3.Distance(before,c.Position);
 }
}
