# IMPL-C2 인계 — 2026-10-24

## 상태

- C2 worktree 안에서만 작업. Unity/Unity MCP/Notion/commit/stash/push/prefab 작업 없음. 원본 프로젝트 접근 없음.
- 아래 ‘완료’는 소유 범위 코드 연결 완료이며 컴파일·실행 통과가 아니다. 시작 시 staged된 B5~B10 결과는 보존했다. C2 이관은 **unstaged diff와 아래 신규 파일** 기준으로 한다.
- 원문: `docs/PLAY_CHECK_DECISIONS_1023.md`, B6/B7/B9 인계의 연결 요청. 달력 수치 추가/변경 없음.

## G별 판정

| G | 판정 | C2 결과 |
|---|---|---|
| G07/G19/G23/G51 (B6 수송 부분) | 완료(소스) | BossStronghold가 있을 때만 회수 API로 분기해 비보스 fallback 재귀 방지. CargoLoad/HasCargo/AddCargo/DepositResources/자동 수송에 핵심물 포함, TryCollect의 별도 차감 제거. 하역의 안전 조건 확인 후 일반 화물보다 먼저 Deliver. 확정 파괴 Die에서 Lose, 단순 OnDisable/복원/해산은 Lose하지 않음. |
| G15 | 완료(소스) | 점유 중 후속 출발 허용·기존 Visitor 유지, 동시 접안 시간만 대기. Deployed인 수송의 접안 시간만 후속 진입을 막음. TransportRouteChecks의 옛 점유 금지 기대값 교체. FitCursor 원형과 호출 유지. |
| G42 | 부분 | TryBoard/TryStation/TryUnloadCrew를 승객별 직접 지시 큐로 접수. OrderRefusalReason 표시·대기, 해소 후 접근 이동/탑승/주둔/하차. 떠남/비활성/사망/포로/다른 수송·주둔지 이동/명령 대상 상태 변경 시 제거. 명시 CancelCrewOrder와 새 수송 지시로 대체 가능. 저장 DTO·장수 직접 정지/다른 명령 연결은 소유 밖이라 아래 요청 필수. |
| G69 | 부분 | Returning→Home, 하차, 하역에 HomeReturnPoint.TryGround/TryAir와 잔류 적 검사 연결. 실패하면 Returning 또는 선상 상태와 화물 유지. 석방 포로 Arrive도 TryGround와 적 검사로 실패 시 Homecoming 보존. 자원 하역은 창고 초과분을 바닥에 1회만 보존. 징집소 없는 생존 병력의 시민 반환은 Units 담당 연결 필요. |

## 수정 파일

- `Assets/_Project/Scripts/World/ExpeditionTransport.cs`: 회수/용량/하역/파괴, 직접 지시 접수, 귀환·접안.
- 신규 `Assets/_Project/Scripts/World/ExpeditionTransport.Orders.cs`: 직접 지시 큐, 표시/취소, 저장용 CrewOrderState·CaptureCrewOrders/RestoreCrewOrders.
- 신규 `Assets/_Project/Scripts/World/ExpeditionTransport.Home.cs`: 잔류 적 판정·육상/항공 안전 귀환 지점.
- `Assets/_Project/Scripts/World/TransportRoute.cs`: 대기 중 직접 지시와 핵심물 하역 전 다음 왕복 차단. **FitCursor 유지**.
- `Assets/_Project/Scripts/World/BossRewardTransfer.cs`: TryCollect 용량 이중 차감 1곳만 제거.
- `Assets/_Project/Scripts/World/DiplomacyPrisoners.cs`: 본거지 석방자 귀환 지점/보류, 반환 장비의 fallback 위치.
- `AgentScripts/TransportRouteChecks.cs`: G15 기대값·접안 검사, G69 창고 초과분 바닥 하역 기대값.
- 신규 `AgentScripts/ImplC2Checks.cs`: 정적 `public static string Run()`과 기존 실행기 호환 `Task<string> Main()`.
- `docs/IMPLEMENTATION_BOARD.md`, 이 문서. `WorldMapExpeditions.cs`는 읽기만 함. 편성 실패 정리는 보상을 아직 싣지 않은 경로라 소실 호출을 넣지 않음.

## 연결 요청 — 소유 밖 파일은 수정하지 않음

1. **Save/SaveDtos.cs::TransportDto**
   - `List<AntColony.World.CrewOrderState> crewOrders` 필드 추가. null 구저장 호환, CurrentVersion=19 유지.
   - CrewOrderState: commanderId, kind(Board/Station/Unload), state(Home/Deployed), siteIndex/sourceTransportIndex/sourceSettlementIndex(-1은 없음).
2. **Save/SaveSnapshot.cs::Capture/Restore**
   - 참조 목록과 승무원 순서가 확정된 뒤 각 수송 `CaptureCrewOrders()`를 DTO에 저장.
   - 모든 수송/장수 생성, RestoreCrew, 주둔지 재연결이 끝난 뒤 `RestoreCrewOrders(dto.crewOrders)` 호출. 이 메서드는 표시만 복원하고 명령 실행은 다음 Tick에서 한다. 큐를 기존 승무원 목록에 미리 넣지 말 것.
3. **Save/SavePreflight.cs::Validate/ForScene**
   - 큐의 ID 존재·전 수송 간 승객 중복·enum·인덱스 범위·수송 현재 상태/원래 소속 일치·승무원/병력 한도(대기 승객 포함) 검증.
   - BossRewardTransfer.CargoLoad 포함 총 용량·고유 bossCarrierId와 boss cargo.carrier 교차 검증. RestoreCrewOrders는 이 사전 검증을 전제로 한다.
