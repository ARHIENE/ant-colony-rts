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
        public static float WalkSeconds(ExpeditionSite site) => GameCalendar.SecondsPerDay
            * Mathf.Max(.25f, site == null ? 1 : Mathf.Round(Vector2.Distance(site.MapPosition, new Vector2(.5f, .5f)) / .16f * 1000) / 1000);
        public static int HomeCommanders => CommanderRoster.Instance == null ? 0 : CommanderRoster.Instance.Commanders
            .Count(c => c != null && c.isActiveAndEnabled && c.IsColonyMember && !c.IsDead && !c.IsAwayFromHome);
        public static float FoodPerDay(CommanderAnt c) => 2 * (c.RationPortion + c.TroopCount * GameBalance.MealFood);
        public static int RecommendedFood(float oneWay, float dailyFood) => Mathf.CeilToInt(
            (2 * oneWay / GameCalendar.SecondsPerDay + 2) * dailyFood);
    }
}
