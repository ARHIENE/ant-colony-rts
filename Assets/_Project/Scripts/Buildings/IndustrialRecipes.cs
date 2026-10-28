using AntColony.Data;
using System.Linq;
using AntColony.Units;

namespace AntColony.Buildings
{
    public static class IndustrialRecipes
    {
        // 원유 배합은 미정: 기존 가공의 원료2/작업14 기준을 재사용한다. 원유는 배관에서 받는다.
        public const int PlasticOil = 2;
        // 기존 가마·가공대의 원료2/작업량 기준을 재사용한 잠정 배합.
        public static Processor.Recipe[] Carbonization(BuildingKind kind) => kind == BuildingKind.CharcoalKiln ? new[] {
            new Processor.Recipe("숯", ResourceType.Charcoal, 1, 12, null, (ResourceType.Wood, 2)) } : new[] {
            new Processor.Recipe("코크스", ResourceType.Coke, 1, 14, null, (ResourceType.Coal, 2)) };
        public static Processor.Recipe[] Chemistry() => new[] {
            new Processor.Recipe("플라스틱", ResourceType.Plastic, 1, 14, null),
            new Processor.Recipe("고무", ResourceType.Rubber, 1, 12, null, (ResourceType.RubberSap, 2)) };
        public static Processor.Recipe[] SolidFuelAlternatives(Processor.Recipe[] recipes) => recipes.Concat(
            recipes.Where(r => r.inputs.Any(i => i.type == ResourceType.Coal)).SelectMany(r => new[] { ResourceType.Charcoal, ResourceType.Coke }
                .Select(fuel => new Processor.Recipe(r.name + " (" + fuel.DisplayName() + ")", r.output, r.amount, r.work, r.support,
                    r.inputs.Select(i => (i.type == ResourceType.Coal ? fuel : i.type, i.amount)).ToArray())))).ToArray();
        // 배합·작업량 미정: 기존 제련 14/20초·원료2개 기준을 재사용하는 잠정 제조법.
        public static Processor.Recipe[] GasSmelting() => new[] {
            new Processor.Recipe("티타늄", ResourceType.Titanium, 1, 14, CommanderActivity.Strength, (ResourceType.TitaniumOre, 2)),
            new Processor.Recipe("티타늄 합금", ResourceType.TitaniumAlloy, 2, 20, CommanderActivity.Strength,
                (ResourceType.Titanium, 2), (ResourceType.Steel, 1)) };
    }
}
