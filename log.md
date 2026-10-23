# 프로젝트 로그

## 현재 상태 — 2026-10-24 SAVE(Claude+Codex) — 플레이 점검 결정 G01~G69 구현 3단계 진행 중
- 프로젝트 E:\Git\ant / develop 개발, master 안정. 공동 보드 docs/IMPLEMENTATION_BOARD.md(맨 위 IMPL-B1~B4·IMPL-G08·G01 항목). 결정 원본 docs/PLAY_CHECK_DECISIONS_1023.md(추천안 자동 선택·잠정 수치).
- 진행 순서(사용자 승인): 1) A2 시작 생존 마무리 → 2) Notion 결정 반영 → 3) 나머지 결정 묶음별 구현(Codex 비중 크게, 사용자 요청). **이어서 하자 하면 아래 '재개 순서'부터.**

### 완료
- **1단계 A2(G02·G03·G12·G24·G29·G34·G54·G56·G57):** NewGame→CrashLandingStart.Prepare, 화재·서비스 지시 저장/복원, v15 작업표 이관 14→15칸, 단궁 원거리 12m·2초(Codex), CrashWreck 템플릿이 연구소를 잡던 버그 수정(FindTemplate<CrashWreck>). ImplA2 208·SaveRoundtrip 57 통과(G08 이전 기준).
- **2단계 Notion:** 기획 하위 14페이지 결정 반영 완료(Claude 7페이지 잔여 '월/주간/매주' 정리 + Codex 교체안 docs/NOTION_APPLY_1023_codex.md 77건 적용, 중복 상단 삽입 4건 제외). 점검 페이지에 '구현 현황' 섹션 추가.
- **3단계 코드(미검증 포함):**
  - Claude G08 달력: GameCalendar 1일 150초(낮100/밤50)·계절 6일·1년 24일, 표시 '일', 하루 2끼(SatietyPerSecond 70/75), 매일 납세(TaxSeconds/DailyFood/DailySoil), 낚시 계절 한도 300(FishSeason), 밤 포식자 계절 첫날 밤만, SeasonVisuals 계절 900초.
  - Claude G01: 병력 0 장수 1인분 교전(AttackDamage×max(1,병력)), 민간 무병력 장수는 대기 중 자동 교전 안 함(SoldierAnt.AutoEngages).
  - Codex B1(G32·G04·G36): 징집소/정찰 초소 목재20·무연구, 동원 상한 ColonyPopulation.Military.cs, 정찰 실패 +20%p·4회차 확정. Claude가 MaxSoldiers/TryDraft 위임·GameMenuConscription 연결.
  - Codex B2(G33·G38·G30): 3일 주기 이주·연령 매년 전환·식품별 상온 보존. 원산지별 생산 경로 연결은 미완.
  - Codex B3(G41·G09): 모든 상태 저장(저장 v19, Save/SaveOrders·SaveOrderValidation·SaveRuntime 신규). Codex 쓰기 거부로 docs/patches/IMPL-B3.patch를 Claude가 git apply. 인계 docs/IMPL-B3-HANDOFF.md.
  - Codex B4(G60·G31·G10): 위생·기분 보정/경고·마지막 장수 배회 붕괴·생활 욕구 우선. 남음: 재산 등급 자동 판정, 물 공급, 독립 의자.
- Unity 전체 컴파일 오류 0(B3 패치 적용 후 확인).

### 검사 결과(2026-10-24, Codex 작업 종료 후 일괄 실행, AgentScripts/RunChecks.cs 경유)
- 통과: FoundationChecks 17, ImplG01Checks 7, ImplB1Checks 72.
- **실패: ImplB2Checks 36행 NullReference** — 검사가 FindAnyObjectByType<Stockpile>()을 가정하나 불시착 시작에는 비축더미가 없음(검사 전제 문제로 추정, 코드 버그 여부 미확인).
- **미실행/결과 미확인:** ImplB3·ImplB4·DayNight·Phase4·ResourceRule·Fishing·Biome·NightThreat·Stage1·SeasonWeather·ImplA2·SaveRoundtrip·TransportRoute·SettlementDefense. G08·B1~B4 변경 뒤 회귀 검증 안 됨 — 완료로 보지 말 것.
- 기존 과거 검사 다수가 12장수/40시민·비축더미 시작을 가정함. 새 시작 사양 기준으로 전제 이관 필요.

### 진행 중 Codex(중단 가능) — worktree 분리
- B5(G05 도보 원정·G48·G15·G21·G44) `E:\Git\ant-wt\B5`, B6(보스 사슬 G07·G19·G22·G23·G47·G51·G06·G40) `E:\Git\ant-wt\B6`, B7(G13·G18·G25·G28·G37·G42·G55·G61·G63·G64) `E:\Git\ant-wt\B7`. 본 프로젝트 재컴파일 방지용 git worktree(detached, 2026-10-24 작업 트리 상태 복제 후 index에 올림). 각 폴더의 docs/IMPL-B5~B7-HANDOFF.md 확인.
- 합치는 법: `cd E:/Git/ant-wt/B5 && git add -A -N && git diff > ../B5.patch` → 본 프로젝트에서 `git apply --check` 후 적용(줄바꿈 차이 주의: worktree는 CRLF 체크아웃). 합친 뒤 `git worktree remove --force`.
- Codex가 중간에 끊겼으면 핸드오프 문서가 없을 수 있음 — git diff로 진행분 확인 후 이어서 맡기거나 폐기.

### 재개 순서
1. Codex B5~B7 상태 확인(`node <codex-companion> status`는 각 worktree 폴더에서) → 결과/진행분 patch로 본 프로젝트에 합치기 → 컴파일.
2. ImplB2Checks Stockpile 전제 수정 후, 미실행 검사 일괄 실행: `bash AgentScripts/tools/run_checks.sh <이름...>`(Play 재시작·SessionState·RunChecks·Temp/check-result.txt 폴링). worktree 생성은 AgentScripts/tools/make_wt.sh NAME. **Codex가 본 프로젝트를 고치는 동안에는 검사 금지(재컴파일로 중단됨).**
3. 실패 수정 → 남은 결정(G11·G14·G16·G17·G20·G26·G27·G35·G39·G43·G45·G46·G49·G50·G52·G53·G58·G59·G62·G65~G69 등) 묶음으로 Codex에 worktree 분리로 배정.
4. 사용자 확인 대기: 장수 Notion 페이지 옛 '개월' 표기(특성·이벤트 지속시간) 일괄 1개월=2일 환산 여부. G08로 밤 50초·식사 75초마다 → 체감 플레이 확인 필요.

### 주의
- Codex는 백그라운드에서 approval policy=never라 Unity MCP·Notion 쓰기 불가, 일부 Save/* 쓰기도 거부됨 → Unity 실행·Notion 쓰기·패치 적용은 Claude 담당(사용자: 현 구조 유지).
- 검사 시 ResourceType은 AntColony.Data.ResourceType 전체 이름(전역 ResourceType 충돌). run_script 60초 제한 → RunChecks.cs 비동기 경유.
- prefab/prefab.meta, 씬, Water.mat, TimeManager, art 등은 커밋 제외. graphify-out 대량 변경도 커밋 제외.
- 카카오 MemoChat은 SAVE 완료 시만.
