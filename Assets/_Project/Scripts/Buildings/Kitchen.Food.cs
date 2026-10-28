using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;

namespace AntColony.Buildings
{
    public sealed partial class Kitchen
    {
        private ResourceType? PlannedOutput => Current?.output ?? ((Staged > 0 || meals.pending >= 0) && meals.orders.Count > 0
            ? Recipes[meals.pending >= 0 ? meals.pending : meals.orders[0].recipe].output : (ResourceType?)null);
        private void RestoreIngredients()
        {
            var stock = meals.ingredients ?? new List<ProductionInputs.Stock>();
            if (meals.ingredients == null && meals.staged > 0)
                stock.Add(new ProductionInputs.Stock { type = ResourceType.Food, amount = meals.staged, lots = meals.stagedLots });
            inputs.Restore(stock); CaptureIngredients();
        }
        private void CaptureIngredients()
        {
            meals.ingredients = inputs.staged;
            var food = inputs.staged.FirstOrDefault(s => s.type == ResourceType.Food);
            meals.staged = food?.amount ?? 0; meals.stagedLots = food?.lots;
        }
        public int DeliverFood(object courier, ResourceType type, int amount, FoodLots lots)
        {
            if (!Usable || Current != null || NextOrder == null || amount <= 0) { inputs.Abort(courier); return 0; }
            var delivered = inputs.Deliver(courier, type, amount, Required, lots);
            if (delivered > 0) meals.pending = NextOrder.recipe;
            CaptureIngredients(); return delivered;
        }
        public void AgeIngredients(float seconds)
        {
            inputs.Age(seconds, Position); CaptureIngredients();
        }
        private static bool ValidIngredients(State s) => s.pending >= -1 && s.pending < Recipes.Length
            && (s.pending < 0 || s.current < 0 && s.orders != null && s.orders.Any(o => o != null && o.recipe == s.pending))
            && (s.ingredients == null || s.ingredients.Count <= StoreBin.TypeCount
                && s.ingredients.All(i => i != null && Enum.IsDefined(typeof(ResourceType), i.type) && i.amount > 0 && i.amount <= 100000000
                    && (i.lots == null || i.lots.Valid && i.lots.Total <= i.amount))
                && s.ingredients.Select(i => i.type).Distinct().Count() == s.ingredients.Count);
    }
}
