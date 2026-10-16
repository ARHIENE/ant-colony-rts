# 프로젝트 로그

## 현재 상태 — 2026-10-16 SAVE(Claude) — HUD-V5-REAL 검증 완료(C·J 제외)
- 프로젝트: 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5 / Pipeline 0.7. develop 개발, master 안정.
- 목표/구조: Notion 기획 전체 구현. 소스 Assets/_Project/Scripts, 검사 AgentScripts(RunChecks.cs로 Play 중 실행, 러너 예: Temp/runone.sh), 공동 보드 docs/IMPLEMENTATION_BOARD.md.
- 이번 구현(HUD-V5-REAL, 10-15~16, SPEC-1013-14 인수분 포함): 연구 화면(GameMenuResearch, 진행 보존·자동 다음 없음)·장비(GameMenuEquipment)·농사(GameMenuFarm, 휴경·해충), 식단(공통·장수별, GameMenuDiet)·요리 재료 실제 운반(IMaterialSink/MaterialSink, Kitchen.Staged)·냉각 정도 저장, 장수 부위별 의료(CommanderHealth/HealthCare/GameMenuHealth: 상처·질병·출혈·쓰러짐·구조·급식·수술·약품 예약), 동물 배설물·우리 위생·퇴비통(CritterHygiene/CompostBin), 사망 기록·묘비·안치·추모(Memorial/Tombstone/CommanderBurial/GameMenuMemorial), 포로 건강·급식·석방(GameMenuPrisoners).
- 이번 세션 수정(실제 버그): ① 일반 처치·투약이 수술용 예약 약품까지 쓰던 문제(CommanderHealthCare.FreeMedicine) ② 장수 유해(remaining 0)가 있으면 저장 거부(SavePreflight).
- 검사 갱신: Spec1015(주석에 삼켜진 요리 활성화, 품질 포화 값, 침입 비활성, 용량 확장, 경쟁 화물 정리, 쿨다운·냉각 재설정), Campaign(새 건강 API·과학→시설 화면), ResourceRule·Spec1012(RestoreCargo lots 인자, 요리 Staged 기준).
- 검증(10-16): Spec1015 318, Campaign 86, ResourceRule 30, Spec1011Ranch 165, Spec1012 159, Spec1013AnimalHealth 11, Spec1013Clinic 20, SaveRoundtrip 57, HudV5Unity 90.
- 잔여 문제: InfirmaryChecks는 옛 시간제 치료 모델 검사라 실패(새 의료로 대체, 폐기 추천 — 파일 삭제는 권한 거부로 보류). Spec1015 Hygiene '배설물 수거' 단계 간헐 실패 1회(원인 미확정). Spec1009 외교 알림 고정 실패(기존). Spec1015를 같은 Play 세션에서 연속 실행하면 Research 버튼 검사 실패(Play 재시작 시 통과).
- 자동 선택(추천안, 사용자 미확인): InfirmaryChecks 폐기, C 배관·신호선/J 수송 확장은 대규모라 다음 세션 사전 확인 후 착수.
- Notion: 개발 일지 https://app.notion.com/p/3fbc4a0ecd31818e98aad8bf85f0805e (건강·연구 화면 캡처 2장). develop 8c03429 push.
- 다음: 1) C 배관·신호선 2) J 수송 확장(둘 다 착수 전 범위 확인) 3) InfirmaryChecks 정리 4) Hygiene 간헐 실패 원인 5) 미실행 회귀(AutonomousDuty·Meal·Stage3 등).
- 기존 미검증 변경 보존: AntColony.unity, Water.mat, TimeManager.asset, graphify 산출물, HTML 보조 파일·reports·tests. prefab/meta 커밋 금지.
