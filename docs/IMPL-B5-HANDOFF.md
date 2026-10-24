# IMPL-B5 인계 (2026-10-24 재개 완료)

## 현재 판정
- `log.md`·`AGENTS.md`·공동 보드·결정 원본과 staged/unstaged diff를 확인했다. 직전 코드가 핸드오프보다 앞서 있었으며, 완료된 기능을 다시 만들지 않았다.
- 아래의 완료는 **코드 구현 기준**이다. Unity 실행·컴파일·RunChecks·화면·실제 파일 save/load는 수행하지 않았다. G48은 금지 파일 연결이 남아 부분 완료다.
- 작업 범위는 이 worktree뿐이다. 원본 프로젝트 접근, Unity/MCP/Notion/카카오, prefab 수정, commit/stash/push, SAVE 일지 작성은 하지 않았다.
- 기존 index에는 B6 등 다른 묶음의 변경도 포함되어 있다. 전체 diff를 B5 소유로 취급하거나 기존 staged 변경을 지우지 말 것. 이번 재개에서도 기존 변경을 보존했다.

| 결정 | 상태 | 구현 내용 |
| --- | --- | --- |
| G05 | 구현 완료·실행 미검증 | 월드맵 도보 편성, 차량/징집소 없이 장수 1명·병력 0명 출발. 첫 보스 편도 1일 이내 거리 계산, 왕복+현지 1일+예비 1일 권장 보급, 병력 포함 하루 2끼 소비, 소지 보존식/화물 소비, 귀환 화물 여유 표시. |
| G48 | 부분 | 편성별 기본 켜짐 설정·저장·기존 징집 UI 토글·`HomeDefenseReturn.Allows` 제외 조건 구현. 실제 20초 자동 귀환 분기는 `CommanderDuty.TickDuty` 연결 필요. |
| G15 | 구현 완료·실행 미검증 | 차량 40/비행기 100은 1회 수송 한도. 동일 거점 복수 부대 출발·현지 합류/재탑승 허용. 차량/항공기는 접안 작업 1초만 배타, 전개 완료 부대는 후속 부대를 막지 않음. Visitor는 전개 부대를 우선 탐색. |
| G21 | 구현 완료·실행 미검증 | 지역별 위협/개별 쓰러짐 사건 합침, 로켓 파괴 감시, 전원 편성 시 본거지 무대응 경고와 왕복 시간 표시, 실제 시간 경과 후 귀환. 본거지 장수 0명일 때 일반 침입 출발·도착 보류. |
| G44 | 구현 완료·실행 미검증 | 일반 알림 비정지, 공격/쓰러짐/로켓 자동 정지 설정, 사건별 중복 정지 방지, 수동/메뉴/협상 이유 분리. 협상 중 배속 요청으로 강제 정지 해제 불가, 종료 시 나머지 이유 유지. |
| G62 | 구현 완료·실행 미검증 | 문명 거점 도착/공격 접촉, 원격 일반 선전포고로 미접촉 문명 접촉 불가. 미접촉 선제 침공 차단. 접촉 후 900초 유예·계절별 정복자 판단·공격대 전력 1.5배 경계. 독립/문명 본거지 침공대는 출처·병력·도착 시각을 저장하고 출발 알림, 원정 거리만큼 이동 후 등장. |
| G67 | 구현 완료·실행 미검증 | 담당 없는 열람/거절, 본거지 활동 담당이 있어야 수락/역제안. 쓰러짐도 담당 제외. 기본 제안 900초, 최후통첩 거절 후 예상 이동+450초, 기한 경계 응답 차단, 종료된 최후통첩 중복 철회 부작용 차단. |

## 이번 재개에서 추가한 것
- `World/DiplomacyNegotiation.cs`: `CanNegotiate`에서 건강 수치는 남아도 쓰러진 담당을 제외. `WithdrawUltimatum`은 아직 보류 중인 최후통첩에만 적용해 평판/위협 신뢰 중복 차감을 막음.
- `World/DiplomacyRaids.cs`: 쓰러진 장수를 본거지 가용 전력에서 제외.
- `AgentScripts/ImplB5Checks.cs`: 차량 탑승 위치를 검사 안에서 준비. 독립 원정대 출발·중복 방지·무인 본거지 도착 보류·재개, 정복자 계절 체크, 이동 복원 시 재결제 없음·중복 등장 없음, 쓰러진 담당/전력 제외, 중복 철회 방지, 구저장 기본값 복원 검사를 보강. 파괴된 방문자 참조는 생존 목록으로 집계.
- 이 핸드오프 및 공동 보드 갱신. 새 구현 파일/의존성 추가 없음.

