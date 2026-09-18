# Graph Report - ant  (2026-09-13)

## Corpus Check
- 84 files · ~35,765 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1604 nodes · 2538 edges · 89 communities (79 shown, 8 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 55 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `985300b7`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- manifest.json
- SelectionManager
- CombatRolePrototypeBootstrapper
- dependencies
- AntColony.Core
- BuildingPlacementController
- SoldierAnt
- packages-lock.json
- ResourceNode
- MapGenerator
- BossCircleAoE
- com.unity.modules.uielements
- com.unity.nuget.newtonsoft-json
- BuildingConstructionSite
- .Main
- CommanderAnt
- com.unity.render-pipelines.core
- IsometricCameraController
- BuildingBase
- com.unity.addressables
- com.unity.modules.jsonserialize
- com.unity.collections
- WildMonster
- ResearchLab
- AntUnitBase
- UnitSelectionController
- ReadmeEditor
- com.unity.test-framework
- .Initialize
- SelectedUnitPanel
- .Main
- com.unity.modules.imageconversion
- com.unity.modules.audio
- com.unity.modules.physics2d
- com.unity.modules.physics
- IDamageable
- CommanderProgression
- .SetMaterial
- SelectableObject
- BossConeAoE
- GroundTelegraphSector
- GroundTelegraphLine
- AttackMoveController
- 2026-09-02 ~ 2026-09-03 (세션 1)
- CommanderRank
- GroundTelegraphCircle
- com.unity.ext.nunit
- Unity Pipeline (agent control)
- .IsAirborne
- EnemyColony
- BossLineAoE
- MonoBehaviour
- BossPatternSequenceSimple
- Unity Pipeline (agent control)
- com.unity.ai.navigation
- com.unity.burst
- com.unity.modules.unitywebrequest
- .Spawn
- ResourceNodeStatus
- .Main
- .SetupFarm
- MeshFilter
- Object
- Collider
- changelog.md
- 프로젝트 로그
- Renderer
- .ConfigureCommander
- DigSite
- Storage
- .Main
- ColonyInvasion
- .CreateSelectionBoxImage
- 2026-09-05 (세션 4)
- 개미 소굴 RTS
- 2026-09-04~05 (세션 3)
- 2026-09-05 (세션 5 — 유닛 UI/병영 티어/야생 몬스터 AI)
- 2026-09-06 (세션 6 — 역할 강화 연구소 / 일개미 건설 배치 기반)
- 2026-09-07 (세션 7 — 건설 흐름 완성 / Ranged 역할 프로토타입)
- 2026-09-08 (세션 10 — Support 역할 완료 / Flying 이동·대공 기반 착수)
- .BakeAll
- 2026-09-07 (세션 8 — Defense 역할 프로토타입 / 생산 활성화 수정)
- 2026-09-08 (세션 9 — 건물 내구도·전멸 패배 / Flying 역할 프로토타입)
- 2026-09-09 (세션 11 — Flying 완성 / 농사 최소 프로토타입)
- dependencies
- 프로젝트 작업 규칙
- 코드 작성 규칙

## God Nodes (most connected - your core abstractions)
1. `CommanderAnt` - 57 edges
2. `BuildingPlacementController` - 44 edges
3. `AntColony.Core` - 43 edges
4. `SoldierAnt` - 39 edges
5. `WorkerAnt` - 39 edges
6. `AntColony.Data` - 33 edges
7. `SelectionManager` - 33 edges
8. `AntColony.Buildings` - 32 edges
9. `BuildingBase` - 31 edges
10. `WildMonster` - 28 edges

## Surprising Connections (you probably didn't know these)
- `WorkerAnt` --references--> `BuildingConstructionSite`  [EXTRACTED]
  Assets/Scripts/Units/WorkerAnt.cs → Assets/Scripts/Buildings/BuildingConstructionSite.cs
- `BuildingPlacementController` --references--> `SelectionManager`  [EXTRACTED]
  Assets/Scripts/Buildings/BuildingPlacementController.cs → Assets/Scripts/Units/SelectionManager.cs
- `BuildingPlacementController` --references--> `WorkerAnt`  [EXTRACTED]
  Assets/Scripts/Buildings/BuildingPlacementController.cs → Assets/Scripts/Units/WorkerAnt.cs
- `AttackMoveController` --references--> `BuildingPlacementController`  [EXTRACTED]
  Assets/Scripts/Units/AttackMoveController.cs → Assets/Scripts/Buildings/BuildingPlacementController.cs
- `UnitSelectionController` --references--> `BuildingPlacementController`  [EXTRACTED]
  Assets/Scripts/Units/UnitSelectionController.cs → Assets/Scripts/Buildings/BuildingPlacementController.cs

## Import Cycles
- None detected.

## Communities (89 total, 8 thin omitted)

### Community 0 - "manifest.json"
Cohesion: 0.04
Nodes (49): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+41 more)

