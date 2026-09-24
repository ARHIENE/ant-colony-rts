using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    public class Barracks : BuildingBase
    {
        private static readonly List<Barracks> Active = new List<Barracks>();
        [SerializeField] private UnitRole role = UnitRole.Melee;
        [SerializeField, Min(1)] private int currentTier = 1;
        [SerializeField, Min(1)] private int maxTier = 3;
        [SerializeField, Min(0)] private int baseUpgradeFoodCost = 50;
        [SerializeField, Min(0)] private int baseUpgradeSoilCost = 50;
        [SerializeField, Min(0f)] private float upgradeTimeSeconds = 5f;
        private bool isUpgrading;
        // 코루틴 대신 남은 시간을 들고 있어야 저장/복원이 가능하다.
        private float upgradeRemaining;

        internal float UpgradeRemaining => isUpgrading ? upgradeRemaining : 0f;

        internal void RestoreState(int tier, float remaining)
        {
            currentTier = Mathf.Clamp(tier, 1, maxTier);
            isUpgrading = remaining > 0f && currentTier < maxTier;
            upgradeRemaining = isUpgrading ? remaining : 0f;
        }

        public UnitRole Role => role;
        public int CurrentTier => currentTier;
        public int MaxTier => maxTier;
        public bool IsUpgrading => isUpgrading;
        public int UpgradeFoodCost => baseUpgradeFoodCost * currentTier;
        public int UpgradeSoilCost => baseUpgradeSoilCost * currentTier;

        protected override void OnEnable()
        {
            base.OnEnable();
            Active.Add(this);
        }

        protected override void OnDisable()
        {
            isUpgrading = false;
            upgradeRemaining = 0f;
            Active.Remove(this);
            base.OnDisable();
        }

        private void Update() => Tick(Time.deltaTime);

        // 검사 스크립트가 시간을 직접 밀어 넣을 수 있도록 분리해 둔다.
        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || !isUpgrading || !(seconds > 0f) || float.IsInfinity(seconds)) return;
            upgradeRemaining -= seconds;
            if (upgradeRemaining > 0f) return;
            upgradeRemaining = 0f;
            isUpgrading = false;
            currentTier++;
            AntColony.UI.ToastManager.Show(role + " barracks upgraded to tier " + currentTier + ".");
        }

        public string GetUpgradeLabel() => isUpgrading ? $"Researching {role}..." : currentTier >= maxTier
            ? $"{role} Tier {currentTier} (Max)"
            : $"{role} T{currentTier}>T{currentTier + 1}\n{UpgradeFoodCost}F {UpgradeSoilCost}S";

        public bool TryUpgrade()
        {
            if (!isActiveAndEnabled || isUpgrading || currentTier >= maxTier || ResourceManager.Instance == null) return false;
            if (!ResourceManager.Instance.TrySpend(UpgradeFoodCost, UpgradeSoilCost)) return false;
            isUpgrading = true;
            upgradeRemaining = upgradeTimeSeconds;
            return true;
        }

        // ponytail: 티어당 공격/방어 +1은 임시 밸런스. 중복 병영은 최고 티어만 적용한다.
        public static float GetRoleBonus(UnitRole targetRole)
        {
            var bonus = 0;
            foreach (var barracks in Active)
                if (barracks != null && barracks.role == targetRole)
                    bonus = Mathf.Max(bonus, barracks.currentTier - 1);
            return bonus;
        }
    }
}
