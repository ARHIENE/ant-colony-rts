using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

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

        public bool CanStartConstruction => isActiveAndEnabled && !IsDead && (state == State.Idle || state == State.MovingByCommand);

        public bool CanReach(Vector3 destination)
        {
            if (!Agent.enabled || !Agent.isOnNavMesh) return false;
            var path = new NavMeshPath();
            return Agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete
                && path.corners.Length > 0 && Vector3.Distance(path.corners[path.corners.Length - 1], destination) <= 1f;
        }

        public override void Initialize(UnitData data, ObjectPool sourcePool, GameObject prefab)
        {
            base.Initialize(data, sourcePool, prefab);
            state = State.Idle;
            targetNode = null;
            targetDeposit = null;
            targetConstruction = null;
            carriedAmount = 0f;
            carriedType = default;
            buildTimer = 0f;
        }

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
            if (node == null || !node.CanGather) return;
            if (carriedAmount > 0f && (carriedType != node.ResourceType || carriedAmount >= Data.carryCapacity))
            {
                BeginReturnIfNeeded();
                return;
            }

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
            if (Data == null || IsDead) return;
            if (!Agent.enabled || !Agent.isOnNavMesh) return;
            if ((state == State.MovingToNode || state == State.ReturningToStorage || state == State.MovingByCommand)
                && !Agent.pathPending && Agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                Agent.ResetPath();
                state = State.Idle;
                return;
            }
            switch (state)
            {
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
                Agent.ResetPath();
                state = State.Idle;
                return;
            }

            // 일시적인 경로 차단으로 이미 지불한 건설현장을 없애지 않는다.
            if (!Agent.pathPending && Agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                buildTimer -= Time.deltaTime;
                if (buildTimer <= 0f)
                {
                    Agent.SetDestination(targetConstruction.Position);
                    buildTimer = 0.5f;
                }
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
                Agent.ResetPath();
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

        private void TickMovingToNode()
        {
            if (targetNode == null || !targetNode.CanGather)
            {
                Agent.ResetPath();
                state = State.Idle;
                if (carriedAmount > 0f) BeginReturnIfNeeded();
                return;
            }

            if (HasArrived())
            {
                state = State.Gathering;
            }
        }

        private void TickGathering()
        {
            if (targetNode == null || !targetNode.CanGather)
            {
                Agent.ResetPath();
                state = State.Idle;
                if (carriedAmount > 0f) BeginReturnIfNeeded();
                return;
            }

            var extracted = targetNode.Extract(Mathf.Min(Data.gatherRate * targetNode.GatherRateMultiplier * Time.deltaTime, Data.carryCapacity - carriedAmount));
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
                Agent.ResetPath();
                state = State.Idle;
                return;
            }

            Agent.SetDestination(targetDeposit.transform.position);
            state = State.ReturningToStorage;
        }

        private void TickReturning()
        {
            if (targetDeposit == null || !targetDeposit.isActiveAndEnabled || targetDeposit.IsDead)
            {
                BeginReturnIfNeeded();
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
            return !Agent.pathPending && Agent.pathStatus == NavMeshPathStatus.PathComplete
                && Agent.remainingDistance <= Agent.stoppingDistance;
        }
    }
}
