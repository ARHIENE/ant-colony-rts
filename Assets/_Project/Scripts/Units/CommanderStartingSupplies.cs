using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        public int RationPortion => Mathf.CeilToInt(GameBalance.MealFood * Traits.FoodMultiplier);
        public int CarriedRationMeals => (PersonalState.carriedRations?.Total ?? 0) / RationPortion;
        public float StartingLoadWeight => (PersonalState.carriedRations?.Total ?? 0) * .05f + (Weapon != null ? 1 : 0)
            + (PersonalState.toolTier > 0 ? 1.5f : 0) + (string.IsNullOrEmpty(PersonalState.ordinaryClothes) ? 0 : .5f); // 무게 잠정. 가장 약한 시작 장수도 7일분을 소지한다.
        public float TotalLoadCapacity => (GameBalance.CarryBase + Talents.Level(CommanderActivity.Strength) * GameBalance.CarryPerStrength) * Traits.CarryMultiplier
            * (Traits.Has(CommanderTrait.DiamondStrength) ? 5 : 1); // 다이아 역발산: 근력·기본 보정 뒤 운반 한도 ×5(무게 감속 규칙은 현재 없음)

        private void TickCarriedSupplies(float seconds)
        {
            if (PersonalState.carriedRations == null) PersonalState.carriedRations = new FoodLots();
            var spoiled = PersonalState.carriedRations.Age(seconds);
            if (spoiled > 0) DiplomacyManager.DropFloor(ResourceType.RottenFood, spoiled, Position);
        }

        // 기존 식사 타이머·포만·기분 처리를 재사용한다. 소지품에서 꺼낼 때만 1회 차감한다.
        private bool TickCarriedMeal(float seconds)
        {
            var m = PersonalState.meal;
            if (m.served || m.pickedFood > 0) return false; // 집은 식사·부분 식량이 있는 동안 소지 식량을 또 꺼내지 않는다
            if (m.satiety > GameBalance.EatBelowSatiety || m.eatSeconds > 0 || m.poisonSeconds > 0 || IsDeployed || IsAwayFromHome
                || IsEmbarked || IsCaptive || WorkPriorities.Red || !DietAllows(DietRaw) || !CanReceiveOrders || CarriedRationMeals < 1) return false;
            SuspendForNeeds("meal");
            PersonalState.carriedRations.Take(RationPortion);
            m.quality = 1; m.cookSkill = CommanderTalents.MaxLevel; m.rotten = false; m.retrySeconds = 0;
            m.eatSeconds = GameBalance.EatSeconds * (Traits.Has(CommanderTrait.SlowEater) ? 2 : 1);
            return false;
        }

        public bool DropCarriedRations(int meals)
        {
            if (meals <= 0 || meals > CarriedRationMeals || !CanReceiveOrders || IsAwayFromHome || IsEmbarked) return false;
            var lots = PersonalState.carriedRations.Take(meals * RationPortion);
            DiplomacyManager.DropFloor(ResourceType.Food, lots.Total, Position + Vector3.right, lots);
            return true;
        }
    }
}
