# 프로젝트 로그

## 현재 상태 — 2026-10-25 SAVE(Claude+Codex) — 미실행 검사 완료, Notion 전체 대조, 미정 9항목 확정, 1차 구현
- 프로젝트 E:\Git\ant / develop 개발, master 안정. 작업 분담 `docs/IMPLEMENTATION_BOARD.md`(VERIFY-1025·IMPL-1025-CODEX·IMPL-1025-CLAUDE).
- SAVE 기록: 커밋 6af9e5a, 개발 일지 https://app.notion.com/p/404c4a0ecd3181339045f8aca1e3ae1f, Trouble Shooting https://app.notion.com/p/404c4a0ecd3181738670fc51725d045e
- **이어서 하자 하면 아래 '재개 순서'부터.**

### 오늘 완료
- 검사 전부 통과: C2 40·C3 43·C5 77·C6 58·Phase4 38·Fishing·Stage1 116·SettlementDefense 86·TransportRoute 57·G06 23·B3 107(30초 정지 재현 안 됨)·WorldMap 230·SaveRoundtrip 57·ImplA2 143/29/36·ImplB4 74·Impl1025Codex 1635.
- 월드맵: 거점 정보 패널 640→360폭·선택 없으면 숨김(지구·이름표 가림 해소), 난이도 칸(■□)→글자+색(쉬움 하늘색·보통 노랑·어려움 빨강). 캡처 확인.
- Notion 기획 전체 대조: `docs/SPEC_AUDIT_1025_codex.md`(자원·장수·건물·과학·특성 236행: 구현108·부분69·미구현54), `docs/SPEC_AUDIT_1025_claude.md`(전투·맵·월드맵·UI·메인·구현현황).
- 미정 9항목 D01~D09: Codex·Claude 각 3안 → 9개 모두 Codex안 채택 `docs/DECISIONS_1025.md`. Notion 반영: 전투·월드맵·맵·UI(Claude), 자원·장수·과학·특성 핵심 10건(Codex 쓰기 차단으로 Claude가 적용, 나머지는 `docs/NOTION_APPLY_1025_codex.md` 미적용 목록).
- 구현: D09 자동 수송 경유지 1~3곳(`World/TransportRoute.cs`, `ExpeditionTransport.TryContinueTo`, 월드맵 '경유지 추가/비우기', 저장 `RouteDto.stops/leg`). Codex: R07/R08 식사 실제 접근·신선도 순·청결 보정 제거, D01 약제대 장수약 3등급+약제 연구 2종(Claude가 CampaignResearch 등록), D05 특성 등급 생성·비전투 다이아 +30%.
- G26 성능 측정(기준 미달): 시민 3만·묶음 1만에서 fps 1.1·저장 9.7초·로드 38초(기준 30fps·5초·10초). 픽스처도 장수 0/30·적 42/200으로 덜 채워짐.

### 재개 순서
1. G26 성능: 묶음 1만 개 각각의 `OnGUI`(`World/ResourceNodeStatus.cs`)·`Update`(`ResourceNode.cs:100`)·매 프레임 `ResourceNode.Available` 전체 순회(GatherDesignation·WorkerAnt·Kitchen 등) 의심 → 프로파일러로 확인 후 수정. 로드 38초는 B3 30초 대기와 같은 원인 가능(`docs/B3-FREEZE-1025.md`). PerfG26Checks 픽스처(장수·적 수)도 보정.
2. 남은 확정 결정 구현: D04 진형(삼국지풍 7+현대식 6, 출전 편성 선택), D06/D07 철수·수송 파손·수리, D02/D03 평가·조사 파견(후긴 `TraitRules.InvestigationMultiplier` 연결), D08 지하 탐사. Codex와 분배.
3. 대조 우선 후보 나머지: 청동/철 도구 사슬, 도구 실물·마모·수리, 지역 작물·요리, 생산 목표 재고 일관화, 고정 성향 사건 변형 중단, 약초 공급 경로(현재 없음), 전투 다이아 5종.
4. D09는 중복·4곳 이상 거부만 검사됨 — 실제 다거점 순회 검사 추가 필요.

### 주의
- 검사 스크립트는 `_TeamImport` 전역 동명 타입 때문에 `AntColony.Boss.BossHealth`처럼 전체 이름 사용. internal 멤버는 리플렉션.
- Codex(rescue)는 Notion MCP 읽기는 되지만 쓰기는 승인 정책 never로 차단 → Notion 쓰기는 Claude가. Unity 실행도 Claude만. Codex 연결이 끊기면 `~/.claude/plugins/data/codex-openai-codex/state/ant-*/broker.json`을 `.stale`로.
- 커밋 제외: prefab/prefab.meta, 씬, Water.mat, TimeManager, art, graphify-out, Planet Earth Free, Assets/Resources/WorldMap.
- 카카오 MemoChat은 SAVE 완료 시만.