### Community 1 - "SelectionManager"
Cohesion: 0.25
Nodes (7): SelectionManager, Camera, LayerMask, List, SelectableObject, Vector2, Rect

### Community 2 - "CombatRolePrototypeBootstrapper"
Cohesion: 0.08
Nodes (23): CombatRolePrototypeBootstrapper, Barracks, GameObject, MenuItem, ResearchLab, Transform, MenuItem, DataAssetBootstrapper (+15 more)

### Community 3 - "dependencies"
Cohesion: 0.04
Nodes (50): dependencies, com.akiojin.unity-cli-bridge, com.unity.ai.assistant, com.unity.ai.inference, com.unity.ai.navigation, com.unity.collab-proxy, com.unity.ide.rider, com.unity.ide.visualstudio (+42 more)

### Community 4 - "AntColony.Core"
Cohesion: 0.09
Nodes (14): AntPool, QueenChamber, UnitData, SetupCommanders, AntColony.Boss.AoE, AntColony.Data, AntColony.Units, AntColony.Core (+6 more)

### Community 5 - "BuildingPlacementController"
Cohesion: 0.06
Nodes (40): BuildingPlacementController, ConsumesPointerInput, IsPlacing, Barracks, BuildingBase, Camera, GameObject, LayerMask (+32 more)

### Community 6 - "SoldierAnt"
Cohesion: 0.05
Nodes (34): IAirborne, IsAirborne, Vector3, SoldierAnt, IsAirborne, IsFlying, State, Attacking (+26 more)

### Community 7 - "packages-lock.json"
Cohesion: 0.04
Nodes (44): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+36 more)

### Community 8 - "ResourceNode"
Cohesion: 0.11
Nodes (15): ResourceNode, AmountRemaining, CanGather, GatherRateMultiplier, IsDepleted, IsRaidLocked, IsRaidLoot, IsRegrowing (+7 more)

### Community 9 - "MapGenerator"
Cohesion: 0.09
Nodes (23): GameObject, MenuItem, MapGeneratorEditor, Collider, MenuItem, SnapToTerrainMenu, Layer, MapGenerator (+15 more)

### Community 10 - "BossCircleAoE"
Cohesion: 0.29
Nodes (5): BossCircleAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 11 - "com.unity.modules.uielements"
Cohesion: 0.07
Nodes (31): dependencies, depth, source, version, dependencies, depth, source, version (+23 more)

### Community 12 - "com.unity.nuget.newtonsoft-json"
Cohesion: 0.07
Nodes (29): dependencies, depth, source, version, dependencies, depth, source, url (+21 more)

### Community 13 - "BuildingConstructionSite"
Cohesion: 0.07
Nodes (18): CommanderChecks, Barracks, Func, MonoBehaviour, QueenChamber, Task, BuildingConstructionSite, BuildTimeSeconds (+10 more)

### Community 14 - ".Main"
Cohesion: 0.09
Nodes (25): RegressionChecks, AttackMoveController, BossHealth, BuildingBase, FieldInfo, Func, GameObject, List (+17 more)

### Community 15 - "CommanderAnt"
Cohesion: 0.08
Nodes (21): CommanderAnt, AllowedRoles, Armor, AttackDamage, CanChangeAllocation, CanStartConstruction, CarryCapacity, CommanderName (+13 more)

