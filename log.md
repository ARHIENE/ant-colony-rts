# 프로젝트 로그

## SAVE 마무리 — 2026-10-07 (Codex)
- 사용자 요청: Codex·Claude가 중단한 SAVE 확인 및 완료. Claude의 2026-10-07 마지막 기록은 캡처 중 회백색 지형 조사와 사용 한도 도달(03:50 초기화), Codex 직전 채팅은 systemError 상태였다.
- 기존 구현 `1e3b4d0`은 origin/develop에도 존재함을 원격 조회로 확인했다. 이번 SAVE는 공동 작업 규칙·보드와 검증/미해결 인계 문서를 저장한다.
- 기획/구현 정적 점검 뒤 공동 보드 `docs/IMPLEMENTATION_BOARD.md` 작성. AGENTS.md·CLAUDE.md에 공동 작업 문서를 연결했고 사용자 요청에 따라 Codex/Claude Code 공동 코드 작업을 허용했다.
- 진행 중 코드: `CommanderDuty.SendToRest()` 숙소 배정 변경과 `AgentScripts/DayNightChecks.cs` 추가 검사. 이번 Unity 재검증: 컴파일 up_to_date, failed=false, errors=[]; DayNightChecks는 65행 `rest destination is dormitory, not recreation room`에서 실패. 원인 미분류, 두 파일은 미커밋 보존하고 완료로 취급하지 않는다.
- 연결: 터미널 `unity pipeline list`는 서버를 발견하지 못했으나 연결된 공식 Unity MCP는 정상 응답했다. autotick → 컴파일 확인 → Play 검사/캡처 → Stop 완료. 재시작·패키지 변경 없음.
- 캡처: FurnitureShot 실행 결과 `room=연회장`. 새 게임의 초록 지형과 휴게 가구 14칸/4열 건설 UI·HUD가 보이는 `Assets/.unity/save-2026-10-07-resume/recreation-ui.png`를 직접 확인했다. 저장 경로는 `.unity/save-2026-10-07-resume/temp-saves`로 격리했다. 기존 HUD 없는 회백색 카메라 캡처는 첨부하지 않는다. 회백색 원인은 확정하지 않았다.
- Notion 개발 일지·Trouble Shooting 및 최종 원격 저장 결과는 아래에 갱신한다.
- 노화 전체 성체 적용·가시/끈끈이 분리는 수정 범위와 노화 처리 방식 사용자 결정 대기. 새 시스템(석유·원자 연구·배관 등)은 별도 후속 작업.
- Graphify AST 갱신 재실행 중. 기존 미커밋 작업·출처 불명 파일을 보존한다.

