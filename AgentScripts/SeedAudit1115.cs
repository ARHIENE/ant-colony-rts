using System;using System.Linq;using AntColony.Core;using AntColony.Units;using AntColony.World;using AntColony.Data;using UnityEngine;
public static class SeedAudit1115 {
 public static string Main(){return string.Join("\n",ResourceNode.Available.Where(n=>n.ResourceType==ResourceType.PlantingMaterial).Select(n=>n.name+" pos="+n.transform.position+" amount="+n.AmountRemaining+" loose="+n.IsLooseCargo+" gather="+n.CanGather))+"\n"+string.Join("\n",CommanderRoster.Instance.Commanders.Select(c=>c.CommanderName+" hygiene="+JsonUtility.ToJson(c.PersonalState.hygiene)+" can="+c.CanReceiveOrders+" labBusy="+c.LabUpgradeBusy+" sleep="+c.IsSleepTime+" sat="+c.Satiety));}
}
