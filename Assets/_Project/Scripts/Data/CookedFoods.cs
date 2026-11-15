namespace AntColony.Data
{
    public static class CookedFoods
    {
        public static readonly ResourceType[] MealTypes = { ResourceType.FineMeal, ResourceType.FriedRice, ResourceType.HoneyBread, ResourceType.SimpleMeal, ResourceType.SmokedFood, ResourceType.CookedRice, ResourceType.Flatbread, ResourceType.BarleyPorridge, ResourceType.Soup, ResourceType.Food, ResourceType.RottenFood };
        public static int Quality(ResourceType type) => type switch
        {
            ResourceType.FineMeal or ResourceType.FriedRice or ResourceType.HoneyBread => 2,
            ResourceType.SimpleMeal or ResourceType.SmokedFood or ResourceType.CookedRice or ResourceType.Flatbread or ResourceType.BarleyPorridge or ResourceType.Soup => 1, _ => 0
        };
        public static string Model(ResourceType type) => type switch { ResourceType.CookedRice => "Dish_Rice", ResourceType.Flatbread => "Bread_Flat", ResourceType.FriedRice => "Dish_FriedRice", ResourceType.BarleyPorridge => "Dish_BarleyPorridge", ResourceType.Soup => "Dish_Soup", ResourceType.HoneyBread => "Bread_Honey", _ => null };
        public static string Name(ResourceType type) => type switch { ResourceType.SmokedFood => "훈연식", ResourceType.CookedRice => "밥", ResourceType.Flatbread => "납작빵", ResourceType.FriedRice => "볶음밥", ResourceType.BarleyPorridge => "보리죽", ResourceType.Soup => "수프", ResourceType.HoneyBread => "꿀빵", _ => null };
    }
}
