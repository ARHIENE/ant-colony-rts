using System.Linq;
using System.Reflection;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
public static class WellWork1115Inspect {
 public static string Main()=>"Water total="+ProductionStock.Count(AntColony.Data.ResourceType.Water)+" target="+StarterCropBed.WaterReserveTarget+"\n"+string.Join("\n",AntColony.World.ResourceNode.Available.Where(n=>n.ResourceType==AntColony.Data.ResourceType.Water).Select(n=>n.name+" pos="+n.transform.position+" amount="+n.AmountRemaining+" loose="+n.IsLooseCargo+" paused="+n.WorkPaused))+"\n"+string.Join("\n",SupplyFacility.All.Where(w=>w.Kind==AntColony.Data.BuildingKind.ManualWell).Select(w=>
  "WELL "+w.Position+" need="+CommanderAnt.ManualWellNeedsWork(w)+" near="+w.NearSource(AntColony.Data.BuildingKind.WaterSource)+" state="+JsonUtility.ToJson(w.Supply)+"\n"+
  string.Join("\n",SceneObjects.All<CommanderAnt>().Where(c=>c.IsColonyMember&&!c.IsDead).Select(c=>{
   object[] args={w,Vector3.zero};var approach=(bool)typeof(CommanderAnt).GetMethod("TryServiceApproach",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,args);
   return c.CommanderName+" ready="+c.CivilianWorkReady+" orders="+c.CanReceiveOrders+" working="+c.IsWorking+" hauling="+c.AllowsJob(CommanderJobs.Hauling)+" priority="+c.JobPriority(CommanderJobs.Hauling)+" approach="+approach+" point="+args[1]+" rawReach="+c.CanReach(w.Position)+" service="+c.ServiceTarget?.name;
  }))));
}
