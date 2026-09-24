using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Units
{
    public class SoldierAnt : AntUnitBase, IAirborne
    {
        protected enum State
        {
            Idle,
            MovingToTarget,
            Attacking,
            AttackMoving
        }

        [SerializeField] private float autoEngageRadius = 5f;
        [SerializeField] private float autoEngageCheckInterval = 0.5f;

        [Header("Flight (role == Flying)")]
        [SerializeField] private float flightAltitude = 3f;
        [SerializeField] private float arriveThreshold = 0.15f;
        [SerializeField] private LayerMask groundMask = 1 << 8;

        private const float GroundCastHeight = 100f;
        protected const float LandingSampleRadius = 10f;

        private State state = State.Idle;
        private IDamageable currentTarget;
        private float attackTimer;
        private float autoEngageTimer;
        private Vector3 flightDestination;

        // 어택무브(공격 이동) 중 경로상에서 자동으로 교전한 것이면 true.
        // 그 대상이 죽으면 원래 어택무브 목적지로 이동을 이어간다(원본 SIMUL-TeaamProject AntAttack.Chasing과 동일 컨벤션).
        private bool isOnAttackMove;
        private Vector3 attackMoveDestination;

        // 비행은 별도 클래스가 아니라 현재 역할로 결정된다. 지휘관이 Flying으로 역할을 바꾸면 즉시 비행한다.
        public virtual bool IsFlying => Data != null && Data.role == UnitRole.Flying;
        public bool IsInCombat => currentTarget != null && CanAttackTarget(currentTarget);
        private UnitRole TargetingRole => IsFlying ? UnitRole.Flying : Data.role;
        protected virtual float MovementSpeed => Data != null ? Data.moveSpeed : 0f;
        bool IAirborne.IsAirborne => IsFlying;

        public override void Initialize(UnitData data, ObjectPool sourcePool, GameObject prefab)
        {
            base.Initialize(data, sourcePool, prefab);
            state = State.Idle;
            currentTarget = null;
            attackTimer = 0f;
            autoEngageTimer = 0f;
            isOnAttackMove = false;
            attackMoveDestination = transform.position;
            ApplyMovementMode();
        }

        // 비행/지상 이동 방식을 현재 역할에 맞춘다. 역할 변경 시에도 다시 호출한다.
        public void ApplyMovementMode()
        {
            if (IsFlying)
            {
                if (Agent != null) Agent.enabled = false;
                flightDestination = ToFlightPoint(transform.position);
                transform.position = flightDestination;
                return;
            }

            if (Agent == null || Agent.enabled) return;

            // 착륙: 공중 좌표 그대로 에이전트를 켜면 NavMesh 밖이므로 가장 가까운 지면으로 내려놓는다.
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var landing, LandingSampleRadius, UnityEngine.AI.NavMesh.AllAreas))
                transform.position = landing.position;

            Agent.enabled = true;
            if (Agent.isOnNavMesh)
            {
                Agent.ResetPath();
                Agent.velocity = Vector3.zero;
            }
        }

        // 역할별 대공/대지 공격 가능 여부. 공중 대상은 Ranged와 Flying만 공격할 수 있다.
        public virtual bool CanAttackTarget(IDamageable target)
        {
            return Data != null && CombatTargeting.CanAttack(TargetingRole, target);
        }

        // 일반 이동: 원본처럼 경로상의 적을 무시하고 그냥 이동만 한다(어택무브는 CommandAttackMove로 별도 지시).
        public virtual void CommandMove(Vector3 destination)
        {
            currentTarget = null;
            isOnAttackMove = false;
            SetMoveDestination(destination);
            state = State.MovingToTarget;
        }

        // 특정 대상을 직접 지정해 공격(어택무브 중 자동 교전이 아니라 플레이어가 직접 지시한 경우).
        public virtual void CommandAttack(IDamageable target)
        {
            if (target == null || !CanAttackTarget(target)) return;
            isOnAttackMove = false;
            currentTarget = target;
            state = State.MovingToTarget;
        }

        // 어택무브: 목적지로 이동하되 경로상에서 적을 만나면 자동 교전, 처치 후 다시 목적지로 이동을 이어간다.
        public virtual void CommandAttackMove(Vector3 destination)
        {
            attackMoveDestination = destination;
            currentTarget = null;
            isOnAttackMove = true;
            SetMoveDestination(destination);
            state = State.AttackMoving;
        }

        // 전투 상태를 즉시 중단한다(일개미 작업이나 배속 변경이 전투 상태를 덮어쓸 때 사용).
        public virtual void CommandStop()
        {
            currentTarget = null;
            isOnAttackMove = false;
            StopMoving();
            state = State.Idle;
        }

        // 이동 지시/도착 판정/대상 거리 계산은 비행 여부에 따라 갈린다.
        protected virtual void SetMoveDestination(Vector3 destination)
        {
            if (IsFlying)
            {
                flightDestination = ToFlightPoint(destination);
                return;
            }
            if (Agent.enabled && Agent.isOnNavMesh) Agent.SetDestination(destination);
        }

        protected virtual void StopMoving()
        {
            if (IsFlying)
            {
                flightDestination = transform.position;
                return;
            }
            if (Agent.enabled && Agent.isOnNavMesh) Agent.ResetPath();
        }

        protected virtual bool HasReachedDestination()
        {
            if (IsFlying)
                return (transform.position - flightDestination).sqrMagnitude <= arriveThreshold * arriveThreshold;
            return Agent.enabled && Agent.isOnNavMesh && !Agent.pathPending
                && Agent.remainingDistance <= Agent.stoppingDistance;
        }

        // 비행 중 공격 사거리는 고도 차이를 빼고 XZ 평면 거리로만 판정한다.
        protected virtual float GetDistanceTo(Vector3 position)
        {
            var delta = position - transform.position;
            if (IsFlying) delta.y = 0f;
            return delta.magnitude;
        }

        // 목적지 Y는 항상 지면을 다시 찾아 계산한다. 공중 대상을 추적할 때 대상 고도에 고도를 또 더해 상승하는 것을 막는다.
        private Vector3 ToFlightPoint(Vector3 point)
        {
            var origin = new Vector3(point.x, point.y + GroundCastHeight, point.z);
            var altitude = Physics.Raycast(origin, Vector3.down, out var hit, GroundCastHeight * 2f, groundMask, QueryTriggerInteraction.Ignore)
                ? hit.point.y + flightAltitude
                : Mathf.Max(flightDestination.y, point.y);
            return new Vector3(point.x, altitude, point.z);
        }

        protected virtual void Update()
        {
            if (Data == null || IsDead) return;
            TickCombat();
            TickFlightMovement();
        }

        protected void TickFlightMovement()
        {
            if (!IsFlying) return;
            transform.position = Vector3.MoveTowards(transform.position, flightDestination, MovementSpeed * Time.deltaTime);
        }

        protected void TickCombat()
        {
            switch (state)
            {
                case State.Idle:
                    TickIdle();
                    break;
                case State.MovingToTarget:
                    TickMovingToTarget();
                    break;
                case State.Attacking:
                    TickAttacking();
                    break;
                case State.AttackMoving:
                    TickAttackMoving();
                    break;
            }
        }

        private void TickAttackMoving()
        {
            autoEngageTimer -= Time.deltaTime;
            if (autoEngageTimer <= 0f)
            {
                autoEngageTimer = autoEngageCheckInterval;
                var nearby = CombatTargeting.FindNearestEnemy(transform.position, autoEngageRadius, TargetingRole);
                if (nearby != null && CanAttackTarget(nearby))
                {
                    // isOnAttackMove는 유지한 채로 교전 상태로 전환(경로상 자동 교전).
                    currentTarget = nearby;
                    state = State.MovingToTarget;
                    return;
                }
            }

            if (HasReachedDestination())
            {
                isOnAttackMove = false;
                state = State.Idle;
            }
        }

        // 교전 중이던 대상을 잃었을 때: 어택무브 도중이었다면 원래 목적지로 이동을 이어가고,
        // 그게 아니면(직접 공격 지시였다면) Idle로 복귀한다.
        private void ResumeAfterTargetLost()
        {
            currentTarget = null;
            if (isOnAttackMove)
            {
                SetMoveDestination(attackMoveDestination);
                state = State.AttackMoving;
            }
            else
            {
                StopMoving();
                state = State.Idle;
            }
        }

        private void TickIdle()
        {
            autoEngageTimer -= Time.deltaTime;
            if (autoEngageTimer > 0f) return;
            autoEngageTimer = autoEngageCheckInterval;

            var nearby = CombatTargeting.FindNearestEnemy(transform.position, autoEngageRadius, TargetingRole);
            if (nearby != null && CanAttackTarget(nearby))
            {
                CommandAttack(nearby);
            }
        }

        private void TickMovingToTarget()
        {
            if (currentTarget != null)
            {
                if (!CanAttackTarget(currentTarget))
                {
                    ResumeAfterTargetLost();
                    return;
                }

                if (GetDistanceTo(currentTarget.Position) <= Data.attackRange)
                {
                    StopMoving();
                    state = State.Attacking;
                }
                else SetMoveDestination(currentTarget.Position);
                return;
            }

            // 일반 이동: 경로상의 적은 무시하고 그냥 목적지까지만 이동한다.
            if (HasReachedDestination())
            {
                state = State.Idle;
            }
        }

        private void TickAttacking()
        {
            if (!CanAttackTarget(currentTarget))
            {
                ResumeAfterTargetLost();
                return;
            }

            if (GetDistanceTo(currentTarget.Position) > Data.attackRange)
            {
                state = State.MovingToTarget;
                return;
            }

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = Data.attackInterval;
                GetComponent<AntVisual>()?.Attack(currentTarget.Position);
                DealDamage(currentTarget);
            }
        }

        // 실제 타격이 일어나는 유일한 지점. 비행/어택무브를 포함한 모든 공격이 여기를 지나므로
        // 처치 판정 같은 부가 처리는 이 메서드만 오버라이드하면 된다.
        protected virtual void DealDamage(IDamageable target)
        {
            target.TakeDamage(AttackDamage);
        }
    }
}
