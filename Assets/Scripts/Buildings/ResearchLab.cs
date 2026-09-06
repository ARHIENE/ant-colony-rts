using System.Collections;
using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    public class ResearchLab : BuildingBase
    {
        private static readonly List<ResearchLab> Active = new List<ResearchLab>();

        [SerializeField] private UnitRole role = UnitRole.Melee;
        [SerializeField, Min(0)] private int attackLevel;
        [SerializeField, Min(0)] private int armorLevel;
        [SerializeField, Min(1)] private int maxLevel = 3;
        [SerializeField, Min(0)] private int baseFoodCost = 30;
        [SerializeField, Min(0)] private int baseSoilCost = 20;
        [SerializeField, Min(0f)] private float researchTimeSeconds = 3f;
        [SerializeField, Min(0f)] private float attackBonusPerLevel = 2f;
        [SerializeField, Min(0f)] private float armorBonusPerLevel = 1f;

        private bool isResearching;

        public UnitRole Role => role;
        public int AttackLevel => attackLevel;
        public int ArmorLevel => armorLevel;
        public int MaxLevel => maxLevel;
        public bool IsResearching => isResearching;

        protected override void OnEnable()
        {
            base.OnEnable();
            Active.Add(this);
        }

        protected override void OnDisable()
        {
            Active.Remove(this);
            base.OnDisable();
        }

        public int GetFoodCost(int currentLevel) => baseFoodCost * (currentLevel + 1);
        public int GetSoilCost(int currentLevel) => baseSoilCost * (currentLevel + 1);

        public string GetAttackResearchLabel()
        {
            if (isResearching) return "Researching...";
            if (attackLevel >= maxLevel) return $"{role} ATK Lv{attackLevel} (Max)";
            return $"{role} ATK Lv{attackLevel + 1}\n{GetFoodCost(attackLevel)}F {GetSoilCost(attackLevel)}S";
        }

        public string GetArmorResearchLabel()
        {
            if (isResearching) return "Researching...";
            if (armorLevel >= maxLevel) return $"{role} Armor Lv{armorLevel} (Max)";
            return $"{role} Armor Lv{armorLevel + 1}\n{GetFoodCost(armorLevel)}F {GetSoilCost(armorLevel)}S";
        }

        public bool TryResearchAttack()
        {
            return TryStartResearch(true, attackLevel);
        }

        public bool TryResearchArmor()
        {
            return TryStartResearch(false, armorLevel);
        }

        private bool TryStartResearch(bool attack, int currentLevel)
        {
            if (isResearching || currentLevel >= maxLevel || ResourceManager.Instance == null) return false;
            if (!ResourceManager.Instance.TrySpend(GetFoodCost(currentLevel), GetSoilCost(currentLevel))) return false;

            StartCoroutine(ResearchRoutine(attack));
            return true;
        }

        private IEnumerator ResearchRoutine(bool attack)
        {
            isResearching = true;
            yield return new WaitForSeconds(researchTimeSeconds);
            if (attack) attackLevel++;
            else armorLevel++;
            isResearching = false;
        }

        public static float GetAttackBonus(UnitRole targetRole)
        {
            var bonus = 0f;
            foreach (var lab in Active)
            {
                if (lab != null && lab.role == targetRole)
                    bonus = Mathf.Max(bonus, lab.attackLevel * lab.attackBonusPerLevel);
            }
            return bonus;
        }

        public static float GetArmorBonus(UnitRole targetRole)
        {
            var bonus = 0f;
            foreach (var lab in Active)
            {
                if (lab != null && lab.role == targetRole)
                    bonus = Mathf.Max(bonus, lab.armorLevel * lab.armorBonusPerLevel);
            }
            return bonus;
        }
    }
}
