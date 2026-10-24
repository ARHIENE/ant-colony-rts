using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    public class Storage : BuildingBase
    {
        private ResourceManager registeredManager;
        private Vector3Int registeredCapacity;
        // 이 창고에 실제로 담긴 물건·허용 품목·우선순위(2026-10-12 산소미포함식). 건물이 있는 동안 유지하고 개보수로 꺼져도 내용물은 남는다.
        public StoreBin Bin { get; private set; }
        internal Vector3Int ResearchBonus => isActiveAndEnabled && data != null && CampaignResearch.Instance != null
            && CampaignResearch.Instance.Has(ScienceTechnology.Fermentation)
            ? new Vector3Int(data.foodCapacityBonus / 2, data.soilCapacityBonus / 2, data.specialCapacityBonus / 2) : Vector3Int.zero;
        protected override bool IsDepositPoint => true;
        internal int RegisteredCapacity(ResourceType t) => t.IsFood() ? registeredCapacity.x : t == ResourceType.Special ? registeredCapacity.z : registeredCapacity.y;

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
            if (Bin == null) Bin = registeredManager.Register(this);
            var capacity = new Vector3Int(data.foodCapacityBonus, data.soilCapacityBonus, data.specialCapacityBonus) + ResearchBonus;
            var delta = capacity - registeredCapacity;
            registeredCapacity = capacity;
            registeredManager.AddCapacity(ResourceType.Food, delta.x);
            registeredManager.AddCapacity(ResourceType.Soil, delta.y);
            registeredManager.AddCapacity(ResourceType.Special, delta.z);
        }

        public override void DepositResources(ResourceType type, int amount)
        {
            var rm = ResourceManager.Instance; if (rm == null) return;
            var left = amount - rm.AddTo(Bin, type, amount);
            if (left > 0) rm.Add(type, left, near: Position); // 이 창고에 못 들어간 몫은 다른 창고, 그래도 넘치면 바닥
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

        // 파괴되면 내용물이 바닥에 나온다. 씬 정리·불러오기로 사라질 때는 내놓지 않는다.
        private void OnDestroy() => ResourceManager.Instance?.Unregister(Bin, IsDead);
    }
}
