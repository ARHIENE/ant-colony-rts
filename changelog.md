# 변경 이력

이 파일은 세션별 상세 작업 기록의 아카이브입니다. 최신 요약/현재 상태는 `log.md` 참고.

---

# 2026-09-26 SAVE — 이전 로그(2026-09-25 의무실·압축 저장) 이관
- 의무실: Infirmary 연구 후 건설, 8m 내 유휴 장수 동시 2명, 중상 4분 치료·충성심 +3, 중단 시 진행도 보존, 저장 복원.
- 압축 저장: 창고 한도 기여분 +50%, 비활성·철거 시 회수, 반복 재로드 중복 방지.
- 당시 과학 직접 효과는 8/30 연결. 이후 2단계에서 30개 전부 연결됨.
- 검증: Infirmary 35·Campaign 86·StorageResearch 33·PlayableLoop 45. 기능 커밋 `5f31825`, 개발 일지 https://app.notion.com/p/3e6c4a0ecd318120acb3f14f18678274

---

# 2026-09-25 SAVE — 이전 로그(2026-09-24 캠페인·장수 개편) 이관
- 장비 기반 역할, 9종 기술, 장수 부상·기분·관계·정신 붕괴, 원정 장비 보상, 과학 30기술·비행선 대이주 승리와 저장 v3/v2 이관을 구현했다.
- 기존 30거점 원정·편입·주둔·포로·자동 수송, 메인 메뉴·저장·장수 관리·목표 HUD가 연결되어 있었다.
- 2026-09-24 검증: Regression 45, Campaign 86, PlayableLoop 45, FullUI 50, WeaponTalent 91, SaveRoundtrip 42 통과. Regression은 검사 중 timeScale=1 적용, graphify 환경 재설치 후 갱신.
- 당시 남은 과제는 의무실 치료 연결, 과학 기술 효과 적용 범위 확인, 밸런스·Player 빌드·화면 품질 검증이었다. 의무실과 압축 저장은 이번 작업에서 연결했으며 최신 상태는 log.md 참조.

---
# 2026-09-24 SAVE — 이전 로그(창고 건설·목표 HUD, Codex 작업 중간 검증) 이관
- 창고 건설 경로 추가: `Build Storage`로 Food/Soil/Special 상한 각 +100, 반납 지점 추가, 건설 인력 복귀·저장 지원. 운반 중단 장수의 우클릭 반납 재개, 원정 화물은 귀환 후 합산.
- 현재 목표 HUD가 인구·낚시·병영 T2·과학연구소·차량·탑승·출정·약탈·귀환 단계를 안내.
- Codex가 연구소·비행선 승리 조건·장수 부상/장비·저장 연결을 미커밋 상태로 남긴 뒤, Claude Code가 이어받아 Regression 45(timeScale 수정)·Campaign 87·PlayableLoop 45·SaveRoundtrip 42·FullUI 50 통과를 확인했다.
- 같은 날 이전 SAVE 시도(README·Notion 진행 표시)가 있었으나 커밋은 되지 않았다. 이전 로그 원문: `.unity/log-before-save-2026-09-24.md`(로컬 보존).

---

# 2026-09-23 SAVE — 이전 로그(2026-09-21 UI 중단 상태) 이관
- 30거점·편입/주둔·침공/포로/구출·자동 수송·난이도 보상은 검증된 상태였다. 보상 50개, 편입 51개, 월드맵 636개 및 핵심 회귀 검사가 통과했다.
- 전체 UI 구현을 승인받아 Claude Opus 5로 시작했으나 사용 한도로 중단했다. 당시 저장/설정/복원 초안 45개는 `.unity/ui-opus-checkpoint-2026-09-21/`에 SHA256 확인 후 분리 보존하고 실행 코드는 검증된 상태로 복귀했다.
- 당시 TerrainGenerator 어셈블리 경계 컴파일 오류, ignored 코드 의존, 맵/시드/NavMesh·저장 복원 검증과 메뉴 구현이 남아 있었다. 지금은 후속 Codex 작업이 진행됐으므로 과거 초안을 현재 코드에 덮어쓰면 안 된다.
- 2026-09-21 Notion 일지에 중단 상태와 기존 캡처를 보존했고, 검증 변경은 develop `99334f7`로 커밋·push 완료했다. 카카오톡은 미연결이었다.
- 이전 로그 원문: `.unity/log-before-save-2026-09-23.md`(로컬 보존). 현재 UI 통합·검증·제한은 log.md와 README.md 참조.

---

# 2026-09-21 SAVE — 이전 로그(정착지 난이도 보상) 이관
- 정착지 초기 Food·Soil·Special 재고, Food·Soil 상한, 편입 생산량에 고정 난이도 1~3 배수 적용. 재정복 시 중복 배수 없음. UI 생산 안내·README·Notion 관련 기획 4개 페이지 반영.
- SettlementRewardChecks 50개, AnnexedSettlementChecks 51개, WorldMapChecks 636개, RegressionChecks 전체 통과. 테스트 타입 충돌·템플릿 검색 오류와 CLI 응답 제한 문제 해결.
- 앞선 SettlementDefenseChecks 76개, TransportRouteChecks 55개, WorkProficiencyLootChecks 68개도 통과한 상태. 과거 채집 간헐 실패는 원인 미확정.
- 30거점 월드맵, 동시 원정, 편입/유기, 주둔·회수, 현지 생산, 침공·상실·포로·탈출·재정복 구출, 단일 거점 자동 수송을 구현했다.
- SAVE 문서·난이도 3 편입 거점 캡처·Notion 2026-09-21 일지는 작성됐으나 develop 커밋/push가 미완료였다. 이 세션 시작 시 검증된 27개 파일이 스테이징되어 있었다.
- 이후 사용자가 「게임 전체 UI 구성」 전체 구현과 Claude Opus 5 코드 작성을 승인했다. 사용 한도로 중단됐으며 미검증 초안은 `.unity/ui-opus-checkpoint-2026-09-21/`에 분리 보존했다. 자세한 재개 절차는 log.md 참고.

---

# 2026-09-14 SAVE — 이전 로그(적 소굴 AI 경제 성장) 이관
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

---

# 2026-09-14 SAVE — 이전 로그(적 소굴 랜덤 배치) 이관

- 게임 시작 시 단일 적 소굴 전체를 플레이어 본진에서 30~60m 떨어진 임의 위치로 옮기도록 구현했다.
- 최대 24개 후보 중 NavMesh에 있고 본진까지 완전한 경로가 있는 위치만 사용하며, 후보가 없으면 기존 위치를 유지했다.
- 소굴 루트를 통째로 이동해 건물·Food/Soil/Special 전리품·침공 생성 지점의 상대 배치를 보존했고 프리팹·씬 변경은 없었다.
- `EnemyColonyPlacementChecks` 5개, 실제 씬의 소굴 이동·침공 도달 검증, 기존 `InvasionChecks`와 핵심 `RegressionChecks`가 통과했다.
- 구현 `425db1c`와 저장 결과 문서 `8ff452e`를 커밋했고, Notion 개발 일지와 캡처 첨부를 완료했다.
- 다음 작업은 적 AI 경제 성장이었고, 그 구현은 이 SAVE 직후 Codex 세션에서 이어졌다.

---

# 2026-09-14 SAVE — 이전 로그(Support 전용 버프) 이관

- 병력이 있는 Support 장수가 반경 6m 안의 다른 장수에게 중첩 없이 일반개미 1마리당 공격력 +1, 부대 방어력 +1을 제공하도록 구현했다.
- 병력 0 또는 범위 밖에서는 버프가 즉시 해제되며 SupportChecks 8개, CommanderChecks 29개, CommanderProgressionChecks 28개와 핵심 RegressionChecks 전체가 통과했다.
- 구현 `f93f615`, SAVE 문서 `7f29481`를 `origin/develop`에 push하고 Notion 개발 일지와 Unity 캡처를 완료했다.
- 다음 작업은 번식·영입·포로, 적 AI 경제 성장·랜덤 배치, Special 소비처·보스 전리품이었다.

---

# 2026-09-14 SAVE — 이전 로그(2026-09-13) 이관

