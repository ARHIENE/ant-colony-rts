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
        private Vector3 homePosition;
        public IReadOnlyList<CommanderAnt> Crew => crew;
        public ExpeditionState State { get; private set; }
        public ExpeditionSite Site { get; private set; }
        public bool Aircraft { get; private set; }
        // 장수 자신도 적재량 1을 차지한다.
        public int Capacity => Aircraft ? 100 : 40;
        public float TravelSeconds => Aircraft ? 10 : 20;
        public float Remaining { get; private set; }
        public int Load
        {
            get { var total = 0; foreach (var c in crew) if (c != null) total += c.TroopCount + 1; return total; }
        }
        protected override bool IsDepositPoint => true;

        public void Initialize(bool aircraft)
        {
            Aircraft = aircraft;
            homePosition = transform.position;
            name = aircraft ? "Aircraft" : "Vehicle";
            GetComponent<Renderer>().material.color = aircraft ? Color.cyan : new Color(.85f, .7f, .3f);
        }

        public int GetCargo(ResourceType type) => cargo.TryGetValue(type, out var amount) ? amount : 0;

        public bool TryBoard(IReadOnlyList<CommanderAnt> passengers)
        {
            if (!isActiveAndEnabled || State != ExpeditionState.Home || passengers == null || passengers.Count == 0) return false;
            var unique = new HashSet<CommanderAnt>();
            var load = Load;
            foreach (var c in passengers)
            {
                if (c == null || !c.isActiveAndEnabled || !unique.Add(c) || c.Transport != null
                    || !c.HasTroops || !c.CanChangeAllocation || c.LabUpgradeBusy
                    || Vector3.Distance(c.Position, Position) > 8) return false;
                load += c.TroopCount + 1;
            }
            if (load > Capacity) return false;
            foreach (var c in passengers)
            {
                crew.Add(c);
                c.Transport = this;
                c.SetEmbarked(true, Position);
            }
            return true;
        }

        public bool TryUnloadCrew()
        {
            if (State != ExpeditionState.Home || crew.Count == 0) return false;
            LandCrew();
            foreach (var c in crew) if (c != null) c.Transport = null;
            crew.Clear();
            return true;
        }

        public bool TryDepart(ExpeditionSite site)
        {
            var world = WorldMapManager.Instance;
            if (!isActiveAndEnabled || world == null || !world.Unlocked || State != ExpeditionState.Home
                || crew.Count == 0 || site == null || site.Visitor != null || Load > Capacity) return false;
            Site = site;
            site.Visitor = this;
            State = ExpeditionState.Outbound;
            Remaining = TravelSeconds;
            return true;
        }

        public bool TryReturn()
        {
            if (!isActiveAndEnabled || State != ExpeditionState.Deployed) return false;
            foreach (var c in crew)
                if (c != null && (!c.isActiveAndEnabled || c.IsCarrying || c.IsConstructing
                    || Vector3.Distance(c.Position, Position) > 8)) return false;
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
                if (crew[i] == null) continue;
                var candidate = Position + new Vector3(3 + i % 4, 0, i / 4 * 1.5f);
                if (NavMesh.SamplePosition(candidate, out var hit, 6, NavMesh.AllAreas)) candidate = hit.position;
                crew[i].SetEmbarked(false, candidate);
            }
        }

        public override void DepositResources(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            // 본거지 창고가 가득 차도 수송 화물은 버리지 않고 남긴다.
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
                resources.Add(type, GetCargo(type));
                cargo[type] = GetCargo(type) - (resources.GetAmount(type) - before);
            }
        }

        protected override void OnDisable()
        {
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
