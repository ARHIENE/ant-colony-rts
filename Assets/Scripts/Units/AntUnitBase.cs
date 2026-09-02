using AntColony.Core;
using AntColony.Data;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Units
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(SelectableObject))]
    public class AntUnitBase : MonoBehaviour, IDamageable
    {
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
