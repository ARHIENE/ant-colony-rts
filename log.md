# 프로젝트 로그

## 현재 상태 — 2026-09-26 (KST, 밤 SAVE)
- 프로젝트: 개미 소굴 RTS, `E:\Git\ant`. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1.
- 일반 개발 `develop`, 안정 `master`. `.prefab`/`.prefab.meta`, `Assets/_TeamImport`, `Assets/Art`, `Assets/Prefabs`는 커밋하지 않는다.
- 주요 경로: `Assets/Scripts/{Core,Save,UI,Map,Units,Buildings,World,Boss}`, `Assets/Scenes/AntColony.unity`, `AgentScripts/`(검사), `Assets/Resources/Fonts/`(UI 폰트).
- 작업 기준 문서: `docs/IMPLEMENTATION_PLAN_2026-09-25.md`(7단계 지시서). **1~6단계 완료, 7단계(UI 마감) 화면 교체 완료·세부 기능 남음.**
- 저장 포맷 v7(v1~v6 자동 이관). 새 게임은 4문명·33거점.

## 7단계 UI 마감 (진행 중)
- 기준 디자인: Claude Design 캔버스 「개미 RTS UI」 https://claude.ai/artifact/NLgjVc64vKNdfV49nvTw6j. 공통 토큰·폰트 `MenuTheme`, 절대 배치 헬퍼 `MenuLayout`(Plate·Well·Label·Button·Meter·List).
- 완료(이번 세션):
  - 하단 콘솔 `HudConsole`(좌 미니맵 236px·중앙 선택 장수 카드·우 커맨드 카드 372px). 미니맵 클릭 = 카메라 이동.
  - `CommandCard` 5×3 버튼(장수: Q 스킬·W 급강하·E/D 병력·R 무기·연구·상세·B 건설 / 장수 미선택: 개미 생산·병영·낚시·확장).
  - `BuildScreen`(B): 5탭(생산·자원·연구·방어·특수)·단축키 1~5/QWERT·ASDFG → 건설 장수 선택(건설 기술순 4명) → 배치. 건설 12버튼 HUD에서 제거, `BuildingPlacementController.BeginPlacement`가 장수를 명시로 받음.
  - `SelectedUnitPanel` 중앙 콘솔용 재작성, `GameMenuFront`(메인 메뉴·새 게임·일시정지·저장 슬롯), `GameMenuRoster`(장수 관리 목록·상세), `GameMenuDiplomacy`(외교·거래 화면), `WorldMapPanel`(3열: 원정/지도·거점 정보/과학·범례) 교체.
  - 월드맵 열리면 목표 상자 숨김(`BetaProgress`), CommandCard 연구 진행 표시·툴팁 보완, `MenuLayout.Item()` 무한 재귀 수정.
- 남은 7단계 항목(지시서 237~246행): 토스트 동시 5개·×N 합치기·+N건(현재 `ToastManager` 3개), 첫 등장 힌트 토스트, 붕괴 경고 필터·오라, 월드맵 3D 행성·원정 박스 펼침, 엔딩 화면, 계절 작물 배율(가을 ×1.25·겨울 정지)·낚시 월 한도.

## 검증 (공식 Unity CLI, 새 Play 세션)
- 이번 세션 통과: Stage1 126·2 178·3 107·4 122·5 122·6 59, Campaign 86, SaveRoundtrip 42, Tooltip 110, FullUI 48, PlayableLoop 45, TransportRoute 56, Regression, AcidTower 26, WeaponTalent(스킬 표시 로케이터 수정), WorldMap.
- 미해결: LabUpgrade 38/39(HUD 진행 중 표시 1건). 이전부터 실패/미실행: ActiveSkill·CommanderEdge·WorkProficiencyLoot·CommanderChecks·AcquisitionBuilding·Invasion/Raid/SceneInvasion·Airborne/Run.
- 검사는 스위트마다 새 Play 세션 권장(공유 세션에서 상태 오염으로 거짓 실패 발생).
- 실행: `unity command run_script --file AgentScripts/XChecks.cs --entry XChecks.Main --timeout_ms 300000 --timeout 310`. Play 재진입은 `editor_stop` → `editor_status` stopped 확인 → `editor_play`.

## 주의
- 에디터 강제 종료 금지(씬 백업 복구 대화상자). 반복 Play로 메모리 증가 주의.
- `capture_game_view --save_path`는 `Assets/` 하위 저장 → 캡처 후 즉시 밖으로 옮기고 `Assets/Temp` 삭제.
- 커밋 제외(로컬 보존): `Assets/Art`, `Assets/Prefabs`, `Assets/_Recovery`, `graphify-out`, `design_skill/`, `docs/_dskills.tgz`, Playwright 설정.
- Notion 기획: https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd / 개발 일지: https://app.notion.com/p/334c4a0ecd3181778dcaf0e6a8d57040
- 공식 CLI: `C:\Users\Shim Hyeonyeop\AppData\Local\Unity\bin\unity.exe`, 에디터 `E:/unity/6000.5.8f1/Editor/Unity.exe`.
