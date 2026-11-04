# E2 — D06 철수 / D07 파손·수리·잔해 / D09 순회 검사

- 작업: `E:/Git/ant-wt/E2`, `codex/e2-withdraw-damage`, 2026-10-25.
- 기준: `DECISIONS_1025.md` 확정 + `DECISIONS_1025_codex.md` D06/D07 A안 원문. 저장 버전 **19 유지**.
- 상태: 구현·오프라인 본체/새 검사 컴파일 오류 0. **Unity 실행, 검사 실행, 화면 확인은 미실행**. UI·진형 코드 수정 없음. SAVE·개발 일지·Notion·캡처·push 없음.

## 변경 파일

- 신규 규칙/상태: `Assets/_Project/Scripts/World/ExpeditionWithdraw.cs`, `ExpeditionBodies.cs`, `TransportDamage.cs` 및 각 `.meta`.
- 신규 검증: `Assets/_Project/Scripts/Save/SaveTransportValidation.cs` 및 `.meta`.
- World 연결: `ExpeditionTransport.cs`, `ExpeditionTransport.Orders.cs`, `ExpeditionSupplies.cs`, `TransportRoute.cs`, `ExpeditionSite.cs`, `WildMonster.cs`, `BossStronghold.cs`, `BossRewardTransfer.cs`.
- Buildings 연결: `BuildingBase.cs`(가상 MaxHealth·수송 잔해 HP 0 복원·사망 반납처 제외), `BuildingRepair.cs`(수송 수리 위임), `ScienceLab.cs`(건조비 공통 조회).
- Save 연결: `SaveDtos.cs`, `SaveSnapshot.cs`, `SavePreflight.cs`, `SavePreflight.Integration.cs`.
- Units 연결: `CommanderPersonal.cs` 이동속도식 끝에 운반 배율 1회 적용. `WorkerAnt.cs` 반납 경계에서 적재 완료까지 실물 유지. **진형 관련 필드/메서드 수정 없음**.
- 검사: `AgentScripts/ImplD06D07Checks.cs`, `AgentScripts/ImplD09Checks.cs`.
- 문서: `README.md`, `docs/IMPLEMENTATION_BOARD.md`, 이 문서.

## 구현·자동 선택

1. 철수
   - `TryReturn()`은 **접수 성공**을 반환한다. 즉시 Returning이 되는 계약에서, Deployed 상태의 철수 작업을 예약하고 완료 후 Returning이 되는 계약으로 바뀌었다.
   - 자력 1초/명, 부상자 들어올리기 2초 + 승차 2초, 한 짐 2초. 공용 작업 자리 2개. 구조자는 기존 장수 이동·NavMesh를 사용하며 기존 이동 배율 뒤에 ×0.75를 한 번만 곱한다.
   - 현재 병력 모델은 장수의 집계 수이므로 실제 이동하는 장수를 집단의 위치로 사용한다. 최대 10명씩 도착 후 승차 시간을 처리한다. 완료 병력은 장수의 현장 병력에서 빼 별도 탑승 명부에 보존하며, 긴급 출발 시 미완료 인원을 함께 태우지 않는다. 정상 승차는 본래 장수에게 합치고, 긴급 귀환한 장수 없는 병력은 본거지 도착 때만 AntPool로 반환한다. 저장의 Assigned 인원 검증에도 포함한다.
   - 부상자는 승무원 중 가능한 장수가 먼저 구조한다. 외부 부상자도 `TryAddInjuredPassenger`로 명부 예약 후 실제 운반할 수 있다. 부상자/시신은 각 운반자당 한 명/한 구. 취소·구조자 무력화 시 현재 위치에 내려놓는다.
   - 시신 10, 유해 5의 화물 공간을 예약하며 적재 2초가 끝나야 선내 화물로 확정한다. 본거지 하역·파괴 때 실물 Corpse로 되돌린다. 선내 시신도 부패 시간이 흐른다.
   - 일반 운반자는 2초 적재 중 실물을 계속 보유한다. `workerId`가 있는 적재 항목은 **공간 예약**이고, 없는 항목은 직접 반납 API가 넘긴 실물 대기 화물이다. 전자는 WorkerAnt의 완료 경계에서만 화물로 옮기며 취소 시 중복 바닥 생성하지 않는다. 일반 반납도 공용 2자리를 쓴다.
