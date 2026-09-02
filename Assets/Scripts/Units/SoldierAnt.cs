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
            Attacking
        }

        [SerializeField] private float autoEngageRadius = 5f;
        [SerializeField] private float autoEngageCheckInterval = 0.5f;

        private State state = State.Idle;
        private IDamageable currentTarget;
        private float attackTimer;
        private float autoEngageTimer;

        public void CommandMove(Vector3 destination)
        {
            currentTarget = null;
            Agent.SetDestination(destination);
            state = State.MovingToTarget;
        }

        public void CommandAttack(IDamageable target)
        {
            if (target == null) return;
            currentTarget = target;
            state = State.MovingToTarget;
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
                    currentTarget = null;
                    state = State.Idle;
                    return;
                }

                Agent.SetDestination(currentTarget.Position);
                if (Vector3.Distance(transform.position, currentTarget.Position) <= Data.attackRange)
                {
                    state = State.Attacking;
                }
                return;
            }

            if (!Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance)
            {
                state = State.Idle;
            }
        }

        private void TickAttacking()
        {
            if (currentTarget == null || currentTarget.IsDead)
            {
                currentTarget = null;
                state = State.Idle;
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
                currentTarget.TakeDamage(Data.attackDamage);
            }
        }
    }
}
