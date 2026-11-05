# 2026-11-01 R1 버그 리뷰

- 대상: `review-1101.diff`(b45d257 대비 1,124줄) 전체와 `review-1101-newfiles.txt`의 15개 파일. Q1/Q2/claude2 게임성·인계 문서, DESIGN_RULES, log.md 및 실제 호출 경로 대조.
- 작업: `E:/Git/ant-wt/R1`, `codex/r1-1101`. 원본 `E:/Git/ant`는 읽기만. 새 기능·커밋·SAVE·캡처·Notion 변경 없음.
- 아래 줄 번호는 **R1 수정 후 파일** 기준. 심각도 P1=명령 취소, P2=잘못된 상태/정보·반복 연산, P3=세션 정리 누락.

## 확정 버그

경로 앞부분은 `Assets/_Project/Scripts/`.

| 파일:줄 | 심각도 | 증상 | 재현 조건 | 수정 여부 |
| --- | --- | --- | --- | --- |
| `Units/CommanderSleep.cs:61` | P1 | 비행 장수는 이동 후 수면 대기 조건에서 제외되어 다음 TickSleep이 SuspendForNeeds→CommandStop으로 방금 내린 이동을 취소함 | 본거지에서 수면 중인 날개 장수에게 먼 이동 지시 | 수정: 비행도 실제 도착까지 깨어 대기. 지상도 경로 보유만으로 도착 후 계속 기다리지 않도록 도착 여부 확인 |
| `UI/ControlGroups.cs:46` | P2 | 회수 시 비활성 대상을 저장 목록에서 제거해, 탑승 중인 장수가 하차해도 부대에서 사라짐 | A/B를 Ctrl+1 지정→A 탑승→1→A 하차→1 | 수정: 파괴된 참조만 영구 삭제하고, 활성 대상만 임시 선택 목록에 담음 |
| `UI/ControlGroups.cs:22` | P3 | 정적 groups/idleIndex/lastGroup/lastTime이 판 종료 후에도 남음. 빈 부대의 파괴된 참조도 기존 코드는 반환 전에 남겨둠 | 부대 지정·회수 후 같은 맵 재시작/로드. 도메인 리로드 없이 새 SelectionManager 생성 | 수정: SelectionManager가 바뀐 첫 Handle에서 전체 세션 상태 초기화. 즉시 씬 종료 콜백 방식은 아님 |
| `UI/ControlGroups.cs:60` | P2 | 쓰러져 행동 불능이지만 활동 문자열이 빈 장수를 유휴 인원으로 선택·카메라 이동함 | 현장에서 쓰러져 있고 병상/작업/수면/붕괴 표시가 없는 장수에게 `.` | 수정: 기존 CanReceiveOrders 판정 재사용 |
| `UI/WorldMapPanel.cs:391` | P2 | 월드맵을 연 매 Update마다 Range/Where/OrderBy/ToList 및 마커별 캡처 람다를 생성함 | 여러 거점이 보이는 월드맵을 계속 열어 둠 | 수정: 선택 거점 우선 2회 순회와 직접 겹침 비교. 해당 검사에서 LINQ·캡처 제거. 전체 UI 무할당을 뜻하지 않음 |
| `Units/CommanderHealthCare.cs:361` | P2 | 시작된 수술도 매 프레임 SurgeryBlock→AutoSurgeon으로 전 장수 LINQ 정렬·TryWorkApproach/NavMesh 경로 검사를 수행하고 결과를 버림 | 병상에서 진행 중인 수술, 의료 가능한 장수가 여러 명 | 수정: Waiting 상태를 먼저 확인해 시작 전 판정만 수행. 시작된 수술의 병상/환자 유효성 검사 유지 |
| `UI/CampaignNextSteps.cs:45` | P2 | 다음 준비 패널만 감속 전 Remaining을 표시하며, 이동 불가·착륙/접안 대기도 0초 또는 이동 중으로 안내함 | 0.75배 속도 차량 Remaining=30이면 실제40초인데30초. 본거지 적 때문에 귀환 대기하면0초 | 수정: Q1 TravelRemainingSeconds/TravelWaitReason을 재사용 |
| `Boss/BossProgress.cs:65` | P2 | B 안내에서 실제 열 피해12를 숨기고 범위 장비만 권장함. Q1 문서의 'B 열0' 전제가 실행 규칙과 다름 | 보스 B의 예고 공격 피격: BossEncounter.Tick→BossEncounterRules.Heat(B)=12 | 수정: 실행 Heat(B)를 직접 읽어 방열·범위 장비 안내. 전투 수치 변경 없음 |

## 의심·미재현

