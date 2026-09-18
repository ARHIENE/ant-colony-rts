using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
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
        private IDamageable currentTarget;
        private NavMeshAgent agent;
        // 침공 개체만 플레이어 건물까지 노린다. 일반 야생 몬스터/반란 개체는 기존 동작 그대로다.
        private bool isRaider;
        public bool IsFlying { get; private set; }

        public bool IsDead => currentHealth <= 0f;
        public float CurrentHealth => currentHealth;
        public Vector3 Position => transform.position;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange;

            // 반란 후에도 비행 개체는 NavMesh에 붙이지 않는다.
            IsFlying = GetComponent<SoldierAnt>() is SoldierAnt soldier && soldier.IsFlying;
            agent.enabled = !IsFlying;
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

            targetSearchTimer -= Time.deltaTime;
            if (!CombatTargeting.IsAlive(currentTarget)) currentTarget = null;

            // 침공 개체는 살아 있는 대상이 있어도 주기적으로 재탐색해, 가로막는 방어 유닛을 먼저 노린다.
            if (currentTarget == null || isRaider)
            {
                if (targetSearchTimer > 0f)
                {
                    if (currentTarget == null)
                    {
                        StopMoving();
                        return;
                    }
                }
                else
                {
                    targetSearchTimer = targetSearchInterval;
                    currentTarget = (IDamageable)FindNearestAnt()
                        ?? (isRaider ? GameManager.Instance?.FindNearestPlayerBuilding(transform.position) : null);
                    if (currentTarget == null)
                    {
                        StopMoving();
                        return;
                    }
                }
            }

            var destination = currentTarget.Position;
            if (IsFlying) destination.y = transform.position.y;
            var distance = Vector3.Distance(transform.position, destination);
            if (distance > attackRange)
            {
                if (IsFlying)
                {
                    transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
                }
                else if (CanMove())
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
                Die();
            }
        }

        // 적 장수(EnemyCommander)가 포로 전환을 끼워 넣을 수 있도록 분리했다.
        protected virtual void Die()
        {
            // 침공 개체 처치는 야생 몬스터 루프 승리가 아니며, 비활성 오브젝트로 쌓이지 않게 제거한다.
            if (isRaider)
            {
                Destroy(gameObject);
                return;
            }
            gameObject.SetActive(false);
            GameManager.Instance?.ReportWildMonsterDefeated();
        }

        private AntUnitBase FindNearestAnt()
        {
            AntUnitBase nearest = null;
            var nearestDistanceSqr = detectionRadius * detectionRadius;
            foreach (var ant in AntUnitBase.Active)
            {
                // 야생 몬스터는 지상 유닛만 공격한다(공중 대상 제외).
                if (!CombatTargeting.IsAlive(ant)) continue;
                if (CombatTargeting.IsAirborne(ant)) continue;

                var distanceSqr = (ant.Position - transform.position).sqrMagnitude;
                if (distanceSqr <= nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearest = ant;
                }
            }
            return nearest;
        }

        public void MakeRaider() => isRaider = true;

        public void ConfigureWeakIntruder()
        {
            maxHealth = 15f;
            attackDamage = 1f;
            attackInterval = 2f;
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

        public static WildMonster FindNearest(Vector3 from, float maxRadius, UnitRole attackerRole)
        {
            WildMonster nearest = null;
            var nearestDistSqr = maxRadius * maxRadius;
            foreach (var monster in Active)
            {
                if (!CombatTargeting.CanAttack(attackerRole, monster)) continue;
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
