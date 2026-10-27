using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;

namespace AntColony.Buildings
{
    public sealed partial class Workshop
    {
        private readonly ProductionInputs inputs = new ProductionInputs();
        public bool Paused { get => !enabled; set => enabled = !value && !Ruined; }
        private IEnumerable<(ResourceType type, int amount)> RequiredInputs
        {
            get
            {
                if (jobs.Count == 0 || jobs[0].materialsPaid) yield break;
                var cost = EquipmentRecipes.Cost(jobs[0].recipe);
                if (cost.x > 0) yield return (ResourceType.Food, cost.x);
                if (cost.y > 0) yield return (ResourceType.Soil, cost.y);
                if (cost.z > 0) yield return (ResourceType.Special, cost.z);
            }
        }
        public bool InputsReady => jobs.Count > 0 && (jobs[0].materialsPaid || inputs.Ready(RequiredInputs));
        public bool NeedsIngredients => NextPickup(out _, out _);
        public bool NextPickup(out ResourceType type, out int amount)
        {
            type = default; amount = 0;
            return !Paused && !Ruined && !IsDead && isActiveAndEnabled && inputs.Next(RequiredInputs, out type, out amount);
        }
        public bool BeginPickup(object courier, ResourceType type, int amount) => !Paused && !Ruined && !IsDead && inputs.Reserve(courier, type, amount, RequiredInputs);
        public int Deliver(object courier, ResourceType type, int amount) => IsDead ? 0 : inputs.Deliver(courier, type, amount, RequiredInputs);
        public int DeliverFood(object courier, ResourceType type, int amount, FoodLots lots) => IsDead ? 0 : inputs.Deliver(courier, type, amount, RequiredInputs, lots);
        public void AgeIngredients(float seconds) => inputs.Age(seconds, Position);
        public void AbortPickup(object courier) => inputs.Abort(courier);
    }
}
