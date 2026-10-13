# 프로젝트 로그

## 현재 상태 — 2026-10-14 SAVE(Claude) — SPEC-1012 완료
- 프로젝트: 개미 소굴 RTS, E:\Gitnt. Unity 6000.5.8f1 / URP 17.5 / Pipeline 0.7. develop 개발, master 안정.
- 목표/구조: Notion 기획 전체 구현. 소스 Assets/_Project/Scripts, 검사 AgentScripts(RunChecks.cs로 Play 중 실행), 공동 보드 docs/IMPLEMENTATION_BOARD.md.
- 이번 구현(SPEC-1012 ①~⑦, 2026-10-12 기획): 작업표 0~9·실제 이동 거리·오래 기다린 요청, 창고 실물 재고(StoreBin·허용·우선순위·꺼내기·운반 예약), 건설 예약·배송·시공 시작 소비·취소 100% 바닥, 철거·이동(포장)·개보수, 식품 신선도(FoodLots·FloorFood·냉장고/냉동고 ColdStorage·썩은 식품·식사), 요리 생산 목록(Kitchen 재작성·GameMenuKitchen), 작업 관리 창(GameMenuTasks·작업 선택 드래그).
- 수정 주요 파일: Core/StoreLedger·FoodLots·ResourceManager, World/FloorFood·ResourceNode·DiplomacyPrisoners, Units/WorkerAnt·CommanderMeal·CommanderDuty·WorkPriorities, Buildings/Kitchen·ColdStorage·BuildingConstructionSite·Demolition·BuildingPlacementController, Save/*, UI/GameMenuKitchen·GameMenuTasks·GameMenuStorage·GatherDesignation·WorkTargetPanel·GameMenuDuty, AgentScripts/Spec1012Checks.
- 검증: Spec1012Checks 153, Spec1011Ranch 165, SaveRoundtrip 57, Stage3 107, AutonomousDuty 56, Phase5 31, FurnitureBatch5 62·6 47, HudV5Unity 90, Meal 347, Spec1010 53. 옛 기대값 갱신: Spec1010(포로 수용소 이동 금지·철거 70% 바닥), MealChecks(요리·썩은 식품도 먹음), Spec1011Ranch(V5 선택 칸), Spec1009(지워진 변수 컴파일 오류).
- 수정한 실제 버그(검사 중 발견): 냉장고·냉동고 건설 템플릿 누락, 냉장고 있으면 저장 실패, 먹을 것이 없어도 식탁으로 걸어가던 문제.
- 잔여 문제: Spec1009 '보류 협상 알림 고정' 실패(외교 알림, 이번 변경과 무관·원인 미확인), InfirmaryChecks 기존 Treat at 실패. 미구현: 식단(공통·장수별)·레시피 재료 허용, 요리 재료 실제 운반(현재 원격 차감), 냉각 정도 저장, Chef 특성 효과 재정의.
- 기획: 2026-10-13 Codex 대화 기획(장수 부상·질병 림월드식, 동물 의료 완화·질병·배설물, 사망·묘비, 원정 장비 압수·수송·철수·출정 준비)을 Notion에 반영(SPEC-1013, 구현 대기). 기존 코드의 동물 수술 후유증 누적은 새 기획과 불일치.
- 다음: SPEC-1013 구현 순서 결정(의료 체계 개편이 큼 — 착수 전 사용자 확인), 위 미구현 항목, Spec1009 외교 알림 원인 확인.
- 기존 미검증 변경 보존: AntColony.unity, Water.mat, TimeManager.asset, graphify 산출물, HTML v2~v4 보조 파일·reports·tests. prefab/meta 커밋 금지.
