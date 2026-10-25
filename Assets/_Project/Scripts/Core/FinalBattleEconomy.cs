using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Core
{
    public readonly struct ReserveRequirement
    {
        public readonly AntColony.Data.ResourceType Resource;
        public readonly int Amount;
        public ReserveRequirement(AntColony.Data.ResourceType resource, int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Resource = resource; Amount = amount;
        }
    }

    public static class FinalBattleEconomy
    {
        public const int DefenseDays = 15, ReserveDays = 20;
        public static float DefenseSeconds => DefenseDays * GameCalendar.SecondsPerDay;
        public static int RecommendedFood(float dailyFood) => Mathf.CeilToInt(Mathf.Max(0, dailyFood) * ReserveDays);
        public static float HomeDailyFood => CommanderRoster.Instance == null ? 0 : CommanderRoster.Instance.Commanders
            .Where(c => c != null && c.isActiveAndEnabled && c.IsColonyMember && !c.IsDead && !c.IsAwayFromHome)
            .Sum(AntColony.World.ExpeditionRules.FoodPerDay);
        public static int FoodOnHand => (ResourceManager.Instance == null ? 0 : new[] {
                AntColony.Data.ResourceType.Food, AntColony.Data.ResourceType.SimpleMeal, AntColony.Data.ResourceType.FineMeal }
                .Sum(ResourceManager.Instance.GetAmount))
            + (CommanderRoster.Instance == null ? 0 : CommanderRoster.Instance.Commanders
                .Where(c => c != null && c.isActiveAndEnabled && c.IsColonyMember && !c.IsDead && !c.IsAwayFromHome)
                .Sum(c => c.PersonalState.carriedRations?.Total ?? 0));

        public static bool IsMajorDefense(BuildingKind kind) => kind == BuildingKind.AcidTower || kind == BuildingKind.AreaAcidTower
            || kind == BuildingKind.SoilWall || kind == BuildingKind.CastleWall || kind == BuildingKind.Gate
            || kind == BuildingKind.TrapPit || kind == BuildingKind.SpikeTrap || kind == BuildingKind.MineField || kind == BuildingKind.Watchtower;

        // 가동 담당자가 연료·탄약의 실제 필요량을 넘긴다. null은 미연결, 빈 배열은 필요량 0을 확인했다는 뜻이다.
        public static string[] ReserveLines(IEnumerable<ReserveRequirement> fuelAndAmmo = null)
        {
            var lines = new List<string> {
                $"최종 방어 {DefenseDays}일 · {DefenseSeconds / 60f:0.#}분 · 납세 {DefenseDays}회",
                $"장수·동원 병력 {ReserveDays}일분 식량: {FoodOnHand} / 권장 {RecommendedFood(HomeDailyFood)}"
                    + (FoodOnHand < RecommendedFood(HomeDailyFood) ? "  부족" : "  충족"),
                "세금은 미동원 성인·노년의 시간별 과세 생산 × 세율로 매일 정산합니다. 인원 변동은 이후 시간에만 반영됩니다." };
            var costs = new Dictionary<AntColony.Data.ResourceType, float>();
            void Add(AntColony.Data.ResourceType type, float amount) { costs.TryGetValue(type, out var old); costs[type] = old + amount; }
            foreach (var b in AntColony.Core.SceneObjects.All<BuildingBase>())
            {
                if (!b.isActiveAndEnabled || b.IsDead || b.Data == null || !IsMajorDefense(b.Data.kind)) continue;
                Add(AntColony.Data.ResourceType.Food, b.Data.foodCost * .3f);
                Add(b.MainMaterial, b.Data.soilCost * .3f);
                Add(AntColony.Data.ResourceType.Special, b.Data.specialCost * .3f);
            }
            lines.Add("주요 방어시설 재건비 30%를 자원별로 비축합니다.");
            if (fuelAndAmmo == null) lines.Add("연료·탄약 필요량: 가동 화면 연결 대기 (확인 전 비축 충족 판정 불가)");
            else foreach (var need in fuelAndAmmo) Add(need.Resource, need.Amount);
            foreach (var cost in costs.Where(c => c.Value > 0))
            {
                var required = Mathf.CeilToInt(cost.Value);
                // 식량 권장은 병력 20일분과 재건 몫을 합산한다.
                if (cost.Key == AntColony.Data.ResourceType.Food) required += RecommendedFood(HomeDailyFood);
                var available = cost.Key == AntColony.Data.ResourceType.Food ? FoodOnHand : (ResourceManager.Instance?.GetAmount(cost.Key) ?? 0)
                    + AntColony.Core.SceneObjects.All<AirshipYard>()
                        .Where(y => !y.IsDead && y.Phase == RocketPhase.Construction).Sum(y => y.LoadedFuel(cost.Key));
                lines.Add($"{cost.Key.DisplayName()}: {available} / 권장 합계 {required}" + (available < required ? "  부족" : "  충족"));
            }
            return lines.ToArray();
        }
    }
}
