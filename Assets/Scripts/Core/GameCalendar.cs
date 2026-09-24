using UnityEngine;

namespace AntColony.Core
{
    public enum Season { Spring, Summer, Autumn, Winter }

    public static class GameCalendar
    {
        public const float SecondsPerMonth = 300f;
        public static float GameSeconds => GameSession.Exists ? GameSession.Instance.GameSeconds : 0f;
        public static int TotalMonths => Mathf.FloorToInt(GameSeconds / SecondsPerMonth);
        public static int Year => TotalMonths / 12 + 1;
        public static int Month => TotalMonths % 12 + 1;
        public static Season CurrentSeason => (Season)((Month - 1) / 3);
        public static float MonthProgress => GameSeconds % SecondsPerMonth / SecondsPerMonth;
        public static string Label => $"Year {Year} / {CurrentSeason} / Month {Month}";
    }
}