- 일반개미 수량 풀과 장수 12명, 병력 배정·회수·보직·관직·병력 체력, 여왕방 생산, 병영 연구, 건설 인력 예약·복귀, 유지비와 UI 연결을 완료한 상태였다.
- 장수 처치 경험치·20레벨 상한·공격/방어 성장과 성장 유지 규칙을 구현했고 채집·농사·낚시·약탈·침공 수비를 장수 중심 구조에 연결했다.
- 사용자 점검 완료 후 2026-09-13 SAVE에서 공식 Unity CLI 캡처와 Notion 개발 일지 작성을 완료했다.
- 구현 커밋 `985300b`와 저장 결과 문서 커밋 `4a5bf0e`를 `origin/develop`에 push했다.
- 다음 작업은 번식·영입·포로, Support 전용 버프, 적 AI 경제 성장·랜덤 배치, Special 소비처·보스 전리품이었다.

---

# 2026-09-13 SAVE — 이전 로그(2026-09-11) 이관

- 이전 상태는 장수 전환 미완료 체크포인트였다. AntPool 수량 풀과 AntUnitBase/SoldierAnt 기반만 추가됐으며 씬·생산·장수 UI는 미연결이었다.
- Claude 세션 `011bfda1-3c65-48fc-a4f9-39454bf6873e`가 한도로 종료되어 승인된 전환 작업을 중단했다.
- IAirborne.IsAirborne 계약 복구로 CS0539를 해결하고 6개 보직+null 검사 및 Unity 재시작/Pipeline ready를 확인했다.
- 전환 전 채집·농사·낚시·약탈·주기적 침공과 회귀 검사 43개가 통과했으나, 당시 장수 전환 코드의 통과를 뜻하지 않았다.
- 다음 범위는 여왕방 일반개미 생산, 장수 배정·체력·보직·관직, 병영 연구 전환, 건설 인력 예약/복귀, 유지비·UI·씬 통합이었다. 번식·영입·포로는 범위 밖이었다.
- Notion 장수 기획과 하위 15개 문서를 읽었으나 관련 구기획 참조 교체는 미완료였다. 기획 부모의 하위 페이지 삭제 금지 원칙을 유지했다.
- 이전 SAVE는 Notion OAuth 복구 후 개발 일지 `3d8c4a0ecd31819aa2fcf2fc64bf0020`와 복구 PNG 첨부를 완료했다. 캡처는 게임플레이 검증 화면이 아닌 에디터 복구 화면이었다.

---

# 2026-09-11 — SAVE 연결 복구

- 직전 log.md에는 CS0539 컴파일 차단과 노션/Unity 미연결로 외부 SAVE 미완료를 기록했음.
- 사용자 노션 재인증 후 실제 개발 일지 작성·재조회와 복구 이미지 첨부 완료.
- IAirborne 계약과 현재 공중 판정을 최소 수정, Unity 재시작으로 Safe Mode 해소 및 공식 Pipeline ready 확인.
- AirborneChecks: 6개 보직과 null 판정 통과. 전체 장수 전환은 여전히 미완료.
- AntColony 씬을 다시 열고 공식 CLI eval로 에디터 복구 화면 캡처. 이전 캡처나 게임플레이 검증 결과로 오인하지 않도록 명시.

---
# 2026-09-11 — 이전 로그 요약 및 중단 지점

- 09-10 로그는 코드 전반 감사 수정, 농사 상태표시, 낚시, 실제 잔량 기반 적 소굴 약탈 프로토타입을 기록했다. 기존 검사 43개와 낚시/약탈 검사가 당시 통과했다.
- 09-10 SAVE는 사용자 요청으로 도중 중단했다. 문서/스테이징 및 `.unity` 캡처만 남았으며 Git 커밋·push와 노션 게시 완료는 확인되지 않았다.
- 이후 주기적 침공·본진 공격·수비 요격을 구현했다. 최초 90초/이후 120초, 2~6마리/동시 12마리 임시값. 침공 전용 검사와 기존 43개 검사 통과, 당시 콘솔 오류 0건.
- 09-11 새 장수 기획 본문과 하위 15개 문서를 읽었다. 사용자가 일반개미 풀·장수 조작/병력 체력·병영 연구·건설 인력 차출/반환·여왕방 낚시·UI 1차 전환을 승인했다.
- Codex 중단 뒤 이전 Claude 프로세스 부재와 장수 코드 미생성을 확인해 재시작했다. 재실행은 실제 파일 읽기·쓰기를 수행했으나 Claude 세션 한도로 중단했다.

---

# 2026-09-10 SAVE — 이전 로그 이관 (2026-09-09 상태)

- 이전 SAVE `8ad340d`: Flying 컴포넌트 연결·대공 제한·지상 범위 공격 면역과 농사 최소 프로토타입 완료.
- 밭 임시값: Soil 30, 건설 4초, 성장 20초, 회당 Food 100. 비행·농사 상태 전이 검증 통과.
- 공식 Unity CLI/Pipeline과 Codex·Claude Code용 Ponytail 설치 완료.
- 당시 다음 작업: 밭 조작 검증·상태 표시 → 낚시 → 특수 자원/약탈 → 침공/AI 성장.
- 당시 남아 있던 지형 스냅 Raycast 범위 문제는 이번 코드 점검에서 수정했다.
- 이전 개발 일지: https://app.notion.com/p/3d6c4a0ecd3181a58580d3087cd58029

---

# 2026-09-09 (세션 11 — Flying 완성 / 농사 최소 프로토타입)

## 이전 SAVE 상태 이관
- Support 역할 생산·연구 검증까지 완료됐고 Flying은 이동 기반 코드만 추가된 중간 상태였다.
- 씬 Flying 원본 연결, 대공 필터, 몬스터·보스의 공중 제외와 Play 검증이 남아 있었다.

## Flying 역할 완성
- 씬 Flying 원본을 `FlyingAnt`로 교체하고 고정 고도 직선 이동 및 지면 기준 고도 계산을 적용했다.
- Ranged/Flying만 공중 공격이 가능하도록 선택·직접 공격·어택무브를 연결했다.
- WildMonster와 보스 지상 패턴, 원·원뿔·직선 범위 공격에서 공중 유닛을 제외했다.
- Unity에서 컴포넌트 연결, Ground 마스크, 역할별 대공 판정, 고도 누적 방지를 검증했다.

## 농사 최소 프로토타입
- 기존 건설 시스템에 밭 종류와 HUD 버튼을 추가하고 `FarmData` 및 씬 내부 비활성 `FarmTemplate`을 만들었다.
- 기존 `ResourceNode`의 수동 채집·창고 반납 흐름을 재사용해 성장 후 Food 수확과 반복 재성장을 구현했다.
- 임시 수치는 Soil 30, 건설 4초, 체력 150, 성장 20초, 회당 Food 100이다.
- Unity 컴파일과 첫 성장·수확 소진·반복 재성장 상태 전이 검증을 통과했다.

## 개발 도구
- Unity 공식 CLI와 Pipeline 패키지를 설치·연결하고 Codex/Claude Code용 로컬 스킬을 추가했다.
- Codex와 Claude Code에 Ponytail 플러그인을 적용해 최소 구현 원칙을 사용하도록 설정했다.

---

# 2026-09-02 ~ 2026-09-03 (세션 1)

## 개요
- 개미 소굴 RTS(가제): 자원 채집 + 건설 + 유닛 강화 + 레이드 RTS
- 시점: 아이소메트릭(쿼터뷰), 싱글플레이 메인(멀티는 후순위)
- 엔진: Unity 6000.5.8f1, URP
- 프로젝트 루트(작업 디렉토리): `E:\Git\ant`
- 버전관리: Git/GitHub 전환 완료(`github.com/ARHIENE/ant-colony-rts`, public). 기존 Plastic SCM 워크스페이스는 제거함
- 기획 원본: 사용자가 전달한 "개미 소굴 RTS(가제) 게임 기획서" 전문(2026-09-02 세션 시작 시 전달)

## 한 줄 컨셉
플레이어는 개미 여왕(플레이어 소굴)의 지휘자가 되어 일개미를 뽑아 자원을 캐고 소굴을 확장하며, 개미 부대로 다른 개미집을 약탈하거나 상대의 공격을 막아내고 거대 보스를 물량으로 레이드하는 RTS.

## 핵심 루프 (기획서 원문)
```
자원 채집 → 소굴 확장 & 건물 건설 → 개미 유닛 생산 & 강화
  → 정찰 & 목표 선택 → 전투(약탈/레이드) → 전리품 획득 → 반복(난이도 상승)
```

