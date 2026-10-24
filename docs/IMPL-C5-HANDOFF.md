# IMPL-C5 인계 (2026-10-24)

상태: **소유 범위 연결·검사 작성 완료, 통합 실행 대기. G28 고정 키/저장은 부분 구현.**
Unity·MCP·컴파일·검사 실행·Notion·git commit/stash/push·prefab 수정 없음. 원본 프로젝트 접근 없음. 기존 staged B5~B10 변경 보존. C5 변경은 unstaged 소스 8개, 신규 검사/본 문서, 보드 첫 줄이다.

## G별 완료 / 부분 / 미구현

| G | 상태 | C5 구현 |
| --- | --- | --- |
| G46 | C5 연결 완료·실행 미검증 | Smelter 철/구리/강철 인덱스 0/1/2 보존 후 SupplyRecipes.For(kind)를 병합. 증기 발전/시추/정유/충전 건설 메뉴 및 FillFuelDrum 명령 연결. |
| G58 | C5 연결 완료·실행 미검증 | 식량 탭 PlanterBed/StarterMushroomBed 추가. WorkTargetPanel 파종재 1로 Plant 호출, 이미 심었으면 버튼 비활성. |
| G59 | 부분 — C1 작업 처리 대기 | 배관 탭 ManualWell/WaterPump/FluidPipe/FluidTank. 취수 지시는 가까운 장수부터 StartService(supply, Hauling) 시도. 물 버퍼의 전량을 Unload(Water, amount)로 바닥 반출. UI가 WorkManual을 직접 호출하지 않음. |
| G14 | C5 연결 완료·실행 미검증 | 외교 월드맵 해금 전에는 기초 자원 캐러밴 화면으로 진입. 해금 후에도 별도 구매 버튼 제공. SupplyPrice가 양수인 전 품목에 1/10개 구매, TryBuySupply 사용. 방문 전에는 비활성, 방문 만료 후 클릭은 API에서 재검사. 기존 문명 거래 유지. |
| G35 | C5 연결 완료·실행 미검증 | 온대/한랭/열대습윤 선택 및 후보 복장 표시. 기존 CommanderRoster의 Clothes(biome) 호출도 현재 세션 climate를 적용하므로 실제 지급 문자열에 반영. 온대는 종전 바이옴 복장, 한랭은 보온, 열대습윤은 통풍. |
| G28 | 부분 — 고정 키/저장 미구현 | 불시착 Apply에서 서로 2m 이상 떨어진 보행 가능한 야생 작물 3개 보장, 40m 이내. ResourceNode + FarmSeedSource.Configure 연결. 기존 작물 중 사막은 가뭄 감쇠가 적은 Honeydew, 나머지는 Fungus로 연결(새 작물/밸런스 추가 없음). MapGenerator 생성 자연 식량에도 종자 출처를 붙이며 기존 명시 출처·느슨한 화물·낚시 노드는 보존/제외. |
| G65/G66 | C5 메뉴 확인 완료·실행 미검증 | 기존 School/CitizenClinic 보존, 빠진 CivicPublicService를 마을 탭에 추가. |

## 수정 파일

Assets/_Project/Scripts 기준:
- UI/BuildScreen.cs
- UI/WorkTargetPanel.cs
- UI/GameMenuDiplomacy.cs
- UI/GameMenuCrashStart.cs
- Buildings/Processor.cs
- Core/CrashLandingStart.cs
- Core/StartingCandidate.cs
- Map/MapGenerator.cs

