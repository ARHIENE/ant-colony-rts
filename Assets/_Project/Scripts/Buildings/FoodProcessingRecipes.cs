using System.Linq;
using AntColony.Data;
using AntColony.Core;
namespace AntColony.Buildings
{
    public static class FoodProcessingRecipes
    {
        // 배합·작업량은 기존 가공대 기준을 재사용한 잠정값. 발효는 작업자가 상주하지 않는 게임1일.
        public static float FermentSeconds => GameCalendar.SecondsPerDay;
        public static ResourceType? CropResource(FarmCrop crop) => crop switch {
            FarmCrop.Wheat => ResourceType.Wheat, FarmCrop.Barley => ResourceType.Barley, FarmCrop.Rice => ResourceType.Rice,
            FarmCrop.Grapes => ResourceType.Grapes, FarmCrop.Sunflower => ResourceType.SunflowerSeed, _ => null };
        public static Processor.Recipe[] For(BuildingKind kind) => kind switch
        {
            BuildingKind.Mill => new[] { new Processor.Recipe("밀가루", ResourceType.WheatFlour, 1, 8, null, (ResourceType.Wheat, 2)) },
            BuildingKind.OilPress => new[] { new Processor.Recipe("해바라기유", ResourceType.SunflowerOil, 1, 10, null, (ResourceType.SunflowerSeed, 3)) },
            BuildingKind.BrewingBarrel => new[] {
                Brew("맥주(밀)", ResourceType.Beer, ResourceType.Wheat), Brew("맥주(보리)", ResourceType.Beer, ResourceType.Barley),
                Brew("쌀술", ResourceType.RiceWine, ResourceType.Rice), Brew("과실주", ResourceType.FruitWine, ResourceType.Grapes),
                Brew("벌꿀주", ResourceType.Mead, ResourceType.Honey), Brew("감로주", ResourceType.HoneydewWine, ResourceType.Honeydew) },
            BuildingKind.Still => IndustrialRecipes.SolidFuelAlternatives(new[] { ResourceType.Beer, ResourceType.RiceWine, ResourceType.FruitWine, ResourceType.Mead, ResourceType.HoneydewWine }
                .Select(t => new Processor.Recipe("증류주 (" + t.DisplayName() + ")", ResourceType.Spirits, 1, 14, null, (t, 2), (ResourceType.Coal, 1))).ToArray()),
            _ => System.Array.Empty<Processor.Recipe>()
        };
        private static Processor.Recipe Brew(string name, ResourceType output, ResourceType input) => new Processor.Recipe(name, output, 2, 2, null, (input, 3));
    }
}