## 기획서 시스템 요약
- **자원**: 식량(Food) / 흙-토양-돌(Soil) / 특수자원(적 소굴·보스 전용, 강화용) — MVP는 Food+Soil만 구현, 특수자원은 범위 밖
- **유닛**: 일개미(채집/건설), 병정개미(근접 전투, 계급 세분화 여지), 특수개미(정찰/은신, 미구현)
- **강화 방식**: 개체별 레벨업이 아닌 "종족 단위 진화"(연구소에서 연구 → 이후 생산되는 전체 유닛에 적용) — MVP 범위 밖(연구소 미구현)
- **건설/확장**: 여왕방/창고/병영/연구소/방어시설. 확장은 흙을 소모해 새 방을 뚫는 형태
- **전투/레이드**: 야생 개미집 약탈, 보스 레이드(물량 대 스케일), 부분 자동 전투 + 플레이어 타겟 지정 혼합
- **아키텍처 권장**: 프로토타입은 오브젝트 풀링 + 단순 State Machine(ECS는 이후 검토), NavMesh 기반 이동, ScriptableObject 데이터 드리븐

## MVP 수직 슬라이스 — 이번 세션 구현 완료
기획서 4.2절 범위(1~5) 그대로 구현. 대상: `Assets/Scripts/` 하위, 씬은 아직 사용자가 직접 구성해야 함(아래 체크리스트 참고).

### 폴더 구조
- `Data/` — `ResourceType`(enum), `UnitData`(SO: 비용/스탯/일개미·병정개미 전용 필드), `BuildingData`(SO: 비용/종류)
- `Core/` — `ResourceManager`(싱글턴, Food/Soil 보유량·저장한도), `ObjectPool`(제네릭 풀링), `GameManager`(싱글턴, 루프 완료 신호), `IDamageable`(전투 대상 공통 인터페이스)
- `Units/` — `AntUnitBase`(공통 체력/NavMeshAgent/풀 반환), `WorkerAnt`(Idle→MoveToNode→Gather→ReturnToStorage→Deposit FSM), `SoldierAnt`(Idle→MoveToTarget→Attack FSM, 근접 자동 교전), `UnitSelectionController`(드래그 박스 선택 + 우클릭 이동/공격 명령)
- `Buildings/` — `BuildingBase`(예치 지점 정적 레지스트리 공용), `QueenChamber`(본진, 시작 일개미 스폰 + 추가 일개미 생산), `Barracks`(병정개미 생산), `Storage`(저장한도 증가), `DigSite`(흙 소모로 확장 구역 활성화)
- `World/` — `ResourceNode`(Food/Soil 채집 노드, 유한량), `WildMonster`(전투 대상 더미, 사망 시 `GameManager.ReportWildMonsterDefeated()` 호출)
- `Camera/` — `IsometricCameraController`(고정 각도 X:30/Y:45 직교 카메라, WASD 팬, 스크롤 줌)
- `UI/` — `HUDController`(런타임 생성 Canvas: 자원 표시, 일개미/병정개미 생산 버튼, 확장 버튼, 승리 메시지 — 수작업 UI 제작 없음)
- `Map/` — `MapGenerator`(랜덤맵 생성: Perlin 노이즈 높이맵 메시 + 높이 기반 텍스처 블렌딩 + 오브젝트 랜덤 스폰). `Assets/Editor/MapGeneratorEditor.cs`(인스펙터 값 변경 시 자동 재생성 + Generate 버튼), `Assets/Shaders/TerrainBlend.shader`(높이 기반 텍스처 레이어 블렌딩 URP 셰이더)

### 랜덤맵 생성(MapGenerator) — 이전 팀 프로젝트에서 포팅
- 출처: `github.com/ARHIENE/SIMUL-TeaamProject`(private) `hyeonyeop` 브랜치의 `TerrainGenerator.cs`/`TerrainGeneratorEditor.cs`/`TerrainShader.shader`를 참고해 그대로 포팅(네임스페이스/클래스명만 프로젝트 컨벤션에 맞게 조정: `MapGenerator`)
- 동작 방식: `xSize`×`zSize` 그리드 메시 생성 → 옥타브 Perlin 노이즈로 정점 높이 결정 → `terrainLayers`(텍스처+시작 높이 목록)를 `TerrainBlend.shader`에 전달해 높이 구간별 텍스처 블렌딩 → `spawnObjects` 목록(프리팹+높이범위+스폰확률+최소간격)에 따라 좌표별 시드 고정 랜덤으로 오브젝트 배치(같은 좌표는 항상 같은 결과 = 재현 가능한 랜덤맵)
- `ResourceNode`/`WildMonster`는 이미 일반 프리팹이라 별도 연동 코드 없이 `spawnObjects`에 그대로 등록해서 쓰면 됨(자동으로 `ResourceNode.Active`/`WildMonster` 정적 레지스트리에 등록되어 기존 채집/전투 로직이 그대로 작동)
- **원본과 다르게 단순화한 부분**: 물(Water)은 원본이 쓰던 전용 노멀맵 워터 셰이더/텍스처 에셋(외부 리소스 팩) 대신, `waterMat`에 아무 반투명 Material이나 연결하는 방식으로 단순화함 — 개미 소굴 RTS 기획서에 물 요소가 없어 시각적 디테일까지는 포팅하지 않음(필요해지면 그때 추가)
- 인스펙터 값을 바꾸면 자동으로 재생성됨(Editor 스크립트가 변경 감지) — 씬에 배치 후 `xSize`/`zSize`/`noiseScale`/`heightMultiplier` 등을 만져보면서 맵 형태 튜닝

### 설계 메모
- 확장(흙 소모)은 실제 지형 파괴 대신 `DigSite`가 흙을 소모해 미리 배치된 `expansionZone` 오브젝트를 활성화하는 방식으로 단순화(지형 메시 변형은 범위 밖)
- 일개미는 완전 자동(채집 대상 자동 탐색), 병정개미만 플레이어가 드래그 선택 + 우클릭으로 이동/공격 명령
- Input System 패키지가 이미 설치돼 있어 `Mouse.current`/`Keyboard.current` 직접 폴링 방식 사용(별도 Input Actions 자산 없음), HUD 버튼 클릭 인식을 위해 `InputSystemUIInputModule` 사용
- Cinemachine 미설치 상태라 카메라는 고정 회전 + 직접 스크립트로 구현(의존성 추가 안 함)

## SIMUL-TeaamProject에서 추가 이식(보스 레이드 / 선택 시스템) — 2026-09-02
`hyeonyeop` 브랜치를 전부 훑어서 사용자가 고른 3가지만 이식. 나머지(AntStats/AntMoving/AntAttack 등 유닛 시스템 전체, GameDataManager류 자원 시스템, InteractionUIManager, 자유회전 카메라, GameSpeed)는 우리 아키텍처와 겹치거나 안 맞아서 스킵(사유는 아래 각 항목 및 이전 대화 참고).

### 보스 AoE/텔레그래프 시스템 — `Assets/Scripts/Boss/`
기획서 3.4절 "보스 레이드"(광역 공격, 페이즈 전환)를 채우는 재사용 가능한 컴포넌트 세트. 지금 씬에는 배치 안 함(보스 몬스터 자체가 아직 없음) — 나중에 보스 레이드 기능 만들 때 바로 쓸 수 있게 스크립트만 준비.
- `BossHealth.cs` — `AntColony.Core.IDamageable` 구현(원본은 자체 IDamageable을 따로 뒀지만, 우리 프로젝트 공용 인터페이스로 통합해서 `SoldierAnt.CommandAttack`이 보스도 그대로 타겟팅 가능해짐)
- `AoE/BossCircleAoE.cs`, `BossConeAoE.cs`, `BossLineAoE.cs` — 원형/부채꼴/직선 범위 공격, 시전 시간(텔레그래프) 후 판정
- `Telegraph/GroundTelegraphCircle.cs`, `GroundTelegraphLine.cs`, `GroundTelegraphSector.cs` — 바닥에 달라붙는 경고 메시 런타임 생성(레이캐스트로 지형 높낮이 따라감 — MapGenerator가 만든 언덕 지형에도 정상 작동)
- `BossBasicPatternLoop.cs`(원형 패턴 단순 반복), `BossPatternSequenceSimple.cs`(원형→부채꼴→직선 순환) — `antLayerMask`로 주변 개미(IDamageable 보유) 자동 탐색
- **아직 안 된 것**: `BossHealth.onDead`가 패턴 루프 스크립트를 자동으로 멈추지 않음(원본 그대로 포팅) — 보스 죽어도 공격 시도는 계속될 수 있음, 실제로 보스 배치할 때 `onDead` 이벤트에 패턴 루프 `enabled = false` 연결 필요
- 사용하려면 개미 유닛들이 속한 Unity Layer를 하나 만들어서(예: "Ants") `antLayerMask`/각 AoE의 `targetMask`에 지정해야 함(지금은 전부 기본값 0이라 아무것도 안 맞음)

