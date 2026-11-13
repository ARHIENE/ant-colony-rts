using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEditor;using AntColony.Buildings;using AntColony.Data;using AntColony.UI;
public static class BuildingInventory1114
{
 public static string Run(){
 var flags=BindingFlags.NonPublic|BindingFlags.Static;
 var tabs=(Array)typeof(BuildScreen).GetField("Tabs",flags).GetValue(null);
 var names=(string[])typeof(BuildScreen).GetField("TabNames",flags).GetValue(null);
 var rows=new List<(string category,string name,BuildingKind kind,string role)>();
 for(int i=0;i<tabs.Length;i++)foreach(var e in (IEnumerable)tabs.GetValue(i)){var t=e.GetType();rows.Add((names[i],(string)t.GetField("name").GetValue(e),(BuildingKind)t.GetField("kind").GetValue(e),t.GetField("role").GetValue(e).ToString()));}
 var unique=rows.GroupBy(r=>r.kind).Select(g=>g.First()).ToArray();var kinds=unique.Select(r=>r.kind).ToHashSet();
 var load=typeof(FacilityVisual).GetMethod("LoadModel",flags);
 var assets=unique.Select(r=>(r,path:AssetDatabase.GetAssetPath((GameObject)load.Invoke(null,new object[]{r.kind,ResourceType.Soil})))).ToArray();
 var cat=FurnitureCatalog.All;var planned=cat.Where(f=>!f.Existing.HasValue).ToArray();var retired=cat.Where(f=>f.Existing.HasValue&&!kinds.Contains(f.Existing.Value)).ToArray();
 Directory.CreateDirectory("work/building-inventory-1114");
 File.WriteAllLines("work/building-inventory-1114/buildable.tsv",new[]{"category\tname\tkind\trole\tasset"}.Concat(assets.Select(a=>$"{a.r.category}\t{a.r.name}\t{a.r.kind}\t{a.r.role}\t{a.path}")));
 File.WriteAllLines("work/building-inventory-1114/catalog.tsv",new[]{"category\tname\tkind\tstatus"}.Concat(cat.Select(f=>$"{f.Category}\t{f.Name}\t{f.Existing}\t{(!f.Existing.HasValue?"plan_only":kinds.Contains(f.Existing.Value)?"menu":"not_in_menu")}")));
 return $"catalog={cat.Count}; planOnly={planned.Length}; catalogLinkedRows={cat.Count-planned.Length}; catalogLinkedKinds={cat.Where(f=>f.Existing.HasValue).Select(f=>f.Existing.Value).Distinct().Count()}; linkedButNotInMenu={retired.Length}; menuEntries={rows.Count}; uniqueMenuKinds={unique.Length}; modelAsset={assets.Count(a=>!string.IsNullOrEmpty(a.path))}; improved={assets.Count(a=>!string.IsNullOrEmpty(a.path)&&!a.path.Contains("/Facilities/"))}\n"+
 string.Join("\n",assets.GroupBy(a=>a.r.category).Select(g=>$"{g.Key}: kinds={g.Count()}, assets={g.Count(a=>!string.IsNullOrEmpty(a.path))}"))+"\nNoModels="+string.Join(",",assets.Where(a=>string.IsNullOrEmpty(a.path)).Select(a=>a.r.kind))+"\nNonMenuCatalog="+string.Join(",",retired.Select(f=>f.Name+":"+f.Existing));
 }
}
