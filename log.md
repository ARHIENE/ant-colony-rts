# 프로젝트 로그

## 현재 상태 — 2026-09-27 SAVE (다음 할 일 1~4 구현·회귀 정리)
- 프로젝트: 개미 소굴 RTS, `E:\Git\ant`. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. 일반 개발 `develop`, 안정 `master`.
- `.prefab`/`.prefab.meta`, `Assets/_TeamImport`, `Assets/Art`, `Assets/Prefabs`, `Assets/_Recovery`, `graphify-out`, `design_skill/`, `docs/_dskills.tgz`는 커밋하지 않는다.
- 주요 경로: `Assets/Scripts/{Core,Save,UI,Map,Units,Buildings,World,Boss}`, `Assets/Scenes/AntColony.unity`, `AgentScripts/`(검사), `Assets/Resources/Fonts/`.
- 최신 기획: Notion 「메인 게임 화면 구체화 (2026-09-26)」 https://app.notion.com/p/3e7c4a0ecd3181e38211f6be9c44637d (기존 `docs/IMPLEMENTATION_PLAN_2026-09-25.md`와 충돌 시 최신 기획 우선).
- 저장 포맷 v9(v1~v8 자동 이관). v9 추가: 낚시터 `fishMonth`, 장수 `fishingProgress`.

## 이번 세션 완료
1. 채집 지정 UX — `UI/GatherDesignation.cs`: 기본 커맨드 카드 `채집 금지`/`지정 취소` 모드(클릭·드래그), 금지 노드 월드 표시. 우클릭·Esc·건설·메뉴·어택무브 시 자동 종료.
2. 장수 상태·전투 피드백 — `UI/CommanderOverhead.cs`(이름·체력/병력 바·하는 일 픽셀 아이콘 `UI/ActivityIcons.cs`·기분 경고), `UI/EnemyAlert.cs`(새 적/교전 시작 → 위기 토스트, 경보음은 `AlarmClip` 훅만·무음). 커맨드 카드 평시(작업표·무기·연구·포상·휴식·치료·건설)/출전(Q/W·어택무브·정지·귀환) 분리. `CommanderDuty`: `SendToRest`(피로 풀릴 때까지 자율 작업 휴식), `SendToTreatment`(빈 침상 의무실로 이동·입원).
3. 자원 규칙 — 기본 균류 180초/Food40, 가을 수확 ×1.25, 겨울 밭 성장 정지. 낚시 20초/Food6×낚시 배율(한파 −20%), 낚시터(노드) 1곳당 월 Food 100·다음 달 회복, 진행도 저장. 상수는 `Core/GameBalance.cs`.
4. 7단계 UI — 토스트 재작성(동시 5·×N 합치기·+N건·위기 고정/경고 10초/일반 설정값·호버 정지·클릭 닫기), 첫 등장 힌트 8종(`UI/FirstHints.cs`, 민트·1회·설정 끄기/초기화, F2 설명서 반영), 붕괴 경고 토스트·오라(`UI/MoodWatch.cs`)·장수 관리 "기분 경고" 필터, 엔딩 기록 화면(`UI/GameMenuEnding.cs`, 점수·등급 없음), 월드맵 3D 행성(`UI/WorldPlanet.cs`, 레이어 31·RenderTexture·드래그 회전·뒷면 마커 숨김)·원정 박스 펼침(장수별 상세·귀환·전장 보기).
5. 월드맵이 열리면 OnGUI 표시(머리 위·채집 금지·노드 상태) 숨김.
6. 원정 병력 결정(사용자 확인): 징집소 출전 편성으로 병력을 받은 장수만 탑승, 원정지 채집·운반 가능(기존 흐름 그대로, 게임 코드 변경 없음).

## 검증 (스위트별 새 Play 세션)
- 신규: GatherDesignation 13, CommanderStatus 38, ResourceRule 27, Stage7UI 46.
- 통과: DutyUI 46, GatheringUI 14, FullUI 48, Tooltip 202, Fishing, SaveRoundtrip 42, AutonomousDuty 52, Campaign 87, WorldMap 226, PlayableLoop 46, Regression, WeaponTalent 93, TransportRoute 56, AnnexedSettlement 48, SettlementDefense 75, Stage1 126·2 178·3 107·4 122·5 122·6 59, AcidTower 26, LabUpgrade 39.
- 검사 수정은 새 규칙에 맞춘 전제 변경: 평시 병력 → 출전 편성(`WorkState.duty = Deployed` 후 `TryAssign`), 피해 병력→개인 체력 순, 겨울 성장 정지·가을 ×1.25, 홈 자율 작업 격리, Play 직후 로딩 대기. WorldMap 마커 겹침 검사는 3D 행성 초점 검사로 교체(746→226).
- 미실행/기존 실패: ActiveSkill·CommanderEdge·WorkProficiencyLoot·CommanderChecks·AcquisitionBuilding·Invasion/Raid/SceneInvasion·Airborne/Run.

## 다음 작업
1. 결정 필요: 출전 후 적 없음 20초 자동 귀환이 수송 수단까지 걷는 중에도 발동할 수 있음, 곰팡이 감염·복수 모드가 병력 0 평시 장수에게 효과 없음.
2. 기존 실패 스위트 정리(위 목록).
3. 기획 확정 후: 작업 대상별 인력 속도 공식, 낮밤 비율·수면/전투 깨우기, 바이옴 자원·이벤트 비율. 임의 수치 확정 금지.
4. 에셋: 경보음 클립, 하는 일 아이콘 스프라이트(현재 코드 생성 픽셀 아이콘), 배경·물가·건물 외형.

## 주의
- 에디터 강제 종료 금지(씬 백업 복구 대화상자). 반복 Play로 에디터 메모리 증가(이번 세션 12.7GB) → 검사는 포그라운드로 하나씩.
- 실행: `unity command run_script --file AgentScripts/XChecks.cs --entry XChecks.Main --timeout_ms 300000 --timeout 310`(namespace 있는 스위트는 `AntColony.Regression.X.Main`). Play 재진입은 `editor_stop` → stopped 확인 → `editor_play`.
- 검사 스크립트에서 `SelectionManager`/`SelectableObject`/`ResourceType`은 `_TeamImport` 타입과 이름이 겹침 → `AntColony.Units.`/`AntColony.Data.` 완전 이름 사용.
- `capture_game_view --source screen`이 오버레이 UI 포함 캡처. `--save_path`는 `Assets/` 하위 저장 → 즉시 밖으로 옮기고 `Assets/Temp` 삭제.
- Notion 기획: https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd / 개발 일지: https://app.notion.com/p/334c4a0ecd3181778dcaf0e6a8d57040
- 공식 CLI: `C:\Users\Shim Hyeonyeop\AppData\Local\Unity\bin\unity.exe`, 에디터 `E:/unity/6000.5.8f1/Editor/Unity.exe`.