### 유닛 선택 시스템 업그레이드 — `Assets/Scripts/Units/`
기존 `UnitSelectionController`(단순 드래그+우클릭)를 역할 분리 구조로 교체:
- `SelectableObject.cs` — 선택 시 렌더러 색상 변경으로 시각 피드백(신규)
- `SelectionManager.cs` — 드래그 박스 선택 + 클릭 단일선택 + Shift 추가선택(신규). 원본은 레거시 `Input` 클래스를 썼지만 이 프로젝트는 전부 새 Input System이라 `Mouse.current`/`Keyboard.current`로 교체함. 선택 박스 UI 이미지는 `HUDController`와 같은 방식으로 런타임 자동 생성(수작업 UI 불필요)
- `UnitSelectionController.cs` — 이제 순수 "명령 전달자" 역할만: `SelectionManager`에서 선택 목록을 받아 우클릭 시 이동/공격 명령만 내림. 클릭한 곳에 **어떤 `IDamageable`이든**(야생 몬스터, 나중의 보스 포함) 있으면 공격, 없으면 이동 — 다중 선택 시 겹쳐서 이동하지 않도록 그리드 포메이션으로 분산 배치(원본 `AntMoveController`의 아이디어 이식)
- **씬에 필요한 추가 작업**: 병정개미 프리팹에는 `SelectableObject`가 `[RequireComponent]`로 자동 부착됨(코드 반영 완료). 다만 `UnitSelectionController`의 `selectionManager` 필드에 씬의 `SelectionManager` 오브젝트를 연결해야 함(비워두면 `FindFirstObjectByType`로 자동 탐색은 되지만 명시 연결 권장)

### 어택무브(Attack-move) UX — `SoldierAnt.cs`에 통합
원본은 A키로 별도 "공격 모드"를 켜는 방식이었지만, 우리는 우클릭 하나로 "적 클릭=공격, 땅 클릭=이동"이 이미 구분되므로 별도 모드 키 없이 **이동 중에도 주기적으로 주변을 살펴 야생 몬스터를 만나면 자동 교전**하도록 `TickMovingToTarget()`에 기존 `autoEngageRadius`/`autoEngageTimer`를 재사용해 추가함(원본의 "AttackMove 중 Chasing 전환" 아이디어를 우리 NavMesh 구조에 맞게 단순화 이식)

---

# 2026-09-03~04 (세션 2)

(요약 미상세 — 세션 1과 세션 3 사이 작업. 자세한 내용은 git log 참고)

---

# 2026-09-04~05 (세션 3)

## 개요
전투/레이드 시스템 첫 구현(보스 실전 배치) + 자원 유지비/반란 + 유닛 조작 UX 보강. 동시에 unity-cli 브릿지 데드락의 실제 근본 원인 2가지(도메인 리로드 타이밍 버그, `runInBackground` 꺼짐)를 규명하고 고침 — 지금까지 "원인 불명"으로만 기록돼 있던 이슈가 대부분 해소됨.

## 보스 레이드 — MiniBirdBoss 실전 배치
- `Ants`(9)/`Ground`(8) Unity Layer 신설, WorkerAnt/SoldierAnt 프리팹과 지형을 각각 배정(지형이 원래 numeric layer 6을 쓰고 있던 걸 몰라서 처음에 이름이 꼬였다가 재수정함)
- 팀 원본 에셋의 `Sparrow` 모델로 `MiniBirdBoss` 생성(3배 스케일, `(25, 8.5, 25)` 배치) → `BossHealth`+`BossPatternSequenceSimple`(원형→부채꼴→직선 순환)+AoE 3종 부착
- `BossHealth.Die()`가 패턴 컴포넌트 비활성화 + `GameManager.ReportBossDefeated()` 자동 호출하도록 코드 연결(기존엔 원본 그대로 포팅돼서 보스 죽어도 공격 계속되는 상태였음)
- **텔레그래프(바닥 경고 장판) 연결**: 원본 팀 프로젝트 프리팹은 우리 것과 다른(네임스페이스 없는) `GroundTelegraphCircle` 등을 참조하고 있어서 `Resources.Load`로 못 찾는 문제 발견 → 우리 `AntColony.Boss.Telegraph.*` 컴포넌트로 새로 프리팹 3종(원형/부채꼴/직선) 제작해 `Assets/Resources/Telegraph/`에 배치, `WarningMat`(빨강 반투명) 적용, 각 AoE 스크립트 `Awake()`에서 `Resources.Load` 폴백으로 자동 연결
- 실전 Play 테스트로 검증: 보스가 근처 일개미를 실제로 탐지→원형 공격 캐스팅→데미지 판정까지 정상 수행해 일개미 3마리를 전멸시킴 확인(공격 데미지 25 > 일개미 기본 체력 20이라 한 방에 즉사 — 밸런스 참고)
- **사용자 피드백으로 수치 조정**: 처음엔 보스 3배 스케일에 맞춰 공격범위도 1.5배(반지름 4→6 등)로 키웠으나 "너무 크다"는 피드백 받고 원본 수치(반지름4/부채꼴사거리6/직선길이8·폭3)로 롤백. 탐지 반경(`searchRadius`)도 원본 30 → 10 → 7로 재차 축소(너무 멀리서부터 탐지·공격한다는 피드백)

## 자원 유지비 / 반란 — `UpkeepManager` 신규
- `UnitData.foodUpkeep` 필드 추가, `AntUnitBase`에 활성 유닛 정적 레지스트리(`Active`) + `Rebel()`(플레이어 통제 이탈 → 그 자리에서 `WildMonster`로 전환) 추가
- `UpkeepManager`(`GameSystems`에 부착): 30초 주기로 전체 유지비 합산 → 식량 부족 시 무작위 개체 1마리를 아사 또는 반란 처리(기획서 "반란" 요구사항 반영)

## 유닛 조작 UX — 원본 팀 프로젝트 대비 누락분 보강
- **일개미 우클릭 이동 안 되던 버그 수정**: `UnitSelectionController`가 `SoldierAnt`만 처리하던 걸 `WorkerAnt`도 포함하도록 확장(`WorkerAnt.CommandMove` 신규)
- **어택무브(A키) 신규**: 원본엔 있었지만 포팅 안 됐던 기능. `AttackMoveController` 신설 — A키로 어택 모드 진입 후 좌클릭 시 적이면 직접 공격, 빈 땅이면 어택무브(경로상 적 자동 교전, 처치 후 원래 목적지로 이동 재개). `SelectionManager`가 어택 모드 중엔 좌클릭 선택 처리를 건너뛰도록 보정(원본엔 없던 조율이지만 안 하면 어택 클릭과 동시에 선택도 바뀌는 문제가 있어서 추가)
- **이동 확인 마커 신규**: `MoveMarker.cs` — 우클릭 이동 지점에 초록색 원판이 스폰돼 0.4초간 줄어들며 사라짐(스타크래프트류), 프리팹 없이 프리미티브로 런타임 생성
- **일개미 자동 채집 완전 제거 (세션 막바지, 사용자 요청)**: `WorkerAnt.TickIdle()`을 빈 메서드로 변경 — 이제 일개미는 우클릭으로 직접 이동시키기 전까진 가만히 있음, 도착해도 자동으로 채집/반납하지 않음(순수 이동만). **주의**: 이 변경으로 자원 채집 루프 자체가 현재 막혀있는 상태 — 다음 세션에서 수동 채집 지시 방식(예: 자원노드 우클릭 시 그 자리에서 채집 시작) 등 대안 설계 필요

