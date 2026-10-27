using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed partial class SupplyFacility
    {
        public bool NeedsRefiner => Operational && CountsTowardPlayerDefeat && Kind == BuildingKind.OilRefinery && Powered
            && FluidNetwork.Has(this, ResourceType.CrudeOil, 1) && FluidNetwork.CanDeliver(this, ResourceType.Petroleum, 1);
        public string RefineryStatus => !Powered ? "정전 · 진행 보존"
            : !FluidNetwork.Has(this, ResourceType.CrudeOil, 1) ? "원유 공급 대기"
            : !FluidNetwork.CanDeliver(this, ResourceType.Petroleum, 1) ? "정제유 출력 공간 부족"
            : AntUnitBase.Active.OfType<CommanderAnt>().Any(c => c.ServiceTarget == this) ? "제작 담당 이동/작업 중" : "제작 담당 대기";

        public bool WorkRefinery(CommanderAnt worker, float seconds)
        {
            if (!NeedsRefiner || worker == null || worker.ServiceTarget != this || worker.ServiceJob != CommanderJobs.Crafting
                || !worker.CivilianWorkReady || !worker.AllowsJob(CommanderJobs.Crafting) || worker.IsEating || worker.IsWashing
                || worker.IsPlaying || worker.IsInCombat || !worker.AtDrill(this) || (worker.Position - Position).sqrMagnitude > 49
                || !(seconds > 0) || float.IsInfinity(seconds) || Time.timeScale <= 0) return false;
            var rate = worker.WorkRate(CommanderActivity.Crafting);
            var work = seconds * rate; var spent = 0f;
            while (work > 0 && NeedsRefiner)
            {
                var dt = Mathf.Min(work, ProcessSeconds - Supply.work); Supply.work += dt; work -= dt; spent += dt / rate;
                if (Supply.work < ProcessSeconds) break;
                Supply.work = 0;
                FluidNetwork.Take(this, ResourceType.CrudeOil, 1);
                FluidNetwork.Deliver(this, ResourceType.Petroleum, 1);
            }
            worker.GainExperience(CommanderActivity.Crafting, spent); return true;
        }
    }
}
