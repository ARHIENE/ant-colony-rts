# 프로젝트 로그

## 현재 상태 — 2026-10-04 SAVE 진행
- 프로젝트: 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. 일반 개발 develop, 안정 master. 소스 Assets/_Project/Scripts, 검사 AgentScripts, 그래프 graphify-out.
- 사용자 요청으로 추가 구현을 중단하고 현재 상태 SAVE 후 컴퓨터 종료. 종료 요청 2026-10-04 12:25 KST부터 1시간 유효. 새 작업 요청 시 취소.
- 이전 세션 변경: HUD v4.1~4.4(불투명 콘솔·청동 테두리·리벳·들어간 패널·장수 얼굴 초상), 바닥 반복/대비/높이 경계/계절 덮임 개선, 농장 회귀 검사 인력 배정, MapGenerator 관리용 자식 보존. 상세 HANDOFF.md.
- 전체 검사: .unity/checks-2026-10-04/runall.sh 경로 수정과 69개 실행은 이전 프로세스가 완료(DONE). 최초 42 통과·27 실패. LabUpgradeChecks는 results.tsv에 PASS이나 본문에 FAIL 2개라 실패로 취급. 전체 통과 아님.
- 이번 세션: 기존 프로세스 종료까지 Assets 수정 없이 대기. RegressionTriageChecks의 외부 ResourceType 충돌을 namespace로 해결, 격리 검사 23개 통과. 새 바이옴/경유 검사도 같은 namespace 적용했으나 기능 미구현이라 실행하지 않음.
- 원인 확인: 자동 작업/초기화/이전 UI 이름에 의존한 검사, 작업 종류·간호·농사 인력·성격 유전·바이옴 재고 등 변경된 규칙 기대값이 혼재. 실제 버그 분류 전체 완료 아님. 수송 반복 재검사 56개 통과(transport-recheck.json). 최초 실패 원인은 확정하지 않음.
- 미완료: Player 빌드 미실행, 바이옴 이벤트(거대한 발자국·물뿌리개·도시 쓰레기/홍수 이름·숲 그늘) 미구현, 다거점 수송 경유 미구현. AgentScripts/BiomeEventChecks.cs·MultiStopRouteChecks.cs는 초안으로 로컬 보존. 사막·동굴 전용 적은 미정, 만들지 않음.
- 다음 세션: HANDOFF.md와 최신 검사 JSON 확인 → 미분류 실패 재현/실제 버그만 수정 → Player 빌드 → 승인된 이벤트/경유 기능 및 저장 호환 구현·검사. 검사 중 Assets 코드 수정·에디터 재시작 금지.
- SAVE: README/Notion/검증 변경 커밋·origin/develop push 진행 중. .prefab/.prefab.meta 제외, master 유지, 사용자 저장 슬롯 보존. 카카오톡 완료 도구 현재 없음.
- Graphify 갱신 완료(5710 노드). SAVE 공식 CLI screen 캡처 .unity/save-2026-10-04/hud-floor.png 직접 확인·Notion 첨부. Play 정지 완료.
- Trouble Shooting: https://app.notion.com/p/3efc4a0ecd31812c884fe8cd1df45dbb

- 개발 일지: https://app.notion.com/p/3efc4a0ecd3181faa123e97894c67f33
