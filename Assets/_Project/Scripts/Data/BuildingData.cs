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
        Hearth, SleepingMat, DoubleBed, Floor, LockedDoor, BarredDoor,
        // 가구 3차: 1인 침대·책장·목욕통·버섯밭·축사(버섯밭·축사는 작물 고정 밭으로 배치되어 저장은 Farm)
        SingleBed, Bookshelf, Bathtub, MushroomFarm, AphidPen,
        // 가구 4차: 저장고(식량)·항아리·무기고
        FoodStore, Jar, Armory,
        // 가구 5차: 2층침대·해먹·큰 식탁 / 휴게 8종 / 장식 4종(조각상~기둥은 Decoration.IsKind 범위로 묶임)
        BunkBed, Hammock, BigTable,
        BoardGame, Janggi, Baduk, WrestlingRing, DartBoard, ExerciseRig, Instrument, WebSwing,
        Statue, Flag, Painting, Pillar,
        // 가구 6차: 조명 3종·장식 6종(화롯불~사람 물건 전시대는 Decoration.IsKind 범위) / 온천 / 연회용 긴 식탁
        Brazier, Lantern, Torch, Carpet, Tapestry, Monument, BronzeStatue, TrophyCase, CuriosDisplay,
        HotSpring, BanquetTable,
        SpikeTrap, // 2026-10-07 가시 함정(반복 피해). 기존 TrapPit은 끈끈이 함정(속박)
        AdminDesk, // 2026-10-08 행정 시설(이름·비용 잠정)
        // 2026-10-10 자원 분화: 가공대(목공대·석공대·가마·물레·용광로)
        Carpentry, Stonecutter, Kiln, SpinningWheel, Smelter,
        // 2026-10-10 목장: 우리 표지(튼튼한 우리는 옛 저장 호환용, 같은 동작)
        Pen, StrongPen,
        // 2026-10-11 목장 시설·약제대(동물용 의약품·수술 키트)
        Feeder, CareStation, AnimalClinic, ButcherTable, MedicineBench
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
