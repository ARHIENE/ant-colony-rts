---
name: project-ant-teamimport-and-bridge-deadlock
description: ant 프로젝트의 unity-cli Play모드 데드락 특성과 SIMUL-TeaamProject 반입 구조
metadata: 
  node_type: memory
  type: project
  originSessionId: bf42b02d-a070-491a-9e79-6cc098a56a0d
  modified: 2026-09-04T14:53:38.463Z
---

**unity-cli 브릿지 데드락은 Play 모드 진입 자체가 트리거, 맵 크기/코드와 무관** (2026-09-03 확인, 2026-09-04 근본 원인 규명+부분 수정)
- ant 프로젝트에서 Play 진입 시 unity-cli 브릿지가 CPU 유휴 상태로 멎는 현상이 맵 크기(xSize/zSize)를 안전값으로 되돌린 뒤에도 동일하게 재현됨. 즉 원인은 프로젝트 코드/씬 데이터가 아니라 브릿지 자체의 도메인 리로드 처리 버그.
- **2026-09-04 근본 원인 확정**: `refresh_assets` 커맨드가 `AssetDatabase.Refresh()`를 커맨드 처리 스택 안에서 동기 호출 → 도메인 리로드가 그 응답을 보내던 호출 자체를 끊어버림. 자세한 원인/수정 내용은 [[unity_cli_bridge_local_patch]] 참고 — `EditorApplication.update` 기반 1회성 지연 콜백으로 수정해서 재현 안 됨(재시작 불필요해짐, 12회 연속 폴링 검증). `delayCall`로 먼저 시도했으나 유니티 창이 백그라운드(포커스 없음)일 때 아예 실행 안 되는 걸 확인해서 폐기.
- 복구는 항상 강제종료(taskkill)+재시작만 통함, 자연 복구는 2분 이상 기다려도 안 됨. **2026-09-04부터 사용자가 이 복구를 확인 없이 바로 하도록 승인함** — [[feedback_unity_bridge_auto_restart]] 참고.
- **How to apply**: ant에서 Play 모드 자동 테스트를 시도할 때 데드락이 나면 맵 값부터 의심하지 말고 바로 강제종료+재시작으로 넘어갈 것(확인 불필요). `refresh_assets` 자체는 이제 안전해졌지만, Play 진입 직후 상태를 반복 폴링하는 호출이 마침 지연된 리로드 타이밍과 겹치면 여전히 멈출 수 있음 — 자동화로 Play 테스트를 완주하기 어려우면, 코드/데이터 수정 후 컴파일만 확인하고 실제 Play 검증은 사용자에게 맡기는 것도 합리적 선택.

**"개미가 안 움직인다/계속 도는 것처럼 멈춰있다" 증상의 진짜 원인: `runInBackground: 0`** (2026-09-04 확정)
- 사용자가 "개미들이 계속 돌아가지는데(멈춰있는데) 맞아?"라고 보고 → 처음엔 에디터 프로세스 자체 프리즈(taskkill 필요한 진짜 데드락)로 오판했으나, 이후 Play 테스트에서 별개의 증상 발견: 브릿지 커맨드 처리(`EditorApplication.update` 기반)는 정상 응답하는데, **개미 좌표/보스 회전 등 실제 게임플레이 상태가 몇 분간 좌표 하나 안 바뀌고 완전히 정지**해 있었음.
- 원인: `ProjectSettings/ProjectSettings.asset`의 `runInBackground: 0` — 유니티 창에 OS 포커스가 없으면 Play 모드의 실제 시뮬레이션(Time.deltaTime 기반 Update 루프)이 사실상 멈춘다. 브릿지 자체는 에디터 레벨(`EditorApplication.update`)이라 이것과 무관하게 계속 응답하므로, "브릿지는 살아있는데 게임 상태만 안 바뀜"이라는 특징적인 패턴으로 구분됨.
- **수정**: `runInBackground: 1`로 변경(직접 파일 edit) 후 재시작 → Play 진입해서 확인해보니 일개미가 실제로 좌표를 바꾸며 이동하고, 보스도 지속적으로 회전하며 타겟을 추적함(정상 확인).
- **How to apply**: 앞으로 Play 모드에서 "아무것도 안 움직인다/멈춰있다"는 보고를 받으면, 먼저 브릿지가 살아있는지(ping/get_editor_state 응답 여부)와 실제 게임 오브젝트 좌표가 폴링마다 바뀌는지를 구분해서 확인할 것 — 브릿지는 응답하는데 좌표만 고정이면 `runInBackground` 설정부터 의심. 이 프로젝트는 이미 켜놨으니 재발 안 하겠지만, 같은 unity-cli 워크플로를 쓰는 다른 프로젝트(FPS Manager 등)에서도 동일 증상이면 이 설정부터 확인.
- **보스 전투 파이프라인 검증 완료**: 위 수정 후 실전 테스트에서 보스가 근처 일개미 3마리를 실제로 탐지→원형 공격 캐스팅→데미지 판정까지 정상 수행해 전멸시킴을 확인(원형 공격 데미지 25 > 일개미 기본 체력 20이라 한 방에 즉사 — 밸런스상 참고할 것). 텔레그래프(빨간 장판) 자체는 짧은 노출시간(~1.6초) 때문에 스크린샷/폴링으로는 못 잡았지만, 데미지가 실제로 들어간 것으로 전체 파이프라인(타겟탐지/캐스팅/판정) 정상 동작을 간접 확인함.

