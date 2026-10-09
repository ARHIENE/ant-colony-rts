using System.Linq;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    // 주거 재개발(2026-10-08 기획, 수치 잠정): 건설 메뉴에서 더 큰 주거를 기존 집 위에 배치하면 교체 공사.
    // 공사 중에는 기존 집이 그대로 살 자리·납세를 유지하고, 완공하는 순간 기존 집을 허문다(임시 거처 없음).
    // 보상비는 실제 거주 인원 비례(빈집 0)이며 배치 순간 식량·재료로 지급한다. 취소해도 보상비는 돌려받지 않는다.
    public static class Redevelopment
    {
        public struct Quote
        {
            public Housing target;
            public int residents, food, soil, special, compensationFood, compensationSoil;
            public float sentiment; // 예상 민심 변화(음수 = 불만)
            public int TotalFood => food + compensationFood;
            public int TotalSoil => soil + compensationSoil;
        }

        // 겹친 주거 중 새 종류보다 정원이 작고, 이미 공사 중이 아닌 것 하나.
        public static Housing FindTarget(BuildingKind kind, Vector3 position, Vector3 extents, int mask)
        {
            if (!Housing.IsKind(kind)) return null;
            return Physics.OverlapBox(position, extents, Quaternion.identity, mask, QueryTriggerInteraction.Collide)
                .Select(hit => hit.GetComponentInParent<Housing>())
                .FirstOrDefault(h => h != null && !h.IsDead && h.Capacity < Housing.CapacityOf(kind) && !UnderConstruction(h));
        }

        public static bool UnderConstruction(Housing h) => Object.FindObjectsByType<RedevelopmentSite>(FindObjectsSortMode.None).Any(s => s.Target == h);

        public static int Residents(Housing h)
        {
            var pop = ColonyPopulation.Instance;
            if (h == null || pop == null || pop.HousingCapacity <= 0) return 0;
            return Mathf.RoundToInt(h.Capacity * Mathf.Clamp01((float)pop.Total / pop.HousingCapacity));
        }

        public static Quote Price(BuildingData data, Housing target)
        {
            var pop = ColonyPopulation.Instance;
            float admin = pop != null ? pop.Administration : 0f, level = pop != null ? pop.S.redevelopCompensation : 1f;
            var q = new Quote { target = target, residents = Residents(target) };
            q.food = Mathf.CeilToInt(data.foodCost * GameBalance.RedevelopCostShare);
            q.soil = Mathf.CeilToInt(data.soilCost * GameBalance.RedevelopCostShare);
            q.special = Mathf.CeilToInt(data.specialCost * GameBalance.RedevelopCostShare);
            var paid = q.residents * level * (1f - GameBalance.AdminCompensationRelief * admin);
            q.compensationFood = Mathf.CeilToInt(paid * GameBalance.RedevelopFoodPerResident);
            q.compensationSoil = Mathf.CeilToInt(paid * GameBalance.RedevelopSoilPerResident);
            q.sentiment = -q.residents * GameBalance.RedevelopUnrestPerResident * Mathf.Max(0f, 1f - level) * (1f - GameBalance.AdminRedevelopRelief * admin);
            return q;
        }

        public static string Describe(Quote q)
        {
            var pop = ColonyPopulation.Instance;
            var after = pop != null ? pop.S.sentiment + q.sentiment : 0f;
            var risk = pop != null && after <= GameBalance.UnrestSentiment ? " · <color=#d9534f>이탈 위험</color>" : "";
            return $"재개발: {q.target.Data.displayName} 교체 · 거주 {q.residents}마리 · 건설 식량 {q.food}/재료 {q.soil}{(q.special > 0 ? $"/특수 {q.special}" : "")}"
                + $" · 보상 식량 {q.compensationFood}/재료 {q.compensationSoil} ({(pop != null ? pop.S.redevelopCompensation : 1f):P0})"
                + $" · 민심 {q.sentiment:+0.#;-0.#;0}{risk} · 행정 성과 {(pop != null ? pop.Administration : 0f):P0}";
        }

        // 공사 지연: 기준 시간을 넘긴 재개발 현장마다 민심이 서서히 내려간다(행정이 완화). 취소하면 즉시 멈춘다.
        internal static void TickDelays(ColonyPopulation pop, float seconds)
        {
            foreach (var site in Object.FindObjectsByType<RedevelopmentSite>(FindObjectsSortMode.None))
            {
                site.Age += seconds;
                if (site.Age <= GameBalance.RedevelopDelaySeconds) continue;
                pop.S.sentiment = Mathf.Max(0f, pop.S.sentiment - GameBalance.RedevelopDelaySentimentPerMonth * seconds / GameCalendar.SecondsPerMonth
                    * (1f - GameBalance.AdminRedevelopRelief * pop.Administration));
            }
        }
    }

    // 재개발 건설 예정지 표식. 완공 시 기존 집을 허문다(건설 현장은 저장 전에 끝내야 하므로 저장하지 않는다).
    public sealed class RedevelopmentSite : MonoBehaviour
    {
        public Housing Target { get; internal set; }
        public float Age { get; internal set; }
        internal void Finish() { if (Target != null) Destroy(Target.gameObject); Target = null; }
    }
}
