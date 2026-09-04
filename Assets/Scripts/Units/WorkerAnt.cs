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
            Depositing,
            MovingByCommand
        }

        private State state = State.Idle;
        private ResourceNode targetNode;
        private BuildingBase targetDeposit;
        private float carriedAmount;
        private AntColony.Data.ResourceType carriedType;

        // 플레이어가 우클릭으로 직접 이동을 지시하면 채집 루프를 중단하고 그 위치로 이동한다.
        // 도착하면 다시 자동 채집 루프(Idle)로 복귀한다.
        public void CommandMove(Vector3 destination)
        {
            targetNode = null;
            Agent.SetDestination(destination);
            state = State.MovingByCommand;
        }

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
                case State.MovingByCommand:
                    TickMovingByCommand();
                    break;
            }
        }

        private void TickMovingByCommand()
        {
            if (HasArrived())
            {
                state = State.Idle;
            }
        }

        // 자동 채집 없음: 플레이어가 우클릭으로 직접 움직여주기 전까지는 가만히 있는다.
        private void TickIdle()
        {
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
