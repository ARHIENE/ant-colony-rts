# Notion 기획 ↔ 코드 감사 — 2026-10-25

- 대상: `E:\Git\ant`, `develop`, HEAD `eec134c` + 현재 작업 트리(기존 미커밋 변경 포함). Unity 6000.5.8f1.
- **Notion MCP fetch 원문 5개 모두 성공**. 로컬 `docs/notion-spec/` 내보내기는 사용하지 않았다. 응답은 페이지 끝까지 제공됐으며 잘림·미지원 블록 경고는 없었다.
- 출처: [자원](https://app.notion.com/p/3d0c4a0ecd31812d80fbeceabb70193e)(수정 10-24 00:03 KST), [장수·유닛](https://app.notion.com/p/3d8c4a0ecd318155a477fda677e21fd1)(10-24 13:09), [건물](https://app.notion.com/p/3d0c4a0ecd3181c28b59e26337c15aba)(10-24 00:03), [과학](https://app.notion.com/p/3e5c4a0ecd3181aa8714fcb45a675818)(10-24 00:11), [특성·다이아 확정 원본](https://app.notion.com/p/401c4a0ecd3181d88860f1cd12558ae3)(10-24 00:11).
- 방법: `log.md`·`docs/IMPLEMENTATION_BOARD.md`·`docs/PLAY_CHECK_DECISIONS_1023.md` 확인 → Graphify 읽기 전용 query → `Assets/_Project/Scripts`의 rg 및 관련 본문 대조. Graphify 광역 결과는 464개 중 56개로 잘려 탐색에만 사용했고 판정 근거는 실제 소스로 확인했다.
- **Unity 실행·컴파일·플레이·저장 왕복·화면 검증 없음. Notion 수정 없음.** 씬·프리팹·실제 직렬화 값은 감사 범위 밖이다. ‘구현’은 소스에 규칙과 처리 경로가 있다는 뜻이며 실행 통과를 뜻하지 않는다.
- 증거 경로는 모두 `Assets/_Project/Scripts/` 기준. ‘grep 없음’은 해당 범위에서 한·영 이름과 관련 데이터/처리 구조를 함께 검색했으나 대응 구현을 찾지 못했다는 뜻이다. 동명 UI 시안·주석만으로 구현 판정하지 않았다.
- 판정: **구현**(확정 규칙 대응), **부분**(일부만 존재하거나 확정 규칙과 충돌), **미구현**(대응 데이터/동작 없음), **판단불가**(스크립트만으로 확정 불가).
- 집계 단위는 아래 **감사 ID가 있는 행**이다. 같은 규칙의 세부 조건·콘텐츠 묶음은 한 행으로 묶었다. 페이지 간 중복도 각 페이지의 요구로 집계하므로 전체 고유 기능 개수는 아니다. 잠정/미정 항목과 G 참조표는 신규 감사 집계에서 제외한다.
- G01~G69는 요청대로 **재감사하지 않았다**. G 참조표의 ‘판단불가’는 구현 여부 미판정이라는 뜻이며 누락 판정이 아니다. 로그의 ‘코드 연결 완료’와 실제 빌드 검증을 동일시하지 않는다.
- 원문 충돌 처리: 날짜가 최신인 확정 원본·G 결정 우선. 옛 221종 후보/삼국지 전설 16종·성격 후천 변화·창고 기본 지급·시민 작업 지원·옛 달력은 현행 요구로 다시 계산하지 않았다. 동물 수술 후유증 누적은 폐기됐지만 자원/건물 본문의 일부 옛 문장이 남아 있다.

## 집계

신규 감사 행 기준. G 참조·잠정/미정 제외, 페이지 간 중복 포함.

| 페이지 | 구현 | 부분 | 미구현 | 판단불가 | 합계 |
|---|---:|---:|---:|---:|---:|
| 자원 시스템 | 40 | 33 | 12 | 1 | 86 |
| 장수·유닛 시스템 | 26 | 16 | 13 | 1 | 56 |
| 건물·방어시설 | 30 | 8 | 3 | 2 | 43 |
| 과학·연구 | 11 | 3 | 2 | 1 | 17 |
| 특성·다이아 | 1 | 9 | 24 | 0 | 34 |
| **합계** | **108** | **69** | **54** | **5** | **236** |

## 1. 자원 시스템

| 항목 | 판정 | 근거(file:line 또는 grep 없음) | 비고 |
|---|---|---|---|
| R01 작업 종류→대상 1~9→실제 경로 거리→요청 순, 0 금지 | 구현 | Units/CommanderDuty.cs:201,211; Units/WorkPriorities.cs:8 | 경보·금지 검사 포함. 실행 순서는 미실측 |
| R02 운반 완료 후 전환·직접 지시 시 짐 내려놓기·한도 묶음·예약/경로 실패 해제 | 구현 | Units/WorkerAnt.cs:1; Core/ResourceManager.cs:154; Units/CommanderWork.cs:40 | 동물은 별도 작업·한 마리 제한 |
| R03 창고 품목 허용·우선순위·고정 용량·썩은 식품 기본 제외 | 구현 | Core/StoreLedger.cs:16,43; Core/ResourceManager.cs:208,215 | 일반 창고 목표 재고 기능과 생산 목표 구분 |
| R04 허용 해제/꺼내기/전체 비우기→즉시 바닥 배출 | 구현 | Core/ResourceManager.cs:208,216,246 | 다시 허용하면 바닥 재운반 가능 |
| R05 창고 간 자동 재배치 없음·목적지 소실 시 재탐색/바닥 | 구현 | Units/WorkerAnt.cs:1; Core/ResourceManager.cs:150 | 창고 우선순위는 입고 대상 선택 |
| R06 물량별 신선도 유지·부패→썩은 식품·보관 불허면 배출·묶음 알림 | 구현 | Core/FoodLots.cs:39,48,60,83; Core/ResourceManager.cs:235 | 종류별 기간 수치는 G30 참조 |
| R07 좋은 정상 식품→낮은 신선도 우선·정상식 없을 때 썩은 식품 | 부분 | Units/CommanderMeal.cs:59,84; Core/ResourceManager.cs:268; Core/FoodLots.cs:48 | 식사 우선 등급/각 lot 오래된 순은 있음. 원격 창고 소비라 실제 식품 접근성 미반영, 창고 간에는 우선순위로 소비해 전체 최저 신선도 우선도 아님 |
| R08 식중독은 경과 회복·청결/오염 보정 없음 | 부분 | Units/CommanderMeal.cs:119 | 경과 타이머는 있으나 식중독 확률에 방 바닥의 FloorPoisonMultiplier를 적용. 명시적 청결 제외와 충돌 |
| R09 냉장/냉동은 내부 재고만·신선도 복구 없음·정전 후 서서히 가온/재냉각 | 구현 | Buildings/ColdStorage.cs:14,20,33; Core/StoreLedger.cs:32 | 냉장 감속·냉동 정지, 식품 창고 필터 |
| R10 냉장/냉동 상태·재고 표시와 비용 차이 | 부분 | Buildings/ColdStorage.cs:18,21; UI/GameMenuStorage.cs:1 | 전력 차이·상태는 있음. 상위 연구/건설비의 실제 자산 값은 별도 확인 필요 |
| R11 퇴비 원료 선택·운반·무인 숙성·목표 도달 시 신규 투입만 중지 | 구현 | Buildings/CompostBin.cs:23,26,44,53 | 비료는 내부 보관, 창고와 구분 |
| R12 숙성 중 꺼내기 원료 반환·성장 주기당 비료 1회·중도 투입 허용 | 구현 | Buildings/CompostBin.cs:74; Buildings/FarmPlot.cs:62,64 | 기존 성장분에 소급 적용하지 않는 구조 |
| R13 날것/기본/정성/호화 식사·레시피로 등급 결정 | 부분 | Buildings/Kitchen.cs:25; Data/ResourceType.cs:53 | 실제 재고 요리는 SimpleMeal/FineMeal 2종, 호화 요리 재고 없음 |
| R14 요리 생산 3모드·시작 시 소비·인계/중단 보존·바닥 완성품 | 구현 | Buildings/Kitchen.cs:57,110,125 | 일반 요리사 인계 가능, 취소는 진행/소비 재료 복구 없음 |
| R15 요리 재료별 허용·대체/혼합·희귀 제외·요리별 요구 기술 | 부분 | Buildings/Kitchen.cs:25,74 | Food 단일 입력. 개별 식재료·필터·레시피별 기술 조건 부족 |
| R16 공통/개인 식단·복사/일괄·식탁 없을 때 불이익 | 구현 | Units/CommanderMeal.cs:26,84,119; UI/GameMenuDiet.cs:14 | 현재 3분류 식단에 한함 |
| R17 품목별 요리사 정보·식중독 확률 추적 | 부분 | Buildings/Kitchen.cs:127 | 품목별 최근 기술 이동 평균 사용. 식품 개체별 제작자/기술 이력 아님 |
| R18 일반 가공 3모드·재료 없는 주문 건너뛰기 | 구현 | Buildings/Processor.cs:72,79,84 | 지정/재고/계속 생산, 비용은 묶음 시작 때 소비 |
| R19 목표 재고: 바닥 완성품·진행 산출 포함, 외부 반출 제외 | 부분 | Buildings/Processor.cs:79; Buildings/Workshop.cs:36,47; Buildings/Kitchen.cs:63 | Processor는 창고 수량만 조회. 모든 생산 시설에 동일한 집계가 적용되지 않음 |
| R20 일반 생산 연료/전력/출력 공간 대기·완성 고체는 항상 옆 바닥 | 부분 | Buildings/Processor.cs:98,108 | 일반 가공은 전력·연료·배출 자리 검사 없이 StoreResource. 창고 여유 시 직접 입고 |
| R21 주재료 단일 재질·부재료 슬롯·필터·완성품 재질 표시 | 부분 | Data/ResourceType.cs:64; Buildings/Demolition.cs:48; Units/EquipmentRecipes.cs:11 | 건축 주재료만 일부. 장비/일반 제조에 확장되지 않음 |
| R22 운반 가능한 미완성품·주문 삭제와 취소 분리 | 부분 | Buildings/Workshop.cs:13,18,101; Buildings/Processor.cs:49 | 제작자/진행은 시설 상태로 보존. 별도 실물 미완성품의 운반·보관 없음. 환급은 G18 |
| R23 금속 장비 재활용 지정·예약 취소 확인·재질/최대 내구도 비례 회수 | 미구현 | grep 없음(Recycle/재활용); Units/CommanderEquipment.cs:14 | 재질·투입 금속량·감소 최대 내구도 데이터 없음 |
| R24 지표 광맥 칸별 지정·부분 소멸·진행 인계·바닥 산출 | 부분 | World/ResourceNode.cs:67,155; Map/MaterialDeposits.cs:79 | 노드 잔량 추출 방식. 광맥 칸별 채굴 작업물/지속 작업량 구조 없음 |
| R25 돌→청동→강철→티타늄 합금 채굴 등급 | 부분 | World/ResourceNode.cs:48,52; Units/GroundCrafting.cs:1 | 현재 나무/돌 도구와 단일 MiningToolTier=2. 고급 등급 없음 |
| R26 모래·점토 삽, 맨손 식물·나머지 작업 등급 제한 없음 | 부분 | World/ResourceNode.cs:48; Units/CommanderPersonalState.cs:1 | 맨손 식물은 가능. 독립 삽/도구 종류 슬롯 없음 |
| R27 도구 내구도 파괴·미보유/등급 부족 사유 | 부분 | Units/CommanderPersonalState.cs:24; World/ResourceNodeStatus.cs:1 | 나무/돌 미보유·등급 차단은 있으나 도구 세트 내구 필드/파괴 처리 없음. 고급 등급·도구별 수리도 없음 |
| R28 지하 탐사기(연구 속도/전력)·영구 발견·범위 강조·심층 채굴기 | 미구현 | grep 없음(GroundScanner/DeepDrill/지하 탐사/심층 채굴) | 지하 이동층을 요구하는 항목 아님 |
| R29 유한 원유/가스 매장량·지점당 시설·고갈 후 재설치 금지/이전 | 부분 | Buildings/SupplyFacility.cs:44,68 | 시추는 근처 OilDeposit 존재만 검사. 유한 매장량 차감 없음, 가스 추출 없음 |
| R30 액체/기체 분리 배관·한 탱크 한 종류·막힘/출력 부족 정지 | 부분 | Buildings/FluidNetwork.cs:8; Buildings/SupplyFacility.cs:42,45,68 | 물/원유/정제유만. 천연가스·기체 망 없음 |
| R31 배관 단절 시 내부 잔량 사용·정유 작업자 필요 | 부분 | Buildings/SupplyFacility.cs:68; Buildings/FluidNetwork.cs:34 | 정유는 자동 Tick, 전용 작업자 없음. 시설별 내부 공급 버퍼도 불완전 |
| R32 원유/가스 용기 충전·배출 작업자·빈 용기 재사용 | 부분 | Buildings/SupplyFacility.cs:94; Data/ResourceType.cs:60 | 정제유 드럼 충전만. 가스/원유 충전·일반 배출·재사용 순환 없음 |
| R33 원유→플라스틱·가스 연료·목재/숯/석탄 허용·효율 표시 | 부분 | Buildings/PowerNode.cs:1; Buildings/Processor.cs:26; grep 없음(Plastic/NaturalGas/Charcoal) | 기본 발전만, 전체 연료 선택 계통 없음 |
| R34 맥주·쌀술·과실주·벌꿀주·감로주·증류주 | 미구현 | grep 없음(Beer/Brew/Distill/양조/증류/감로주) | 생산 목록·알코올 자원 없음 |
| R35 양조 무인 발효·시작 후 추가 불가·취소 손실 경고·증류 연료/작업 | 미구현 | grep 없음(Brew/Distill/발효); Core/CampaignResearch.cs:63 | Fermentation은 ‘압축 저장’ 연구 이름일 뿐 양조 기능 아님 |
| R36 음주 여가·허용/간격·어린이 금지·취기/의존 | 미구현 | UI/GameMenuManagement.cs:138; grep 없음(Alcohol) | U30~U32와 같은 공백 |
| R37 목재 3종/동종 판재·석재 4종/동종 블록 | 부분 | Data/ResourceType.cs:37; Buildings/Processor.cs:28 | Wood/Plank, Stone/StoneBlock만. 단단/가벼운 목재·사암/화강암/대리석/응회암 독립 재고 없음 |
| R38 구리/주석/철/은/금/티타늄/성휘 광석·금속/합금 단계 | 부분 | Data/ResourceType.cs:43,60,60; Data/SupplyRecipes.cs:1 | 구리/주석/철·강철/청동까지 일부. 은/금/티타늄/성휘 계열 없음 |
| R39 섬유/실/털별 직물·얇은/두꺼운 가죽·모피 | 부분 | Data/ResourceType.cs:43,45; Buildings/Processor.cs:32 | Fiber/Thread/Cobweb→Cloth. 털·가죽·모피 독립 자원 없음 |
| R40 화석/광물/수액/부산물 목록 | 부분 | Data/ResourceType.cs:43,55,60 | 석탄/원유/모래/점토/황/수지/배설물 있음. 가스·석회석·천연고무 수액·짚 없음 |
| R41 숯·코크스·연료·플라스틱·고무·유리·벽돌·도자기·생석회·퇴비 | 부분 | Buildings/Processor.cs:26; Data/ResourceType.cs:45,55,60 | 정제유/유리/벽돌/비료만 일부. 나머지 독립 재고·공정 없음 |
| R42 기존 흙·잎·갑각·거미줄·밀랍·물 유지 | 구현 | Data/ResourceType.cs:37 | 기존 자원 enum 유지 |
| R43 곡물·채소·과일/열매·견과/씨앗 확정 전체 목록 | 미구현 | grep 없음(Rice/Wheat/Barley/쌀/밀/보리의 식재료 정의); Data/ResourceType.cs:37 | 원문 목록 전부 독립 식재료 재고 없음. Food 묶음으로 대체 중 |
| R44 버섯·민물/바닷물고기·수산물 확정 전체 목록 | 미구현 | Data/ResourceType.cs:37; Buildings/FarmPlot.cs:13 | Fungus/낚시 Food는 종별 식재료가 아님 |
| R45 고기·식용 곤충·알/유제품/소금 확정 전체 목록 | 미구현 | Data/ResourceType.cs:37; Buildings/RanchButcher.cs:70 | 해체 산출은 Food/Chitin. 종별 고기·식용곤충·알·우유·소금 재고 없음 |
| R46 목화/아마/약초/사탕·수액/향신/차·커피·카카오 작물 | 미구현 | Buildings/FarmPlot.cs:13; Data/ResourceType.cs:37 | 독립 작물/재고 없음. Herbs 연구만으로 작물 구현 판정 안 함 |
| R47 가루/기름/두부·유제품/설탕·시럽/보존식/조미료/커피·코코아·맥아 가공 목록 | 미구현 | Buildings/Processor.cs:26; Data/ResourceType.cs:37 | 원문 가공 식품 전체의 제조법/재고 없음 |
| R48 밥·빵·면/국물·채소/고기·수산/알·곤충/후식·음료 요리 목록 | 미구현 | Buildings/Kitchen.cs:25 | 간단/좋은 요리의 추상 레시피만 있음 |
| R49 조리 중간물은 별도 재고 아님·완성식 재조리 금지 | 구현 | Buildings/Kitchen.cs:25,74 | 현 Food 입력 체계에서 중간 재고/완성식 재투입 없음. 다종 조리 완성 의미는 아님 |
| R50 7지역 주산지·계절별 식생/광물 분포·모든 자원 한 맵 배치 금지 | 부분 | Map/MaterialDeposits.cs:24,42,59; Buildings/FarmPlot.cs:13 | 기존 원료 전종을 양만 다르게 배치. 확정 식재료/희귀 분포 누락. 시작 보장은 G35/G58/G59 |
| R51 작물별 온도·기간·계절·온실·야생 재생 | 부분 | World/ResourceNode.cs:104,112; Buildings/FarmPlot.cs:13 | 균류 3종·일반 재생/계절만, 실물 온실/광범위 작물 환경표 없음 |
| R52 재질별 장비 성능·내구도·재료 고정 채굴 등급·상위는 신규 제작 | 부분 | Units/CommanderEquipment.cs:14; World/ResourceNode.cs:52 | 품질은 있으나 장비 재질 필드·상위 금속 제작 없음 |
| R53 산업 열전도·단열·내열·액체/기체 용량·식품 영양 단위 | 부분 | Data/ResourceType.cs:64; Buildings/SpaceQuality.cs:65; Buildings/SupplyFacility.cs:18 | 건물 단열·유체 정수량 일부. 공정 열전도/내열·개별 영양 데이터 없음 |
| R54 시민 생계/세금은 맵 고갈·창고 식량과 독립·시민 평시 작업 없음 | 구현 | Core/ColonyPopulation.cs:1; Core/ColonyPopulation.Admin.cs:24; Units/CommanderDuty.cs:1 | 생산원과 소비 주체 분리. 달력/상세 세액은 G08/G17 |
| R55 재료 세금은 지역 기초 원료·우선 납부 품목·희귀 제외 | 구현 | Core/ColonyPopulation.cs:1; Core/ColonyPopulation.Civilians.cs:1 | 지역 기본 재료·납세 방침 처리. 비중 상한은 미정 |
| R56 세율 1%/직접 입력·납세 방침·기간별 대상/세율 누적·민심 목표 수렴 | 구현 | Core/ColonyPopulation.cs:1; UI/GameMenuPopulation.cs:1 | 매일 지급/행정 크기 수치는 G08/G17. 수치 밸런스 미검증 |
| R57 외교 성립 직전 재검증·즉시 교환·초과/창고 없음 바닥 보존 | 구현 | World/DiplomacyTrade.cs:56,87; World/DiplomacyPrisoners.cs:208 | 진행물 보호는 G51 참조 |
| R58 농사 파종→자연 성장→수확·고정 담당 없음·운반 별도·밭 확장 | 구현 | Buildings/FarmPlot.cs:40,107; Units/CommanderDuty.cs:348 | 작물 종류 확장은 R46/R51. 씨앗은 G28 |
| R59 낚시 연구·물가·계절 한도·겨울 허용 | 구현 | Core/CampaignResearch.cs:90; World/ResourceNode.cs:103,104 | 시간/산출은 잠정, 첫 해금 조건은 G29 |
| R60 원정/편입 재고 현지 생산·실물 화물 귀환·재정복 초기화 금지 | 구현 | World/AnnexedSettlement.cs:1; World/ExpeditionSite.cs:1; World/ExpeditionTransport.cs:1 | 수량·주기는 잠정. 도보·복구는 G05/G14 |
| R61 우리 합사·필요 면적/과밀·벽/문 성립·뚫리면 먹기 외 중지 | 구현 | Buildings/Ranch.cs:33,48,62; World/Critter.cs:185 | 열린 문/방 경계는 RoomSystem 사용. 독립 울타리는 잠정 대체 |
| R62 우리 축소/삭제·출입 시 자동 관리·설정 보존·자동 복귀 없음 | 구현 | Buildings/Ranch.cs:41; World/Critter.cs:185 | 종 허용은 운반 목적지 필터 |
| R63 생산 자동 준비/직접 채취 적재 상한·수동 위험 취급 | 구현 | World/Critter.cs:167; Buildings/Ranch.cs:106,111 | 바닥 산출은 별도 운반 |
| R64 자동 번식·새끼 야생·굶으면 성장 중지·노령 번식 저하/자연사 | 구현 | Buildings/Ranch.cs:62; World/Critter.cs:128 | 종/기간 수치는 잠정. 구체 성별·임신/알은 R65 |
| R65 질병 중 새 번식 중지·진행 임신/알 보존·중증만 진행 중지 | 부분 | Buildings/Ranch.cs:62; World/Critter.cs:25 | 종 설정의 breed 누적은 있음. 개체 임신·알 상태 구분 없음 |
| R66 직접 포획→결박·실패 취소/피해·길들인 즉시 결박 | 구현 | Units/CommanderHusbandry.cs:159; World/Critter.cs:1 | 덫/체력 임계 선행 없음 |
| R67 한 마리 운반·쓰러짐 무결박·결박 타이머 정지/만료·목적지 변경 | 구현 | Units/CommanderHusbandry.cs:1,178; World/Critter.cs:175 | 운반 중단 시 현재 위치 하차 |
| R68 이름/목록 필터·일괄 포획/운반/도축/방생·제외 사유·개별 취소 | 구현 | UI/GameMenuRanchBulk.cs:27,67,76,120 | UI 소스 경로만 확인, 실제 조작 미검증 |
| R69 먹이통 필터/목표/예약·초과 배출·통간 재분배 없음 | 구현 | Buildings/RanchFacility.cs:52,58,61,70 | 먹이통 이동 내용물은 R70의 원문 충돌 참고 |
| R70 먹이통 이동 시 내용물 유지 | 판단불가 | Buildings/Demolition.cs:79; Buildings/RanchFacility.cs:1 | 자원 원문은 유지, 건물 최신 확정안은 원위치 바닥 배출. 코드는 후자를 따름. 기획 간 충돌로 미구현 집계 안 함 |
| R71 동물 실제 접근 섭식·바닥/창고/밭·정상 우선/썩은 먹이·비독점 섭취 | 부분 | World/CritterBehaviour.cs:46,55,76; Buildings/CritterSpecies.cs:36 | 실제 이동/섭취는 있음. Feeds/Eats에 RottenFood·조리식 없음, 창고도 개별 재고 아닌 전체량으로 선택. 원문의 썩은/조리식·물량별 신선도 누락 |
| R72 포식 추격 무알림·첫 사체 섭취만 알림·먹은 양만큼 해체량 차감 | 구현 | World/CritterBehaviour.cs:99; World/CritterCarcass.cs:1 | 포식 중 사체 운반 시 섭취 중지 |
| R73 수동 도축만·진행 후 취소 금지·강제 중단 생존/결박 재개 | 구현 | World/CritterSlaughter.cs:16,18; Units/CommanderHusbandry.cs:1 | 목표 수는 알림용 |
| R74 도축대 FIFO·종 필터·1대 1구·중단 인계·부패·바닥 일괄 배출 | 구현 | Buildings/RanchButcher.cs:29,44,53,64 | 이동/철거 시 사체 배출·진행 초기화 |
| R75 돌봄 반복·무소모·영구 길들임·실패 후 자동 재시도 차단 | 구현 | World/Critter.cs:159; Buildings/Ranch.cs:110; Units/CommanderHusbandry.cs:1 | 불가종은 효과만, 먹이 우선/시설 호출 존재 |
| R76 동물 경상 자연 회복·투약 당시 손상만 가속·재투약/중복 제한 | 구현 | World/Critter.cs:149; Units/CommanderHusbandry.cs:1 | 중상 수술과 분리 |
| R77 동물약/키트는 의료만 생산·3모드·부패/품질 없음 | 구현 | Buildings/Processor.cs:37,43,105; Core/FoodLots.cs:26 | 정밀 제조량·해금은 미정 |
| R78 수술 전 빈자리/키트 예약·시작 소비·인계 가중 능력·재시도 수동 | 구현 | Units/CommanderHusbandry.cs:50; World/CritterSurgery.cs:38,54,62 | 횟수 위험 가산은 제거됨 |
| R79 수술 성공 완치→회복→복귀·파괴 시 잔여 회복·새 중상 재수술 | 구현 | World/CritterSurgery.cs:62,88,106 | 수술/시설 회복 중에만 배고픔 정지 |
| R80 동물 치료기 내부 급식 없음·대기 중 허기 진행 | 부분 | World/CritterDisease.cs:45; Units/CommanderHusbandry.cs:11,48; Units/CommanderAnimalFeeding.cs:1 | 대기 허기는 진행하나 FeedPatient 작업으로 치료기 급식 가능. 명시적 금지와 충돌 |
| R81 동물 질병 경/중증·증상 뒤 전염·같은 우리/종·치료시설 제외 | 구현 | World/CritterDisease.cs:18,64 | 종별 감수성표 미정이라 같은 종으로 제한한 잠정 구현 |
| R82 배설물 바닥 수거·양/방치 위생·질병 위험·묶음 경고 | 부분 | World/CritterHygiene.cs:15,26,32; Buildings/Ranch.cs:87 | 위험은 주로 면적 대비 수량. 원문이 요구한 방치 시간의 독립 가중이 없음 |
| R83 질병 중 생산 감소/중증 정지·이동/도축·장수↔동물 비전염 | 구현 | World/Critter.cs:167; World/CritterDisease.cs:64; World/CritterSlaughter.cs:16 | 질병 세부 밸런스는 미검증 |
| R84 진료 대기·위독·복귀/집도의 상태와 위험 표시 | 구현 | UI/GameMenuRanch.cs:68,161; Buildings/Ranch.cs:87 | 원문에 잔존한 후유증 ‘벌점’ 문장은 최신 폐기 기준 우선 |
| R85 자원 연계 다이아 6종 | 미구현 | grep 없음(무중생유/낙토/태공/식선/역발산/하멜른); Units/CommanderTraits.cs:8 | D15~D18,D27,D28,D31에서 세부 대조. 중복 요구 행 |
| R86 장수용 약품·약초의 의료 생산 연결 | 부분 | Data/ResourceType.cs:57; Buildings/Processor.cs:37 | 기초/일반/고급 약품 재고는 있으나 약제대 제조법은 동물약/키트뿐. 장수용 약품 생산 경로 누락 |

### 기존 G 추적(재감사 제외)

| 항목 | 판정 | 근거 | 비고 |
|---|---|---|---|
| 첫 돌/도구·시작 식량·씨앗·보존기간/겨울 수지 | 판단불가 | docs/PLAY_CHECK_DECISIONS_1023.md | G02·G12·G28·G30 참조 |
| 고갈 복구·시대 표본·진행물 보호·핵심물 | 판단불가 | 동일 문서 | G14·G06·G51·G23 참조 |
| 달력·세금/행정·첫 석유·도시 식량/수원·최종 비축 | 판단불가 | 동일 문서 | G08·G17·G46·G35·G58·G59·G68 참조 |

### 잠정·미정(미구현 집계 제외)

- 개별 레시피 수량/시간/전력·재료 가격·희귀도·성휘합금 배합·도구 속도 1.0~2.4·재활용 50%·청동 3:1은 밸런스 초깃값. **공정/재질 기능 자체의 부재는 R23/R25/R38에서 별도 집계**했다.
- 냉장 비용/용량/냉각 시간·동물 종 확정(진딧물/누에/거미/벌은 후보)·식성/포식표·성별/임신/알·생산량·수명·시설 해금/약제 레시피·유체 유출/회수 방식은 미정.
- 감로 밭은 임시 구현. 감로 사육 종·생산 확정 전 이를 사육 누락으로 세지 않았다. 잔해 물자/작업량·납세 품목 비중 상한·식중독 기간/세부 증상도 미정.

## 2. 장수·유닛 시스템

| 항목 | 판정 | 근거(file:line 또는 grep 없음) | 비고 |
|---|---|---|---|
| U01 장수 개체 조작·시민 수량·평시 시민 지원 없음 | 구현 | Units/CommanderAnt.cs:1; Core/ColonyPopulation.cs:1; Units/CommanderDuty.cs:1 | 병력 편성과 평시 작업 분리 |
| U02 시작 1~3명·18~30세·관계 없음·기후 의류/나무 장비·시민 0 | 구현 | Core/StartingCandidate.cs:1; Core/CrashLandingStart.cs:1; Core/StartSupplyRules.cs:1 | 후보 표시/필수 작업은 G03/G24, 식량은 G12 |
| U03 완성 주거 정원만 수용·파괴 시 즉시 인구 제거 없음·특별 합류 초과 허용 | 구현 | Core/ColonyPopulation.cs:1; Core/ColonyPopulation.Civilians.cs:1; World/ColonyEvents.cs:1 | 이주 주기/정수 배분은 G33/G38 |
| U04 클릭/드래그 선택·우클릭 이동/자원/공격·A 어택무브 | 구현 | Units/UnitSelectionController.cs:1; Units/AttackMoveController.cs:1 | 실제 입력/카메라 충돌은 미검증 |
| U05 작업표 0금지/1~9·경보·실제 거리/오래된 요청·직접 지시 후 자율 복귀 | 구현 | Units/CommanderDuty.cs:201,211; Units/WorkPriorities.cs:1; Units/WorkerAnt.cs:1 | 16칸 명칭은 G56/G57 |
| U06 무기가 전투 방식·병영 보정 즉시 전환·보유 장비 무료 교체 | 부분 | Units/CommanderEquipment.cs:52,65,88,121 | 무기별 전환은 있음. 본거지 밖 교체는 전면 차단하여 원정 소지 장비 안 교체 기획과 차이 |
| U07 근접/산 분사/방패/지휘 신호·비행 갑옷·지원 오라 비중첩 | 구현 | Units/CommanderEquipment.cs:10,92; Units/CommanderAnt.cs:1 | 세부 공격/사거리/방어 수치는 잠정 |
| U08 성별 50:50·통계 차이 없음·성적 지향/성별 특성·표시 | 부분 | Units/CommanderPersonalState.cs:1; Units/CommanderTraits.cs:8; Units/CommanderPersonal.cs:1 | 성별/암수 출산은 있음. 동성애/양성애/무성애·성별 혐오 특성 enum 없음 |
| U09 지휘 한도·장신구/부상·초과 시 추가 배정만 금지 | 구현 | Units/CommanderAnt.cs:1; Units/CommanderEquipment.cs:95 | 지휘 수식·보정 수치는 잠정. 첫 동원은 G04/G32 |
| U10 정치 행정 실제 작업·성과 누적/감소·징수/민심/이주/재개발·외교 | 구현 | Core/ColonyPopulation.Admin.cs:28,31,36; Buildings/AdminDesk.cs:1; World/DiplomacyTrade.cs:53 | 고정 담당 필수 아님. 인구 규모는 G17 |
| U11 기술 16종에 조사 포함·기술/작업표 구분 | 부분 | Units/CommanderTraits.cs:24; Units/CommanderTalents.cs:14; UI/GameMenuRoster.cs:13 | Count=16이지만 마지막은 Firefighting(소화). 조사 기술 없음 |
| U12 기본 0~20·CA 소수 합·PA≤200 생성 고정·직무 전환/영입 시 보존 | 구현 | Units/CommanderTalents.cs:14,28,124,131; Buildings/PrisonerCamp.cs:1 | 잠정 생성 분포와 별개로 상한/성장 구조 있음 |
| U13 유효 활동 주80/보조20·주제별 연구/제작·공통 예산/PA 초과 환원 | 구현 | Units/CommanderTalents.cs:44,56; Core/CampaignResearch.cs:40; Units/EquipmentRecipes.cs:18 | 보조 미분류는 주 능력만. 감소 기준은 G25 |
| U14 실제 행동만 이력·이동/대기/중복 전투 XP 배제·부하 조건 | 부분 | Units/CommanderTalents.cs:56; Units/CommanderAnt.cs:1; Units/CommanderJoy.cs:114 | 공통 기록 구조는 있으나 모든 활동의 유효량·주/보조 중복 방지를 이 감사에서 증명하지 못함. 개별 호출부 후속 확인 필요 |
| U15 조사 담당 지정·우리/타 세력 평가·오차·상대 별점·부재 중 갱신 중지 | 미구현 | UI/GameMenuManagement.cs:155; grep 없음(평가 담당/조사 기술 처리) | 평가 화면은 미구현 안내 |
| U16 실제 CA/PA 숫자 숨김·종합/분야별 현재 별점·공통 잠재 별점 | 부분 | UI/GameMenuRoster.cs:114; UI/GameMenuManagement.cs:155 | 오히려 CA/PA 실수를 직접 표시. 기술별 활용 비중 표시는 있음 |
| U17 외부 조사 이동/대상/탐색·완료 후 귀환 보고·발각/포로·미완료 무효 | 미구현 | UI/GameMenuManagement.cs:146; grep 없음(조사 파견/보고서 상태) | ScoutPost 영입 파견은 조사 대체 아님 |
| U18 조사 보고 관심 등록·직접 영입 불가·포로 정보 소실/영구 보고 구분 | 미구현 | UI/GameMenuManagement.cs:148 | 보고서/관심 등록 데이터 없음 |
| U19 기본 평진·삼국지풍 7·현대식 6·출전 전 선택/전투 중 변경 불가 | 미구현 | UI/GameMenuManagement.cs:156; grep 없음(진형 전투 보정) | WalkingFormation은 도보 편성 함수명으로 진형 구현 아님 |
| U20 지휘 0/10/20 적성 1개씩 영구·중복 지급 없음 | 미구현 | grep 없음(진형 적성/FormationAffinity) | 레기온 예외는 D25 |
| U21 장수당 개별 공격/방어 3단계 연구·동시 한 건·취소 무환급 | 구현 | Buildings/ResearchLab.cs:52,61,104 | 무기 교체/시설 철거 후 완료 보정 유지 |
| U22 강타·방어태세·산성비·집결·급강하·Q/W·쿨 보존 | 구현 | Units/CommanderSkills.cs:10,41,57; Units/CommanderAdvancedSkills.cs:12,20,28,38; UI/SkillTargeting.cs:1 | 조건/수치는 잠정. 기술10/15 상위 스킬 미정 |
| U23 특성·열정·배타·4슬롯·생성/유전/후천 | 부분 | Units/CommanderTraits.cs:8,30,62,83 | D01~D14에서 최신 원본 감사. 옛 후보 목록은 현행 전체 요구로 안 셈 |
| U24 열정 1~4개·큰 불꽃 제한·학습 속도·부모 열정 약화 유전 | 구현 | Units/CommanderTraits.cs:93; Units/CommanderTalents.cs:131 | 분포/효과는 잠정. PA 상한 증가 아님 |
| U25 기분 요인·붕괴·이탈/파벌 반란·원정/포로/포상 사건 | 구현 | Units/CommanderPersonal.cs:1; Units/CommanderSocial.cs:38,125; Units/CommanderDeparture.cs:1 | 시작 기분·마지막 장수·사망 기분은 G31/G64 |
| U26 우정/라이벌·결투·연인/배우자·파벌·친구 복수 | 구현 | Units/CommanderPersonal.cs:1; Units/CommanderSocial.cs:17,21,110 | 친구 복수는 역린 다이아와 다른 기존 효과. D23 참조 |
| U27 번식 양육실·관계/거리/기분·암수·어린 자녀·근친 연인 차단 | 구현 | Buildings/NurseryChamber.cs:1; Units/CommanderPersonal.cs:1 | 성체화/노화는 G37, 타고난 특성만 유전은 미반영(D07) |
| U28 스카우트 동행 장수/시민·일시 파견·회유 기존 개체 유지 | 구현 | Buildings/ScoutPost.cs:1; Buildings/PrisonerCamp.cs:1 | 연속 실패 구제/첫 방랑자는 G36 |
| U29 포로 몸값/교환·압수 장비 별도 협상·거리 비례 안전 귀환·부상 보존 | 구현 | World/DiplomacyPrisoners.cs:39,50,56,61,116,134; World/DiplomacyTrade.cs:56 | 제3자 포로 구매가 아닌 귀환 처리 |
| U30 일반 음주 허용/간격·소집 중 제외·어린이 금지 | 미구현 | UI/GameMenuManagement.cs:138; grep 없음(Alcohol) | 술 자원/행동 없음 |
| U31 알코올 의존 생성/후천·충동 명령 무시·실제 음주 뒤 해소 | 미구현 | Units/CommanderTraits.cs:8; grep 없음(Alcohol/금단) | G42의 명령 우선순위와 별개의 원인 기능 |
| U32 술 약함/강함 배타·취기/만취/숙취·상태 표시 | 미구현 | grep 없음(취기/숙취/Alcohol) | 수치 미정이어도 상태 시스템 요구는 확정 |
| U33 13 신체 부위·상처/통증/출혈/의식·시력/조작/이동·상실 | 구현 | Units/CommanderHealth.cs:12,46,48,50,60,65 | 두 팔/두 다리, 비행은 장비. 세부 장기는 미정 |
| U34 무약/약품 처치·품질·환자 약품 단계·즉시 완치 아님 | 구현 | Units/CommanderHealth.cs:15,99; Units/CommanderHealthCare.cs:26 | 자가 처치와 전원 쓰러짐은 G11/G55 |
| U35 일반 질병/기생충·환경 저체온/열사병·식중독 증상 | 부분 | Units/CommanderHealth.cs:14; Units/CommanderMeal.cs:12; Buildings/SpaceQuality.cs:54 | 상처 감염/진균·식중독 타이머 일부. 명시적 저체온/열사병 질환·구토 없음 |
| U36 현장 처치·병상 휴식·구조/급식·의식 저하 명령 차단 | 구현 | Units/CommanderHealthCare.cs:13,52; Units/CommanderHealth.cs:36,65 | 의료/운반 직무 분리 |
| U37 장수/동물 공통 위험도→우선순위→거리→대기·진행 치료 완료 우선 | 부분 | Units/CommanderHealthCare.cs:67; Buildings/Ranch.cs:139; Units/CommanderDuty.cs:201 | 장수 위험도/동물 작업은 있으나장수와 동물를 같은 위험 척도로 통합 정렬하는 완전한 큐는 확인 못함 |
| U38 수술 예약/시작 소비·자동/지정 집도·인계 가중·중도 취소 금지/수동 재시도 | 구현 | Units/CommanderHealthCare.cs:260,276,302,313; Units/CommanderHealth.cs:225 | 실제 수술 종류는 절단/재생뿐 |
| U39 대체 팔다리/내부 장기 설치·제거·교체·뇌 교체 금지 | 미구현 | Units/CommanderHealth.cs:225; Units/CommanderHealthCare.cs:263 | SurgeryKind=Amputate/Regenerate. 보철/장기·설치 효과/재고 없음. 상세 품목 수치는 미정 |
| U40 건강 화면/출혈 사망 예측/치료 설정/수술 대기·환자별 위기 알림 | 구현 | UI/GameMenuHealth.cs:1; Units/CommanderHealth.cs:69; Units/CommanderHealthCare.cs:52 | UI 육안/클릭 연결은 미검증 |
| U41 시신 묘비 안치·1구·빈 추모비/고인 중복 금지·꺼내기 후 철거 | 구현 | World/Memorial.cs:1; Buildings/Tombstone.cs:1; Units/CommanderBurial.cs:1 | 자동 소멸/사망 기분은 G63/G64 |
| U42 영토 내 장비/짐 회수·외부 압수·시신 별도·사망 후 예약/담당 해제 | 구현 | World/DiplomacyPrisoners.cs:61,72,94; Units/CommanderSocial.cs:74; Units/CommanderAnt.cs:1 | 병력 후퇴/귀환은 G16/G69 연계 |
| U43 장비 3슬롯·4품질·공방/교역/전리품/보스 경로 | 구현 | Units/CommanderEquipment.cs:8,14; Units/EquipmentRecipes.cs:27; World/EquipmentLoot.cs:1 | 보스 목록/효과 수치 잠정 |
| U44 무기·갑옷·장신구에 더해 별도 도구 슬롯·평상복/기후 적합·복수 도구 | 부분 | Units/CommanderEquipment.cs:8; Units/CommanderPersonalState.cs:1; UI/GameMenuManagement.cs:131 | toolTier/평상복 문자열만. 도구 별도 실물·교환 슬롯/다중 도구 없음 |
| U45 도구 사용·운반물 손 부착·의류/무기 외형·얼굴 감정 | 부분 | Units/KemonoModel.cs:1; Units/KemonoAppearance.cs:1; Units/AntVisual.cs:1 | 새 모델/동작 코드 존재. 장비별 실제 부착·표정·작업점 정합은 씬/에셋/실행 범위라 미검증 |
| U46 쓰러짐 무기 드롭·자동 재장착 없음·수동 실물 회수 | 부분 | Units/CommanderEquipment.cs:65; Units/CommanderSocial.cs:74 | 사망 전리품과 착용 API는 있음. 쓰러짐 전용 무기 실물 드롭/수동 접근 회수 경로 부족 |
| U47 일반 의류/갑옷/도구 마모·0 파괴 알림 | 부분 | Units/CommanderEquipment.cs:23; Units/CommanderPersonalState.cs:1 | 보스 장비만 일부 내구 처리. 일반 무기/갑옷·옷·도구 통일 내구 없음 |
| U48 수리량 비례 최대 내구도 감소·품질 유지·능력별 소모/손실 | 미구현 | Units/CommanderEquipment.cs:14; grep 없음(장비 최대 내구/EquipmentRepair) | 건물 BuildingRepair는 장비 수리가 아님 |
| U49 제작대 수리 큐·수동 담당 변경·진행별 기여/예약·완료 후 재장착 | 미구현 | Buildings/Workshop.cs:13,18; UI/GameMenuManagement.cs:135 | 수리 대시보드만 아닌 장비 수리 작업 자체 없음 |
| U50 수리 일괄 요청/취소·임시 장비·착용자 사망/시설 파괴 보존·대기 사유 | 미구현 | grep 없음(EquipmentRepair/장비 수리 상태) | U49와 연관되나 수명주기·UX 요구 별도 |
| U51 금속 단계·품질 상호작용·재활용·즉시 재질 변환 금지 | 부분 | Units/EquipmentRecipes.cs:11,27; Units/CommanderEquipment.cs:14 | 품질만 구현. 금속 단계/재활용은 R23/R38/R52 |
| U52 성장/감소·활용 이력·소수 진행 저장·기술별 원인 표시 | 부분 | Units/CommanderTalents.cs:17,23,84; UI/GameMenuRoster.cs:114 | 이력 데이터/활용 비중은 있음. 관련 작업 원인 툴팁 전부와 조사 기술은 부족. 실제 저장 검증은 G09/G41 |
| U53 새 두 팔/두 다리 모델·동작·작업점/장비 부착 완성 | 판단불가 | Units/KemonoModel.cs:1; Units/AntVisual.cs:1 | 코드 존재만으로 리그/애니/시각 완성 판정 불가. Unity 금지에 따라 확인 안 함 |
| U54 다이아 의료/식사/근력/후긴·레기온/엑스텐 | 미구현 | Units/CommanderTraits.cs:8; grep 없음(전용 다이아 처리) | D24~D28,D32 참조 |
| U55 일반 장비 교체는 실물 위치까지 이동·탈착품 바닥 배출 | 부분 | Units/CommanderEquipment.cs:65,77 | 현재 전역 보관함에서 즉시 장착/교환하고 벗은 장비를 Items에 넣음. 실제 이동/바닥 배출과 차이 |
| U56 소집 시 짐 바닥·도구 숨김/무기 꺼냄·해제 후 자율 작업 | 부분 | Units/WorkerAnt.cs:270; Units/AntVisual.cs:1; Units/KemonoModel.cs:1 | 짐/작업 전환은 있음. 종류별 소유 도구/무기의 휴대→손 부착 전환과 모든 외형 실제 동작은 미검증 |

### 기존 G 추적(재감사 제외)

| 항목 | 판정 | 근거 | 비고 |
|---|---|---|---|
| 병력/개인 체력·시작 후보·인원/기술 재추첨 | 판단불가 | docs/PLAY_CHECK_DECISIONS_1023.md | G01·G03·G24 |
| 성장 감소 기준·기분/생활/위생·직무 표 | 판단불가 | 동일 문서 | G25·G31·G10·G54·G56·G57·G60 |
| 스카우트 구제·연령·이주/시민 연령·동원/출전/보급 | 판단불가 | 동일 문서 | G36·G37·G33·G38·G04·G32·G05·G61 |
| 자가 처치/전원 쓰러짐·포로/패배·명령 불응 | 판단불가 | 동일 문서 | G11·G55·G50·G53·G42 |
| 미완성품·시신/애도·예약/저장·로켓 탑승/엔딩 | 판단불가 | 동일 문서 | G18·G63·G64·G13·G09·G41·G20·G43·G49·G52 |

### 잠정·미정(미구현 집계 제외)

- 별점/오차/상대 분야·조사 시간/이동수단/위험/보상·최초 평가자 부재·귀환 중 추방 처리·조사 XP는 미정. 평가/조사 시스템 자체는 확정 요구라 U15~U18에서 누락 집계.
- 기술 효과/XP·PA 분포/유전·무기/장신구 수치·붕괴/사회관계 확률·스카우트 비용·번식 호감·치료/질병 기간은 잠정. 기술10/15 상위 스킬·보스 고유 장비 최종 목록·보철별 레시피/장기 성능은 미정.
- 특성 후보 표는 최신 다이아 원본 우선. 원문 하단의 옛 ‘사건으로 성격 변화’·‘9/15종 기술’·‘시신 300초 삭제’와 과거 CA/PA 공개 기록은 현행 규칙이 아니다.
- 종족/외형별 능력 차이·새 외형 종류/이름·생물학적 머리 장식 기능·술 도수/금단·의존 치료는 미정. 새 모델은 소스만으로 완성 여부 판단하지 않았다.

## 3. 건물·방어시설

| 항목 | 판정 | 근거(file:line 또는 grep 없음) | 비고 |
|---|---|---|---|
| B01 1m 격자·경사/평탄/장애물 검사·지하 확장 없음 | 구현 | Buildings/BuildingPlacementController.cs:85; Buildings/RoomSystem.cs:63 | 자유 배치 옛 규칙 대체 |
| B02 장수 선택 없이 청사진·녹/적색·클릭1개/Shift연속·Esc/우클릭 취소 | 구현 | Buildings/BuildingPlacementController.cs:92,298,306 | Shift는 선형 배치, 면 채우기 아님 |
| B03 자원 부족 청사진·부분 예약/배송·전량 도착 후 소비·협동 시공 | 구현 | Buildings/BuildingConstructionSite.cs:32,38,58,67,135 | 시민 차출 없는 장수 작업 |
| B04 대상 우선순위 변경은 기존 예약을 뺏지 않음·시작 전 재질 변경 | 구현 | Buildings/BuildingConstructionSite.cs:67,151 | 재질 변경은 예약 해제/배송분 배출 후 재요청 |
| B05 일시정지 신규 배송/시공 중지·미픽업 예약 해제·진행 보존 | 구현 | Buildings/BuildingConstructionSite.cs:144 | 운반 중 실물/예약 해제 상세 실행은 미검증 |
| B06 시작 전/후 취소 100% 실물 반환·미완공 피해/파괴 취소 처리 | 구현 | Buildings/BuildingConstructionSite.cs:183; Buildings/BuildingBase.cs:1 | G14는 완성 후 철거70%로 구분 |
| B07 철거 현장 작업·시작 기능 중지/내용물 배출·완료70%·취소 복구 | 부분 | Buildings/Demolition.cs:79,115 | 기본 건설 환급은 있음. Processor.CancelCurrent가 철거 때 생산 재료까지 환급하여 ‘소비 생산 재료 무반환’과 충돌 |
| B08 가구 이동 회수/포장/재설치·비용0·회수 후 취소 포장품 보존 | 구현 | Buildings/Demolition.cs:35,93,130 | 주거·벽/바닥·방어시설은 무료 이동 제외 |
| B09 개보수 예약/시작 소비·사용 중지·주문/진행 보존·새100/옛70% | 구현 | Buildings/Demolition.cs:56,115,130; Buildings/BuildingConstructionSite.cs:183 | 주거 재개발과 별도 |
| B10 사용 침대/좌석 대기·환자/포로/입원 동물 시설 비우기 전 금지 | 구현 | Buildings/Demolition.cs:19,24; Buildings/BuildingConstructionSite.cs:41 | 공격 파괴는 별도 |
| B11 주재료 하나/고정 부품·재료별 비교·귀한 부재료 자동 대체 금지 | 부분 | Data/ResourceType.cs:64; Buildings/Demolition.cs:48; UI/GameMenuRenovate.cs:1 | 건축 주재료는 있음. 일반 부품별 대체 필터/필수 재료 체계는 제한적 |
| B12 산업 설비/배관 열전도·단열·내열 적용 | 부분 | Data/ResourceType.cs:64; Buildings/SpaceQuality.cs:65 | 건물 단열·미관만, 산업 열전도/내열 처리 없음 |
| B13 장수 공간 미관 기분·체류/상한·시민 주거 주변 미관 | 구현 | Buildings/SpaceQuality.cs:36; Core/ColonyPopulation.Civilians.cs:1; Units/CommanderPersonal.cs:1 | 별도 스트레스 없음. 개별 시각/효과 균형 미검증 |
| B14 벽/문 방 판정·개인실/숙소·필수/금지 가구·방정보/전체 보기 | 부분 | Buildings/RoomSystem.cs:119; Buildings/RoomRoofs.cs:15; UI/WorkTargetPanel.cs:191 | 자동 판정/지붕·선택 건물의 방정보는 있음. 정착지 전체 색 구분 ‘방 보기’ 대응 UI는 grep 없음 |
| B15 제작대 주문3모드·자동 장수·시민 지원 없음 | 구현 | Buildings/Workshop.cs:16,31,47; Buildings/Processor.cs:72 | 자세한 재고/바닥 출력 차이는 R19/R20 |
| B16 주거 직접 신축/기존 위 교체·보상비 배치 지불·취소 무환급 | 구현 | Buildings/Redevelopment.cs:23,39; Buildings/BuildingPlacementController.cs:82 | 자동 업그레이드 아님 |
| B17 거주 인원 비례 보상·예상 비용/민심/이탈·정치 보정·지연 불만 | 구현 | Buildings/Redevelopment.cs:33,39,55,66 | 보상/지연 수치는 잠정 |
| B18 대기/공사 중 기존 주거/납세 유지·완공 새 정원·취소 기존 기능 유지 | 구현 | Buildings/Redevelopment.cs:84; Buildings/BuildingConstructionSite.cs:163,183 | 임시 거처 없음 |
| B19 행정 시설 실제 업무·여러 장수·시민 직원 배정 없음 | 구현 | Buildings/AdminDesk.cs:1; Core/ColonyPopulation.Admin.cs:31; Units/CommanderDuty.cs:1 | 이름/비용/정원 수치는 미정 |
| B20 창고 자원별 한도+100·10동 상한·압축 저장+50% | 판단불가 | Buildings/Storage.cs:7; Core/ScienceEffects.cs:1; Buildings/BuildingPlacementController.cs:1 | 증설/해금 코드는 있음. 직렬화 자산의 실제 비용/용량까지 스크립트만으로 확정 불가. 숫자는 잠정 |
| B21 밭 자연 성장/자동 파종수확·낚시 전용 건물 없음 | 구현 | Buildings/FarmPlot.cs:1; World/ResourceNode.cs:103; Units/CommanderDuty.cs:348 | 자원 페이지 R58/R59. 씨앗 G28 |
| B22 5무기 훈련장·선형3티어·최고 티어만·비행 장비 T1 | 구현 | Buildings/Barracks.cs:11,13,69,80; Core/ScienceEffects.cs:1 | 병영 보직별 이름과 공격/방어 수치는 잠정 |
| B23 연구소 장수 개인 강화·무기별 적용·다른 장수 병렬/중복 효과 없음 | 구현 | Buildings/ResearchLab.cs:39,61,104; Buildings/Barracks.cs:80 | 과학 공동 연구와 다른 시스템 |
| B24 양육실·스카우트·포로수용소·장수 의료 침상 | 구현 | Buildings/NurseryChamber.cs:1; Buildings/ScoutPost.cs:1; Buildings/PrisonerCamp.cs:1; Buildings/Infirmary.cs:16 | 상세는 장수/자원 표. G36/G55 제외 |
| B25 생활: 침대/식탁·오락 가구·옛 RestRoom 대체 | 부분 | Buildings/Dormitory.cs:1; Buildings/RecreationSpot.cs:1; Buildings/RestRoom.cs:1 | 실제 오락 가구 존재하나 옛 휴게실 타입도 잔존. 단순 파일 잔존만으로 전체 실패 판정 안 함 |
| B26 도축대/먹이통/돌봄/동물치료·우리 성립 | 구현 | Buildings/RanchFacility.cs:28,32; Buildings/Ranch.cs:33; Buildings/RanchButcher.cs:1 | 자원 표 R61~R84 세부 참조 |
| B27 목공/석재/재봉·숯/제련/대장간·제분/착유/도자기·양조/증류 시설 | 부분 | Buildings/Processor.cs:26; Data/BuildingData.cs:1 | 목공/석재/가마/방적/제련 일부만. 전용 대장간·재봉 큐·제분/착유/양조 등 없음 |
| B28 산업: 코크스/전기가공/냉장/온실·탐사/심층 채굴 | 부분 | Buildings/ColdStorage.cs:11; Data/BuildingData.cs:1; grep 없음(DeepDrill/GroundScanner/온실) | 냉장/전력은 있음, 산업 생산 시설 전체는 아님 |
| B29 시추/가스/배관/탱크/정유/화학/충전·티타늄/합금/고급채굴 | 부분 | Buildings/SupplyFacility.cs:11,42,94; Data/BuildingData.cs:1 | 물/원유/정제유만 일부. R29~R33,R38 |
| B30 무기/갑옷 대장간·천/가죽옷 재봉·수리/제작 큐 통합 | 미구현 | Units/EquipmentRecipes.cs:11; Buildings/Workshop.cs:13; grep 없음(EquipmentRepair) | U48~U50 |
| B31 방어시설 장수 협동·유지비 없음·전용 연구소 강화 | 구현 | Buildings/BuildingConstructionSite.cs:32; Buildings/DefenseLab.cs:6; Core/UpkeepManager.cs:1 | 방어비와 시민 운영비 구분 |
| B32 단일 분사탑 지상/공중·가까운 적·아군 제외·공격/체력/쿨 | 구현 | Buildings/AcidTower.cs:24; Save/SaveBuildings.cs:1 | NavMesh/녹색 효과 육안은 미검증, 수치 잠정 |
| B33 범위 분사탑 지상만·범위 피해/주기 | 구현 | Buildings/AreaAcidTower.cs:19 | 공격/체력/범위 수치 잠정 |
| B34 끈끈이: 속박/보스 짧게·무피해·파손/수동 수리 | 구현 | Buildings/TrapPit.cs:23,75 | 재장전3단계 외 자동 회복 없음 |
| B35 가시: 지상 범위 지속피해·8회 파손·수리/연구 | 구현 | Buildings/TrapPit.cs:57,75 | 아군/공중 제외, 원문 수치 잠정 |
| B36 흙벽 통행 차단/방어·산불 방화 | 구현 | Buildings/SoilWall.cs:1; Buildings/Wall.cs:1; Buildings/Wall.cs:37 | 실경로/화재 확산은 미실행 |
| B37 감시탑 편입3곳·월드맵60초 경보/경로·로컬 예고 제외 | 구현 | Buildings/Watchtower.cs:15; World/ColonyInvasion.cs:1 | 감시 수/시간은 잠정, 실제 지도 표시는 미검증 |
| B38 폭약 매설 적 감지·범위피해·1회 소모·최대8 | 구현 | Buildings/MineField.cs:11,16 | 비용/반경/피해는 잠정 |
| B39 보스 광역기가 건물 피해 | 구현 | Boss/AoE/BossCircleAoE.cs:1; Boss/AoE/BossLineAoE.cs:1; Boss/AoE/BossConeAoE.cs:1 | 실제 공격 대상 마스크/에셋 연결은 별도 |
| B40 방어연구4라인×3단계·화력/사거리/내구/함정60초 복구 | 구현 | Buildings/DefenseLab.cs:6; Core/ScienceEffects.cs:1; Buildings/TrapPit.cs:23 | 계수/비용은 잠정 |
| B41 바벨 75% 시공 기여·피해파괴 1회 기초·75%재료/50%작업 재건 | 미구현 | grep 없음(Babel/바벨/기초 기여도); Buildings/BuildingConstructionSite.cs:32 | 협동 시공자 목록이 누적 기여도 기록을 뜻하지 않음. D16 |
| B42 무라마사 단독 제작 걸작·10% 유일작·재활용 시 소멸 | 미구현 | grep 없음(Muramasa/무라마사/유일작); Buildings/Workshop.cs:13 | D19 |
| B43 실제 건물 체력 기본300/단일탑250 및 건설비·모든 시설 시각 완성 | 판단불가 | Buildings/BuildingBase.cs:1; Buildings/AcidTower.cs:7; Buildings/FacilityVisual.cs:1 | 직렬화 값/에셋과 실행 화면은 범위 밖. 소스 기본값만으로 판정 안 함 |

### 기존 G 추적(재감사 제외)

| 항목 | 판정 | 근거 | 비고 |
|---|---|---|---|
| 기초 목공대·첫 연구대·상위 승급·씨앗/도시밭 | 판단불가 | docs/PLAY_CHECK_DECISIONS_1023.md | G02·G29·G28·G58 |
| 기본 생활·오락·위생·수원/전력·소화 | 판단불가 | 동일 문서 | G31·G10·G54·G60·G59·G46·G56 |
| 징집소/정찰초소·시민주거/이주·학당/진료소 | 판단불가 | 동일 문서 | G32·G36·G33·G65·G66 |
| 건물과 무관한 패배·시신/묘비·철거복구·로켓/모듈/파괴 | 판단불가 | 동일 문서 | G01·G11·G50·G63·G64·G14·G20·G52·G69 |

### 잠정·미정(미구현 집계 제외)

- 주거 정원20/50/120·건설비/작업시간·창고100/10동·병영3티어 계수/비용·방어7종 성능/수리비/최대8·방어연구 배율은 잠정. 시스템 존재와 최종 수치 일치를 구분했다.
- 재개발 교체 가능 조합/크기·보상 범위/지연/회복 계수·행정 시설 수치·산업 열 정밀도·유체 파괴 유출·원유 매장 처리 범위·로켓과 바벨 재건 예외는 미정.
- 건물 원문 일부의 ‘구현 대기’는 역사적 상태다. 현재 소스와 직접 대조해 판단했다. 반납 지점 표현·초기 비축더미 여부는 최신 불시착 시작을 우선했다.

## 4. 과학·연구

| 항목 | 판정 | 근거(file:line 또는 grep 없음) | 비고 |
|---|---|---|---|
| S01 정착→증기→석유→전기→원자→미래 6시대 | 구현 | Core/CampaignResearch.cs:56,57 | 시대 이름/등급은 존재. 시대별 콘텐츠 충실도는 S02/S03 |
| S02 석유/원자 시대 연구·차량 석유/항공 전기 배치 | 부분 | Core/CampaignResearch.cs:55,71,80 | TODO로 3/5시대 빈 상태. Vehicle은 여전히2시대. 항공4·미래6 존재 |
| S03 생산 단계 전체 해금·해당 시설/제조법 연구 | 부분 | Core/CampaignResearch.cs:58; Core/CampaignResearch.Resources.cs:1; Buildings/Processor.cs:26 | 표본 연구 일부 추가됐으나 R28/R34/R38~R48의 생산 계통이 빠짐 |
| S04 한 번에 하나·연구소당1장수·여러 연구소 합산·시민 지원 없음 | 구현 | Core/CampaignResearch.cs:174,190,205; Buildings/ScienceLab.cs:28 | 자원 표본과 일반 연구의 동시 진행도 차단 |
| S05 완료 후 자동 다음 선택/반복 없음 | 구현 | Core/CampaignResearch.cs:205 | 완료 후 active=-1, 자원 연구도 별도 선택 |
| S06 같은 시대 핵심 선행만·하위 시대 전체완료 불필요 | 구현 | Core/CampaignResearch.cs:58,157 | 명시적 Prerequisites 검사. 승급 조건 자체는 G29 |
| S07 상위 연구소/시대 벽·낮은 등급 공동 연구 불가 | 구현 | Core/CampaignResearch.cs:157,190,205 | LabWaitReason/Tick 모두 등급 검사 |
| S08 연구 주 능력/주제 보조·주80/보조20·장비/열정/능력 보정 | 구현 | Core/CampaignResearch.cs:40,205; Units/CommanderTalents.cs:56 | 프로젝트 진행 자체는 분할 안 함. 계수/밸런스는 잠정 |
| S09 실제 연구 유지/감소/PA·사회연구도 연구 기술 | 구현 | Core/CampaignResearch.cs:40,205; Units/CommanderTalents.cs:44 | 사회 주제 중 보조지휘 일부. 감소는 G25 |
| S10 생물/기계/군사/사회 4분야 표시/분류 | 부분 | Core/CampaignResearch.cs:10,40; UI/GameMenuResearch.cs:1 | 항목/보조분류는 있으나 정의에 명시적 4분야 필드 없음. 화면의 완전한 분야별 대응 미확인 |
| S11 시민 과세 생산량 연구·주거/시설 성과 연결 | 구현 | Core/ColonyPopulation.Admin.cs:24; Core/ScienceEffects.cs:1 | 구체 사회연구 항목/최종 증가량은 미정 |
| S12 이벤트 대응 연구는 피해 감소만·사전 경고 연구 없음 | 구현 | Core/ScienceEffects.cs:1; World/ColonyEvents.cs:1 | 감시탑 침공 예고는 월드맵 군사 위협의 별도 규칙 |
| S13 현대식6진형 연구 해금·전 장수/레기온도 연구 필수 | 미구현 | Core/CampaignResearch.cs:10; UI/GameMenuManagement.cs:156 | 연구 항목·진형 시스템 없음. 시대/비용 미정은 별도 |
| S14 천기 75%기여→직접 후속20%·한번만·잠금/자동완료 | 미구현 | Core/CampaignResearch.cs:205; grep 없음(천기/Insight/연구 기여도) | 일반 공동 연구 합산은 있어도 기여자/통찰 기록 없음. D20 |
| S15 동물 약/키트 의료만 생산·3모드·부패/품질 없음 | 구현 | Buildings/Processor.cs:37,43,105 | 연구/정확한 제조법은 미정. 자원 R77 중복 요구 |
| S16 로켓 연구·일반 준비+마지막 보스 핵심물·구매 우회 금지 | 구현 | Core/CampaignResearch.cs:83,155,157; Core/RocketCampaign.cs:1 | 상태/탑승·핵심 복구는 아래 G 참조 |
| S17 10시간 이상 한 판·시대당1.5~2시간 달성 | 판단불가 | Core/GameBalance.cs:1; Core/CampaignResearch.cs:58 | 원문도 목표이지 검증 완료 아님. 플레이 금지로 달성 판단 안 함 |

### 기존 G 추적(재감사 제외)

| 항목 | 판정 | 근거 | 비고 |
|---|---|---|---|
| 첫 연구대/상위 승급 재료·이전 도구 표본 반입·첫 석유 전원 | 판단불가 | docs/PLAY_CHECK_DECISIONS_1023.md | G29·G06·G46·G51 |
| 보스 탐지 사슬/핵심 회수/복구·로켓 좌석/가동/15일 준비/연료/재건 | 판단불가 | 동일 문서 | G07·G19·G23·G20·G52 |
| 공세 고정/연속 유입·평시 위기 중지·최종전 비축/경제 | 판단불가 | 동일 문서 | G39·G45·G68 |
| 탑승 명령/발사 틱/승패·미탑승 경고·결과6분류 | 판단불가 | 동일 문서 | G42·G49·G43·G50 |
| 학당/진료소·저장·플레이 시간/성능 검증 | 판단불가 | 동일 문서 | G65·G66·G09·G41·G26·G27 |

### 잠정·미정(미구현 집계 제외)

- 시대별 연구량200/400/800/1500/2800/5000·항목별 자원 비용·상위 연구소 재료/배치·각 산업 연구의 세부 선행은 잠정/미정. G29 승급은 재감사하지 않았다.
- 원자 에너지/레이저/로봇·미래 에너지/가구는 시대 성격으로 제시됐고 구체 항목은 미확정. 이를 개별 확정 연구 누락으로 확대하지 않았다.
- 현대 진형 연구 시대/비용·표본/유적 자료별 연결·로켓 연구 노드/설계도/핵심물 관계·모듈 비용·적 유입 상세·탑승 파괴 피해는 미정(현행 G 결정이 확정한 부분은 G 참조).
- 천기 자동 완료 연쇄/기여/완료 비용·골드 기본 보정·동물 시설/약제 연구 세부는 미정. 천기 확정 전용 효과 부재는 S14에서 집계.

## 5. 특성·다이아 확정 원본

| 항목 | 판정 | 근거(file:line 또는 grep 없음) | 비고 |
|---|---|---|---|
| D01 빨강/실버/골드/다이아 4등급·전체4칸·다이아 개수 별도 제한 없음 | 부분 | Units/CommanderTraits.cs:8,30,62 | MaxTraits=4만 있음. 등급/다이아 자체 데이터 없음 |
| D02 복합 장단점 전체 효용·긍정은 강도·고정/후천과 등급 독립 | 미구현 | grep 없음(TraitGrade/TraitRarity/특성 등급); Units/CommanderTraits.cs:8 | 전체 배정표/출현 확률 미정과 별개로 등급 구조 없음 |
| D03 신체/생활/성격/재능 생성 전용·경험으로 고정 성향 변경 금지 | 부분 | Units/CommanderSocial.cs:46,101,125; Units/CommanderAnt.cs:353; Units/CommanderPersonal.cs:160 | 중상3회 Robust제거/Frail추가·원정10회 Wanderer·구출 ColdBlooded·보스 담력 상승·붕괴 정신력 하락이 남아 최신 금지와 충돌 |
| D04 천재/둔재/기억력·기술+3 재능→학습속도만, PA/상한 불변 | 부분 | Units/CommanderTraits.cs:41,49,56; Units/CommanderTalents.cs:131 | 천재/둔재 학습 배율 있음. Gourmet은 아직 CookingBonus +3, 전체 재능 목록/전환 미완료 |
| D05 신규 생성 1개95%/2개4.5%/3개0.5%·최소1빈칸·부정 필수 아님 | 부분 | Units/CommanderTraits.cs:83 | 현재 count는 30%/50%/20%. 1~3개/비부정강제는 맞지만 확률 다름 |
| D06 시작 후보 다이아 제외·외부/유전 허용·영입/회유 재추첨 금지 | 부분 | Units/CommanderTraits.cs:83; Buildings/PrisonerCamp.cs:1; Core/StartingCandidate.cs:1 | 회유 개체 유지는 있음. 등급 자체 없어 시작/외부 추첨 풀 구분 없음 |
| D07 타고난 특성만 부모 후보로 유전·후천 전문성 비유전 | 부분 | Units/CommanderTraits.cs:88; Units/CommanderSocial.cs:139 | 부모 Has(t) 전체 순회. 생성/후천 출처 구분 없음 |
| D08 부모 후보 무작위+신규 발생·정원·등급 우대 없음 | 부분 | Units/CommanderTraits.cs:83 | enum 순서대로 첫 부모→둘째 부모50% 판정. 전체 후보 무작위 조합 아님. 세부 가중은 미정 |
| D09 중복/배타 금지 | 구현 | Units/CommanderTraits.cs:62,69 | TryAdd/Group/Conflicts. 최신 추가 특성 자체가 없다는 문제는 별도 |
| D10 전문성 생성/후천 획득·미사용/기술 하락에도 영구 유지 | 부분 | Units/CommanderTraits.cs:8; Units/CommanderSocial.cs:139 | 일부 사건 획득은 있으나 전문성 분류/영구성 시스템 없음. 고정 성향 변화를 전문성 구현으로 간주 안 함 |
| D11 반복행동 평생1회·큰 사건 종류별1회·특성별 판정·자동 알림 | 부분 | Units/CommanderSocial.cs:46,125,155; Units/CommanderPersonal.cs:160 | 옛 개별 횟수/알림만. 보스 처치는 반복 확률이며 공통 평생 기회 기록 없음 |
| D12 만석도 기회 소모·무관한 특성 교체 금지·동시 성공 기본기술 우선/동률랜덤 | 미구현 | Units/CommanderTraits.cs:62; grep 없음(획득 후보/평생 판정/trait opportunity) | TryAdd 용량 거부만으로 기회 소모/동시 우선 선택을 구현한 것은 아님 |
| D13 같은 전문성 상위만 동일슬롯 교체·하위 중첩/재추가 금지·고정 제외 | 미구현 | grep 없음(전문성 계열/trait upgrade); Units/CommanderSocial.cs:146 | 옛 성격 단계 이동은 고정 제외 규칙과 반대 |
| D14 삼국지 유지21개(안목만 생성/유전, 나머지20 전문성)·전설16 제외 | 미구현 | Units/CommanderTraits.cs:8; grep 없음(분쇄/함정 경계/향도/안목 특성) | 현 enum에 유지21개 없음. 과거221종/미확정 후보 전부를 누락으로 세지 않음 |
| D15 무중생유: 획득+30%·고갈30초 재탐색·최초량50%·노드당1회·누구나 후속 채집 | 미구현 | grep 없음(무중생유/고갈의 부정); World/ResourceNode.cs:8 | 일반 식물 재생은 이 효과 아님 |
| D16 바벨: 전체시공75%·첫 피해파괴 기초·재료75%/작업50%·직접철거 제외 | 미구현 | grep 없음(바벨/불멸의 초석); Buildings/BuildingConstructionSite.cs:32 | 누적 기여도/기초 이력 없음 |
| D17 낙토: 보유자 파종/관리/수확 주기만 토양/지역 불이익 면제·타인 관여 해제·계절/온도 유지 | 미구현 | grep 없음(낙토/황무지의 축복); Buildings/FarmPlot.cs:25 | 파종자/관리자 이력 없음 |
| D18 태공: 성공 어획5% 희귀묶음+그중10% 장비·현재 단계 제한·품질5/15/50/30 | 미구현 | grep 없음(태공/물밑의 보물); World/ResourceNode.cs:103 | 낚시 일반 Food 산출만 |
| D19 무라마사: 동일 제작자 걸작만10% 유일작·중단재개·종류별1효과·추출/이전 불가 | 미구현 | grep 없음(무라마사/유일작); Units/CommanderEquipment.cs:14 | 일반 makerId는 있어도 특성/유일작 데이터 없음. 효과 목록은 미정 |
| D20 천기: 기여75%·직접후속 미완료 무통찰 중1개·필요량20%·잠금 후 반영/자동완료 | 미구현 | grep 없음(천기/연쇄 통찰/Insight); Core/CampaignResearch.cs:205 | 일회 이력/기여자 없음 |
| D21 무간: 일반 근접 총2타×100%·참경 두타 적용 | 미구현 | grep 없음(무간/무한연격); Units/CommanderAnt.cs:1 | 첫타 사망 후 두번째 처리 미정은 제외 |
| D22 참경: 일반 근접 대상 방어30%무시·스킬 제외·건물/보스 포함 | 미구현 | grep 없음(참경/경계 절단); Units/CommanderAnt.cs:1 | 일반 방어 계산과 다른 전용 예외 없음 |
| D23 역린: 실제아군사망10m→20초 네능력+200%·살해자 우선·탈진60초 -50%·탈진중 재발동 | 미구현 | Units/CommanderSocial.cs:110; grep 없음(역린/수호의 분노) | 친구 복수는60초/친구 쓰러짐/포로·일반 보정으로 별개. 연장20초/120초·탑승대기는 G42만 참조 |
| D24 엑스텐: 일반원거리 사거리+100%·무제한관통×0.9·산탄중앙/연사전체·최대사거리 제한 | 미구현 | grep 없음(엑스텐/X-ten/꿰뚫는 중심); Units/CommanderAnt.cs:1 | 범위 공격/산성비는 관통 특성 아님 |
| D25 레기온: 지휘+50%·삼국지7전부/현대연구·유리한진형×1.5·15m생존병력 가까운순 인계 | 미구현 | grep 없음(레기온/Legion/군세 계승); UI/GameMenuManagement.cs:156 | 인계 후 초과 귀환 포함. 실제 사망 병력 생성 없음 규칙도 미구현 |
| D26 파나케이아: 동일의료자 전과정·일반작업량2배/같은약·영구손상 전체회복·상실 제외·재치료 | 미구현 | grep 없음(파나케이아/Panacea/완전 치유); Units/CommanderHealthCare.cs:1 | 일반 수술 Regenerate와 다른 효과 |
| D27 식선: 동일조리자 전과정 요리→기분+20/1일·재섭취 비중첩/시간 비갱신 | 미구현 | grep 없음(식선/잊지 못할 한 끼); Buildings/Kitchen.cs:19,127 | 식품별 동일 조리자 전과정 추적 없음 |
| D28 역발산: 근력/기본보정 후 운반한도×5·짐/무기/갑옷 무게 감속 면제 | 미구현 | grep 없음(역발산/부동); Units/CommanderStartingSupplies.cs:13 | Mighty×2는 다른 특성. 5배/무게 면제 없음 |
| D29 화룡: 동일제작자 걸작 예술품·휴전/석방/접촉+30%p·성공때만 전달·거절15일 재시도 | 미구현 | grep 없음(화룡/경외); World/DiplomacyTrade.cs:56; Buildings/Decoration.cs:1 | 작품 제작자/교섭 보너스/작품별 재시도 기록 없음 |
| D30 여래장: 현정착지 새불만-30%·추가보유자 절반씩·부재3일 유지·현지 우선 | 미구현 | grep 없음(여래장/장중정세); Core/ColonyPopulation.cs:209 | 일반 행정 완화와 별개 |
| D31 하멜른: 동일보유자 전과정 길들임·동정착지일때 모든아군 공격/이탈방지·떠남/귀환 재적용 | 미구현 | grep 없음(하멜른/Hameln/무리의 신뢰); World/Critter.cs:159 | 일반 영구 Tame만, 길들인 장수/거주지 이력 없음 |
| D32 후긴: 직접평가 완료·오판없는 잠재별점·PA숨김·영구기록·외부귀환보고·기존정보와 구분 | 미구현 | grep 없음(후긴/Huginn/꿰뚫는 안목); UI/GameMenuManagement.cs:155 | 일반 평가부터 미구현 |
| D33 다이아=대응 골드 ‘증가치’×1.5+전용효과·전용숫자는 재증폭 금지 | 미구현 | grep 없음(골드 보정/다이아 보정); Units/CommanderTraits.cs:8 | 정확한 골드 숫자 미정을 이유로 공통 등급/효과 시스템 존재로 간주하지 않음 |
| D34 다이아 툴팁: 이름→분위기문장→전용효과명/실제효과·18개 확정 문구 | 미구현 | UI/DetailTabs.cs:108; Units/CommanderTraits.cs:58; UI/GameMenuManagement.cs:153 | enum/보유특성 텍스트만. 18개 툴팁 원문 데이터 없음 |

### 기존 G 추적(재감사 제외)

| 항목 | 판정 | 근거 | 비고 |
|---|---|---|---|
| 역린 추가사망당 남은시간+20초/상한120초·분노 중 탑승 지시 대기 | 판단불가 | docs/PLAY_CHECK_DECISIONS_1023.md | G42. 일반 명령 우선순위/친구복수 구현과 역린 특성 구현은 구분 |

### 잠정·미정(미구현 집계 제외)

- 등급별 확률·전체 등급표·153개 초안/8개 보류·전문성별 획득 조건/확률/상위계열표·재능 학습배율·골드 기준 수치.
- 유전50%는 잠정. 양부모 중복 가중·정원 초과 추첨·신규 발생률·자녀 무작위 전문성 포함 여부는 미정.
- 태공 보물표/가치·무라마사 고유효과 목록·천기 자동연쇄/기여·무간 첫타 사망·역린 최종합산·엑스텐 다른 투사체/장애물/오사·레기온 합산/동거리/반올림은 미정.
- 파나케이아 치료 후유증 목록/재치료 기준량·화룡 재시도 단위/확률상한·여래장 복수부재/사망 잔여·하멜른 기타 원인·후긴 별점 해상도/비교기준 변경 처리는 미정.
- **철회:** 다이아2개 제한/초과 전량재추첨, 생성 항상3개, 고정 성격 후천변화, 무간3회누적, 낙토 타인인계 유지. 이를 구현 후보로 되살리지 않는다.

## 우선 구현 후보 Top 10

G01~G69 밖의 차이만 대상으로, 첫 플레이의 생존/첫 제작 경로 → 성장/중반 순서다. 순위는 구현 승인이나 수치 확정이 아니다.

| 순위 | 후보 | 영향·범위 |
|---|---|---|
| 1 | **실제 접근 가능한 식사/신선도와 금지된 청결 보정 정리** — R07/R08 | 막힌 창고 식량 원격 소비·창고 우선순위 소비를 바로잡고 바닥 마감 식중독 보정을 제거. 첫 식사/생존 규칙부터 일치시킴 |
| 2 | **장수용 약품의 의료 생산 경로** — R86/U34 | 약품 재고 enum과 실제 의료 생산을 연결. 동물약으로 자동 대체하지 않으며 미정 제조량은 별도 결정 |
| 3 | **돌 이후 청동 도구/철 채굴의 최소 생산 사슬** — R23~R25/R38/S03 | 광석→제련→도구 제작→본격 채굴을 연결. G06 표본·G02 첫 돌은 재설계하지 않고 이후 재질별 단계 누락을 해결 |
| 4 | **고정 성향의 사건 변형 중단** — D03 | 중상·보스·원정·구출·붕괴가 시작 장수 성격을 임의로 바꾸지 않게 최신 확정안 적용. 후천 전문성과 분리 |
| 5 | **특성 생성 95/4.5/0.5% 및 유전 출처 구분** — D05/D07/D08 | 시작 후보와 첫 자녀부터 적용되는 큰 확률 차이. 전문성을 부모 고정 특성처럼 유전하는 문제를 방지 |
| 6 | **기초 도구 실물/마모·장비 교체·최소 수리 경로** — U44/U47~U50/U55 | 시작 도구부터 재료별 제작·사용·파괴·수리를 연결. 실제 위치에서 장착하고 탈착품을 바닥에 보존 |
| 7 | **시작 지역 작물/식재료와 기본 요리의 첫 묶음** — R13/R15/R43~R48/R51 | Food 추상값에서 지역 식재료·파종·간단 레시피·식단 선택으로 이어지는 최소 구간. 미확정 사육 종을 임의 확정하지 않음 |
| 8 | **생산 목표 재고/실물 배출/출력 정지 일관화** — R19/R20/R22/B07 | 바닥·진행분을 포함해 중복 생산을 막고, 일반 가공 바닥 배출·미완성품 보존·철거 생산환급 모순 해결 |
| 9 | **석유 산업의 유한 매장/작업자/용기 및 시대 연결** — R29~R33/S02/S03 | 기존 첫 석유 G46 위에 유한 매장량·정유 담당·원유/가스 운송 순환과 비어 있는 시대 콘텐츠를 연결 |
| 10 | **조사 기술/평가의 최소 완결 경로·PA 숫자 비공개** — U11/U15~U18/U52 | 소화를 기술 수에 포함한 오류를 정리하고 담당 평가→별점→외부 조사/귀환보고의 기반 마련. 별점/오차 등 미정 수치는 먼저 결정 필요 |

- 진형/다이아 18종은 확정 요구지만 기본 생존·제작·의료·성장 경로 다음 후보로 둔다. 전용 효과 구현 전 골드 수치·등급표 등 미정을 임의 확정하지 않는다.

