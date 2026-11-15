using System;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;

namespace AntColony.Buildings
{
    // Renewable sites exchange actual commander work for floor cargo. No passive income.
    public static class ResourceIndustry
    {
        public static bool IsKind(BuildingKind kind) => kind is BuildingKind.Quarry or BuildingKind.LoggingCamp
            or BuildingKind.SandPit or BuildingKind.ClayPit or BuildingKind.CultivationGarden or BuildingKind.OreMine or BuildingKind.DeepMine;
        public static string Name(BuildingKind kind) => kind switch {
            BuildingKind.Quarry => "채석장", BuildingKind.LoggingCamp => "벌목장", BuildingKind.SandPit => "모래 채취장",
            BuildingKind.ClayPit => "점토 채취장", BuildingKind.CultivationGarden => "자원 재배원", BuildingKind.OreMine => "갱도 광산", BuildingKind.DeepMine => "심부 광산", _ => "" };
        public static (ResourceType type, int amount)[] Cost(BuildingKind kind) => kind switch {
            // 기초 굴착·벌목 작업장은 현장 목재 틀로 세운다. 첫 흙/돌 공급에 자기 산출물을 요구하지 않는다.
            BuildingKind.Quarry or BuildingKind.LoggingCamp => new[] { (ResourceType.Wood, 20) },
            BuildingKind.DeepMine => new[] { (ResourceType.Steel, 60), (ResourceType.Copper, 20), (ResourceType.Component, 8) },
            BuildingKind.OreMine => new[] { (ResourceType.Wood, 40), (ResourceType.Stone, 20) },
            BuildingKind.CultivationGarden => new[] { (ResourceType.Wood, 15), (ResourceType.Soil, 15) },
            _ => new[] { (ResourceType.Wood, 15), (ResourceType.Soil, 10) } };
        private static Processor.Recipe Gather(ResourceType output, int amount, float work) =>
            new Processor.Recipe(output.DisplayName(), output, amount, work, CommanderActivity.Strength);
        private static Processor.Recipe Grow(ResourceType output, int amount, float work) =>
            new Processor.Recipe(output.DisplayName(), output, amount, work, null, (ResourceType.Water, 1));
        public static Processor.Recipe[] For(BuildingKind kind) => kind switch {
            // 기존 저장의 돌 주문은 index 0을 유지한다. 흙은 자동 부산물이 아닌 별도 굴착 주문이다.
            BuildingKind.Quarry => new[] { Gather(ResourceType.Stone, 5, 30), Gather(ResourceType.Soil, 5, 25) },
            BuildingKind.LoggingCamp => new[] { Gather(ResourceType.Wood, 5, 25) },
            BuildingKind.SandPit => new[] { Gather(ResourceType.Sand, 5, 20) },
            BuildingKind.ClayPit => new[] { Gather(ResourceType.Clay, 5, 20) },
            BuildingKind.CultivationGarden => new[] { Grow(ResourceType.Herb, 2, 25), Grow(ResourceType.Fiber, 4, 25),
                Grow(ResourceType.Leaf, 4, 25), Grow(ResourceType.Resin, 2, 35), Grow(ResourceType.PlantingMaterial, 1, 40), Grow(ResourceType.RubberSap, 2, 40) },
            BuildingKind.OreMine => new[] { Gather(ResourceType.CopperOre, 3, 35), Gather(ResourceType.TinOre, 3, 35),
                Gather(ResourceType.Coal, 3, 35), Gather(ResourceType.IronOre, 3, 40), Gather(ResourceType.Sulfur, 2, 40) },
            BuildingKind.DeepMine => new[] { Gather(ResourceType.TitaniumOre, 3, 90), Gather(ResourceType.StellarOre, 1, 120) },
            _ => Array.Empty<Processor.Recipe>() };
        public static bool Unlocked(ResourceType type) => (!ResourceStudyRules.RequiresResearch(type)
            || CampaignResearch.Instance?.ResourceResearched(type) == true)
            && (type != ResourceType.StellarOre || ScienceEffects.Has(ScienceTechnology.StellarExtraction));
        public static bool CanWork(CommanderAnt worker, BuildingKind kind) => worker != null
            && (kind is not (BuildingKind.Quarry or BuildingKind.OreMine) || worker.PersonalState.toolTier >= 2);
    }
}
