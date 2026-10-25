using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public partial class ExpeditionTransport
    {
        public bool Walking { get; private set; }
        public float FoodDebt { get; internal set; }
        public float DockingRemaining { get; internal set; }
        public int ReturnCargoSpace => Mathf.Max(0, CargoCapacity - CargoLoad - ReservedCargo);
        private int WalkingCargoCapacity => Mathf.FloorToInt(crew.Where(c => c != null && !c.IsDead)
            .Sum(c => Mathf.Max(0, c.TotalLoadCapacity - c.StartingLoadWeight)) / .05f);
        public float FormationWalkMultiplier => crew.Where(c => c != null && !c.IsDead)
            .Select(c => c.FormationWorldMoveMultiplier).DefaultIfEmpty(1).Min();
        public float DailyFood => crew.Where(c => c != null && !c.IsDead).Sum(ExpeditionRules.FoodPerDay) + withdraw.troops * 2 * GameBalance.MealFood;
        public int RecommendedFood(ExpeditionSite destination) => ExpeditionRules.RecommendedFood(
            Walking ? ExpeditionRules.WalkSeconds(destination) / FormationWalkMultiplier : TravelSeconds, DailyFood);
        public int AvailableFood => GetCargo(AntColony.Data.ResourceType.Food) + crew.Where(c => c != null && !c.IsDead)
            .Sum(c => c.PersonalState.carriedRations?.Total ?? 0);
        public bool LeavesHomeUnattended => State == ExpeditionState.Home && CommanderLoad > 0 && ExpeditionRules.HomeCommanders == 0;

        public bool TryLoadFood(int amount)
        {
            if (State != ExpeditionState.Home || Route?.IsRunning == true || amount <= 0 || amount > ReturnCargoSpace
                || ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(amount, 0, reason: ResourceReason.Expedition)) return false;
            cargo[AntColony.Data.ResourceType.Food] = GetCargo(AntColony.Data.ResourceType.Food) + amount;
            return true;
        }

        private void TickSupplies(float seconds)
        {
            FoodDebt += DailyFood * seconds / GameCalendar.SecondsPerDay;
            var needed = Mathf.FloorToInt(FoodDebt);
            if (needed == 0) return;
            var take = Mathf.Min(needed, GetCargo(AntColony.Data.ResourceType.Food));
            cargo[AntColony.Data.ResourceType.Food] = GetCargo(AntColony.Data.ResourceType.Food) - take;
            needed -= take; FoodDebt -= take;
            foreach (var c in crew)
            {
                if (c == null || c.IsDead || needed == 0) continue;
                var rations = c.PersonalState.carriedRations;
                take = Mathf.Min(needed, rations?.Total ?? 0);
                if (take > 0) rations.Take(take);
                needed -= take; FoodDebt -= take;
            }
            if (needed > 0) AntColony.UI.ToastManager.SetPinned("supplies:" + GetEntityId(), "원정 보급 부족: " + name);
            else
            {
                AntColony.UI.ToastManager.SetPinned("supplies:" + GetEntityId(), null);
                foreach (var c in crew)
                    if (c != null && !c.IsDead && c.PersonalState.meal.satiety <= GameBalance.EatBelowSatiety)
                        c.PersonalState.meal.satiety = 100;
            }
        }
    }
}
