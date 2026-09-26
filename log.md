# 프로젝트 로그

## 현재 상태 — 2026-09-26 (KST, 저녁 SAVE)
- 프로젝트: 개미 소굴 RTS, `E:\Git\ant`. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1.
- 일반 개발 `develop`, 안정 `master`. `.prefab`/`.prefab.meta`, `Assets/_TeamImport`, `Assets/Art`, `Assets/Prefabs`는 커밋하지 않는다.
- 주요 경로: `Assets/Scripts/{Core,Save,UI,Map,Units,Buildings,World,Boss}`, `Assets/Scenes/AntColony.unity`, `AgentScripts/`(검사), `Assets/Resources/Fonts/`(UI 폰트).
- 작업 기준 문서: `docs/IMPLEMENTATION_PLAN_2026-09-25.md`(7단계 지시서). **1~6단계 완료, 7단계(UI 마감) 진행 중.**
- 저장 포맷 v7(v1~v6 자동 이관). 새 게임은 4문명·33거점, v6 이하 저장은 기존 30거점 배치 유지.

## 6단계 외교·교역·반란 (완료)
- 파일: `World/Diplomacy{Manager,Rules,Trade,Rebels}.cs`, `World/WorldGeneration.cs`, `UI/GameMenuDiplomacy.cs`. 문명 4개(공개·숨김 어젠다), 평화/전쟁·기습 선전포고 −30, 협정 3종(1년), AI 제안·월별 선전포고·3개월 침공, 평화 협상 배상, 거래 화면(자원·장비·거점·포로·협정, 수락식 `1.2-호감도/250`), 교역소 3곳·설계도, 캐러밴 90초, 반란 세력(초기 전쟁 −60, 소멸).
- 이번 세션 보완: 적 세력이 우리 장수를 격파하면 전쟁 점수 +10(`DiplomacyManager.CommanderDowned`, 15m 내 교전 세력 추정).
- 편입 거점 침공은 교전 중 문명 일정으로만 시작(거점 타이머 = 외교 시계 기준).

## 7단계 UI 마감 (진행 중)
- 기준 디자인: Claude Design 캔버스 「개미 RTS UI」 https://claude.ai/artifact/NLgjVc64vKNdfV49nvTw6j (9화면: HUD·건설·월드맵·장수·외교·거래·메인메뉴·새 게임·일시정지). 로컬 `design/`은 이전 버전.
- UI 방식: 기존 uGUI 코드 생성 유지. 공통 토큰·폰트는 `MenuTheme`(Plate #1b1712, Accent #f2a93b, Text #efe7da, Noto Sans KR + Barlow).
- 완료: 1) 테마 색·폰트 전체 적용 2) HUD/메뉴 캔버스 기준 1440×900, 상단 40px 바(메뉴 Esc·장수 G·과학 K·외교 J·로그 L·월드맵 M / 날짜·속도 / 자원·개미), 목표 상자 좌상단, 알림 우상단 판넬. 검사가 찾는 오브젝트 이름은 유지하고 표시 문구만 한글.
- 다음: 3) 하단 콘솔(미니맵·선택 장수·커맨드 카드) + 건설 12버튼을 건설(B) 화면으로 이동 — **조작 흐름 변경이라 사용자 확인 후 진행** 4) 장수 관리·일시정지/저장·메인메뉴/새 게임·외교/거래·월드맵 화면 교체.

## 검증 (공식 Unity CLI, 새 Play 세션)
- 통과: Stage1 126·2 178·3 107·4 122·5 122·6 59, WorldMap 743, Tooltip 362, FullUI 50, Regression, SaveRoundtrip 42, TransportRoute 56, Campaign 86, Fishing, EnemyColonyEconomy, Foundation 16, SettlementDefense 71, AnnexedSettlement 48, LabUpgrade 39, Support 8, Beta 39, CommanderAcquisition 59, Infirmary 35, PlayableLoop 45, StorageResearch 33, WeaponTalent 91, SettlementReward 50, AcidTower 26, AcquisitionSetup 66, AntWorkVisual, EnemyColonyPlacement.
- 실패/미실행: ActiveSkill·CommanderEdge(선택 패널 UI 의존, 3단계에서 재작성), WorkProficiencyLoot(특수 전리품 반납 대기 실패, 원인 미상), CommanderChecks(실행마다 다른 줄 실패, 불안정), AcquisitionBuilding(양육실 문구), Invasion·Raid·SceneInvasion(씬 `EnemyNestPrototype` 의존), Airborne·Run(top-level 형식, 러너 비호환). CommanderProgressionChecks는 삭제된 경험치·계급 시스템 전용이라 제거.
- 새 Play 세션은 메인 메뉴(일시정지)로 시작 → 검사는 `GameSession.GameStarted` 아니면 새 게임 시작 후 진행해야 함.
- 실행: `unity command run_script --file AgentScripts/XChecks.cs --entry XChecks.Main --timeout_ms 300000 --timeout 310`. Play 재진입은 `editor_stop` 후 `editor_status`로 stopped 확인 뒤 `editor_play`.

## 주의
- 에디터 강제 종료(taskkill) 시 씬 백업 복구 대화상자가 뜸 → 에디터는 끄지 말고 한 인스턴스로 검사. 반복 Play로 메모리가 15GB+까지 증가해 OOM 크래시 1회 발생.
- `capture_game_view --save_path`는 `Assets/` 하위에 저장됨 → 캡처 후 즉시 밖으로 옮기고 `Assets/Temp` 삭제.
- 커밋 제외(로컬 보존): `Assets/Art`, `Assets/Prefabs`, `graphify-out` 변경, `design_skill/`, `docs/_dskills.tgz`, Playwright 설정.
- Notion 기획: https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd / 개발 일지: https://app.notion.com/p/334c4a0ecd3181778dcaf0e6a8d57040
- 공식 CLI: `C:\Users\Shim Hyeonyeop\AppData\Local\Unity\bin\unity.exe`, 에디터 `E:/unity/6000.5.8f1/Editor/Unity.exe`.
