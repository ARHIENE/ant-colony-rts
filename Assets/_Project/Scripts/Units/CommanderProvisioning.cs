using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        // 작업표와 대상 우선순위가 같은 경우에만 적용한다. 지정하지 않은 채집·생산은 만들지 않는다.
        private (bool raw, bool cooked, bool herbs) ProvisioningNeeds()
        {
            var rm = ResourceManager.Instance;
            if (rm == null) return (false, false, false);
            var people = HomePatients().ToArray();
            var rawTarget = people.Where(c => c.DietAllows(DietRaw)).Sum(c => c.RationPortion * 2);
            var cookedTarget = people.Count(c => c.DietAllows(DietSimple) || c.DietAllows(DietFine));
            var cooked = CookedFoods.MealTypes.Where(t => CookedFoods.Quality(t) > 0).Sum(rm.GetAmount);
            // 약제대는 입고된 약초로 재료 운반을 시작한다. 바닥 재고가 있어도 먼저 입고해야 한다.
            var herbs = MedicineShortage() && rm.GetAmount(ResourceType.Herb) < 2
                && SceneObjects.All<Processor>().Any(p => p.IsMedical && p.isActiveAndEnabled && !p.IsDead && !p.Paused
                    && p.IngredientNeeds.Any(i => i.type == ResourceType.Herb));
            // 창고가 없거나 반입 금지/경로 차단이면 먼저 창고를 지을 수 있어야 한다.
            return (rm.GetAmount(ResourceType.Food) < rawTarget && HasCargoDestination(ResourceType.Food),
                cooked < cookedTarget && CookedFoods.MealTypes.Any(t => CookedFoods.Quality(t) > 0 && HasCargoDestination(t)), herbs);
        }

        private static bool MedicineShortage() => HomePatients().Any(p => p.Health.medicine != MedicineLevel.NoCare
            && p.Health.medicine != MedicineLevel.NoMedicine && p.Health.illnesses.Any(i => i.NeedsTend)
            && MedicalRules.BestMedicine(p.Health.medicine, FreeMedicine) < 0);

        private static int WorkNeedPriority(Component target, CommanderJobs job, bool rawNeeded, bool cookedNeeded, bool herbsNeeded)
        {
            if (job == CommanderJobs.Nursing && target is CommanderAnt patient)
                return patient.PersonalHealth <= 0 || patient.Health.LifeThreatening ? 5 : 4;
            if (job == CommanderJobs.Nursing && target is Processor processor && processor.IsMedical
                && MedicineShortage()) return 4;
            if (target is ResourceNode node)
            {
                if (node.ResourceType == ResourceType.Herb && herbsNeeded) return 4;
                if (node.IsLooseCargo && CookedFoods.Quality(node.ResourceType) > 0 && cookedNeeded) return 4;
                if (node.ResourceType == ResourceType.Food && rawNeeded) return node.IsLooseCargo ? 3 : 2;
                if (SceneObjects.All<BuildingConstructionSite>().Any(s => s.isActiveAndEnabled && !s.Paused
                    && s.Needs.Any(n => n.type == node.ResourceType && n.need > n.delivered + n.reserved + s.InTransit(n.type)))) return 1;
            }
            if (job == CommanderJobs.Cooking && target is Kitchen && cookedNeeded) return 3;
            if (job == CommanderJobs.Repair && target is BuildingBase building && CanFundRepair(building))
                return building.CurrentHealth < building.MaxHealth * .5f ? 3 : 1;
            // 확보한 재료의 운반과 시공을 끝내 창고·화덕·방어시설을 사용할 수 있게 한다.
            if (job == CommanderJobs.Building && target is BuildingConstructionSite site && (site.CanWork || site.PendingPickup != null)) return 1;
            return 0;
        }

        private static bool CanFundRepair(BuildingBase building)
        {
            if (building.TryGetComponent<BuildingRepair>(out var repair) && repair.Credit > 0) return true;
            var rm = ResourceManager.Instance;
            if (rm == null || building.Data == null || building.MaxHealth <= 0) return false;
            var fraction = (building.MaxHealth - building.CurrentHealth) / building.MaxHealth * GameBalance.RepairCostShare;
            return rm.CanAfford(Mathf.CeilToInt(building.Data.foodCost * fraction), Mathf.CeilToInt(building.Data.soilCost * fraction),
                building.MainMaterial, Mathf.CeilToInt(building.Data.specialCost * fraction));
        }

        private static bool HasFoodFor(CommanderAnt patient)
        {
            if (patient.MealState.served || patient.MealState.eatSeconds > 0) return true;
            var rm = ResourceManager.Instance;
            if (rm == null) return false;
            var bins = rm.Bins.Where(b => b.Spendable && patient.TryWorkApproach(b.Position, out _)).ToArray();
            foreach (var type in CookedFoods.MealTypes)
            {
                if (patient.MealState.pickedFood > 0 && patient.MealState.pickedType != (int)type) continue;
                var quality = CookedFoods.Quality(type);
                if (type != ResourceType.RottenFood && !patient.DietAllows(quality == 2 ? DietFine : quality == 1 ? DietSimple : DietRaw)) continue;
                var need = quality > 0 ? 1 : patient.RationPortion;
                if (FoodLots.MealSources(bins, type).Sum(b => b.Amount(type)) >= need - patient.MealState.pickedFood) return true;
            }
            return SceneObjects.All<Kitchen>().Any(k => k.BestMeal(patient.Diet) != null && patient.TryWorkApproach(k.Position, out _));
        }
    }
}

