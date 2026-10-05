using UnityEngine;

namespace AntColony.Data
{
    public enum BuildingKind
    {
        QueenChamber, // Phase 4: 여왕방 삭제 → 비축더미(Stockpile). 직렬화 번호 유지
        Barracks,
        Storage,
        DigSite,
        ResearchLab,
        Farm,
        // 장수 획득 경로 3종. 기존 값의 직렬화 번호가 밀리지 않도록 뒤에 추가한다.
        Nursery,
        ScoutPost,
        PrisonerCamp,
        ScienceLab,
        AcidTower,
        AirshipYard,
        Infirmary,
        // 과학 2단계 시설
        SoilWall,
        TrapPit,
        AreaAcidTower,
        Watchtower,
        MineField,
        DefenseLab,
        RestRoom,
        Workshop,
        ConscriptionPost,
        // 2026-09-28 생활 시설
        Dormitory, Kitchen, FlowerPot, ShellDecoration, MarbleMosaic, BottleMobile, FireflyLamp,
        // 오락 시설(처음부터 2종, 2026-09-29 사용자 선택)
        Campfire, GamblingDen,
        // Phase 4 주거 건물: 초가집·흙집·큰 아파트
        Hut, House, Apartment,
        // Phase 5 벽·문: 나뭇잎 벽·병뚜껑 벽·문·성벽·성문
        LeafWall, CapWall, Door, CastleWall, Gate,
        // 2026-10-05 위생(화장실·세면대·샤워기) + 전력 1차(쳇바퀴·장작 발전기·전선·배터리·전등)
        Toilet, Washbasin, Shower, Treadmill, WoodGenerator, PowerWire, Battery, ElectricLamp,
        // 2026-10-05 가구 2차: 화덕(조리대)·자리·큰침대·바닥·잠금문·창살문
        Hearth, SleepingMat, DoubleBed, Floor, LockedDoor, BarredDoor
    }

    [CreateAssetMenu(fileName = "BuildingData", menuName = "AntColony/Building Data")]
    public class BuildingData : ScriptableObject
    {
        public string displayName = "Building";
        public BuildingKind kind = BuildingKind.Storage;

        [Header("Combat")]
        public float maxHealth = 300f;

        [Header("Cost")]
        public int foodCost = 0;
        public int soilCost = 20;
        [Min(0)] public int specialCost = 0;
        public float buildTimeSeconds = 3f;
        // ponytail: 임시 건설 인력. 건물별 밸런스 확정 시 에셋에서 조정한다.
        [Min(0)] public int constructionAnts = 5;

        [Header("Storage Only")]
        public int foodCapacityBonus = 0;
        public int soilCapacityBonus = 0;
        [Min(0)] public int specialCapacityBonus = 100;
    }
}
