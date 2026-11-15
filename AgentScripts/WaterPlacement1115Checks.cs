using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;
using AntColony.Buildings;using AntColony.Core;using AntColony.Data;using AntColony.Units;using AntColony.UI;using UnityEngine;
public static class WaterPlacement1115Checks {
 const string Log="work/colony-polish-1115/water-placement.txt";
 static void Check(bool ok,string text){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new Exception(text);}
 public static string Main(){File.WriteAllText(Log,"Live natural map; read-only rules and rejected placement. No resources or progress injected.\n");
 var source=SupplyFacility.All.First(s=>s.Operational&&s.Kind==BuildingKind.WaterSource);
 foreach(var kind in new[]{BuildingKind.ManualWell,BuildingKind.WaterPump}){
 Check(UndergroundProduction.PlacementReason(kind,source.Position+Vector3.right*3.9f)==null,kind+" accepts location within source range");
 Check(UndergroundProduction.PlacementReason(kind,source.Position+Vector3.right*4.1f)!=null,kind+" rejects location beyond source range");
 }
 var well=SupplyFacility.All.First(s=>s.Operational&&s.Kind==BuildingKind.ManualWell&&!s.NearSource(BuildingKind.WaterSource));
 Check(UndergroundProduction.PlacementReason(BuildingKind.ManualWell,well.Position)!=null,"Existing misplaced well is identified as outside source range");
 var pc=SceneObjects.Any<BuildingPlacementController>();int before=SceneObjects.All<BuildingConstructionSite>().Count();
 Check(pc.BeginPlacement(BuildingKind.ManualWell,UnitRole.Worker,null),"Open ordinary well placement");
 Check(pc.PlaceLine(new List<Vector3>{well.Position},new List<bool>{true})==0&&SceneObjects.All<BuildingConstructionSite>().Count()==before,"Commit rechecks source even when supplied preview is valid");pc.CancelPlacement();
 foreach(var kind in new[]{BuildingKind.ManualWell,BuildingKind.WaterPump,BuildingKind.StarterMushroomBed}){
 var t=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{kind,UnitRole.Worker});
 var text=BuildTooltips.For(kind.ToString(),kind,t.GetComponent<BuildingBase>().Data,"","",ResourceType.Soil);
 Check(!string.IsNullOrEmpty(text)&&(kind==BuildingKind.StarterMushroomBed?text.Contains("물이 없으면"):text.Contains("4m")),kind+" tooltip explains operating dependency");
 }return "PASS water placement and operation guidance";
 }
}
