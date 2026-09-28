# 프로젝트 로그

## 현재 상태 — 2026-09-29 SAVE
- 프로젝트: 개미 소굴 RTS, `E:\Git\ant`. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. 개발 `develop`, 안정 `master`.
- 주요 경로: `Assets/Scripts/{Core,Save,UI,Map,Units,Buildings,World,Boss}`, `Assets/Scenes/AntColony.unity`, `AgentScripts/`(회귀 검사), `design/`(UI 목업).
- 현재 저장 포맷 v10: 13종 기술·12종 작업, 대상별 작업 인력, 요리 재고·수리 진행도·수면 상태를 저장한다. v9 기술/작업 마이그레이션 검증 완료.
- `.prefab`/`.prefab.meta`, `Assets/_TeamImport`, `Assets/Art`, `Assets/Prefabs`, `Assets/_Recovery`, `graphify-out`, `design_skill/`, `docs/_dskills.tgz`는 커밋 대상에서 제외한다.

## 구현·검증 완료
1. 기존 HUD v2 구현 확인: 달력·낮밤 표시, 속도 1/2/3/5배, 장수 바·상세 탭·미니맵 필터·평시 명령. `HudV2Checks` 31개 통과.
2. 낮 10분·밤 5분, 야행성·숙소 4인·노숙·피로·출전 기상·야간 조명 확인. 접근 불가능한 숙소를 침상 수면으로 계산하던 버그 수정 및 재현 검사 추가. `DayNightChecks` 37개 통과.
3. 작업 인력 배정/반환, 운반·간호·수리·사냥·요리·예술, 기술/특성, 저장 왕복 구현 확인. 검사에서 TryLoad 호출 및 Herbs 치료 보너스 기대값 수정. `WorkforceChecks` 79개 통과(수면 수정 후 재검증).
- 합계 147개 검사 통과. Unity 컴파일 실패 없음. 검사 일부에 기존 obsolete API 경고가 남아 있다. AI Assistant/Pipeline 연결 오류는 별도로 콘솔에 남아 있으나 위 검사는 완료했다.
- `graphify update .` 완료(4940 nodes, 9871 edges). 검사 종료 후 Play 모드 종료.
- SAVE 진행 중: 변경사항 정리·기능 캡처·Notion 기록·develop 커밋/push 결과는 아래에 반영한다.

## 다음 작업
- 기존 실패/미실행 스위트: InvasionChecks, RaidChecks, SceneInvasionChecks, AirborneChecks/Run. 전체 회귀·Player 빌드는 이번에 실행하지 않았다.
- 기존 검토 항목: 수송 수단까지 걷는 중 출전 20초 자동 귀환, 병력 0 평시 장수의 곰팡이 감염·복수 모드 영향.
- 후속 생활 단계(요리 재고 소비·욕구 등)와 바이옴 자원/이벤트는 최신 Notion 기획을 확인한 뒤 구현한다. 임의 밸런스 확정 금지.
- 에셋: 경보음, 하는 일 아이콘 스프라이트, 배경·물가·건물 외형.

## 실행·주의
- 에디터 강제 종료 금지. 검사 하나씩 실행: `unity command run_script --file AgentScripts/XChecks.cs --entry XChecks.Main --timeout_ms 300000 --timeout 310`.
- 최신 파일이 Editor에 반영되지 않았다면 Play 종료 → AssetDatabase.Refresh → 컴파일 완료 → Play 진입 순서로 검증한다.
- SAVE 이외에는 일지·캡처·커밋/push를 하지 않는다. 캡처는 UI가 포함된 실제 기능 화면을 확인한다.
- Notion 기획: https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd / 개발 일지: https://app.notion.com/p/334c4a0ecd3181778dcaf0e6a8d57040
- 공식 CLI: `C:\Users\Shim Hyeonyeop\AppData\Local\Unity\bin\unity.exe`.
