using System;
using System.IO;
using System.Reflection;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
public static class ProductionSupply1115Checks
{
 public static string Main()
 {
  if(ResourceManager.Instance.GetAmount(ResourceType.Fiber)!=0)throw new Exception("Fixture requires no stocked fiber");
  var log=new System.Collections.Generic.List<string>();
  void Check(bool ok,string note){if(!ok)throw new Exception(note);log.Add("PASS "+note);}
  var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{BuildingKind.SpinningWheel,UnitRole.Worker});
  var go=UnityEngine.Object.Instantiate(template,new Vector3(10000,0,10000),Quaternion.identity);
  var cargo=new GameObject("Temporary supply priority fixture");cargo.SetActive(false);cargo.transform.position=new Vector3(10002,0,10000);
  try {
   go.SetActive(true);var bench=go.GetComponent<Processor>();Check(bench.AddOrder(0,Workshop.OrderMode.Count,1),"Player order accepted");
   var node=cargo.AddComponent<ResourceNode>();node.ConfigureLoot(ResourceType.Fiber,3);cargo.SetActive(true);
   var priority=typeof(CommanderAnt).GetMethod("WorkNeedPriority",BindingFlags.Static|BindingFlags.NonPublic);
   int Rank(bool raw=false,bool cooked=false)=>(int)priority.Invoke(null,new object[]{node,CommanderJobs.Hauling,raw,cooked,false});
   Check(Rank()==1,"Ordered material on floor prioritized without consuming it");
   Check(bench.WaitReason.Contains("섬유 3개 반입 필요"),"Shows actual missing input and quantity");
   bench.Paused=true;Check(Rank()==0,"Paused order does not request collection");bench.Paused=false;
   node.ConfigureNatural(ResourceType.Fiber,3,0);Check(Rank()==0,"Does not designate natural resources");
   node.ConfigureLoot(ResourceType.SimpleMeal,1);Check(Rank(cooked:true)==4,"Ready meals retain higher urgency");
   node.ConfigureLoot(ResourceType.Fiber,3);bench.RemoveOrder(bench.Orders[0]);Check(Rank()==0,"Removed order stops procurement");
   Check(ResourceManager.Instance.GetAmount(ResourceType.Fiber)==0,"Campaign inventory unchanged");
  } finally {go.SetActive(false);cargo.SetActive(false);UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(cargo);}
  var text=string.Join("\n",log);File.WriteAllText("work/colony-polish-1115/production-supply.txt",text);return text;
 }
}
