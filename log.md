# 프로젝트 로그

## 프로젝트 구조와 규칙
- 개미 소굴 RTS / Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1.
- E:\Git\ant, origin github.com/ARHIENE/ant-colony-rts. develop에서 개발, master는 안정 버전.
- Assets/Scripts/: Core/Data/Units/Buildings/World/Boss/UI/Map. 씬 Assets/Scenes/AntColony.unity, 설정·검사 AgentScripts/.
- .prefab/.prefab.meta와 Assets/_TeamImport/ 커밋 금지. Ponytail full, 공식 Unity CLI/Pipeline 우선. 개발 일지·캡처는 SAVE 때만.

## 2026-09-14 SAVE — 적 소굴 AI 경제 성장과 코드 검토 반영
- 적 소굴이 살아 있는 건물 수에 비례해 10초마다 Food·Soil을 벌고, Soil을 써서 최대 5동까지 확장하며, 침공 개체 1마리당 Food를 소비한다. 전리품 노드가 곧 소굴 창고다.
- 경제 구현은 Codex가 했고, 이어진 코드 검토에서 나온 지적을 이번에 반영했다.
- 수입에 Food 300 / Soil 200 상한을 둬 장기전에서 전리품이 무한히 불어나지 않게 했다.
- 확장 건물은 본진에서 초기 배치 최소 거리(30m) 안쪽에는 세우지 않는다.
- 확장은 건물만 복제한다. 복제본에서 전리품 노드와 소굴 제어 컴포넌트를 떼어내고, 소굴 루트를 겸하는 건물은 복제 템플릿에서 제외한다.
- 창고 노드 조회를 시작 시점에 한 번만 고정해, 확장 복제본이 창고를 가로채거나 매 틱 탐색이 반복되지 않게 했다.
- 재고 부족이나 스폰 위치 실패로 한 마리도 내보내지 못한 침공 파동은 통째로 사라지지 않고 15초 뒤 다시 시도한다.
- Food/Soil 전리품 노드가 없는 소굴은 침공을 경제 도입 전처럼 무상으로 보내고 확장은 하지 않으며, 시작할 때 경고를 남긴다.

## 검증
- Unity 컴파일 오류 0건. 씬 `AntColony`에서 Play 약 70초 동안 콘솔 오류·경고·로그 0건.
- Play 중 `EnemyNestPrototype` 아래 `Enemy Nest Building 3`, `4`가 차례로 생성돼 자원 기반 확장이 실제로 동작함을 확인했다. Play 종료로 씬 상태는 원복했다.
- 씬 구조 확인: 전리품 `Nest Stock Food/Soil/Special`과 `InvasionSpawn`은 소굴 루트의 직속 자식이고 건물에는 자식이 없다. 시작 경고가 뜨지 않아 노드 설정 누락도 없다.
- 검사 4종을 각각 새 Play 모드에서 실행해 전부 통과했다.
  - `EnemyColonyEconomyChecks`: PASS — 수입, 전리품 상한, 확장, 생산 예산, 소굴 파괴 후 수입 정지. 뒤 두 항목은 이번에 새로 추가한 판정이다.
  - `InvasionChecks`: PASS — 지연·증가·상한 웨이브, NavMesh 스폰, 행군·건물 공격, 방어 요격, 오판정 없음, 비활성/재개, 파괴된 소굴 스폰 정지.
  - `EnemyColonyPlacementChecks`: PASS — 무작위 배치 5항목.
  - `RegressionChecks`: PASS — 44항목 전부.
- 검사 후 Play를 종료해 씬 상태를 원복했다. 캡처는 `Assets/.unity/save-2026-09-14-enemy-colony-economy.png`.

## 다음 작업과 미정 사항
- 다음 작업은 번식·영입·포로다.
- Soil은 확장 외 소비처가 없어 상한에 머문다. 다수 AI 세력·AI 간 전투·유닛 약탈, Special 소비처·보스 전리품은 후속 작업.
- 병력 0 장수는 선택·보충 대상으로 남음. 사망·포로 규칙과 건설 예약만 남았을 때 유지비 정책은 미정/임시.
- 경제 수치(수입 2/1, 침공 5, 확장 Soil 30, 상한 300/200, 재시도 15초)는 전부 1차 프로토타입 값이다. 난이도 설정이 생기면 그쪽에서 가져온다.
- Support 수치와 시각 효과는 밸런스·연출 단계에서 확정한다.
- 기획 부모 `334c4a0ecd3180c4a796e5220302a0bd`에는 replace_content+allow_deleting_content 조합을 사용하지 않는다.

## SAVE 결과
- 수정 파일: `Assets/Scripts/World/EnemyColony.cs`, `Assets/Scripts/World/ColonyInvasion.cs`, `AgentScripts/EnemyColonyEconomyChecks.cs`, `AgentScripts/InvasionChecks.cs`, `README.md`, `log.md`, `changelog.md`.
- 커밋 2개(`feat: add enemy colony economy growth`, `docs: record enemy colony economy save`)로 나눠 develop에 push했다.
- 에이전트 스킬(`.agents/skills/`, `.claude/skills/`)은 로컬 전용이라 git 인덱스에서 제거했다.
- 개인 도구 설정(.claude/settings.json, .codex/), .gitattributes, graphify-out/, Assets/_Recovery/와 메타 파일은 로컬에 보존한다.
- 사용자는 앞으로 모든 GitHub push를 승인했다.
