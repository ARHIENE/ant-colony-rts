# IMPL-D4 회귀 검사 인계 — 2026-10-24

## 범위와 검증 상태

- 작업 위치: `E:/Git/ant-wt/D4`, `codex/d4-checks`. 원본 프로젝트에는 쓰지 않았다. Unity CLI·Play 실행·캡처·커밋·SAVE는 수행하지 않았다.
- **게임 어셈블리와 지정 체크 5개를 각각 오프라인 컴파일해 모두 오류 0건.** 이것은 Play 통과를 뜻하지 않는다. 특히 낚시의 보고된 120초 정체와 SettlementDefense의 과거 15분 정체는 실행 당시 상태가 없어 단일 원인을 확정할 수 없다.
- 기존 미커밋 변경(UI 탐색/행성 좌표, BuildingBase·WildMonster 수정 등)을 보존했다. 아래는 이번 작업의 변경 내용이다.

## 1. WorldMapChecks

- **null 원인 확정:** `CrashLandingStart.Apply()`가 본거지 기본 건물(비축더미 포함)을 제거한다. `FindFirstObjectByType<Stockpile>()` 결과가 null인 상태에서 `.All(c => ... home.Position)`이 역참조했다. EnemyColony가 null이라는 뜻이 아니다. 비교 기준을 `WorldMapManager.HomePosition`으로 교체했다.
- 이어서 실행하면 실패할 오래된 기대값도 교정했다. 현재 월드맵은 시작부터 열리며, G06 기초 연구대는 인구·낚시·2등급 막사를 요구하지 않는다. 옛 식량 100 비용 대신 `EarlyConstruction.Cost(ScienceLab)`의 원목·돌이 완공까지 정확히 한 번 소비되는지 검사한다. 자재 운반용 비축더미와 건설·운반 작업을 준비한다.
- 연구 담당 장수의 Research 작업을 명시적으로 켠다. 새 게임 시작 후보의 금지 특성에 따라 임의로 실패하지 않도록 검사용 후보 특성을 비운다.
- G15에서 폐기된 ‘점유 거점으로 두 번째 수송 출발 거부’ 기대값을 제거했다. 복수 부대 접안은 TransportRouteChecks가 검사한다.
- G62 이후 `LocalIncursions.TrySpawn()`은 외교 출발 스케줄을 요청하므로 즉시 방문자 3명을 기대할 수 없다. 지역 전투·포로 검사는 명시적 `TrySpawnIndependent(3)`로 분리했다. 실제 외교 원정 소요시간은 이 체크의 검사 범위가 아니다.

## 2. SettlementDefenseChecks

- **습격 거부 원인:** `DeclareWar(Data.civilizations[0])`의 성공 여부를 무시했다. G62에서는 미접촉 문명에 일반 선전포고가 거부되며, 첫 문명은 정복 뒤 멸망했을 수도 있다. `TryStartRaid()`는 살아 있는 교전 문명이 전혀 없으면 false를 반환한다. 전쟁 조건을 제거하는 런타임 수정은 하지 않았다.
- 평화 상태를 명시적으로 준비하고, 살아 있는 다른 문명 거점까지 실제 수송을 보내 접촉한 뒤 선전포고 성공을 검사한다. G39 로켓 위기 억제가 없는 새 세션인지 별도로 확인한다. 실패 진단의 internal `GuardTemplate` 접근은 리플렉션으로 바꿨다.
- **무한 대기 경로 확정:** 새 게임 전후 `while (SaveSystem.Busy)` 두 곳에 종료 시각이 없었다. 각각 90초 제한과 단계별 실패 메시지를 추가했다. 기존 `Wait` 헬퍼는 이미 30초 실시간 제한이 있었다. 포로/탈출용 동기 난수 시드 탐색도 최대 10,000회로 제한하고 난수 상태를 finally에서 복원한다.
- 적 도착/습격은 `ThreatIncidents.Scan()`의 자동 일시정지를 유발할 수 있다. 실제 전투를 기다리는 검사 동안 `autoPauseThreat`를 끄고 finally에서 원래 설정을 복원한다. 이것이 과거 15분 정체의 실제 발생 지점이었는지는 미확정이다.

