using System;
using System.Linq;
using System.Reflection;
using AntColony.Core;
using AntColony.Units;
using AntColony.Buildings;
using AntColony.Data;
using UnityEngine;
public static class CommanderRemoval1112Checks
{
 static int pass; static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL "+label);pass++;}
 public static string Main(){
  pass=0;var random=UnityEngine.Random.state;var c=CommanderRoster.Instance.Commanders.First(x=>x!=null&&x.IsColonyMember);
  var form=JsonUtility.ToJson(c.FormationState);var traits=c.Traits.values.ToList();
  var pop=ColonyPopulation.Instance;var saved=pop.Capture();var research=CampaignResearch.Instance;var rs=research.CaptureState();
  var pool=AntPool.Instance;var fields=new[]{"LegacyFree","LegacyAssigned","LegacyReserved"}.Select(n=>typeof(AntPool).GetField(n,BindingFlags.Instance|BindingFlags.NonPublic)).ToArray();
  var counts=fields.Select(f=>(int)f.GetValue(pool)).ToArray();var restore=typeof(AntPool).GetMethod("RestoreCounts",BindingFlags.Instance|BindingFlags.NonPublic);
  try{
   c.FormationState.selected=Formation.Plain;float atk=c.AttackDamage,armor=c.Armor,range=c.EffectiveAttackRange,interval=c.EffectiveAttackInterval;
   c.Traits.values.Add(CommanderTrait.Legion);
   foreach(Formation f in Enum.GetValues(typeof(Formation))){c.FormationState.selected=f;Check(Mathf.Approximately(c.AttackDamage,atk)&&Mathf.Approximately(c.Armor,armor)&&Mathf.Approximately(c.EffectiveAttackRange,range)&&Mathf.Approximately(c.EffectiveAttackInterval,interval)&&c.FormationWorldMoveMultiplier==1,"retired formation stats "+f);}
   Check(!c.HasLegion&&!c.CanSelectFormation&&!c.TrySelectFormation(Formation.Arrow)&&c.CommandLimit==0,"no formation/legion/command limit");
   var t=new CommanderTalents();t.levels[8]=13;t.experience[8]=12;t.potential=150;var old=JsonUtility.ToJson(t);t.Add(CommanderActivity.Command,1000);t.Train(CommanderActivity.Command,1000,_=>1);Check(old==JsonUtility.ToJson(t),"old command data retained but cannot train");
   for(int i=0;i<30;i++){var generated=StartingCandidate.Roll(i);Check(generated.talents.Level(CommanderActivity.Command)==0&&!generated.traits.values.Contains(CommanderTrait.Legion)&&generated.traits.passions.All(p=>CommanderTalents.IsActive(p.activity)),"new candidate has 15 live skills "+i);}
   var legacy=new CommanderTraits();Check(!legacy.TryAdd(CommanderTrait.Legion)&&legacy.TryAdd(CommanderTrait.Legion,true)&&!legacy.Has(CommanderTrait.Legion),"legacy trait accepted only by save validation");
   var state=pop.Capture();state.young=5;state.old=10;state.taxFood=100;state.taxSoil=100;state.schoolFoodDebt=10;state.clinicMedicineDebt=10;pop.Restore(state);
   var frozen=JsonUtility.ToJson(pop.Capture());var rm=ResourceManager.Instance;float food=rm.GetAmount(ResourceType.Food),soil=rm.GetAmount(ResourceType.Soil);
   pop.Tick(1500);pop.Immigrate();pop.Monthly();pop.PayTax();pop.PayCivicUpkeep();pop.AgeYear();
   Check(frozen==JsonUtility.ToJson(pop.Capture()),"civilian state archived without economic ticks");
   Check(food==rm.GetAmount(ResourceType.Food)&&soil==rm.GetAmount(ResourceType.Soil),"no civilian income or upkeep");
   Check(pop.Total==0&&pop.HousingCapacity==0&&pop.LocatedCivilians==0&&pop.CivilianLocations.Count==0&&pop.AvailableMilitary(true)==0&&pop.MaxSoldiers(true)==0,"no civilian live counts targets capacity draft");
   restore.Invoke(pool,new object[]{70,20,10});pool.Breed(100);Check(pool.Free==0&&pool.Assigned==0&&pool.Reserved==0&&pool.Total==0&&!pool.TryAssign(1)&&!pool.TryReserve(1)&&pool.TryReserve(0),"no numeric workforce gate");
   Check((int)fields[0].GetValue(pool)==70&&(int)fields[1].GetValue(pool)==20,"legacy counts retained");
   for(int i=0;i<CampaignResearch.Technologies.Length;i++)Check((int)CampaignResearch.Technologies[i].Technology==i,"research save index "+i);
   foreach(var d in CampaignResearch.Technologies.Where(d=>CampaignResearch.Retired(d.Technology)))Check(!research.TryStart(d.Technology),"retired research blocked "+d.Technology);
   var legacyResearch=research.CaptureState();legacyResearch.active=(int)ScienceTechnology.FormationFocus;legacyResearch.progress=1;legacyResearch.completed.Remove(legacyResearch.active);legacyResearch.partialIds.Clear();legacyResearch.partialProgress.Clear();research.RestoreState(legacyResearch);
   Check(research.Active==null&&research.CaptureState().partialIds.Contains((int)ScienceTechnology.FormationFocus)&&CampaignResearch.Validate(research.CaptureState(),out _),"old active formation research archived with valid progress");
   foreach(var kind in new[]{BuildingKind.Hut,BuildingKind.House,BuildingKind.Apartment,BuildingKind.AdminDesk,BuildingKind.School,BuildingKind.CitizenClinic,BuildingKind.CivicPublicService})Check(!ScienceEffects.BuildingUnlocked(kind),"retired civic building "+kind);
   Check(ScienceEffects.BuildingUnlocked(BuildingKind.Dormitory),"commander sleeping facilities retained");
   return "PASS "+pass+" removal checks; settings and fixture state restored";
  }finally{JsonUtility.FromJsonOverwrite(form,c.FormationState);c.Traits.values=traits;pop.Restore(saved);research.RestoreState(rs);restore.Invoke(pool,counts.Cast<object>().ToArray());UnityEngine.Random.state=random;}
 }
}
