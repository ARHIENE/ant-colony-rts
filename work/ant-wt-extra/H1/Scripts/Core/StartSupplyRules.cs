using AntColony.Data;

namespace AntColony.Core
{
    public static class StartSupplyRules
    {
        public const float Radius = 40, ManualWaterSeconds = 5;
        public const int ManualWaterAmount = 10, MushroomWaterPerDay = 1;
        public const float CropSeconds = GameCalendar.SecondsPerDay * 3;
        public static int DailyFood => UnityEngine.Mathf.CeilToInt(GameBalance.MealFood * 2);
        public static int StartFood(int commanders) => DailyFood * 7 * commanders;
        public static string WaterName(MapBiome biome) => biome == MapBiome.Desert ? "지하수 지점"
            : biome == MapBiome.Cave ? "용출수" : "얕은 수원";
    }
}
