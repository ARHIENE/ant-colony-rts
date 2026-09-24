# Graph Report - ant  (2026-09-23)

## Corpus Check
- 141 files · ~78,835 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2540 nodes · 4807 edges · 144 communities (134 shown, 6 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 145 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `41896bd8`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- manifest.json
- .Main
- BuildingPlacementController
- dependencies
- AntColony.Core
- SettlementDefense
- SoldierAnt
- packages-lock.json
- ResourceNode
- MapGenerator
- BossCircleAoE
- com.unity.modules.audio
- com.unity.dt.app-ui
- BuildingConstructionSite
- CombatRolePrototypeBootstrapper
- CommanderAnt
- com.unity.render-pipelines.core
- BossHealth
- ReadmeEditor
- com.unity.modules.accessibility
- dependencies
- CommanderTraits
- com.unity.burst
- ResearchLab
- .Prepare
- com.unity.nuget.newtonsoft-json
- .Main
- com.unity.test-framework
- BuildingBase
- SelectedUnitPanel
- .Main
- .Button
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
- HUDController
- 2026-09-02 ~ 2026-09-03 (세션 1)
- .Main
- ResourceManager
- com.unity.modules.imgui
- IsometricCameraController
- AntUnitBase
- .Main
- ExpeditionTransport
- .Main
- AttackMoveController
- .Capture
- com.unity.ai.navigation
- UnitRole
- com.unity.modules.unitywebrequest
- SelectionManager
- .Main
- ScienceLab
- .Spawn
- ScoutPost
- EnemyColony
- SelectableObject
- changelog.md
- 프로젝트 로그
- .SetMaterial
- ExpeditionSite
- DifficultyLevel
- com.unity.modules.ui
- .Setup
- BuildingKind
- GameMenuController
- HomeMapBuilder
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
- .Main
- com.unity.addressables
- .Setup
- .Main
- BossPatternSequenceSimple
- MonoBehaviour
- 프로젝트 작업 규칙
- CLAUDE.md
- WildMonster
- NewGameOptions
- Storage
- IDamageable
- CommanderWorkProficiency
- WorldMapManager
- Barracks
- WorkerAnt
- .ConfigureCommander
- com.unity.modules.uielements
- AnnexedSettlement
- .CreateSelectionBoxImage
- LocalIncursions
- QueenChamber
- .SetupFarm
- EnemyCommander
- UserSettings
- BossBasicPatternLoop
- GroundTelegraphLine
- .Main
- GameManager
- .List
- .Main
- .NewGameScreen
- .Main
- 2026-09-05 (Codex 인수인계 / SAVE 연결 검증)
- ConquestDisposition
- CommanderRoster
- CommanderAcquisitionPanel
- com.unity.profiling.core
- .Main
- UpkeepManager
- Encyclopedia
- com.unity.modules.wind
- GameSession
- .Main
- .Validate
- SaveCatalog
- MenuTooltip
- .GetTemplate
- .Main

## God Nodes (most connected - your core abstractions)
1. `CommanderAnt` - 139 edges
2. `AntColony.Core` - 76 edges
3. `ResourceNode` - 71 edges
4. `BuildingBase` - 68 edges
5. `AntColony.Data` - 61 edges
6. `ExpeditionSite` - 61 edges
7. `AntColony.Buildings` - 59 edges
8. `BuildingPlacementController` - 57 edges
9. `AntColony.Units` - 57 edges
10. `AntColony.World` - 51 edges

## Surprising Connections (you probably didn't know these)
- `WorkerAnt` --references--> `State`  [EXTRACTED]
  Assets/Scripts/Units/WorkerAnt.cs → Assets/Scripts/World/ExpeditionTransport.cs
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

## Communities (144 total, 6 thin omitted)

### Community 0 - "manifest.json"
Cohesion: 0.04
Nodes (49): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+41 more)

### Community 1 - ".Main"
Cohesion: 0.16
Nodes (11): WorldMapChecks, Barracks, BindingFlags, Button, Func, NavMeshObstacle, PrisonerCamp, QueenChamber (+3 more)

### Community 2 - "BuildingPlacementController"
Cohesion: 0.15
Nodes (8): BuildingPlacementController, ConsumesPointerInput, IsPlacing, Camera, Collider, LayerMask, Renderer, Vector3

