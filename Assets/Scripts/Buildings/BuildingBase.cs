using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    public class BuildingBase : MonoBehaviour, IDamageable
    {
        private static readonly List<BuildingBase> DepositPoints = new List<BuildingBase>();

        [SerializeField] protected BuildingData data;
        [SerializeField] private bool countsTowardPlayerDefeat = true;
        [SerializeField, Min(1f)] private float fallbackMaxHealth = 300f;
        [SerializeField] private float currentHealth;

        public BuildingData Data => data;
        public bool CountsTowardPlayerDefeat => countsTowardPlayerDefeat;
        public float MaxHealth => BaseMaxHealth * (UsesDefenseDurability ? DefenseUpgrades.DurabilityMultiplier : 1f);
        // 저장 검증용: 연구소 내구 라인이 최고 단계일 때의 체력 상한.
        internal float MaxPossibleHealth => BaseMaxHealth * (UsesDefenseDurability ? 1f + .2f * DefenseUpgrades.MaxLevel : 1f);
        private float BaseMaxHealth => data != null ? data.maxHealth : fallbackMaxHealth;
        // 방어시설(분사탑·흙벽)은 방어시설 연구소 내구 라인을 받는다. 기본 건물은 방어력 0이다.
        protected virtual bool UsesDefenseDurability => false;
        public virtual float Armor => 0f;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;
        public Vector3 Position => transform.position;
        internal void ConfigureRuntime(BuildingData definition) { data = definition; currentHealth = MaxHealth; }

        protected virtual bool IsDepositPoint => false;

        protected virtual void Awake()
        {
            currentHealth = MaxHealth;
        }

        protected virtual void OnEnable()
        {
            if (IsDepositPoint)
            {
                DepositPoints.Add(this);
            }
            // 건설 중인 건물과 배치용 템플릿은 비활성 상태이므로, 완공되어 활성화된 건물만 콜로니 존속 판정에 등록된다.
            GameManager.Instance?.RegisterBuilding(this);
        }

        protected virtual void OnDisable()
        {
            DepositPoints.Remove(this);
            GameManager.Instance?.UnregisterBuilding(this);
        }

        // 저장 복원 전용. 0 이하로는 내리지 않는다(복원 중 파괴 연쇄를 일으키지 않기 위해).
        internal void RestoreHealth(float value)
        {
            currentHealth = Mathf.Clamp(value, this is Workshop ? 0f : 0.01f, MaxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || !(amount > 0f)) return;
            // 방어력은 피해를 깎되, 약한 공격도 최소 1(원래 피해가 더 작으면 그 값)은 들어간다.
            currentHealth -= Mathf.Max(Mathf.Min(amount, 1f), amount - Armor);
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Die();
            }
        }

        protected virtual void Die()
        {
            Destroy(gameObject);
        }

        public virtual void DepositResources(ResourceType type, int amount) => ResourceManager.Instance?.Add(type, amount);

        public static BuildingBase FindNearestDepositPoint(Vector3 from)
        {
            BuildingBase nearest = null;
            var nearestDistSqr = float.MaxValue;
            foreach (var point in DepositPoints)
            {
                if (point == null) continue;
                var distSqr = (point.transform.position - from).sqrMagnitude;
                if (distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = point;
                }
            }
            return nearest;
        }
    }
}
