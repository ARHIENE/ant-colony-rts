namespace AntColony.Core
{
    // 2026-09-25 작업 지시서 1단계 수치. 전부 잠정값이므로 밸런스 조정은 이 파일만 고친다.
    public static class GameBalance
    {
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

        // 장수 본인 유지비: 30초 주기마다 1명당 Food (식성 특성 배율 적용)
        public const float CommanderUpkeepFood = 2;

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

        // 작물: 기본 균류는 밭 템플릿 값 그대로(7단계에서 계절과 함께 조정)
        public const float HoneydewSeconds = 300, HoneydewFood = 80;
        public const float AdvancedFungusSeconds = 360, AdvancedFungusFood = 150;
        public const float AdvancedFungusSpecialChance = .1f;
        public const int AdvancedFungusSpecial = 2;
    }
}
