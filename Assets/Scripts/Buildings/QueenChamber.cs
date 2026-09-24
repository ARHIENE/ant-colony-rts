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
        // 코루틴 대신 남은 시간을 들고 있어야 저장/복원이 가능하다. 진행 조건은 이전과 같다.
        private float productionRemaining;
        private float fishingRemaining;

        internal float ProductionRemaining => isProducing ? productionRemaining : 0f;
        internal float FishingRemaining => isFishingResearching ? fishingRemaining : 0f;

        internal void RestoreState(float production, float fishing)
        {
            isProducing = production > 0f;
            productionRemaining = Mathf.Max(0f, production);
            isFishingResearching = fishing > 0f;
            fishingRemaining = Mathf.Max(0f, fishing);
        }

        protected override bool IsDepositPoint => true;
        public string GetProductionLabel() => isProducing ? "Producing Ant..." :
            workerAntData != null ? $"Produce Ant\n{workerAntData.foodCost}F" : "No Ant Data";

        public bool TryProduceWorker()
        {
            if (isProducing || !isActiveAndEnabled || ResourceManager.Instance == null
                || AntPool.Instance == null || workerAntData == null) return false;
            if (!ResourceManager.Instance.TrySpend(workerAntData.foodCost, 0)) return false;
            isProducing = true;
            productionRemaining = workerAntData.buildTimeSeconds;
            return true;
        }

        private void Update() => Tick(Time.deltaTime);

        // 검사 스크립트가 시간을 직접 밀어 넣을 수 있도록 분리해 둔다.
        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || !(seconds > 0f) || float.IsInfinity(seconds)) return;
            if (isProducing)
            {
                productionRemaining -= seconds;
                if (productionRemaining <= 0f)
                {
                    isProducing = false;
                    productionRemaining = 0f;
                    AntPool.Instance?.Breed(1);
                    AntColony.UI.ToastManager.Show("Ant production complete.");
                }
            }
            if (!isFishingResearching) return;
            fishingRemaining -= seconds;
            if (fishingRemaining > 0f) return;
            isFishingResearching = false;
            fishingRemaining = 0f;
            if (GameManager.Instance != null) GameManager.Instance.FishingUnlocked = true;
            AntColony.UI.ToastManager.Show("Fishing research complete.");
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
            isFishingResearching = true;
            fishingRemaining = fishingResearchSeconds;
            return true;
        }

        protected override void OnDisable()
        {
            isProducing = isFishingResearching = false;
            productionRemaining = fishingRemaining = 0f;
            base.OnDisable();
        }
    }
}
