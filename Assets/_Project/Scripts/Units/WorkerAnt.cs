using System.Linq;
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
            Building,
            FetchingMaterial,   // 건설 재료를 가지러 창고로(2026-10-12)
            DeliveringMaterial  // 가져온 재료를 예정지로
        }

        private State state = State.Idle;
        private ResourceNode targetNode;
        private BuildingBase targetDeposit;
        private StoreBin targetBin; // 본거지 반납 목적지 칸(2026-10-12). 원정 화물선 반납은 null.
        private IMaterialSink deliverySite; private StoreBin fetchBin; private ResourceType fetchType; private int fetchAmount;
        public IMaterialSink DeliverySite => deliverySite;
        private float carriedAmount;
        private FoodLots carriedLots = new FoodLots(); // 들고 있는 식품의 신선도(2026-10-12)
        private AntColony.Data.ResourceType carriedType;
        private BuildingConstructionSite targetConstruction;
        private float buildTimer;

        public virtual bool CanStartConstruction => isActiveAndEnabled && !IsDead && state == State.Idle;

        // 자원을 들고 있거나 건설 중이면 병력 배정/보직 변경을 막아야 한다(운반 중 자원 증발 방지).
        public float CarriedAmount => carriedAmount;
        public ResourceType CarriedType => carriedType;
        internal void RestoreCargo(float amount, ResourceType type, FoodLots lots = null)
        {
            carriedAmount = amount; carriedType = type; carriedLots = lots ?? new FoodLots();
            if (FoodLots.Perishable(type)) carriedLots.Match(Mathf.CeilToInt(amount), type);
        }
        internal FoodLots CaptureCargoLots() => JsonUtility.FromJson<FoodLots>(JsonUtility.ToJson(carriedLots));
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
                if (drop.TryGetComponent<FloorFood>(out var food)) food.Lots.Add(carriedLots); // 들고 있던 식품 신선도 그대로
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
            if (drop.TryGetComponent<FloorFood>(out var food)) food.Lots.Add(carriedLots); // 들고 있던 식품 신선도 그대로
            drop.AddComponent<ResourceNodeStatus>(); drop.SetActive(true);
            carriedAmount = 0; CommandStop();
        }
        private void ReleaseDestination()
        {
            targetBin = null; ResourceManager.Instance?.Reserve(this, null, default, 0);
            if (deliverySite != null) { deliverySite.AbortPickup(this); deliverySite = null; }
        }

        // 건설 재료 배송(2026-10-12): 예약된 재료가 있는 창고에 가서 집고 예정지에 가져다 놓는다. 운반 장수와 건설 장수 모두 한다.
        // 조리대 재료도 같은 흐름(2026-10-15): 요리사가 창고에서 집어 와 조리대에 놓고, 조리 시작 때 소비한다.
        public bool CommandFetchFor(IMaterialSink site)
        {
            var rm = ResourceManager.Instance;
            if (site == null || !site.NextPickup(out var needType, out var needAmount) || rm == null || IsCarrying || !isActiveAndEnabled || IsDead || !TryWorkApproach(site.Position, out _)) return false;
            foreach (var bin in rm.Bins.Where(b => b.Spendable && b.Amount(needType) > 0).OrderBy(b => (b.Position - Position).sqrMagnitude))
            {
                var host = bin.Host != null ? bin.Host : BuildingBase.FindNearestDepositPoint(bin.Position);
                if (host == null || !TryWorkApproach(host.Position, out var approach)) continue;
                var amount = Mathf.Min(needAmount, Mathf.FloorToInt(CarryCapacity), bin.Amount(needType));
                if (amount <= 0) return false;
                CommandStop();
                if (!site.BeginPickup(this, needType, amount)) return false;
                deliverySite = site; fetchBin = bin; fetchType = needType; fetchAmount = amount;
                SetMoveDestination(approach); state = State.FetchingMaterial;
                return true;
            }
            return false;
        }
        // 시설 장애물 가장자리에서 몇 cm 못 미쳐 멈추는 경우도 도착으로 본다(2026-10-15 조리대 배송에서 발견).
        private bool NearDestination => !IsFlying && Agent.enabled && Agent.isOnNavMesh && !Agent.pathPending && Agent.remainingDistance <= .5f;
        private void TickFetching()
        {
            if (deliverySite == null || deliverySite.Paused) { ReleaseDestination(); StopMoving(); state = State.Idle; return; }
            if (!HasReachedDestination() && !NearDestination) return;
            FoodLots lots = null; var taken = ResourceManager.Instance != null ? ResourceManager.Instance.TakeFrom(fetchBin, fetchType, fetchAmount, out lots) : 0;
            if (taken <= 0) { ReleaseDestination(); state = State.Idle; return; } // 그새 다른 소비로 비었다 → 다음 판단에서 다시 예약
            deliverySite.BeginPickup(this, fetchType, taken); // 실제 집은 양으로 맞춘다(모자란 몫은 예약 분배가 다시 채운다)
            carriedAmount = taken; carriedType = fetchType; carriedLots = lots ?? new FoodLots();
            if (!TryWorkApproach(deliverySite.Position, out var approach)) { DropCargo(); return; }
            SetMoveDestination(approach); state = State.DeliveringMaterial;
        }
        private void TickDelivering()
        {
            if (deliverySite == null) { state = State.Idle; if (IsCarrying) BeginReturnIfNeeded(); return; } // 예정지가 사라지면 짐은 창고로
            if (!HasReachedDestination() && !NearDestination) return;
            var given = deliverySite.Deliver(this, carriedType, Mathf.FloorToInt(carriedAmount + .001f));
            carriedAmount = Mathf.Max(0, carriedAmount - given); deliverySite = null;
            if (carriedAmount >= 1) BeginReturnIfNeeded(); else { carriedAmount = 0; state = State.Idle; }
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

        // 실제 이동 거리(경로 길이). 갈 수 없으면 무한대. 비행 중이면 직선거리.
        public float TravelDistance(Vector3 destination)
        {
            if (IsFlying) return Vector3.Distance(Position, destination);
            if (!Agent.enabled || !Agent.isOnNavMesh || !NavMesh.SamplePosition(destination, out var hit, 4, NavMesh.AllAreas)) return float.PositiveInfinity;
            var path = new NavMeshPath();
            if (!Agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) return float.PositiveInfinity;
            float length = 0; for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return length;
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
            targetConstruction = null; // 예정지·진행도는 남겨 다른 장수(또는 복귀 후 본인)가 이어 짓는다.
            ReleaseDestination();
            targetNode = null;
            state = State.Idle;
            base.CommandStop();
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
            if (site == null || !CanStartConstruction) return; // 여러 장수가 한 예정지에 함께 참여한다.
            if (!site.CanWork) { if (!site.Paused && !site.Ready) CommandFetchFor(site); return; } // 재료가 다 오기 전엔 시공하지 않고 재료를 가지러 간다
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
            var bin = commander != null && commander.IsAwayFromHome ? null : ResourceManager.Instance?.BinOf(deposit);
            if (bin != null && !bin.Accepts(carriedType, this)) return false; // 허용하지 않거나 가득 찬 창고
            if (!TryWorkApproach(deposit.Position, out var approach)) return false;
            CommandStop();
            targetDeposit = deposit; targetBin = bin;
            if (bin != null) ResourceManager.Instance.Reserve(this, bin, carriedType, Mathf.CeilToInt(carriedAmount));
            SetMoveDestination(approach);
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
                if ((state == State.MovingToNode || state == State.ReturningToStorage || state == State.FetchingMaterial || state == State.DeliveringMaterial)
                    && !Agent.pathPending && Agent.pathStatus != NavMeshPathStatus.PathComplete)
                {
                    // 운반 중 길이 막혀 갈 수 없으면 현재 위치에 내려놓고 다른 작업을 찾는다(2026-10-12). 원정지 화물은 기존대로 들고 멈춘다.
                    var home = !(this is CommanderAnt c) || !c.IsAwayFromHome;
                    StopMoving();
                    if ((state == State.ReturningToStorage || state == State.DeliveringMaterial) && home && IsCarrying) { DropCargo(); return; }
                    if (state == State.FetchingMaterial) ReleaseDestination();
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
                case State.FetchingMaterial:
                    TickFetching();
                    break;
                case State.DeliveringMaterial:
                    TickDelivering();
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

            if (HasReachedDestination()) state = State.Building;
        }

        private void TickBuilding()
        {
            if (targetConstruction == null)
            {
                StopMoving();
                state = State.Idle;
                return;
            }

            if (!targetConstruction.CanWork) { targetConstruction = null; StopMoving(); state = State.Idle; return; } // 일시정지 등
            targetConstruction.BeginWork(); // 재료가 모두 도착한 뒤 첫 시공 순간 소비
            // 공동 건설: 참여한 장수마다 자기 작업 효율만큼 현장의 남은 작업량을 줄인다.
            OnWorked(Mathf.Min(Time.deltaTime, targetConstruction.RemainingWork / WorkSpeed));
            targetConstruction.RemainingWork = Mathf.Max(0, targetConstruction.RemainingWork - Time.deltaTime * WorkSpeed);
            if (targetConstruction.RemainingWork > 0f) return;

            targetConstruction.Complete(this as CommanderAnt);
            if (this is CommanderAnt builder) builder.SetWorkTarget(null);
            targetConstruction = null;
            state = State.Idle;
        }

        protected override void OnDisable()
        {
            targetConstruction = null; ReleaseDestination();
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

            if (targetNode.IsRegrowing || targetNode.HoldsHandWork) return;
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
                if (carriedAmount <= 0f) carriedLots = new FoodLots();
                var floor = targetNode.TryGetComponent<FloorFood>(out var food) ? food.Sync() : null; var before = floor?.Total ?? 0;
                extracted = targetNode.Extract(Mathf.Min(GatherRate * targetNode.GatherRateMultiplier * Time.deltaTime, CarryCapacity - carriedAmount));
                if (floor != null) carriedLots.Add(floor.Take(before - Mathf.RoundToInt(targetNode.AmountRemaining))); // 바닥 식품은 신선도 낮은 몫부터 집는다
                carriedAmount += extracted;
                carriedType = targetNode.ResourceType;
                if (extracted > 0f) { OnGathered(extracted); OnWorked(extracted / (GatherRate * targetNode.GatherRateMultiplier)); }
            }

            // 같은 물자는 운반량 한도까지 묶어 나른다: 바닥 더미를 다 집었는데 자리가 남으면 가까운 같은 종류 더미를 더 집는다(2026-10-12).
            if (targetNode.IsDepleted && targetNode.IsLooseCargo && carriedAmount < CarryCapacity && NextLoosePile() is ResourceNode next
                && TryWorkApproach(next.transform.position, out var nextApproach))
            {
                targetNode = next; SetMoveDestination(nextApproach); state = State.MovingToNode;
                if (this is CommanderAnt self) self.SetWorkTarget(next);
                return;
            }
            // 밭 수확물은 현장 바닥에 내려놓고 운반 작업이 옮긴다(2026-10-15 V5 기획).
            if (targetNode.TryGetComponent<AntColony.Buildings.FarmPlot>(out _) && (carriedAmount >= CarryCapacity || targetNode.IsDepleted))
            {
                var node = targetNode; DropCargo();
                if (!node.IsDepleted && node.CanGather) { targetNode = node; state = State.Gathering; if (this is CommanderAnt me) me.SetWorkTarget(node); }
                return;
            }
            if (carriedAmount >= CarryCapacity || targetNode.IsDepleted)
            {
                BeginReturnIfNeeded();
            }
        }

        // ponytail: 8m 안 같은 종류 바닥 더미만 묶는다. 다른 장수가 맡은 더미는 건너뛴다.
        private ResourceNode NextLoosePile() => ResourceNode.Available.Where(n => n != null && n != targetNode && n.IsLooseCargo && n.CanGather
                && n.ResourceType == carriedType && !n.IsRaidLoot && (n.transform.position - Position).sqrMagnitude < 64f
                && !AntUnitBase.Active.OfType<WorkerAnt>().Any(w => w != this && w.CurrentResourceNode == n))
            .OrderBy(n => (n.transform.position - Position).sqrMagnitude).FirstOrDefault();

        // 노드에서 실제로 캐낸 양이 있을 때만 호출된다.
        protected virtual void OnGathered(float amount) { }
        protected virtual void OnWorked(float seconds) { }

        internal void ReturnCargoToStorage() => BeginReturnIfNeeded();

        private void BeginReturnIfNeeded()
        {
            ReleaseDestination();
            if (this is CommanderAnt commander && commander.IsAwayFromHome)
            {
                targetDeposit = commander.Transport != null ? commander.Transport : commander.Garrison.DockedTransport;
                if (targetDeposit == null) { StopMoving(); state = State.Idle; return; }
                SetMoveDestination(targetDeposit.transform.position);
                state = State.ReturningToStorage;
                return;
            }
            // 본거지: 허용·빈 공간이 있는 칸 중 우선순위 높은 곳 → 가까운 곳. 받을 곳이 없거나 갈 수 없으면 현재 위치에 내려놓는다(2026-10-12).
            targetDeposit = null; var rm = ResourceManager.Instance; var approach = Vector3.zero;
            if (rm != null)
                foreach (var bin in rm.Destinations(carriedType, Position, this))
                {
                    var host = bin.Host != null ? bin.Host : BuildingBase.FindNearestDepositPoint(bin.Position);
                    if (host == null || !TryWorkApproach(host.Position, out approach)) continue;
                    targetBin = bin; targetDeposit = host; break;
                }
            if (targetDeposit == null) { if (IsCarrying) DropCargo(); else { StopMoving(); state = State.Idle; } return; }
            rm.Reserve(this, targetBin, carriedType, Mathf.CeilToInt(carriedAmount));
            SetMoveDestination(approach);
            state = State.ReturningToStorage;
        }

        private void TickReturning()
        {
            // 목적지가 없어지거나 품목이 금지되면 다른 창고를 찾고, 없으면 현재 위치에 내려놓는다.
            if (targetDeposit == null || !targetDeposit.isActiveAndEnabled || targetDeposit.IsDead || targetBin != null && (!targetBin.Usable || !targetBin.Allows(carriedType)))
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
                    var bin = targetBin ?? resources.BinOf(targetDeposit);
                    int amount = resources.AddTo(bin, carriedType, Mathf.FloorToInt(carriedAmount + .001f), this, lots: carriedLots);
                    if (amount > 0) { carriedAmount = Mathf.Max(0f, carriedAmount - amount); OnDelivered(amount); }
                }
                ReleaseDestination();
                if (carriedAmount >= 1) { BeginReturnIfNeeded(); return; } // 다 못 넣었으면 다음 창고, 없으면 바닥
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