**SIMUL-TeaamProject(팀 원본 저장소) 반입 구조**
- `E:\Git\ant\Assets\_TeamImport\`에 `github.com/ARHIENE/SIMUL-TeaamProject`(private, hyeonyeop 브랜치)의 Assets를 반입해둠. `.gitignore`에 `Assets/_TeamImport/`로 커밋 제외 처리됨(1.65GB, 유료 에셋스토어 패키지 다수 포함이라 절대 커밋 금지).
- `_TeamImport/Scripts`는 별도 어셈블리(`_TeamImport.asmdef`, autoReferenced:false)로 격리해둠 — 그대로 두면 네임스페이스 없는 global `ResourceType`/`IDamageable` 등이 우리 `AntColony.*` 코드와 충돌해서 컴파일 에러 남. 이 asmdef 덕분에 원본 TerrainGenerator 컴포넌트 등을 씬에서 그대로 확인 가능.
- 실제 활용 가능한 에셋: 지형 텍스처는 `_TeamImport/Prefabs/TerrainSampleAssets/Textures/Terrain/`(Grass_A/B, Sand, Rock 등), 로우폴리(확정 아트 스타일) 나무/바위 프리팹은 `_TeamImport/Prefabs/SimpleNaturePack/Prefabs/`(Tree_01~05, Rock_01~05) — 원본 3DScene.unity(400x400 규모, 자동생성 오브젝트 1만개+)는 너무 커서 그대로 쓰지 않고, 이 텍스처/프리팹만 뽑아 우리 AntColony.unity의 MapGenerator에 연결하는 방식 채택.
- `MapGenerator.terrainLayers[0].texture`는 이미 Grass_A_BaseColor.tif로 교체 완료(기존엔 URP 로고 placeholder였음).

**AntColony.unity를 3DScene(팀 원본 씬) 기반으로 완전히 교체함** (2026-09-03 세션 마지막)
- 기존 10x10 자체 제작 맵 대신, `Assets/_TeamImport/Scenes/3DScene.unity`(400x400, TerrainGenerator + 실제 텍스처/장식 프리팹 수천개 이미 배치됨) 내용을 `Assets/Scenes/AntColony.unity`에 덮어씀.
- 기존 MVP 씬은 `Assets/Scenes/AntColony_MVP_backup.unity`로 백업해둠(git 커밋 대상, 롤백용으로 보존).
- 우리 게임 오브젝트(QueenChamber/Barracks/Storage/DigSite/FoodNode1-2/SoilNode1-2/WildMonster/GameSystems/SelectionSystem/HUD/Main Camera)는 백업 씬을 Additive로 로드한 뒤 `modify_gameobject`로 `/TerrainGenerator`(상대 씬의 루트 오브젝트)에 reparent했다가 다시 루트로 unparent하는 방식으로 씬 간 이동시킴 — unity-cli에 씬 이동 전용 툴이 없어서 찾은 우회법.
- 팀 원본의 자체 게임플레이 오브젝트(Ant, Canvas, GameManager, SpawnPoint, GameDataManager, Sparrow, 팀 자체 Main Camera)는 전부 삭제(우리 게임과 무관, 우리 시스템으로 대체).
- **주의**: 씬 간 이동 직후 바로 저장하지 않고 여러 단계(재컴파일 등)를 거치면 이동시킨 오브젝트가 저장 파일에서 통째로 누락되는 현상 실제 발생함(Main Camera 1회 유실, 백업에서 재이동으로 복구). 오브젝트를 씬 간 이동시킨 직후에는 바로 save_scene → grep으로 파일에 실제 포함됐는지 즉시 검증할 것, 다른 작업 여러 개를 거친 뒤에 저장하지 말 것.
- `SnapToTerrainMenu.cs`가 지형 오브젝트 이름을 `"MapGenerator"`로 하드코딩했던 버그 수정함(`"TerrainGenerator"`도 찾도록) — 씬 교체 후 이 코드를 다시 쓸 일 있으면 이미 고쳐져 있음.
- 건물들은 새 400x400 지형의 원점 근처(로컬 좌표 0~10 구간, 지형 전체 중 아주 작은 구석)에 있음 — 실제 지형 높이로 스냅은 됐지만 위치 자체(중앙 배치 등)는 아직 재배치 안 함.

- **검사 스크립트 함정(2026-10-24):** _TeamImport에 전역 네임스페이스 동명 타입(BossHealth·BossBasicPatternLoop·BossPatternSequenceSimple·Boss*AoE·GroundTelegraph*·IDamageable·SelectionManager·ResourceType 등)이 있어 AgentScripts(전역 클래스)에서 `using AntColony.Boss`보다 이쪽이 먼저 해석됨 → 검사에서는 항상 `AntColony.Boss.BossHealth`처럼 전체 이름 사용.
