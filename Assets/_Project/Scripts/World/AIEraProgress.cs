using System;
using AntColony.Core;

namespace AntColony.World
{
    // 세력 공동 연구비는 외교 재고에서만 지불한다. 현장 창고 자원을 중복 차감하지 않는다.
    public static class AIEraProgress
    {
        public static float Work(int era) => 600 * era;
        public static int Cost(int era) => 40 * era;
        public static int AttackBonus(int era) => 2 * (era - 1);
        public static int ArmorBonus(int era) => era - 1;
        public static int CombatEra(WildMonster unit)
        {
            if (unit.GuardBoss >= 0 || unit.Allied) return 1;
            var manager = DiplomacyManager.Instance;
            var faction = string.IsNullOrEmpty(unit.DiplomaticFactionId) ? manager?.Faction(unit.GetComponentInParent<ExpeditionSite>())
                : manager?.Data.civilizations.Find(c => c.id == unit.DiplomaticFactionId);
            return faction?.era ?? 1;
        }
        public static bool Valid(Civilization c) => c.era >= 1 && c.era <= CampaignResearch.EraCount
            && c.eraWork >= 0 && !float.IsInfinity(c.eraWork) && c.eraWork <= Work(c.era)
            && (c.era < CampaignResearch.EraCount || c.eraWork == 0 && !c.eraPaid);
        public static bool Tick(Civilization c, float seconds, int buildings)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || buildings <= 0 || c.extinct || c.era >= CampaignResearch.EraCount) return false;
            if (!c.eraPaid)
            {
                int cost = Cost(c.era);
                if (c.resources[0] < cost || c.resources[1] < cost) return false;
                c.resources[0] -= cost; c.resources[1] -= cost; c.eraPaid = true;
            }
            c.eraWork = Math.Min(Work(c.era), c.eraWork + seconds);
            if (c.eraWork < Work(c.era)) return false;
            c.era++; c.eraWork = 0; c.eraPaid = false;
            return true;
        }
        public static void Credit(Civilization c, int buildings)
        {
            for (int i = 0; i < 2; i++) c.resources[i] = (int)Math.Min(int.MaxValue, (long)c.resources[i] + buildings * c.era);
        }
    }
}
