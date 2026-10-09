# 프로젝트 로그

## 현재 상태 — 2026-10-10 SAVE(Claude) — 회귀 마무리 + 기획 확정분 4건 구현
- 프로젝트: 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. develop 개발, master 안정. 소스 Assets/_Project/Scripts, 검사 AgentScripts. 기준 기획: Notion '기획(스펙 문서)' 하위(메인 구체화 3e7c4a0ecd3181e38211f6be9c44637d 등). 수치 전부 잠정.
- **커밋:** `14efc3a` 창고 가득 시 건설 취소 반환 초과분 → 창고 주변 바닥 더미(`DiplomacyManager.StoreResource` 재사용), 바닥 장비(`EquipmentLoot`)를 운반 작업 장수가 보관함으로 자동 운반. `56525d5` 아래 4건.
- **철거·가구 이동(`Buildings/Demolition.cs`):** 건물 선택 → 명령 카드 '철거'(5칸)·'이동'(6칸). 둘 다 건설 예정지처럼 작업표 '건설' 장수가 현장 작업(작업량 = 건설 시간 × 0.5, 잠정). 철거 완료 시 건설비 70% 반환(넘치면 바닥 더미), 이동은 무료·청사진으로 새 위치 지정(`BuildingPlacementController.BeginMove`). 벽·문·성벽·바닥·기둥·전선은 이동 불가, 비축더미는 철거 불가. 예정지 취소는 반환 없음.
- **제작 생산 목록(`Workshop.Orders`):** 지정 수량 / 재고 유지(보관함 재고 기준) / 계속 생산. 대기열이 비면 목록 위에서부터 1개씩 대기열에 넣고 그때 비용 차감(기존 대기열·환급 규칙 유지). 공방 화면에서 방식·수량 고른 뒤 품목 추가, 기존 '직접 대기열 추가' 버튼은 목록으로 대체. 저장(`Workshop.State.orders`, 최대 10개·수량 1~30 검증).
- **재개발 Shift 배치:** Shift 줄 배치 칸마다 겹친 작은 집을 찾아 재개발로 처리(줄 배치에서도 주거 실내 금지 검사).
- **연구·제작 보조 능력:** `CampaignResearch.Topic`(농업→농사, 생물·의료→의료, 무기→근접/원거리, 기계·수송·로켓→제작, 군사 전술·병역→지휘, 그 외 연구만), `EquipmentRecipes.Topic`(큰턱·방패·코팅→근력, 분사기·신호기·날개→연구, 장신구→예술). 분류는 Claude 초안(사용자 확인 필요). 제작 경험치가 주 80%로 바뀌어 Stage3 기대값 30→24 수정.
- **검사(Play 순차, 통과):** Meal 347, AutonomousDuty 56(수정 후 2회), Spec1009 68(바닥 더미·장비 운반 4개 추가), Spec1010 44(신규), Stage3 107, SaveRoundtrip 57. Spec1009 'held talk pinned' 1회 실패는 에디터 포커스 상실로 프레임이 느려진 탓(포커스 유지 후 통과).
- **다음 할 일:** 1) Notion 기획 문서의 '구현 대기' 문구를 구현 내용으로 교체(외교·포로·경보·작업표·행정·포텐·철거·생산 목록 등, 하위 페이지 안에서만). 2) 연구·제작 보조 분류 초안 사용자 확인. 3) 사용자 화면 피드백 4건(대상이 작음·물체 역할 안 보임·공간 구분 없음·UI 과다) 범위 확인 후 착수.
- **기획 미정이라 보류:** 특성 등급 배정·획득 체계, 가구 재료 업그레이드·상위 방, 석유·원자 시대 항목, 물·배관, 자동화(센서·논리), 200개 건물 확장·배·잠수함·터널 기차, 마을 공공시설 수치, 바이옴 전용 적(개미귀신·박쥐) 수치·에셋, 외교 정치 경험치, 다거점 수송(검사 초안 `MultiStopRouteChecks.cs`만 있고 코드 없음), 몸값·부상 확률·귀환 거리 계수·협상 대사.
- 보존(미커밋): Water.mat, TimeManager.asset, graphify-out, design/·design_skill/, package.json·playwright·tests/·reports/·research_notes/, BiomeEvent/MultiStopRoute/SaveMapCapture/FurnitureShot 초안, Assets/Screenshots. .prefab 커밋 금지.
- 도구: Unity 공식 MCP(`mcp__unity-editor-mcp__*`, ToolSearch로 로드). 검사: eval로 SessionState `AntColony.CheckFile` 설정(타임아웃 나면 값 재확인) → Play → editor_focus → eval_file `AgentScripts/RunChecks.cs` → `AntColony.CheckResult` 폴링. 검사마다 Play 재시작. 컴파일 오류는 RunChecks가 숨기므로 eval로 RoslynCompilationService 진단 출력. Python은 `py -3`(python은 스토어 별칭).