## 3. TransportRouteChecks

- 시작 체크에 이미 반영돼 있던 G15 후속 출발·접안 대기·방문자 소유 유지 기대값은 `ExpeditionTransport.Tick()`과 일치하므로 유지했다. G69 넘친 화물의 바닥 하역 기대값도 유지했다.
- G42의 `TryBoard`/`TryUnloadCrew` 반환값은 지시 접수 성공이지 무조건 즉시 완료가 아니다. 귀환 뒤 빈 승무원 검사는 pending 지시가 끝날 때까지 최대 30초 기다리도록 했다. 이미 같은 수송의 승무원인 장수에게 재탑승을 요구하지 않는 기존 변경은 유지했다.
- 경로 체크의 `Wait`는 원래 300초 실시간 상한이 있다. 무한 루프는 아니지만 여러 단계에서 대기하면 셸 스크립트 기본 대기(600초)를 넘을 수 있다. 따라서 과거 셸 TIMEOUT만으로 게임 코드 무한 루프라고 판단할 수 없다.
- 검사용 후보의 금지 특성을 제거하고 위협 자동 정지를 일시 해제한다. 임시 `SaveStorage.RootOverride`는 finally에서 원래 값으로 복원한다.

## 4. ImplG06Checks / 표본 연구

- **런타임 결함 확정:** `CommanderDuty.TickDuty()`가 `CampaignResearch.Active`(과학 연구)만 확인했다. 표본 연구는 `ActiveResource`를 사용하므로 다음 장수 틱에서 연구소 배정이 해제됐다. 과학·표본 연구가 모두 없을 때만 배정을 해제하도록 조건 한 곳을 수정했다.
- 기존 체크는 `Time.timeScale = 0` 상태에서 `research.Tick()`만 직접 호출해 위 결함을 놓쳤다. 직접 duty 틱과 실제 2프레임 경과 후 배정 유지 검사를 추가했다. 연구소 비활성 시 중단·재배정 후 재개도 검사한다.
- `ResourceManager.Add`는 Gathering/Expedition/Trade/Reward만 표본으로 기록한다. 생산·환불은 제외한다. `AddTo`는 실제 담은 양이 양수일 때만 기록한다. 체크가 `RecordSampleDelivery`를 직접 호출하는 대신 이 공개 입고 경로를 검증하도록 바꿨다(생산/환불/빈 입고 제외, 원정 Add·교역 AddTo 포함).
- Add의 창고 초과분은 본거지 바닥에 내려놓으므로 표본 반입으로 기록하는 현 동작을 유지했다. CanMine의 도구 등급 조건과 직렬화 검증은 구현과 일치했다. 초기화 90초 대기 뒤에는 반드시 Busy 해제를 확인하며 시간 배율을 finally에서 복원한다.

## 5. FishingChecks / NavRoute

- **보고된 pathPending의 실제 원인은 미확정.** `WorkerAnt.CommandGather()`는 `TryWorkApproach()`로 NavMesh.SamplePosition(4m)과 도달 가능성을 확인한 뒤 한 번 목적지를 지정한다. `TickMovingToNode()`에는 매 프레임 SetDestination 호출이 없다. 짧은 이동이면 NavRouteFollower도 생성하지 않는다. 따라서 보고 좌표만으로 ‘낚시 갱신이 매 프레임 경로를 초기화했다’고 확정할 수 없다.
- `agent.isStopped`를 낚시 경로에서 켜는 코드는 찾지 못했다. 무조건 재시도·Warp·isStopped 해제 같은 추측성 우회는 추가하지 않았다.
- 기존 동일 목적지 생략은 중복 요청 방지로 유지하되, XZ 10cm 이내만 비교하던 조건을 **XYZ 1cm 이내**로 좁혔다. 높이가 다른 새 목적지가 잘못 무시되는 일을 막는다. 원인 확정처럼 적힌 기존 주석은 제거했다.
- 체크에 동일 최종 목적지를 반복 요청해도 10초 내 pathPending이 끝나고 PathComplete인지 확인하는 회귀 검사를 추가했다. 장거리 경유지가 있으면 agent.destination(현재 경유지)이 아니라 최종 접근점을 재요청한다.
- 실패 진단 문자열은 대기 시작 시점이 아니라 **실패 시점**에 만든다. 시간 배율·게임 내 경과초·화면·노드·좌표·접근점·NavMesh·pending·경로 상태·isStopped·속도·수면·명령 가능 여부를 남긴다. 이 값으로 시뮬레이션 정지와 실제 경로 계산 정체를 구분해야 한다.
- 위협 자동 정지를 검사 동안 해제하고, 불시착 시작에 없는 비축더미를 검사용으로 준비했다. 반납 시설 부재는 도착 후 반납 실패의 확정 원인이며, 이동 중 pathPending의 증거는 아니다. Busy 대기는 90초로 제한했다. G08 어획 상한/보충 표현을 월간에서 계절로 교정했다.

