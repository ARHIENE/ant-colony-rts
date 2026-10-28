using AntColony.Core;
using AntColony.Data;

namespace AntColony.Buildings
{
    public static class FarmCropRules
    {
        // 신규 작물의 성장/수확은 잠정 밸런스. 자연 성장만의 기준이며 파종·수확·가공 노동과 환경 보정은 별도다.
        public static float GrowSeconds(FarmCrop crop) => crop switch
        {
            FarmCrop.Honeydew => GameBalance.HoneydewSeconds, FarmCrop.AdvancedFungus => GameBalance.AdvancedFungusSeconds,
            FarmCrop.Wheat => 240, FarmCrop.Barley => 180, FarmCrop.Rice => 360,
            FarmCrop.Grapes => 300, FarmCrop.Sunflower => 300, _ => GameBalance.FungusSeconds
        };
        public static float Yield(FarmCrop crop) => crop switch
        {
            FarmCrop.Honeydew => GameBalance.HoneydewFood, FarmCrop.AdvancedFungus => GameBalance.AdvancedFungusFood,
            FarmCrop.Wheat => 50, FarmCrop.Barley => 32, FarmCrop.Rice => 90,
            FarmCrop.Grapes => 60, FarmCrop.Sunflower => 45, _ => GameBalance.FungusFood
        };
        public static string Role(FarmCrop crop) => crop switch
        {
            FarmCrop.Wheat => "제분→납작빵 또는 맥주 · 제분 노동 필요",
            FarmCrop.Barley => "짧은 주기의 양조 원료 · 바로 먹을 수 없음",
            FarmCrop.Rice => "긴 주기·높은 수확 · 밥/볶음밥 또는 쌀술",
            FarmCrop.Grapes => "곡물보다 짧은 보존 · 과실주 원료 · 바로 먹을 수 없음",
            FarmCrop.Sunflower => "착유→볶음밥/가공 · 씨앗은 바로 먹을 수 없음",
            FarmCrop.Honeydew => "가뭄 피해가 작음 · 감로주 원료",
            FarmCrop.AdvancedFungus => "긴 주기·고수확 · 수확 시 Special 확률",
            _ => "기본 식량 · 별도 제분/착유 없이 조리 가능"
        };
        public static string Description(FarmCrop crop)
        {
            var type = FoodProcessingRecipes.CropResource(crop) ?? ResourceType.Food;
            var shelf = (type == ResourceType.Food ? FoodLots.ShelfLife(FoodPreservation.Mushroom) : FoodLots.ShelfLife(type)) / GameCalendar.SecondsPerDay;
            return $"기준 성장 {GrowSeconds(crop) / GameCalendar.SecondsPerDay:0.#}일 · 수확 {Yield(crop):0}개\n{Role(crop)}\n상온 보존 {shelf:0.#}일 · 실제 신선도는 생산 물량 기준\n겨울 성장 정지 · 환경/연구/비료 보정 별도 · 넓은 밭 종자2개 · 수치 잠정";
        }
    }
}
