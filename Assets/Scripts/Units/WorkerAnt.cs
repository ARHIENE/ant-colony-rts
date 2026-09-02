using AntColony.Buildings;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public class WorkerAnt : AntUnitBase
    {
        private enum State
        {
            Idle,
            MovingToNode,
            Gathering,
            ReturningToStorage,
            Depositing
        }

        [SerializeField] private float searchInterval = 1f;

        private State state = State.Idle;
        private ResourceNode targetNode;
        private BuildingBase targetDeposit;
        private float carriedAmount;
        private AntColony.Data.ResourceType carriedType;
        private float searchTimer;

        private void Update()
        {
            switch (state)
            {
                case State.Idle:
                    TickIdle();
                    break;
                case State.MovingToNode:
                    TickMovingToNode();
                    break;
                case State.Gathering:
                    TickGathering();
                    break;
                case State.ReturningToStorage:
                    TickReturning();
                    break;
                case State.Depositing:
                    Deposit();
                    break;
            }
        }

        private void TickIdle()
        {
            searchTimer -= Time.deltaTime;
            if (searchTimer > 0f) return;
            searchTimer = searchInterval;

            targetNode = ResourceNode.FindNearestActive(transform.position);
            if (targetNode == null) return;

            Agent.SetDestination(targetNode.transform.position);
            state = State.MovingToNode;
        }

        private void TickMovingToNode()
        {
            if (targetNode == null || targetNode.IsDepleted)
            {
                state = State.Idle;
                return;
            }

            if (HasArrived())
            {
                state = State.Gathering;
            }
        }

        private void TickGathering()
        {
            if (targetNode == null || targetNode.IsDepleted)
            {
                state = carriedAmount > 0f ? State.ReturningToStorage : State.Idle;
                BeginReturnIfNeeded();
                return;
            }

            var extracted = targetNode.Extract(Data.gatherRate * Time.deltaTime);
            carriedAmount += extracted;
            carriedType = targetNode.ResourceType;

            if (carriedAmount >= Data.carryCapacity || targetNode.IsDepleted)
            {
                carriedAmount = Mathf.Min(carriedAmount, Data.carryCapacity);
                BeginReturnIfNeeded();
            }
        }

        private void BeginReturnIfNeeded()
        {
            targetDeposit = BuildingBase.FindNearestDepositPoint(transform.position);
            if (targetDeposit == null)
            {
                state = State.Idle;
                return;
            }

            Agent.SetDestination(targetDeposit.transform.position);
            state = State.ReturningToStorage;
        }

        private void TickReturning()
        {
            if (targetDeposit == null)
            {
                state = State.Idle;
                return;
            }

            if (HasArrived())
            {
                state = State.Depositing;
            }
        }

        private void Deposit()
        {
            if (carriedAmount > 0f && ResourceManager.Instance != null)
            {
                ResourceManager.Instance.Add(carriedType, Mathf.RoundToInt(carriedAmount));
            }
            carriedAmount = 0f;
            state = State.Idle;
        }

        private bool HasArrived()
        {
            return !Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance;
        }
    }
}
