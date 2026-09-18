# Graph Report - ant  (2026-09-16)

## Corpus Check
- 99 files · ~47,983 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1808 nodes · 3134 edges · 108 communities (97 shown, 6 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 77 edges (avg confidence: 0.81)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `53a12918`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- manifest.json
- BuildingPlacementController
- dependencies
- AntColony.Core
- BuildingBase
- SoldierAnt
- packages-lock.json
- ResourceNode
- MapGenerator
- GroundTelegraphCircle
- com.unity.modules.audio
- dependencies
- BuildingConstructionSite
- .Main
- CommanderAnt
- com.unity.render-pipelines.core
- IsometricCameraController
- Barracks
- com.unity.modules.uielements
- com.unity.modules.jsonserialize
- CommanderTraits
- com.unity.burst
- ResearchLab
- AntUnitBase
- com.unity.nuget.mono-cecil
- CombatRolePrototypeBootstrapper
- com.unity.test-framework
- WorkerAnt
- HUDController
- .Main
- ReadmeEditor
- PrisonerCamp
- com.unity.collections
- com.unity.modules.physics
- CommanderSkills
- NurseryChamber
- State
- State
- GroundTelegraphSector
- BossConeAoE
- BossLineAoE
- SelectedUnitPanel
- 2026-09-02 ~ 2026-09-03 (세션 1)
- CommanderRank
- ColonyInvasion
- com.unity.modules.imgui
- BossCircleAoE
- UnitRole
- .Main
- EnemyCommander
- BossHealth
- AttackMoveController
- CommanderAcquisitionPanel
- com.unity.ai.navigation
- .Main
- com.unity.modules.unitywebrequest
- SelectionManager
- EnemyColony
- BuildingKind
- .Spawn
- ScoutPost
- SelectableObject
- changelog.md
- 프로젝트 로그
- .Main
- CommanderProgression
- com.unity.nuget.newtonsoft-json
- com.unity.modules.ui
- .Main
- dependencies
- .CreateSelectionBoxImage
- .Create
- 2026-09-05 (세션 4)
- 개미 소굴 RTS
- Q: read log.md and continue task
- .Main
- 2026-09-04~05 (세션 3)
- 2026-09-05 (세션 5 — 유닛 UI/병영 티어/야생 몬스터 AI)
- 2026-09-06 (세션 6 — 역할 강화 연구소 / 일개미 건설 배치 기반)
- 2026-09-07 (세션 7 — 건설 흐름 완성 / Ranged 역할 프로토타입)
- 2026-09-08 (세션 10 — Support 역할 완료 / Flying 이동·대공 기반 착수)
- 2026-09-14 SAVE — 이전 로그(적 소굴 AI 경제 성장) 이관
- .BakeAll
- SupportChecks
- .Main
- 2026-09-07 (세션 8 — Defense 역할 프로토타입 / 생산 활성화 수정)
- 2026-09-08 (세션 9 — 건물 내구도·전멸 패배 / Flying 역할 프로토타입)
- 2026-09-09 (세션 11 — Flying 완성 / 농사 최소 프로토타입)
- com.unity.modules.physics2d
- IDamageable
- .Main
- .Setup
- .Main
- BossPatternSequenceSimple
- GroundTelegraphLine
- 프로젝트 작업 규칙
- CLAUDE.md
- .ConfigureCommander
- .CreateButton
- Prisoner
- BossBasicPatternLoop
- .Main
- .SetupFarm

## God Nodes (most connected - your core abstractions)
1. `CommanderAnt` - 94 edges
2. `BuildingPlacementController` - 51 edges
3. `AntColony.Core` - 51 edges
4. `BuildingBase` - 49 edges
5. `AntColony.Data` - 46 edges
6. `AntColony.Buildings` - 44 edges
7. `UnitRole` - 44 edges
8. `ResourceNode` - 43 edges
9. `SoldierAnt` - 39 edges
10. `WorkerAnt` - 39 edges

## Surprising Connections (you probably didn't know these)
- `BossCircleAoE` --references--> `GroundTelegraphCircle`  [EXTRACTED]
  Assets/Scripts/Boss/AoE/BossCircleAoE.cs → Assets/Scripts/Boss/Telegraph/GroundTelegraphCircle.cs
- `BossBasicPatternLoop` --references--> `BossCircleAoE`  [EXTRACTED]
  Assets/Scripts/Boss/BossBasicPatternLoop.cs → Assets/Scripts/Boss/AoE/BossCircleAoE.cs
- `BossPatternSequenceSimple` --references--> `BossCircleAoE`  [EXTRACTED]
  Assets/Scripts/Boss/BossPatternSequenceSimple.cs → Assets/Scripts/Boss/AoE/BossCircleAoE.cs
- `BossConeAoE` --references--> `GroundTelegraphSector`  [EXTRACTED]
  Assets/Scripts/Boss/AoE/BossConeAoE.cs → Assets/Scripts/Boss/Telegraph/GroundTelegraphSector.cs
- `BossPatternSequenceSimple` --references--> `BossConeAoE`  [EXTRACTED]
  Assets/Scripts/Boss/BossPatternSequenceSimple.cs → Assets/Scripts/Boss/AoE/BossConeAoE.cs

## Import Cycles
- None detected.

## Communities (108 total, 6 thin omitted)

### Community 0 - "manifest.json"
Cohesion: 0.04
Nodes (49): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+41 more)

