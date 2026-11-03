# WAVE-RETREAT-1103 인계 — 2026-11-03

- 원본 `E:\Git\ant`, `develop`에서 구현. 2026-11-03 사용자 승인에 따라 기존 전멸 종료에 대규모 잔당 퇴각을 추가했다. GAME_DIRECTION.md의 이전 전멸 전용 문구보다 이번 작업 지시를 우선했다.
- Unity 실행·캡처·커밋·Notion 작업 없음. 다른 담당자의 변경은 보존했다.

## 변경 파일과 동작

- `Assets/_Project/Scripts/World/WaveDefense.cs`: 생성 완료(`pending == 0`)·기존 EndGrace 5초 이후 검사. 총 규모 100 이상이고 생존 마릿수가 `max(3, ceil(total * 0.02))` 이하면 방문자들에게 퇴각을 지시하고 즉시 기존 `End()`를 호출한다. 평화 4일·성공 이야기 기록·기존 말풍선을 보존하며 `남은 적 N마리가 퇴각합니다` 토스트를 추가했다. 100 미만은 전멸해야 종료한다. AliveEnemies/RemainingEnemies에서 퇴각 개체를 제외하며 무리는 HordeCount로 센다.
- `Assets/_Project/Scripts/World/WildMonster.cs`: 전투 처리 전에 퇴각 분기로 반환한다. Threatening도 false가 된다.
- 신규 `Assets/_Project/Scripts/World/WildMonster.Retreat.cs`: 가장 가까운 현재 맵 가장자리를 목적지로 잡고 기존 NavMeshAgent·RequestDestination/NavRoute로 이동한다. 비행은 기존 MoveTowards 방식을 사용한다. 목적지 도착 또는 게임 시간 30초 경과 시 비활성화·Destroy하며 Die/포획/전리품 처리를 호출하지 않는다. 경로가 막혀도 공격으로 전환하지 않고 제한 시간에 제거된다. HordeVisual은 기존 HordeCount를 그대로 읽어 이동 중 떼 표시를 유지한다.
- `Assets/_Project/Scripts/Save/SaveRuntime.cs`: 저장 체력 계산만 변경했다.
- 신규 `AgentScripts/WaveRetreat1103Checks.cs`: 기존 Wave1102 패턴의 `public static async Task<string> Main()`, 내부 접근은 reflection. 격리 저장 경로·새 게임을 사용한다. 99마리 전멸 전용, 100마리 기준 4→3, 3000마리 기준 61→60, 생성 대기·EndGrace, 종료/평화/집계 제외, 도착·시간 제한 제거, 실제 생존 장수를 대상으로 공격 중단, 실제 TrySave→TryLoad→재저장을 검사한다. 설정·저장 경로·timeScale은 finally에서 복원한다.
- `docs/IMPLEMENTATION_BOARD.md`: 최상단 담당·상태 한 줄.

## 저장 방식

- 퇴각 개체의 DTO 체력만 0으로 기록한다. 런타임에서는 계속 걸어 나가므로 저장 자체가 퇴각 연출을 중단하지 않는다.
- 로드하면 기존 RestoreHealth(0) 경로로 비활성 복원되어 공격·웨이브 집계에 재진입하지 않는다. 저장 키/대상 참조는 보존하므로 별도 DTO 필드·버전·마이그레이션이 필요 없다.
- 사망 함수를 호출하지 않으므로 로드 때문에 처치 보상·사체·포획이 발생하지 않는다. 비활성 런타임 방문자는 이후 저장 대상에서 자연히 제외된다.
- 로드 이후 퇴각 이동 연출은 이어지지 않는다. 단순성을 위해 의도한 동작이다.

## 잠정 수치

| 상수 | 값 |
| --- | --- |
| RetreatFromTotal | 100마리 |
| RetreatMin | 3마리 |
| RetreatShare | 0.02 (2%, 올림) |
| WildMonster.RetreatTimeout | 게임 시간 30초 |

- 예: 총 100 → 3마리, 총 3000 → 60마리에서 퇴각.
- `WaveDefense.Count()`·MaxEnemies·IndividualCap·HordeSize는 수정하지 않았다. LocalIncursions·WaveBalance1103Checks·ExpeditionRules·ExpeditionTransport*·GoalChain*·CampaignNextSteps·prefab·scene·ProjectSettings도 수정하지 않았다.

## 검증 결과와 남은 실행

- Unity 6000.5.8f1 내장 dotnet/csc + `Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp`를 복사해 현재 게임 소스 목록을 갱신하고 Library 경로를 절대 경로로 변환했다. 출력·refout은 원본 Library에 쓰지 않게 Temp로 변경/제거했다.
- 별도 asmdef인 `_TeamImport` 및 Editor 소스를 게임 어셈블리 소스에서 제외했다. 최초 전체 Assets 병합 시 해당 어셈블리 경계 누락으로 생긴 ResourceType 충돌을 컴파일용 목록 수정으로 해소했다. 게임 소스 우회 수정 없음.
- 게임 DLL 컴파일: **exit 0, 오류 0**. 기존 경고 33건.
- Editor rsp를 사용해 현재 Editor 소스와 WaveRetreat1103Checks만 컴파일하고, 위에서 만든 게임 DLL을 별도 `-r:` 인자로 참조: **exit 0, 오류 0**. 기존 Editor 경고 13건. RunChecks.cs 등 무관한 최상위 실행 파일은 포함하지 않았다.
- 재실행: `powershell -NoProfile -ExecutionPolicy Bypass -File Temp/WaveRetreat1103/compile.ps1`. 응답 파일·DLL·컴파일 로그는 같은 Temp 폴더에 있다.
- `git diff --check`로 담당 변경의 공백 오류를 검사했다.
- `graphify update .`: exit 0. AST 기반 코드 그래프를 18,225노드·39,765관계로 갱신했다. 커뮤니티 재구성에 따라 기존 이름 322개가 대표 노드명으로 자동 조정되었다(LLM 이름 재생성은 하지 않음).
- **Unity/검사 Main은 실행하지 않았다.** Claude가 Play에서 `WaveRetreat1103Checks.Main()`을 실행하고 퇴각 이동·무리 표시·토스트·저장 왕복의 실제 동작을 확인해야 한다. 경로가 막힐 때 30초 제한 제거도 화면에서 확인할 항목이다.
- 카카오톡 완료 알림 도구가 이번 세션에 제공되지 않아 전송하지 못했다.
