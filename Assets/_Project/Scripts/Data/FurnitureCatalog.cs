using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;

namespace AntColony.Data
{
    // 건설 분류 16종(2026-10-05 확정). BuildScreen 탭 순서와 같다.
    public enum BuildCategory { Tile, Living, Storage, Environment, Power, Automation, Plumbing, Food, Work, Medical, Defense, Recreation, Decoration, Military, Village, Transport }
    // 가구가 잇는 망: 전선(전력)·배관(물·석유)·신호선(자동화). 배관·신호선은 데이터만 있고 구현은 다음 차례(TODO).
    public enum NetworkLayer { None, Power, Pipe, Signal }

    public sealed class FurnitureDef
    {
        public readonly string Name;
        public readonly BuildCategory Category;
        public readonly int Era; // 1 소굴 ~ 6 미래 (CampaignResearch.EraNames)
        public readonly RoomKind Room; // 필수 방. None = 아무 데나(방 밖 포함)
        public readonly BuildingKind? Existing; // 지금 게임에 있는 건물로 대신 쓸 때(모델·기능 재사용)
        public readonly NetworkLayer Network;
        public readonly bool MaterialUpgradable; // 같은 가구를 더 좋은 재료로 다시 지어 올리기(재료 목록 미정)
        public FurnitureDef(string name, BuildCategory category, int era, RoomKind room, BuildingKind? existing, NetworkLayer network, bool upgradable)
        { Name = name; Category = category; Era = era; Room = room; Existing = existing; Network = network; MaterialUpgradable = upgradable; }
    }

    // 가구·건물 목록(2026-10-05 '분류별 구체화' + '추가 확정 200개 확장'). 이름·분류·시대·필수 방만 담은 데이터 정의.
    // 수치는 전부 잠정. 시대는 문서에 적힌 것(예: 미래 버전, 전기 상위판)을 따르고, 적히지 않은 것은 성격으로 추정했다.
    // 실제 건설 가능한 것은 Existing이 있는 항목뿐이다. 나머지는 모델·기능을 만들 때 기존 건물을 먼저 재사용한다.
    public static class FurnitureCatalog
    {
        public static readonly string[] CategoryNames = { "타일", "생활", "저장", "환경", "전력", "자동화", "배관", "식량", "작업", "의료", "방어", "휴게", "장식", "군사", "마을", "이동수단" };

        private static BuildCategory cat;
        private static readonly List<FurnitureDef> list = new List<FurnitureDef>();
        private static void In(BuildCategory c) => cat = c;
        // 연결 망(전선·배관·신호선)만 하는 칸은 재료 업그레이드 대상이 아니다.
        private static void F(string name, int era, RoomKind room = RoomKind.None, BuildingKind? existing = null, NetworkLayer net = NetworkLayer.None, bool upgradable = true)
            => list.Add(new FurnitureDef(name, cat, era, room, existing, net, upgradable));

        public static readonly IReadOnlyList<FurnitureDef> All = Build();
        public static IEnumerable<FurnitureDef> InCategory(BuildCategory c) => All.Where(f => f.Category == c);
        public static FurnitureDef For(BuildingKind kind) => All.FirstOrDefault(f => f.Existing == kind);

