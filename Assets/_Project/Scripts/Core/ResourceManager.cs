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
        // 흙 외 재료 보유량(저장 v16). 순서는 ResourceLabels.Materials에서 흙을 뺀 것.
        internal int[] CaptureMaterials() => System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(OtherMaterials, GetAmount));
        internal void RestoreMaterials(int[] values)
        {
            for (int i = 0; i < OtherMaterials.Length; i++) amounts[OtherMaterials[i]] = values != null && i < values.Length ? Mathf.Clamp(values[i], 0, GetCapacity(OtherMaterials[i])) : 0;
            OnResourcesChanged?.Invoke();
        }
        internal static readonly ResourceType[] OtherMaterials = System.Array.FindAll(ResourceLabels.Materials, t => t != ResourceType.Soil);

        public int GetAmount(ResourceType type) => amounts.TryGetValue(type, out var value) ? value : 0;
        // 재료는 종류마다 같은 한도(창고 '재료' 한도)를 쓴다.
        public int GetCapacity(ResourceType type) => capacities.TryGetValue(type.IsMaterial() ? ResourceType.Soil : type, out var value) ? value : 0;

        public void AddCapacity(ResourceType type, int amount)
        {
            if (type.IsMaterial()) type = ResourceType.Soil;
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

        // 주재료를 고른 비용: 식량 + 주재료 수량 + 특수. 주재료가 흙이면 기존 TrySpend와 같다.
        public bool CanAfford(int foodCost, int materialCost, ResourceType material, int specialCost)
            => material == ResourceType.Soil ? CanAfford(foodCost, materialCost, specialCost)
            : material.IsMaterial() && CanAfford(foodCost, 0, specialCost) && materialCost >= 0 && GetAmount(material) >= materialCost;
        public bool TrySpend(int foodCost, int materialCost, ResourceType material, int specialCost, ResourceReason reason = ResourceReason.Other)
        {
            if (material == ResourceType.Soil) return TrySpend(foodCost, materialCost, specialCost, reason);
            if (!CanAfford(foodCost, materialCost, material, specialCost) || !TrySpend(foodCost, 0, specialCost, reason)) return false;
            amounts[material] = GetAmount(material) - materialCost;
            CampaignHistory.Resource(material, materialCost, true, reason);
            OnResourcesChanged?.Invoke();
            return true;
        }
        public bool TrySpend(ResourceType type, int amount, ResourceReason reason = ResourceReason.Other)
        {
            if (amount < 0 || GetAmount(type) < amount) return false;
            if (amount == 0) return true;
            amounts[type] = GetAmount(type) - amount;
            CampaignHistory.Resource(type, amount, true, reason);
            OnResourcesChanged?.Invoke();
            return true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