### Community 3 - "dependencies"
Cohesion: 0.04
Nodes (50): dependencies, com.akiojin.unity-cli-bridge, com.unity.ai.assistant, com.unity.ai.inference, com.unity.ai.navigation, com.unity.collab-proxy, com.unity.ide.rider, com.unity.ide.visualstudio (+42 more)

### Community 4 - "AntColony.Core"
Cohesion: 0.06
Nodes (17): ResourceType, Food, Soil, Special, AntColony.Boss.AoE, AntColony.Data, AntColony.Units, AntColony.Core (+9 more)

### Community 5 - "SettlementDefense"
Cohesion: 0.12
Nodes (13): SettlementDefense, Attackers, CaptureProgress, CurrentRaidInterval, Prisoners, Remaining, Status, UnderAttack (+5 more)

### Community 6 - "SoldierAnt"
Cohesion: 0.19
Nodes (8): IAirborne, IsAirborne, Vector3, SoldierAnt, IsAirborne, IsFlying, LayerMask, Vector3

### Community 7 - "packages-lock.json"
Cohesion: 0.04
Nodes (44): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+36 more)

### Community 8 - "ResourceNode"
Cohesion: 0.08
Nodes (19): IEnumerator, NavMeshAgent, State, ResourceNode, AmountRemaining, CanGather, GatherRateMultiplier, IsDepleted (+11 more)

### Community 9 - "MapGenerator"
Cohesion: 0.07
Nodes (27): MonoBehaviour, SetupFullUI, GameObject, MenuItem, MapGeneratorEditor, Collider, MenuItem, SnapToTerrainMenu (+19 more)

### Community 10 - "BossCircleAoE"
Cohesion: 0.15
Nodes (10): BossCircleAoE, IsCasting, IEnumerator, LayerMask, Vector3, GroundTelegraphCircle, LayerMask, Mesh (+2 more)

### Community 11 - "com.unity.modules.audio"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, version, dependencies, depth, source, version (+13 more)

### Community 12 - "com.unity.dt.app-ui"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, url, version, dependencies, depth, source (+8 more)

### Community 13 - "BuildingConstructionSite"
Cohesion: 0.07
Nodes (17): CommanderChecks, Barracks, Func, MonoBehaviour, QueenChamber, Task, BuildingConstructionSite, BuildTimeSeconds (+9 more)

### Community 14 - "CombatRolePrototypeBootstrapper"
Cohesion: 0.22
Nodes (7): CombatRolePrototypeBootstrapper, Barracks, GameObject, MenuItem, MonoScript, ResearchLab, Transform

### Community 15 - "CommanderAnt"
Cohesion: 0.05
Nodes (40): CommanderAnt, AllowedRoles, Armor, AttackDamage, CanChangeAllocation, CanDefensiveStance, CanPowerStrike, CanStartConstruction (+32 more)

### Community 16 - "com.unity.render-pipelines.core"
Cohesion: 0.09
Nodes (25): depth, source, version, dependencies, depth, source, version, dependencies (+17 more)

### Community 17 - "BossHealth"
Cohesion: 0.05
Nodes (32): WorkProficiencyLootChecks, Action, BindingFlags, BoxCollider, Button, Collider, FieldInfo, Func (+24 more)

### Community 18 - "ReadmeEditor"
Cohesion: 0.12
Nodes (13): GUIStyle, ReadmeEditor, BodyStyle, ButtonStyle, HeadingStyle, LinkStyle, TitleStyle, Texture2D (+5 more)

### Community 19 - "com.unity.modules.accessibility"
Cohesion: 0.18
Nodes (10): dependencies, depth, source, version, dependencies, depth, source, version (+2 more)

### Community 20 - "dependencies"
Cohesion: 0.09
Nodes (26): dependencies, depth, source, version, dependencies, depth, source, version (+18 more)

### Community 21 - "CommanderTraits"
Cohesion: 0.15
Nodes (10): CommanderPersonality, Balanced, Brave, Cautious, Devoted, CommanderTraits, ArmorBonus, AttackBonus (+2 more)

### Community 22 - "com.unity.burst"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, dependencies, depth, source (+9 more)

### Community 23 - "ResearchLab"
Cohesion: 0.15
Nodes (7): ResearchLab, IsResearching, MaxLevel, ResearchIsAttack, ResearchRemaining, Role, Target

