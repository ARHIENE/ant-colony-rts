# 프로젝트 로그

## 현재 상태 — 2026-10-03 SAVE
- 프로젝트: 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. 일반 개발 develop, 안정 master.
- 주요 경로: Assets/Scripts/{Core,Save,UI,Map,Units,Buildings,World,Boss}, AgentScripts(회귀 검사), Assets/Scenes/AntColony.unity, design/hud(HUD 시안 v2~v4).
- 구현 누적(10-01~10-03, 이번 SAVE에서 일괄 커밋): Phase 2(재료 단일 자원·낚시 과학 1티어·장수 0명 패배), HUD v3, Phase 3(충성심 삭제→기분), Phase 4(인구·세금·민심·병역·주거·비축더미·이주 제안, 저장 v12), Phase 5(방·지붕·벽/문/성문), Phase 6(사막·동굴 바이옴), HUD v4. 수치는 전부 잠정.
- HUD v4(design/hud/hud-v4.html 기준, 실제 게임 데이터 연결):
  - 선택 상태 5종별 하단 정보·2×2 명령: 선택 없음(소굴 요약 + 작업표·장수·징집소·건설) / 장수 1명 평시 / 다중 선택(요약·공통 명령·선택 해제) / 건물(숙소 침대 4개·배정·건설) / 출전(어택무브·귀환 등).
  - 상단: 대기·동원 가능 인력 표시(병역 제도 상한 반영), 숙소 부족 계산(HudOverview). 알림에 '위치' 버튼(알림 유지한 채 대상 이동).
  - 채집 금지·지정 취소 → 작업표, 병영 강화·굴착 → 해당 건물 선택 화면으로 이동.
  - 반응형(HudResponsiveLayout): 폭 ≤1280 상단 2줄(장수 바 아래 줄), ≤1000 미니맵 숨김, ≤720 명령 칸을 정보 아래로 쌓음. 자원 버튼은 상단 첫 줄 고정, 쌓인 명령 칸은 바닥 기준.
- 검증(10-03): HudV4 276(5개 해상도)·HudV2 40·Tooltip 62·CommanderStatus 37·PlayableLoop 46·Commander 33·CommanderEdge 24·SaveRoundtrip 55 통과. HudV2/Tooltip 검사는 v4 배치 기준으로 갱신.
- 10-02 Phase 3~6 검증 목록과 기존 실패(Stage2·Infirmary·CommanderAcquisition·Campaign·WorldMap, 변경 무관 판단)는 changelog 참고.

## 다음 단계·주의
- HUD v4 캡처(1920×1080)에서 발견, 미수정: 우상단 '대기·동원 가능' 글자가 인구 수요 막대와 겹침(HudOverview WorkforceSummary y=-31), 다중 선택 요약에 대기 장수 활동이 빈 괄호 '()'로 표시. 캡처 원본 .unity/save-2026-10-03/hud-v4-*.png.
- 남은 미반영: 정찰 파견 장수 동행, 늙으면 멍청해지는 특성, 벽 줄 드래그 배치, 벽 부수기·방화 전용 적 AI, 난방 가구, 사막·동굴 전용 적 개체, 가구 목록 확정 후 이름·크기 교체.
- 병역 모병제 5%는 초반 병력이 매우 적음(성체 40 → 2마리). 플레이테스트로 조정 필요.
- 기획: https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd (상위 문서 replace_content+allow_deleting_content 금지).
- 개발 일지 상위: https://app.notion.com/p/334c4a0ecd3181778dcaf0e6a8d57040 . 같은 한국 날짜 ant 일지 중 최신 페이지에 이어 쓰기.
- 공식 CLI: C:\Users\Shim Hyeonyeop\AppData\Local\Unity\bin\unity.exe. run_script 검사는 하나씩, 긴 검사 timeout_ms/timeout 지정.
- 전체 회귀·Player 빌드 미실행. 기존 InvasionChecks/RaidChecks/SceneInvasionChecks/AirborneChecks 별도.
- .prefab/.prefab.meta 커밋 금지. master 반영 없음. 사용자 저장 슬롯 보존.