## unity-cli 브릿지 데드락 — 근본 원인 2가지 규명 및 수정
자세한 진단 과정/코드 위치는 메모리([[unity_cli_bridge_local_patch]], [[project_ant_teamimport_and_bridge_deadlock]]) 참고. 이 프로젝트 안에는 코드 변경 없음(전부 `E:\Git\_tools\unity-cli`와 `ProjectSettings/*.asset` 쪽 수정).
1. **`refresh_assets` 커맨드가 자기 응답을 만드는 도중 동기적으로 도메인 리로드를 유발해서 그 응답을 영영 못 보내는 버그**를 브릿지 소스(`BridgeCommandRouter.cs`)에서 발견·수정(`EditorApplication.update` 기반 1회성 지연 콜백으로 교체, `delayCall`은 창 포커스 없을 때 실행 자체가 안 되는 걸 확인해서 폐기)
2. **`runInBackground: 0`이 진짜 "개미가 안 움직인다" 문제의 원인**이었음 — 창 포커스 없으면 브릿지 응답은 멀쩡한데 실제 Play 시뮬레이션(Update 루프)이 멈춤. `ProjectSettings/ProjectSettings.asset`에서 `1`로 변경
3. Play 진입 시 강제 도메인 리로드하던 `EnterPlayModeOptions`도 꺼서(`ProjectSettings/EditorSettings.asset`) Play 진입 자체가 트리거가 되는 경우를 줄임
- **교훈**: 이 세션 중 데드락 대응으로 taskkill+재시작을 10회 이상 반복했는데, 한 번은 **사용자가 직접 Play를 누르고 테스트하던 도중에 재시작해버려서 항의받음** — 사용자가 에디터를 직접 조작 중인 신호가 있으면 그 순간부터는 데드락이어도 자동 재시작하지 말고 먼저 물어볼 것으로 규칙 수정([[feedback_unity_bridge_auto_restart]])

---

# 2026-09-05 (세션 4)

## 개요
세션 3에서 막혀있던 자원 채집 루프를 수동 지시 방식으로 복구, RTS 카메라 무한 패닝 버그 수정, 건물/자원노드/보스 클러스터를 지형 중앙으로 재배치 + NavMesh 재베이크. unity-cli는 MCP 도구가 아니라 로컬 CLI 바이너리라는 점을 재확인하고 관련 사용법을 메모리에 정리.

## 일개미 수동 채집 지시 — `WorkerAnt.CommandGather` 신규
- 세션 3 막바지에 꺼진 자동 채집(`TickIdle` 비어있음)을 대체할 조작 방식 구현. 기존 FSM(`MovingToNode→Gathering→ReturningToStorage→Depositing`)은 이미 온전했으므로 트리거만 재연결
- `WorkerAnt.CommandGather(ResourceNode node)`: 노드 위치로 이동 후 `MovingToNode` 상태 진입
- `UnitSelectionController`: 우클릭 대상에서 `ResourceNode` 컴포넌트 감지 → 선택된 일개미 전원에게 `CommandGather` 호출(다른 대상/빈 땅 클릭 시엔 기존처럼 `CommandMove`)
- 동작 확인: 자원노드 우클릭 → 이동 → 자동 채집 → 가득 차면 자동 반납까지 정상 파이프라인. Notion "자원 시스템"/"유닛(개미) 시스템" 문서의 관련 "미해결" 기술을 구현 완료 상태로 갱신함

## RTS 카메라 — 엣지스크롤 무한 패닝 버그 수정
- **증상**: 마우스를 화면 가장자리에 대고 있으면 지형(400×400) 범위를 벗어나 빈 공간까지 카메라가 끝없이 이동
- 처음엔 "커서가 화면 밖으로 나가면 좌표가 경계값에 고정돼 계속 스크롤된다"는 다른 가설로 잘못 고쳤다가(커서가 화면 밖으로 나가면 패닝을 멈추는 가드), 사용자가 "그게 문제가 아니라 지형 밖으로 카메라가 계속 나가는 게 문제"라고 정정 — 해당 가드는 롤백함
- **실제 수정**: `IsometricCameraController`에 `minX/maxX/minZ/maxZ`(기본값 0~400, `TerrainGenerator`의 `xSize`/`zSize`와 동일) 필드 추가, 패닝 후 `focusPoint.x`/`z`를 이 범위로 클램프
- unity-cli `input_mouse`의 `move` 액션으로 마우스를 화면 양쪽 끝에 20초 이상 고정해두고 `focusPoint` 값을 반복 폴링하는 방식으로 자동 검증: 오른쪽 끝 → x가 정확히 400에서 정지(z는 반대쪽 경계 0에서 클램프), 왼쪽 끝 → x=0, z=400에서 정지, 이후로도 안 넘어감 확인. (`input_mouse`의 클릭/버튼 이벤트는 실제 Input System에 반영 안 되는 걸로 보이지만, 단순 위치값 읽기는 정상 반영되는 걸 이번에 확인 — [[unity_cli_bridge_local_patch]] 참고)

## 건물/자원노드/보스 클러스터 재배치 + NavMesh 재베이크
- 원인: 전부 원점 구석(x/z 0~9 범위)에 몰려있어서 지형 대부분이 비어있는 상태였음(세션 3 다음 할 일 목록의 2번 항목)
- QueenChamber/Barracks/Storage/DigSite/FoodNode1·2/SoilNode1·2/WildMonster/MiniBirdBoss를 지형 중앙(약 200,200)으로 상대 배치 유지한 채 평행이동
- 기존에 있던 `Assets/Editor/SnapToTerrainMenu.cs`(레이캐스트로 지형 표면에 스냅하는 에디터 메뉴)의 대상 목록에 `MiniBirdBoss`를 추가하고 실행 → 새 위치의 실제 지형 높이로 자동 스냅
- `NavMeshBakeArea`(NavMeshSurface, Volume 60×60)도 같은 오프셋만큼 이동 후 `Assets/Editor/NavMeshBakeMenu.cs`("Tools/Ant Colony/Bake All NavMesh Surfaces")로 재베이크(85ms, 볼륨 크기 자체는 그대로라 빠름) — 새 위치에서 스폰된 일개미의 `NavMeshAgent.isOnNavMesh == true` 확인
- 씬 저장 완료

## unity-cli 관련 정리 (메모리로 이관)
- unity-cli는 MCP 서버가 아니라 순수 로컬 CLI 바이너리(`~/.local/bin/unity-cli`, Bash로 직접 실행)라는 걸 재확인 — Claude Code MCP 도구 목록에 뜨는 게 정상이 아님. 포트는 프로젝트마다 다름(ant=16401)
- Play 모드에서 `input_mouse`의 클릭/드래그(버튼 press/release 이벤트)는 실제 게임의 `Mouse.current`에 반영 안 되는 것으로 보임(에디터가 진짜 OS 포커스를 못 받는 게 원인으로 추정) — UX 클릭 흐름 자동검증은 컴포넌트 필드 직접 조회로 대체할 것. 단, 단순 마우스 "위치" 읽기(엣지스크롤처럼 버튼 이벤트가 필요 없는 로직)는 정상 반영됨
- `set_component_field`의 `objectReference`는 프로젝트 에셋(assetPath/guid)만 연결 가능하고 씬 안의 다른 GameObject/컴포넌트 인스턴스는 주입 불가
- 자세한 내용은 메모리 [[unity_cli_bridge_local_patch]] 참고

## unity-cli 브릿지 데드락 — 이번 세션에도 2회 재발
- `refresh_assets` 이후 재컴파일 데드락이 두 번 발생, 둘 다 사용자가 직접 조작 중이라는 신호가 없어 승인된 정책대로 물어보지 않고 taskkill 후 재시작
- 두 번째 발생 시점에 씬에 미저장 변경사항(건물 재배치)이 있었음 — 강제 종료 전에 Win32 `SetForegroundWindow`+`SendKeys`로 Unity 창에 직접 Ctrl+S를 보내 저장부터 확인(파일 mtime으로 실제 저장 확인)한 뒤 재시작. 브릿지가 죽어도 에디터 자체(OS 메시지 펌프)는 살아있는 경우 이 방법으로 데이터 손실 없이 복구 가능하다는 걸 확인 — 다음에 비슷한 상황(브릿지 데드락 + 미저장 변경)이면 taskkill 전에 먼저 시도해볼 것

---

# 2026-09-05 (Codex 인수인계 / SAVE 연결 검증)

## 이전 log.md 요약 이관
- 세션 4: 일개미 수동 채집 명령 추가, 카메라 focusPoint 0~400 제한, 건물/자원/보스를 지형 중앙으로 이동하고 NavMesh 재베이크.
- 이전 완료 기록 중 채집 전체 흐름과 내비게이션 검증은 제한적이었다. 실제 코드는 반납 후 Idle로 끝나며 isOnNavMesh만으로 목적지 도달을 보장하지 않는다.
- 다음 작업은 선택 유닛 정보 UI 및 기획서의 미구현 시스템. 기존 세션 4 상세 이력은 그대로 유지한다.

