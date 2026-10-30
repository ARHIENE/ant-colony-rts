using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public static class ExpeditionRules
    {
        // 잠정: 가장 가까운 띠(.16)는 편도 하루, 이후 거리는 비례한다.
        public const float DockSeconds = 1;
        public static float TransportSeconds(float distance, bool aircraft)
        {
            int ring = distance < .235f ? 0 : distance < .385f ? 1 : 2;
            return aircraft ? (ring == 0 ? 10 : ring == 1 ? 20 : 35) : (ring == 0 ? 20 : ring == 1 ? 40 : 70);
        }
        // 편입지 간에는 본거지 띠를 쓰지 않고 최근거리 .16의 기존 속도로 실제 거리를 환산한다(잠정).
        public static float BetweenSeconds(Vector2 from, Vector2 to, bool walking, bool aircraft)
        {
            double x = to.x - from.x, y = to.y - from.y;
            return (float)(System.Math.Sqrt(x * x + y * y) / .16 * (walking ? GameCalendar.SecondsPerDay : aircraft ? 10 : 20));
        }
        public static float WalkSeconds(ExpeditionSite site) => GameCalendar.SecondsPerDay
            * Mathf.Max(.25f, site == null ? 1 : Mathf.Round(Vector2.Distance(site.MapPosition, new Vector2(.5f, .5f)) / .16f * 1000) / 1000);
        public static int HomeCommanders => CommanderRoster.Instance == null ? 0 : CommanderRoster.Instance.Commanders
            .Count(c => c != null && c.isActiveAndEnabled && c.IsColonyMember && !c.IsDead && !c.IsAwayFromHome);
        public static float FoodPerDay(CommanderAnt c) => 2 * (c.RationPortion + c.TroopCount * GameBalance.MealFood);
        public static int RecommendedFood(float oneWay, float dailyFood) => Mathf.CeilToInt(
            (2 * oneWay / GameCalendar.SecondsPerDay + 2) * dailyFood);
    }
}
