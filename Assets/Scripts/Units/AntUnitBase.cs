using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Units
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(SelectableObject))]
    public class AntUnitBase : MonoBehaviour, IDamageable
    {
        public static readonly List<AntUnitBase> Active = new List<AntUnitBase>();

        public UnitData Data { get; protected set; }
        public NavMeshAgent Agent { get; private set; }

        [SerializeField] private float currentHealth;

        private ObjectPool pool;
        private GameObject sourcePrefab;

        public virtual bool IsDead => currentHealth <= 0f;
        public virtual float CurrentHealth => currentHealth;

        // 연구 보너스는 저장하지 않고 매번 계산한다. 연구 완료가 살아있는 유닛에 즉시 반영된다.
        public virtual float AttackDamage => Data == null ? 0f : Data.attackDamage + ResearchLab.GetAttackBonus(Data.role);
        public virtual float Armor => Data == null ? 0f : Data.armor + ResearchLab.GetArmorBonus(Data.role);
        public Vector3 Position => transform.position;

        protected virtual void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
        }

        protected virtual void OnEnable()
        {
            Active.Add(this);
        }

        protected virtual void OnDisable()
        {
            Active.Remove(this);
        }

        // 식량 부족으로 인한 반란: 플레이어 통제를 벗어나 그 자리에서 야생 개체로 전환된다.
        public void Rebel()
        {
            if (IsDead) return;
            var selectable = GetComponent<SelectableObject>();
            if (selectable != null) selectable.enabled = false;
            if (Agent != null) Agent.enabled = false;
            gameObject.AddComponent<WildMonster>();
            Destroy(this);
        }

        public virtual void Initialize(UnitData data, ObjectPool sourcePool, GameObject prefab)
        {
            Data = data;
            pool = sourcePool;
            sourcePrefab = prefab;
            currentHealth = data.maxHealth;
            Agent.speed = data.moveSpeed;
            if (Agent.enabled && Agent.isOnNavMesh)
            {
                Agent.ResetPath();
                Agent.velocity = Vector3.zero;
            }
        }

        public virtual void TakeDamage(float amount)
        {
            if (IsDead) return;
            currentHealth -= Mathf.Max(1f, amount - Armor);
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Die();
            }
        }

        protected virtual void Die()
        {
            gameObject.SetActive(false);
            if (pool != null && sourcePrefab != null)
            {
                pool.Release(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
