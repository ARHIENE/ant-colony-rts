using System;
using System.IO;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
public static class SoilSupply1115Checks
{
 public static string Main(){
  var rows=new System.Collections.Generic.List<string>();
  void Check(bool ok,string note){if(!ok)throw new Exception(note);rows.Add("PASS "+note);}
  var q=SceneObjects.All<Processor>().Single(p=>p.isActiveAndEnabled&&!p.IsDead&&p.Data.kind==BuildingKind.Quarry);
  Check(q.Current!=null&&q.Current.output==ResourceType.Stone&&q.Progress>.8f,"Existing save retains unfinished stone batch");
  Check(q.Orders.Any(o=>o.recipe==0)&&q.Recipes[0].output==ResourceType.Stone,"Existing saved stone order retains recipe identity");
  var soil=q.Recipes.Single(r=>r.output==ResourceType.Soil);
  Check(q.RecipeUnlocked(soil)&&soil.work>0&&q.Demand==0,"Soil is available as manual work without electricity");
  var team=CommanderRoster.Instance.Commanders;
  Check(team.Any(c=>ResourceIndustry.CanWork(c,BuildingKind.Quarry))&&team.Any(c=>!ResourceIndustry.CanWork(c,BuildingKind.Quarry)),"Real stone-tool worker eligible; basic-tool worker blocked");
  Check(!q.Orders.Any(o=>q.Recipes[o.recipe].output==ResourceType.Soil),"Adding recipe does not add a production order or grant soil");
  var result=string.Join("\n",rows);File.WriteAllText("work/colony-polish-1115/soil-supply.txt",result);return result;
 }
}