        private static IReadOnlyList<FurnitureDef> Build()
        {
            In(BuildCategory.Tile); // 벽은 재료별로 따로(실제 목록은 재료 확정 후)
            F("흙벽", 1, existing: BuildingKind.SoilWall); F("나뭇잎 벽", 1, existing: BuildingKind.LeafWall); F("돌벽", 1); F("병뚜껑 벽", 2, existing: BuildingKind.CapWall);
            F("문", 1, existing: BuildingKind.Door); F("잠금문", 1, existing: BuildingKind.LockedDoor); F("창살문", 1, existing: BuildingKind.BarredDoor); F("자동문", 6); F("유리벽", 6);
            F("바닥", 1, existing: BuildingKind.Floor); F("기둥", 1, existing: BuildingKind.Pillar); F("울타리", 1); F("방화벽", 2); F("다리", 1);

            In(BuildCategory.Living);
            F("자리", 1, RoomKind.Bedroom, BuildingKind.SleepingMat); F("침대", 1, RoomKind.Bedroom, BuildingKind.SingleBed); F("숙소(4인, 구 건물)", 1, RoomKind.Bedroom, BuildingKind.Dormitory); F("큰침대", 1, RoomKind.Bedroom, BuildingKind.DoubleBed); F("2층침대", 2, RoomKind.Bedroom, BuildingKind.BunkBed); F("해먹", 1, RoomKind.Bedroom, BuildingKind.Hammock);
            F("요람", 1, RoomKind.Nursery, BuildingKind.Nursery);
            F("작은 식탁", 1, RoomKind.Dining, BuildingKind.Kitchen); F("큰 식탁", 1, RoomKind.Dining, BuildingKind.BigTable); F("연회용 긴 식탁", 2, RoomKind.BanquetHall, BuildingKind.BanquetTable);
            F("구덩이 변소", 1, RoomKind.Bathroom); F("화장실", 2, RoomKind.Bathroom, BuildingKind.Toilet); F("수세식 변기", 4, RoomKind.Bathroom, net: NetworkLayer.Pipe); F("분해 변기", 6, RoomKind.Bathroom);
            F("세면대", 1, RoomKind.Bathroom, BuildingKind.Washbasin); F("샤워기", 2, RoomKind.Bathroom, BuildingKind.Shower);

            In(BuildCategory.Storage); // 창고 없이 바닥 보관도 가능(비 맞으면 상함)
            F("수납장", 1, RoomKind.Storeroom, BuildingKind.Storage); F("항아리", 1, RoomKind.Storeroom, BuildingKind.Jar); F("저장고", 1, RoomKind.ColdStorage, BuildingKind.FoodStore);
            F("냉장고", 4, RoomKind.ColdStorage, net: NetworkLayer.Power); F("냉동창고", 5, RoomKind.ColdStorage, net: NetworkLayer.Power);
            F("운반 레일", 4); F("순간이동 보관함", 6, RoomKind.Storeroom, net: NetworkLayer.Power);

            In(BuildCategory.Environment); // 불 쓰는 가구는 화재 가능. 연료: 나무 → 화석연료 → 석유 → 전기 → 태양광·핵융합
            F("화롯불", 1, existing: BuildingKind.Brazier); F("등불", 1, existing: BuildingKind.Lantern); F("횃불", 1, existing: BuildingKind.Torch); F("반딧불 등", 1, existing: BuildingKind.FireflyLamp); F("난로", 2); F("환풍구", 2);
            F("전등", 4, existing: BuildingKind.ElectricLamp, net: NetworkLayer.Power); F("가로등", 4, net: NetworkLayer.Power);
            F("에어컨", 4, net: NetworkLayer.Power); F("전기난로", 4, net: NetworkLayer.Power); F("가습기", 4, net: NetworkLayer.Power); F("제습기", 4, net: NetworkLayer.Power);
            F("온도 조절기", 4, net: NetworkLayer.Signal); F("공기청정기", 5, net: NetworkLayer.Power); F("인공 태양", 6, net: NetworkLayer.Power);

            In(BuildCategory.Power); // 풍차·물레방아·태양광판은 방 밖 전용
            F("쳇바퀴", 1, RoomKind.PowerPlant, BuildingKind.Treadmill, NetworkLayer.Power); F("장작 발전기", 1, RoomKind.PowerPlant, BuildingKind.WoodGenerator, NetworkLayer.Power);
            F("반딧불 발전기", 1, RoomKind.PowerPlant, net: NetworkLayer.Power); F("물레방아", 1, net: NetworkLayer.Power); F("풍차", 2, net: NetworkLayer.Power);
            F("석탄 발전기", 2, RoomKind.PowerPlant, net: NetworkLayer.Power); F("시추선", 3, net: NetworkLayer.Pipe); F("석유 발전기", 3, RoomKind.PowerPlant, net: NetworkLayer.Power);
            F("태양광판", 5, net: NetworkLayer.Power); F("핵융합로", 5, RoomKind.PowerPlant, net: NetworkLayer.Power);
            F("반물질로", 6, RoomKind.PowerPlant, net: NetworkLayer.Power); F("우주 태양광", 6, net: NetworkLayer.Power);
            F("전선", 1, existing: BuildingKind.PowerWire, net: NetworkLayer.Power, upgradable: false); F("배터리", 1, existing: BuildingKind.Battery, net: NetworkLayer.Power);
            F("스위치", 1, net: NetworkLayer.Power, upgradable: false); F("전력 변압기", 4, net: NetworkLayer.Power);

            In(BuildCategory.Automation); // 산소미포함식 센서·논리, 신호선을 전선과 따로 깐다
            foreach (var n in new[] { "온도계", "습도계", "무게 감압판", "시계 센서", "낮밤 센서", "저장량 센서", "전력량 센서",
                "AND 게이트", "OR 게이트", "NOT 게이트", "NOR 게이트", "XOR 게이트", "버퍼 게이트", "필터 게이트", "기억 장치", "카운터", "신호선" })
                F(n, 4, net: NetworkLayer.Signal, upgradable: false);

            In(BuildCategory.Plumbing); // 물·석유가 배관으로 흐름(시추선 → 정유기 → 발전기). 단물은 들고 나름
            F("배관", 2, net: NetworkLayer.Pipe, upgradable: false); F("펌프", 2, net: NetworkLayer.Pipe); F("밸브", 2, net: NetworkLayer.Pipe, upgradable: false);
            F("액체 탱크", 2, net: NetworkLayer.Pipe); F("빗물 받이", 1, net: NetworkLayer.Pipe); F("이슬 수집기", 1, net: NetworkLayer.Pipe); F("정수기", 4, net: NetworkLayer.Pipe);

            In(BuildCategory.Food); // 농장 방 = 밭·수경 농장·버섯밭·축사·먹이통, 밭은 방 밖도 가능
            F("밭", 1, RoomKind.Farm, BuildingKind.Farm); F("버섯밭", 1, RoomKind.Farm, BuildingKind.MushroomFarm); F("축사", 1, RoomKind.Farm, BuildingKind.AphidPen); F("먹이통", 1, RoomKind.Farm); F("수경 농장", 4, RoomKind.Farm, net: NetworkLayer.Pipe);
            F("화덕", 1, RoomKind.Kitchen, BuildingKind.Hearth); F("가스레인지", 3, RoomKind.Kitchen, net: NetworkLayer.Pipe); F("전기조리대", 4, RoomKind.Kitchen, net: NetworkLayer.Power); F("미래 조리기구", 6, RoomKind.Kitchen, net: NetworkLayer.Power);
            F("해체대", 1, RoomKind.Kitchen); F("물고기 양식장", 2, RoomKind.Fishery); F("목장", 2, RoomKind.Fishery);
            F("벌레덫", 1); F("양갱기", 2, RoomKind.Kitchen); F("꿀단지 개미방", 1, RoomKind.Storeroom); F("발효통", 1); F("영양죽 기계", 3, RoomKind.Kitchen);
            F("제분기", 2, RoomKind.ProcessingRoom); F("사료통", 1, RoomKind.Dining); F("비료 제조기", 3, RoomKind.Farm); F("훈제실·건조대", 1, RoomKind.Kitchen);
            F("꿀벌통", 2); F("씨앗 보관고", 1, RoomKind.Storeroom); F("단물 짜는 기계", 2, RoomKind.Farm); F("음료 바", 2, RoomKind.Recreation); F("연회 요리대", 3, RoomKind.Kitchen);
            F("식량 프린터", 6, RoomKind.Kitchen, net: NetworkLayer.Power); F("합성육 배양기", 6, RoomKind.Kitchen, net: NetworkLayer.Power);

            In(BuildCategory.Work); // 연구대는 분야별로 따로(분야: 생물·기계·군사·사회, 분야별 연구 목록 미정)
            F("석공대", 1, RoomKind.ProcessingRoom); F("물레", 1, RoomKind.ProcessingRoom); F("무두장이대", 1, RoomKind.ProcessingRoom); F("분해대", 2, RoomKind.ProcessingRoom);
            F("무기 공방", 2, RoomKind.Workshop, BuildingKind.Workshop); F("갑옷 공방", 2, RoomKind.Workshop); F("장신구 공방", 2, RoomKind.Workshop); F("수리대", 1, RoomKind.Workshop);
            F("연구대", 1, RoomKind.Laboratory, BuildingKind.ScienceLab); F("예술대", 1, RoomKind.Studio);
            F("용광로·제련소", 2, RoomKind.ProcessingRoom); F("유리 가마", 2, RoomKind.ProcessingRoom); F("화학 실험대", 3, RoomKind.Laboratory); F("정유기", 3, RoomKind.ProcessingRoom, net: NetworkLayer.Pipe);
            F("재봉틀", 3, RoomKind.ProcessingRoom); F("부품 조립대", 4, RoomKind.Workshop, net: NetworkLayer.Power); F("로봇 공장", 5, RoomKind.Workshop, net: NetworkLayer.Power);

            In(BuildCategory.Medical); // 관·묘비는 시신 처리용(장례 이벤트 없음), 방 밖도 가능
            F("병상", 1, RoomKind.Hospital, BuildingKind.Infirmary); F("약제대", 1, RoomKind.Hospital); F("수술대", 2, RoomKind.Hospital); F("격리 병상", 2, RoomKind.Hospital);
            F("의족 제작대", 2, RoomKind.Hospital); F("기계 다리 제작대", 6, RoomKind.Hospital, net: NetworkLayer.Power); F("재생 탱크", 5, RoomKind.Hospital, net: NetworkLayer.Power);
            F("관", 1, RoomKind.Graveyard); F("묘비", 1, RoomKind.Graveyard); F("약초밭", 1); F("화장터", 2);

            In(BuildCategory.Defense); // 방어 건물은 방 밖 전용. 방어탑 시대별: 개미산탑 → 투석기 → 화염탑 → 레이저탑
            F("성벽", 1, existing: BuildingKind.CastleWall); F("성문", 1, existing: BuildingKind.Gate); F("망루", 1, existing: BuildingKind.Watchtower);
            F("가시 함정", 1, existing: BuildingKind.TrapPit); F("끈끈이 함정", 1); F("폭발 함정", 4, existing: BuildingKind.MineField);
            F("개미산탑", 1, existing: BuildingKind.AcidTower); F("투석기", 2); F("화염탑", 3); F("레이저탑", 5, net: NetworkLayer.Power);
            F("모래주머니 엄폐물", 1); F("해자", 2); F("경보종", 1); F("탐지등", 4, net: NetworkLayer.Power); F("방어막 발생기", 6, net: NetworkLayer.Power);

            In(BuildCategory.Recreation); // 휴게실 = 휴게 가구 2종 이상
            F("놀이판", 1, RoomKind.Recreation, BuildingKind.BoardGame); F("장기판", 1, RoomKind.Recreation, BuildingKind.Janggi); F("바둑판", 1, RoomKind.Recreation, BuildingKind.Baduk); F("씨름판", 1, RoomKind.Recreation, BuildingKind.WrestlingRing);
            F("가시 다트", 1, RoomKind.Recreation, BuildingKind.DartBoard); F("도토리 볼링", 1, RoomKind.Recreation); F("악기", 1, RoomKind.Recreation, BuildingKind.Instrument); F("운동기구", 2, RoomKind.Recreation, BuildingKind.ExerciseRig);
            F("진딧물 꿀술바", 2, RoomKind.Recreation); F("거미줄 그네", 1, RoomKind.Recreation, BuildingKind.WebSwing); F("민들레 홀씨 활강", 1, RoomKind.Recreation);
            F("이야기 모닥불", 1, RoomKind.Recreation, BuildingKind.Campfire); F("주사위 도박판", 1, RoomKind.Recreation, BuildingKind.GamblingDen); F("무대", 2, RoomKind.Recreation);
            F("목욕통", 1, RoomKind.Bathhouse, BuildingKind.Bathtub); F("온천", 1, RoomKind.Bathhouse, BuildingKind.HotSpring); F("안마의자", 4, RoomKind.Recreation, net: NetworkLayer.Power);
            F("책장", 1, RoomKind.Library, BuildingKind.Bookshelf); F("홀로그램 극장", 6, RoomKind.Recreation, net: NetworkLayer.Power); F("게임기·VR", 6, RoomKind.Recreation, net: NetworkLayer.Power);

            In(BuildCategory.Decoration); // 방 안 = 방 등급, 방 밖 = 주변 기분
            F("조각상", 1, existing: BuildingKind.Statue); F("화분", 1, existing: BuildingKind.FlowerPot); F("깃발", 1, existing: BuildingKind.Flag); F("그림", 1, existing: BuildingKind.Painting); F("조개껍데기", 1, existing: BuildingKind.ShellDecoration);
            F("구슬 모자이크", 1, existing: BuildingKind.MarbleMosaic); F("병뚜껑 모빌", 1, existing: BuildingKind.BottleMobile); F("카펫", 1, existing: BuildingKind.Carpet); F("태피스트리", 1, existing: BuildingKind.Tapestry);
            F("사람 물건 전시대", 2, existing: BuildingKind.CuriosDisplay); F("분수대", 2, net: NetworkLayer.Pipe); F("기념비", 2, existing: BuildingKind.Monument); F("동상", 2, existing: BuildingKind.BronzeStatue); F("전리품 진열대", 2, existing: BuildingKind.TrophyCase); F("홀로그램 장식", 6, net: NetworkLayer.Power);

            In(BuildCategory.Military); // 막사 세부(병력 상한 등) 미정
            F("허수아비(근접 훈련대)", 1, RoomKind.TrainingRoom, BuildingKind.Barracks); F("과녁(원거리 훈련대)", 1, RoomKind.TrainingRoom); F("작전판(지휘 훈련대)", 1, RoomKind.TrainingRoom);
            F("징집소", 1, existing: BuildingKind.ConscriptionPost); F("무기고", 1, RoomKind.Armory, BuildingKind.Armory); F("막사", 1, RoomKind.Barracks);
            F("작전실 지도대", 2, RoomKind.WarRoom); F("신호탑", 4, RoomKind.WarRoom, net: NetworkLayer.Power); F("포로 수용소", 1, RoomKind.Prison, BuildingKind.PrisonerCamp);
            F("군기·북", 1); F("정찰 초소", 1, existing: BuildingKind.ScoutPost);

            In(BuildCategory.Village); // 일반개미용, 방 밖 전용(시티즈 공공서비스식). 물이 자원이 될지는 미정
            F("초가집", 1, existing: BuildingKind.Hut); F("흙집", 1, existing: BuildingKind.House); F("큰 아파트", 1, existing: BuildingKind.Apartment);
            F("진료소", 1); F("소방서", 2); F("경비초소", 1); F("학당", 1); F("시장", 1); F("공원·광장", 1); F("쓰레기장", 1);
            F("물탱크", 2, net: NetworkLayer.Pipe); F("우물", 1, net: NetworkLayer.Pipe); F("도로", 1, upgradable: false); F("제단", 1, RoomKind.Temple); F("여관", 1);

            In(BuildCategory.Transport); // 방 밖 전용. 로켓은 미래 시대 승리
            F("로켓 발사대", 6, existing: BuildingKind.AirshipYard); F("로켓 선체", 6); F("로켓 추진기관", 6); F("동면 고치", 6);
            F("차량", 2); F("비행기", 4); F("비행선", 4); F("배·뗏목", 1); F("잠수함", 5); F("터널 기차", 3, net: NetworkLayer.Power);
            F("타는 벌레 마구간", 1); F("민들레 열기구", 1); F("정류장·역", 3); F("활주로", 4); F("항구", 3);
            return list.ToArray();
        }
    }
}