기타: AgentScripts/ImplC5Checks.cs, docs/IMPLEMENTATION_BOARD.md 첫 줄, 본 문서.
Save/*·Units/*·World/* 등 소유 밖 소스는 수정하지 않았다. 새 저장 버전/필드도 추가하지 않았다.

## 연결 요청 — 정확한 파일 / 메서드 / 의도

1. **C1 / Units/CommanderWork.cs::StartService·TickService, CommanderDuty.cs::AutoCandidates**
   - ManualWell의 Hauling 허용, 실제 접근·도착 후 WorkManual(seconds) 호출. 현 StartService는 Hauling+PowerNode만 허용하여 C1 병합 전 C5 버튼은 실패 안내를 표시한다.
   - 자동 취수의 수원/수량/용량 요구 정책은 B10 인계 1번 그대로. 서비스 대상 기존 저장 경로의 중복 생산 없는 복원 확인.
2. **G28 / Buildings/FarmSeedSource.cs::Key·Configure (C5 소유 밖)**
   - 위치 문자열 대신 생성 고정 키 필드/API 필요. 예: Configure(FarmCrop crop, string key), Key는 지정 키 우선, 구데이터만 기존 위치 키 호환.
   - Core/CrashLandingStart.cs::CreateWildCrops는 `start:{mapSeed}:{biome}:{slot}`를 전달하도록 후속 연결.
   - Map/MapGenerator.cs::SpawnObjects는 `terrain:{spawnSeedBase}:{xOffset}:{zOffset}:{x}:{z}:{spawnObjectIndex}:{childNodeIndex}`처럼 재생성 가능한 키를 전달. GetEntityId 같은 런타임 ID는 저장 키로 쓰지 말 것.
   - C5는 금지된 FarmSeedSource 파일을 수정하거나 이름/위치로 Key 변경을 우회하지 않았다. 위치 변경 시 재채집 가능한 기존 문제는 아직 남음.
3. **G28 / Save/SaveDtos.cs::ResourceNodeDto, Save/SaveSnapshot.cs::Node·노드 복원**
   - 종자 crop/고정 key를 v19 추가 필드로 저장(기본값 호환), 동적 `new:` 자연 노드 생성 시 FarmSeedSource를 붙여 Configure 복원.
   - 시작 야생 작물은 SaveCatalog.Initialize 이후 Apply에서 생성되므로 `new:` 경로로 저장되며, 현재 로드 경로는 ResourceNode만 재생성한다. 이 연결 전 로드 후 시작 작물의 종자 출처가 사라짐.
   - FarmSeeds.collected와 동일 키로 복원하여 로드 후 중복 종자 획득을 방지. 기존 위치 키 수집 기록 이관도 검토.
4. **통합 검증 / Claude**
   - 소유 범위 제한 때문에 graphify update는 실행하지 않음. 병합 후 `graphify update .` 수행. query가 바꾼 cache stamp는 원래 index 내용으로 복원하여 그래프 diff 없음.
   - README는 이번 소유 목록 밖이므로 변경하지 않음. 통합 후 실제 구현 상태·새 건설 항목/기후/초기 캐러밴 조작법 반영.

## 검사 목록 / 실행 방법

AgentScripts/ImplC5Checks.cs: `public static async Task<string> Run()` → 결과 문자열, 기존 RunChecks.cs 호환 `Main() => Run()` 제공. 실패 시 예외, 성공 시 PASS n. **작성만 했으며 PASS 결과 없음.**
- Play 중 전용 검사용 세션에서 RunChecks로 실행. 현재 게임을 새 게임으로 교체한다.
- 임시 SaveStorage.RootOverride, 자동저장 끄기, 설정·난수·배속 복원. Stockpile 가정 없음. 필요한 Storage/재배 화분/탱크/충전기는 검사에서 직접 생성.
- 제련 기존 3개 인덱스 및 뒤 SupplyRecipes 순서 보존.
- 13종 건설 항목 각 1개, 자연 수원/유전 메뉴 제외.
- 전 바이옴 한랭/열대 복장, 1명 숲/2명 사막 불시착 실제 보온복·비축더미 없음.
- 시작 야생 3개 접근 가능·작물 출처·개체별 종자 1회.
- WorkTargetPanel 실제 명령 분기: 파종 1회 소비, 물 7 전량 바닥 반출, 정제유 10+드럼통 1 충전 1회(중복 호출 방지).
- 월드맵 해금 전 Diplomacy에서 구매 버튼 접근, 방문 중 실제 UI 구매 2회 비용·바닥 공급 확인.
- 수동 우물 실제 작업/고정 키 이동 후 중복 방지/종자 출처 save-load는 미구현 연결에 종속되어 이 검사에서 통과 판정하지 않는다.

정적 확인: enum 참조 BuildingKind 118개·ResourceType 32개·MapClimate 3개·FarmCrop 2개·CommanderJobs 8개의 정의 존재 확인. 신규 호출 API는 rg로 선언·접근성 확인. 변경 파일 중괄호 균형 및 git diff --check 확인. 검사 코드에서 internal HomePosition 참조를 공개 WorldMapManager.HomePosition으로 교체.
이 확인은 컴파일·런타임·UI 육안 검증을 뜻하지 않는다.

## 남은 위험

- G28 생성 고정 키와 동적 종자 출처 복원은 **미구현**. C1 취수 처리는 다른 묶음 병합 필요. 해당 연결 전 전체 공급/종자 체인 완료로 판정하지 말 것.
- 기존 FarmCrop 3종 범위에서 바이옴 작물을 연결했으며 신규 바이옴 전용 작물/아트는 만들지 않음. 야생 식량 1씩 3개는 시작 7일 보장 자원과 별도.
- 시작 작물 3개 지점 확보 실패 시 시작 오류를 표시한다. 실제 모든 맵 시드·크기·바이옴 플레이 검증 필요.
- UI는 기존 MenuLayout/MenuTheme 및 스크롤 재사용. 실제 화면·다양한 화면 폭·버튼 클릭·캐러밴 만료 동작은 미검증.
- C5 검사도 컴파일/실행 미확인. Unity 재개 후 기존 ImplB10/ImplB7/ImplA2/SaveRoundtrip 회귀 및 C5 실행 필요.
- 카카오톡 완료 알림 도구가 제공되지 않아 전송하지 못함.