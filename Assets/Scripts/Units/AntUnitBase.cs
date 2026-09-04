using System.Collections.Generic;
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

        public UnitData Data { get; private set; }
        public NavMeshAgent Agent { get; private set; }

        [SerializeField] private float currentHealth;

        private ObjectPool pool;
        private GameObject sourcePrefab;

        public bool IsDead => currentHealth <= 0f;
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
