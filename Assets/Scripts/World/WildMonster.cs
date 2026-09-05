using System.Collections.Generic;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class WildMonster : MonoBehaviour, IDamageable
    {
        private static readonly List<WildMonster> Active = new List<WildMonster>();

        [SerializeField] private float maxHealth = 150f;
        [SerializeField] private float detectionRadius = 8f;
        [SerializeField] private float attackDamage = 8f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackInterval = 1.25f;
        [SerializeField] private float targetSearchInterval = 0.5f;
        [SerializeField] private float moveSpeed = 2.5f;

        private float currentHealth;
        private float attackTimer;
        private float targetSearchTimer;
        private AntUnitBase currentTarget;
        private NavMeshAgent agent;

        public bool IsDead => currentHealth <= 0f;
        public float CurrentHealth => currentHealth;
        public Vector3 Position => transform.position;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange;

            // 반란으로 전환된 개미는 기존 에이전트가 꺼진 상태이므로 야생화 직후 다시 이동할 수 있게 한다.
            if (!agent.enabled)
            {
                agent.enabled = true;
            }
        }

        private void OnEnable()
        {
            currentHealth = maxHealth;
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
            currentTarget = null;
            StopMoving();
        }

        private void Update()
        {
            if (IsDead) return;

            if (currentTarget == null || currentTarget.IsDead || !currentTarget.isActiveAndEnabled)
            {
                currentTarget = null;
                targetSearchTimer -= Time.deltaTime;
                if (targetSearchTimer > 0f)
                {
                    StopMoving();
                    return;
                }

                targetSearchTimer = targetSearchInterval;
                currentTarget = FindNearestAnt();
                if (currentTarget == null)
                {
                    StopMoving();
                    return;
                }
            }

            var distance = Vector3.Distance(transform.position, currentTarget.Position);
            if (distance > attackRange)
            {
                if (CanMove())
                {
                    agent.SetDestination(currentTarget.Position);
                }
                return;
            }

            StopMoving();
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = attackInterval;
                currentTarget.TakeDamage(attackDamage);
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            currentHealth -= amount;
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                gameObject.SetActive(false);
                GameManager.Instance?.ReportWildMonsterDefeated();
            }
        }

        private AntUnitBase FindNearestAnt()
        {
            AntUnitBase nearest = null;
            var nearestDistanceSqr = detectionRadius * detectionRadius;
            foreach (var ant in AntUnitBase.Active)
            {
                if (ant == null || ant.IsDead || !ant.isActiveAndEnabled) continue;

                var distanceSqr = (ant.Position - transform.position).sqrMagnitude;
                if (distanceSqr <= nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearest = ant;
                }
            }
            return nearest;
        }

        private bool CanMove()
        {
            return agent != null && agent.enabled && agent.isOnNavMesh;
        }

        private void StopMoving()
        {
            if (CanMove() && agent.hasPath)
            {
                agent.ResetPath();
            }
        }

        public static WildMonster FindNearest(Vector3 from, float maxRadius)
        {
            WildMonster nearest = null;
            var nearestDistSqr = maxRadius * maxRadius;
            foreach (var monster in Active)
            {
                if (monster == null || monster.IsDead) continue;
                var distSqr = (monster.transform.position - from).sqrMagnitude;
                if (distSqr <= nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = monster;
                }
            }
            return nearest;
        }
    }
}