## 변경 파일

- `AgentScripts/WorldMapChecks.cs`
- `AgentScripts/SettlementDefenseChecks.cs`
- `AgentScripts/TransportRouteChecks.cs`
- `AgentScripts/ImplG06Checks.cs`
- `AgentScripts/FishingChecks.cs`
- `Assets/_Project/Scripts/Units/CommanderDuty.cs`
- `Assets/_Project/Scripts/Map/NavRoute.cs`
- `docs/IMPLEMENTATION_BOARD.md`
- `docs/IMPL-D4-HANDOFF.md`
- 자동 생성: `graphify-out/` 그래프·보고서·캐시 갱신. 개발 일지나 SAVE 기록은 작성하지 않았다.
- 검증 산출물(Temp, 제품 코드 아님): `Temp/off/compile.ps1`, 응답 파일·DLL·로그 및 `dependencies/`.

## 오프라인 컴파일 및 다음 실행

- 사용자가 지정한 Unity 6000.5.8f1 내장 dotnet/Roslyn과 원본 Bee `Assembly-CSharp.rsp` 옵션을 사용했다. 기존 .cs 목록·out/refout/analyzer/generatedfilesout을 제거하고 Library 참조를 절대 경로로 바꿨다. D4의 Assets 소스를 열거해 게임 DLL을 생성했다.
- D4에는 원본의 무시된 외부 에셋 스크립트(SpinFree, TMP 예제, _TeamImport 등)가 없었다. 원본 rsp가 참조하던 실제 소스를 `Temp/off/dependencies`에 복사해 포함했다. 가짜 타입/stub은 만들지 않았다. 이로써 전역 동명 타입이 존재하는 조건에서도 각 체크 컴파일을 확인했다.
- 체크는 `Assembly-CSharp-Editor.rsp`의 참조에서 원본 게임 DLL 참조를 제거하고 D4 출력 DLL을 참조해 **각각 별도 컴파일**했다. 게임 및 5개 체크 모두 exit 0, 오류 0건. 기존 obsolete API 등의 경고는 남아 있다. `git diff --check` 통과.
- 재현 명령: PowerShell에서 `& ([scriptblock]::Create((Get-Content -Raw Temp/off/compile.ps1)))`. 결과는 `Temp/off/*.log`와 `*.rsp`에 있다.
- Graphify query 및 `graphify update .` 수행. 자동 커뮤니티 이름 갱신 안내가 있었으며 LLM 라벨링은 수행하지 않았다.
- 후속 작업: 원본에 변경을 통합한 뒤 **각각 새 Play 세션**에서 RunChecks로 실행. `run_checks.sh`는 원본 `E:/Git/ant`로 cd하므로 D4 자체를 검증하는 명령으로 오해하지 말 것. 낚시가 다시 실패하면 새 진단값을 확보하고, SettlementDefense는 새 단계별 실패 메시지로 실제 중단 위치를 확인한다. 현재 Play 통과 수는 보고하지 않는다.
- 카카오톡 완료 알림 도구는 이 세션에서 제공되지 않아 전송하지 못했다.