## 현재 상태 — 2026-10-07 SAVE (리뷰 마감·가구 5~6차·성능)
- 프로젝트: 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. develop 개발, master 안정. 소스 Assets/_Project/Scripts, 검사 AgentScripts. 기준 기획: Notion '메인 게임 화면 구체화'(3e7c4a0ecd3181e38211f6be9c44637d). 수치 전부 잠정.
- **리뷰 마감(docs/review-3a257fc-3b4b640.md):** Codex 수정 4건(무기고 파괴 후 초과 장비 저장, 배터리 저장값 검증, 바닥 1칸 +0.5점, 감옥 판정) 반영. 감옥 판정은 사용자 정정으로 **포로 수용소 + 창살문·잠금문일 때만 감옥**(그 방 침대는 감옥 침대로 셈, 침대만 있는 잠금문 방은 숙소·개인실). Phase5 31 재실행 통과.
- **가구 5차(15종):** 2층침대(2인, 혼자도 숙소)·해먹(기분 -1)·큰 식탁(먹기 전용: 가장 가까운 비축 식사를 원격으로 집어 식탁에서 먹음) / 휴게 놀이판(관계 +2)·장기판·바둑판(지휘 경험치)·씨름판(근력, 라이벌 관계 -3)·가시 다트(원거리)·운동기구(근력)·악기(회복 ×1.5)·거미줄 그네 / 장식 조각상 +4·깃발 +2·그림 +3·기둥(방 점수만). 휴게 8종은 Recreation 연구 해금.
- **가구 6차(11종):** 화롯불·등불·횃불(빛 + 기분 +1, 빛은 연출만) / 카펫 +1·태피스트리 +2·기념비·동상 +5·전리품 진열대 +3·사람 물건 전시대 +4 / 온천(4인, 피로 -40, 목욕탕) / 연회용 긴 식탁(먹기 전용, 장식 3+면 연회장·식사 기분 ×1.5, 모자라면 식당).
- 지을 수 있는 가구 81 / 카탈로그 239. 휴게 종류 4→13(질림 배열 자동 확장, '다양한 오락' 기분 상한 +10). 저장은 새 종류 이름으로.
- **UI:** 명령 칸 버튼 수와 무관하게 3×3 칸 크기 고정(빈칸 유지). 건설 화면 칸 12개 초과 시 4열 + 라벨 BestFit(Truncate), 단축키는 앞 10칸만(11번째부터 IndexOutOfRange 수정).
- **원정 멈춤 '버그'는 환경 문제:** Unity 포커스가 없으면 Play 프레임이 거의 안 돌아 실시간 대기 검사가 실패. editor_focus 후 ExpeditionGatherChecks 21 두 번 통과. 게임 코드 변경 없음. 60초 넘는 검사는 AgentScripts/RunChecks.cs(SessionState) + 포커스 반복.
- **성능:** Decoration.MoodAt이 기분 계산마다 FindObjectsByType → 활성 목록으로 교체. Mood 0.308→0.014ms, Medium 맵 프레임 60→24~29ms(RosterBar가 프레임당 Mood 30회 호출). WorldMapChecks 상태(~750ms/프레임) 재측정 안 함.
- 구식 검사 Invasion·SceneInvasion·Raid 삭제(없는 EnemyNestPrototype 전제, EnemyColonyEconomy·SettlementDefense·Siege·WorldMap이 대체).
- 바이옴 None(검사·옛 저장)은 바이옴 스타일 미적용 → 옛 MapGenerator 높이색(만년설 흰 바닥·초록 나무). 실제 새 게임은 항상 6종 중 하나. 옛 저장 처리는 미정.
- 검사(순차): Batch2 29·3 25·4 21·5 62·6 47, HygienePower 35, CommitRange 39, Phase5 31, Catalog 40, BuildCategory 62, HudV4 313, Joy 45, ExpeditionGather 21. 저장 왕복 검사는 RootOverride 공유라 **병렬 금지**(병렬 시 Batch5 시간 초과, 실제 슬롯은 md5로 무사 확인).
- 남은 결정: 끈끈이 함정(기존 '가시 함정' TrapPit이 실제로는 속박=끈끈이), 훈련대 과녁·작전판(기존 훈련장 역할별 구조와 연결 방식), 분수대(배관), 조명 빛의 게임 효과(밤 작업 규칙 없음), 옛 저장 바이옴 None 처리, 마을 공공시설 효과(미승인).
- 잔여: Player 빌드 미실행, 전체 회귀 미실행, 석유·원자 연구·배관·신호선·더러움·부패·재료 업그레이드·상위 방 이름 미구현.
- 보존(미커밋): Water.mat(_SrcBlend 5→1, 출처 불명), TimeManager.asset 직렬화 형식 변경, graphify-out, design/·design_skill/, package.json·playwright·tests/(출처 불명), BiomeEvent/MultiStopRoute/SaveMapCapture 초안. .prefab 커밋 금지.
- 검사 보조: AgentScripts/BuildTabShot.cs(건설 탭 열기), SeasonShot.cs(바이옴·계절 이동), PerfProbe.cs(프로파일러 상위 항목).
