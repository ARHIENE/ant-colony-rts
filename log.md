# 프로젝트 로그

## 프로젝트 구조와 규칙
- 개미 소굴 RTS / Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1.
- E:\Git\ant, origin github.com/ARHIENE/ant-colony-rts. develop에서 개발, master는 안정 버전.
- Assets/Scripts/: Core/Data/Units/Buildings/World/Boss/UI/Map. 씬 Assets/Scenes/AntColony.unity, 설정·검사 AgentScripts/.
- .prefab/.prefab.meta와 Assets/_TeamImport/ 커밋 금지. Ponytail full, 공식 Unity CLI/Pipeline 우선. 개발 일지·캡처는 SAVE 때만.

## 2026-09-15 SAVE — 장수 획득 경로 3종(번식·영입·포로)

기획 "장수개미 시스템"의 장수 획득 경로 3가지를 구현했다. 지금까지 장수는 게임 시작 시 12명이 고정 생성될 뿐 늘릴 방법이 없었다.

### 공통 기반
- `CommanderTraits`(신규): 장수 성격(Balanced/Brave/Cautious/Devoted)과 충성심(0~100). `CommanderProgression`처럼 순수 데이터라 보직·관직·병력 변경으로 초기화되지 않는다.
- 성격이 전투 수치에 직접 반영된다. 용감형 +공격/-방어, 신중형 +방어/-공격. 기존 레벨·병영·지원 보너스와 같은 경로로 더해지며, 1마리분 공격력이 음수가 되지 않게 0으로 하한을 뒀다.
- `CommanderRoster`를 장수 "생성 창구"로 바꿨다. 시작 장수·번식·영입·포로 회유가 모두 `Create()` 하나를 거치므로 장수를 만드는 방법은 한 곳에만 있다. 싱글턴으로 노출해 세력 규모(`Count`)를 다른 시스템이 읽는다.

### 1. 번식 — `NurseryChamber`(양육실)
- 반경 안에 함께 있는 장수 쌍에게 호감도가 쌓이고, 기준치를 넘고 식량·정원 조건이 맞으면 출산한다. 장수 x 장수만 가능하다.
- 태어난 장수는 부모 중 한쪽의 성격을 물려받고 충성심은 부모 평균 ±10, 전투 보직도 부모에게서 계승한다.
- 이 컴포넌트가 곧 양육실이라 건물이 파괴·비활성화되면 번식이 멈춘다.
- 출산 후 해당 쌍의 호감도를 지워 매 틱 연속 출산하지 않는다. 자리를 못 잡아 생성이 취소되면 낸 식량을 환불한다.

### 2. 스카우트 영입 — `ScoutPost`
- 식량과 개미 1마리를 써서 파견하고, 이동 시간이 지나면 확률 판정으로 장수가 합류한다. 한 번에 한 건만 진행된다.
- 성공률은 세력 규모(장수 수)에 비례해 오르고 상한이 있어 확정 영입이 되지 않는다.
- 파견 개미는 성공·실패와 무관하게 귀환 시 대기 풀로 돌아온다. 비용을 못 내면 파견 자체가 거부되고 개미도 차출하지 않는다.

### 3. 포로 — `PrisonerCamp` + `EnemyCommander`
- 적 소굴의 침공 파동에 적 장수가 따라붙는다(`ColonyInvasion`, 템플릿 미지정 시 기존과 동일 동작).
- 쓰러진 적 장수는 수용소에 자리가 있으면 포로가 되고, 없으면 평소대로 죽는다.
- 회유는 확률 판정이다. 충성심이 높을수록 어렵고 헌신형은 더 어렵다. 반복 시도할수록 확률이 조금씩 오른다.
- 실패해도 포로는 남아 다시 시도할 수 있고, 성공하면 이름·관직·보직·성격을 유지한 채 내 장수로 합류한다. 언제든 처형할 수 있으며, 방치하면 주기적 판정으로 탈출한다.

