using AntColony.Data;
using AntColony.Units;

namespace AntColony.Buildings
{
    public static class IndustrialRecipes
    {
        // 배합·작업량 미정: 기존 제련 14/20초·원료2개 기준을 재사용하는 잠정 제조법.
        public static Processor.Recipe[] GasSmelting() => new[] {
            new Processor.Recipe("티타늄", ResourceType.Titanium, 1, 14, CommanderActivity.Strength, (ResourceType.TitaniumOre, 2)),
            new Processor.Recipe("티타늄 합금", ResourceType.TitaniumAlloy, 2, 20, CommanderActivity.Strength,
                (ResourceType.Titanium, 2), (ResourceType.Steel, 1)) };
    }
}
