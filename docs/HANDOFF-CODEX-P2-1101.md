# P2 인계 — 2026-11-01

작업: `E:/Git/ant-wt/P2`, `codex/p2-1101`. 원본 `E:/Git/ant`는 지시 문서·Bee rsp·Unity 참조 DLL·외부 SpinFree 소스 읽기에만 사용했다. Unity 에디터 실행/조작·캡처·커밋·push·SAVE 일지·Notion 쓰기 없음. P1 금지 파일 수정 없음. prefab 생성 없음.

## 1단계 판정

| 항목 | 판정 | 수정 파일·확인 경계 |
| --- | --- | --- |
| Missing building:0 | 정적으로 재현 가능한 원인 경로 수정. 원래 단발과 동일 원인인지는 Play 확인 필요 | CrashLandingStart가 카탈로그 작성 후 기본 시설을 비활성화+지연 Destroy한다. 같은 프레임 Capture는 살아 있는 참조를 정상 건물로 기록하고, Restore 첫 yield 이후에는 참조가 사라진다. SaveCatalog.RemoveBuilding으로 삭제 예약 순간 슬롯을 null 처리하고 GameBootstrap의 Busy 해제도 한 프레임 늦춤. SaveSnapshot.Restore는 누락 시설을 장수 제거 전 거부. ID0의 미등록 문제가 아니라 배열 첫 슬롯이다 |
| AI 산업·기술·장비·전술 | 최소 구현·오프라인 규칙 검사 완료 / 실전 잔여 | AIIndustry 신규, 기존 EnemyColony의 실제 건물 슬롯에 본부/정련/공방/연구/지휘 역할. 현장 자원 생산·교체 장비 제작·재고 소비·구형 회수·유상 재건, 연구 시설 조건·원거리 거리 확보. 신규 모집의 기본 무기는 기존 모집비에 포함하며 전체 독립 산업/연구 트리를 재현하지 않음 |
| 보스 미완성품 인계 | 수동 인계 코드·검사 진입점 구현 / 실제 장면 잔여 | Workshop.Boss·Workshop·GameMenuWorkshop. 사망/Left/명부 소실 시 후임으로 makerId만 변경. 일반 장비 주문은 원제작자 고정 유지. 일시 부재/정신 이상/원정은 자동 인계하지 않음 |
| 구저장 병력 장비 | 1회 소급 및 다음 저장 연결·오프라인 검사 완료 / 전체 왕복 잔여 | MonsterDto의 militaryVersion/era/member/equipment → SaveRuntime 캡처/복원 → SavePreflight 버전·종류·ID 중복 검사. 기본0이 구저장이며 새 저장은version1. 저장24 선택 필드 확장, 이전 빌드가 새 저장을 읽는 역호환은 지원하지 않음 |
| 장기 난도·장갑 | 계산 판정·회귀 통과 / 장기 플레이 잔여 | 장갑0~10→0~5, 지휘소 없으면 절반. 공격8·체력300 모델 최대300타→100타. 6시간 연구/생산 모델에서 정상6시대·연구소 상실3시대 정지·잔고 비음수. 실제 손실·교역·이동·난이도별 전쟁은 별도 |
| N2 비육안 | 코드 보완 / 육안 잔여 유지 | SelectionOutline의 Collider.bounds를 현재 위치/회전/스케일에 맞춰 갱신. 월드맵 산업 정보는 기존 WorldMapMarker 툴팁 재사용. 투영·가시성·선택 프레임·기호의 새 누락은 정적 검토에서 발견하지 않음. 실제 색각/해상도 검증을 했다는 의미는 아님 |

저장 초기화의 다른 후보(씬 재진입 정적 목록, 런타임 건물 미등록, 템플릿 지연 생성)는 코드 추적으로 검토했다. 해당 오류 문자열은 non-runtime 카탈로그 슬롯에서만 발생하며 위 삭제 대기 경로만으로 조건이 성립한다. 원본의 단발 오류 로그/콜스택을 새로 얻지는 못했다. 에러를 무시하거나 손실된 건물을 임의로 되살리는 처리는 넣지 않았다.

