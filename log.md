# 프로젝트 로그

## 현재 상태 — 2026-10-23 SAVE 2(Codex) — 지도·카메라·지형·날씨·HUD
- 프로젝트 E:\Git\ant / develop 개발, master 안정. 공동 보드 docs/IMPLEMENTATION_BOARD.md. 이전 V6 상태는 changelog.md로 이관.
- 사용자 요청: 미니맵 확대/식별/클릭/누른 채 드래그, 수평 테두리 시야 표시, 가장자리 이동 속도, 언덕 높이·부자연스러운 흰 벽, UI 이름표 겹침, 비의 화면 가장자리 누락, 눈의 회색 지면·주황색 입자.
- 미니맵: 212~260px 정사각형 높이 지도(물·고도·절벽), 자원/야생 기본 숨김+필터. 클릭 좌표 pivot 보정, 왼쪽 누르기·드래그, 영역 밖 드래그는 가장자리로 제한. 채움 없는 수평 사각형은 카메라 지면 투영 범위를 감쌈(회전하지 않음).
- 카메라: 가장자리 속도 기존 2배, 줌 비례·대각선 정규화, 일시정지/배속 독립. 가장자리 두께 0이면 비활성.
- HUD: 월드 이름표(자원·장수·채집 표식)가 활성 HUD 영역과 겹치면 숨김. 같은 프레임의 이름표끼리도 영역을 예약하여 겹친 후속 이름표 숨김. 지도 확대에 맞춰 선택 카드·상세 창 위치 조정, 재료 목록/달력 분리.
- 지형: 언덕 층별 밝기·절벽 전용 세로 암반 텍스처, 바닥의 높이 무관 얼룩 축소, 눈/낙엽 적용 후 절벽 음영 유지. 셰이더 재임포트 시 numTextures/terrainHeights가 유실되는 회색 지면 원인 재현, 기존 Texture2DArray를 재사용해 SeasonVisuals→SetTint에서 복구. 주의: HomeMapBuilder가 MapGenerator.enabled=false로 두므로 OnWillRenderObject 복구는 실행되지 않아 폐기했다.
- 날씨: 카메라 로컬 입자층이 뷰포트를 덮도록 크기·위치 갱신. 계절/날씨 전환 시 이전 색 입자 제거, 실제 눈 방출색 흰색 검사. 사용자가 본 주황 현상과 동일 원인인지 육안 검수 필요.
- 검증: 최종 HudV6Checks 17·MapHudChecks 128·SeasonWeatherChecks 323 통과(저장/불러오기 포함), V6LayoutChecks 2070 뷰포트/글씨 검사 통과. 컴파일·지형 셰이더 오류 없음. SAVE 실제 캡처·육안 확인과 문서/Notion/커밋/push 진행 중. Player 빌드 미실행.
- 기존 미완료 지형/길찾기 변경과 출처 불명 씬/Water.mat/TimeManager 등 보존. MapGenerator.cs와 TerrainBlend.shader는 기존 변경 위에 후속 수정했으므로 기존 검증 범위를 확인하고 SAVE 대상을 정할 것. prefab/prefab.meta 제외.
- 길찾기 잔여: 큰 물가 seed261019 296/300의 서쪽 4곳, 1칸 경유지 탐색은 성공했지만 2→1칸 재시도 미구현. 작은 맵 전체·완전 회귀는 별도.
- 오늘 Notion 개발 일지: https://app.notion.com/p/401c4a0ecd31816fb1f2ea2360701ccb / Trouble Shooting: https://app.notion.com/p/401c4a0ecd31811b99e4c6aebb4fe995 (동일 날짜 페이지에 추가 예정).
- **컴퓨터 종료 요청 취소:** 사용자가 Claude 한도 소진 후 로그 기록이라는 새 작업을 요청했으므로 전역 규칙에 따라 기존 종료 승인 자동 취소. shutdown 예약/실행 안 함.
- Git: 번들 Git 사용(시스템 Git HTTPS helper 누락), 명령 단위 maintenance/gc 비활성. 현재 원격 마지막 SAVE 98bd1c6. 카카오 도구 미제공.
## 중단·인계 — Claude 한도 소진, 사용자 요청으로 현재 상태 기록
- **최신 실제 확인:** Assets/reports/save/2026-10-23/map-hud-cliffs.png를 캡처·직접 확인했다. 1920×1080: 큰 높이 지도, 채움 없는 수평 테두리, 절벽의 암반 질감·높이 경계, HUD 위 이름표 억제 확인. 이전 캡처 map-hud-terrain.png는 이름표 겹침 수정 전이므로 최종 증빙으로 사용하지 말 것.
- **회색 지면 재현→2차 수정:** SAVE 첫 절벽 캡처가 회색으로 나와 실제 실패 확인. numTextures=0, terrainHeights=null, terraceBase=0이었고 MapGenerator.enabled=false여서 렌더 콜백이 실행되지 않았음. SetTint에서 복구하도록 연결, 자동 프레임 대기 검사 MapHudChecks 128 통과. 최신 map-hud-cliffs.png에서는 회색 아님.
- **아직 할 일 1 — 날씨 화면 검수:** 봄/맑음 절벽 캡처만 최종 확인했다. 비와 겨울/눈 실제 캡처를 준비해 양옆·하단까지 비가 보이는지, 눈이 흰색인지, 겨울에도 절벽·층 구별이 되는지 직접 확인해야 한다. 주황색 원인은 이전 낙엽/모래 입자 잔류 가능성을 코드로 확인했지만 사용자 화면의 정확한 원인 확정은 아님. 눈 색·전환 시 입자 정리 자동 검사 통과와 실제 화면 검수는 구분할 것.
- **아직 할 일 2 — SAVE 마무리:** README/log/changelog 정리는 로컬 반영, 이번 후속 Notion 기록·캡처 첨부·커밋·push는 아직 안 됐다. 오늘 기존 일지/트러블 페이지에 추가하고 기존 기록은 보존할 것. Unity Play 종료 뒤 편집 상태 저장도 남음. 현 테스트 게임은 임시 저장 경로로 생성했으며 실제 사용자 저장 슬롯을 덮어쓰지 않음.
- **아직 할 일 3 — Git 범위 주의:** 마지막 status에서 작업 시작 때 없던 다수 파일 변경이 추가로 나타났다(GameBalance/GameOptions/ResourceType/BuildScreen/GameMenu* 등). 다른 작업자의 변경일 수 있으므로 일괄 add/restore 금지. 본인 파일도 다른 담당 변경과 겹칠 수 있어 diff 확인 후 부분 stage. 원격 HEAD는 마지막 확인 98bd1c6이며 이번 작업은 미커밋.
- **커밋 의존성:** 이번 MinimapTerrain은 기존 미커밋 MapGenerator.Active/BaseXSize/BaseZSize와 terrace 셰이더 값을 사용한다. MapGenerator 전체를 커밋하면 미커밋 TerraceHeights/NavRoute 의존성이 포함된다. UI 파일만 커밋해도 깨질 수 있으므로 의존성을 포함해 검증하거나 안전하게 부분 분리한 뒤 커밋해야 한다. 기존 큰 물가 4곳 미해결을 완료로 표기하지 말 것.
- **주요 수정 파일:** UI/HudConsole.cs(미니맵), 신규 UI/MinimapTerrain.cs(+Unity 생성 meta), UI/HudResponsiveLayout.cs(배치/이름표 영역), UI/CommanderOverhead.cs·GatherDesignation.cs, World/ResourceNodeStatus.cs, Camera/IsometricCameraController.cs, Map/Precipitation.cs, Map/MapGenerator.cs(층 정보 복구; 기존 변경 포함), Shaders/TerrainBlend.shader(절벽/겨울; 기존 변경 포함), 신규 AgentScripts/MapHudChecks.cs.
- **검사 실행 방법:** 공식 unity CLI, Play 모드에서 AgentScripts/HudV6Checks.cs V6Checks.Main으로 임시 새 게임 생성(17 통과), 이어 MapHudChecks.cs MapHudChecks.Main(128 통과). SeasonWeatherChecks.Main 323 통과(저장/불러오기 포함), HudV6LayoutChecks.Main 2070 통과(960/1280/1440 모의). 공유 Unity 테스트는 직렬 실행. Player 빌드 미실행.
- **Unity 현재 상태:** 마지막 확인 Play 중, 임시 Forest Small seed77 테스트 게임. 카메라 컴포넌트 enabled=false, Time.timeScale=0, focus 약(225.30,10,225.30), yaw45, ortho30으로 SAVE 캡처용 준비. 작업 재개 시 카메라 enabled=true로 복구하거나 Play 종료 후 다시 시작. static 상태가 초기화되면 씬·세션 실제 상태를 재확인할 것.
- **공식 CLI 주의:** editor_play가 Already in play mode라고 해도 Application.isPlaying=false였던 적이 있어 eval로 실제 확인. 필요 시 UnityEditor.EditorApplication.EnterPlaymode/ExitPlaymode 사용. 스크린샷 source=screen(카메라 방식은 Overlay HUD 누락), 상대 reports/... 경로는 Assets/reports/...로 저장됨.
- **Graphify:** 여러 차례 update 완료했지만 마지막 SetTint 복구·이름표끼리 겹침 수정 이후 재갱신 필요. 카카오 도구는 검색 결과 없음.
## 통합 SAVE 착수 — Claude에서 Codex로 진행한 작업 포함(사용자 추가 승인)
- 확인한 Codex Companion Task: 01a168c7(A2 구현, 중단), 01a168a3(V6 관리 창, 정적 컴파일 완료), 01a1688f(Notion 결정 반영, 승인 정책으로 쓰기 0건).
- A2: 시작 후보·불시착/개인 물자·맨바닥 제작·기초 건설/연구·소화·오락 관련 코드가 로컬에 있음. SaveSystem.NewGame 호출 연결 등 누락 요청이 보드에 남음. 완료로 표기하지 않고 현재 상태 그대로 통합 보존하며 빌드 가능한지 확인한다.
- V6 관리 창: 묶음 탭·하단 메뉴·Dropdown 등 구현, 정적 컴파일만 기존 통과. 실제 Unity 회귀는 새로 수행해야 함. Notion 결정 반영은 보고서 기준 적용 0건이며 기획 반영 완료로 쓰지 않는다.
- Unity는 이미 정상 종료된 상태(Logs/Editor.log 종료 확인). 새 실행으로 컴파일 검증 시도. 종료 요청은 취소 상태 유지.- **통합 확인 결과:** Unity 6000.5.8f1 재실행, 전체 소스 컴파일 완료(Logs/Editor.log Csc 및 Reload 완료, recompile_status compilationFailed=false). 새 Play 진입 후 메인 스레드 응답 5초 제한으로 검사/캡처 호출 지연, 새 관리 창·A2 Play 통과로 기록하지 않음. 기존 17/128/323/2070 결과는 통합 전 기준.
- **통합 Notion 갱신:** 같은 날짜 개발 일지에 9~11 항목 추가(지도/지형 수정·Companion 인수·실제 절벽 캡처 첨부), Trouble Shooting에 7~9 항목 추가. 기존 페이지/하위 페이지 삭제 없이 보존.
- **최종 SAVE 범위:** 현재 Scripts 전체와 관련 신규 .cs/.meta, 지형 셰이더, 4개 검사 및 변경된 HudV6LayoutChecks, 디자인 규칙/점검 결정/Notion 미적용 보고서, README/log/changelog/보드. A2 중단 상태는 명시하고 스냅샷으로 저장. prefab/씬/Water.mat/TimeManager/art/기타 미추적 파일 제외.- **SAVE 종료 확인:** Play 종료 후 Unity AssetDatabase.SaveAssets 및 SaveOpenScenes 호출. 통합 Play 캡처 combined-start-candidates.png는 실제로 타이틀 화면이므로 후보 검증 증빙이 아님(이름만 보고 사용 금지). 재개 시 후보 화면/게임 시작 직접 검증 우선. 최종 절벽 캡처와 다른 Companion 검증 시점 구분. Graphify update 완료(10007 nodes).