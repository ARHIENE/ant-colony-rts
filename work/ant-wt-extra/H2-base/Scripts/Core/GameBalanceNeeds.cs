using UnityEngine;

namespace AntColony.Core
{
    public static class GameBalanceNeeds
    {
        public const float HygienePerSecond = 25f / GameCalendar.SecondsPerDay;
        public const float ToiletSeconds = 3, ToiletGain = 50, ToiletCap = 70, TemporaryCap = 30;
        public static int WaterCost(int kind) => kind == 1 ? 1 : kind == 2 ? 3 : 0;
        public static float Washed(float hygiene, int kind) => kind == 2 ? 100
            : kind == 1 ? Mathf.Min(100, hygiene + 30) : Mathf.Max(hygiene, Mathf.Min(ToiletCap, hygiene + ToiletGain));
        public static int HygieneMood(float hygiene) => hygiene < 10 ? -6 : hygiene < 30 ? -3 : 0;
        // 0 저재산 / 1 정착지급 / 2 산업급. 정착지 전체 재산(RocketEconomy.CaptureOwned)으로 판정한다.
        public const float SettlementWealth = 3000, IndustrialWealth = 15000; // 잠정(결정 원문에 경계값 없음)
        public static int ExpectationTier(float wealth) => wealth >= IndustrialWealth ? 2 : wealth >= SettlementWealth ? 1 : 0;
        public static int ExpectationMood(int tier) => tier == 0 ? 15 : tier == 1 ? 5 : 0;
        public static int MoodWarning(float mood) => mood <= 5 ? 3 : mood <= 20 ? 2 : mood <= 35 ? 1 : 0;
    }
}