### Community 16 - "com.unity.render-pipelines.core"
Cohesion: 0.09
Nodes (25): depth, source, version, dependencies, depth, source, version, dependencies (+17 more)

### Community 17 - "IsometricCameraController"
Cohesion: 0.26
Nodes (4): IsometricCameraController, Camera, Vector3, AntColony.Camera

### Community 18 - "BuildingBase"
Cohesion: 0.05
Nodes (27): BuildingBase, CountsTowardPlayerDefeat, CurrentHealth, Data, IsDead, IsDepositPoint, MaxHealth, Position (+19 more)

### Community 19 - "com.unity.addressables"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, depth, source, url, version (+8 more)

### Community 20 - "com.unity.modules.jsonserialize"
Cohesion: 0.08
Nodes (25): dependencies, depth, source, version, dependencies, depth, source, version (+17 more)

### Community 21 - "com.unity.collections"
Cohesion: 0.08
Nodes (27): dependencies, depth, source, url, version, dependencies, depth, source (+19 more)

### Community 22 - "WildMonster"
Cohesion: 0.08
Nodes (20): InvasionChecks, BuildingBase, EnemyColony, FieldInfo, Func, List, NavMeshAgent, Task (+12 more)

### Community 23 - "ResearchLab"
Cohesion: 0.05
Nodes (24): Barracks, CurrentTier, IsUpgrading, MaxTier, Role, UpgradeFoodCost, UpgradeSoilCost, IEnumerator (+16 more)

### Community 24 - "AntUnitBase"
Cohesion: 0.09
Nodes (21): ObjectPool, Dictionary, GameObject, Quaternion, Vector3, AntUnitBase, Agent, Armor (+13 more)

### Community 25 - "UnitSelectionController"
Cohesion: 0.22
Nodes (8): UnitSelectionController, AttackMoveController, Camera, Color, IDamageable, LayerMask, ResourceNode, Vector2

### Community 26 - "ReadmeEditor"
Cohesion: 0.18
Nodes (8): GUIStyle, ReadmeEditor, BodyStyle, ButtonStyle, HeadingStyle, LinkStyle, TitleStyle, GUIContent

### Community 27 - "com.unity.test-framework"
Cohesion: 0.12
Nodes (17): dependencies, dependencies, depth, source, url, version, depth, dependencies (+9 more)

### Community 28 - ".Initialize"
Cohesion: 0.15
Nodes (9): GameObject, ObjectPool, UnitData, GameObject, ObjectPool, UnitData, GameObject, ObjectPool (+1 more)

### Community 29 - "SelectedUnitPanel"
Cohesion: 0.24
Nodes (11): SelectedUnitPanel, Button, Color, Font, GameObject, Image, RectTransform, Text (+3 more)

### Community 30 - ".Main"
Cohesion: 0.18
Nodes (10): CommanderProgressionChecks, BossHealth, BuildingBase, IDamageable, MonoBehaviour, SelectableObject, Task, Vector3 (+2 more)

### Community 31 - "com.unity.modules.imageconversion"
Cohesion: 0.06
Nodes (32): dependencies, depth, source, url, version, dependencies, depth, source (+24 more)

### Community 32 - "com.unity.modules.audio"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, version, dependencies, depth, source, version (+13 more)

### Community 33 - "com.unity.modules.physics2d"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, version, dependencies, depth, source, version (+18 more)

### Community 34 - "com.unity.modules.physics"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 35 - "IDamageable"
Cohesion: 0.18
Nodes (8): BossBasicPatternLoop, LayerMask, Transform, Vector3, IDamageable, IsDead, Position, Vector3

### Community 36 - "CommanderProgression"
Cohesion: 0.18
Nodes (8): IDamageable, CommanderProgression, ArmorBonus, AttackBonus, Level, Xp, XpToNext, IDamageable

### Community 37 - ".SetMaterial"
Cohesion: 0.20
Nodes (8): SetupFishing, Collider, Color, GameObject, Material, Renderer, AntColony.Setup, NavMeshObstacle