### Community 24 - ".Prepare"
Cohesion: 0.40
Nodes (3): Task, Vector3, SaveDifficultyCapture

### Community 25 - "com.unity.nuget.newtonsoft-json"
Cohesion: 0.07
Nodes (29): dependencies, depth, source, version, dependencies, depth, source, url (+21 more)

### Community 26 - ".Main"
Cohesion: 0.16
Nodes (13): RegressionChecks, BoxCollider, FieldInfo, Func, GameObject, List, MeshFilter, NavMeshAgent (+5 more)

### Community 27 - "com.unity.test-framework"
Cohesion: 0.12
Nodes (17): dependencies, dependencies, depth, source, url, version, depth, dependencies (+9 more)

### Community 28 - "BuildingBase"
Cohesion: 0.09
Nodes (16): SceneInvasionChecks, Task, SetupRaid, BuildingBase, CountsTowardPlayerDefeat, CurrentHealth, Data, IsDead (+8 more)

### Community 29 - "SelectedUnitPanel"
Cohesion: 0.25
Nodes (10): SelectedUnitPanel, Button, Color, GameObject, Image, RectTransform, Text, Transform (+2 more)

### Community 30 - ".Main"
Cohesion: 0.14
Nodes (12): CommanderProgressionChecks, BindingFlags, MonoBehaviour, Task, Vector3, CommanderProgression, ArmorBonus, AttackBonus (+4 more)

### Community 31 - ".Button"
Cohesion: 0.13
Nodes (21): Image, MenuTheme, Action, Button, Canvas, CanvasScaler, Color, GraphicRaycaster (+13 more)

### Community 32 - "PrisonerCamp"
Cohesion: 0.11
Nodes (15): Prisoner, PrisonerCamp, Capacity, Count, EscapedCount, EscapeTimer, ExecutedCount, HasSpace (+7 more)

### Community 33 - "com.unity.collections"
Cohesion: 0.10
Nodes (22): dependencies, depth, source, url, version, dependencies, depth, source (+14 more)

### Community 34 - "com.unity.modules.physics"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, version, dependencies, depth, source, version (+18 more)

### Community 35 - "CommanderSkills"
Cohesion: 0.16
Nodes (6): CommanderSkills, DefensiveStanceActive, DefensiveStanceCooldownLeft, DefensiveStanceTimeLeft, PowerStrikeArmed, PowerStrikeCooldownLeft

### Community 36 - "NurseryChamber"
Cohesion: 0.11
Nodes (11): NurseryChamber, BirthCount, Primary, Pair, First, IsAlive, Second, Dictionary (+3 more)

### Community 37 - "State"
Cohesion: 0.25
Nodes (8): State, Building, Depositing, Gathering, Idle, MovingToBuildSite, MovingToNode, ReturningToStorage

### Community 38 - "State"
Cohesion: 0.40
Nodes (5): State, Attacking, AttackMoving, Idle, MovingToTarget

### Community 39 - "GroundTelegraphSector"
Cohesion: 0.24
Nodes (6): GroundTelegraphSector, LayerMask, Mesh, MeshFilter, Quaternion, Vector3

### Community 40 - "BossConeAoE"
Cohesion: 0.29
Nodes (5): BossConeAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 41 - "BossLineAoE"
Cohesion: 0.29
Nodes (5): BossLineAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 42 - "HUDController"
Cohesion: 0.12
Nodes (19): HUDController, SelectedCommander, Barracks, Button, Canvas, CanvasScaler, EventSystem, GraphicRaycaster (+11 more)

### Community 43 - "2026-09-02 ~ 2026-09-03 (세션 1)"
Cohesion: 0.15
Nodes (13): 2026-09-02 ~ 2026-09-03 (세션 1), MVP 수직 슬라이스 — 이번 세션 구현 완료, SIMUL-TeaamProject에서 추가 이식(보스 레이드 / 선택 시스템) — 2026-09-02, 개요, 기획서 시스템 요약, 랜덤맵 생성(MapGenerator) — 이전 팀 프로젝트에서 포팅, 보스 AoE/텔레그래프 시스템 — `Assets/Scripts/Boss/`, 설계 메모 (+5 more)

### Community 44 - ".Main"
Cohesion: 0.27
Nodes (8): LabUpgradeChecks, BindingFlags, Func, GameObject, List, ResearchLab, Task, Text

