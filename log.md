# 프로젝트 로그

## 현재 상태 — 2026-10-11 SAVE(Claude, 저녁) — 목장 기획 불일치 수정 + 일괄 요청·이름 + HUD V5
- 프로젝트: 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. develop 개발, master 안정. 소스 Assets/_Project/Scripts, 검사 AgentScripts. 기준 기획: Notion '기획(스펙 문서)' 하위(자원 시스템 '목장 운영·포획·탈출 확정안'이 최신, 10-11 10:37 UTC 갱신분 반영). 수치 전부 잠정. 공동 작업 보드 `docs/IMPLEMENTATION_BOARD.md`(SPEC-K·HUD-V5 행).
- **목장 기획 불일치 수정(SPEC-K):** 자동 도축·도축 금지 제거 → 수동 도축 요청(두 번 눌러 이름·마릿수 확인, 시작 후 취소 불가, 강제 중단 시 최대 체력 10% 피해·생존), 치료 시설 안 도축·방생 금지. 도축대 = 사체별 대기열(등록 순서·사체별 산출·새끼 50%·허용 종·일시정지, 사체별 남은 부패 시간 보존·진행). 약제대 = 간호(의료) 작업·의료 능력만(보조 기술 없음), 치료가 생산보다 먼저, 의약품·키트 예약(`Units/AnimalMedicalStock.cs`). 수술 = 기여도 가중 성공률·실패(사망/악화/유지)·후유증·회복 기간·수동 재수술·고위험 집도의 지정·재입원(`World/CritterSurgery.cs`). 투약은 투약 당시 경상만, 회복 중 시설 안 새 경상은 자연 회복·투약.
- **최신 기획 추가 구현:** 포획·운반·도축·방생 일괄 요청/취소(제외 사유·마릿수), 여러 종 운반 공통 목적지, 개체 이름 변경('이름(종)'), 개체 목록 필터·정렬(`UI/GameMenuRanchBulk.cs`), 운반은 결박·쓰러짐만(길들인 개체도 저항 없이 포획). 작업표 표기 '간호(의료)'.
- **사용자 확정 잠정 수치(Notion 반영):** 수술 성공률 = 50% + 의료×2.5%p − 부상 보정(체력 40%→0, 0%→20%p) − 후유증×10%p(5~95%), 실패 시 사망 10%+후유증×5%p·악화 30%(최대 체력 10%), 고위험 = 최고 예상 50% 미만, 회복 = 1개월×(1−수술 전 체력), 최소 0.25개월, 새끼 산출 50%, 중단 피해 10%, 허용 해제 사체 바닥, 간호 = 의료 작업, 길들인 개체도 포획 후 운반.
- **HUD V5:** `design/hud/hud-v5.html/.css/.js`(V4 보존) 인터랙티브 시안(예시 데이터, Unity 미연동). 검사 `node design/hud/hud-v5.check.cjs`(Playwright 72), 스크린샷 `reports/hud-v5/`(미커밋). Unity 이식은 아직 안 함.
- **검사(Play 순차):** Spec1011Ranch 165(최종), Spec1010Material 44, Spec1010 53, AutonomousDuty 56, Spec1008FI 29, SaveRoundtrip 57(일괄 요청·이름 추가 전 실행). InfirmaryChecks는 기존 문제로 실패(`Treat at` 버튼이 f0f0fa0에서 제거됨).
- **Unity 연결:** 공식 CLI가 안 붙으면 `Library/Pipeline/.unity-pipeline-port` 누락 확인 → Window → Pipeline → Stop Server → Start Server.
- **다음 할 일:** 1) Codex 2차 리뷰(SPEC-K·HUD-V5). 2) 미실행 회귀 묶음(Workforce·DutyUI·WorkAlert·Spec1009·Spec1008FI 외 Stage3·5·6·AcidTower·NightThreat·CommanderStatus·Regression·SettlementDefense)과 일괄 요청 이후 SaveRoundtrip 재실행. 3) InfirmaryChecks 버튼 이름 갱신. 4) 목장 화면이 버튼 세로 나열이라 개체가 많으면 길어짐 — V5 디자인 Unity 이식 때 정리. 5) 이전 할 일: 추천안 자동 선택 항목 사용자 확인, Notion '구현 대기' 문구·화면 피드백 4건.
- **기획 미정이라 보류:** 사체·생산물 부패와 질병, 종 최종 목록·생산물·수치, 특수종 전용 시설, 전동 도축대, 배관·자동화, 상위 방, 특성 등급, 석유·원자 시대, 200개 건물 확장, 바이옴 전용 적, 다거점 수송, 몸값·부상 확률·귀환 계수, 협상 대사, 단축키.
- 보존(미커밋): AntColony.unity, Water.mat, TimeManager.asset, graphify-out, design/hud v2~v4 보조 파일·design_skill/, package.json·playwright·tests/·reports/·research_notes/, debug.log, BiomeEvent/MultiStopRoute/SaveMapCapture/FurnitureShot 초안, Assets/Screenshots. .prefab 커밋 금지.
- 도구: Unity 공식 MCP(`mcp__unity-editor-mcp__*`). 순차 검사: Play → editor_focus → SessionState `AntColony.CheckFile` 설정 → `eval_file AgentScripts/RunChecks.cs` → `AntColony.CheckResult` 폴링. Python은 `py -3`.
