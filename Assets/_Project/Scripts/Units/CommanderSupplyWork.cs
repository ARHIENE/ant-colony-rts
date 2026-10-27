using AntColony.Buildings;
using AntColony.Core;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        internal bool AtDrill(SupplyFacility drill) => ServiceTarget == drill && HasReachedDestination();
        public static bool DeepDrillNeedsWork(SupplyFacility drill) => drill != null && UndergroundProduction.IsSolidDrill(drill.Kind) && UndergroundProduction.Ready(drill);
        public static bool ManualWellNeedsWork(SupplyFacility well) => well != null && well.Operational
            && well.Kind == AntColony.Data.BuildingKind.ManualWell && well.CountsTowardPlayerDefeat
            && well.NearSource(AntColony.Data.BuildingKind.WaterSource)
            && FluidNetwork.CanDeliver(well, AntColony.Data.ResourceType.Water, StartSupplyRules.ManualWaterAmount);
    }
}
