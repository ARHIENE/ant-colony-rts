using AntColony.Core;
using System.Linq;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        public bool DeploymentEating => IsDeployed && WorkState.deploymentMealSeconds > 0;
        public int DeploymentPortion => RationPortion * (troopCount + 1);
        public string DeploymentFoodStatus => $"부대 식량 {WorkState.rations?.Total ?? 0}/{DeploymentPortion} · 포만 {Satiety:0}%"
            + (Satiety <= 30 ? " · 허기 경고" : "");
        private void AllocateDeploymentFood()
        {
            WorkState.rations ??= new FoodLots();
            var need = Mathf.Max(0, DeploymentPortion * 2 - WorkState.rations.Total);
            var carried = personalState.carriedRations?.Take(need);
            WorkState.rations.Add(carried); need -= carried?.Total ?? 0;
            var rm = ResourceManager.Instance;
            if (need > 0 && rm != null)
                foreach (var bin in rm.Bins.Where(b => b.Spendable && b.Amount(AntColony.Data.ResourceType.Food) > 0)
                    .OrderBy(b => b.Lots(AntColony.Data.ResourceType.Food).Stalest).ToArray())
                {
                    var taken = rm.TakeFrom(bin, AntColony.Data.ResourceType.Food, need, out var lots);
                    WorkState.rations.Add(lots); need -= taken;
                    if (need <= 0) break;
                }
        }
        public bool OrderDeploymentMeal()
        {
            if (!IsDeployed || IsReturning || !CanReceiveOrders || (WorkState.rations?.Total ?? 0) < DeploymentPortion) return false;
            CommandStop(); WorkState.deploymentMealSeconds = 3; return true;
        }
        private void TickDeploymentMeal(float seconds)
        {
            var spoiled = WorkState.rations.Age(seconds);
            if (spoiled > 0) AntColony.World.DiplomacyManager.DropFloor(AntColony.Data.ResourceType.RottenFood, spoiled, Position);
            AddFatigue(GameBalance.FatiguePerWorkSecond * seconds);
            AntColony.UI.ToastManager.SetPinned("dutyfood:" + personalState.id, Satiety <= 30 ? commanderName + ": 소집 허기 30% 이하 · 식사/교대 필요" : null);
            if (!DeploymentEating) return;
            if (OrderRefusalReason != "" || WorkState.rations.Total < DeploymentPortion) { WorkState.deploymentMealSeconds = 0; return; }
            WorkState.deploymentMealSeconds = Mathf.Max(0, WorkState.deploymentMealSeconds - seconds);
            if (WorkState.deploymentMealSeconds == 0)
            {
                WorkState.rations.Take(DeploymentPortion); MealState.satiety = 100;
                CampaignHistory.Resource(AntColony.Data.ResourceType.Food, DeploymentPortion, true, ResourceReason.Upkeep);
            }
        }
        public bool RecallTroops(int count)
        {
            if (!IsDeployed || IsAwayFromHome || !CanReceiveOrders || count <= 0 || count > troopCount) return false;
            CommandStop();
            if (ReturnTroops(count) == 0) return false;
            if (troopCount == 0) FinishReturn();
            return true;
        }
    }
}
