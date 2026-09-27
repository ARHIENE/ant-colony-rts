# 작업표·징집소 로직과 UI 연결

기준: [메인 게임 화면 구체화, 2026-09-26](https://app.notion.com/p/3e7c4a0ecd3181e38211f6be9c44637d). 작업표·징집소 편성 UI 연결까지 반영한다.

| 용도 | API |
|---|---|
| 작업 체크박스 | `commander.AllowsJob(job)`, `SetJobEnabled(job, enabled)` |
| 고정 우선순위 | 건설 → 제작 → 연구 → 농사 → 낚시 → 채집 (`CommanderJobs`) |
| 채집 금지·해제 | `ResourceNode.GatheringForbidden` |
| 이 작업 먼저 | 기존 `CommandGather(node)`, `CommandBuild(site)`; 작업 종료 후 작업표 재개 |
| 징집소 건설 | `BuildingKind.ConscriptionPost`, 기존 `BeginPlacement(kind, role, builder)` |
| 출전 편성 | `post.TryDeploy(commanders, troops)`; 중복·한도·부족 인원 검사 후 전체 편성을 한 번에 적용 |
| 출전 상태 | `IsDeployed`, `IsReturning`, `WorkState.duty` |
| 즉시 귀환 명령 | `ReturnToPost()`; 귀환 지점에 도착하면 생존 병력을 풀에 반환 |
| 체력 표시 | `PersonalHealth`, `GameBalance.CommanderHealth`, `TroopHealth`, `TroopCount` |

- 새 장수는 평시 병력 0으로 시작한다. 민간인은 공격 대상이 되며 개인 체력이 소진될 때 기존 부상·사망 판정을 받는다.
- 출전 시 연구·제작 배정을 해제한다. 제작 대기열, 건설 현장과 진행도는 보존한다. 운반 자원은 현장 자원 노드로 남긴다.
- 주변 적이 없으면 20초 후 귀환한다. 출전 장수 수 제한은 없고 병력은 개인 지휘 한도 내에서 편성한다. 징집소는 건설 중인 현장까지 포함해 본거지 한 곳만 배치한다.
- 작업 체크 해제는 이미 시작한 작업을 취소하지 않는다. 채집 금지는 진행 중인 채집도 중지하며 이미 얻은 자원은 반납한다.
- 저장 v8: 작업표, 개인 체력/회복, 출전·귀환, 운반 자원, 채집 금지를 저장한다. 이전 저장의 병력은 출전 상태로 보존한다. 본거지 채집은 복원 후 화물을 반납하고 작업표를 다시 평가한다. 건설 중 저장은 기존처럼 제한한다.
- 개인 체력 100·쓰러진 뒤 회복 60초·징집소 F30/S50/인력4/8초·적 감지 반경12m는 잠정값이며 `GameBalance`에서 조정한다.
- UI: `GameMenuDuty`는 작업표, `GameMenuConscription`은 징집소 편성을 담당한다. 장수 관리·하단 카드에서 접근하며, 건설 특수 탭과 징집소 클릭도 연결했다. E는 단일 선택 시 징집소, D는 귀환이다. 기존 키 설정 인덱스는 유지한다.
- 선택 패널의 개인 체력과 병력 바를 분리했다. 평시 병력 바는 숨기며, 병력 0인 건강한 민간 장수를 쓰러짐으로 표시하지 않는다.
- 노드 좌클릭 → `GameMenuResource.ShowResourceNode`에서 채집 금지/허용 전환. 기존 플래그와 저장 v8을 재사용한다. `GatheringUIChecks` 14개로 클릭·수동/자동 채집 차단/재개·화물 보존·저장 복원을 검증했다.
- 작업 대상별 인력 배치·속도 공식, 낮밤·바이옴, 머리 위 상태 표시·경보음·채집 금지 일괄 지정 및 월드 표시는 후속 범위다.

UI 검증: `DutyUIChecks.Main` 46개 통과(6개 작업 체크, 편성 인원 부족 차단, 2명 출전, 귀환·인력 반환, 체력 표시, 상태 변경·징집소 비활성화 대응, 저장 복원, 체크박스 배치 경계).

검사: `AgentScripts/AutonomousDutyChecks.cs` (Unity Play, `AutonomousDutyChecks.Main`). 실제 반복 채집/반납, 출전 원자성, 개인 체력, 귀환, 연구·제작, 건설 중단 보존, 저장 왕복·이관을 확인한다.

검증 결과: 신규 52, Regression 46, Campaign 87, PlayableLoop 46, FullUI 48, WeaponTalent 93, SaveRoundtrip 42, WorldMap 746개 통과. 기존 검사의 평시 병력 배정 전제는 민간 작업·출전 준비로 변경했다. Unity 컴파일 오류 없음.
