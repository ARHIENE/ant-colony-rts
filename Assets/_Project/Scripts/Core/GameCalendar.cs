using UnityEngine;

namespace AntColony.Core
{
    public enum Season { Spring, Summer, Autumn, Winter }

    public static class GameCalendar
    {
        // 2026-10-23 G08: '월' 폐지, 계절·일 달력. 1일 = 150초(낮 100·밤 50), 1계절 = 6일(15분), 1년 = 24일(60분).
        public const float SecondsPerDay = 150f, DaySeconds = 100f;
        public const int DaysPerSeason = 6, DaysPerYear = DaysPerSeason * 4;
        public const float SecondsPerSeason = SecondsPerDay * DaysPerSeason, SecondsPerYear = SecondsPerSeason * 4;
        // 옛 '1개월'(300초 = 2일) 비율 단위. 기존 월간 수치(세금·유지비·나이 등)의 내부 환산에만 쓴다.
        public const float SecondsPerMonth = SecondsPerYear / 12f;
        public static float GameSeconds => GameSession.Exists ? GameSession.Instance.GameSeconds : 0f;
        public static int Year => Mathf.FloorToInt(GameSeconds / SecondsPerYear) + 1;
        public static int TotalSeasons => Mathf.FloorToInt(GameSeconds / SecondsPerSeason);
        public static Season CurrentSeason => (Season)(TotalSeasons % 4);
        public static int Day => Mathf.FloorToInt(GameSeconds / SecondsPerDay);
        public static int DayOfSeason => Day % DaysPerSeason + 1;
        public static string Label => $"Year {Year} / {CurrentSeason} / Day {DayOfSeason}";
        static readonly string[] SeasonNames = { "봄", "여름", "가을", "겨울" };
        // 기록용 날짜(사망일·귀환 예정일 등).
        public static string Format(float gameSeconds)
        {
            var d = Mathf.FloorToInt(Mathf.Max(0, gameSeconds) / SecondsPerDay);
            return $"{d / DaysPerYear + 1}년 {SeasonNames[d / DaysPerSeason % 4]} {d % DaysPerSeason + 1}일";
        }

        public static float TimeOfDay => GameSeconds % SecondsPerDay;
        public static bool IsNight => TimeOfDay >= DaySeconds;
        public static float SecondsUntilPhaseChange => IsNight ? SecondsPerDay - TimeOfDay : DaySeconds - TimeOfDay;
    }
}
