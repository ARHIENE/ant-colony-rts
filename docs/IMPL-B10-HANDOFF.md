# IMPL-B10 인계

2026-10-24 재개 작업 반영. **소유 범위 구현·검사 작성 완료, 통합 연결 대기. G 전체 완료 판정은 아직 아님.**
원본 프로젝트 접근, Unity 실행·MCP·캡처, Notion, commit/stash/push, prefab 수정은 하지 않았다.
이번 worktree의 기존 staged 변경(B6 및 다른 묶음)은 보존했다. B10 패치는 `git diff`의 unstaged 변경과 신규 `.cs.meta`이며, 기존 staged 변경을 B10 변경으로 함께 가져가지 말 것.

## G별 상태

| 항목 | 상태 | 구현 내용 / 남은 부분 |
| --- | --- | --- |
| G35 | 부분 — 시작 공급·기후 데이터 구현 | 기존 6종 바이옴 이름을 결정문에 대응. `StartingEnvironment.Create`를 불시착 자원 배치에 연결. 식량·목재·돌·구리/주석/철광석·석탄·파종재 및 수원·유전을 40m 안 보행 가능 지점에 배치. 한랭/열대습윤 기후의 날씨 변형·저장 추가. 기후 선택 UI와 기후별 복장 표시는 미연결. 지형 메시/아트 재제작은 하지 않음. |
| G58 | 부분 — 독립 재배 구현 | 도시 전체 밭 금지 제거, 바닥 타일이 있는 칸의 노지만 금지. 신규 `PlanterBed`/`StarterMushroomBed`는 처음부터 해금·시민 배정 0. 파종재 1 소비 후 3일(450초)마다 장수 1일분 식량 4 생산, 1인당 3칸. 시작 식용 자원 7일분(장수당 28)·파종재 3 보장. FarmPlot 수정 없음. 건설 목록·파종 명령 UI 미연결. |
| G59 | 부분 — 수원·취수·배관 구현 | 얕은 수원/사막 지하수/동굴 용출수 보장. 우물 수동 작업 누적 5초당 물 10, 펌프·배관·탱크 초기 해금. 버섯 1칸 하루 물 1, 급수 없으면 성장 정지, 이미 지불한 급수 시간 저장. 물을 바닥으로 반출 가능. 수동 우물 장수 작업·선택 UI 연결 필요. 기존 B4 세면 물 1 소비는 유지. |
| G46 | 부분 — 발전·시추·정유·충전 구현 | 손발전 100W, 시추 100W, 정유 200W. 석탄 증기 발전→전선→시추/정유 전원, 유전 필요, 배관 직결로 용기 없이 가동. 전선 구리, 배관 구리, 탱크/충전기 강철의 고정 비용. 드럼통 강철 5·가스 용기 강철 10 레시피 데이터 제공. 정제유 10+빈 드럼통 1→충전 드럼통 1 API. Processor 레시피 병합·건설/선택 UI·반출 연료 사용처 미연결. |
| G14 | 부분 — 철거·계절 방문·구매 API 구현 | 기존 70% 고정 재료 반환 재사용, 유체 시설 철거 시 내용물 전량 바닥 반환. 자연 수원·유전 철거 차단. 매 계절 마지막 날까지 무작위 방문이 없으면 90초 캐러밴 보장, 계절 키·잔여 시간 저장. 목재/석재/광석/금속/석탄/유황/특수/부품/파종재 반복 구매 API. 기초 목재·석재 세금과 도보 원정은 타 담당 연결 요청, 캐러밴 구매 UI 미연결. |

수원·유전은 근처 4m 취수/시추 조건이며, 배관을 연결하는 노드로 취급하지 않는다. 배관망은 맞닿은 유체 설비/배관을 순회하며 탱크와 설비 자체 버퍼를 쓴다. 탱크 혼합 금지, 단절·철거/비활성화를 즉시 반영한다.