### Community 2 - "BuildingPlacementController"
Cohesion: 0.13
Nodes (13): BuildingPlacementController, ConsumesPointerInput, IsPlacing, Barracks, Camera, Collider, GameObject, LayerMask (+5 more)

### Community 3 - "dependencies"
Cohesion: 0.04
Nodes (50): dependencies, com.akiojin.unity-cli-bridge, com.unity.ai.assistant, com.unity.ai.inference, com.unity.ai.navigation, com.unity.collab-proxy, com.unity.ide.rider, com.unity.ide.visualstudio (+42 more)

### Community 4 - "AntColony.Core"
Cohesion: 0.07
Nodes (19): SetupFishing, Collider, Color, GameObject, Material, Renderer, SetupInvasion, AntColony.Boss.AoE (+11 more)

### Community 5 - "BuildingBase"
Cohesion: 0.07
Nodes (19): SetupRaid, BuildingBase, CountsTowardPlayerDefeat, CurrentHealth, Data, IsDead, IsDepositPoint, MaxHealth (+11 more)

### Community 6 - "SoldierAnt"
Cohesion: 0.20
Nodes (8): IAirborne, IsAirborne, SoldierAnt, IsAirborne, IsFlying, GameObject, LayerMask, Vector3

### Community 7 - "packages-lock.json"
Cohesion: 0.04
Nodes (44): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+36 more)

### Community 8 - "ResourceNode"
Cohesion: 0.08
Nodes (19): ResourceNode, AmountRemaining, CanGather, GatherRateMultiplier, IsDepleted, IsRaidLocked, IsRaidLoot, IsRegrowing (+11 more)

### Community 9 - "MapGenerator"
Cohesion: 0.09
Nodes (23): GameObject, MenuItem, MapGeneratorEditor, Collider, MenuItem, SnapToTerrainMenu, Layer, MapGenerator (+15 more)

### Community 10 - "GroundTelegraphCircle"
Cohesion: 0.32
Nodes (5): GroundTelegraphCircle, LayerMask, Mesh, MeshFilter, Vector3

### Community 11 - "com.unity.modules.audio"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, version, dependencies, depth, source, version (+13 more)

### Community 12 - "dependencies"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, url, version, dependencies, depth, source (+14 more)

### Community 13 - "BuildingConstructionSite"
Cohesion: 0.07
Nodes (17): CommanderChecks, Barracks, Func, MonoBehaviour, QueenChamber, Task, BuildingConstructionSite, BuildTimeSeconds (+9 more)

### Community 14 - ".Main"
Cohesion: 0.27
Nodes (6): InvasionChecks, FieldInfo, Func, List, NavMeshAgent, Task

### Community 15 - "CommanderAnt"
Cohesion: 0.06
Nodes (30): CommanderAnt, AllowedRoles, Armor, AttackDamage, CanChangeAllocation, CanDefensiveStance, CanPowerStrike, CanStartConstruction (+22 more)

### Community 16 - "com.unity.render-pipelines.core"
Cohesion: 0.09
Nodes (25): depth, source, version, dependencies, depth, source, version, dependencies (+17 more)

### Community 17 - "IsometricCameraController"
Cohesion: 0.26
Nodes (4): IsometricCameraController, Camera, Vector3, AntColony.Camera

### Community 18 - "Barracks"
Cohesion: 0.06
Nodes (22): Barracks, CurrentTier, IsUpgrading, MaxTier, Role, UpgradeFoodCost, UpgradeSoilCost, IEnumerator (+14 more)

