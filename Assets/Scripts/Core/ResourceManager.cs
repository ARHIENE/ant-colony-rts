using System;
using System.Collections.Generic;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Core
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [SerializeField] private int startingFood = 100;
        [SerializeField] private int startingSoil = 50;
        [SerializeField] private int baseFoodCapacity = 200;
        [SerializeField] private int baseSoilCapacity = 200;
        [SerializeField, Min(0)] private int baseSpecialCapacity = 100;

        private readonly Dictionary<ResourceType, int> amounts = new Dictionary<ResourceType, int>();
        private readonly Dictionary<ResourceType, int> capacities = new Dictionary<ResourceType, int>();

        public event Action OnResourcesChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            capacities[ResourceType.Food] = baseFoodCapacity;
            capacities[ResourceType.Soil] = baseSoilCapacity;
            capacities[ResourceType.Special] = baseSpecialCapacity;
            amounts[ResourceType.Food] = Mathf.Min(startingFood, baseFoodCapacity);
            amounts[ResourceType.Soil] = Mathf.Min(startingSoil, baseSoilCapacity);
        }

        // 저장 복원 전용. 상한을 먼저 세우고 보유량을 그 안으로 맞춘다.
        internal void RestoreState(int food, int soil, int special, int foodCapacity, int soilCapacity, int specialCapacity)
        {
            capacities[ResourceType.Food] = Mathf.Max(0, foodCapacity);
            capacities[ResourceType.Soil] = Mathf.Max(0, soilCapacity);
            capacities[ResourceType.Special] = Mathf.Max(0, specialCapacity);
            amounts[ResourceType.Food] = Mathf.Clamp(food, 0, capacities[ResourceType.Food]);
            amounts[ResourceType.Soil] = Mathf.Clamp(soil, 0, capacities[ResourceType.Soil]);
            amounts[ResourceType.Special] = Mathf.Clamp(special, 0, capacities[ResourceType.Special]);
            OnResourcesChanged?.Invoke();
        }

        public int GetAmount(ResourceType type) => amounts.TryGetValue(type, out var value) ? value : 0;
        public int GetCapacity(ResourceType type) => capacities.TryGetValue(type, out var value) ? value : 0;

        public void AddCapacity(ResourceType type, int amount)
        {
            capacities[type] = Mathf.Max(0, GetCapacity(type) + amount);
            OnResourcesChanged?.Invoke();
        }

        public void Add(ResourceType type, int amount, ResourceReason reason = ResourceReason.Gathering)
        {
            if (amount <= 0) return;
            var next = GetAmount(type) + Mathf.Min(amount, Mathf.Max(0, GetCapacity(type) - GetAmount(type)));
            CampaignHistory.Resource(type, next - GetAmount(type), false, reason);
            amounts[type] = next;
            OnResourcesChanged?.Invoke();
        }

        public bool CanAfford(int foodCost, int soilCost, int specialCost = 0)
        {
            return foodCost >= 0 && soilCost >= 0 && specialCost >= 0 && GetAmount(ResourceType.Food) >= foodCost
                && GetAmount(ResourceType.Soil) >= soilCost && GetAmount(ResourceType.Special) >= specialCost;
        }

        public bool TrySpend(int foodCost, int soilCost, int specialCost = 0, ResourceReason reason = ResourceReason.Other)
        {
            if (!CanAfford(foodCost, soilCost, specialCost)) return false;
            amounts[ResourceType.Food] = GetAmount(ResourceType.Food) - foodCost;
            amounts[ResourceType.Soil] = GetAmount(ResourceType.Soil) - soilCost;
            amounts[ResourceType.Special] = GetAmount(ResourceType.Special) - specialCost;
            CampaignHistory.Resource(ResourceType.Food, foodCost, true, reason);
            CampaignHistory.Resource(ResourceType.Soil, soilCost, true, reason);
            CampaignHistory.Resource(ResourceType.Special, specialCost, true, reason);
            OnResourcesChanged?.Invoke();
            return true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
