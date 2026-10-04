using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Units
{
    // 채집/건설 작업을 SoldierAnt(이동·전투) 위에 얹은 계층.
    // workState가 Idle이면 전투 계층이 그대로 돌고, 작업 중에는 전투 tick을 돌리지 않는다.
    // 이렇게 해야 한 GameObject에 AntUnitBase 파생 컴포넌트를 둘 이상 붙이지 않고도 보직 전환이 가능하다.
    public class WorkerAnt : SoldierAnt
    {
        private new enum State
        {
            Idle,
            MovingToNode,
            Gathering,
            ReturningToStorage,
            Depositing,
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

        public virtual bool CanStartConstruction => isActiveAndEnabled && !IsDead && state == State.Idle;

        // 자원을 들고 있거나 건설 중이면 병력 배정/보직 변경을 막아야 한다(운반 중 자원 증발 방지).
        public float CarriedAmount => carriedAmount;
        public ResourceType CarriedType => carriedType;
        internal void RestoreCargo(float amount, ResourceType type) { carriedAmount = amount; carriedType = type; }
        public bool IsCarrying => carriedAmount > 0f;
        public bool IsWorking => state != State.Idle;
        public bool IsGatheringAnimation => state == State.Gathering;
        public bool IsBuildingAnimation => state == State.Building;
        public BuildingConstructionSite ConstructionTarget => targetConstruction;
        public bool IsConstructing => state == State.MovingToBuildSite || state == State.Building;

        // 거점 상실 때도 이미 채집한 자원은 화물 또는 회수 가능한 현장 노드로 남긴다.
        internal void EvacuateCargo(ExpeditionSite site, ExpeditionTransport ship = null)
        {
            if (!IsCarrying) return;
            if (ship != null) ship.DepositResources(carriedType, Mathf.RoundToInt(carriedAmount));
            else
            {
                var drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                drop.name = "Dropped " + carriedType;
                drop.SetActive(false);
                drop.transform.SetParent(site.transform);
                drop.transform.position = new Vector3(Position.x, .3f, Position.z);
                drop.transform.localScale = Vector3.one * .6f;
                drop.AddComponent<ResourceNode>().ConfigureLoot(carriedType, carriedAmount);
                drop.AddComponent<ResourceNodeStatus>();
                drop.SetActive(true);
            }
            carriedAmount = 0;
            CommandStop();
        }

        internal void DropCargo()
        {
            if (!IsCarrying) return;
            var drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            drop.name = "Dropped " + carriedType; drop.SetActive(false);
            drop.transform.position = Position; drop.transform.localScale = Vector3.one * .6f;
            drop.AddComponent<ResourceNode>().ConfigureLoot(carriedType, carriedAmount);
            drop.AddComponent<ResourceNodeStatus>(); drop.SetActive(true);
            carriedAmount = 0; CommandStop();
        }

        // 채집 성능은 장수가 병력 수만큼 배수로 올릴 수 있게 훅으로 분리한다.
        public ResourceNode CurrentResourceNode => targetNode;
        protected virtual float WorkSpeed => 1f;
        protected virtual float GatherRate => Data.gatherRate;
        protected virtual float CarryCapacity => Data.carryCapacity;
        protected virtual float FishingCatchMultiplier => 1f;
        protected virtual float FishingWorkSpeed => 1f;
        protected virtual void OnDelivered(float amount) { }
        private float fishingProgress;
        private ResourceNode fishingNode; // 진행도가 속한 낚시터. 불러온 직후(null)엔 처음 낚는 곳이 이어받는다.
        internal float FishingProgress => fishingProgress;
        internal void RestoreFishing(float progress) { fishingProgress = Mathf.Clamp(progress, 0f, GameBalance.FishingCatchSeconds); fishingNode = null; }

        public bool CanReach(Vector3 destination)
        {
            if (IsFlying) return true;
            if (!Agent.enabled || !Agent.isOnNavMesh) return false;
            var path = new NavMeshPath();
            return Agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete
                && path.corners.Length > 0 && Vector3.Distance(path.corners[path.corners.Length - 1], destination) <= 1f;
        }

        public bool TryWorkApproach(Vector3 target, out Vector3 destination)
        {
            destination = target;
            if (IsFlying) return true;
            if (!NavMesh.SamplePosition(target, out var hit, 4, NavMesh.AllAreas)) return false;
            destination = hit.position;
            return CanReach(destination);
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

        // 전투/이동 명령은 작업을 중단시킨다. 단 이미 지불한 건설현장을 버리게 두지는 않는다.
        public override void CommandMove(Vector3 destination)
        {
            if (IsConstructing) return;
            AbandonGathering();
            base.CommandMove(destination);
        }

        public override void CommandAttack(IDamageable target)
        {
            if (IsConstructing) return;
            AbandonGathering();
            base.CommandAttack(target);
        }

        public override void CommandAttackMove(Vector3 destination)
        {
            if (IsConstructing) return;
            AbandonGathering();
            base.CommandAttackMove(destination);
        }

        // 채집 목표만 버리고 이미 들고 있는 화물은 유지한다(명령 한 번으로 자원이 사라지지 않게).
        private void AbandonGathering()
        {
            targetNode = null;
            state = State.Idle;
        }

        // 정지는 전투뿐 아니라 진행 중인 작업/건설까지 정리한다.
        // 병력이 0이 된 장수가 계속 채집·건설 tick을 도는 것을 막는 단일 지점이다.
        public override void CommandStop()
        {
            CancelConstruction();
            targetNode = null;
            state = State.Idle;
            base.CommandStop();
        }

        // 예약 인력 반환은 건설현장이 담당하므로 여기서는 참조만 정확히 한 번 넘긴다.
        private void CancelConstruction()
        {
            if (targetConstruction == null) return;
            var site = targetConstruction;
            targetConstruction = null;
            site.Cancel();
        }

        internal void SuspendWork()
        {
            // Mobilization leaves the paid blueprint and completed work for the next civilian.
            targetConstruction = null;
            CommandStop();
            DropCargo();
        }

        // 플레이어가 자원노드를 우클릭하면 그 자리로 이동해 채집을 시작한다(수동 채집 지시).
        public virtual void CommandGather(ResourceNode node)
        {
            if (IsConstructing) return;
            if (node == null || !node.CanGather) return;
            if (!TryWorkApproach(node.transform.position, out var approach)) return;
            if (carriedAmount > 0f && (carriedType != node.ResourceType || carriedAmount >= CarryCapacity))
            {
                BeginReturnIfNeeded();
                return;
            }

            CommandStop();
            targetNode = node;
            SetMoveDestination(approach);
            state = State.MovingToNode;
        }

        public virtual void CommandBuild(BuildingConstructionSite site)
        {
            if (site == null || site.HasBuilder || !CanStartConstruction) return;
            CommandStop();
            targetNode = null;
            targetConstruction = site;
            SetMoveDestination(site.Position);
            state = State.MovingToBuildSite;
        }

        // 이동/전투로 중단된 운반도 반납 장소를 우클릭해 다시 시작할 수 있다.
        public bool TryReturnCargo(BuildingBase deposit)
        {
            if (!isActiveAndEnabled || IsDead || !IsCarrying || IsConstructing
                || deposit == null || !deposit.isActiveAndEnabled || deposit.IsDead) return false;
            var commander = this as CommanderAnt;
            if (commander != null && (commander.IsCaptive || commander.IsEmbarked)) return false;
            if (commander != null && commander.IsAwayFromHome)
            {
                var ship = commander.Transport != null ? commander.Transport : commander.Garrison.DockedTransport;
                if (deposit != ship || ship == null || ship.State != ExpeditionState.Deployed) return false;
            }
            else if (!deposit.CountsTowardPlayerDefeat || (!(deposit is Stockpile) && !(deposit is Storage))) return false;
            if (!CanReach(deposit.Position)) return false;
            CommandStop();
            targetDeposit = deposit;
            SetMoveDestination(deposit.Position);
            state = State.ReturningToStorage;
            return true;
        }

        protected override void Update()
        {
            if (Data == null || IsDead) return;

            // 작업이 없으면 전투 계층(자동 교전 포함)이 그대로 동작한다.
            if (state == State.Idle)
            {
                base.Update();
                return;
            }

            if (!IsFlying)
            {
                if (!Agent.enabled || !Agent.isOnNavMesh) return;
                if ((state == State.MovingToNode || state == State.ReturningToStorage)
                    && !Agent.pathPending && Agent.pathStatus != NavMeshPathStatus.PathComplete)
                {
                    StopMoving();
                    state = State.Idle;
                    return;
                }
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
                case State.MovingToBuildSite:
                    TickMovingToBuildSite();
                    break;
                case State.Building:
                    TickBuilding();
                    break;
            }

            TickFlightMovement();
        }

        private void TickMovingToBuildSite()
        {
            if (targetConstruction == null)
            {
                StopMoving();
                state = State.Idle;
                return;
            }

            // 일시적인 경로 차단으로 이미 지불한 건설현장을 없애지 않는다.
            if (!IsFlying && !Agent.pathPending && Agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                buildTimer -= Time.deltaTime;
                if (buildTimer <= 0f)
                {
                    SetMoveDestination(targetConstruction.Position);
                    buildTimer = 0.5f;
                }
                return;
            }

            if (HasReachedDestination())
            {
                buildTimer = targetConstruction.RemainingWork;
                state = State.Building;
            }
        }

        private void TickBuilding()
        {
            if (targetConstruction == null)
            {
                StopMoving();
                state = State.Idle;
                return;
            }

            OnWorked(Mathf.Min(Time.deltaTime, buildTimer / WorkSpeed));
            buildTimer -= Time.deltaTime * WorkSpeed;
            targetConstruction.RemainingWork = Mathf.Max(0, buildTimer);
            if (buildTimer > 0f) return;

            targetConstruction.Complete(this as CommanderAnt);
            if (this is CommanderAnt builder) builder.SetWorkTarget(null);
            targetConstruction = null;
            state = State.Idle;
        }

        protected override void OnDisable()
        {
            CancelConstruction();
            base.OnDisable();
        }

        private void TickMovingToNode()
        {
            if (targetNode == null || !targetNode.CanGather)
            {
                StopMoving();
                state = State.Idle;
                if (carriedAmount > 0f) BeginReturnIfNeeded();
                return;
            }

            if (HasReachedDestination())
            {
                state = State.Gathering;
            }
        }

        private void TickGathering()
        {
            if (targetNode == null || !targetNode.CanGather)
            {
                StopMoving();
                state = State.Idle;
                if (carriedAmount > 0f) BeginReturnIfNeeded();
                return;
            }

            if (targetNode.IsRegrowing) return;
            float extracted;
            if (targetNode.RequiresFishing)
            {
                // 낚시: 20초마다 한 번, Food 6 × 낚시 배율. 진행도는 반납 왕복·저장을 넘어 이어지고, 다른 낚시터로 바꾸면 처음부터.
                if (fishingNode != targetNode) { if (fishingNode != null) fishingProgress = 0f; fishingNode = targetNode; }
                fishingProgress += Time.deltaTime * FishingWorkSpeed;
                if (fishingProgress < GameBalance.FishingCatchSeconds) return;
                fishingProgress -= GameBalance.FishingCatchSeconds;
                extracted = targetNode.Extract(Mathf.Min(GameBalance.FishingCatchFood * FishingCatchMultiplier * BiomeRules.FishingYield, CarryCapacity - carriedAmount));
                carriedAmount += extracted;
                carriedType = targetNode.ResourceType;
                if (extracted > 0f) { OnGathered(extracted); OnWorked(GameBalance.FishingCatchSeconds / FishingWorkSpeed); }
            }
            else
            {
                fishingNode = null; fishingProgress = 0f;
                extracted = targetNode.Extract(Mathf.Min(GatherRate * targetNode.GatherRateMultiplier * Time.deltaTime, CarryCapacity - carriedAmount));
                carriedAmount += extracted;
                carriedType = targetNode.ResourceType;
                if (extracted > 0f) { OnGathered(extracted); OnWorked(extracted / (GatherRate * targetNode.GatherRateMultiplier)); }
            }

            if (carriedAmount >= CarryCapacity || targetNode.IsDepleted)
            {
                BeginReturnIfNeeded();
            }
        }

        // 노드에서 실제로 캐낸 양이 있을 때만 호출된다.
        protected virtual void OnGathered(float amount) { }
        protected virtual void OnWorked(float seconds) { }

        internal void ReturnCargoToStorage() => BeginReturnIfNeeded();

        private void BeginReturnIfNeeded()
        {
            targetDeposit = this is CommanderAnt commander && commander.IsAwayFromHome
                ? commander.Transport != null ? commander.Transport : commander.Garrison.DockedTransport
                : BuildingBase.FindNearestDepositPoint(transform.position);
            if (targetDeposit == null)
            {
                StopMoving();
                state = State.Idle;
                return;
            }

            SetMoveDestination(targetDeposit.transform.position);
            state = State.ReturningToStorage;
        }

        private void TickReturning()
        {
            if (targetDeposit == null || !targetDeposit.isActiveAndEnabled || targetDeposit.IsDead)
            {
                BeginReturnIfNeeded();
                return;
            }

            if (HasReachedDestination())
            {
                state = State.Depositing;
            }
        }

        private void Deposit()
        {
            if (this is CommanderAnt commander && !commander.IsAwayFromHome)
            {
                var resources = ResourceManager.Instance;
                if (resources != null && targetDeposit != null && !targetDeposit.IsDead)
                {
                    // 프레임 단위 채집 합산 오차(10 → 9.9999971)로 1이 바닥 더미로 떨어지지 않게 여유를 둔다.
                    int amount = Mathf.Min(Mathf.FloorToInt(carriedAmount + .001f), resources.GetCapacity(carriedType) - resources.GetAmount(carriedType));
                    if (amount > 0) { targetDeposit.DepositResources(carriedType, amount); carriedAmount = Mathf.Max(0f, carriedAmount - amount); OnDelivered(amount); }
                }
                if (carriedAmount > 0 && carriedAmount < 1) DropCargo();
                state = State.Idle; return;
            }
            if (carriedAmount > 0f && ResourceManager.Instance != null)
            {
                if (targetDeposit != null) { targetDeposit.DepositResources(carriedType, Mathf.RoundToInt(carriedAmount)); OnDelivered(carriedAmount); }
            }
            carriedAmount = 0f;
            state = State.Idle;
        }
    }
}