### 설계 결정(기획 미정 사항을 프로토타입에서 이렇게 정함)
- 기획의 "병력 0 이후 사망/포로 규칙"은 여전히 미정이다. 이번에는 **적 장수에만** 포로 경로를 넣었고, 플레이어 장수의 정책(`CommanderAnt.IsDead == false`)은 건드리지 않았다.
- 적 장수는 일반개미 풀을 쓰지 않으므로 `WildMonster`의 체력을 그대로 쓰고, 체력 0을 "무력화"로 보아 포로로 넘긴다. 이를 위해 `WildMonster`의 사망 처리를 `protected virtual Die()`로 추출했다(동작 변화 없음).
- 양육실·스카우트 파견소·수용소는 `BuildingData` 에셋 없이 동작하는 컴포넌트로 만들었다. 실제 건물에 붙이면 그대로 건물 기능이 된다.

## 검증
- Unity 컴파일 오류 0건, 콘솔 오류 0건.
- `CommanderAcquisitionChecks`(신규): PASS — 59항목. 번식(식량 부족 시 미출산, 호감도 충족 출산, 유전, 출산 후 호감도 초기화, 거리 조건, 정원 초과 시 식량 미소비), 영입(파견 비용·개미 차출, 중복 파견 거부, 도착 전 무결과, 성공/실패, 세력 규모에 따른 확률 상승, 비용 부족 시 거부), 포로(포획, 정원 초과 거부, 충성심별 확률, 회유 실패 시 유지·재시도, 회유 성공 합류, 처형, 탈출), 적 장수→포로 연결, 성격의 전투 수치 반영.
- 회귀 검사 전부 통과(각각 새 Play 모드): `RegressionChecks` 44항목, `CommanderChecks`, `CommanderProgressionChecks`, `InvasionChecks`, `EnemyColonyEconomyChecks`.
- 검사 후 Play를 종료해 씬 상태를 원복했다. 캡처는 `Assets/.unity/save-2026-09-15-commander-acquisition.png`.

## 다음 작업과 미정 사항
- 획득 경로 3종의 UI가 없다. 현재는 코드/검사로만 조작 가능하며, HUD에 양육실·파견·수용소 패널을 붙이는 것이 다음 작업이다.
- 양육실·파견소·수용소를 실제 건설 가능한 건물(`BuildingData`)로 승격하는 작업이 남아 있다.
- 장수 사망 규칙이 확정되면 플레이어 장수도 포로가 될 수 있어야 한다. 현재는 적 장수만 해당한다.
- 숙련도 성장, 연구소 개별 강화, 액티브 스킬은 아직 미착수다.
- 번식 수치(호감도 10/초, 기준 100, 식량 30), 영입 수치(식량 20, 30초, 기본 25%+장수당 2%, 상한 80%), 포로 수치(회유 기본 60%, 충성심 페널티 50%, 재시도 +5%, 탈출 30초마다 10%)는 전부 1차 프로토타입 값이다.
- 기존 미정 사항(다수 AI 세력, Special 소비처, 보스 전리품, 유지비 정책)은 그대로 남아 있다.
- 기획 부모 `334c4a0ecd3180c4a796e5220302a0bd`에는 replace_content+allow_deleting_content 조합을 사용하지 않는다.

## SAVE 결과
- 신규 파일: `Assets/Scripts/Units/CommanderTraits.cs`, `Assets/Scripts/Buildings/NurseryChamber.cs`, `Assets/Scripts/Buildings/ScoutPost.cs`, `Assets/Scripts/Buildings/PrisonerCamp.cs`, `Assets/Scripts/World/EnemyCommander.cs`, `AgentScripts/CommanderAcquisitionChecks.cs`.
- 수정 파일: `Assets/Scripts/Core/CommanderRoster.cs`, `Assets/Scripts/Units/CommanderAnt.cs`, `Assets/Scripts/World/WildMonster.cs`, `Assets/Scripts/World/ColonyInvasion.cs`, `README.md`, `log.md`, `changelog.md`.
- develop에 커밋·push하고, master도 develop으로 fast-forward해 잔디 집계에 반영한다(기본 브랜치가 master라 develop 커밋만으로는 집계되지 않는다).
- 세션 중 Unity 에디터가 한 번 종료돼 브릿지가 끊겼고, 에디터를 재시작해 복구했다.