### Community 19 - "com.unity.modules.uielements"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, url, version, depth, source, version (+7 more)

### Community 20 - "com.unity.modules.jsonserialize"
Cohesion: 0.08
Nodes (25): dependencies, depth, source, version, dependencies, depth, source, version (+17 more)

### Community 21 - "CommanderTraits"
Cohesion: 0.16
Nodes (10): CommanderPersonality, Balanced, Brave, Cautious, Devoted, CommanderTraits, ArmorBonus, AttackBonus (+2 more)

### Community 22 - "com.unity.burst"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, url, version, dependencies, depth, source (+14 more)

### Community 23 - "ResearchLab"
Cohesion: 0.18
Nodes (6): ResearchLab, IsResearching, MaxLevel, Role, Target, IEnumerator

### Community 24 - "AntUnitBase"
Cohesion: 0.05
Nodes (32): QueenChamber, SetupCommanders, MenuItem, DataAssetBootstrapper, CommanderRoster, Commanders, Count, Instance (+24 more)

### Community 25 - "com.unity.nuget.mono-cecil"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, version, dependencies, depth, source, url (+9 more)

### Community 26 - "CombatRolePrototypeBootstrapper"
Cohesion: 0.23
Nodes (7): CombatRolePrototypeBootstrapper, Barracks, GameObject, MenuItem, MonoScript, ResearchLab, Transform

### Community 27 - "com.unity.test-framework"
Cohesion: 0.12
Nodes (17): dependencies, dependencies, depth, source, url, version, depth, dependencies (+9 more)

### Community 28 - "WorkerAnt"
Cohesion: 0.12
Nodes (9): Vector3, WorkerAnt, CanStartConstruction, CarryCapacity, GatherRate, IsCarrying, IsConstructing, Vector3 (+1 more)

### Community 29 - "HUDController"
Cohesion: 0.10
Nodes (19): HUDController, SelectedCommander, Barracks, Button, Canvas, CanvasScaler, Font, GraphicRaycaster (+11 more)

### Community 30 - ".Main"
Cohesion: 0.27
Nodes (6): CommanderProgressionChecks, BindingFlags, MonoBehaviour, Task, Vector3, MethodInfo

### Community 31 - "ReadmeEditor"
Cohesion: 0.12
Nodes (13): GUIStyle, ReadmeEditor, BodyStyle, ButtonStyle, HeadingStyle, LinkStyle, TitleStyle, Texture2D (+5 more)

### Community 32 - "PrisonerCamp"
Cohesion: 0.13
Nodes (12): PrisonerCamp, Capacity, Count, EscapedCount, ExecutedCount, HasSpace, Instance, PersuadeFoodCost (+4 more)

### Community 33 - "com.unity.collections"
Cohesion: 0.06
Nodes (33): dependencies, depth, source, url, version, dependencies, depth, source (+25 more)

### Community 34 - "com.unity.modules.physics"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 35 - "CommanderSkills"
Cohesion: 0.18
Nodes (6): CommanderSkills, DefensiveStanceActive, DefensiveStanceCooldownLeft, DefensiveStanceTimeLeft, PowerStrikeArmed, PowerStrikeCooldownLeft

### Community 36 - "NurseryChamber"
Cohesion: 0.14
Nodes (11): NurseryChamber, BirthCount, Primary, Pair, First, IsAlive, Second, Dictionary (+3 more)

### Community 37 - "State"
Cohesion: 0.25
Nodes (8): State, Building, Depositing, Gathering, Idle, MovingToBuildSite, MovingToNode, ReturningToStorage

### Community 38 - "State"
Cohesion: 0.40
Nodes (5): State, Attacking, AttackMoving, Idle, MovingToTarget

### Community 39 - "GroundTelegraphSector"
Cohesion: 0.27
Nodes (6): GroundTelegraphSector, LayerMask, Mesh, MeshFilter, Quaternion, Vector3

### Community 40 - "BossConeAoE"
Cohesion: 0.29
Nodes (5): BossConeAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 41 - "BossLineAoE"
Cohesion: 0.29
Nodes (5): BossLineAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 42 - "SelectedUnitPanel"
Cohesion: 0.23
Nodes (11): SelectedUnitPanel, Button, Color, Font, GameObject, Image, RectTransform, Text (+3 more)

