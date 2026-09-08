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
        public float MaxHealth => data != null ? data.maxHealth : fallbackMaxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;
        public Vector3 Position => transform.position;

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

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            currentHealth -= amount;
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
