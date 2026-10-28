using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;

namespace AntColony.Buildings
{
    public sealed partial class Processor : PowerGrid.IUser
    {
        public bool Chemical => Data != null && Data.kind == BuildingKind.ChemistryBench;
        public bool Industrial => Data != null && Data.kind is BuildingKind.GasSmelter or BuildingKind.ChemistryBench;
        public int BufferedOil => state.bufferedOil;
        private int OilRequired(Recipe recipe) => recipe?.output == ResourceType.Plastic ? IndustrialRecipes.PlasticOil : 0;
        private bool HasIndustrialInputFor(Recipe recipe) => Chemical ? OilRequired(recipe) <= state.bufferedOil
            || FluidNetwork.Has(this, ResourceType.CrudeOil, IndustrialRecipes.PlasticOil - state.bufferedOil) : FluidNetwork.Has(this, ResourceType.NaturalGas, 1);
        private bool HasIndustrialInput => HasIndustrialInputFor(Current ?? (PendingOrder is Order order ? Recipes[order.recipe]
            : state.orders.Where(o => o.recipe >= 0 && o.recipe < Recipes.Count && Wants(o, Recipes[o.recipe])).Select(o => Recipes[o.recipe]).FirstOrDefault()));
        public bool Powered { get; set; }
        public float Demand => Industrial && isActiveAndEnabled && !IsDead ? SupplyFacility.RefineryWatts : 0;
        public IEnumerable<UnityEngine.Vector2Int> Cells => RoomSystem.Footprint(this);
        private bool HasIndustrialSpace(Recipe recipe) => recipe == null || ResourceNode.Available.Where(n => n.IsLooseCargo
            && (n.transform.position - Position).sqrMagnitude <= 16).Sum(n => n.AmountRemaining) + recipe.amount <= SupplyFacility.Capacity;
        private bool IndustrialSpace => HasIndustrialSpace(Current ?? (PendingOrder is Order order ? Recipes[order.recipe] : null));
        // gasPaid는 기존 저장 필드명을 유지하며 화학 작업대에서도 묶음 유체 선투입을 뜻한다.
        private bool IndustrialReady => !Industrial || Powered && IndustrialSpace && (state.gasPaid || HasIndustrialInput);
        protected override void OnEnable() { base.OnEnable(); if (Industrial) PowerGrid.Register(this); }
        private bool PayIndustrialFuel(Recipe recipe)
        {
            if (!Industrial) return true;
            if (!Chemical) return FluidNetwork.Take(this, ResourceType.NaturalGas, 1);
            if (OilRequired(recipe) == 0) return true;
            var need = IndustrialRecipes.PlasticOil - state.bufferedOil;
            if (need > 0 && !FluidNetwork.Take(this, ResourceType.CrudeOil, need)) return false;
            state.bufferedOil = IndustrialRecipes.PlasticOil; return true;
        }
        public string IndustrialStatus => !Powered ? "정전 · 진행 보존" : !IndustrialSpace ? "바닥 출력 공간 부족 · 운반 대기"
            : !state.gasPaid && !HasIndustrialInput ? Chemical ? $"원유 공급 필요 · 내부 보관 {state.bufferedOil} · 액체 배관/탱크 확인" : "가스 공급 필요 · 기체 배관/탱크 확인"
            : state.gasPaid ? "현재 묶음 유체 투입 완료 · 공급 단절에도 작업 가능" : Chemical ? $"원유2/묶음 · 내부 보관 {state.bufferedOil} · 전력200W" : "가스1/묶음 · 전력200W";
    }
}
