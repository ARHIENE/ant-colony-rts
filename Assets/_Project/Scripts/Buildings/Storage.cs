using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    public class Storage : BuildingBase
    {
        private ResourceManager registeredManager;
        private Vector3Int registeredCapacity;
        internal Vector3Int ResearchBonus => isActiveAndEnabled && data != null && CampaignResearch.Instance != null
            && CampaignResearch.Instance.Has(ScienceTechnology.Fermentation)
            ? new Vector3Int(data.foodCapacityBonus / 2, data.soilCapacityBonus / 2, data.specialCapacityBonus / 2) : Vector3Int.zero;
        protected override bool IsDepositPoint => true;

        private void Start()
        {
            RefreshCapacity();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshCapacity();
        }

        internal void RefreshCapacity()
        {
            if (!isActiveAndEnabled || data == null || ResourceManager.Instance == null) return;
            registeredManager = ResourceManager.Instance;
            var capacity = new Vector3Int(data.foodCapacityBonus, data.soilCapacityBonus, data.specialCapacityBonus) + ResearchBonus;
            var delta = capacity - registeredCapacity;
            registeredCapacity = capacity;
            registeredManager.AddCapacity(ResourceType.Food, delta.x);
            registeredManager.AddCapacity(ResourceType.Soil, delta.y);
            registeredManager.AddCapacity(ResourceType.Special, delta.z);
        }

        protected override void OnDisable()
        {
            if (registeredManager != null)
            {
                registeredManager.AddCapacity(ResourceType.Food, -registeredCapacity.x);
                registeredManager.AddCapacity(ResourceType.Soil, -registeredCapacity.y);
                registeredManager.AddCapacity(ResourceType.Special, -registeredCapacity.z);
            }
            registeredCapacity = Vector3Int.zero;
            registeredManager = null;
            base.OnDisable();
        }
    }
}
