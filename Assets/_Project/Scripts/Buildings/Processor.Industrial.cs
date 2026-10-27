using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;

namespace AntColony.Buildings
{
    public sealed partial class Processor : PowerGrid.IUser
    {
        public bool Industrial => Data != null && Data.kind == BuildingKind.GasSmelter;
        public bool Powered { get; set; }
        public float Demand => Industrial && isActiveAndEnabled && !IsDead ? SupplyFacility.RefineryWatts : 0;
        public IEnumerable<UnityEngine.Vector2Int> Cells => RoomSystem.Footprint(this);
        private bool HasIndustrialSpace(Recipe recipe) => recipe == null || ResourceNode.Available.Where(n => n.IsLooseCargo
            && (n.transform.position - Position).sqrMagnitude <= 16).Sum(n => n.AmountRemaining) + recipe.amount <= SupplyFacility.Capacity;
        private bool IndustrialSpace => HasIndustrialSpace(Current ?? (PendingOrder is Order order ? Recipes[order.recipe] : null));
        private bool IndustrialReady => !Industrial || Powered && IndustrialSpace && (state.gasPaid || FluidNetwork.Has(this, ResourceType.NaturalGas, 1));
        protected override void OnEnable() { base.OnEnable(); if (Industrial) PowerGrid.Register(this); }
        private bool PayIndustrialFuel() => !Industrial || FluidNetwork.Take(this, ResourceType.NaturalGas, 1);
        public string IndustrialStatus => !Powered ? "정전 · 진행 보존" : !IndustrialSpace ? "바닥 출력 공간 부족 · 운반 대기"
            : !state.gasPaid && !FluidNetwork.Has(this, ResourceType.NaturalGas, 1) ? "가스 공급 필요 · 기체 배관/탱크 확인"
            : state.gasPaid ? "현재 묶음 연료 투입 완료 · 공급 단절에도 작업 가능" : "가스1/묶음 · 전력200W";
    }
}