### Community 45 - "ResourceManager"
Cohesion: 0.27
Nodes (3): ResourceManager, Instance, Dictionary

### Community 46 - "com.unity.modules.imgui"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, version, dependencies, depth, source, url (+14 more)

### Community 47 - "IsometricCameraController"
Cohesion: 0.18
Nodes (7): IsometricCameraController, FocusPoint, Yaw, Bounds, Camera, Vector3, AntColony.Camera

### Community 48 - "AntUnitBase"
Cohesion: 0.09
Nodes (22): ObjectPool, Dictionary, GameObject, Quaternion, Queue, Vector3, UnitData, AntUnitBase (+14 more)

### Community 49 - ".Main"
Cohesion: 0.33
Nodes (4): EnemyColonyEconomyChecks, FieldInfo, Func, Task

### Community 50 - "ExpeditionTransport"
Cohesion: 0.05
Nodes (41): WorldMapPanel, IsOpen, PanelRect, Button, GameObject, Image, List, RectTransform (+33 more)

### Community 51 - ".Main"
Cohesion: 0.17
Nodes (10): AnnexedSettlementChecks, BindingFlags, Button, Canvas, EventSystem, Func, GraphicRaycaster, ResearchLab (+2 more)

### Community 52 - "AttackMoveController"
Cohesion: 0.14
Nodes (11): AttackMoveController, ConsumesPointerInput, IsAttackMode, Camera, LayerMask, Vector2, IReadOnlyList, UnitSelectionController (+3 more)

### Community 53 - ".Capture"
Cohesion: 0.17
Nodes (25): SaveBuildings, List, PrisonerCamp, ScoutPost, AffinityDto, BuildingDto, CameraDto, ColonyDto (+17 more)

### Community 54 - "com.unity.ai.navigation"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, url, version, dependencies, depth, source (+3 more)

### Community 55 - "UnitRole"
Cohesion: 0.18
Nodes (7): UnitRole, Defense, Flying, Melee, Ranged, Support, Worker

### Community 56 - "com.unity.modules.unitywebrequest"
Cohesion: 0.06
Nodes (42): dependencies, dependencies, depth, source, version, dependencies, depth, source (+34 more)

### Community 57 - "SelectionManager"
Cohesion: 0.27
Nodes (6): SelectionManager, Camera, LayerMask, List, Vector2, Rect

### Community 58 - ".Main"
Cohesion: 0.27
Nodes (6): InvasionChecks, FieldInfo, Func, List, NavMeshAgent, Task

### Community 59 - "ScienceLab"
Cohesion: 0.18
Nodes (8): ScienceLab, Aircraft, Busy, Constructing, PrerequisitesMet, Remaining, SpawnPosition, Vector3

### Community 60 - ".Spawn"
Cohesion: 0.24
Nodes (6): MoveMarker, Collider, Color, Material, MeshRenderer, Vector3

### Community 61 - "ScoutPost"
Cohesion: 0.15
Nodes (9): ScoutPost, CurrentChance, DispatchAnts, DispatchedAnts, DispatchFoodCost, FailureCount, IsDispatched, Remaining (+1 more)

### Community 62 - "EnemyColony"
Cohesion: 0.09
Nodes (15): ColonyInvasion, ActiveRaiderCount, EconomyTimer, ScaledWaveInterval, WaveIndex, WaveTimer, List, Transform (+7 more)

### Community 63 - "SelectableObject"
Cohesion: 0.19
Nodes (6): SelectableObject, IsSelected, Color, Renderer, Transform, Vector3

### Community 64 - "changelog.md"
Cohesion: 0.12
Nodes (16): 2026-09-03~04 (세션 2), 2026-09-10 SAVE — 이전 로그 이관 (2026-09-09 상태), 2026-09-11 — SAVE 연결 복구, 2026-09-11 — 이전 로그 요약 및 중단 지점, 2026-09-13 SAVE — 이전 로그(2026-09-11) 이관, 2026-09-14 SAVE — 이전 로그(2026-09-13) 이관, 2026-09-14 SAVE — 이전 로그(Support 전용 버프) 이관, 2026-09-14 SAVE — 이전 로그(적 소굴 랜덤 배치) 이관 (+8 more)

### Community 65 - "프로젝트 로그"
Cohesion: 0.29
Nodes (6): SAVE 진행 — 2026-09-23, 검증 및 제한, 다음 작업, 전체 UI 통합 현황, 프로젝트 로그, 현재 상태 — 2026-09-23 (KST)

