using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;
using AntColony.Core;using AntColony.Save;using AntColony.Units;using AntColony.World;using AntColony.UI;using UnityEngine;
public static class EscortReinforcement1115Checks
{
 const string FileName="work/campaign-complete-1115/reinforcement.txt";
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);File.AppendAllText(FileName,"PASS "+message+"\n");}
 public static async Task<string> Main()
 {
  if(string.IsNullOrEmpty(SaveStorage.RootOverride))throw new Exception("Isolated checkpoint required");
  UnityEditor.EditorApplication.isPaused=false;Time.timeScale=0;File.WriteAllText(FileName,"RUNNING reinforcement round trip\n");
  var party=WorldMapManager.Instance.Transports.First(t=>t.State==ExpeditionState.Outbound&&t.Escorts.Any());
  var c=party.Escorts.First();string id=c.PersonalState.id;Vector3 home=c.WorkState.returnPosition;
  party.CancelCrewOrder(c);c.CommandStop();typeof(CommanderAnt).GetMethod("FinishEscort",Hidden).Invoke(c,null);
  Check(c.Agent.Warp(home),"Place reinforcement at home checkpoint");
  Check(party.AssignEscorts(new[]{c}),"Assign reinforcement after expedition departure");
  typeof(ExpeditionTransport).GetMethod("TickCrewOrders",Hidden).Invoke(party,null);
  Check(party.Escorts.Contains(c)&&c.IsFollowingCombatOrder,"First tick does not finish reinforcement at its starting home position");
  Check(SaveSystem.TrySave(false,0,out var error),"Save reinforcement departure: "+error);
  Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"Reload reinforcement departure: "+error);
  var until=DateTime.UtcNow.AddSeconds(90);while(SaveSystem.Busy&&DateTime.UtcNow<until)await Task.Delay(100);
  Check(!SaveSystem.Busy,"Reinforcement reload completed");Time.timeScale=0;
  c=CommanderRoster.Instance.Commanders.First(x=>x.PersonalState.id==id);
  Check(ExpeditionTransport.IsEscorting(c)&&c.IsFollowingCombatOrder,"Reinforcement movement survives full scene reload");
  float furthest=0;SimulationPause.UserSpeed(3);until=DateTime.UtcNow.AddSeconds(120);
  try{while(ExpeditionTransport.IsEscorting(c)&&DateTime.UtcNow<until){furthest=Mathf.Max(furthest,Vector3.Distance(home,c.Position));await Task.Delay(100);}}
  finally{SimulationPause.UserSpeed(0);}
  Check(furthest>10,"Reinforcement physically left home, distance="+furthest);
  Check(!ExpeditionTransport.IsEscorting(c)&&!c.IsDeployed&&Vector3.Distance(home,c.Position)<4,"Reinforcement returned home and resumed civilian duty");
  return File.ReadAllText(FileName);
 }
}
