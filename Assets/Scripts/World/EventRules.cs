using AntColony.Core;

namespace AntColony.World
{
    public enum ColonyEvent { Cold, Flood, Drought, Wildfire, Mold, Wasps, Wanderer, Driftwood, Harvest, Migration, Caravan }
    public static class EventRules
    {
        public const float CheckSeconds = 150, MinimumGap = 60, Cooldown = 600;
        public const float ColdSeconds = 90, FloodSeconds = 60, DroughtSeconds = 120;
        public const float ColdMovePenalty = .25f, ColdGatherPenalty = .2f;
        public const float WaterRadius = 20, FireRadius = 8, InfectionRadius = 8, InfectionLossSeconds = 20, InfectionSpreadSeconds = 30;
        public const float InfectionChance = .25f, WandererSeconds = 60, DriftSeconds = 300, RecruitBonus = .2f, ApproachRadius = 3;
        public const int DriftFood = 80, DriftSoil = 40, Migrants = 10, Wasps = 3;
        public const float WaspHealth = 15, WaspDamage = 1, WaspInterval = 2, WaspSpeed = 2.5f;
        public static readonly string[] Names = { "한파", "홍수", "가뭄", "산불", "곰팡이 감염", "기생 말벌", "장수 후보 방랑", "표류물", "풍작", "이주 개미떼", "교역 캐러밴" };
        // 잠정 난이도 확률: 150초마다 온화 50% / 보통 60% / 가혹 100%.
        public static float Chance(DifficultyLevel d) => d == DifficultyLevel.Gentle ? .5f : d == DifficultyLevel.Harsh ? 1f : .6f;
        public static float CrisisChance(DifficultyLevel d) => d == DifficultyLevel.Gentle ? .4f : d == DifficultyLevel.Harsh ? .75f : .6f;
        public static bool InSeason(ColonyEvent e, Season season) => e switch
        {
            ColonyEvent.Cold => season == Season.Winter,
            ColonyEvent.Flood => season == Season.Spring,
            ColonyEvent.Drought => season == Season.Summer,
            ColonyEvent.Wildfire or ColonyEvent.Harvest => season == Season.Autumn,
            ColonyEvent.Migration => season == Season.Spring || season == Season.Summer,
            ColonyEvent.Caravan => DiplomacyManager.Instance?.Available == true,
            _ => true
        };
    }
}