## 이번 재개에서 보완한 부분

- 기존 구현을 재작성하지 않고 검사 시작 후보의 특성만 고정하여 무작위 작업 불가로 시작 검사가 실패하는 것을 방지.
- 수원/유전이 끊어진 배관을 대신 연결하던 경로 제거 및 자연 지점 철거 차단.
- 발전기 Update에서 일시정지·로딩·게임 시작 전 연료 소비 방지.
- 노지 포장 판정을 중심점에서 전체 점유 칸으로 보완(넓은 밭 가장자리의 포장도 차단).
- 캐러밴에 유황·특수·부품 공급 추가(용광로 등의 특수 비용과 후반 부품 고갈 복구 경로).
- 보행 경로, 급수 선지불 저장, 자연 지점 배관 우회, 충전 용기 소비/중복 방지, 정지 중 연료, 반복 필수 자원 공급 검사 추가. 신규 스크립트 9개의 `.meta` 작성.

## 수정 파일 — B10 전용 목록

아래 경로는 모두 `Assets/_Project/Scripts/` 기준(검사·문서 제외).

- 신규 Buildings: `SupplyFacility.cs`, `FluidNetwork.cs`, `StarterCropBed.cs`, `SupplyBuildingTemplates.cs`, `StartGroundRules.cs` 및 각 `.meta`.
- 신규 Core: `StartingEnvironment.cs`, `StartSupplyRules.cs` 및 각 `.meta`.
- 신규 Data/World: `Data/SupplyRecipes.cs`, `World/CaravanSupplies.cs` 및 각 `.meta`.
- Buildings 수정: `BuildingPlacementController.cs`, `EarlyConstruction.cs`, `PowerNode.cs`, `PowerGrid.cs`, `Storage.cs`, `Demolition.cs`.
- Core 수정: `Biome.cs`, `CrashLandingStart.cs`(자원 배치 호출 1줄), `GameBalance.cs`(손발전 수치), `GameBootstrap.cs`(로드 실패 복구 시 기후), `GameOptions.cs`, `ResourceManager.cs`(물/신규 자원 보유량·용량 저장).
- Data/Map/World 수정: `Data/BuildingData.cs`, `Data/ResourceType.cs`, `Map/WeatherSystem.cs`, `World/ResourceNode.cs`(주석광석 도구 조건), `World/ColonyEvents.cs`.
- Save 수정: `SaveDtos.cs`, `SaveBuildings.cs`, `SaveCatalog.cs`, `SaveSnapshot.cs`, `SaveSystem.cs`, `SavePreflight.cs`, `SaveValidator.cs`. **버전 v19 유지**, enum은 뒤에 추가, 기존 저장은 기후 온대/빈 공급·재배 상태/연료 0 기본값.
- 검사/문서: `AgentScripts/ImplB10Checks.cs`, 이 문서, `docs/IMPLEMENTATION_BOARD.md`.
- graphify 갱신 산출물은 별도 자동 생성 변경이며 게임 구현 패치와 구분.

## 연결 요청 — Claude/해당 담당자가 병합 시 처리

1. **수동 우물 장수 작업(Units 금지 범위)**
   - `Units/CommanderWork.cs`의 `StartService(BuildingBase, CommanderJobs)` 허용 조건과 `TickService(float)`의 운반 작업 분기: `SupplyFacility.Kind == ManualWell`을 허용하고 도착 후 `WorkManual(seconds)` 호출. 벽 너머 원격 작업이나 별도 무료 자동 틱을 추가하지 말 것.
   - `Units/CommanderDuty.cs`의 `AutoCandidates(CommanderJobs)` 운반 작업 목록(기존 쳇바퀴 열거 옆)에 수동 우물 작업을 추가. 수원 부재·탱크 용량/필요 수량을 고려해 끝없이 물을 생산하지 않게 작업 요구 정책을 연결.
   - 저장의 service 대상은 이미 BuildingBase 키를 보존한다. 작업 연결 후 수동 작업 중 실제 save/load 검사 추가 필요.
