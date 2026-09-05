using System.Collections;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    public class Barracks : BuildingBase
    {
        [SerializeField] private UnitRole role = UnitRole.Melee;
        [SerializeField] private GameObject soldierAntPrefab;
        [SerializeField] private Data.UnitData soldierAntData;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private ObjectPool pool;

        [Header("Independent Tier")]
        [SerializeField, Min(1)] private int currentTier = 1;
        [SerializeField, Min(1)] private int maxTier = 3;
        [SerializeField, Min(0)] private int baseUpgradeFoodCost = 50;
        [SerializeField, Min(0)] private int baseUpgradeSoilCost = 50;
        [SerializeField, Min(0f)] private float upgradeTimeSeconds = 5f;

        private bool isProducing;
        private bool isUpgrading;

        public UnitRole Role => role;
        public int CurrentTier => currentTier;
        public int MaxTier => maxTier;
        public bool IsProducing => isProducing;
        public bool IsUpgrading => isUpgrading;
        public int UpgradeFoodCost => baseUpgradeFoodCost * currentTier;
        public int UpgradeSoilCost => baseUpgradeSoilCost * currentTier;
        public bool HasCompatibleUnit =>
            role != UnitRole.Worker && soldierAntData != null && soldierAntData.role == role;
        public bool CanProduceAssignedUnit =>
            HasCompatibleUnit && soldierAntData.requiredBarracksTier <= currentTier;

        public string GetProductionLabel()
        {
            if (isProducing) return "Producing...";
            if (soldierAntData == null) return "No Unit Assigned";
            if (!HasCompatibleUnit) return "Unit Role Mismatch";
            if (soldierAntData.requiredBarracksTier > currentTier)
                return $"Requires Tier {soldierAntData.requiredBarracksTier}";
            return $"Produce {soldierAntData.displayName}";
        }

        public string GetUpgradeLabel()
        {
            if (isUpgrading) return $"Upgrading {role}...";
            if (currentTier >= maxTier) return $"{role} Tier {currentTier} (Max)";
            return $"{role} T{currentTier}>T{currentTier + 1}\n{UpgradeFoodCost}F {UpgradeSoilCost}S";
        }

        public bool TryProduceSoldier()
        {
            if (isProducing || isUpgrading) return false;
            if (ResourceManager.Instance == null || !CanProduceAssignedUnit || soldierAntPrefab == null) return false;
            if (!ResourceManager.Instance.TrySpend(soldierAntData.foodCost, 0)) return false;

            StartCoroutine(ProduceRoutine());
            return true;
        }

        private IEnumerator ProduceRoutine()
        {
            isProducing = true;
            yield return new WaitForSeconds(soldierAntData.buildTimeSeconds);

            var origin = spawnPoint != null ? spawnPoint.position : transform.position;
            var instance = pool != null
                ? pool.Get(soldierAntPrefab, origin, Quaternion.identity)
                : Instantiate(soldierAntPrefab, origin, Quaternion.identity);

            var soldier = instance.GetComponent<SoldierAnt>();
            soldier.Initialize(soldierAntData, pool, soldierAntPrefab);

            isProducing = false;
        }

        public bool TryUpgrade()
        {
            if (isProducing || isUpgrading || currentTier >= maxTier) return false;
            if (ResourceManager.Instance == null) return false;
            if (!ResourceManager.Instance.TrySpend(UpgradeFoodCost, UpgradeSoilCost)) return false;

            StartCoroutine(UpgradeRoutine());
            return true;
        }

        private IEnumerator UpgradeRoutine()
        {
            isUpgrading = true;
            yield return new WaitForSeconds(upgradeTimeSeconds);
            currentTier++;
            isUpgrading = false;
        }
    }
}