2. 파손·수리·잔해
   - 차량/항공 HP 400/600. h<25% 이동 0, 25%≤h<50% ×0.75, 50% 이상 정상. 실제 월드 이동 Remaining 감소에 적용한다. 조종 가능한 장수가 없으면 출발하지 않는다.
   - 기존 `BuildingBase.TakeDamage`→`Die` 경로를 사용한다. 기존 적 타깃 획득의 아군 장수 탐색 뒤에 현장 수송수단을 추가했다. 이동 중 무작위 고장·습격은 추가하지 않았다.
   - 현행 실제 ScienceLab 건조비는 차량 식량50+흙60, 항공 식량80+흙100이다. 수리/잔해에는 **흙60/100만** 쓴다. 문서의 재료200 예시는 건조비로 복사하지 않았다. 향후 건조 레시피 변경은 `TransportDamage` 공통 조회부터 수정할 것.
   - `GameBalance.RepairCostShare=.5`, `RepairPerSecond=.02`를 재사용한다. 건설 WorkRate·경험치·Repair 작업 가능 여부를 적용한다. 누적 실제 회복 비율(double)을 저장하고 누적 올림 비용의 차액만 소비한다. 현장은 수송 화물의 흙, 본거지는 기존 ResourceManager를 사용한다. 부족하면 대기하며 보급 뒤 재개한다.
   - 기존 수송에 별도 연료 상태/소비 규칙이 없어 연료 수치·자원을 새로 발명하지 않았다. 연료·식량·기타 탑재품을 수리 재료로 소비하지 않는다.
   - 완전 파괴는 영구 HP 0 잔해로 남고, 자원·장비·시신을 현장에 내린다. 생존자는 잔해의 현장 명부에 남겨 기존 다른 수송수단의 Board로 구조 가능하다. 잔해는 접안/반납처/보스 재등장 차단 수송수단에서 제외한다. 고유 보스 핵심물의 손실 처리는 기존 `BossRewardTransfer.Lose` 계약을 유지한다.
   - 잔해 해체는 건설 작업 10초, 재료별 내림 25%를 바닥에 한 번 반환한다. 현 레시피에서는 차량 흙15/항공 흙25. 실물 화물과 해체 수익은 분리한다. 수리·해체 작업자와 해체 경과도 저장한다.
3. 저장·검증
   - `TransportDamageState.present`를 명시적 존재 표시로 사용한다. JsonUtility가 빈 객체를 복원해도 present=false이면 만체력이다. 철수는 active 및 실제 작업/명부 값으로 판정한다. 클래스 null만으로 구저장을 판정하지 않는다.
   - HP·누적 수리비·해체 경과/완료·작업자, 철수 승차/들어올리기 경과·조력자·완료 병력, 적재 실물/예약·운반자, 시신 화물/회수 작업을 저장·복원한다. 복원 자체가 결제·보상·이동을 재실행하지 않는다.
   - 기존 저장은 화물이 Food/Soil/Special 3종뿐이라 목재 등 적재물이 유실될 수 있었다. 이번 연결에서 자원 enum 전체의 `cargoAmounts`를 함께 저장하고, 필드 없는 구저장은 기존 3종에서 복원한다.
   - SaveValidator→SavePreflight 경로로 범위/유한수/인원·작업자 소유/중복/슬롯·화물 예약/Assigned 명부/잔해 상태/경유 순번을 검증한다.
4. D09
   - 다음 경유지를 예약한 뒤 아직 이전 거점에서 탑승하는 동안 `Site != Destination`으로 자동 수송이 중단될 수 있던 순서를 수정했다. 일부 병력만 먼저 승차한 중간 상태도 수집자 상실로 판정하지 않는다.
   - 자원·장비·시신·적재 예약을 같은 화물 여유에서 차감한다. 다음 거점 출발 직전 여유/유효성을 다시 확인한다. 없어진 경유지는 건너뛰며 본거지 재출발 간격·하역 대기·Stop Auto의 화물 보존은 유지한다.

## 연결 요청 — Claude/UI 담당

- 철수 버튼: `ship.TryReturn()`이 true여도 아직 Deployed일 수 있다. `IsWithdrawing`으로 상태를 표시하고 반복 접수보다 진행 표시를 사용한다.
- 진행: `ship.WithdrawalProgress.People`, `.Loads`, `.EstimatedSeconds`. 예상 시간은 남은 작업의 두 자리 배치 추정이며 경로 이동/조력자/시신 회수 대기 등 현재 예측 불가 시 NaN이다. NaN은 “이동/구조 대기”로 표시한다. `ReservedCargo`, `TroopPassengers`는 완료 화물·병력과 별도로 확인 가능하다.
- 취소/긴급: `CancelWithdrawal()`, `TryEmergencyReturn()`. 긴급 출발 전에 진행 정보의 남은 인원/짐을 보여줄 것. 탑승 완료한 조종 가능 장수가 필요하다. 플레이어의 기존 장수 직접 명령은 철수/현장 작업/운반 예약을 취소하도록 연결했다.
- 부상자/시신: `TryAddInjuredPassenger(patient)`, `TryRecoverBody(corpse, carrier)`, `CancelBodyRecovery(bodyId)`; 구조자가 접근할 수 있어야 한다. 잔해에 남은 독립 병력은 `wreck.TryRescueTroops(commander)`로 지휘 한도 안에서 구조한다.
- HP 바: `CurrentHealth / MaxHealth`, `IsWreck`, `CanOperate`, `HasPilot`, `TravelSpeedMultiplier`를 사용한다. HP 0은 복구 불가.
- 수리 버튼: 근처 3m 안의 가능한 장수로 `TryRepair(commander)`를 호출하면 ship.Tick이 진행한다. 현장 재료는 실려 있는 흙이다. `FieldWorkActive`는 자동 수송의 새 출발을 대기시킨다. 중단은 `CancelFieldWork()`.
- 잔해 버튼: `TrySalvage(commander)`, `SalvageRemaining`, `Salvaged`. 3m 접근 필요. 완료 뒤 버튼 비활성화. HP0 잔해를 삭제/부활시키거나 별도 보상 지급하지 말 것.
- 과거 AgentScripts 일부는 TryReturn 성공 즉시 Returning 또는 무승무원 출발을 전제한다. 새 계약에 맞춰 철수/조종 장수 대기를 사용해야 한다. 기존 전체 검사들을 이번 작업에서 일괄 변경하거나 실행하지 않았다.

