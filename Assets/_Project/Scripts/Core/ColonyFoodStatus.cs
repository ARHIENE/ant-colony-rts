using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Core
{
    // A stock estimate, not a promise that every diet/path can use every meal.
    public sealed class ColonyFoodStatus
    {
        public int residents, cooked, raw, floor, spoiling, unplanted, seedBlocked, waterBlocked, sowing, cookingBlocked;
        public float days;
        public string Summary => residents == 0 ? "본거지 식사 인원 없음" :
            $"창고 식량 약 {days:0.#}일분 · 요리 {cooked}끼 / 원료 {raw}개";
        public string Bottlenecks => string.Join(" · ", new[] {
            floor > 0 ? $"음식 운반 대기 {floor}개" : null,
            spoiling > 0 ? $"1일 내 부패 {spoiling}개" : null,
            unplanted > 0 ? $"화분 파종 필요 {unplanted}곳" : null,
            seedBlocked > 0 ? $"종자 부족 {seedBlocked}곳" : null,
            waterBlocked > 0 ? $"급수 중단 {waterBlocked}곳" : null,
            sowing > 0 ? $"밭 파종 대기 {sowing}곳" : null,
            cookingBlocked > 0 ? $"조리 재료 부족 {cookingBlocked}곳" : null
        }.Where(s => s != null));

        public static ColonyFoodStatus Read()
        {
            var result = new ColonyFoodStatus();
            var crew = CommanderRoster.Instance?.Commanders.Where(c => c && c.IsColonyMember && !c.IsDead
                && !c.IsAwayFromHome && !c.IsCaptive).ToArray() ?? System.Array.Empty<CommanderAnt>();
            result.residents = crew.Length;
            var rm = ResourceManager.Instance;
            if (rm != null)
            {
                var bins = rm.Bins.Where(b => b.Spendable && ProductionStock.AtHome(b.Position)).ToArray();
                foreach (var type in CookedFoods.MealTypes.Where(t => t.IsEdible()))
                {
                    int amount = bins.Sum(b => b.Amount(type));
                    if (CookedFoods.Quality(type) > 0) result.cooked += amount;
                    else result.raw += amount;
                    result.spoiling += bins.Where(b => b.Amount(type) > 0).Sum(b => b.Lots(type)?.lots
                        .Where(l => l.shelf <= GameCalendar.SecondsPerDay).Sum(l => l.amount) ?? 0);
                }
                float portion = crew.Length > 0 ? crew.Average(c => (float)c.RationPortion) : GameBalance.MealFood;
                result.days = crew.Length == 0 ? 0 : (result.cooked + result.raw / Mathf.Max(1, portion)) / (crew.Length * 2f);
                result.floor = Mathf.FloorToInt(AntColony.World.ResourceNode.Available.Where(n => n && n.IsLooseCargo
                    && !n.IsRaidLoot && n.ResourceType.IsEdible() && ProductionStock.AtHome(n.transform.position)).Sum(n => n.AmountRemaining));
            }
            foreach (var crop in SceneObjects.All<StarterCropBed>().Where(b => b && b.isActiveAndEnabled && !b.IsDead))
            {
                if (!crop.Crop.planted) result.unplanted++;
                else if (crop.Crop.wateredSeconds <= 0 && (rm?.GetAmount(ResourceType.Water) ?? 0) < StartSupplyRules.MushroomWaterPerDay
                    && !FluidNetwork.Has(crop, ResourceType.Water, StartSupplyRules.MushroomWaterPerDay)) result.waterBlocked++;
            }
            foreach (var plot in FarmPlot.All.Where(p => p && p.isActiveAndEnabled && !p.Data.fallow))
            {
                if (plot.Data.seedPending && FarmSeeds.Count(plot.Crop) < plot.SeedCost) result.seedBlocked++;
                else if (plot.Node.NeedsSowing) result.sowing++;
            }
            result.cookingBlocked = SceneObjects.All<Kitchen>().Count(k => k && k.isActiveAndEnabled && !k.IsDead
                && !k.IsTable && !k.Paused && k.WaitReason?.Contains("부족") == true);
            return result;
        }
    }
}