4. **Units/CommanderAnt.cs·CommanderDuty.cs 및 실제 플레이어 명령 전달부**
   - 명시 ‘정지/다른 이동/다른 작업/취소’가 승인될 때 `world.Transports`를 돌며 `ship.CancelCrewOrder(commander)` 호출. 내부 CommandStop(탑승/하차/자율 행동에도 사용)에 무조건 넣으면 큐가 스스로 취소되므로 **사용자 명령 경계에서만** 처리할 것.
   - 현재 큐는 매 Tick 직접 이동을 재요청한다. 일반 자동 작업보다 우선하도록 자율 작업 시작 전 pending 여부 확인 경로를 연결해야 전체 G42 우선순위가 완성된다. 탑승 대기 중 생활 욕구/자동 작업/새 플레이어 명령의 실제 프레임 경쟁은 미검증.
   - G69: 하차된 원정 장수가 생존 병력을 징집소 없이 기존 ColonyPopulation/AntPool로 **한 번만** 반환하도록 연결. C2는 소유 밖 병력/시민 집계를 수정하지 않음.
5. **UI/WorldMapPanel.cs·GameMenuWalking.cs 등 직접 지시 호출자**
   - TryBoard/TryStation/TryUnloadCrew의 true는 이제 ‘접수됨’이며 완료 보장이 아님. `PendingCrewOrderCount`/`CrewOrderStatus`로 구분. 기존 ToastManager pinned에도 사유 표시됨.
   - 접근 대기 중 TryDepart/TryReturn은 false. ‘탑승→출발’ 단일 클릭 흐름이 있다면 큐 완료 후 다시 출발하도록 연결하고 대기 중 재편성/다른 명령/취소 UI 연결.
6. **확정 소실 처리 담당**
   - TakeDamage→Die 경로는 Lose 연결 완료. 체력 파괴를 거치지 않고 수송을 영구 삭제하는 별도 경로를 추가하면 목록 제거/Destroy 전에 Lose 호출. 씬 재구성/복원/일시 비활성/정상 해산에는 호출 금지.
7. **README/Graphify 통합 담당**
   - README 수정은 C2 소유 밖이므로 통합 시 현재 구현/미연결 사항 반영. Graphify는 query만 수행했고 소유 밖 생성물 변경을 피하려고 update는 보류했다. 통합 후 `graphify update .` 실행.

## 검사 목록

- **미실행**: `ImplC2Checks.Run()` 또는 기존 실행기에서 `ImplC2Checks.Main()` 호출. 불시착 새 Play 세션(1~2장수, 비축더미 없음)을 전제로, 장수·수송·보스/편입 거점·잔류 적 fixture를 직접 생성하고 finally에서 정리한다.
- G42: 분노 중 탑승 접수/불응 사유/명시 취소, 큐 capture/restore의 무부작용·불응 유지, 해소 후 실행, Binge 하차 대기, 새 수송 명령으로 교체, 비활성 승객 제거, 주둔 대기/실행.
- B6: A 핵심물 2칸을 정확히 남긴 일반 화물 뒤 회수(이중 차감 회귀), 핵심물 포함 적재 한도/HasCargo/CargoFull, 중복 회수 차단, 옛 설계도/장비 우회, 반입 1회, 단순 비활성은 유지·확정 파괴만 소실.
- G69: 빈 지도 bounds로 귀환 지점 실패→Returning/승객/화물 유지, 잔류 적 보류, 적 제거 후 육상 복귀, 불응 승객이 남아도 핵심물 반입, 창고/바닥 합계로 일반 화물 1회 지급, 석방 포로 안전 지점 실패/재시도.
- G15: 동시 접안 대기→시간 해소 후 기존 부대와 합류. 기존 TransportRouteChecks도 새 G15/G69 기대값으로 수정.
- 설정·난수·시간배율·캠페인·창고 칸·기존 바닥 수량·자원 이력을 복원한다. 검사용 장수 생성 순번 등 전체 게임 상태의 완전한 롤백을 보장하는 검사는 아니므로 검사용 새 세션에서 실행.

## 정적 검증 / 남은 위험

- 컴파일·Unity 실행·Play 검사·실제 save/load·화면 검증은 하지 않음. PASS 수치는 없음. 소스 rg 심볼 확인·diff 공백 검사만 실시.
- 새 partial 2개 `.meta`는 Unity 재개 시 생성 필요. prefab 변경 없음.
- 실제 저장 연결 전 G42 대기 명령은 디스크에 저장되지 않는다. 사용자 정지/다른 명령 취소 연결 전에는 대기 큐가 해당 명령을 재요청할 수 있다.
- 실제 지형에서 TryGround/TryAir가 false면 상태를 유지하고 재시도한다. NavMesh 가장자리/좁은 항공 빈터 검증은 B9 API와 함께 필요.
- 기존 일반 화물 신선도 모델은 바꾸지 않음. 적재 한도 초과 운반분 처리도 기존 WorkerAnt 반환 규약을 그대로 사용하므로 후속 검증 필요.
- 통합 순서: 위 소유 밖 연결→신규 meta/Unity 컴파일→ImplC2Checks→TransportRouteChecks/ImplB5Checks/ImplB6Checks/SaveRoundtripChecks→본거지 전소 실제 플레이 확인.
