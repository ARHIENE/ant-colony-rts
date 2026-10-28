namespace AntColony.Data
{
    public static class CookedFoods
    {
        public static readonly ResourceType[] MealTypes = { ResourceType.FineMeal, ResourceType.FriedRice, ResourceType.SimpleMeal, ResourceType.CookedRice, ResourceType.Flatbread, ResourceType.Food, ResourceType.RottenFood };
        public static int Quality(ResourceType type) => type switch
        {
            ResourceType.FineMeal or ResourceType.FriedRice => 2,
            ResourceType.SimpleMeal or ResourceType.CookedRice or ResourceType.Flatbread => 1, _ => 0
        };
        public static string Model(ResourceType type) => type switch { ResourceType.CookedRice => "Dish_Rice", ResourceType.Flatbread => "Bread_Flat", ResourceType.FriedRice => "Dish_FriedRice", _ => null };
        public static string Name(ResourceType type) => type switch { ResourceType.CookedRice => "밥", ResourceType.Flatbread => "납작빵", ResourceType.FriedRice => "볶음밥", _ => null };
    }
}
