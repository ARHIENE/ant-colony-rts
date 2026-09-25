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
        public static IReadOnlyList<WildMonster> All => Active;

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
        private bool eventWasp;
        internal float EventAttackCooldown { get => attackTimer; set => attackTimer = value; }
        internal void ConfigureEventWasp()
        {
            eventWasp = isRaider = IsFlying = true;
            maxHealth = EventRules.WaspHealth; attackDamage = EventRules.WaspDamage;
            attackInterval = EventRules.WaspInterval; moveSpeed = EventRules.WaspSpeed;
        }
        private ExpeditionSite raidSite;
        public bool IsFlying { get; private set; }
        // 함정에 걸리면 이동만 멈춘다(사거리 안이면 공격은 계속한다).
        public float RootRemaining { get; private set; }
        public void Root(float seconds) { RootRemaining = Mathf.Max(RootRemaining, seconds); StopMoving(); }

        public bool IsDead => currentHealth <= 0f;
        public float CurrentHealth => currentHealth;
        internal bool InCombat => CombatTargeting.IsAlive(currentTarget);
        internal void RestoreHealth(float value)
        {
            gameObject.SetActive(value > 0);
            currentHealth = value;
        }
        public Vector3 Position => transform.position;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange;

            // 반란 후에도 비행 개체는 NavMesh에 붙이지 않는다.
            IsFlying = eventWasp || GetComponent<SoldierAnt>() is SoldierAnt soldier && soldier.IsFlying;
            agent.enabled = !IsFlying;
            if (this is EnemyCommander) AntVisual.Attach(gameObject);
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
            RootRemaining = Mathf.Max(0f, RootRemaining - Time.deltaTime);

            targetSearchTimer -= Time.deltaTime;
            if (!CombatTargeting.IsAlive(currentTarget)) currentTarget = null;

            // 침공 개체는 살아 있는 대상이 있어도 주기적으로 재탐색해, 가로막는 방어 유닛을 먼저 노린다.
            if (currentTarget == null || isRaider)
            {
                if (targetSearchTimer > 0f)
                {
                    if (currentTarget == null)
                    {
                        ApproachSettlement();
                        return;
                    }
                }
                else
                {
                    targetSearchTimer = targetSearchInterval;
                    currentTarget = (eventWasp ? FindFirstObjectByType<QueenChamber>() : null) ?? (IDamageable)FindNearestAnt()
                        ?? (isRaider && raidSite == null ? GameManager.Instance?.FindNearestPlayerBuilding(transform.position) : null);
                    if (currentTarget == null)
                    {
                        ApproachSettlement();
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
                GetComponent<AntVisual>()?.Attack(currentTarget.Position);
                currentTarget.TakeDamage(attackDamage);
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            currentHealth -= amount;
            GetComponent<AntVisual>()?.Action("Hit");
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Die();
            }
        }

        // 적 장수(EnemyCommander)가 포로 전환을 끼워 넣을 수 있도록 분리했다.
        protected virtual void Die()
        {
            GetComponent<AntVisual>()?.Death();
            // 침공 개체 처치는 야생 몬스터 루프 승리가 아니며, 비활성 오브젝트로 쌓이지 않게 제거한다.
            if (isRaider)
            {
                if (eventWasp) CampaignHistory.Record("이벤트 결과", "기생 말벌", "말벌 1마리 처치");
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

        public void MakeRaider() { isRaider = true; AntVisual.Attach(gameObject); }

        internal void RaidSettlement(ExpeditionSite site)
        {
            isRaider = true;
            raidSite = site;
            AntVisual.Attach(gameObject);
        }

        private void ApproachSettlement()
        {
            if (raidSite != null && CanMove()) agent.SetDestination(raidSite.Landing);
            else StopMoving();
        }

        public void ConfigureWeakIntruder()
        {
            maxHealth = 15f;
            attackDamage = 1f;
            attackInterval = 2f;
        }

        private bool CanMove()
        {
            return agent != null && agent.enabled && agent.isOnNavMesh && RootRemaining <= 0f;
        }

        private void StopMoving()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath)
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
