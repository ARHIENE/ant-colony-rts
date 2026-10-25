using System.Linq;
using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    public enum ExpeditionState { Home, Outbound, Deployed, Returning }

    // 전투용 건물이 아닌 수송/화물 보관소. 장수 GameObject를 끄지 않아 병력과 성장이 보존된다.
    public partial class ExpeditionTransport : BuildingBase
    {
        private readonly List<CommanderAnt> crew = new List<CommanderAnt>();
        private readonly Dictionary<AntColony.Data.ResourceType, int> cargo = new Dictionary<AntColony.Data.ResourceType, int>();
        public List<EquipmentItem> EquipmentCargo { get; internal set; } = new List<EquipmentItem>();
        public bool BlueprintCargo { get; internal set; }
        private Vector3 homePosition;
        public IReadOnlyList<CommanderAnt> Crew => crew;
        public ExpeditionState State { get; private set; }
        public ExpeditionSite Site { get; private set; }
        public bool Aircraft { get; private set; }
        public TransportRoute Route { get; private set; }
        public bool HasCargo
        {
            get { foreach (var amount in cargo.Values) if (amount > 0) return true; return BlueprintCargo || EquipmentCargo.Count > 0 || BossRewardTransfer.CargoLoad(this) > 0; }
        }
        // 장수 슬롯과 일반개미 정원은 따로 센다.
        // 대형 수송: 일반개미 +50%·장수 +2 / 수송 효율: 화물 +50%
        public int Capacity => Walking ? int.MaxValue : Mathf.FloorToInt((Aircraft ? GameBalance.AircraftTroops : GameBalance.VehicleTroops) * ScienceEffects.TroopCapacityMultiplier);
        public int CommanderCapacity => Walking ? int.MaxValue : (Aircraft ? GameBalance.AircraftCommanders : GameBalance.VehicleCommanders) + ScienceEffects.CommanderSlotBonus;
        public int CargoCapacity => Walking ? WalkingCargoCapacity : Mathf.FloorToInt((Aircraft ? GameBalance.AircraftCargo : GameBalance.VehicleCargo) * ScienceEffects.CargoMultiplier);
        public float TravelSeconds => Walking ? ExpeditionRules.WalkSeconds(Site) : Aircraft ? 10 : 20;
        public float Remaining { get; private set; }
        public int Load
        {
            get { var total = 0; foreach (var c in crew) if (c != null) total += c.TroopCount; return total; }
        }
        public int CommanderLoad
        {
            get { var total = 0; foreach (var c in crew) if (c != null) total++; return total; }
        }
        public int CargoLoad
        {
            get { var total = BossRewardTransfer.CargoLoad(this); foreach (var amount in cargo.Values) total += amount; return total; }
        }
        public bool CargoFull => CargoLoad >= CargoCapacity;
        protected override bool IsDepositPoint => true;

        public void Initialize(bool aircraft, bool walking = false)
        {
            Route = new TransportRoute(this);
            Aircraft = aircraft;
            Walking = walking;
            homePosition = transform.position;
            name = walking ? "도보 원정대" : aircraft ? "Aircraft" : "Vehicle";
            GetComponent<Renderer>().material.color = aircraft ? Color.cyan : new Color(.85f, .7f, .3f);
        }

        public int GetCargo(AntColony.Data.ResourceType type) => cargo.TryGetValue(type, out var amount) ? amount : 0;

        internal Vector3 HomePosition => homePosition;

        // 저장 복원 전용. 승무원은 CommanderAnt 복원이 끝난 뒤 RestoreCrew로 따로 넣는다.
        internal void RestoreState(ExpeditionState state, float remaining, ExpeditionSite site,
            Vector3 position, Vector3 home, int food, int soil, int special)
        {
            homePosition = home;
            State = state;
            Remaining = Mathf.Max(0f, remaining);
            Site = site;
            if (site != null && (state == ExpeditionState.Outbound || state == ExpeditionState.Deployed)) site.Visitor = this;
            transform.position = position;
            cargo[AntColony.Data.ResourceType.Food] = Mathf.Max(0, food);
            cargo[AntColony.Data.ResourceType.Soil] = Mathf.Max(0, soil);
            cargo[AntColony.Data.ResourceType.Special] = Mathf.Max(0, special);
        }

        // 저장 복원 전용. 이동 중/귀환 중이면 탑승 상태(렌더러 끔)로, 현지 전개 중이면 내려 둔 상태로 되돌린다.
        internal void RestoreCrew(List<CommanderAnt> passengers)
        {
            crew.Clear();
            if (passengers == null) return;
            foreach (var c in passengers)
            {
                if (c == null) continue;
                crew.Add(c);
                c.Transport = this;
            }
            if (State == ExpeditionState.Deployed) LandCrew();
            else foreach (var c in crew) if (c != null) c.SetEmbarked(true, Position);
        }

        public bool TryBoard(IReadOnlyList<CommanderAnt> passengers)
        {
            return QueueCrewOrders(passengers, CrewOrderKind.Board);
        }

        private bool BoardNow(CommanderAnt c)
        {
            if (!c.CanChangeAllocation || Load + c.TroopCount > Capacity || CommanderLoad + 1 > CommanderCapacity
                || State == ExpeditionState.Home && ColonyPopulation.Instance?.S.policy == MilitaryPolicy.Reserve && c.HasTroops) return false;
            c.Transport?.DetachCommander(c);
            if (c.Garrison != null) c.Garrison.Remove(c);
            crew.Add(c);
            c.Transport = this;
            // 현장 합류는 귀환 명부에만 넣는다. 실제 숨김은 TryReturn에서 처리한다.
            if (State == ExpeditionState.Home) c.SetEmbarked(true, Position);
            else c.CommandStop();
            return true;
        }

        public bool TryStation(IReadOnlyList<CommanderAnt> passengers)
        {
            return QueueCrewOrders(passengers, CrewOrderKind.Station);
        }

        private bool StationNow(CommanderAnt c)
        {
            var settlement = Site != null ? Site.Settlement : null;
            if (settlement == null || !settlement.isActiveAndEnabled || Site.Disposition != ConquestDisposition.Annexed
                || !c.HasTroops || !c.CanChangeAllocation) return false;
            c.CommandStop();
            crew.Remove(c);
            c.Transport = null;
            settlement.Add(c);
            return true;
        }

        public bool TryUnloadCrew()
        {
            if (Route != null && Route.IsRunning) return false;
            if (State != ExpeditionState.Home || crew.Count == 0) return false;
            return QueueCrewOrders(new List<CommanderAnt>(crew), CrewOrderKind.Unload);
        }

        internal void DetachCommander(CommanderAnt c)
        {
            CancelCrewOrder(c);
            crew.Remove(c); c.Transport = null;
            Route?.Stop("장수 이탈로 자동 수송 중단");
        }

        public bool TryDepart(ExpeditionSite site)
        {
            if (PendingCrewOrderCount > 0) return false;
            if (Route != null && Route.IsRunning
                && (site != Route.Destination || HasCargo || Route.WaitSeconds > 0)) return false;
            var world = WorldMapManager.Instance;
            if (!isActiveAndEnabled || world == null || !world.Unlocked || State != ExpeditionState.Home
                || site == null || !world.Sites.Contains(site) || !WorldMapManager.SiteRevealed(site)
                || (crew.Count == 0 && site.Disposition != ConquestDisposition.Annexed)
                || Walking && !crew.Any(c => c != null && !c.IsDead)
                || site.Disposition == ConquestDisposition.Abandoned || Load > Capacity || CommanderLoad > CommanderCapacity) return false;
            Site = site;
            if (site.Visitor == null) site.Visitor = this;
            State = ExpeditionState.Outbound;
            foreach (var c in crew) if (c != null && c.IsColonyMember) c.OnExpeditionStarted();
            Remaining = TravelSeconds;
            return true;
        }

        public bool TryReturn()
        {
            if (!TryEmbarkToLeave()) return false;
            State = ExpeditionState.Returning;
            Remaining = TravelSeconds;
            return true;
        }

        // 자동 수송 경유: 본거지를 거치지 않고 현지에서 다음 편입 거점으로 출발한다.
        internal bool TryContinueTo(ExpeditionSite next)
        {
            if (next == null || next == Site || !TryEmbarkToLeave()) return false;
            if (Site != null && Site.Visitor == this) Site.Visitor = null;
            Site = next;
            if (next.Visitor == null) next.Visitor = this;
            State = ExpeditionState.Outbound;
            Remaining = TravelSeconds;
            return true;
        }

        private bool TryEmbarkToLeave()
        {
            if (PendingCrewOrderCount > 0) return false;
            if (!isActiveAndEnabled || State != ExpeditionState.Deployed) return false;
            foreach (var c in crew)
                if (c != null && !c.IsDead && (!c.isActiveAndEnabled || c.OrderRefusalReason != "" || c.IsCarrying || c.IsConstructing
                    || Vector3.Distance(c.Position, Position) > 8)) return false;
            if (Site != null && Site.Settlement != null)
            {
                foreach (var c in Site.Settlement.Garrison)
                    if (c != null && c.IsCarrying) return false;
                // 수송수단이 떠난 뒤 채집을 계속해 반납할 곳 없이 화물을 들지 않게 한다.
                foreach (var c in Site.Settlement.Garrison) if (c != null && c.IsWorking) c.CommandStop();
            }
            foreach (var c in crew) if (c != null) c.SetEmbarked(true, Position);
            if (WorldMapManager.Instance.ViewedSite == Site) WorldMapManager.Instance.ViewSite(null);
            return true;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
            if (State == ExpeditionState.Home) UnloadCargo();
            Route?.Tick(Time.deltaTime);
        }

        internal void EvacuateLostSite()
        {
            Route?.Stop("Stopped: destination lost");
            if (State == ExpeditionState.Home || State == ExpeditionState.Returning) return;
            foreach (var c in crew)
                if (c != null)
                {
                    c.EvacuateCargo(Site, this);
                    c.SetEmbarked(true, Position);
                }
            State = ExpeditionState.Returning;
            Remaining = TravelSeconds;
        }

        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || !(seconds > 0) || float.IsInfinity(seconds)) return;
            TickCrewOrders();
            DockingRemaining = Mathf.Max(0, DockingRemaining - seconds);
            if (State != ExpeditionState.Home) TickSupplies(seconds);
            if (State != ExpeditionState.Outbound && State != ExpeditionState.Returning) return;
            Remaining = Mathf.Max(0, Remaining - seconds);
            if (Remaining > 0) return;
            if (State == ExpeditionState.Outbound)
            {
                if (!Walking && WorldMapManager.Instance.Transports.Any(t => t != null && t != this && t.Site == Site
                    && t.State == ExpeditionState.Deployed && t.DockingRemaining > 0)) return;
                DockingRemaining = Walking ? 0 : ExpeditionRules.DockSeconds;
                transform.position = Site.Landing;
                State = ExpeditionState.Deployed;
                LandCrew();
                DiplomacyManager.Instance?.Contact(Site);
                if (Site.Kind == ExpeditionSiteKind.TradePost) AntColony.UI.GameMenuController.Instance?.TradeAt(Site);
            }
            else
            {
                if (!TryHomeLanding(out var landing)) return;
                transform.position = landing;
                State = ExpeditionState.Home;
                if (Site != null && Site.Visitor == this) Site.Visitor = null;
                Site = null;
                TryUnloadCrew();
                UnloadCargo();
            }
        }

        private void LandCrew()
        {
            for (var i = 0; i < crew.Count; i++)
            {
                if (crew[i] == null || crew[i].IsDead) continue;
                var candidate = Position + new Vector3(3 + i % 4, 0, i / 4 * 1.5f);
                if (NavMesh.SamplePosition(candidate, out var hit, 6, NavMesh.AllAreas)) candidate = hit.position;
                crew[i].SetEmbarked(false, candidate);
            }
        }

        public override void DepositResources(AntColony.Data.ResourceType type, int amount)
        {
            // 핵심물과 일반 화물은 같은 적재 한도를 사용한다.
            AddCargo(type, amount);
            if (State == ExpeditionState.Home) UnloadCargo();
        }

        public int AddCargo(AntColony.Data.ResourceType type, int amount)
        {
            var added = Mathf.Clamp(amount, 0, Mathf.Max(0, CargoCapacity - CargoLoad));
            if (added > 0) cargo[type] = GetCargo(type) + added;
            return added;
        }

        private void UnloadCargo()
        {
            if (State != ExpeditionState.Home || !TryHomeLanding(out var landing)) return;
            BossRewardTransfer.Deliver(this);
            var resources = ResourceManager.Instance;
            foreach (AntColony.Data.ResourceType type in System.Enum.GetValues(typeof(AntColony.Data.ResourceType)))
            {
                if (type == AntColony.Data.ResourceType.Food && crew.Count > 0 && Route?.IsRunning != true) continue;
                var amount = GetCargo(type);
                if (amount <= 0) continue;
                cargo[type] = 0; // Add는 창고 초과분까지 바닥에 보존한다. 이벤트 재진입 전에 화물을 뺀다.
                if (resources != null) resources.Add(type, amount, ResourceReason.Expedition, landing);
                else DiplomacyManager.DropFloor(type, amount, landing);
            }
            if (BlueprintCargo && CampaignResearch.Instance != null) { CampaignResearch.Instance.AcquireBlueprint(); BlueprintCargo = false; }
            if (EquipmentInventory.Instance != null)
            {
                var overflow = new List<EquipmentItem>();
                foreach (var item in EquipmentCargo) if (!EquipmentInventory.Instance.Add(item)) overflow.Add(item);
                if (overflow.Count > 0) EquipmentLoot.Drop(Position, overflow);
                EquipmentCargo.Clear();
            }
            else if (EquipmentCargo.Count > 0)
            {
                EquipmentLoot.Drop(landing, EquipmentCargo);
                EquipmentCargo.Clear();
            }
        }

        public bool TryCollectRewards()
        {
            if (Site != null && Site.GetComponent<BossStronghold>() != null) return BossRewardTransfer.TryCollect(this);
            if (State != ExpeditionState.Deployed || Site == null || !Site.Cleared || Site.RewardsClaimed
                || Site.Kind == ExpeditionSiteKind.ResourceSite || Site.Kind == ExpeditionSiteKind.TradePost
                || !DiplomacyManager.Hostile(Site)) return false;
            var ready = false;
            foreach (var c in crew) if (c != null && c.CanReceiveOrders && (c.Position - Position).sqrMagnitude <= 64) ready = true;
            if (!ready) return false;
            Site.RewardsClaimed = true;
            if (Site.Kind == ExpeditionSiteKind.BossNest) BlueprintCargo = true;
            var count = Site.Kind == ExpeditionSiteKind.BossNest ? 1 : Random.Range(1, 3);
            var overflow = new List<EquipmentItem>();
            for (var i = 0; i < count; i++)
            {
                var item = EquipmentItem.Random(Site.Difficulty);
                if (EquipmentInventory.Instance == null || EquipmentInventory.Instance.Items.Count + EquipmentCargo.Count >= EquipmentInventory.Capacity) overflow.Add(item);
                else EquipmentCargo.Add(item);
            }
            if (overflow.Count > 0) EquipmentLoot.Drop(Position, overflow);
            return true;
        }

        protected override void OnDisable()
        {
            ClearCrewOrders();
            AntColony.UI.ToastManager.SetPinned("supplies:" + GetEntityId(), null);
            Route?.Stop();
            if (Application.isPlaying)
            {
                LandCrew();
                foreach (var c in crew) if (c != null) c.Transport = null;
                crew.Clear();
                if (Site != null && Site.Visitor == this) Site.Visitor = null;
            }
            base.OnDisable();
        }

        protected override void Die()
        {
            BossRewardTransfer.Lose(this); // 확정 파괴만 소실. 복원/씬 정리/정상 해산은 OnDisable만 거친다.
            base.Die();
        }
    }
}
