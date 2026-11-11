using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AntColony.Units;
public static class Meshy1111WearableChecks
{
 public static string Run()
 {
  var library=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");var report=new List<string>();
  for(int rig=0;rig<library.characters.Length;rig++)foreach(var kind in new[]{"Coating","Wings","Durable","HeatShield","Move","Gather","Mood","Command","Sword","GrandBow","SingleRifle"})
  {
   var pivot=new GameObject("Wearable check");pivot.transform.localScale=Vector3.one*2;
   var model=UnityEngine.Object.Instantiate(library.characters[rig],pivot.transform,false);
   try
   {
    EquipmentItem armor=null,trinket=null,weapon=null;
    if(kind=="Coating"||kind=="Wings")armor=new EquipmentItem{slot=EquipmentSlot.Armor,armor=(ArmorKind)Enum.Parse(typeof(ArmorKind),kind)};
    else if(kind=="Durable"||kind=="HeatShield")armor=new EquipmentItem{slot=EquipmentSlot.Armor,bossEquipment=(BossEquipmentKind)Enum.Parse(typeof(BossEquipmentKind),kind)};
    else if(kind=="Sword"||kind=="GrandBow"||kind=="SingleRifle")weapon=new EquipmentItem{slot=EquipmentSlot.Weapon,weapon=(WeaponKind)Enum.Parse(typeof(WeaponKind),kind)};
    else trinket=new EquipmentItem{slot=EquipmentSlot.Trinket,effect=(TrinketEffect)Enum.Parse(typeof(TrinketEffect),kind)};
    var before=pivot.GetComponentsInChildren<Renderer>().ToHashSet();var wear=pivot.AddComponent<MeshyEquipmentWearables>();wear.Apply(armor,trinket,weapon,null,false);
    var added=pivot.GetComponentsInChildren<Renderer>().Where(r=>!before.Contains(r)).ToArray();if(added.Length==0)throw new Exception("Missing "+kind);
    var bounds=added.Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
    if(bounds.size.magnitude>2||bounds.size.magnitude<.02f)throw new Exception("Invalid size "+kind+" "+bounds);
    var part=added[0].transform;while(part.parent&&!part.parent.name.StartsWith("mixamorig:"))part=part.parent;
    if(!part.parent)throw new Exception("Unbound "+kind);
    var offset=part.localPosition;part.parent.rotation=Quaternion.Euler(20,30,10)*part.parent.rotation;
    if(Vector3.Distance(offset,part.localPosition)>.0001f)throw new Exception("Attachment drift "+kind);
    if(weapon!=null){wear.Apply(null,null,weapon,MeshyEquipmentRules.HeldModel(weapon),false);if(part.gameObject.activeSelf)throw new Exception("Duplicate held/holstered weapon");}
    report.Add("rig="+rig+" "+kind+" PASS bound="+part.parent.name+" worldSize="+bounds.size);
   }finally{UnityEngine.Object.DestroyImmediate(pivot);}
  }
  System.IO.File.WriteAllLines("work/animation-1111/wearables.txt",report);return string.Join("\n",report);
 }
}
