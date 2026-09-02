using AntColony.Core;
using AntColony.Data;

namespace AntColony.Buildings
{
    public class Storage : BuildingBase
    {
        protected override bool IsDepositPoint => true;

        private void Start()
        {
            if (data == null || ResourceManager.Instance == null) return;
            ResourceManager.Instance.AddCapacity(ResourceType.Food, data.foodCapacityBonus);
            ResourceManager.Instance.AddCapacity(ResourceType.Soil, data.soilCapacityBonus);
        }
    }
}