## 이번 작업
- 프로젝트 주요 시스템을 읽고 코드와 기록의 차이를 확인. 게임 코드 변경 없음.
- Notion MCP 로그인과 개발 일지 조회 확인.
- PlayMCP 자동 OAuth 등록이 IP 제한으로 거절되어 공식 일회용 토큰 교환 후 mcporter의 mcp-gateway 연결 성공. 카카오톡 나와의 채팅방 메시지 전송 성공.
- 인증은 사용자 홈에만 저장. 토큰/인증 파일은 저장소에 넣지 않는다.
- SAVE 개발 일지 이미지는 2026-09-05 12:18:35 기존 unity-cli 캡처 재사용(이번 세션 새 실행/테스트 화면 아님).

---

# 2026-09-05 (세션 5 — 유닛 UI/병영 티어/야생 몬스터 AI)

## 이전 상태 이관
- Codex 인수인계와 Notion/Kakao PlayMCP 연결을 마친 상태에서 개발을 재개했다.
- 당시 우선순위는 선택 유닛 정보 UI, 역할군별 병영, 카테고리별 강화였고 WildMonster는 피격만 가능해 반격 AI가 없었다.

## 선택 유닛 정보 UI
- `SelectedUnitPanel`을 추가하고 `HUDController`에서 런타임 생성하도록 연결했다.
- 단일 선택은 유닛 이름·현재/최대 체력·체력바, 다중 선택은 선택 수와 합산 체력을 표시한다.
- 사망·비활성화된 선택 대상을 자동으로 제거하고 풀에서 재사용된 유닛은 다시 선택해야 표시되도록 보정했다.
- Unity Play 테스트 10개 항목 통과. 실제 화면에서 Worker Ant 체력 75/100 표시를 확인했다.

## 역할군별 병영과 독립 티어
- 유닛 역할을 Worker/Melee/Ranged/Defense/Flying/Support로 확장했다.
- 병영은 자신의 역할과 일치하는 `UnitData`만 생산하며 유닛별 요구 병영 티어를 검사한다.
- 병영별 1~3티어 업그레이드, 증가 비용, 생산/업그레이드 상호 배타 처리를 구현했다.
- 현재 씬의 병영은 Melee, Soldier Ant는 요구 티어 1로 설정했다.
- 실제 UI 클릭으로 병정개미 생산(식량 20 소모)과 병영 1→2티어 업그레이드(식량/흙 각 50 소모), 다음 비용 100/100, 자원 부족 시 차단을 검증했다.

## 야생 몬스터 공격 AI
- 가장 가까운 플레이어 개미 탐지, NavMesh 추적, 사거리 내 주기 공격, 대상 사망·비활성화 후 재탐색을 구현했다.
- 기본값은 탐지 반경 8, 공격 사거리 1.5, 피해 8, 공격 간격 1.25초, 이동 속도 2.5다.
- 반란 개체가 WildMonster로 전환될 때 꺼진 `NavMeshAgent`를 다시 활성화해 같은 AI로 움직이게 했다.
- 실제 Play 테스트에서 WildMonster가 WorkerAnt를 추적·처치하는 것을 확인했고 콘솔 오류/예외는 0건이었다.

## 문서 동기화
- Notion `유닛(개미) 시스템`, `건물·소굴 확장 시스템`, `전투·레이드 시스템`의 구현 상태를 현재 코드에 맞게 갱신했다.

---

# 2026-09-06 (세션 6 — 역할 강화 연구소 / 일개미 건설 배치 기반)

## 이전 SAVE 상태 이관
- 자원 채집·유지비·반란, 기본 유닛 조작, 선택 정보 UI, Melee 병영 티어, 야생 몬스터 AI, MiniBird 보스 패턴까지 구현된 상태에서 개발을 이어갔다.
- 다음 우선순위는 역할별 공격력·방어력 연구와 일개미 건물 배치·건설 과정이었다.

## 역할 강화 연구소
- `ResearchLab`과 `ResearchLabData`를 추가하고 현재 Melee 역할의 공격력·방어력 연구를 각각 3레벨까지 구현했다.
- 레벨당 공격력 +2, 방어력 +1을 제공하며 연구 비용은 현재 레벨에 비례해 증가한다. 한 연구소에서는 한 번에 하나의 연구만 진행할 수 있다.
- 강화 수치는 연구 완료 뒤 새로 생산된 해당 역할 유닛에 적용된다. 기존 유닛의 수치는 유지한다.
- `AntUnitBase`에 런타임 공격력·방어력을 두고, 방어력만큼 받는 피해를 줄이되 최소 피해 1은 보장했다. `SoldierAnt`는 데이터 원본 대신 런타임 공격력을 사용한다.
- 선택 UI에 단일 유닛의 공격력·방어력을 표시하고 HUD에 공격력·방어력 연구 버튼을 추가했다.

## 연구소 Play 검증
- 강화 전 Soldier Ant 공격력 5 / 방어력 0 확인.
- 공격력·방어력 1레벨 연구 뒤 새 Soldier Ant 공격력 7 / 방어력 1 확인.
- 연구 전에 생산된 Soldier Ant는 5 / 0을 유지하는 것을 확인.
- 공격력 8인 WildMonster에게 두 번 피격 시 체력 40→26으로 감소해 방어력 1이 매 공격에 적용됨을 확인.
- 단계별 비용 증가와 자원 부족 차단, 최종 콘솔 오류 0건을 확인했다.

## 일개미 건물 배치·건설 기반
- `BuildingPlacementController`와 `BuildingConstructionSite`를 추가했다. 선택된 가용 일개미만 병영·연구소 배치를 시작할 수 있다.
- 지면 레이캐스트, 경사 제한, 장애물 중첩 검사, 녹색/빨간색 배치 미리보기, 우클릭·Esc 취소, 자원 선차감 흐름을 구현했다.
- 배치 확정 뒤 일개미가 건설 지점으로 이동해 건설 시간을 채우면 비활성 건물 인스턴스를 활성화한다. 건설 중 이동·채집 명령은 차단한다.
- 씬의 기존 Melee 병영과 연구소를 비활성 템플릿으로 전환하고 `BuildingPlacementSystem`을 연결했다. 시작 시 HUD에서 두 건물의 건설 버튼을 제공한다.
- 실제 마우스로 일개미 생산·선택과 건설 버튼 노출까지 확인했다. 배치 모드는 테스트 중 반복된 Esc 입력으로 취소되어 자원 차감·이동·완공 검증은 다음 세션으로 이관한다.

## 문서 동기화
- Notion `유닛(개미) 시스템`, `건물·소굴 확장 시스템`에 역할 강화 연구소 구현 내용을 반영했다.


---

# 2026-09-07 (세션 7 — 건설 흐름 완성 / Ranged 역할 프로토타입)

## 이전 SAVE 상태 이관
- Melee 연구소의 공격력·방어력 연구와 일개미 건설 배치 기반까지 구현돼 있었다.
- 건설은 일개미 생산·선택과 버튼 노출까지만 확인되어 실제 자원 차감·이동·완공 검증이 남아 있었다.
- 전투 역할은 enum과 역할 일치 검사 기반만 있었고 실제 씬 연결은 Melee만 완료된 상태였다.

## 병영·연구소 건설 흐름 완성
- Melee 병영과 연구소에 필요한 BuildingData 연결을 바로잡고 두 템플릿을 시작 씬에서 비활성 상태로 유지했다.
- Unity Play에서 배치 확정, 자원 차감, 일개미의 건설 지점 이동, 건설 시간 경과, 완공 건물 활성화까지 각각 확인했다.
- 건설 중 일개미 상태와 완공 후 HUD 연결을 확인했으며 콘솔 오류는 없었다.

## Ranged 역할 프로토타입
- `RangedAntData`를 추가했다. 임시 수치는 식량 25, 생산 8초, 체력 25, 이동속도 3.2, 공격력 3, 사거리 4, 공격 간격 1.2초, 유지비 1이다.
- 병영에 연결된 기존 유닛 원본을 복제해 씬 내부 비활성 `RangedAnt` 원본을 만들고, Ranged 병영·연구소 템플릿을 역할별 데이터와 연결했다.
- Ranged 병영 건설과 Ranged Ant 생산을 실제 Play에서 확인했다. 생산 개체의 Data·체력·공격력·방어력 수치도 확인했다.
- 별도 prefab은 만들지 않았으며 프로젝트 규칙대로 prefab 파일은 커밋 대상에 포함하지 않았다.

