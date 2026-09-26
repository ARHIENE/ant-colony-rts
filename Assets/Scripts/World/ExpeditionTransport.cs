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
    public class ExpeditionTransport : BuildingBase
    {
        private readonly List<CommanderAnt> crew = new List<CommanderAnt>();
        private readonly Dictionary<ResourceType, int> cargo = new Dictionary<ResourceType, int>();
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
            get { foreach (var amount in cargo.Values) if (amount > 0) return true; return BlueprintCargo || EquipmentCargo.Count > 0; }
        }
        // 장수 슬롯과 일반개미 정원은 따로 센다.
        // 대형 수송: 일반개미 +50%·장수 +2 / 수송 효율: 화물 +50%
        public int Capacity => Mathf.FloorToInt((Aircraft ? GameBalance.AircraftTroops : GameBalance.VehicleTroops) * ScienceEffects.TroopCapacityMultiplier);
        public int CommanderCapacity => (Aircraft ? GameBalance.AircraftCommanders : GameBalance.VehicleCommanders) + ScienceEffects.CommanderSlotBonus;
        public int CargoCapacity => Mathf.FloorToInt((Aircraft ? GameBalance.AircraftCargo : GameBalance.VehicleCargo) * ScienceEffects.CargoMultiplier);
        public float TravelSeconds => Aircraft ? 10 : 20;
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
            get { var total = 0; foreach (var amount in cargo.Values) total += amount; return total; }
        }
        public bool CargoFull => CargoLoad >= CargoCapacity;
        protected override bool IsDepositPoint => true;

        public void Initialize(bool aircraft)
        {
            Route = new TransportRoute(this);
            Aircraft = aircraft;
            homePosition = transform.position;
            name = aircraft ? "Aircraft" : "Vehicle";
            GetComponent<Renderer>().material.color = aircraft ? Color.cyan : new Color(.85f, .7f, .3f);
        }

        public int GetCargo(ResourceType type) => cargo.TryGetValue(type, out var amount) ? amount : 0;

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
            cargo[ResourceType.Food] = Mathf.Max(0, food);
            cargo[ResourceType.Soil] = Mathf.Max(0, soil);
            cargo[ResourceType.Special] = Mathf.Max(0, special);
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
            if (Route != null && Route.IsRunning) return false;
            var settlement = State == ExpeditionState.Deployed && Site != null ? Site.Settlement : null;
            if (!isActiveAndEnabled || (State != ExpeditionState.Home && settlement == null)
                || passengers == null || passengers.Count == 0) return false;
            var unique = new HashSet<CommanderAnt>();
            var load = Load;
            var commanders = CommanderLoad;
            foreach (var c in passengers)
            {
                if (c == null || !c.isActiveAndEnabled || !unique.Add(c) || c.Transport != null
                    || c.Garrison != settlement || (settlement == null && !c.HasTroops)
                    || !c.CanChangeAllocation || c.LabUpgradeBusy
                    || Vector3.Distance(c.Position, Position) > 8) return false;
                load += c.TroopCount;
                commanders++;
            }
            if (load > Capacity || commanders > CommanderCapacity) return false;
            foreach (var c in new List<CommanderAnt>(passengers))
            {
                if (settlement != null) settlement.Remove(c);
                crew.Add(c);
                c.Transport = this;
                // 현장 재탑승은 귀환 명부로 복귀한다. 실제 탑승/숨김은 TryReturn에서 처리한다.
                if (State == ExpeditionState.Home) c.SetEmbarked(true, Position);
                else c.CommandStop();
            }
            return true;
        }

        public bool TryStation(IReadOnlyList<CommanderAnt> passengers)
        {
            if (Route != null && Route.IsRunning) return false;
            var settlement = Site != null ? Site.Settlement : null;
            if (!isActiveAndEnabled || State != ExpeditionState.Deployed || settlement == null
                || Site.Disposition != ConquestDisposition.Annexed
                || !settlement.isActiveAndEnabled || passengers == null || passengers.Count == 0) return false;
            var unique = new HashSet<CommanderAnt>();
            foreach (var c in passengers)
                if (c == null || !c.isActiveAndEnabled || !unique.Add(c) || c.Transport != this || !crew.Contains(c)
                    || c.Garrison != null || !c.HasTroops || !c.CanChangeAllocation || c.LabUpgradeBusy
                    || Vector3.Distance(c.Position, Position) > 8) return false;
            foreach (var c in new List<CommanderAnt>(passengers))
            {
                c.CommandStop();
                crew.Remove(c);
                c.Transport = null;
                settlement.Add(c);
            }
            return true;
        }

        public bool TryUnloadCrew()
        {
            if (Route != null && Route.IsRunning) return false;
            if (State != ExpeditionState.Home || crew.Count == 0) return false;
            LandCrew();
            foreach (var c in crew) if (c != null) c.Transport = null;
            crew.Clear();
            return true;
        }

        internal void DetachCommander(CommanderAnt c)
        {
            crew.Remove(c); c.Transport = null;
            Route?.Stop("장수 이탈로 자동 수송 중단");
        }

        public bool TryDepart(ExpeditionSite site)
        {
            if (Route != null && Route.IsRunning
                && (site != Route.Destination || HasCargo || Route.WaitSeconds > 0)) return false;
            var world = WorldMapManager.Instance;
            if (!isActiveAndEnabled || world == null || !world.Unlocked || State != ExpeditionState.Home
                || site == null || site.Visitor != null
                || (crew.Count == 0 && site.Disposition != ConquestDisposition.Annexed)
                || site.Disposition == ConquestDisposition.Abandoned || Load > Capacity || CommanderLoad > CommanderCapacity) return false;
            Site = site;
            site.Visitor = this;
            State = ExpeditionState.Outbound;
            foreach (var c in crew) if (c != null && c.IsColonyMember) c.OnExpeditionStarted();
            Remaining = TravelSeconds;
            return true;
        }

        public bool TryReturn()
        {
            if (!isActiveAndEnabled || State != ExpeditionState.Deployed) return false;
            foreach (var c in crew)
                if (c != null && !c.IsDead && (!c.isActiveAndEnabled || c.IsCarrying || c.IsConstructing
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
            State = ExpeditionState.Returning;
            Remaining = TravelSeconds;
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
            if (!isActiveAndEnabled || !(seconds > 0) || float.IsInfinity(seconds)
                || (State != ExpeditionState.Outbound && State != ExpeditionState.Returning)) return;
            Remaining = Mathf.Max(0, Remaining - seconds);
            if (Remaining > 0) return;
            if (State == ExpeditionState.Outbound)
            {
                transform.position = Site.Landing;
                State = ExpeditionState.Deployed;
                LandCrew();
                DiplomacyManager.Instance?.Contact(Site);
                if (Site.Kind == ExpeditionSiteKind.TradePost) AntColony.UI.GameMenuController.Instance?.TradeAt(Site);
            }
            else
            {
                transform.position = homePosition;
                State = ExpeditionState.Home;
                if (Site != null) Site.Visitor = null;
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

        public override void DepositResources(ResourceType type, int amount)
        {
            // 화물 한도를 넘는 양은 싣지 못한다. 본거지 창고가 가득 차도 실린 화물은 버리지 않고 남긴다.
            // ponytail: 한도를 넘긴 마지막 운반분의 초과량은 사라진다. 손실이 문제 되면 현장 노드로 떨군다.
            amount = Mathf.Min(amount, CargoCapacity - CargoLoad);
            if (amount <= 0) return;
            cargo[type] = GetCargo(type) + amount;
            if (State == ExpeditionState.Home) UnloadCargo();
        }

        private void UnloadCargo()
        {
            var resources = ResourceManager.Instance;
            if (resources == null) return;
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                var before = resources.GetAmount(type);
                resources.Add(type, GetCargo(type), ResourceReason.Expedition);
                cargo[type] = GetCargo(type) - (resources.GetAmount(type) - before);
            }
            if (BlueprintCargo && CampaignResearch.Instance != null) { CampaignResearch.Instance.AcquireBlueprint(); BlueprintCargo = false; }
            if (EquipmentInventory.Instance != null)
            {
                var overflow = new List<EquipmentItem>();
                foreach (var item in EquipmentCargo) if (!EquipmentInventory.Instance.Add(item)) overflow.Add(item);
                if (overflow.Count > 0) EquipmentLoot.Drop(Position, overflow);
                EquipmentCargo.Clear();
            }
        }

        public bool TryCollectRewards()
        {
            if (State != ExpeditionState.Deployed || Site == null || !Site.Cleared || Site.RewardsClaimed
                || Site.Kind == ExpeditionSiteKind.ResourceSite || Site.Kind == ExpeditionSiteKind.TradePost
                || !DiplomacyManager.Hostile(Site)) return false;
            var ready = false;
            foreach (var c in crew) if (c != null && c.CanReceiveOrders && c.HasTroops && (c.Position - Position).sqrMagnitude <= 64) ready = true;
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
            Route?.Stop();
            if (Application.isPlaying)
            {
                LandCrew();
                foreach (var c in crew) if (c != null) c.Transport = null;
                crew.Clear();
                if (Site != null) Site.Visitor = null;
            }
            base.OnDisable();
        }
    }
}
