# 프로젝트 로그

## 현재 상태 — 2026-10-24 SAVE 2(Claude+Codex) — 플레이 점검 결정 G01~G69 코드 구현 완료, 검증 진행 중
- 프로젝트 E:\Git\ant / develop 개발, master 안정. 결정 원본 docs/PLAY_CHECK_DECISIONS_1023.md(추천안 자동 선택). 통합 인계 docs/IMPL-I1-HANDOFF.md, 묶음별 인계 docs/IMPL-B3·B5~B10-HANDOFF.md, IMPL-C2~C5-HANDOFF.md.
- **이어서 하자 하면 아래 '재개 순서'부터.**

### 완료(코드, Unity 컴파일 오류 0)
- G01~G69 중 G26(측정)·G27(판정표) 외 전부 코드 연결. Codex 2차 B5~B10·3차 C1~C6(사용량 한도로 5개 중단 → Claude가 I1 worktree에서 통합·마무리).
- Claude 직접 구현: G33 이주 수용 중단 UI, G06 표본 연구(CampaignResearch.Resources.cs), G31 재산 등급 자동 판정, G39 외교 습격 위기 중단, G11 패배 사유 표시·저장, G42 직접 지시 저장·취소·자율 작업보다 우선, G28 종자 출처 고정 키·저장, G30 원산지별 상온 보존, G45 예상 공세 표시, G16 대피 버튼, G43 이탈자 기록, G69 하역 보류 범위, G04 노년 상한, G26 측정 검사(PerfG26Checks).
- 실제 버그 수정: 비축더미 없는 시작에서 가장자리 이벤트 미출현, 수송 경로 커서 저장 거부, 로켓 snapshot 빈 객체로 모든 로드 실패, 최종 공세 적 장수 생성 null, 새 건물 저장 키 순서(objectName 저장), PowerGrid 씬 종료 재생성, 구저장 노년 이관, 모닥불 정원.
- 월드맵 행성 → 'Planet Earth Free' EarthMedium(Resources/WorldMap 사본, 머티리얼 URP 변환, glow 끔, 없으면 절차 구체). 플레이 캡처 확인.
- Notion 장수 페이지 '개월' 8곳 1개월=2일 환산(사용자 승인). 잠정값 전부 사용자 승인으로 유지.

### 검사(2026-10-24, RunChecks)
- 통과: A2 208, B1 72, B2 136, B4 74, B5 82, B6 128, B7 92, B8 80, B9 62, B10 332, C1 97, C4 34, G01 7, Foundation 17, Biome 22, SeasonWeather 323, NightThreat 17, SaveRoundtrip 57, DayNight 42, ResourceRule 30.
- **미실행/재실행 필요(메모리 부족으로 중단):** C2·C3·C5·C6·Phase4·Fishing·Stage1·SettlementDefense·TransportRoute(검사 수정 후 미확인), B3(로드 30초 정지 원인 미확인), G06(신규), G26(CHECK_POLLS=180 필요). 완료로 보지 말 것.

### 재개 순서
1. 메모리 확인(Unity가 반복 검사로 12GB까지 늘어남) → 위 미실행 검사 3~4개씩: `CHECK_POLLS=90 bash AgentScripts/tools/run_checks.sh <이름...>`.
2. 실패 수정 후 docs/G27_PLAY_PATH_1024.md(C01~C14 × 1명·3명) 칸 채우기 = G27.
3. G26 PerfG26Checks 실행(CHECK_POLLS=180) → 기준 미달 항목 정리.
4. G01~G69 밖 Notion 기획 전체 대조는 사용자 확인 후.

### 주의
- 검사 스크립트는 `_TeamImport`의 전역 동명 타입(BossHealth·Boss 패턴·IDamageable 등)에 가려지므로 `AntColony.Boss.BossHealth`처럼 전체 이름 사용. internal 멤버는 리플렉션.
- Unity 없이 컴파일 확인: 내장 Roslyn + Bee rsp(메모리 ant_offline_roslyn_compile). 통합 worktree E:/Git/ant-wt/I1(Temp/off/a.rsp·c.rsp).
- Codex 백그라운드는 Unity·Notion 불가, 사용량 한도로 끊기면 Claude가 이어서(broker.json 낡으면 .stale로). 끊긴 worktree C1~C6·I1은 SAVE 때 정리.
- 커밋 제외: prefab/prefab.meta, 씬, Water.mat, TimeManager, art, graphify-out, Planet Earth Free(서드파티 원본·4k 텍스처), Assets/Resources/WorldMap.
- 카카오 MemoChat은 SAVE 완료 시만.