### Community 38 - "SelectableObject"
Cohesion: 0.22
Nodes (6): SelectableObject, IsSelected, Color, Renderer, Transform, Vector3

### Community 39 - "BossConeAoE"
Cohesion: 0.29
Nodes (5): BossConeAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 40 - "GroundTelegraphSector"
Cohesion: 0.27
Nodes (6): GroundTelegraphSector, LayerMask, Mesh, MeshFilter, Quaternion, Vector3

### Community 41 - "GroundTelegraphLine"
Cohesion: 0.24
Nodes (6): GroundTelegraphLine, LayerMask, Mesh, MeshFilter, Quaternion, Vector3

### Community 42 - "AttackMoveController"
Cohesion: 0.21
Nodes (7): AttackMoveController, ConsumesPointerInput, IsAttackMode, Camera, LayerMask, Vector2, IReadOnlyList

### Community 43 - "2026-09-02 ~ 2026-09-03 (세션 1)"
Cohesion: 0.15
Nodes (13): 2026-09-02 ~ 2026-09-03 (세션 1), MVP 수직 슬라이스 — 이번 세션 구현 완료, SIMUL-TeaamProject에서 추가 이식(보스 레이드 / 선택 시스템) — 2026-09-02, 개요, 기획서 시스템 요약, 랜덤맵 생성(MapGenerator) — 이전 팀 프로젝트에서 포팅, 보스 AoE/텔레그래프 시스템 — `Assets/Scripts/Boss/`, 설계 메모 (+5 more)

### Community 44 - "CommanderRank"
Cohesion: 0.22
Nodes (7): CommanderRank, Captain, Corporal, General, Lieutenant, Sergeant, CommanderRanks

### Community 45 - "GroundTelegraphCircle"
Cohesion: 0.32
Nodes (5): GroundTelegraphCircle, LayerMask, Mesh, MeshFilter, Vector3

### Community 46 - "com.unity.ext.nunit"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, version, dependencies, depth, source, url (+4 more)

### Community 47 - "Unity Pipeline (agent control)"
Cohesion: 0.18
Nodes (10): 1. Install & verify, 2. Autonomous edit loop (Editor), 3. Runtime hot reload, 4. Bulk construction: `run_script` (the builder pattern), 5. Project audit (Editor), 5. Quick C# eval (Runtime), Alternative: with helper — `reload_file_override`, Gotchas (+2 more)

### Community 48 - ".IsAirborne"
Cohesion: 0.42
Nodes (4): CombatTargeting, IDamageable, UnitRole, Vector3

### Community 49 - "EnemyColony"
Cohesion: 0.20
Nodes (6): SetupRaid, EnemyColony, IsDefeated, RemainingBuildings, List, Vector3

### Community 50 - "BossLineAoE"
Cohesion: 0.29
Nodes (5): BossLineAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 51 - "MonoBehaviour"
Cohesion: 0.13
Nodes (11): BossHealth, CurrentHp, IsDead, MaxHp, Position, Vector3, CommanderRoster, Transform (+3 more)

### Community 52 - "BossPatternSequenceSimple"
Cohesion: 0.29
Nodes (4): BossPatternSequenceSimple, LayerMask, Transform, Vector3

### Community 53 - "Unity Pipeline (agent control)"
Cohesion: 0.18
Nodes (10): 1. Install & verify, 2. Autonomous edit loop (Editor), 3. Runtime hot reload, 4. Bulk construction: `run_script` (the builder pattern), 5. Project audit (Editor), 5. Quick C# eval (Runtime), Alternative: with helper — `reload_file_override`, Gotchas (+2 more)

### Community 54 - "com.unity.ai.navigation"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, url, version, dependencies, depth, source (+3 more)

### Community 55 - "com.unity.burst"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, url, version, dependencies, depth, source (+14 more)

### Community 56 - "com.unity.modules.unitywebrequest"
Cohesion: 0.06
Nodes (38): dependencies, dependencies, depth, source, version, dependencies, depth, source (+30 more)

### Community 57 - ".Spawn"
Cohesion: 0.24
Nodes (6): MoveMarker, Collider, Color, Material, MeshRenderer, Vector3