## B5 수정 파일 (직전 진행분 포함)
경로 접두사는 `Assets/_Project/Scripts/`이다. 아래 목록은 B5 변경만 가리키며 worktree 전체 변경 목록이 아니다.
- World: `ExpeditionRules.cs`, `ExpeditionSupplies.cs`, `ExpeditionTransport.cs`, `ExpeditionSite.cs`, `WorldMapExpeditions.cs`, `WorldMapManager.cs`(원정 부분), `HomeDefenseReturn.cs`, `LocalIncursions.cs`, `ColonyInvasion.cs`, `DiplomacyRules.cs`, `DiplomacyManager.cs`, `DiplomacyRaids.cs`, `DiplomacyNegotiation.cs`, `DiplomacyRebels.cs`.
- UI: `GameMenuWalking.cs`, `WorldMapPanel.cs`(원정 부분), `SimulationPause.cs`, `ThreatIncidents.cs`, `EnemyAlert.cs`, `GameMenuNegotiation.cs`.
- 이전 세션 연결분도 보존: `UI/GameMenuController.cs`, `UI/GameMenuConscription.cs`, `Core/UserSettings.cs`, `Save/SaveDtos.cs`, `Save/SaveSnapshot.cs`, `Save/SavePreflight.cs`. 이번 재개에서는 이 파일들을 수정하지 않았다.
- 검사/문서: `AgentScripts/ImplB5Checks.cs`, `README.md`의 B5 통합 대기 문단(이전 진행분), `docs/IMPLEMENTATION_BOARD.md`, `docs/IMPL-B5-HANDOFF.md`.
- graphify 생성물은 별도 대량 변경으로 분리해서 검토할 것. 기존 신규 파일 `.meta`는 대부분 있으며 `DiplomacyRaids.cs.meta`는 통합 시 Unity에서 생성 확인 필요.

## 연결 요청 (Claude/해당 소유자)
1. **필수 — `Assets/_Project/Scripts/Units/CommanderDuty.cs::TickDuty`의 `if (IsDeployed)` 분기**
   - quietSeconds 누적 전 `AntColony.World.HomeDefenseReturn.Allows(this)`를 확인한다.
   - false일 때 `WorkState.quietSeconds = 0; return;` 처리. true일 때 기존 적 탐색·20초 누적·`ReturnToPost()` 유지.
   - 기존 수동 귀환 `if (IsReturning)` 분기를 건드리지 말 것. 원정/현지 채집/구조/운반/시공/수동 귀환과 설정 꺼짐을 자동 귀환에서 제외하는 의도다. B5 수정 금지 파일이라 미수정.
   - 연결 후 실제 `TickDuty` 20초 경계·설정 off·구조/현지 작업 중 귀환 안 됨을 Play 검사에 추가할 것. 현재 B5 검사는 설정/직렬화/원정 제외 API까지만 확인한다.
2. **기존 연결분 누락 방지 — `UI/GameMenuController.cs`**
   - `SetSpeed`→`SimulationPause.UserSpeed`, 창 전환/`Resume`→menu/negotiation 이유 해제, `ShowLoading`→저장 pause 복원을 함께 합친다. 원래 Time.timeScale 직접 복원 코드로 되돌리면 G44가 깨진다.
3. **기존 설정/UI/저장 연결분 누락 방지**
   - `UI/GameMenuConscription.cs::Conscription`의 `Home Defense Auto Return` 및 출전 시 `DefenseReturn.Set`.
   - `Core/UserSettings.cs`의 autoPauseThreat/autoPauseDowned/autoPauseRocket 기본값 true.
   - `Save/SaveDtos.cs`의 walking/foodDebt/dockingRemaining, world.pause/defenseReturn; `SaveSnapshot` 캡처/복원 및 `SavePreflight` 검증. 저장 버전은 v19 유지.
   - 외교 접촉 시각/계절 체크/이동 침공대는 기존 `SaveFile.diplomacy` 전체 직렬화로 보존한다. DTO 별도 중복 추가 불필요. 구저장은 빈 이동 목록·접촉 유예 부여, 보상/출발 비용 재실행 없음.
