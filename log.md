# 프로젝트 로그

## 현재 상태 — 2026-10-12 SAVE(Codex) — Unity V5 UI 연결
- 프로젝트: 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5 / Pipeline 0.7. develop 개발, master 안정.
- 목표/구조: Notion 기획 전체 구현. 소스 Assets/_Project/Scripts, 검사 AgentScripts, UI 시안 design/hud/hud-v5.html, 공동 보드 docs/IMPLEMENTATION_BOARD.md.
- 이번 구현: V5 공통 관리 창, 건설 재료 목록 비교·선택, 작업표 좌/우 클릭 0~5, 목장·먹이통·도축대·생산·개보수 구역 분리, 개체 개요/의료 탭·의료 재고·약제대 연결, 동물 전용 9칸 명령, 생산 방식 3종 직접 선택·수량 입력(게임 한도 적용). 실제 게임 데이터 사용.
- 수정 파일: UI/GameMenuV5.cs·CommandCardAnimal.cs(신규), BuildScreen.cs·CommandCard.cs·GameMenuDuty.cs·GameMenuRanch.cs·GameMenuRanchBulk.cs·GameMenuProcessor.cs·GameMenuWorkshop.cs·GameMenuRenovate.cs, CommanderOverhead.cs·World/ResourceNodeStatus.cs(건설 창 위 이름 겹침 방지), AgentScripts/HudV5UnityChecks.cs, README·구현 보드.
- 검증: 컴파일 오류 0, HudV5UnityChecks 90개 통과. 임시 저장 경로에서 검사, Play 종료, 씬 저장 안 함. Graphify 갱신 완료(7157 nodes/15432 edges). 전체 회귀·Player 빌드·픽셀 일치는 미검증. InfirmaryChecks의 기존 Treat at 버튼 검사 실패는 잔여.
- 다음: 실제 플레이에서 UI 시각 검토, 전체 회귀/SaveRoundtrip, 기존 InfirmaryChecks 정리. 농사·배관·자동화 등 구현 대기 기획은 별도 작업.
- SAVE 기록: log 먼저 작성·changelog 요약 이관·README 확인 완료. 의료/생산/건설 UI 캡처 3장 직접 확인 후 Notion 개발 일지에 첨부. 검증한 변경 20개 파일을 develop 커밋 대상으로 선별했고 원격 저장 결과는 아래 Notion 기록과 Git 이력 참조.
- Notion: 개발 일지 https://app.notion.com/p/3f6c4a0ecd318100ab94efefd890ebd5 · Trouble Shooting https://app.notion.com/p/3f6c4a0ecd3181048eeac0dbf3fea12a. 카카오톡 완료 알림 도구 미연결.
- 기존 미검증 변경 보존: AntColony.unity, Water.mat, TimeManager.asset, 과거 graphify 산출물·캐시, HTML v2~v4 보조 파일·reports·tests·research_notes. prefab/meta 커밋 금지.
