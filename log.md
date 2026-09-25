# 프로젝트 로그

## 현재 상태 — 2026-09-26 (KST)
- 프로젝트: 개미 소굴 RTS, `E:\Git\ant`. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1.
- 일반 개발 `develop`, 안정 `master`. `.prefab`/`.prefab.meta` 및 `Assets/_TeamImport`는 커밋하지 않는다.
- 주요 경로: `Assets/Scripts/{Core,Save,UI,Map,Units,Buildings,World,Boss}`, `Assets/Scenes/AntColony.unity`, `AgentScripts/`.
- 작업 기준 문서: `docs/IMPLEMENTATION_PLAN_2026-09-25.md`(7단계 지시서, 수치는 잠정값). 1~5단계 완료, 6·7단계 남음.
- 저장 포맷 v6(v1~v5 자동 이관).

## 이번 작업 (2026-09-25~26, Codex + Claude Code)
- 1단계 기획 정합: 과학 연구량 300/800/1800/4000·티어별 자원, 비행선 고치 최대 20, 수송수단 장수 슬롯 분리(차량 4+40 / 비행기 8+100), 연인 쌍만 번식, 장수 본인 유지비 30초 Food 2(식성 배율), 과학 한글 표시, 단축키 개편(Z/C 회전, Q/W 스킬, E/D 병력, R 무기, G 장수, P 일시정지, K/L/M) + `KeyBindings`·설정 변경. `GameBalance`에 상수 집약.
- 2단계 과학 30기술 효과 전부 연결(`ScienceEffects`). 새 시설 7종: 흙벽·함정 구덩이·범위형 분사탑·감시탑·자폭개미 매설지·방어시설 연구소(4라인×3단계)·휴게실. 밭 작물 3종(`FarmPlot`), 가뭄·홍수 대응, 약초·부위 재생 의무실 연결.
- 3단계 공방·장비: 공방 대기열 3, 제작 장수 배정, 품질 판정(조잡/보통/정교/걸작), 장비 10종, 공용 보관함 30(초과분 전리품 보존), 장수 사망 시 장비 드롭. `Workshop`, `EquipmentRecipes`, `WorkshopMigration`.
- 4단계 랜덤 이벤트: 한파·홍수·가뭄·산불·곰팡이 감염·기생 말벌·방랑 장수·표류물·풍작·이주 개미떼(캐러밴은 6단계 전 비활성). 이벤트 로그(L) 20건, 엔딩용 누적 기록 `CampaignHistory`(자원 사유별 집계는 ResourceManager 한 곳).
- 5단계 충성심·사회: 충성심 사건(회수·포상·구출·방치·원정·굶주림 등 특성 보정), 이탈(탈주/무장 반란→60초 후 퇴각, 6단계 전엔 "이탈"), 반란군 포로·재회유(충성심 30), 관계·친구/라이벌·결투·복수 분노·파벌, 무기 스킬 산성비·집결·급강하, 사건 특성 변화. `CommanderSocial*`, `CommanderDeparture`, `CommanderAdvancedSkills`, `SkillTargeting`.
- UI 디자인 목업(별도 트랙): `design/`(HUD·메인메뉴·새 게임·장수·월드맵·외교·거래·건설·일시정지 HTML), `DESIGN.md`, `PRODUCT.md`, `.impeccable/` 리뷰 캡처. 실제 uGUI 반영은 7단계.

## 검증 (공식 Unity CLI, 각각 새 Play 세션)
- Stage1 125, Stage2 187, Stage3 107, Stage4 122, Stage5 122 통과(각 단계 완료 시점).
- 2단계 후 기존 14개 스위트 통과 확인(WorldMap 644, Transport 54, Infirmary 등). 4단계 후 Regression 46 통과(씬 초기화 대기 수정).
- 3~5단계 이후 전체 회귀 스위트 일괄 재실행은 안 함 → 다음 세션 첫 작업으로 권장.
- Stage 검사 실행: `unity command run_script --file AgentScripts/StageNChecks.cs --entry StageNChecks.Main --timeout_ms 180000 --timeout 190`.

## 제한 및 다음 작업
- 6단계 외교·교역·반란 세력(문명 4개·거점 33곳·협정·거래 화면·J 외교), 7단계 UI 마감(3D 월드맵·토스트 규칙·장수 관리·엔딩 화면·계절 반영).
- 전체 회귀 일괄 재실행, Player 빌드, 밸런스·화면 품질 검증 미완.
- 커밋 제외(로컬 보존): `Assets/Art`, `Assets/Prefabs`, `graphify-out` 변경, `design_skill/`, `docs/_dskills.tgz`, Playwright 설정(`package*.json`, `playwright.config.ts`, `tests/`, `skills-lock.json`).
- Notion 기획: https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd
- 개발 일지: https://app.notion.com/p/334c4a0ecd3181778dcaf0e6a8d57040
- 공식 CLI: `C:\Users\Shim Hyeonyeop\AppData\Local\Unity\bin\unity.exe`, 프로젝트 `E:\Git\ant`.