## 검사·컴파일 결과

- `ImplD06D07Checks.Main()` / `ImplD09Checks.Main()`은 `public static async Task<string>`. 새 Play 세션용이고, 임시 저장 RootOverride/자동 저장/위협 일시정지/TimeScale을 finally에서 복원한다. internal은 reflection, 게임 타입은 AntColony 전체 이름을 사용한다.
- D06/D07: 경계값·반복 수리비·2자리/시간·철수 중 실제 save/load·구저장 필드 없음·비정상 저장 거부·운반자 실물/예약·현장 수리·이동 감속·부상자/취소·시신·잔해 화물/중간 해체 실제 save/load·중복 해체·부분 병력 긴급 출발/Assigned 보존 검사를 작성했다.
- D09: 실제 Route.Tick/ship.Tick으로 1→2→3과 2거점 순회, 각 구간 실제 이동 시간, 중간 철수/경유 순번 save/load, 여유 1칸에서 진행·예약/분할 적재·0칸에서 귀환, 하역/재출발 간격/Stop Auto 보존을 검사한다. 빈 거점으로 채집 시간만 분리하며 순회 상태를 강제 덮어쓰지 않는다.
- 오프라인 Roslyn: Unity 6000.5.8f1의 dotnet/csc와 원본 Bee `1900b0aE.dag/Assembly-CSharp.rsp`를 사용했다. 원본 소스/out/refout/analyzer/generatedfilesout을 제거하고 Library 참조를 절대 경로화, worktree Assets runtime 소스를 재열거했다.
- worktree에 없는 git 제외 외부 의존 소스 43개(SpinFree·TMP 예제·_TeamImport 등)는 원본에서 `Temp/e2/dependencies/`로 **수정 없이 복사**해 포함했다. 가짜 타입/스텁 없음. UI 파일 수정 없음.
- 본체 `Temp/e2/a.rsp` → `Temp/e2/Assembly-CSharp.dll`: **exit 0, 오류 0**. 기존 BuildingConstructionSite.cs:33/109 CS0252 경고 2개. 기존 obsolete CS0618 경고는 출력에서 숨겼다.
- 새 검사 두 파일 `Temp/e2/check.rsp` → `Temp/e2/Checks.dll`: **exit 0, 오류 0**. Editor rsp 참조에서 원본 Assembly-CSharp 참조를 제거하고 위 출력 dll을 참조한다. top-level 파일은 포함하지 않았다.
- `git diff --check` 통과. Unity 실행/검사 PASS/시각 검증/실제 저장 왕복 성공으로 해석하지 말 것. 위 왕복들은 **검사 코드 작성 및 컴파일 완료**라는 의미다.

## 다음 확인

1. Claude만 Unity에서 새 검사 두 개를 순차 실행하고 결과를 기록한다.
2. 위 UI 진행·HP·수리/해체 버튼을 연결하고 플레이 상태를 확인한다. 캡처는 SAVE 때만 한다.
3. 병력 손실/구조자 사망/다른 수송으로 구조, 기존 자동 수송·저장 회귀를 실행한다. 기존 즉시 귀환 검사 전제는 새 비동기 철수에 맞춰 확인한다.

## Graphify·커밋 결과

- `graphify update .` 완료: AST-only, API 비용 없음, 9822 nodes / 22503 edges / 457 communities. 커뮤니티 명칭 갱신은 수행하지 않았다(기존 레이블과 구성 차이 안내). 생성된 graphify-out 변경/캐시는 커밋 대상에서 제외했다.
- 최종 본체/새 검사 컴파일 각각 exit 0, 오류 0. git diff --check 통과.
- **커밋 차단:** `git add`가 `E:/Git/ant/.git/worktrees/E2/index.lock` 생성 권한 거부로 exit 128. worktree 공용 Git 메타데이터가 쓰기 허용 범위 밖이라 이 세션에서 커밋을 생성하지 못했다. 파일 변경은 모두 worktree에 보존했다. push 없음.
- 권한 있는 세션에서 실행할 명시적 파일 목록/메시지/명령을 `Temp/e2/commit-files.txt`, `commit-message.txt`, `commit.ps1`에 준비했다. 스크립트는 브랜치·무관한 staged 파일·공백 오류를 확인하며 prefab/graphify 파일을 추가하지 않고 push하지 않는다. 메시지 마지막은 `Co-Authored-By: Codex`다.
- 카카오 MemoChat 도구는 이 세션에 없어 완료 알림을 전송하지 못했다.
