using System;
using System.Collections.Generic;
using System.Reflection;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using Object=UnityEngine.Object;
public class MeshyEquipmentDummy:MonoBehaviour,IDamageable
{
 public float Received;public int Hits;public bool IsDead=>false;public Vector3 Position=>transform.position;
 public void TakeDamage(float value){Received+=value;Hits++;}
}
public static class MeshyEquipmentFirearmChecks
{
 public static string Main()
 {
  var root=new GameObject("Isolated firearm checks");root.SetActive(false);root.transform.position=new Vector3(5000,5000,5000);
  var data=ScriptableObject.CreateInstance<UnitData>();var targets=new List<GameObject>();int count=0;
  void Check(bool ok,string why){if(!ok)throw new Exception(why);count++;}
  try{
   var c=root.AddComponent<CommanderAnt>();typeof(AntUnitBase).GetProperty("Data").GetSetMethod(true).Invoke(c,new object[]{data});data.attackRange=15;
   MeshyEquipmentDummy Dummy(float x,float z){var go=new GameObject("Isolated target");go.transform.position=root.transform.position+new Vector3(x,0,z);targets.Add(go);return go.AddComponent<MeshyEquipmentDummy>();}
   var front=Dummy(0,5);var behind=Dummy(0,9);var side=Dummy(4,5);var deadDirection=Dummy(0,-5);
   var fire=typeof(CommanderAnt).GetMethod("FireEquipment",BindingFlags.NonPublic|BindingFlags.Instance);
   foreach(var kind in new[]{WeaponKind.SingleRifle,WeaponKind.BurstCarbine,WeaponKind.Scattergun,WeaponKind.SniperRifle})
   foreach(var pierce in new[]{false,true}){
    c.PersonalState.equipment.Clear();c.PersonalState.equipment.Add(new EquipmentItem{slot=EquipmentSlot.Weapon,weapon=kind,quality=1});c.Traits.values.Clear();if(pierce)c.Traits.values.Add(CommanderTrait.DiamondPierce);
    foreach(var target in new[]{front,behind,side,deadDirection}){target.Received=0;target.Hits=0;}
    fire.Invoke(c,new object[]{front,10f});
    Check(front.Hits>0,"No primary hit "+kind);
    Check(side.Hits==0&&deadDirection.Hits==0,"Hit outside firing line "+kind);
    Check(pierce?behind.Hits>0:behind.Hits==0,"Pierce gate "+kind);
    if(kind==WeaponKind.BurstCarbine)Check(front.Hits==3&&(!pierce||behind.Hits==3),"Every burst round must pierce");
    if(kind==WeaponKind.Scattergun&&pierce)Check(behind.Hits==1,"Only central pellet must pierce");
    if(kind==WeaponKind.SingleRifle&&pierce)Check(Mathf.Abs(behind.Received-front.Received*.9f)<.001f,"Pierce falloff");
   }
   return count+" live damage / burst / shotgun central-pellet / range checks passed";
  }finally{foreach(var go in targets)Object.DestroyImmediate(go);Object.DestroyImmediate(root);Object.DestroyImmediate(data);}
 }
}
