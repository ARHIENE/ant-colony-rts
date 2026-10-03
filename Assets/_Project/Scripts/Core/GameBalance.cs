namespace AntColony.Core
{
    // 2026-09-25 작업 지시서 1단계 수치. 전부 잠정값이므로 밸런스 조정은 이 파일만 고친다.
    public static class GameBalance
    {
        // 새 기획의 미확정 수치는 잠정값.
        public const float CommanderHealth = 100, CommanderRecoverySeconds = 60;
        public const float AutoReturnSeconds = 20, ReturnEnemyRadius = 12, WorkScanSeconds = 1;
        public const int ConscriptionFood = 30, ConscriptionSoil = 50, ConscriptionAnts = 4;
        public const float ConscriptionBuildSeconds = 8;
        public const int WorkshopFood = 50, WorkshopSoil = 80, WorkshopSpecial = 10, WorkshopAnts = 6;
        public const float WorkshopBuildSeconds = 10, CraftWork = 90, WorkshopRadius = 8;
        public const int CraftQueueCapacity = 3;
        // 과학 연구: tier 1~4
        public static readonly float[] ScienceWork = { 300, 800, 1800, 4000 };
        public static readonly int[] ScienceFood = { 50, 100, 200, 400 };
        public static readonly int[] ScienceSoil = { 50, 100, 200, 400 };
        public static readonly int[] ScienceSpecial = { 0, 10, 30, 80 };

        // 비행선 동면 고치: 고치 1개 = 장수 1명 + 그 장수의 병력 전부
        public const int CocoonFood = 100, CocoonSoil = 50, CocoonSpecial = 10;
        public const float CocoonSeconds = 60;
        public const int MaxCocoons = 20;

        // 수송수단: 장수 슬롯은 일반개미 정원과 별도
        public const int VehicleCommanders = 4, VehicleTroops = 40, VehicleCargo = 200;
        public const int AircraftCommanders = 8, AircraftTroops = 100, AircraftCargo = 500;

        // 번식: 연인(관계 70↑) 쌍만, 둘 다 양육실 반경 안
        public const float LoverRelation = 70, NurseryRadius = 8;
        public const float HappyMood = 60, HappyBreedMultiplier = 1.5f;
        public const float SadMood = 30, SadBreedMultiplier = .5f;
        public const float ParentChildRelation = 40;

        // 한 끼 Food(2026-09-28: 장수 자동 유지비 삭제, 식사로 대체). 식성 특성은 한 끼 양 배율.
        public const float MealFood = 2;

        // 2단계 시설 성능
        public const float WallArmor = 2;
        public const float TrapTriggerRadius = 1.2f, TrapRootSeconds = 4, TrapBossRootSeconds = 1;
        public const int TrapRepairSoil = 10;
        public const float TrapRepairSeconds = 4, TrapAutoRepairSeconds = 60;
        public const float AreaTowerRange = 9, AreaTowerRadius = 3, AreaTowerDamage = 8, AreaTowerInterval = 2.5f;
        public const int WatchtowerSites = 3;
        public const float WatchtowerWarningSeconds = 60;
        public const float MineTriggerRadius = 1.2f, MineRadius = 4, MineDamage = 60;
        public const int MaxMines = 8;
        public const float RestRoomRadius = 8, RestMood = 5;
        public const int RestRoomSeats = 4;
        public const float RegenerationSeconds = 480;
        public const int RegenerationSpecial = 20;

        // 작물: 기본 균류 3분·Food 40. 가을 수확 ×1.25, 겨울 성장 정지(밭만).
        public const float FungusSeconds = 180, FungusFood = 40, AutumnHarvestMultiplier = 1.25f;
        // 낚시: 1회 20초·Food 6 × 낚시 기술 배율(한파 −20%). 낚시터 1곳당 월 Food 100, 다음 달 회복.
        public const float FishingCatchSeconds = 20, FishingCatchFood = 6, FishingMonthlyFood = 100;
        public const float HoneydewSeconds = 300, HoneydewFood = 80;
        public const float AdvancedFungusSeconds = 360, AdvancedFungusFood = 150;
        public const float AdvancedFungusSpecialChance = .1f;
        public const int AdvancedFungusSpecial = 2;

        // 낮밤·수면 (2026-09-28). 숙소 정원 4 확정, 나머지 잠정.
        // Phase 4 인구(2026-10-01, 잠정): 기본 살 자리, 세금(30초마다 납세 개미 × 세율 × 이 값 Food), 민심·이주·노화.
        public const int BaseHousing = 50, MinImmigrationDemand = 30;
        public const float DoorBashDamage = 15; // Phase 5: 문에 닿은 적이 0.25초마다 주는 피해(잠정)
        // 공성(2026-10-03, 잠정): 길이 막힌 침공 개체가 벽을 찾는 반경, 나뭇잎 벽 불(지속·초당 피해·옆 벽 번질 확률)
        public const float SiegeSearchRadius = 8, WallFireSeconds = 8, WallFireDamagePerSecond = 20, WallFireSpreadChance = .5f;
        public const float TaxFoodPerAnt = .5f, MaxTaxRate = .5f, RaidSentiment = 10, UnrestSentiment = 15;
        public const float ImmigrationShare = .3f, AdultAging = .03f, OldDeath = .15f, UnrestDesertion = .05f, StarveDesertion = .05f;
        // 주거 건물(방 밖에 짓는 건물 단위, 자동 레벨업 없음): 수용 수·재료·인력·시간
        public const int HutHousing = 20, HutSoil = 30, HouseHousing = 50, HouseSoil = 80, ApartmentHousing = 120, ApartmentSoil = 200, ApartmentSpecial = 20;
        // 장수 나이(개월): 시작 12~72, 어린 장수 12개월 미만, 늙음 96개월 이상, 수명 120~168개월.
        public const float ChildMonths = 12, ElderMonths = 96, MinLifespan = 120, MaxLifespan = 168;
        public const int DormitoryBeds = 4, DormitoryFood = 20, DormitorySoil = 40, DormitoryAnts = 4;
        public const float DormitoryBuildSeconds = 8;
        public const float RoughSleepMood = -15, RivalRoommateMood = -5, BadSleepMood = -5;
        public const int RoughSleepStreakMood = -5, RoughSleepNights = 3;
        public const float PoorSleepWork = .8f;
        // 피로 0~100: 낮 작업 10분(한 낮)이면 가득, 숙소에서 한 밤 자면 0. 설친 잠은 절반만 풀린다.
        public const float FatiguePerWorkSecond = 100f / 600f, FatigueRestPerSecond = .5f, TiredFatigue = 50;
        public const float FullSleepShare = .9f;

        // 인력(2026-09-28): 상한 = 5 + 해당 작업 기술×2(확정). 개미 1마리당 효율은 일정(잠정 +10%).
        public const int WorkforceBase = 5, WorkforcePerSkill = 2;
        public const float WorkforcePerAnt = .1f;
        // 한 짐 운반량 = 10 + 근력×1.5 + 인력 1마리당 2 (확정). 근력 경험치는 운반량만큼(잠정).
        public const float CarryBase = 10, CarryPerStrength = 1.5f, CarryPerAnt = 2;
        // 수리 비용 = 잃은 체력 비율 × 건설비 50%(확정). 수리 속도 잠정: 초당 최대 체력 2%.
        public const float RepairCostShare = .5f, RepairPerSecond = .02f;
        // 간호 장수가 없으면 의무실 치료 절반 속도(확정).
        public const float UnnursedTreatment = .5f;

        // 식사(2026-09-28): 하루 3끼·날것 -8·식중독 60초·-10은 확정, 나머지 잠정.
        // 포만 0~100, 깨어 있는 낮(10분) 동안 3끼 먹도록 30 이하에서 식당으로 간다.
        public const float SatietyPerSecond = 70f / 200f, EatBelowSatiety = 30, EatSeconds = 10, StarveRetrySeconds = 30;
        public static readonly float[] MealMood = { -8, 0, 5, 10 }; // 날것·간단·좋은·고급
        public const float FoodPoisonSeconds = 60, FoodPoisonMood = -10;

        // 오락(2026-09-28 확정: 욕구·질림·다양성 보너스·장식 근처 +20%). 수치는 잠정.
        // 오락 0~100, 깨어 있는 낮 동안 100 → 0. 30 이하면 놀러 간다. 20초 놀면 +50 × (1 - 질림).
        public const float JoyPerSecond = 100f / 600f, PlayBelowJoy = 30, PlaySeconds = 20, PlayJoy = 50;
        public const float BoredomPerPlay = .3f, BoredomRecoverPerSecond = .3f / 900f, DecorationPlayBonus = .2f;
        public const float LowJoyMood = -5, VeryLowJoyMood = -10, VarietyMoodPerKind = 2;
        public const int CampfireSoil = 15, GamblingDenSoil = 25, CampfireSeats = 6, GamblingDenSeats = 4;
        // 도박장(2026-09-29 초안 승인): 함께 한 판 끝나면 승자 +5·패자 -3(3분), 10% 말다툼 관계 -5.
        public const float GambleWinMood = 5, GambleLoseMood = -3, GambleQuarrelChance = .1f, GambleQuarrel = -5;
    }
}