L2 추가 공백: XP 배율 적용 후 절대 상한은 미구현(배율 전200 유지, P1 성장/다이아 경로 통합 필요). 전체 개별 서사 아카이브·예약 증원 자동 제한·임시 거처 전용 모델은 현재 정책 유지 또는 잔여이며 구현한 것으로 표시하지 않는다. 자세한 근거는 DECISIONS_1101_P2.

## 2단계

`GAMEFEEL_1101_P2.md`:27건, 상·중17건 코드 보완, 하10건 잔여(부분 구현1건 포함). 각 행에 코드 근거·레퍼런스 비교·추천안·미구현 이유를 기재했다. 신규 대규모 진형·물리 보급대·후기 승리 조건·전체 그래픽은 추가하지 않았다. 값과 선택은 모두 `DECISIONS_1101_P2.md`의 **추천안 자동 선택(사용자 미확인)**이다.

## 검사

- 게임: 사용자 제공 `offc.sh /e/Git/ant-wt/P2`, **exit0·오류 0**.
- 검사 컴파일: `offcheck.sh /e/Git/ant-wt/P2 Review1101P2Checks.cs Review1031M2Checks.cs Review1031ClaudeMilitaryChecks.cs`(각 검사 실제 절대 경로 전달), **exit0·오류 0**.
- Unity 내장 dotnet/Roslyn으로 만든 `Temp/off/A.dll`에 대해 .NET8 오프라인 실행: P2 **181**, M2 **298**, L2 **97**, N2 **77**, Boss **38**, Finale **31**, K1 **87**, K2 **107** 통과. 범위가 겹치므로 개별 기능 수와 동일하지 않다. 기존 사용 중단 API/FormatterServices 경고는 유지했다.
- P2 Main: 프레임 삭제 전 카탈로그 tombstone·키 보존,6시대 장갑/프로필/장비/고체력 재저장 상한/재능 보존, 산업 역할별 소득·연구소 상실,6시간 연구 계산, 잘못된 군사 버전·시대·순번·프로필·중복ID 거부.
- M2의 두 기대값은 바뀐 정책에 맞춰 갱신했다(장갑=시대-1, 복원 상한=max(프로필,저장 체력)). 종전 기능 검사를 지우거나 우회하지 않았다.
- `Review1101P2Checks.RunP2Scene()` **컴파일만 완료·미실행**. 내부 접근은 리플렉션, 타입은 AntColony.* 전체 이름 사용. `RunP2Scene`은 현재 게임을 새 게임으로 교체하므로 Claude의 격리 Play에서만 호출할 것.
- Unity Native/JsonUtility/SaveSystem.LoadScene/실전투/UI·Player 빌드 미실행. 근거 로그: `docs/validation/P2-1101-offline.txt`. Graphify AST 갱신 완료:12338노드/28769간선/566커뮤니티. 커뮤니티249개 이름이 허브 기준으로 변경됐고 의미 라벨 재생성은 하지 않았다. 그래프 산출물은 게임 병합 필수 파일과 별개다.

오프라인 컴파일 시 worktree에 없는 외부 `Assets/Planet Earth Free/SpinFree.cs`가 필요했다. 원본 파일을 같은 상대 경로에 잠깐 복사해 제공된 offc.sh를 실행하고 복사본은 제거했다. 해당 외부 소스는 패치에 포함하지 않는다. 원본 파일 쓰기·변경은 하지 않았다. 같은 worktree에서 재검사할 때만 동일한 읽기 복사가 필요하다.

## Claude 실행·화면 확인 요청

