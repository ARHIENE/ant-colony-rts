using System.Linq;
using System.Reflection;
using AntColony.Buildings;
using AntColony.Data;
using UnityEngine;
public static class BuildingCosts1115Inspect {
 public static string Main()=>string.Join("\n",new[]{BuildingKind.AcidTower,BuildingKind.Hearth,BuildingKind.StarterMushroomBed}.Select(k=>{
  var go=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{k,UnitRole.Worker});
  return k+" "+JsonUtility.ToJson(go.GetComponent<BuildingBase>().Data);
 }));
}