### Community 66 - ".SetMaterial"
Cohesion: 0.15
Nodes (9): SetupFishing, Collider, GameObject, Material, NavMeshObstacle, Renderer, SetupInvasion, Color (+1 more)

### Community 67 - "ExpeditionSite"
Cohesion: 0.07
Nodes (27): ExpeditionSite, Boss, CanResolveConquest, Cleared, Colony, Defense, Difficulty, Disposition (+19 more)

### Community 68 - "DifficultyLevel"
Cohesion: 0.12
Nodes (14): DifficultyLevel, Gentle, Harsh, Normal, DifficultyProfile, DifficultyRuntime, IntervalScale, Level (+6 more)

### Community 69 - "com.unity.modules.ui"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 70 - ".Setup"
Cohesion: 0.24
Nodes (8): CommanderAcquisitionBootstrapper, GameObject, MenuItem, MonoScript, PrisonerCamp, ScoutPost, Transform, Vector3

### Community 71 - "BuildingKind"
Cohesion: 0.13
Nodes (14): MenuItem, DataAssetBootstrapper, BuildingData, BuildingKind, Barracks, DigSite, Farm, Nursery (+6 more)

### Community 72 - "GameMenuController"
Cohesion: 0.19
Nodes (8): GameMenuController, BlocksInput, Instance, ScreenName, GameObject, InputField, RectTransform, Text

### Community 73 - "HomeMapBuilder"
Cohesion: 0.19
Nodes (10): HomeMapBuilder, CurrentWorldBounds, Instance, Rebuilt, Status, WorldBounds, Bounds, NavMeshData (+2 more)

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
Cohesion: 0.26
Nodes (6): SettlementDefenseChecks, Func, NavMeshAgent, Task, Text, Vector3

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
Cohesion: 0.27
Nodes (6): FishingChecks, FieldInfo, Func, MonoBehaviour, QueenChamber, Task

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

### Community 91 - ".Main"
Cohesion: 0.26
Nodes (6): TransportRouteChecks, Button, Func, Task, Text, Vector3

### Community 92 - "com.unity.addressables"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, depth, source, url, version (+7 more)

### Community 93 - ".Setup"
Cohesion: 0.24
Nodes (7): WorldMapBootstrapper, GameObject, MenuItem, Object, ScienceLab, Transform, Vector3

### Community 94 - ".Main"
Cohesion: 0.29
Nodes (6): ActiveSkillChecks, BindingFlags, Button, MonoBehaviour, Task, Vector3

### Community 95 - "BossPatternSequenceSimple"
Cohesion: 0.29
Nodes (4): BossPatternSequenceSimple, LayerMask, Transform, Vector3

### Community 96 - "MonoBehaviour"
Cohesion: 0.22
Nodes (7): GameBootstrap, IEnumerator, GameNotifications, Dictionary, LoadSceneMode, MonoBehaviour, Scene

### Community 101 - "WildMonster"
Cohesion: 0.12
Nodes (9): WildMonster, CurrentHealth, InCombat, IsDead, IsFlying, Position, List, NavMeshAgent (+1 more)

### Community 102 - "NewGameOptions"
Cohesion: 0.14
Nodes (8): NewGameOptions, SaveStorage, Root, RootOverride, SavesFolder, SaveSystem, Busy, PlaySeconds

### Community 104 - "IDamageable"
Cohesion: 0.14
Nodes (6): CombatTargeting, Vector3, IDamageable, IsDead, Position, Vector3

### Community 105 - "CommanderWorkProficiency"
Cohesion: 0.25
Nodes (4): CommanderWorkProficiency, GatherMultiplier, Level, Progress

### Community 106 - "WorldMapManager"
Cohesion: 0.11
Nodes (15): WorldMapManager, AircraftResearched, HomePosition, Instance, Researcher, SettlementNotice, Sites, Transports (+7 more)

### Community 107 - "Barracks"
Cohesion: 0.14
Nodes (9): Barracks, CurrentTier, IsUpgrading, MaxTier, Role, UpgradeFoodCost, UpgradeRemaining, UpgradeSoilCost (+1 more)