### Community 58 - "ResourceNodeStatus"
Cohesion: 0.29
Nodes (4): ResourceNodeStatus, StatusText, Camera, GUIStyle

### Community 59 - ".Main"
Cohesion: 0.33
Nodes (5): CommanderEdgeChecks, Button, ResourceNode, SelectableObject, Task

### Community 60 - ".SetupFarm"
Cohesion: 0.40
Nodes (4): GameObject, MenuItem, Transform, FarmPrototypeBootstrapper

### Community 64 - "changelog.md"
Cohesion: 0.20
Nodes (9): 2026-09-03~04 (세션 2), 2026-09-05 (Codex 인수인계 / SAVE 연결 검증), 2026-09-10 SAVE — 이전 로그 이관 (2026-09-09 상태), 2026-09-11 — SAVE 연결 복구, 2026-09-11 — 이전 로그 요약 및 중단 지점, 2026-09-13 SAVE — 이전 로그(2026-09-11) 이관, 변경 이력, 이번 작업 (+1 more)

### Community 65 - "프로젝트 로그"
Cohesion: 0.40
Nodes (4): 2026-09-13 SAVE — 사용자 점검 완료, 다음 작업과 미정 사항, 프로젝트 구조와 규칙, 프로젝트 로그

### Community 67 - ".ConfigureCommander"
Cohesion: 0.36
Nodes (3): List, UnitRole, IEnumerable

### Community 70 - ".Main"
Cohesion: 0.09
Nodes (18): FishingChecks, FieldInfo, Func, MonoBehaviour, QueenChamber, ResourceNode, Task, RaidChecks (+10 more)

### Community 71 - "ColonyInvasion"
Cohesion: 0.25
Nodes (4): SetupInvasion, ColonyInvasion, List, Transform

### Community 73 - ".CreateSelectionBoxImage"
Cohesion: 0.25
Nodes (6): AttackMoveController, Canvas, CanvasScaler, GraphicRaycaster, Image, RectTransform

### Community 74 - "2026-09-05 (세션 4)"
Cohesion: 0.29
Nodes (7): 2026-09-05 (세션 4), RTS 카메라 — 엣지스크롤 무한 패닝 버그 수정, unity-cli 관련 정리 (메모리로 이관), unity-cli 브릿지 데드락 — 이번 세션에도 2회 재발, 개요, 건물/자원노드/보스 클러스터 재배치 + NavMesh 재베이크, 일개미 수동 채집 지시 — `WorkerAnt.CommandGather` 신규

### Community 75 - "개미 소굴 RTS"
Cohesion: 0.29
Nodes (6): 개미 소굴 RTS, 게임 설명, 기술 정보, 브랜치, 조작법, 현재 구현 상태

### Community 78 - "2026-09-04~05 (세션 3)"
Cohesion: 0.33
Nodes (6): 2026-09-04~05 (세션 3), unity-cli 브릿지 데드락 — 근본 원인 2가지 규명 및 수정, 개요, 보스 레이드 — MiniBirdBoss 실전 배치, 유닛 조작 UX — 원본 팀 프로젝트 대비 누락분 보강, 자원 유지비 / 반란 — `UpkeepManager` 신규

### Community 79 - "2026-09-05 (세션 5 — 유닛 UI/병영 티어/야생 몬스터 AI)"
Cohesion: 0.33
Nodes (6): 2026-09-05 (세션 5 — 유닛 UI/병영 티어/야생 몬스터 AI), 문서 동기화, 선택 유닛 정보 UI, 야생 몬스터 공격 AI, 역할군별 병영과 독립 티어, 이전 상태 이관

### Community 80 - "2026-09-06 (세션 6 — 역할 강화 연구소 / 일개미 건설 배치 기반)"
Cohesion: 0.33
Nodes (6): 2026-09-06 (세션 6 — 역할 강화 연구소 / 일개미 건설 배치 기반), 문서 동기화, 역할 강화 연구소, 연구소 Play 검증, 이전 SAVE 상태 이관, 일개미 건물 배치·건설 기반