## 역할 선택 HUD와 배치 시스템
- HUD에 전투 역할 순환 버튼을 추가해 Melee/Ranged/Defense/Flying/Support를 선택하도록 했다.
- 생산·병영 강화·공격/방어 연구·병영/연구소 건설 버튼이 현재 선택 역할의 활성 건물과 템플릿을 찾도록 변경했다.
- `BuildingPlacementController`가 단일 Melee 원본을 캐시하지 않고 건물 종류와 역할에 맞는 비활성 템플릿을 동적으로 찾도록 변경했다.
- 아직 원본이 없는 Defense/Flying/Support는 HUD에 사용할 건물 없음 또는 건설 불가로 표시된다.

## 검증과 문서 동기화
- 최종 씬에서 WildMonster 활성, Melee/Ranged 병영·연구소 원본 비활성, Ranged Ant 원본 비활성 상태를 확인했다.
- Unity 편집 모드 저장 후 콘솔 오류 0건·예외 0건을 확인했다. unity-cli 연결 상태 경고만 남았다.
- Notion `유닛(개미) 시스템`, `건물/소굴 확장 시스템`의 구현 상태를 2026-09-07 기준으로 갱신했다.
- 개발 일지 `2026-09-07` 페이지를 만들고 Unity 캡처를 첨부했다.

---

# 2026-09-07 (세션 8 — Defense 역할 프로토타입 / 생산 활성화 수정)

## 이전 SAVE 상태 이관
- 일개미 건설 흐름과 Melee/Ranged 역할별 병영·연구소가 구현돼 있었다.
- 다음 우선순위는 Defense 역할 유닛·병영·연구소 프로토타입이었다.

## Defense 역할 프로토타입
- `DefenseAntData`를 추가하고 임시 수치를 식량 30, 생산 10초, 체력 60, 이동속도 2.3, 방어력 2, 공격력 4, 사거리 1.4, 공격 간격 1.4초, 유지비 2로 설정했다.
- 씬 내부 비활성 `DefenseAnt`, `DefenseBarracksTemplate`, `DefenseResearchLabTemplate`을 추가해 기존 역할 선택·건설·연구 흐름에 연결했다.
- Ranged 전용 에디터 설정 도구를 `CombatRolePrototypeBootstrapper`로 일반화해 Ranged와 Defense 프로토타입 구성을 함께 지원한다.

## 비활성 생산 개체 수정과 검증
- 비활성 씬 원본을 Instantiate하면 복제본도 비활성으로 남아 생산은 끝났지만 게임에 참여하지 못하는 결함을 확인했다.
- `Barracks`가 생성 인스턴스를 먼저 활성화한 뒤 `SoldierAnt.Initialize`를 호출하도록 수정했다.
- Unity Play에서 Defense Ant 생산 후 활성 상태, `DefenseAntData`, 체력 60, 공격력 4, 방어력 2를 확인했다.
- 테스트 후 병영·연구소 원본을 모두 비활성으로 복구하고 WildMonster 활성 상태를 확인했다. 콘솔 오류·예외·Assert는 0건이었다.

## 문서 동기화
- Notion `유닛(개미) 시스템`, `건물/소굴 확장 시스템`의 구현 상태를 Melee/Ranged/Defense 기준으로 갱신했다.

---

# 2026-09-08 (세션 9 — 건물 내구도·전멸 패배 / Flying 역할 프로토타입)

## 이전 SAVE 상태 이관
- 자원 채집·건설·역할별 연구와 Melee/Ranged/Defense 유닛·병영·연구소 프로토타입까지 구현돼 있었다.
- 다음 우선순위는 Flying 역할 프로토타입이었고, 건물은 피해·파괴·패배 판정이 없는 상태였다.

## 건물 내구도와 전멸 패배
- `BuildingData`에 기본 최대 체력 300을 추가하고 `BuildingBase`가 `IDamageable`을 구현하도록 확장했다.
- 활성화된 플레이어 건물을 `GameManager`에 등록하고 마지막 건물이 파괴되면 패배 이벤트와 HUD 메시지를 표시하도록 구현했다.
- 건물 데이터가 없는 기존 씬 오브젝트도 체력 300을 사용하도록 fallback을 적용했다.
- 플레이어 유닛의 직접 공격과 어택무브가 플레이어 건물을 공격 대상으로 선택하지 않도록 막았다.
- Unity Play에서 주요 건물 체력 300과 모든 건물 제거 시 패배 상태 전환을 확인했다.

## Flying 역할 프로토타입
- `FlyingAntData`를 추가했다. 임시 수치는 식량 30, 생산 9초, 체력 35, 이동속도 3.5, 방어력 1, 공격력 4, 사거리 1.6, 공격 간격 1.2초, 유지비 2다.
- 씬 내부 비활성 `FlyingAnt`, `FlyingBarracksTemplate`, `FlyingResearchLabTemplate`을 추가해 기존 역할 선택·건설·연구 흐름에 연결했다.
- 현재 Flying은 실제 공중 이동과 공중/대공 판정 없이 지상 `SoldierAnt`와 NavMesh를 재사용하는 임시 프로토타입이다.
- Unity Play에서 Flying 병영 생산, 데이터·체력·공격력·방어력 적용을 확인했다. Flying 공격 연구 1레벨 뒤 새 유닛 공격력이 4에서 6으로 적용되는 것도 확인했다.

## 검증과 문서 동기화
- Play 종료 후 Flying 유닛·병영·연구소 원본을 모두 비활성으로 복구하고 씬을 저장했다.
- 최종 컴파일 오류 0건, 콘솔 오류·예외·Assert 0건을 확인했다. 경고는 unity-cli 연결/해제 로그뿐이었다.
- Notion `유닛(개미) 시스템`, `건물/소굴 확장 시스템`, `전투/레이드 시스템`에 Flying 구현 상태와 건물 내구도·전멸 패배 규칙을 반영했다.
- SAVE 캡처: `.unity/capture/image_game_2026-09-08_01-45-00.png`.

---

# 2026-09-08 (세션 10 — Support 역할 완료 / Flying 이동·대공 기반 착수)

## 이전 SAVE 상태 이관
- Melee/Ranged/Defense/Flying 역할 유닛·병영·연구소 프로토타입과 역할별 연구가 구현돼 있었다.
- Flying은 이름과 데이터만 구분되고 실제로는 지상 `SoldierAnt`와 NavMesh를 사용하는 상태였다.

## Support 역할 프로토타입
- `SupportAntData`를 추가하고 임시 수치를 식량 25, 생산 8초, 체력 30, 이동속도 3.2, 방어력 0, 공격력 2, 사거리 2.5, 공격 간격 1.4초, 유지비 1로 설정했다.
- 씬 내부 비활성 `SupportAnt`, `SupportBarracksTemplate`, `SupportResearchLabTemplate`을 추가해 역할별 생산·건설·연구 흐름에 연결했다.
- Unity Play에서 Support Ant 생산과 체력 30·공격력 2·방어력 0 적용을 확인했다.
- Support 공격 연구 1레벨 뒤 새로 생산한 유닛의 공격력이 4로 적용되는 것을 확인했다.

## 비행·대공 규칙 확정
- Flying은 고정 고도에서 직선 이동하며 지형과 장애물을 무시한다.
- Ranged와 Flying만 공중 대상을 공격할 수 있고, Flying은 지상·공중 대상을 모두 공격한다.
- Melee/Defense/Support와 일반 지상 몬스터는 공중 대상을 공격할 수 없으며 지상 범위 공격도 공중 유닛에 적용하지 않는다.

## Flying 기반 코드 착수
- 공중 유닛 표식과 역할별 공격 가능 여부를 모은 `CombatTargeting`을 추가했다.
- `SoldierAnt`에 공격 대상 검사와 이동 확장 지점을 추가하고, 고정 고도 직선 이동을 위한 `FlyingAnt` 클래스를 추가했다.
- 현재 코드는 컴파일되지만 씬 Flying 원본 연결, 선택·어택무브 필터, 몬스터 대공 제한, 지상 범위 공격 제외와 Unity Play 검증은 남아 있다.

