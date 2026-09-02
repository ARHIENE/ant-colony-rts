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
            amounts[ResourceType.Food] = Mathf.Min(startingFood, baseFoodCapacity);
            amounts[ResourceType.Soil] = Mathf.Min(startingSoil, baseSoilCapacity);
        }

        public int GetAmount(ResourceType type) => amounts.TryGetValue(type, out var value) ? value : 0;
        public int GetCapacity(ResourceType type) => capacities.TryGetValue(type, out var value) ? value : 0;

        public void AddCapacity(ResourceType type, int amount)
        {
            capacities[type] = GetCapacity(type) + amount;
            OnResourcesChanged?.Invoke();
        }

        public void Add(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            var next = Mathf.Min(GetAmount(type) + amount, GetCapacity(type));
            amounts[type] = next;
            OnResourcesChanged?.Invoke();
        }

        public bool CanAfford(int foodCost, int soilCost)
        {
            return GetAmount(ResourceType.Food) >= foodCost && GetAmount(ResourceType.Soil) >= soilCost;
        }

        public bool TrySpend(int foodCost, int soilCost)
        {
            if (!CanAfford(foodCost, soilCost)) return false;
            amounts[ResourceType.Food] = GetAmount(ResourceType.Food) - foodCost;
            amounts[ResourceType.Soil] = GetAmount(ResourceType.Soil) - soilCost;
            OnResourcesChanged?.Invoke();
            return true;
        }
    }
}