### Community 108 - "WorkerAnt"
Cohesion: 0.12
Nodes (8): WorkerAnt, CanStartConstruction, CarryCapacity, GatherRate, IsCarrying, IsConstructing, IsWorking, Vector3

### Community 110 - "com.unity.modules.uielements"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, url, version, depth, source, version (+7 more)

### Community 111 - "AnnexedSettlement"
Cohesion: 0.15
Nodes (8): SettlementRewardChecks, AnnexedSettlement, DockedTransport, Elapsed, Garrison, Site, IReadOnlyList, List

### Community 112 - ".CreateSelectionBoxImage"
Cohesion: 0.29
Nodes (5): Canvas, CanvasScaler, GraphicRaycaster, Image, RectTransform

### Community 113 - "LocalIncursions"
Cohesion: 0.31
Nodes (5): LocalIncursions, SavedTimer, Visitors, IReadOnlyList, List

### Community 114 - "QueenChamber"
Cohesion: 0.18
Nodes (5): QueenChamber, FishingRemaining, IsDepositPoint, ProductionRemaining, UnitData

### Community 115 - ".SetupFarm"
Cohesion: 0.40
Nodes (4): GameObject, MenuItem, Transform, FarmPrototypeBootstrapper

### Community 116 - "EnemyCommander"
Cohesion: 0.12
Nodes (13): CommanderRank, Captain, Corporal, General, Lieutenant, Sergeant, CommanderRanks, EnemyCommander (+5 more)

### Community 117 - "UserSettings"
Cohesion: 0.31
Nodes (4): UserSettings, Current, Path, UserSettingsData

### Community 118 - "BossBasicPatternLoop"
Cohesion: 0.31
Nodes (4): BossBasicPatternLoop, LayerMask, Transform, Vector3

### Community 119 - "GroundTelegraphLine"
Cohesion: 0.27
Nodes (6): GroundTelegraphLine, LayerMask, Mesh, MeshFilter, Quaternion, Vector3

### Community 120 - ".Main"
Cohesion: 0.39
Nodes (5): Button, InputField, ScrollRect, Task, FullUIChecks

### Community 121 - "GameManager"
Cohesion: 0.13
Nodes (7): GameManager, FishingUnlocked, Instance, SavedBoss, SavedDefeat, SavedLoop, List

### Community 122 - ".List"
Cohesion: 0.10
Nodes (14): AcquisitionSetupChecks, PrisonerCamp, Renderer, ScoutPost, Transform, EnemyColonyPlacementChecks, Func, Task (+6 more)

### Community 123 - ".Main"
Cohesion: 0.33
Nodes (5): AcquisitionBuildingChecks, Canvas, PrisonerCamp, ScoutPost, Task

### Community 124 - ".NewGameScreen"
Cohesion: 0.15
Nodes (7): ToastManager, Count, Queue, Text, expires, message, RuntimeInitializeOnLoadMethod

### Community 125 - ".Main"
Cohesion: 0.40
Nodes (4): CommanderAcquisitionChecks, PrisonerCamp, ScoutPost, Task

### Community 126 - "2026-09-05 (Codex 인수인계 / SAVE 연결 검증)"
Cohesion: 0.67
Nodes (3): 2026-09-05 (Codex 인수인계 / SAVE 연결 검증), 이번 작업, 이전 log.md 요약 이관

### Community 127 - "ConquestDisposition"
Cohesion: 0.33
Nodes (5): ConquestDisposition, Abandoned, Annexed, Lost, Undecided

### Community 128 - "CommanderRoster"
Cohesion: 0.14
Nodes (11): QueenChamber, SetupCommanders, CommanderRoster, Commanders, Count, Instance, IEnumerable, IReadOnlyList (+3 more)

### Community 129 - "CommanderAcquisitionPanel"
Cohesion: 0.13
Nodes (15): CommanderAcquisitionPanel, Camp, Feedback, PanelRect, Scout, SelectedPrisoner, Button, Color (+7 more)

### Community 130 - "com.unity.profiling.core"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.profiling.core

### Community 131 - ".Main"
Cohesion: 0.50
Nodes (3): CommanderEdgeChecks, Button, Task

### Community 134 - "Encyclopedia"
Cohesion: 0.22
Nodes (9): Book, Encyclopedia, Entries, Path, IEnumerable, IReadOnlyList, List, DiscoveryDto (+1 more)

### Community 135 - "com.unity.modules.wind"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.wind