### Community 81 - "2026-09-07 (세션 7 — 건설 흐름 완성 / Ranged 역할 프로토타입)"
Cohesion: 0.33
Nodes (6): 2026-09-07 (세션 7 — 건설 흐름 완성 / Ranged 역할 프로토타입), Ranged 역할 프로토타입, 검증과 문서 동기화, 병영·연구소 건설 흐름 완성, 역할 선택 HUD와 배치 시스템, 이전 SAVE 상태 이관

### Community 82 - "2026-09-08 (세션 10 — Support 역할 완료 / Flying 이동·대공 기반 착수)"
Cohesion: 0.33
Nodes (6): 2026-09-08 (세션 10 — Support 역할 완료 / Flying 이동·대공 기반 착수), Flying 기반 코드 착수, Support 역할 프로토타입, 검증과 문서 동기화, 비행·대공 규칙 확정, 이전 SAVE 상태 이관

### Community 84 - ".BakeAll"
Cohesion: 0.40
Nodes (3): MenuItem, NavMeshBakeMenu, NavMeshSurface

### Community 87 - "2026-09-07 (세션 8 — Defense 역할 프로토타입 / 생산 활성화 수정)"
Cohesion: 0.40
Nodes (5): 2026-09-07 (세션 8 — Defense 역할 프로토타입 / 생산 활성화 수정), Defense 역할 프로토타입, 문서 동기화, 비활성 생산 개체 수정과 검증, 이전 SAVE 상태 이관

### Community 88 - "2026-09-08 (세션 9 — 건물 내구도·전멸 패배 / Flying 역할 프로토타입)"
Cohesion: 0.40
Nodes (5): 2026-09-08 (세션 9 — 건물 내구도·전멸 패배 / Flying 역할 프로토타입), Flying 역할 프로토타입, 건물 내구도와 전멸 패배, 검증과 문서 동기화, 이전 SAVE 상태 이관

### Community 89 - "2026-09-09 (세션 11 — Flying 완성 / 농사 최소 프로토타입)"
Cohesion: 0.40
Nodes (5): 2026-09-09 (세션 11 — Flying 완성 / 농사 최소 프로토타입), Flying 역할 완성, 개발 도구, 농사 최소 프로토타입, 이전 SAVE 상태 이관

### Community 90 - "dependencies"
Cohesion: 0.10
Nodes (20): dependencies, depth, source, version, dependencies, depth, source, version (+12 more)

## Knowledge Gaps
- **618 isolated node(s):** `graphify`, `Role`, `CurrentTier`, `MaxTier`, `IsUpgrading` (+613 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 874 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **8 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `SoldierAnt` connect `SoldierAnt` to `CombatRolePrototypeBootstrapper`, `AntColony.Core`, `AttackMoveController`, `.Main`, `WildMonster`, `AntUnitBase`, `UnitSelectionController`, `.Initialize`?**
  _High betweenness centrality (0.055) - this node is a cross-community bridge._
- **Why does `BuildingPlacementController` connect `BuildingPlacementController` to `SelectionManager`, `AntColony.Core`, `SoldierAnt`, `.CreateSelectionBoxImage`, `AttackMoveController`, `.Main`, `MonoBehaviour`, `UnitSelectionController`?**
  _High betweenness centrality (0.055) - this node is a cross-community bridge._
- **Why does `CommanderAnt` connect `CommanderAnt` to `.ConfigureCommander`, `AntColony.Core`, `CommanderProgression`, `.Main`, `SoldierAnt`, `CommanderRank`, `BuildingConstructionSite`, `.Main`, `MonoBehaviour`, `WildMonster`, `.Main`, `.Initialize`, `SelectedUnitPanel`, `.Main`?**
  _High betweenness centrality (0.046) - this node is a cross-community bridge._
- **What connects `graphify`, `Role`, `CurrentTier` to the rest of the system?**
  _618 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `manifest.json` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._
- **Should `CombatRolePrototypeBootstrapper` be split into smaller, more focused modules?**
  _Cohesion score 0.08350951374207188 - nodes in this community are weakly interconnected._
- **Should `dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._