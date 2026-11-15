using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;
using AntColony.Core;using AntColony.Data;using AntColony.Save;using AntColony.Units;using AntColony.Buildings;using AntColony.World;using UnityEngine;
public static class ColonyPolish1115Checks
{
 const string FileName="work/colony-polish-1115/checks.txt";
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);File.AppendAllText(FileName,"PASS "+text+"\n");}
 public static async Task<string> Main()
 {
  if(!(SaveStorage.RootOverride??"").Contains("ColonyPolish1115")||SaveSystem.Busy)throw new Exception("Paused isolated checkpoint required");
  Time.timeScale=0;File.WriteAllText(FileName,"Disposable injected regression, NOT natural economy proof\n");
  var c=CommanderRoster.Instance.Commanders.First(c=>!c.Traits.Has(CommanderTrait.IronWill)&&!c.Traits.Has(CommanderTrait.Fragile));
  var k=SceneObjects.All<Kitchen>().First(k=>k.isActiveAndEnabled&&!k.IsTable&&!k.IsDead);
  var rm=ResourceManager.Instance;rm.TrySpend(ResourceType.Rice,rm.GetAmount(ResourceType.Rice));rm.Add(ResourceType.Food,20);
  typeof(Kitchen).GetProperty("Meals").SetValue(k,new Kitchen.State{configured=true});
  k.AddOrder(2,Workshop.OrderMode.KeepStock,20);k.AddOrder(0,Workshop.OrderMode.Count,2);
  Check(k.NextPickup(out var type,out var amount)&&type==ResourceType.Food,"Unavailable rice bill does not block simple meal ingredients");
  Check(k.OrderStatus(k.Orders[0]).Contains("쌀"),"Blocked bill names missing rice");
  var courier=new object();Check(k.BeginPickup(courier,type,amount),"Reserve actual meal input");
  k.MoveOrder(k.Orders[1],-1);Check(k.NextPickup(out _,out _)==false,"Reordering does not duplicate reserved input");
  Check(rm.TrySpend(type,amount),"Withdraw real input before delivery");
  Check(k.Deliver(courier,type,amount)==amount,"Reserved delivery remains accepted after reorder");
  Check(k.NeedsCook,"Cooking resumes after delivery");
  var oldStock=ProductionStock.Count(ResourceType.SimpleMeal);Check(k.Work(c,1),"Begin normal cooking work");
  k.Work(c,30);Check(ProductionStock.Count(ResourceType.SimpleMeal)==oldStock+1,"Blocked-queue fallback produces exactly one real meal");
  k.MoveOrder(k.Orders[0],1);Check(k.Orders[0].recipe==2,"Player can restore priority order");
  var choose=typeof(CommanderAnt).GetMethod("ChooseMentalBreak",BindingFlags.Instance|BindingFlags.NonPublic);
  foreach(var target in new[]{30f,15f,0f})
  {
   c.PersonalState.moodFactors.Clear();var baseMood=c.Mood;
   c.PersonalState.moodFactors.Add(new MoodFactor{reason="fixture",value=(target-baseMood)/c.Traits.NegativeMoodMultiplier,remaining=999});
   Check(c.BreakSeverity==(target==30?1:target==15?2:3),"Mood severity for "+target);
   var seen=new System.Collections.Generic.HashSet<MentalBreak>();
   for(int i=0;i<1000;i++)seen.Add((MentalBreak)choose.Invoke(c,null));
   if(target>5)Check(!seen.Contains(MentalBreak.Flee)&&!seen.Contains(MentalBreak.AttackCommander),"Noncritical mood excludes departure and ally attacks");
   if(target>20)Check(!seen.Contains(MentalBreak.AttackBuilding)&&!seen.Contains(MentalBreak.SelfHarm),"Mild mood excludes destructive breakdowns");
   if(target==0)Check(seen.Contains(MentalBreak.AttackCommander)&&seen.Contains(MentalBreak.Flee),"Critical mood retains severe consequences");
  }
  c.PersonalState.moodFactors.Clear();
  foreach(var crop in new[]{"Planted","Young","Growing","Mature","Pest"})
  {
   var asset=Resources.Load<GameObject>("CropStages1115/CC_CropFungus_"+crop);
   Check(asset&&asset.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m&&m.shader.name=="Universal Render Pipeline/Lit")),crop+" crop asset and URP materials connected");
  }
  Check(SaveSystem.TrySave(false,0,out var error),"Save queue and colony state: "+error);
  Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"Reload queue and colony state: "+error);
  var deadline=DateTime.UtcNow.AddSeconds(90);while(SaveSystem.Busy&&DateTime.UtcNow<deadline)await Task.Delay(100);
  Check(!SaveSystem.Busy,"Full scene reload completed");Time.timeScale=0;
  k=SceneObjects.All<Kitchen>().First(k=>k.isActiveAndEnabled&&!k.IsTable&&!k.IsDead);
  Check(k.Orders.Count==2&&k.Orders[0].recipe==2&&k.Orders[1].amount==1,"Queue order and completed count persist after reload");
  return File.ReadAllText(FileName);
 }
}
