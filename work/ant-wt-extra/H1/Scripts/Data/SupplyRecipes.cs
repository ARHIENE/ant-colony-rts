using AntColony.Buildings;

namespace AntColony.Data
{
    public static class SupplyRecipes
    {
        // 청동 비율·작업량은 기존 제련 레시피에 맞춘 잠정값. 용기 강철 비용은 G46 확정값.
        public static Processor.Recipe[] For(BuildingKind kind) => kind == BuildingKind.Smelter ? new[]
        {
            new Processor.Recipe("청동", AntColony.Data.ResourceType.Bronze, 2, 14, null,
                (AntColony.Data.ResourceType.CopperOre, 2), (AntColony.Data.ResourceType.TinOre, 1), (AntColony.Data.ResourceType.Coal, 1)),
            new Processor.Recipe("드럼통", AntColony.Data.ResourceType.Drum, 1, 14, null, (AntColony.Data.ResourceType.Steel, 5)),
            new Processor.Recipe("가스 용기", AntColony.Data.ResourceType.GasCylinder, 1, 14, null, (AntColony.Data.ResourceType.Steel, 10))
        } : System.Array.Empty<Processor.Recipe>();
    }
}