### Community 43 - "2026-09-02 ~ 2026-09-03 (세션 1)"
Cohesion: 0.15
Nodes (13): 2026-09-02 ~ 2026-09-03 (세션 1), MVP 수직 슬라이스 — 이번 세션 구현 완료, SIMUL-TeaamProject에서 추가 이식(보스 레이드 / 선택 시스템) — 2026-09-02, 개요, 기획서 시스템 요약, 랜덤맵 생성(MapGenerator) — 이전 팀 프로젝트에서 포팅, 보스 AoE/텔레그래프 시스템 — `Assets/Scripts/Boss/`, 설계 메모 (+5 more)

### Community 44 - "CommanderRank"
Cohesion: 0.18
Nodes (7): CommanderRank, Captain, Corporal, General, Lieutenant, Sergeant, CommanderRanks

### Community 45 - "ColonyInvasion"
Cohesion: 0.23
Nodes (4): ColonyInvasion, List, Transform, ResourceType

### Community 46 - "com.unity.modules.imgui"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, version, dependencies, depth, source, url (+14 more)

### Community 47 - "BossCircleAoE"
Cohesion: 0.29
Nodes (5): BossCircleAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 48 - "UnitRole"
Cohesion: 0.22
Nodes (7): UnitRole, Defense, Flying, Melee, Ranged, Support, Worker

### Community 49 - ".Main"
Cohesion: 0.33
Nodes (4): EnemyColonyEconomyChecks, FieldInfo, Func, Task

### Community 50 - "EnemyCommander"
Cohesion: 0.18
Nodes (10): CommanderAcquisitionChecks, PrisonerCamp, ScoutPost, Task, EnemyCommander, CommanderName, Rank, Roles (+2 more)

### Community 51 - "BossHealth"
Cohesion: 0.20
Nodes (7): BossHealth, CurrentHp, IsDead, MaxHp, Position, Vector3, UnityEvent

### Community 52 - "AttackMoveController"
Cohesion: 0.12
Nodes (12): AttackMoveController, ConsumesPointerInput, IsAttackMode, Camera, LayerMask, Vector2, IReadOnlyList, UnitSelectionController (+4 more)

### Community 53 - "CommanderAcquisitionPanel"
Cohesion: 0.13
Nodes (9): CommanderAcquisitionPanel, Camp, Feedback, PanelRect, Scout, SelectedPrisoner, Button, List (+1 more)

### Community 54 - "com.unity.ai.navigation"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, url, version, dependencies, depth, source (+3 more)

### Community 55 - ".Main"
Cohesion: 0.07
Nodes (22): RegressionChecks, FieldInfo, Func, GameObject, List, MeshFilter, NavMeshAgent, Object (+14 more)

### Community 56 - "com.unity.modules.unitywebrequest"
Cohesion: 0.05
Nodes (48): dependencies, dependencies, depth, source, version, dependencies, depth, source (+40 more)

### Community 57 - "SelectionManager"
Cohesion: 0.20
Nodes (7): SelectionManager, Camera, LayerMask, List, Vector2, GUIStyle, Rect

### Community 58 - "EnemyColony"
Cohesion: 0.19
Nodes (5): EnemyColony, IsDefeated, RemainingBuildings, List, Vector3

### Community 59 - "BuildingKind"
Cohesion: 0.18
Nodes (11): BuildingData, BuildingKind, Barracks, DigSite, Farm, Nursery, PrisonerCamp, QueenChamber (+3 more)

### Community 60 - ".Spawn"
Cohesion: 0.24
Nodes (6): MoveMarker, Collider, Color, Material, MeshRenderer, Vector3

### Community 61 - "ScoutPost"
Cohesion: 0.18
Nodes (8): ScoutPost, CurrentChance, DispatchAnts, DispatchFoodCost, FailureCount, IsDispatched, Remaining, SuccessCount

### Community 63 - "SelectableObject"
Cohesion: 0.20
Nodes (6): SelectableObject, IsSelected, Color, Renderer, Transform, Vector3

### Community 64 - "changelog.md"
Cohesion: 0.15
Nodes (12): 2026-09-03~04 (세션 2), 2026-09-05 (Codex 인수인계 / SAVE 연결 검증), 2026-09-10 SAVE — 이전 로그 이관 (2026-09-09 상태), 2026-09-11 — SAVE 연결 복구, 2026-09-11 — 이전 로그 요약 및 중단 지점, 2026-09-13 SAVE — 이전 로그(2026-09-11) 이관, 2026-09-14 SAVE — 이전 로그(2026-09-13) 이관, 2026-09-14 SAVE — 이전 로그(Support 전용 버프) 이관 (+4 more)

