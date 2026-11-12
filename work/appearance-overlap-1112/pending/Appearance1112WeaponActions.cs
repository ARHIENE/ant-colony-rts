using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Units;
using AntColony.Core;
public static class Appearance1112WeaponActions
{
 public static async Task<string> Run()
 {
  var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember);var v=c.GetComponent<AntVisual>();
  var oldDuty=c.WorkState.duty; var oldHold=c.WorkState.holdDeployment; c.WorkState.duty=CommanderDuty.Deployed; c.WorkState.holdDeployment=true; var old=c.PersonalState.equipment.ToArray();var report=new List<string>();
  try
  {
   foreach(WeaponKind kind in Enum.GetValues(typeof(WeaponKind)))
   {
    c.PersonalState.equipment.RemoveAll(e=>e.slot==EquipmentSlot.Weapon);
    var item=new EquipmentItem{slot=EquipmentSlot.Weapon,weapon=kind};c.PersonalState.equipment.Add(item);c.RefreshEquipment();
    c.CommandStop();v.Attack(c.Position+Vector3.forward*4);await Task.Delay(180);
    string expected=MeshyEquipmentRules.IsBow(kind)?"BowShoot":kind==WeaponKind.Shield?"ShieldBlock":kind==WeaponKind.Mandible?"SpearThrust":kind==WeaponKind.QuakeMaul?"Tool_Hammer":MeshyEquipmentRules.IsFirearm(kind)||kind==WeaponKind.AcidSprayer||kind==WeaponKind.Pheromone?"Idle_A":"Attack";
    if(v.MotionName!=expected)throw new Exception(kind+" mapped to "+v.MotionName+" expected "+expected);
    if(v.HeldTool!=MeshyEquipmentRules.HeldModel(item))throw new Exception("Wrong tool "+kind);
    if(!c.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&!r.forceRenderingOff&&r.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith(v.HeldTool))))throw new Exception("Invisible equipment "+kind);
    report.Add(kind+" PASS "+v.MotionName+" / "+v.HeldTool+" native="+c.GetComponent<MeshyNativeMotionPlayer>()?.PlayingMotion);
   }
   System.IO.File.WriteAllLines("work/animation-1111/weapon-actions.txt",report);return string.Join("\n",report);
  }
  finally{c.WorkState.duty=oldDuty;c.WorkState.holdDeployment=oldHold;c.PersonalState.equipment.Clear();c.PersonalState.equipment.AddRange(old);c.RefreshEquipment();v.Action("Idle_A");}
 }
}

