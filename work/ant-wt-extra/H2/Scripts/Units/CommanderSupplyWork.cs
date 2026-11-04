using AntColony.Buildings;
using AntColony.Core;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        public static bool ManualWellNeedsWork(SupplyFacility well) => well != null && well.Operational
            && well.Kind == AntColony.Data.BuildingKind.ManualWell && well.CountsTowardPlayerDefeat
            && well.NearSource(AntColony.Data.BuildingKind.WaterSource)
            && FluidNetwork.CanDeliver(well, AntColony.Data.ResourceType.Water, StartSupplyRules.ManualWaterAmount);
    }
}
