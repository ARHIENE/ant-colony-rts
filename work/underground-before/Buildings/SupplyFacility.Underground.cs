using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed partial class SupplyFacility
    {
        public bool WorkDrill(CommanderAnt worker, float seconds)
        {
            if (Kind != BuildingKind.DeepDrill || !UndergroundProduction.Ready(this) || worker == null
                || worker.ServiceTarget != this || worker.ServiceJob != CommanderJobs.Gathering || !worker.CivilianWorkReady
                || !worker.AllowsJob(CommanderJobs.Gathering) || worker.IsEating || worker.IsWashing || worker.IsPlaying
                || worker.IsInCombat || (worker.Position - Position).sqrMagnitude > 49 || !worker.HasReachedDestination()
                || !(seconds > 0) || float.IsInfinity(seconds) || Time.timeScale <= 0) return false;
            var work = seconds * worker.WorkRate(CommanderActivity.Gathering);
            while (work > 0 && UndergroundProduction.Ready(this))
            {
                var dt = Mathf.Min(work, ProcessSeconds - Supply.work); Supply.work += dt; work -= dt;
                if (Supply.work < ProcessSeconds) break;
                var type = (ResourceType)UndergroundProduction.LayerAt(Kind, Position).type;
                var amount = UndergroundProduction.Take(this, UndergroundProduction.Batch(this));
                if (amount <= 0) break;
                Supply.work = 0; DiplomacyManager.DropFloor(type, amount, Position + Vector3.right);
            }
            worker.GainExperience(CommanderActivity.Gathering, seconds); return true;
        }
        private void TickExtraction(float seconds)
        {
            while (seconds > 0 && UndergroundProduction.Ready(this))
            {
                var dt = Mathf.Min(seconds, ProcessSeconds - Supply.work); Supply.work += dt; seconds -= dt;
                if (Supply.work < ProcessSeconds) break;
                var type = (ResourceType)UndergroundProduction.LayerAt(Kind, Position).type;
                var amount = UndergroundProduction.Take(this, UndergroundProduction.Batch(this));
                if (amount <= 0) break;
                Supply.work = 0;
                FluidNetwork.Deliver(this, type, amount); // 공간을 확인한 같은 틱 안에서만 잔량 차감·입고.
                if (UndergroundSurvey.Instance.MapAt(Position) == 0) CampaignResearch.Instance.RecordSampleDelivery(type, amount);
            }
        }
        public bool GasNetwork => Kind is BuildingKind.GasExtractor or BuildingKind.GasPipe or BuildingKind.GasTank;
        public bool AcceptsFluid(ResourceType type) => IsFluid((int)type) && (GasNetwork == (type == ResourceType.NaturalGas));
        public static bool ValidForKind(BuildingKind kind, State s) => Valid(s) && (s.amount == 0
            || (kind is BuildingKind.GasExtractor or BuildingKind.GasPipe or BuildingKind.GasTank) == (s.fluid == (int)ResourceType.NaturalGas));
    }
}