## 검증과 문서 동기화
- 프로젝트 스크립트 컴파일 오류 0건을 확인했다. Unity AI Toolkit 계정 API 연결 오류 1건은 프로젝트 코드와 무관하다.
- Notion `유닛(개미) 시스템`, `건물·소굴 확장 시스템`에 Support 구현 상태를 반영했다.
- Notion `유닛(개미) 시스템`, `전투·레이드 시스템`에 확정된 비행·대공 규칙과 Flying 구현 중 상태를 반영한다.
- SAVE 캡처: `.unity/capture/image_game_2026-09-08_22-39-12.png`.


## 2026-09-15 — 이전 로그 요약: 장수 획득 3종과 UI 연결 중단 상태
- 번식·스카우트 영입·적 장수 포로 획득과 성격·충성심을 구현했다. 획득 규칙 59개와 기존 회귀 검사를 통과했다.
- 양육실·파견소·수용소를 건설 가능한 건물로 연결하고 획득 HUD, 다중 건물 처리, 적 장수 템플릿 설정을 추가했다.
- 이전 중단 시 FindActive<T>의 Component 제약으로 CS1061 오류가 남았고 건설·설정 최종 검사는 미실행이었다. 이번 SAVE에서 수정·검증을 완료했다.
- 번식은 장수 쌍 사이 거리 조건이며, 플레이어 사망·포로와 다수 AI·Special 소비처 등은 미정이다.

# 2026-09-15 SAVE — 장수 획득 UI·건설 연결(2026-09-16 이관)
- 양육실·스카우트 파견소·포로 수용소의 건물 데이터·씬 템플릿·건설 버튼·비용/인력 예약을 연결했다.
- CommanderAcquisitionPanel에 번식, 파견, 다중 건물 전환, 포로 회유/처형/결과를 구현했다. 다중 수용소는 빈자리 우선, 양육실은 한 곳만 번식, 파견소 비활성/파괴 시 차출 인력을 한 번만 반환한다.
- AcquisitionSetupChecks 30개, AcquisitionBuildingChecks 42개, CommanderAcquisitionChecks 59개, CommanderChecks 29개, CommanderProgressionChecks 27개와 Regression/Invasion/EnemyColonyEconomyChecks 통과. 컴파일 오류 0건.
- CLI 30초 제한은 --timeout 90, 성장 UI 대기는 최대 5초 조건 대기로 보완했다. 기존 검색 API obsolete 경고는 남았다.
- README·Graphify 정리, Play 종료. 개발 일지 https://app.notion.com/p/3dcc4a0ecd318194b460d137e2b6b2fa 에 기록했다.
- 이전 캡처 Assets/.unity/save-2026-09-15-acquisition-hud.png는 Overlay HUD가 빠진 카메라 화면이었다. 이후 SAVE는 실제 변경 UI가 보이는 화면을 확인해 첨부한다.
- 다음 후보는 숙련도 성장·연구소 개별 강화·액티브 스킬이었다. 장수 사망/포로, 다수 AI, Special/보스 전리품, 유지비 정책 미정. 번식 거리는 장수 쌍 사이 거리.
- SAVE 시 develop 커밋·origin/develop push까지 수행하는 규칙을 명시했다. 카카오톡 도구 부재로 알림 불가.

---


# 2026-09-16 SAVE — 장수 개별 강화·액티브 스킬(2026-09-18 이관)
- 연구소의 역할 전체 보너스를 장수 개별 공격/방어 강화로 바꿨다. 각 3단계, 단계당 공격 +2/방어 +1, 연구 3초, 비용 45/30 → 90/60 → 135/90(Food/Soil). 보직 변경 후에도 강화가 유지되고 다중 연구소 중복 연구를 막는다.
- 장수 액티브 스킬을 구현했다. 레벨 2 강타(다음 타격 피해 2배, 15초 쿨다운), 레벨 3 방어 태세(5초간 방어력 +5, 20초 쿨다운). CommanderSkills 분리, SelectedUnitPanel에 잠김·활성·쿨다운 표시.
- ActiveSkillChecks 36개, LabUpgradeChecks 39개, SupportChecks 8개, CommanderProgressionChecks 27개, RegressionChecks 전체 통과. 컴파일 오류 0건.
- 다음 작업으로 본거지 + 월드맵 원정 구조 전환을 승인받은 상태로 종료했다.

---

---

# 2026-09-19 기록 요약 — 2026-09-20 SAVE에서 이관
- 30개 월드맵 거점(정착지 18, MiniBird 둥지 6, 중립 자원지 6), Amber/Azure/독립 세력과 고정 수비 난이도 구현. 중립 자원은 실제 채집·수송·귀환 반납 경로 재사용.
- 과학연구소 조건은 인구 60·낚시 해금·T2 병영. 차량→비행기 연구·건조, 첫 수송수단으로 지도 해금. 차량 40명/20초, 비행기 100명/10초는 임시값.
- 장수 탑승 상태로 성장·숙련도 보존, 동시 원정과 거점별 수송수단 한 대 점유, C/B/R 마커 30개 및 생성 순서 보완.
- 2026-09-19 컴파일 오류 0, WorldMapChecks 600개 및 RegressionChecks 전체 통과. 검사 빌더의 작업 중지·병력 보충을 보완했으며 과학 선행 조건은 유지.
- 당시 편입/유기·생산·주둔·수송 회수는 다음 구현 후보였다. 2026-09-20 현재 구현·검증 완료로 log.md와 README를 갱신했다.
- 당시 log만 저장했고 코드 커밋·push·Notion·캡처는 미실행. 당시 Opus 5 코딩 지시는 현행 프로젝트 AGENTS.md의 Codex 코딩 규칙으로 대체됨.

---

# 2026-09-20 SAVE — 이전 로그(편입 거점·자동 수송 검증) 요약
- 30거점 월드맵, 편입/유기, 현지 생산·주둔·회수, 침공·상실·포로/탈출·재정복 구출 및 Worker 자동 왕복을 구현했다.
- 수송수단 귀환 시 주둔 장수의 이동·전투 명령을 유지하고, 채집 작업만 중지하도록 수정했다.
- AnnexedSettlementChecks 51, SettlementDefenseChecks 76, TransportRouteChecks 55, WorldMapChecks 633 및 RegressionChecks가 통과했다.
- 당시 log.md에는 SAVE 진행 중으로 남아 있었다. 이후 보스·중립 자원지 난이도 차등과 채집 검사 진단 보강을 추가했으며 최신 상태는 log.md에 기록한다.

---

# 2026-09-21 SAVE — 이전 로그(2026-09-20) 이관
- 30거점 원정·편입/유기·생산/주둔/회수·침공/상실/포로/구출·Worker 단일 거점 자동 수송 구현. 귀환 시 주둔 이동·전투 명령 유지.
- 보스 템플릿 체력·Food/Special 전리품과 중립 Food/Soil 100에 고정 난이도 배수 적용. 정착지 보상 차등은 당시 후속 과제였다.
- WorldMapChecks 636, RegressionChecks 전체 통과. 편입 51, 수비 76, 수송 55, 채집 68(새 Play 2회) 통과. 과거 채집 대기 간헐 실패 원인은 미확정, 진단 출력 보강.
- README·Notion·graphify 갱신. log는 SAVE 진행 중 상태였고 develop 커밋·push가 남아 있었다. 카카오톡 도구 미연결.

---

# 2026-09-23 SAVE — 전체 UI·저장 통합(2026-09-24 이관)
- 메인 메뉴, 새 게임 옵션(맵 크기·침공 난이도·시드), 설정, 수동 3+자동 1 저장 슬롯, 도감, 관직순 장수 관리, 일시정지, 비차단 토스트, 툴팁을 통합했다.
- `GameBootstrap`/`GameSession`으로 씬 재시작과 메뉴 수명을 관리하고, 명시적 DTO·검증·원자적 파일 교체·백업·복원 API를 추가했다.
- 런타임 지형 생성과 NavMesh를 연결하고, 읽기 불가 바위 메시를 콜라이더 경계 상자의 Not Walkable 소스로 처리했다.
- 빈 소굴의 JSON 직렬화가 불러오기를 막던 문제를 수정했다. 빈 소굴은 허용하고 예기치 않은 건물 데이터는 거부한다.
- FullUIChecks 50, RegressionChecks 45, WorldMapChecks 635개를 통과했고 Unity 컴파일 오류가 없었다.
- 진행 중 이동·작업·운반·건설·전투·침공·거점 방어·자동 수송 현지 채집은 저장을 거부하고 이유를 표시하도록 했다.
- 당시 전체 저장 상황과 UI 화면 품질 검증이 남았고, SAVE 절차는 로그 기록 단계에서 중단됐다.