### Community 136 - "GameSession"
Cohesion: 0.14
Nodes (8): GameSession, Exists, GameStarted, Instance, Options, PendingLoad, PlaySeconds, SaveFileV1

### Community 137 - ".Main"
Cohesion: 0.43
Nodes (4): RaidChecks, Func, MonoBehaviour, Task

### Community 138 - ".Validate"
Cohesion: 0.20
Nodes (5): SavePreflight, List, ResourceNode, State, SaveValidator

### Community 139 - "SaveCatalog"
Cohesion: 0.31
Nodes (3): SaveCatalog, Ready, Transform

### Community 140 - "MenuTooltip"
Cohesion: 0.19
Nodes (9): Button, RectTransform, Text, TooltipChecks, MenuTooltip, Graphic, IPointerEnterHandler, IPointerExitHandler (+1 more)

### Community 141 - ".GetTemplate"
Cohesion: 0.24
Nodes (6): Barracks, GameObject, PrisonerCamp, ResearchLab, ScienceLab, ScoutPost

### Community 142 - ".Main"
Cohesion: 0.40
Nodes (3): MonoBehaviour, Task, SaveProgressionCapture

## Knowledge Gaps
- **829 isolated node(s):** `AntColony.EditorTools`, `IsCasting`, `IsCasting`, `IsCasting`, `CurrentHp` (+824 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1232 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `CommanderAnt` connect `CommanderAnt` to `CommanderRoster`, `.Main`, `.Main`, `AntColony.Core`, `.OnDisable`, `SoldierAnt`, `UpkeepManager`, `ResourceNode`, `.Main`, `SettlementDefense`, `BuildingConstructionSite`, `.Main`, `BossHealth`, `CommanderTraits`, `ResearchLab`, `.Main`, `SelectedUnitPanel`, `.Main`, `CommanderSkills`, `NurseryChamber`, `HUDController`, `.Main`, `AntUnitBase`, `ExpeditionTransport`, `.Main`, `.Capture`, `UnitRole`, `.Main`, `ExpeditionSite`, `GameMenuController`, `.Main`, `SupportChecks`, `.Main`, `.Main`, `.Main`, `MonoBehaviour`, `IDamageable`, `CommanderWorkProficiency`, `WorkerAnt`, `.ConfigureCommander`, `AnnexedSettlement`, `EnemyCommander`?**
  _High betweenness centrality (0.121) - this node is a cross-community bridge._
- **Why does `BuildingBase` connect `BuildingBase` to `.Main`, `BuildingPlacementController`, `AntColony.Core`, `.Main`, `.Validate`, `SaveCatalog`, `.GetTemplate`, `.OnDisable`, `ResearchLab`, `.Prepare`, `.Main`, `.Main`, `.Main`, `ExpeditionTransport`, `.Main`, `.Capture`, `UnitRole`, `.Main`, `ScienceLab`, `EnemyColony`, `ExpeditionSite`, `.Setup`, `BuildingKind`, `HomeMapBuilder`, `.Main`, `.Main`, `MonoBehaviour`, `Storage`, `IDamageable`, `Barracks`, `WorkerAnt`, `QueenChamber`, `.SetupFarm`, `GameManager`, `.List`, `.Main`?**
  _High betweenness centrality (0.053) - this node is a cross-community bridge._
- **Why does `ResourceNode` connect `ResourceNode` to `.Main`, `.Main`, `AntColony.Core`, `.Main`, `.Validate`, `SaveCatalog`, `.GetTemplate`, `BossHealth`, `.Prepare`, `.Main`, `BuildingBase`, `.Main`, `ExpeditionTransport`, `.Main`, `AttackMoveController`, `.Capture`, `.Main`, `EnemyColony`, `.SetMaterial`, `ExpeditionSite`, `.Main`, `.Main`, `.Main`, `MonoBehaviour`, `WorkerAnt`, `AnnexedSettlement`, `.SetupFarm`?**
  _High betweenness centrality (0.049) - this node is a cross-community bridge._
- **What connects `AntColony.EditorTools`, `IsCasting`, `IsCasting` to the rest of the system?**
  _829 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `manifest.json` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._
- **Should `dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._
- **Should `AntColony.Core` be split into smaller, more focused modules?**
  _Cohesion score 0.060717264386989156 - nodes in this community are weakly interconnected._