2. **기초 제련 레시피(Processor 금지 범위)**
   - `Buildings/Processor.cs::RecipesFor(BuildingKind)`의 Smelter 배열 **기존 항목 뒤**에 `Data/SupplyRecipes.For(kind)`를 합칠 것. 기존 주문은 정수 인덱스로 저장되므로 철/구리/강철 순서를 변경하지 말 것.
   - 청동·드럼통·가스 용기는 이 연결 전 게임에서 제작 불가. `Workshop.cs` 수정은 현재 필요 없음. FilledFuelDrum 사용처가 생기면 컨테이너 회수 규칙과 함께 별도 연결.
   - B6 연구/도구 담당: 시작 석재→돌 도구→구리/주석 채굴→청동 연구/제련 경로 및 용광로 최초 건설 비용(현재 기존 soil80+special5) 접근성을 통합 검증. B10은 CampaignResearch/Workshop/Processor를 수정하지 않았음.
3. **건설·선택 UI(이번 소유 범위 밖)**
   - `UI/BuildScreen.cs`의 `Tabs`에 PlanterBed/StarterMushroomBed, ManualWell/WaterPump/FluidPipe/FluidTank, SteamGenerator/OilDrill/OilRefinery/FuelCharger를 해당 식량/배관/전력 탭에 추가. WaterSource/OilDeposit는 자연 지점이므로 목록에 넣지 말 것.
   - `UI/WorkTargetPanel.cs::LateUpdate/Act`: `StarterCropBed.Plant()`, 수동 우물 작업 지시, `SupplyFacility.Unload(Water, amount)`, `FillFuelDrum()`에 접근 제공. 물 취수→바닥 물 운반→기존 HygieneFixture 소비가 실제 사용자 입력으로 연결되어야 함.
   - 화분/버섯은 초기 무연구이며 시민 0에서도 건설 가능. FarmPlot 연결은 필요하지 않음.
4. **교역 UI(침공·협상 시스템 수정 없음)**
   - `UI/GameMenuDiplomacy.cs::Diplomacy(Civilization)`는 `Available`(월드맵 해금) 검사로 조기 반환한다. 초기 캐러밴 구매는 이 제한과 무관하게 `ColonyEvents.CaravanPresent` 확인 후 `SupplyPrice(type)` 표시·`TryBuySupply(type, amount)` 호출 경로 제공.
   - 기존 `OpenTrade/TradeScreen`은 문명 TradeOffer용이며 B10 확장 재료 구매를 자동으로 처리하지 않는다. 기존 J 화면만 안내하면 초반 구매가 막힘.
5. **기후 선택 및 G14 다른 소유 범위**
   - `UI/GameMenuCrashStart.cs::DrawCrashStart`에 `NewGameOptions.climate` 선택 연결. `Core/StartingCandidate.cs::Clothes(MapBiome)`는 아직 바이옴만 보므로 기후 표시/적응 복장을 담당자와 연결.
   - `Core/ColonyPopulation*` 납세 담당: 기본 목재·석재 세금 공급 연결. `World/WorldMapExpeditions.cs`/`Expedition*` 담당: 도보 자원 원정과 반복 공급 검증. 이번 작업은 해당 파일을 건드리지 않았음.

## 검사 목록 및 실행 상태

`AgentScripts/ImplB10Checks.cs`는 기존 B1 형식의 `public static async Task<string> Main()`, 실패 예외/성공 `PASS n`. **작성만 했으며 실행 결과 없음.** Claude가 Unity Play에서 RunChecks로 실행해야 한다.

