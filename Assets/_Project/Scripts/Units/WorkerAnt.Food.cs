using AntColony.Core;
using AntColony.Data;
using AntColony.World;

namespace AntColony.Units
{
    public partial class WorkerAnt
    {
        internal void AgeCargoFood(float seconds)
        {
            if (!IsCarrying || !FoodLots.Perishable(carriedType)) return;
            carriedLots.Match(UnityEngine.Mathf.CeilToInt(carriedAmount), carriedType);
            var spoiled = carriedLots.Age(seconds);
            carriedAmount = UnityEngine.Mathf.Max(0, carriedAmount - spoiled);
            if (spoiled > 0) { SpoilNotice.Add(carriedType, spoiled); DiplomacyManager.DropFloor(ResourceType.RottenFood, spoiled, Position); }
        }
    }
}
