using AntColony.Core;
using UnityEngine;

namespace AntColony.World
{
    public sealed partial class ColonyEvents
    {
        public bool CaravanPresent => state.caravanRemaining > 0;
        // 고급 필수 재료의 반복 공급처. 교환가는 별도 확정 전 잠정값이며 협상·외교 해금과 무관하다.
        public static int SupplyPrice(AntColony.Data.ResourceType type) => type switch
        {
            AntColony.Data.ResourceType.Wood or AntColony.Data.ResourceType.Stone or AntColony.Data.ResourceType.PlantingMaterial => 1,
            AntColony.Data.ResourceType.IronOre or AntColony.Data.ResourceType.CopperOre or AntColony.Data.ResourceType.Coal or AntColony.Data.ResourceType.Sulfur => 2,
            AntColony.Data.ResourceType.TinOre or AntColony.Data.ResourceType.Iron or AntColony.Data.ResourceType.Copper => 3,
            AntColony.Data.ResourceType.Steel or AntColony.Data.ResourceType.Bronze or AntColony.Data.ResourceType.Special => 6,
            AntColony.Data.ResourceType.Component => 8,
            _ => 0
        };
        public bool TryBuySupply(AntColony.Data.ResourceType type, int amount)
        {
            var price = SupplyPrice(type);
            if (!CaravanPresent || price == 0 || amount < 1 || amount > 100 || ResourceManager.Instance == null
                || !ResourceManager.Instance.TrySpend(AntColony.Data.ResourceType.Food, amount * price, ResourceReason.Trade)) return false;
            DiplomacyManager.DropFloor(type, amount, ResourceManager.HomePosition + Vector3.right * 3);
            return true;
        }
    }
}
