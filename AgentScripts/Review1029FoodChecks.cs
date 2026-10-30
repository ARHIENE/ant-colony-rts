using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Review1029FoodChecks
{
    public static async Task<string> Main()
    {
        await Task.CompletedTask;
        var count = 0; var report = new StringBuilder();
        void Check(bool ok, string why) { if (!ok) throw new Exception(why); count++; }
        var simple = AntColony.Buildings.Kitchen.Recipes.Single(r => r.output == AntColony.Data.ResourceType.SimpleMeal);
        var fine = AntColony.Buildings.Kitchen.Recipes.Single(r => r.output == AntColony.Data.ResourceType.FineMeal);
        Check(fine.food == simple.food * 2 && fine.seconds == simple.seconds * 2, "25 좋은 요리는 식량·노동 두 배");
        Check(AntColony.Core.GameBalance.MealMood[2] == 5 && AntColony.Core.GameBalance.MealMood[1] == 0, "25 기분 차이5");
        var state = new AntColony.Units.CommanderPersonalState();
        state.AddMood("식사: 좋은 식사", 5, 75); state.AddMood("식사: 좋은 식사", 5, 75);
        Check(state.moodFactors.Count(f => f.reason == "식사: 좋은 식사") == 1, "25 같은 식사 기분 무한 중첩 없음");
        var day = AntColony.Core.GameCalendar.SecondsPerDay;
        Check(AntColony.Core.FoodLots.ShelfLife(AntColony.Data.ResourceType.FineMeal) == 3 * day, "완성 좋은 요리3일");
        Check(AntColony.Core.FoodLots.ShelfLife(AntColony.Core.FoodPreservation.Cooked)
            == AntColony.Core.FoodLots.ShelfLife(AntColony.Core.FoodPreservation.RawMeat), "23 조리 별도 보존 보너스 없음");
        var oldFood = new AntColony.Core.FoodLots(); oldFood.Add(2, 30);
        var fineFood = new AntColony.Core.FoodLots(); fineFood.Add(1, 450);
        Check(oldFood.Age(31) == 2 && fineFood.Age(31) == 0, "28 등급 우선 섭취 중 저가식 부패 위험 재현");
        var order = AntColony.Data.CookedFoods.MealTypes;
        Check(Array.IndexOf(order, AntColony.Data.ResourceType.FineMeal) < Array.IndexOf(order, AntColony.Data.ResourceType.SimpleMeal), "기존 좋은 요리 우선 유지");
        var saving = AntColony.Units.CommanderAnt.DietRaw | AntColony.Units.CommanderAnt.DietSimple;
        Check((saving & AntColony.Units.CommanderAnt.DietFine) == 0 && saving == 3, "아끼기 프리셋 기존 식단 비트만 사용");
        var lots = new AntColony.Core.FoodLots(); lots.Add(3, 100); lots.Add(2, 20);
        Check(lots.Take(2).Freshest == 20 && lots.Total == 3, "같은 종류는 오래된 물량 먼저");
        foreach (AntColony.Buildings.Species species in Enum.GetValues(typeof(AntColony.Buildings.Species)))
        {
            var info = AntColony.Buildings.SpeciesInfo.For(species);
            var feed = 100 / AntColony.Core.GameBalance.CritterHungerMonths / AntColony.Core.GameBalance.CritterHungerPerUnit;
            Check(info.directPerMonth + info.floorPerMonth > 0 && feed == 4, "26 비식량 제품과 기본 먹이 요구");
            report.AppendLine($"26 {info.name}: 무돌봄/건강성체/과밀없음 기준 월 직접{info.direct} {info.directPerMonth} + 바닥{info.floor} {info.floorPerMonth}, 먹이 {feed}, 공간 {info.space}");
        }
        var cloth = AntColony.Buildings.Processor.RecipesFor(AntColony.Data.BuildingKind.SpinningWheel);
        Check(cloth.Any(r => r.inputs.Any(i => i.type == AntColony.Data.ResourceType.Thread))
            && cloth.Any(r => r.inputs.Any(i => i.type == AntColony.Data.ResourceType.Cobweb)), "26 목축산물 실제 직물 대체 배합");
        report.AppendLine($"25 하루2끼: 간단식 식량{simple.food*2}/작업{simple.seconds*2} vs 좋은식 식량{fine.food*2}/작업{fine.seconds*2}, 기분+5/75초. 장식·오락·포상과 중첩 가능한 보완수단");
        report.AppendLine("23 건조/훈연/염장 전용 자원·제조법은 현재 미구현: 구현된 보존식 가치로 판정하지 않음");
        return $"PASS {count} 식품 계산 검사\n" + report;
    }
}