- 지상 이동 종료 시 `Agent.hasPath`가 계속 참이면 기존 수면 대기가 밤 내내 유지될 수 있다. SoldierAnt의 일반 이동 완료는 state만 Idle로 바꾸며 ResetPath하지 않는다. 엔진의 실제 경로 종료 동작은 미검증이므로 별도 확정 건수에 포함하지 않았다. 수면 수정에 실제 도착 판정도 넣었다.
- 자동 탑승/철수의 반복 CommandMove가 매번 '잠 설침'을 붙인다는 의심은 **현재 호출 경로에서 확인되지 않음**. WakeForDuty는 asleep일 때만 실행하고 즉시 false로 바꾼다. 본거지 이동 대기와 원정 canSleep 제한 때문에 매 호출 재부여로 단정할 수 없다.
- WorldMap 라벨 좌표는 같은 mapRoot의 좌상단 anchor/pivot과 anchoredPosition을 사용한다. `y-MarkerSize`는 아래쪽 경계라 부호 오류를 확인하지 못했다. 실제 화면·배율별 겹침은 미검증.
- CommanderDevelopment의 감소 조건·10단계 하한·경험치 문구, MedicalOutlook의 상처·면역 계산은 실제 규칙과 대조했고 확정 수치 오류를 찾지 못했다.
- ControlGroups의 파괴 참조는 Unity null 단락 평가로 접근을 막는다. 파괴 참조 접근 예외가 재현된 것으로 보고하지 않는다.
- Q1 Planning 출정 조건은 이전 TryDepart의 조건을 보존한다. BossStronghold.Planning의 현지/운송/본거지 수량은 기존 보상 상태에서 계산한다. 새 저장 필드가 없어 DTO 추가 누락을 찾지 못했다. 완전한 저장 왕복·구저장 로드는 실행하지 않았다.
- 원문 키/기분 이유/관계 ID/log.md/changelog.md 보존 경로를 변경하지 않았다. 원본 log.md도 수정하지 않았다.

## 검증

- 사용자 지정 `offc.sh E:/Git/ant-wt/R1`: **exit0, 오류 0**. 첫 실행은 worktree에 없는 타사 `SpinFree` 때문에 실패. 원본 `Assets/Planet Earth Free/SpinFree.cs`를 R1 동일 경로에 검증용으로 읽기 복사한 뒤 같은 명령으로 통과.
- 후속 로컬 응답 파일에는 나머지 누락 타사 소스42개를 원본 읽기 참조로 추가해 전체 게임 재컴파일 **오류 0**. 검사 DLL/오프라인 실행 호스트도 **오류 0**.
- 실제 오프라인 실행: **Q1 86 / Q2 135 / R1 3 통과**. R1 검사는 B/C/D 위험 안내의 열 피해를 실제 규칙과 비교한다. UI 입력·수면·수술·경로의 Unity 실행 통과를 뜻하지 않는다.
- 로그: `Temp/R1-validation.txt`, `Temp/off/out.txt`, `Temp/off/cout.txt`. 재현: `powershell -NoProfile -ExecutionPolicy Bypass -File Temp/Review1101R1Compile.ps1 -OfflineChecks`.
- `git diff --check`, 수정 전용 패치의 `git apply --reverse --check` 통과. Graphify AST 갱신 완료: 12,576노드 / 29,348간선 / 564커뮤니티. 커뮤니티 변경으로 335개 라벨을 허브 이름으로 자동 변경했다는 경고가 있으며 LLM 재라벨링은 하지 않았다. 로그 `Temp/R1-graphify.txt`.

## 전달 파일과 기준선

**R1에서 실제로 고친 런타임 파일 6개:**

- `Assets/_Project/Scripts/Boss/BossProgress.cs`
- `Assets/_Project/Scripts/Units/CommanderSleep.cs`
- `Assets/_Project/Scripts/Units/CommanderHealthCare.cs`
- `Assets/_Project/Scripts/UI/ControlGroups.cs`
- `Assets/_Project/Scripts/UI/WorldMapPanel.cs`
- `Assets/_Project/Scripts/UI/CampaignNextSteps.cs`

추가: `AgentScripts/Review1101R1Checks.cs`, 본 보고서, `docs/IMPLEMENTATION_BOARD.md` 담당/결과 표시, `docs/patches/REVIEW_1101_R1.patch`.

R1에는 오늘 변경이 없었으므로 리뷰 대상 C# **50개를 원본 현재 파일에서 먼저 복사**해 컴파일 기준선을 구성했다. 이 복사분은 R1의 새 기능 구현이 아니다. 목록 `Temp/review-r1-baseline-files.txt`, 수정 전 사본 `Temp/review-r1-baseline/`. 리뷰 중 원본의 SelectionManager 공개 API가 `AddToSelection` 대신 `Add`로 좁혀져 있었으며 현재 원본을 복사해 맞췄다.

**병합은 git diff 전체 대신 수정 전용 `docs/patches/REVIEW_1101_R1.patch`를 사용한다.** 이 패치는 복사 당시 원본 대비 위 6개 파일의 수정만 포함한다. 타사 SpinFree·기준선 복사분·Graphify·prefab은 포함하지 않는다. 최신 원본에 대한 무충돌 적용은 통합 담당자가 확인해야 한다.

카카오톡 도구가 이 세션에 없어 완료 알림은 발송하지 못했다.