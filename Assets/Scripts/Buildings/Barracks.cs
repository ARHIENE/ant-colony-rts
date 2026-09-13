using System.Collections;
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
            StopAllCoroutines();
            isUpgrading = false;
            Active.Remove(this);
            base.OnDisable();
        }

        public string GetUpgradeLabel() => isUpgrading ? $"Researching {role}..." : currentTier >= maxTier
            ? $"{role} Tier {currentTier} (Max)"
            : $"{role} T{currentTier}>T{currentTier + 1}\n{UpgradeFoodCost}F {UpgradeSoilCost}S";

        public bool TryUpgrade()
        {
            if (!isActiveAndEnabled || isUpgrading || currentTier >= maxTier || ResourceManager.Instance == null) return false;
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
