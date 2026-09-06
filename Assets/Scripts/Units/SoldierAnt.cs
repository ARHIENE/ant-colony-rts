using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    public class SoldierAnt : AntUnitBase
    {
        private enum State
        {
            Idle,
            MovingToTarget,
            Attacking,
            AttackMoving
        }

        [SerializeField] private float autoEngageRadius = 5f;
        [SerializeField] private float autoEngageCheckInterval = 0.5f;

        private State state = State.Idle;
        private IDamageable currentTarget;
        private float attackTimer;
        private float autoEngageTimer;

        // 어택무브(공격 이동) 중 경로상에서 자동으로 교전한 것이면 true.
        // 그 대상이 죽으면 원래 어택무브 목적지로 이동을 이어간다(원본 SIMUL-TeaamProject AntAttack.Chasing과 동일 컨벤션).
        private bool isOnAttackMove;
        private Vector3 attackMoveDestination;

        // 일반 이동: 원본처럼 경로상의 적을 무시하고 그냥 이동만 한다(어택무브는 CommandAttackMove로 별도 지시).
        public void CommandMove(Vector3 destination)
        {
            currentTarget = null;
            isOnAttackMove = false;
            Agent.SetDestination(destination);
            state = State.MovingToTarget;
        }

        // 특정 대상을 직접 지정해 공격(어택무브 중 자동 교전이 아니라 플레이어가 직접 지시한 경우).
        public void CommandAttack(IDamageable target)
        {
            if (target == null) return;
            isOnAttackMove = false;
            currentTarget = target;
            state = State.MovingToTarget;
        }

        // 어택무브: 목적지로 이동하되 경로상에서 적을 만나면 자동 교전, 처치 후 다시 목적지로 이동을 이어간다.
        public void CommandAttackMove(Vector3 destination)
        {
            attackMoveDestination = destination;
            currentTarget = null;
            isOnAttackMove = true;
            Agent.SetDestination(destination);
            state = State.AttackMoving;
        }

        private void Update()
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
                var nearby = World.WildMonster.FindNearest(transform.position, autoEngageRadius);
                if (nearby != null)
                {
                    // isOnAttackMove는 유지한 채로 교전 상태로 전환(경로상 자동 교전).
                    currentTarget = nearby;
                    state = State.MovingToTarget;
                    return;
                }
            }

            if (!Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance)
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
                Agent.SetDestination(attackMoveDestination);
                state = State.AttackMoving;
            }
            else
            {
                state = State.Idle;
            }
        }

        private void TickIdle()
        {
            autoEngageTimer -= Time.deltaTime;
            if (autoEngageTimer > 0f) return;
            autoEngageTimer = autoEngageCheckInterval;

            var nearby = World.WildMonster.FindNearest(transform.position, autoEngageRadius);
            if (nearby != null)
            {
                CommandAttack(nearby);
            }
        }

        private void TickMovingToTarget()
        {
            if (currentTarget != null)
            {
                if (currentTarget.IsDead)
                {
                    ResumeAfterTargetLost();
                    return;
                }

                Agent.SetDestination(currentTarget.Position);
                if (Vector3.Distance(transform.position, currentTarget.Position) <= Data.attackRange)
                {
                    state = State.Attacking;
                }
                return;
            }

            // 일반 이동: 경로상의 적은 무시하고 그냥 목적지까지만 이동한다.
            if (!Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance)
            {
                state = State.Idle;
            }
        }

        private void TickAttacking()
        {
            if (currentTarget == null || currentTarget.IsDead)
            {
                ResumeAfterTargetLost();
                return;
            }

            if (Vector3.Distance(transform.position, currentTarget.Position) > Data.attackRange)
            {
                state = State.MovingToTarget;
                return;
            }

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = Data.attackInterval;
                currentTarget.TakeDamage(AttackDamage);
            }
        }
    }
}