### Community 65 - "프로젝트 로그"
Cohesion: 0.29
Nodes (6): 2026-09-15 SAVE — 장수 획득 UI·건설 연결 완료, SAVE 결과, 검증, 다음 작업과 미정 사항, 프로젝트 구조와 규칙, 프로젝트 로그

### Community 66 - ".Main"
Cohesion: 0.10
Nodes (17): FishingChecks, FieldInfo, Func, MonoBehaviour, QueenChamber, Task, LabUpgradeChecks, BindingFlags (+9 more)

### Community 67 - "CommanderProgression"
Cohesion: 0.20
Nodes (6): CommanderProgression, ArmorBonus, AttackBonus, Level, Xp, XpToNext

### Community 68 - "com.unity.nuget.newtonsoft-json"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, url, version, dependencies, depth, source (+4 more)

### Community 69 - "com.unity.modules.ui"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 70 - ".Main"
Cohesion: 0.47
Nodes (3): EnemyColonyPlacementChecks, Func, Task

### Community 71 - "dependencies"
Cohesion: 0.10
Nodes (20): depth, source, version, dependencies, depth, source, version, dependencies (+12 more)

### Community 72 - ".CreateSelectionBoxImage"
Cohesion: 0.29
Nodes (5): Canvas, CanvasScaler, GraphicRaycaster, Image, RectTransform

### Community 74 - "2026-09-05 (세션 4)"
Cohesion: 0.29
Nodes (7): 2026-09-05 (세션 4), RTS 카메라 — 엣지스크롤 무한 패닝 버그 수정, unity-cli 관련 정리 (메모리로 이관), unity-cli 브릿지 데드락 — 이번 세션에도 2회 재발, 개요, 건물/자원노드/보스 클러스터 재배치 + NavMesh 재베이크, 일개미 수동 채집 지시 — `WorkerAnt.CommandGather` 신규

### Community 75 - "개미 소굴 RTS"
Cohesion: 0.29
Nodes (6): 개미 소굴 RTS, 게임 설명, 기술 정보, 브랜치, 조작법, 현재 구현 상태

### Community 76 - "Q: read log.md and continue task"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: read log.md and continue task, Source Nodes

### Community 77 - ".Main"
Cohesion: 0.29
Nodes (6): AcquisitionSetupChecks, PrisonerCamp, Renderer, ScoutPost, Transform, T

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
Cohesion: 0.29
Nodes (7): 2026-09-08 (세션 10 — Support 역할 완료 / Flying 이동·대공 기반 착수), 2026-09-15 — 이전 로그 요약: 장수 획득 3종과 UI 연결 중단 상태, Flying 기반 코드 착수, Support 역할 프로토타입, 검증과 문서 동기화, 비행·대공 규칙 확정, 이전 SAVE 상태 이관

### Community 83 - "2026-09-14 SAVE — 이전 로그(적 소굴 AI 경제 성장) 이관"
Cohesion: 0.50
Nodes (4): 2026-09-14 SAVE — 이전 로그(적 소굴 AI 경제 성장) 이관, SAVE 결과, 검증, 다음 작업과 미정 사항

### Community 84 - ".BakeAll"
Cohesion: 0.40
Nodes (3): MenuItem, NavMeshBakeMenu, NavMeshSurface

### Community 86 - ".Main"
Cohesion: 0.50
Nodes (3): CommanderEdgeChecks, Button, Task

### Community 87 - "2026-09-07 (세션 8 — Defense 역할 프로토타입 / 생산 활성화 수정)"
Cohesion: 0.40
Nodes (5): 2026-09-07 (세션 8 — Defense 역할 프로토타입 / 생산 활성화 수정), Defense 역할 프로토타입, 문서 동기화, 비활성 생산 개체 수정과 검증, 이전 SAVE 상태 이관

### Community 88 - "2026-09-08 (세션 9 — 건물 내구도·전멸 패배 / Flying 역할 프로토타입)"
Cohesion: 0.40
Nodes (5): 2026-09-08 (세션 9 — 건물 내구도·전멸 패배 / Flying 역할 프로토타입), Flying 역할 프로토타입, 건물 내구도와 전멸 패배, 검증과 문서 동기화, 이전 SAVE 상태 이관

### Community 89 - "2026-09-09 (세션 11 — Flying 완성 / 농사 최소 프로토타입)"
Cohesion: 0.40
Nodes (5): 2026-09-09 (세션 11 — Flying 완성 / 농사 최소 프로토타입), Flying 역할 완성, 개발 도구, 농사 최소 프로토타입, 이전 SAVE 상태 이관

