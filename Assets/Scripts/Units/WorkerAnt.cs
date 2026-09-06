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
            MovingByCommand,
            MovingToBuildSite,
            Building
        }

        private State state = State.Idle;
        private ResourceNode targetNode;
        private BuildingBase targetDeposit;
        private float carriedAmount;
        private AntColony.Data.ResourceType carriedType;
        private BuildingConstructionSite targetConstruction;
        private float buildTimer;

        public bool CanStartConstruction => state == State.Idle || state == State.MovingByCommand;

        // 플레이어가 우클릭으로 직접 이동을 지시하면(빈 땅) 그 위치로 이동만 하고 멈춘다.
        public void CommandMove(Vector3 destination)
        {
            if (state == State.MovingToBuildSite || state == State.Building) return;
            targetNode = null;
            Agent.SetDestination(destination);
            state = State.MovingByCommand;
        }

        // 플레이어가 자원노드를 우클릭하면 그 자리로 이동해 채집을 시작한다(수동 채집 지시).
        public void CommandGather(ResourceNode node)
        {
            if (state == State.MovingToBuildSite || state == State.Building) return;
            if (node == null || node.IsDepleted) return;

            targetNode = node;
            Agent.SetDestination(node.transform.position);
            state = State.MovingToNode;
        }

        public void CommandBuild(BuildingConstructionSite site)
        {
            if (site == null || !CanStartConstruction) return;
            targetNode = null;
            targetConstruction = site;
            Agent.SetDestination(site.Position);
            state = State.MovingToBuildSite;
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
                case State.MovingToBuildSite:
                    TickMovingToBuildSite();
                    break;
                case State.Building:
                    TickBuilding();
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

        private void TickMovingToBuildSite()
        {
            if (targetConstruction == null)
            {
                state = State.Idle;
                return;
            }

            if (HasArrived())
            {
                buildTimer = targetConstruction.BuildTimeSeconds;
                state = State.Building;
            }
        }

        private void TickBuilding()
        {
            if (targetConstruction == null)
            {
                state = State.Idle;
                return;
            }

            buildTimer -= Time.deltaTime;
            if (buildTimer > 0f) return;

            targetConstruction.Complete();
            targetConstruction = null;
            state = State.Idle;
        }

        protected override void OnDisable()
        {
            if (targetConstruction != null)
            {
                targetConstruction.Cancel();
                targetConstruction = null;
            }
            base.OnDisable();
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