1. 패치 반영·Unity 컴파일 후 Main, RunP2Scene, 기존 Review1031ClaudeMilitaryChecks.NoWait를 격리 Play에서 실행. RunP2Scene은 추가 대기 없는 최초 Capture/저장, 누락 시설 복원 전 거부, 사망 제작자 수동 인계, 실제 공방 생산/파괴 중단, 전체 TrySave→TryLoad→다시 저장 검증, 구필드 누락 저장 이관을 검사한다.
2. 원거리 후퇴가 좁은 길·벽·5배속에서 사격을 막지 않는지, 방패/산 분사기/단궁의 실제 전선·사거리 확인. 공격 대상이 겹쳤을 때 후퇴할 공간이 없으면 기존 공격을 계속해야 한다.
3. AI 공방 파괴→교체 장비 공급 중단, 연구소 파괴→후기 연구 정지, 유상 재건→복구, 지휘소 파괴→장갑 감소를 실제 공격으로 확인. 실물 장비 처치 드롭/수거/포로 전환 뒤 중복 없음 확인.
4. 외교의 시대 비용·재고/산업 툴팁, 월드맵 기존 선택/기호/뒤쪽 가시성, 대상 패널의 시설 역할·무기 정보, 공방 인계 버튼을1280/1920에서 확인. WorkTargetPanel 수정은 AI 분기13줄만이므로 원본/P1의 같은 파일 수정과 hunk 단위 병합할 것.
5. N2의 배치+/X·선택 외곽·마름모와 작은 화면 겹침은 기존 잔여 그대로 확인. **캡처는 SAVE 때만** 수행한다.
6. 잠정 규칙을 통합 확정하면 관련 Notion 기획 하위 페이지·참조 일지를 함께 확인. 부모 페이지 전체 교체 금지. 카카오 완료 알림 도구는 이 세션에 없어 발송하지 못했다.

## 수정 파일

- 신규: `World/AIIndustry.cs`·`WildMonster.Military.cs` 및 `.meta`, `AgentScripts/Review1101P2Checks.cs`, 결정·게임성·본 인계·검증 로그.
- 저장: `Save/SaveCatalog.cs`, `SaveDtos.cs`, `SavePreflight.cs`, `SaveRuntime.cs`, `SaveSnapshot.cs`.
- 시작 경계: `Core/CrashLandingStart.cs`, `GameBootstrap.cs`.
- AI/전투: `World/AIEraMilitary.cs`, `AIEraProgress.cs`, `DiplomacyManager.cs`, `EnemyColony.cs`, `ExpeditionSite.cs`, `WildMonster.cs`.
- 제작: `Buildings/Workshop.Boss.cs`, `Workshop.cs`.
- 표시: `UI/GameMenuDiplomacy.cs`, `GameMenuWorkshop.cs`, `WorldMapMarker.cs`, `WorkTargetPanel.cs`; `Units/SelectionOutline.cs`.
- 회귀 기대값: `AgentScripts/Review1031M2Checks.cs`.
- 현행 연결: README, IMPLEMENTATION_BOARD, OPERATIONS_100_STATUS, GAPS_1031_L2/M2/N2. 과거 HANDOFF/결정 원문은 역사 기록으로 보존.

패치: `E:/Git/ant-wt/P2/docs/patches/P2-1101.patch`.

- 기본 `git add -N .`는 `E:/Git/ant/.git/worktrees/P2/index.lock: Permission denied`로 거부됐다. 대신 `GIT_INDEX_FILE=Temp/off/P2.index`, `GIT_OBJECT_DIRECTORY=Temp/off/git-objects`, `GIT_ALTERNATE_OBJECT_DIRECTORIES=E:/Git/ant/.git/objects`로 P2 내부 임시 인덱스·객체 저장소를 만들고 `read-tree HEAD` → 요청한 `git -C <worktree> add -N .` → `git diff --binary --output=...`를 수행했다. 원본 Git 메타데이터도 수정하지 않았다.
- 요청한 공용 경로 `E:/Git/ant-wt/patches/P2-1101.patch` 출력 역시 `could not open ... for writing: Permission denied`로 거부돼 위 P2 내부 경로로 전달한다. 권한 요청·커밋은 하지 않았다.
- 패치에는 사용자 지정 전체 diff 방식에 따라 Graphify 산출물·캐시도 포함된다. 게임 병합 시 이 산출물은 별도로 다뤄도 된다. 패치 파일 자신은 패치에 포함되지 않는다.
