using System.Collections;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Buildings
{
    public class QueenChamber : BuildingBase
    {
        [SerializeField] private Data.UnitData workerAntData;
        [SerializeField, Min(0)] private int fishingFoodCost = 30;
        [SerializeField, Min(0)] private int fishingSoilCost = 20;
        [SerializeField, Min(0f)] private float fishingResearchSeconds = 3f;
        private bool isProducing;
        private bool isFishingResearching;

        protected override bool IsDepositPoint => true;
        public string GetProductionLabel() => isProducing ? "Producing Ant..." :
            workerAntData != null ? $"Produce Ant\n{workerAntData.foodCost}F" : "No Ant Data";

        public bool TryProduceWorker()
        {
            if (isProducing || !isActiveAndEnabled || ResourceManager.Instance == null
                || AntPool.Instance == null || workerAntData == null) return false;
            if (!ResourceManager.Instance.TrySpend(workerAntData.foodCost, 0)) return false;
            StartCoroutine(ProduceRoutine());
            return true;
        }

        private IEnumerator ProduceRoutine()
        {
            isProducing = true;
            yield return new WaitForSeconds(workerAntData.buildTimeSeconds);
            AntPool.Instance?.Breed(1);
            isProducing = false;
        }

        public string GetFishingResearchLabel() => GameManager.Instance != null && GameManager.Instance.FishingUnlocked
            ? "Fishing Unlocked" : isFishingResearching ? "Learning Fishing..."
            : $"Unlock Fishing\n{fishingFoodCost}F {fishingSoilCost}S";

        public bool TryResearchFishing()
        {
            if (!isActiveAndEnabled || isFishingResearching || GameManager.Instance == null
                || GameManager.Instance.FishingUnlocked || ResourceManager.Instance == null) return false;
            foreach (var queen in FindObjectsByType<QueenChamber>(FindObjectsSortMode.None))
                if (queen.isFishingResearching) return false;
            if (!ResourceManager.Instance.TrySpend(fishingFoodCost, fishingSoilCost)) return false;
            StartCoroutine(FishingRoutine());
            return true;
        }

        private IEnumerator FishingRoutine()
        {
            isFishingResearching = true;
            yield return new WaitForSeconds(fishingResearchSeconds);
            if (GameManager.Instance != null) GameManager.Instance.FishingUnlocked = true;
            isFishingResearching = false;
        }

        protected override void OnDisable()
        {
            StopAllCoroutines();
            isProducing = isFishingResearching = false;
            base.OnDisable();
        }
    }
}