- 1명 숲/2명 도시 불시착 새 게임, Stockpile 없는 시작. 검사 객체(우물·탱크·밭·전선·발전기·시추·정유·충전기)는 검사 안에서 직접 생성. 임시 SaveStorage.RootOverride·자동저장 끄기·난수/설정 복구 사용.
- 모든 바이옴 × 장수 1/2 공급량, 시작 7일 식량·3칸 파종재, 40m 및 NavRoute.Reachable, 수원 종류, 도시 포장 칸만 밭 금지, 초기 해금·시민 배정 0.
- 한랭 강우/폭풍 제거, 열대습윤 눈/눈보라 제거(날씨 가중치 100개 표본).
- 수동 취수 2+3초/중간 JSON 복원, 수원 부재·NaN/Infinity·혼합 유체 거부.
- 3칸 3일 수확·물 9 소비·건조 정지, 하루 선지불 급수 JSON 복원 후 중복 지불 없음.
- 100W/100W/200W, 외부 소비자만 있는 망의 발전 요구, 정전·석탄 증기 전력, 용기 없는 시추/정유, 배관 단절·유전 우회 차단·매장지 부재, 정지 중 연료 미소비.
- 드럼통 강철 5·가스 용기 강철 10, 빈 용기 없을 때 정제유 손실 없음, 정제유 10+드럼통 1 충전·재실행 중복 방지.
- 철거 목재 10→7 반환 및 완료 중복 호출 시 추가 반환 없음, 자연 수원 철거 차단.
- 계절 보장 중복/JSON 복원/다음 계절 재방문, 강철·유황·특수·부품 구매, 만료·잘못된 거래 거부.
- 실제 TrySave/TryLoad로 물·기후·발전 연료·시추 진행/버퍼·재배 성장/선지불 급수·캐러밴 키·시작 자연 식량 보존(아직 미실행).

정적 확인: `git diff --check` 통과(줄바꿈 LF/CRLF 안내만 있음). 변경 C# 34개에서 enum 참조 138개와 관련 메서드 이름 28개의 소스 존재 확인, 중괄호 수·신규 스크립트 meta·금지 경로의 unstaged 변경 없음 확인. 이는 오버로드/접근성/Unity 생명주기 검증이나 컴파일 통과를 뜻하지 않는다.
`graphify query` 후 `graphify update .` 실행. 전체 그래프 자동 갱신 및 community 이름 재배정 안내가 있었으며, LLM 재라벨링은 하지 않았다.

## 남은 위험·확인 필요

- **컴파일·Play·RunChecks·실제 save/load 미검증.** 검사 성공 숫자를 기록하지 말 것. 검사는 현재 게임을 새 게임/로드로 교체하므로 전용 검사용 세션에서 실행.
- 현재 UI/Units/Processor 연결 없이는 전체 플레이 공급 루프가 완성되지 않는다. 이 문서의 '부분'을 완료로 바꾸기 전에 위 연결과 런타임 검증 필요.
- 증기 400W, 석탄 1/10초, 펌프 100W, 펌프/시추 10·정유 1/5초, 탱크 100, 확정값 외 건설 비용/청동 비율/교환가는 잠정값. G46 확정 전력과 용기 비용만 확정 수치로 취급.
- 포장은 현재 `FloorTile`의 점유 칸으로 판정한다. 향후 도시 지형이 독립 포장 마스크를 제공하면 `StartGroundRules.OpenFieldAllowed`에 그 판정을 함께 연결해야 함.
- 수원·유전은 런타임 `SupplyFacility` 건물 형태로 저장한다. 자연 지점에 별도 고갈량·지하수 재충전·컨테이너 압력 시스템은 없음. 기존 건물 대상 판정이 자연 지점을 어떻게 취급하는지도 통합 플레이에서 확인 필요.
- 추가 자원 enum은 기존 뒤에 붙였고 Water를 보유량 배열에 포함했다. 다른 worktree의 ResourceType/BuildingKind 추가와 병합할 때 기존 번호를 보존하고 배열 확장 저장 회귀 검사를 실행할 것.
- 카카오톡 완료 알림 도구가 이 세션에 없어 전송하지 못함.
