using AntColony.Core;
using AntColony.Data;

namespace AntColony.Buildings
{
    public class Storage : BuildingBase
    {
        private ResourceManager registeredManager;
        protected override bool IsDepositPoint => true;

        private void Start()
        {
            RegisterCapacity();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RegisterCapacity();
        }

        private void RegisterCapacity()
        {
            if (registeredManager != null || data == null || ResourceManager.Instance == null) return;
            registeredManager = ResourceManager.Instance;
            registeredManager.AddCapacity(ResourceType.Food, data.foodCapacityBonus);
            registeredManager.AddCapacity(ResourceType.Soil, data.soilCapacityBonus);
            registeredManager.AddCapacity(ResourceType.Special, data.specialCapacityBonus);
        }

        protected override void OnDisable()
        {
            if (registeredManager != null && data != null)
            {
                registeredManager.AddCapacity(ResourceType.Food, -data.foodCapacityBonus);
                registeredManager.AddCapacity(ResourceType.Soil, -data.soilCapacityBonus);
                registeredManager.AddCapacity(ResourceType.Special, -data.specialCapacityBonus);
            }
            registeredManager = null;
            base.OnDisable();
        }
    }
}
