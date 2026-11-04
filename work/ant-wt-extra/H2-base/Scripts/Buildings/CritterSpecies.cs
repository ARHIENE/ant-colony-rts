using System.Linq;
using AntColony.Data;
using AntColony.Units;

namespace AntColony.Buildings
{
    // 목장 생물(2026-10-11 개편). 진딧물·누에·거미·벌은 기획 후보이며 종별 수치·식성·포식 관계는 기획 미정이라 전부 잠정.
    public enum Species { Aphid, Silkworm, Spider, Bee }
    public enum CritterTemper { Flee, Attack, Conditional } // 도주형 / 주변 공격형 / 조건부 공격형(다룬 장수만)
    public enum HandlingRisk { Safe, Caution, Danger }

    public sealed class SpeciesInfo
    {
        public string name; public CritterTemper temper; public bool tameable, plantDiet;
        public ResourceType direct, floor; public float directPerMonth, floorPerMonth; // 직접 채취 / 바닥 생산(한 마리·한 달)
        public float space, weight, health, damage;          // 필요 공간(칸), 운반 무게(0~1), 체력, 공격력
        public float babyMonths, oldMonths, lifeMonths;      // 새끼 기간 / 노령 시작 / 이 나이부터 자연사 확률 급증
        public int meat, chitin; public Species[] prey = new Species[0];

        // ponytail: 종별 수치는 잠정(기획 미정). 확정되면 이 표만 고친다.
        private static readonly SpeciesInfo[] Table =
        {
            new SpeciesInfo { name = "진딧물", temper = CritterTemper.Flee, tameable = true, plantDiet = true, direct = ResourceType.Honeydew, directPerMonth = 3, floor = ResourceType.Honeydew,
                space = .5f, weight = .1f, health = 40, damage = 2, babyMonths = 1, oldMonths = 4, lifeMonths = 6, meat = 6, chitin = 1 },
            new SpeciesInfo { name = "누에", temper = CritterTemper.Flee, tameable = true, plantDiet = true, direct = ResourceType.Thread, directPerMonth = 2, floor = ResourceType.Thread,
                space = .5f, weight = .15f, health = 40, damage = 2, babyMonths = 1, oldMonths = 6, lifeMonths = 8, meat = 8, chitin = 1 },
            new SpeciesInfo { name = "거미", temper = CritterTemper.Attack, tameable = false, plantDiet = false, direct = ResourceType.Cobweb, floor = ResourceType.Cobweb, floorPerMonth = 2,
                space = 3, weight = .4f, health = 120, damage = 8, babyMonths = 2, oldMonths = 10, lifeMonths = 14, meat = 20, chitin = 4,
                prey = new[] { Species.Aphid, Species.Silkworm, Species.Bee } },
            new SpeciesInfo { name = "벌", temper = CritterTemper.Conditional, tameable = true, plantDiet = true, direct = ResourceType.Honey, directPerMonth = 2, floor = ResourceType.Wax, floorPerMonth = 1,
                space = 1, weight = .15f, health = 50, damage = 5, babyMonths = 1, oldMonths = 7, lifeMonths = 10, meat = 6, chitin = 2 },
        };
        public static SpeciesInfo For(Species s) => Table[(int)s];

        // 식성: 식물성(잎·섬유, 귀한 감로·꿀은 먹이통에 직접 허용할 때만 운반) / 동물성(식량·사체·사냥).
        public ResourceType[] Feeds => plantDiet ? new[] { ResourceType.Leaf, ResourceType.Fiber, ResourceType.Honeydew, ResourceType.Honey } : new[] { ResourceType.Food };
        public bool Eats(ResourceType t) => Feeds.Contains(t);
        public static bool Precious(ResourceType t) => t == ResourceType.Honeydew || t == ResourceType.Honey;
        public static readonly ResourceType[] AllFeeds = { ResourceType.Food, ResourceType.Leaf, ResourceType.Fiber, ResourceType.Honeydew, ResourceType.Honey };

        // 취급 위험: 길들인 개체는 저항하지 않는다(0). 야생은 성향 기본 위험 × (1 − 해당 능력/25). 안전·주의는 자동, 위험은 유저 지시 대기.
        public static float FailChance(Species sp, bool tame, CommanderAnt c, CommanderActivity skill = CommanderActivity.Husbandry)
        {
            if (tame) return 0;
            var t = For(sp).temper; var basis = t == CritterTemper.Attack ? .45f : t == CritterTemper.Conditional ? .25f : .1f;
            return basis * UnityEngine.Mathf.Clamp01(1f - (c != null ? c.Talents.Level(skill) : 0) / 25f);
        }
        public static HandlingRisk Risk(float chance) => chance < .08f ? HandlingRisk.Safe : chance < .16f ? HandlingRisk.Caution : HandlingRisk.Danger;
        public static string RiskName(HandlingRisk r) => r == HandlingRisk.Safe ? "안전" : r == HandlingRisk.Caution ? "주의" : "위험";
        public static string TemperName(CritterTemper t) => t == CritterTemper.Flee ? "도주형" : t == CritterTemper.Attack ? "공격형" : "조건부 공격형";
    }
}
