using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed partial class Kitchen
    {
        private ResourceType? PlannedOutput => Current?.output ?? ((inTransit.Count > 0 || meals.staged > 0) && meals.orders.Count > 0
            ? Recipes[meals.orders[0].recipe].output : (ResourceType?)null);
        public int DeliverFood(object courier, ResourceType type, int amount, FoodLots lots)
        {
            AbortPickup(courier);
            if (type != ResourceType.Food || amount <= 0 || !Usable || NextOrder == null || Current != null) return 0;
            var take = Mathf.Min(amount, Mathf.Max(0, Recipes[NextOrder.recipe].food - meals.staged));
            meals.stagedLots ??= new FoodLots(); meals.stagedLots.Match(meals.staged, type);
            var delivered = lots == null ? new FoodLots() : JsonUtility.FromJson<FoodLots>(JsonUtility.ToJson(lots));
            if (delivered.Total < take) delivered.Match(take, type);
            meals.stagedLots.Add(delivered.Take(take)); meals.staged += take; return take;
        }
        public void AgeIngredients(float seconds)
        {
            if (meals.staged <= 0) return;
            meals.stagedLots ??= new FoodLots(); meals.stagedLots.Match(meals.staged, ResourceType.Food);
            var spoiled = meals.stagedLots.Age(seconds); meals.staged -= spoiled;
            if (spoiled > 0) { SpoilNotice.Add(ResourceType.Food, spoiled); DiplomacyManager.DropFloor(ResourceType.RottenFood, spoiled, Position); }
        }
    }
}
