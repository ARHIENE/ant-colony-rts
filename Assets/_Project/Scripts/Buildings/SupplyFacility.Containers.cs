using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed partial class SupplyFacility : IMaterialSink
    {
        // 기존 정제유 드럼통의 10단위와 시설 기본 5초 작업을 공통 잠정값으로 사용한다.
        public const int ContainerAmount = 10;
        private readonly ProductionInputs containerInputs = new ProductionInputs();
        private ProductionInputs ContainerInputs { get { if (containerInputs.staged != Supply.containerStock) containerInputs.Restore(Supply.containerStock); Supply.containerStock = containerInputs.staged; return containerInputs; } }
        public ResourceType ContainerFluid => Supply.containerMode % 3 == 2 ? ResourceType.NaturalGas : Supply.containerMode % 3 == 1 ? ResourceType.CrudeOil : ResourceType.Petroleum;
        public ResourceType EmptyContainer => ContainerFluid == ResourceType.NaturalGas ? ResourceType.GasCylinder : ResourceType.Drum;
        public ResourceType FilledContainer => ContainerFluid == ResourceType.NaturalGas ? ResourceType.FilledGasCylinder : ContainerFluid == ResourceType.CrudeOil ? ResourceType.FilledCrudeDrum : ResourceType.FilledFuelDrum;
        public bool UnpackingContainer => Supply.containerMode >= 3;
        public ResourceType ContainerInput => UnpackingContainer ? FilledContainer : EmptyContainer;
        private (ResourceType type, int amount)[] ContainerBill => new[] { (ContainerInput, 1) };
        public bool Paused => Supply.containerPaused || !Operational;
        public bool ContainerOrderActive => Kind == BuildingKind.FuelCharger && !Paused && Supply.containerOrders > 0;
        public bool ContainerNeedsIngredients => ContainerOrderActive && !ContainerInputs.Ready(ContainerBill);
        private bool ContainerSpace => ResourceNode.Available.Where(n => n.IsLooseCargo && (n.transform.position - Position).sqrMagnitude <= 16).Sum(n => n.AmountRemaining) + 1 <= Capacity;
        public bool NeedsContainerWorker => ContainerOrderActive && ContainerSpace && ContainerInputs.Ready(ContainerBill)
            && (UnpackingContainer ? FluidNetwork.CanDeliver(this, ContainerFluid, ContainerAmount) : FluidNetwork.Has(this, ContainerFluid, ContainerAmount));
        public string ContainerStatus => Paused ? "일시정지" : Supply.containerOrders == 0 ? "충전/배출 지시 없음"
            : ContainerNeedsIngredients ? ContainerInput.DisplayName() + " 배송 대기" : !ContainerSpace ? "바닥 출력 공간 부족 · 운반 대기"
            : !NeedsContainerWorker ? UnpackingContainer ? "배관/탱크 공간 부족" : ContainerFluid.DisplayName() + " 공급 대기"
            : AntUnitBase.Active.OfType<CommanderAnt>().Any(c => c.ServiceTarget == this && c.AtDrill(this)) ? "용기 " + (UnpackingContainer ? "배출" : "충전") + " 작업 중"
            : AntUnitBase.Active.OfType<CommanderAnt>().Any(c => c.ServiceTarget == this) ? "제작 담당 이동 중" : "제작 담당 대기";
        public bool SetContainerMode(int mode)
        {
            if (Kind != BuildingKind.FuelCharger || mode < 0 || mode > 5 || Supply.amount > 0 && (mode % 3 == 2) != (Supply.fluid == (int)ResourceType.NaturalGas)) return false;
            if (Supply.containerMode == mode) return true;
            CancelContainerWork(); Supply.containerMode = mode; return true;
        }
        public bool AddContainerOrder(int amount)
        {
            if (!Operational || Kind != BuildingKind.FuelCharger || amount <= 0 || amount > 10000 - Supply.containerOrders) return false;
            Supply.containerOrders += amount; return true;
        }
        public void PauseContainers(bool paused) { Supply.containerPaused = paused; }
        public void CancelContainerWork()
        {
            ContainerInputs.Drop(Position); Supply.containerOrders = 0; Supply.work = 0;
            foreach (var c in AntUnitBase.Active.OfType<CommanderAnt>().Where(c => c.ServiceTarget == this).ToArray()) c.CommandStop();
        }
        public bool NextPickup(out ResourceType type, out int amount)
        { type = default; amount = 0; return ContainerOrderActive && ContainerInputs.Next(ContainerBill, out type, out amount); }
        public bool BeginPickup(object courier, ResourceType type, int amount) => ContainerOrderActive && ContainerInputs.Reserve(courier, type, amount, ContainerBill);
        public int Deliver(object courier, ResourceType type, int amount) => ContainerOrderActive ? ContainerInputs.Deliver(courier, type, amount, ContainerBill) : 0;
        public void AbortPickup(object courier) => ContainerInputs.Abort(courier);
        protected override void Die() { if (Kind == BuildingKind.FuelCharger) CancelContainerWork(); base.Die(); }
        public bool WorkContainers(CommanderAnt worker, float seconds)
        {
            if (!NeedsContainerWorker || worker == null || worker.ServiceTarget != this || worker.ServiceJob != CommanderJobs.Crafting
                || !worker.CivilianWorkReady || !worker.AllowsJob(CommanderJobs.Crafting) || worker.IsEating || worker.IsWashing || worker.IsPlaying
                || worker.IsInCombat || !worker.AtDrill(this) || (worker.Position - Position).sqrMagnitude > 49
                || !(seconds > 0) || float.IsInfinity(seconds) || Time.timeScale <= 0) return false;
            var rate = worker.WorkRate(CommanderActivity.Crafting);
            var dt = Mathf.Min(seconds * rate, ProcessSeconds - Supply.work); Supply.work += dt;
            worker.GainExperience(CommanderActivity.Crafting, dt / rate);
            if (Supply.work < ProcessSeconds) return true;
            // 입력과 출력은 같은 틱에 교환한다. 진행 중 정지·취소는 아직 소비하지 않은 용기만 돌려준다.
            if (UnpackingContainer) FluidNetwork.Deliver(this, ContainerFluid, ContainerAmount);
            else FluidNetwork.Take(this, ContainerFluid, ContainerAmount);
            ContainerInputs.Consume(ContainerBill); Supply.work = 0; Supply.containerOrders--;
            DiplomacyManager.DropFloor(UnpackingContainer ? EmptyContainer : FilledContainer, 1, Position + Vector3.right);
            return true;
        }
        private static bool ValidContainers(BuildingKind kind, State s)
        {
            if (s.containerMode < 0 || s.containerMode > 5 || s.containerOrders < 0 || s.containerOrders > 10000) return false;
            var input = s.containerMode >= 3 ? s.containerMode % 3 == 2 ? ResourceType.FilledGasCylinder : s.containerMode % 3 == 1 ? ResourceType.FilledCrudeDrum : ResourceType.FilledFuelDrum
                : s.containerMode % 3 == 2 ? ResourceType.GasCylinder : ResourceType.Drum;
            return (kind == BuildingKind.FuelCharger || s.containerMode == 0 && s.containerOrders == 0 && !s.containerPaused)
                && (s.containerStock == null || s.containerStock.Count <= 1 && s.containerStock.All(v => kind == BuildingKind.FuelCharger && v != null && v.type == input && v.amount == 1));
        }
    }
}
