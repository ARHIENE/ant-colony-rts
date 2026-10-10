using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    public enum WildlifeTemperament { Timid, Defensive, Predator }

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
        private float siegeCheck;
        private NavMeshPath siegePath; // 필드 초기화로 만들면 네이티브 경로가 비어 예외가 난다. 처음 쓸 때 만든다.
        private NavMeshAgent agent;
        // 침공 개체만 플레이어 건물까지 노린다. 일반 야생 몬스터/반란 개체는 기존 동작 그대로다.
        private bool isRaider;
        private bool eventWasp;
        private bool nightPredator;
        public string DiplomaticFactionId { get; internal set; }
        public bool Allied { get; internal set; }
        public string RebelId { get; internal set; }
        internal void ConfigureForce(float health) { maxHealth = health; }
        // 목장 생물(2026-10-11): 평소엔 온순(Critter가 이동·먹기를 맡음, 함정·방어시설·경보 대상 아님), 취급 실패·공격받으면 잠시 성향대로 행동.
        internal void ConfigureCritter(float health, float damage) { maxHealth = health; attackDamage = damage; Docile = true; }
        public bool Docile { get; internal set; }
        internal void Calm() { provoked = false; currentTarget = null; StopMoving(); }
        public NavMeshAgent Agent => agent;
        internal float MoveSpeed => moveSpeed;
        internal float MaxHealth => maxHealth;
        internal void Heal(float amount) { if (!IsDead && amount > 0) currentHealth = Mathf.Min(maxHealth, currentHealth + amount); }
        // 놀라게 하지 않는 체력 감소(굶주림·포식·노령·도축).
        public void Wound(float amount)
        {
            if (IsDead || !(amount > 0)) return;
            currentHealth -= amount;
            if (currentHealth <= 0f) { currentHealth = 0f; Die(); }
        }
        internal float EventAttackCooldown { get => attackTimer; set => attackTimer = value; }
        internal void ConfigureEventWasp()
        {
            eventWasp = isRaider = IsFlying = true;
            maxHealth = EventRules.WaspHealth; attackDamage = EventRules.WaspDamage;
            attackInterval = EventRules.WaspInterval; moveSpeed = EventRules.WaspSpeed;
        }
        internal void ConfigureNightPredator()
        {
            nightPredator = true;
            isRaider = true;
            maxHealth = EventRules.NightPredatorHealth; attackDamage = EventRules.NightPredatorDamage;
            attackInterval = EventRules.WaspInterval; moveSpeed = EventRules.NightPredatorSpeed;
        }
        private ExpeditionSite raidSite;
        public bool IsFlying { get; private set; }
        public WildlifeTemperament Temperament { get; set; } = WildlifeTemperament.Predator;
        public bool HuntDesignated { get; set; }
        public bool Huntable => isActiveAndEnabled && !IsDead && !isRaider && !(this is EnemyCommander) && !Allied
            && string.IsNullOrEmpty(DiplomaticFactionId) && string.IsNullOrEmpty(RebelId) && GetComponentInParent<ExpeditionSite>() == null;
        private bool provoked;
        // 함정에 걸리면 이동만 멈춘다(사거리 안이면 공격은 계속한다).
        public float RootRemaining { get; private set; }
        public void Root(float seconds) { RootRemaining = Mathf.Max(RootRemaining, seconds); StopMoving(); }
        // 사육 생물 탈출·포획 실패(2026-10-10): 놀라게 하거나(도주형은 달아남) 다룬 장수를 노리게 한다.
        internal void Provoke() => provoked = true;
        internal void Aggro(IDamageable target) { provoked = true; currentTarget = target; }

        public bool IsDead => currentHealth <= 0f;
        public float CurrentHealth => currentHealth;
        internal bool InCombat => CombatTargeting.IsAlive(currentTarget);
        public void RestoreHealth(float value)
        {
            gameObject.SetActive(value > 0);
            currentHealth = value;
        }
        public Vector3 Position => transform.position;

        private Critter critter;
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            critter = GetComponent<Critter>();
            // 중심 피벗의 임시 큐브는 발 위치를 NavMesh에 맞춘다.
            var model = GetComponent<MeshFilter>();
            if (!(this is EnemyCommander) && model != null && model.sharedMesh != null)
                agent.baseOffset = -model.sharedMesh.bounds.min.y * transform.lossyScale.y;
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
            if (IsDead || Docile) return;
            if (!Allied && !DiplomacyManager.Hostile(this)) { currentTarget = null; StopMoving(); return; }
            RootRemaining = Mathf.Max(0f, RootRemaining - Time.deltaTime);

            if (Huntable && (Temperament == WildlifeTemperament.Timid || Temperament == WildlifeTemperament.Defensive && !provoked))
            {
                if (Temperament == WildlifeTemperament.Timid && provoked && FindNearestAnt() is IDamageable threat && CanMove())
                {
                    var away = Position + (Position - threat.Position).normalized * 5;
                    if (NavMesh.SamplePosition(away, out var hit, 5, NavMesh.AllAreas)) agent.SetDestination(hit.position);
                }
                return;
            }
            targetSearchTimer -= Time.deltaTime;
            if (!CombatTargeting.IsAlive(currentTarget)) currentTarget = null;

            // 침공 개체는 살아 있는 대상이 있어도 주기적으로 재탐색해, 가로막는 방어 유닛을 먼저 노린다.
            if (currentTarget == null || isRaider && !IsBarrier(currentTarget))
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
                    currentTarget = Allied ? CombatTargeting.FindNearestEnemy(Position, detectionRadius, UnitRole.Melee)
                        : (eventWasp ? FindFirstObjectByType<Stockpile>() : null) ?? (IDamageable)FindNearestAnt()
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
            // 벽은 장애물이라 가운데까지 못 가므로 표면까지의 거리로 잰다.
            var distance = IsBarrier(currentTarget) && ((Component)currentTarget).GetComponent<Collider>() is Collider wallCollider
                ? Vector3.Distance(transform.position, wallCollider.ClosestPoint(transform.position)) : Vector3.Distance(transform.position, destination);
            if (distance > attackRange)
            {
                if (IsFlying)
                {
                    transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
                }
                else if (CanMove())
                {
                    // 공성(2026-10-03): 침공 개체는 길이 벽에 막히면 가장 가까운 벽·문·닫힌 성문을 부순다.
                    if (isRaider && !IsBarrier(currentTarget) && (siegeCheck -= Time.deltaTime) <= 0)
                    {
                        siegeCheck = targetSearchInterval; siegePath ??= new NavMeshPath();
                        if (NavMesh.SamplePosition(currentTarget.Position, out var goal, 2f, NavMesh.AllAreas)
                            && NavMesh.CalculatePath(agent.nextPosition, goal.position, NavMesh.AllAreas, siegePath)
                            && siegePath.status == NavMeshPathStatus.PathPartial && FindBarrier() is BuildingBase barrier) currentTarget = barrier;
                    }
                    agent.speed = moveSpeed * AntColony.Map.WeatherSystem.MoveAt(false) * (AntColony.Map.MapGenerator.InWater(Position) ? AntColony.Map.MapGenerator.WaterMoveMultiplier : 1f);
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
                if (currentTarget is BuildingBase flammable && WallFire.Flammable(flammable)) WallFire.Ignite(flammable); // 가연성 재료(나뭇잎 벽·목재 등)엔 불을 지른다.
                currentTarget.TakeDamage(attackDamage);
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || !(amount > 0) || float.IsInfinity(amount)) return;
            provoked = true;
            if (!Allied && !DiplomacyManager.TryAttack(this)) return;
            if (critter != null) critter.OnHurtByUnit(); // 사냥·전투로 공격받은 목장 생물은 성향대로 반응
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
            if (critter != null) { HuntDesignated = false; critter.OnKilled(); return; } // 목장 생물: 사체(해체 가능)·관리 알림은 Critter가
            if (!(this is EnemyCommander captured) || !captured.WasCaptured)
                Corpse.Drop(this, this is EnemyCommander ? CorpseKind.EnemyCommander : isRaider && !eventWasp && !nightPredator || GetComponent<AntUnitBase>() != null ? CorpseKind.Ant : CorpseKind.Wildlife,
                    this is EnemyCommander commander ? commander.CommanderName : name,
                    food: HuntDesignated && !isRaider && !(this is EnemyCommander) ? 20 : 0);
            HuntDesignated = false;
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

        private IDamageable FindNearestAnt()
        {
            IDamageable nearest = null;
            var seen = detectionRadius * AntColony.Map.WeatherSystem.Visibility; // 안개·모래폭풍은 시야를 줄인다.
            var nearestDistanceSqr = seen * seen;
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
            foreach (var ally in Active)
                if (ally.Allied && !ally.IsDead && (ally.Position - Position).sqrMagnitude < nearestDistanceSqr)
                { nearestDistanceSqr = (ally.Position - Position).sqrMagnitude; nearest = ally; }
            return nearest;
        }

        public void MakeRaider() { isRaider = true; AntVisual.Attach(gameObject); }

        internal void RaidSettlement(ExpeditionSite site)
        {
            isRaider = true;
            raidSite = site;
            AntVisual.Attach(gameObject);
        }

        public static bool IsBarrier(IDamageable target) => target is Wall || target is SoilWall || target is Door || target is Gate gate && !gate.Open;
        private BuildingBase FindBarrier()
        {
            BuildingBase best = null; var bestSqr = float.MaxValue;
            foreach (var hit in Physics.OverlapSphere(Position, GameBalance.SiegeSearchRadius, ~0, QueryTriggerInteraction.Ignore))
            {
                var b = hit.GetComponentInParent<BuildingBase>();
                if (b == null || b.IsDead || !IsBarrier(b)) continue;
                var d = (hit.ClosestPoint(Position) - Position).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = b; }
            }
            return best;
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