### Community 90 - "com.unity.modules.physics2d"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, version, dependencies, depth, source, version (+18 more)

### Community 91 - "IDamageable"
Cohesion: 0.22
Nodes (5): Vector3, IDamageable, IsDead, Position, Vector3

### Community 92 - ".Main"
Cohesion: 0.33
Nodes (5): AcquisitionBuildingChecks, Canvas, PrisonerCamp, ScoutPost, Task

### Community 93 - ".Setup"
Cohesion: 0.24
Nodes (8): CommanderAcquisitionBootstrapper, GameObject, MenuItem, MonoScript, PrisonerCamp, ScoutPost, Transform, Vector3

### Community 94 - ".Main"
Cohesion: 0.29
Nodes (6): ActiveSkillChecks, BindingFlags, Button, MonoBehaviour, Task, Vector3

### Community 95 - "BossPatternSequenceSimple"
Cohesion: 0.29
Nodes (4): BossPatternSequenceSimple, LayerMask, Transform, Vector3

### Community 96 - "GroundTelegraphLine"
Cohesion: 0.31
Nodes (6): GroundTelegraphLine, LayerMask, Mesh, MeshFilter, Quaternion, Vector3

### Community 102 - ".CreateButton"
Cohesion: 0.31
Nodes (7): Color, Font, GameObject, Image, Text, Transform, UnityAction

### Community 104 - "BossBasicPatternLoop"
Cohesion: 0.31
Nodes (4): BossBasicPatternLoop, LayerMask, Transform, Vector3

### Community 105 - ".Main"
Cohesion: 0.43
Nodes (4): RaidChecks, Func, MonoBehaviour, Task

### Community 107 - ".SetupFarm"
Cohesion: 0.40
Nodes (4): GameObject, MenuItem, Transform, FarmPrototypeBootstrapper

## Knowledge Gaps
- **676 isolated node(s):** `IsCasting`, `IsCasting`, `IsCasting`, `CurrentHp`, `MaxHp` (+671 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 930 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `CommanderAnt` connect `CommanderAnt` to `AntColony.Core`, `BuildingConstructionSite`, `.Main`, `CommanderTraits`, `ResearchLab`, `AntUnitBase`, `WorkerAnt`, `HUDController`, `.Main`, `CommanderSkills`, `NurseryChamber`, `SelectedUnitPanel`, `CommanderRank`, `UnitRole`, `CommanderAcquisitionPanel`, `.Main`, `.Main`, `CommanderProgression`, `.Create`, `SupportChecks`, `.Main`, `IDamageable`, `.Main`, `.ConfigureCommander`, `.Main`, `.OnDisable`?**
  _High betweenness centrality (0.079) - this node is a cross-community bridge._
- **Why does `BuildingBase` connect `BuildingBase` to `BuildingPlacementController`, `AntColony.Core`, `ResourceNode`, `.Main`, `Barracks`, `ResearchLab`, `WorkerAnt`, `.Main`, `.Main`, `.Main`, `EnemyColony`, `BuildingKind`, `.GetBuildLabel`, `.Main`, `.Main`, `.Main`, `IDamageable`, `.Main`, `.Setup`, `.Main`, `.SetupFarm`?**
  _High betweenness centrality (0.051) - this node is a cross-community bridge._
- **Why does `dependencies` connect `dependencies` to `com.unity.collections`, `com.unity.modules.physics`, `com.unity.nuget.newtonsoft-json`, `com.unity.modules.ui`, `packages-lock.json`, `com.unity.modules.audio`, `dependencies`, `com.unity.modules.imgui`, `com.unity.render-pipelines.core`, `com.unity.modules.uielements`, `com.unity.modules.jsonserialize`, `com.unity.ai.navigation`, `com.unity.burst`, `com.unity.modules.unitywebrequest`, `com.unity.nuget.mono-cecil`, `com.unity.modules.physics2d`, `com.unity.test-framework`?**
  _High betweenness centrality (0.050) - this node is a cross-community bridge._
- **What connects `IsCasting`, `IsCasting`, `IsCasting` to the rest of the system?**
  _676 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `manifest.json` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._
- **Should `BuildingPlacementController` be split into smaller, more focused modules?**
  _Cohesion score 0.13012477718360071 - nodes in this community are weakly interconnected._
- **Should `dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._