using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed partial class SupplyFacility : BuildingBase, PowerGrid.IUser
    {
        [Serializable] public sealed class State
        {
            public float work;
            public int fluid = -1, amount;
            public bool depositPresent;
            public int depositRemaining;
            public int containerMode, containerOrders;
            public bool containerPaused;
            public List<ProductionInputs.Stock> containerStock = new List<ProductionInputs.Stock>();
        }
        public const int Capacity = 100;
        // 펌프·시추·정유의 처리량과 펌프 전력은 G46 미정이므로 잠정값.
        public const float DrillWatts = 100, RefineryWatts = 200, PumpWatts = 100, ProcessSeconds = 5;
        public static readonly List<SupplyFacility> All = new List<SupplyFacility>();
        public State Supply { get; private set; } = new State();
        public BuildingKind Kind => Data.kind;
        public bool IsNaturalSource => Kind == BuildingKind.WaterSource || Kind == BuildingKind.OilDeposit;
        public static bool IsKind(BuildingKind kind) => SupplyBuildingTemplates.IsKind(kind)
            && kind != BuildingKind.PlanterBed && kind != BuildingKind.StarterMushroomBed && kind != BuildingKind.SteamGenerator && kind != BuildingKind.GasGenerator && !Processor.IsKind(kind);
        public bool Powered { get; set; }
        public IEnumerable<Vector2Int> Cells => RoomSystem.Footprint(this);
        public float Demand => UndergroundProduction.IsExtractor(Kind) ? DrillWatts : Kind == BuildingKind.OilRefinery ? RefineryWatts
            : Kind == BuildingKind.WaterPump ? PumpWatts : 0;
        public bool Operational => isActiveAndEnabled && !IsDead;
        protected override void OnEnable() { base.OnEnable(); All.Add(this); InitializeDeposit(); if (Demand > 0) PowerGrid.Register(this); }
        protected override void OnDisable() { All.Remove(this); PowerGrid.Unregister(this); Powered = false; base.OnDisable(); }
        private void Update()
        {
            if (!AntColony.Save.SaveSystem.Busy && GameSession.Exists && GameSession.Instance.GameStarted) Tick(Time.deltaTime);
        }
        public void RestoreSupply(State value) { Supply = value ?? new State(); InitializeDeposit(); }
        public static bool Valid(State s) => s != null && !float.IsNaN(s.work) && !float.IsInfinity(s.work)
            && s.work >= 0 && s.work < ProcessSeconds && s.amount >= 0 && s.amount <= Capacity
            && (!s.depositPresent || s.depositRemaining >= 0 && s.depositRemaining <= 600)
            && (s.fluid == -1 && s.amount == 0 || IsFluid(s.fluid));
        public static bool IsFluid(int value) => value == (int)AntColony.Data.ResourceType.Water
            || value == (int)AntColony.Data.ResourceType.CrudeOil || value == (int)AntColony.Data.ResourceType.Petroleum || value == (int)AntColony.Data.ResourceType.NaturalGas;
        public bool NearSource(BuildingKind source) => All.Any(n => n != this && n.Operational && n.Kind == source && (n.Position - Position).sqrMagnitude <= 16);
        public bool CanHold(AntColony.Data.ResourceType fluid, int amount) => AcceptsFluid(fluid) && amount > 0 && Supply.amount <= Capacity - amount
            && (Supply.amount == 0 || Supply.fluid == (int)fluid);
        public bool Fill(AntColony.Data.ResourceType fluid, int amount)
        {
            if (!Operational || !IsFluid((int)fluid) || !CanHold(fluid, amount)) return false;
            Supply.fluid = (int)fluid; Supply.amount += amount; return true;
        }
        internal void Drain(int amount) { Supply.amount -= amount; if (Supply.amount == 0) Supply.fluid = -1; }

        // 현장 장수의 작업 틱에서만 호출한다. 금지된 CommanderWork 연결은 인계 문서에 명시.
        public bool WorkManual(float seconds)
        {
            if (!Operational || Kind != BuildingKind.ManualWell || !NearSource(BuildingKind.WaterSource)
                || !(seconds > 0) || float.IsInfinity(seconds)) return false;
            Supply.work += seconds;
            while (Supply.work >= StartSupplyRules.ManualWaterSeconds)
            {
                Supply.work -= StartSupplyRules.ManualWaterSeconds;
                if (!FluidNetwork.Deliver(this, AntColony.Data.ResourceType.Water, StartSupplyRules.ManualWaterAmount))
                    DiplomacyManager.DropFloor(AntColony.Data.ResourceType.Water, StartSupplyRules.ManualWaterAmount, Position + Vector3.right);
            }
            return true;
        }
        public void Tick(float seconds)
        {
            if (!Operational || !Powered || !(seconds > 0) || float.IsInfinity(seconds)) return;
            if (Time.timeScale <= 0) return;
            if (Kind == BuildingKind.OilDrill || Kind == BuildingKind.GasExtractor) { TickExtraction(seconds); return; }
            if (Kind != BuildingKind.WaterPump || !NearSource(BuildingKind.WaterSource)) return;
            var output = AntColony.Data.ResourceType.Water;
            var batch = 10;
            while (seconds > 0)
            {
                if (!FluidNetwork.CanDeliver(this, output, batch)) return;
                var dt = Mathf.Min(seconds, ProcessSeconds - Supply.work); Supply.work += dt; seconds -= dt;
                if (Supply.work < ProcessSeconds) break;
                Supply.work = 0;
                FluidNetwork.Deliver(this, output, batch);
            }
        }
        public bool Unload(AntColony.Data.ResourceType fluid, int amount)
        {
            if (!Operational || fluid != AntColony.Data.ResourceType.Water || amount <= 0 || Supply.fluid != (int)fluid || Supply.amount < amount) return false;
            Drain(amount); DiplomacyManager.DropFloor(fluid, amount, Position + Vector3.right); return true;
        }
        // 용기는 첫 시추·정유에 필요 없으며 망 밖으로 연료를 반출할 때만 소비한다.
        public bool FillFuelDrum()
        {
            return Kind == BuildingKind.FuelCharger && SetContainerMode(0) && AddContainerOrder(1);
        }
        public void EjectStored()
        {
            if (Kind == BuildingKind.FuelCharger) CancelContainerWork();
            if (Supply.amount <= 0 || !IsFluid(Supply.fluid) || GasNetwork) return; // 기체 유출 규칙 확정 전 바닥 고체로 바꾸지 않는다.
            var fluid = (AntColony.Data.ResourceType)Supply.fluid; var amount = Supply.amount;
            Drain(amount); DiplomacyManager.DropFloor(fluid, amount, Position + Vector3.right);
        }
    }
}