4. **통합 검증**
   - 신규 .meta 생성 확인 → Unity 전체 컴파일 → `RunChecks`로 `AgentScripts/ImplB5Checks.cs` 실행 → 기존 TransportRoute/SettlementDefense/SaveRoundtrip 회귀 검사.
   - B6가 동일 worktree 스냅샷에 포함되어 있으므로 `WorldMapManager` 보스 연결과 B5 원정 변경을 함께 보존하며 해당 소유자와 병합할 것.

## 검사 목록·현재 결과
- `ImplB5Checks`는 기존 B1 형식의 public static class / `public static async Task<string> Main()`이다. 실패는 예외, 성공은 `PASS n`. **현재 실행 결과는 없다.**
- 불시착 2장수·병력0으로 새 게임을 만들며 비축더미/BuildingData를 검사에서 직접 생성한다. 자동저장을 끄고 임시 `SaveStorage.RootOverride`, 난수/설정을 finally에서 복원한다. 검사는 새 게임으로 현재 실행 상태를 바꾸므로 통합 검사 환경에서 실행할 것.
- G05: 거리·보급식·무차량/무병력 출발·병력 식량·화물·왕복 시간.
- G15: 중복 탑승 거부·복수 도보대·현지 합류·차량/비행기 1회 정원·접안 대기/해제.
- G48: 기본 켜짐·편성별 off JSON·원정 제외(API). 실제 CommanderDuty 귀환 검사는 연결 후 필요.
- G62/G21: 미접촉 문명 침공 차단·중립 도착 비접촉·접촉 유예·1.5배 경계·정복자 계절 주기·실제 출발 비용·독립/문명 이동·무인 본거지 보류·복원 무재결제·중복 등장 방지·잘못된 도착 시각 거부·구저장 복원.
- G67: 담당 없는 수락 차단/거절 허용·쓰러짐 제외·900초 기한·거절 최후통첩 이동+3일·기한 경계·철회 1회.
- G44/G21: 공격 자동정지·협상 종료 시 수동 정지 유지·같은 사건 재정지 없음·협상 강제정지·일반 알림 비정지·동시 지역 사건 합침·JSON 기본 호환.
- 정적 확인: 참조 심볼 105개를 `rg`로 존재 확인(누락 0), 변경 메서드 호출자 확인, `git diff --check` 공백 오류 없음. 이것은 컴파일/런타임 성공을 뜻하지 않는다.
- `graphify query` 후 `graphify update .` AST 갱신 완료(최종 8553 nodes / 19362 edges). 커뮤니티 재분류에 따른 기존 라벨 일부 갱신 안내가 있었으며, 의미 라벨 재생성은 하지 않았다.

## 남은 위험
- G48 실제 연결은 필수 미완료다. 다른 G 항목도 실행 검증 전 최종 완료로 보고하지 말 것.
- 정복자 전력은 기존 외교 추정 단위(장수20+병력, 공격대1명20)를 재사용한다. 상세 무기/부상별 전투력 시뮬레이션을 새로 만들지 않았다.
- 이동 침공대는 플레이어 월드 이동과 같은 출처·병력·도착 시각 모델이며, 이동 중 개별 로컬 유닛/지도 마커는 만들지 않는다. 도착 시 기존 LocalIncursions 경로로 생성한다.
- 실제 save/load에서 동시 접안·복수 승무원·소지식량·보급 부채·침공대·정지 이유가 1회만 복원되는지 통합 검증 필요. JSON 검사는 실제 파일 왕복을 대신하지 않는다.
- 로켓 파괴 OnDisable 알림, 협상창 실제 개폐, 월드맵 화면·보급 수치·경고 문구의 육안 검증은 미수행. SAVE 전용 캡처는 하지 않았다.
