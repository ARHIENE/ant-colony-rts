# Graph Report - ant  (2026-09-22)

## Corpus Check
- 140 files · ~77,777 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2528 nodes · 4765 edges · 149 communities (137 shown, 8 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 142 edges (avg confidence: 0.82)
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
- GroundTelegraphCircle
- com.unity.modules.audio
- com.unity.nuget.newtonsoft-json
- BuildingConstructionSite
- CombatRolePrototypeBootstrapper
- CommanderAnt
- com.unity.render-pipelines.core
- BossHealth
- ReadmeEditor
- dependencies
- com.unity.modules.jsonserialize
- CommanderTraits
- com.unity.burst
- ResearchLab
- IsometricCameraController
- com.unity.nuget.mono-cecil
- .Main
- com.unity.test-framework
- BuildingBase
- SelectedUnitPanel
- .Main
- .Button
- PrisonerCamp
- com.unity.bindings.openimageio
- com.unity.modules.physics
- CommanderSkills
- NurseryChamber
- State
- State
- GroundTelegraphSector
- BossConeAoE
- MonoBehaviour
- HUDController
- 2026-09-02 ~ 2026-09-03 (세션 1)
- .Main
- Barracks
- com.unity.ext.nunit
- BossCircleAoE
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
- EnemyColony
- WorldMapPanel
- .Spawn
- ScoutPost
- ColonyInvasion
- SelectableObject
- changelog.md
- 프로젝트 로그
- .SetMaterial
- ExpeditionSite
- DifficultyLevel
- com.unity.modules.uielements
- .Main
- com.unity.modules.accessibility
- GameMenuController
- HomeMapBuilder
- 2026-09-05 (세션 4)
- 개미 소굴 RTS
- Q: read log.md and continue task
- .Spawn
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
- ScienceLab
- .Main
- TransportRoute
- .Main
- BossPatternSequenceSimple
- .Main
- 프로젝트 작업 규칙
- CLAUDE.md
- WildMonster
- NewGameOptions
- .Prepare
- IDamageable
- CommanderWorkProficiency
- WorldMapManager
- .SetupFarm
- WorkerAnt
- ResourceNodeStatus
- .Setup
- AnnexedSettlement
- .ConfigureCommander
- .Main
- BuildingKind
- .Main
- CommanderRank
- UserSettings
- BossBasicPatternLoop
- .Setup
- .Main
- GameManager
- .List
- .Main
- .NewGameScreen
- EnemyCommander
- 2026-09-05 (Codex 인수인계 / SAVE 연결 검증)
- .ChangeCrew
- CommanderRoster
- CommanderAcquisitionPanel
- LocalIncursions
- .Main
- .Restore
- ConquestDisposition
- Encyclopedia
- ExpeditionState
- GameSession
- .Main
- .Validate
- SaveCatalog
- MenuTooltip
- .LateUpdate
- .Main
- .Create
- UpkeepManager
- com.unity.modules.wind
- .Main

## God Nodes (most connected - your core abstractions)
1. `CommanderAnt` - 139 edges
2. `AntColony.Core` - 75 edges
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
- `BossCircleAoE` --references--> `GroundTelegraphCircle`  [EXTRACTED]
  Assets/Scripts/Boss/AoE/BossCircleAoE.cs → Assets/Scripts/Boss/Telegraph/GroundTelegraphCircle.cs
- `BossBasicPatternLoop` --references--> `BossCircleAoE`  [EXTRACTED]
  Assets/Scripts/Boss/BossBasicPatternLoop.cs → Assets/Scripts/Boss/AoE/BossCircleAoE.cs
- `BossPatternSequenceSimple` --references--> `BossCircleAoE`  [EXTRACTED]
  Assets/Scripts/Boss/BossPatternSequenceSimple.cs → Assets/Scripts/Boss/AoE/BossCircleAoE.cs
- `BossConeAoE` --references--> `GroundTelegraphSector`  [EXTRACTED]
  Assets/Scripts/Boss/AoE/BossConeAoE.cs → Assets/Scripts/Boss/Telegraph/GroundTelegraphSector.cs

## Import Cycles
- None detected.

## Communities (149 total, 8 thin omitted)

### Community 0 - "manifest.json"
Cohesion: 0.04
Nodes (49): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+41 more)

### Community 1 - ".Main"
Cohesion: 0.16
Nodes (11): WorldMapChecks, Barracks, BindingFlags, Button, Func, NavMeshObstacle, PrisonerCamp, QueenChamber (+3 more)

### Community 2 - "BuildingPlacementController"
Cohesion: 0.12
Nodes (14): BuildingPlacementController, ConsumesPointerInput, IsPlacing, Barracks, Camera, Collider, GameObject, LayerMask (+6 more)

### Community 3 - "dependencies"
Cohesion: 0.04
Nodes (50): dependencies, com.akiojin.unity-cli-bridge, com.unity.ai.assistant, com.unity.ai.inference, com.unity.ai.navigation, com.unity.collab-proxy, com.unity.ide.rider, com.unity.ide.visualstudio (+42 more)

### Community 4 - "AntColony.Core"
Cohesion: 0.06
Nodes (18): ResourceType, Food, Soil, Special, AntColony.Boss.AoE, AntColony.Data, AntColony.Units, AntColony.Core (+10 more)

### Community 5 - "SettlementDefense"
Cohesion: 0.12
Nodes (13): SettlementDefense, Attackers, CaptureProgress, CurrentRaidInterval, Prisoners, Remaining, Status, UnderAttack (+5 more)

### Community 6 - "SoldierAnt"
Cohesion: 0.19
Nodes (7): IAirborne, IsAirborne, SoldierAnt, IsAirborne, IsFlying, LayerMask, Vector3

### Community 7 - "packages-lock.json"
Cohesion: 0.04
Nodes (44): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+36 more)

### Community 8 - "ResourceNode"
Cohesion: 0.09
Nodes (16): ResourceNode, AmountRemaining, CanGather, GatherRateMultiplier, IsDepleted, IsRaidLocked, IsRaidLoot, IsRegrowing (+8 more)

### Community 9 - "MapGenerator"
Cohesion: 0.07
Nodes (27): MonoBehaviour, SetupFullUI, GameObject, MenuItem, MapGeneratorEditor, Collider, MenuItem, SnapToTerrainMenu (+19 more)

### Community 10 - "GroundTelegraphCircle"
Cohesion: 0.36
Nodes (5): GroundTelegraphCircle, LayerMask, Mesh, MeshFilter, Vector3

### Community 11 - "com.unity.modules.audio"
Cohesion: 0.07
Nodes (31): dependencies, depth, source, version, dependencies, depth, source, version (+23 more)

### Community 12 - "com.unity.nuget.newtonsoft-json"
Cohesion: 0.06
Nodes (34): dependencies, depth, source, url, version, dependencies, depth, source (+26 more)

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
Cohesion: 0.08
Nodes (23): WorkProficiencyLootChecks, Action, BindingFlags, BoxCollider, Button, Collider, FieldInfo, Func (+15 more)

### Community 18 - "ReadmeEditor"
Cohesion: 0.12
Nodes (13): GUIStyle, ReadmeEditor, BodyStyle, ButtonStyle, HeadingStyle, LinkStyle, TitleStyle, Texture2D (+5 more)

### Community 19 - "dependencies"
Cohesion: 0.10
Nodes (22): dependencies, depth, source, version, depth, source, url, version (+14 more)

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
Cohesion: 0.15
Nodes (7): ResearchLab, IsResearching, MaxLevel, ResearchIsAttack, ResearchRemaining, Role, Target

### Community 24 - "IsometricCameraController"
Cohesion: 0.18
Nodes (7): IsometricCameraController, FocusPoint, Yaw, Bounds, Camera, Vector3, AntColony.Camera

### Community 25 - "com.unity.nuget.mono-cecil"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, version, dependencies, depth, source, url (+9 more)

### Community 26 - ".Main"
Cohesion: 0.16
Nodes (13): RegressionChecks, BoxCollider, FieldInfo, Func, GameObject, List, MeshFilter, NavMeshAgent (+5 more)

### Community 27 - "com.unity.test-framework"
Cohesion: 0.08
Nodes (27): dependencies, depth, source, url, version, dependencies, depth, source (+19 more)

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
Cohesion: 0.12
Nodes (20): Image, VerticalLayoutGroup, MenuTheme, Action, Button, Canvas, CanvasScaler, Color (+12 more)

### Community 32 - "PrisonerCamp"
Cohesion: 0.13
Nodes (14): Prisoner, PrisonerCamp, Capacity, Count, EscapedCount, EscapeTimer, ExecutedCount, HasSpace (+6 more)

### Community 33 - "com.unity.bindings.openimageio"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, url, version, dependencies, depth, source (+9 more)

### Community 34 - "com.unity.modules.physics"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 35 - "CommanderSkills"
Cohesion: 0.16
Nodes (6): CommanderSkills, DefensiveStanceActive, DefensiveStanceCooldownLeft, DefensiveStanceTimeLeft, PowerStrikeArmed, PowerStrikeCooldownLeft

### Community 36 - "NurseryChamber"
Cohesion: 0.12
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

### Community 41 - "MonoBehaviour"
Cohesion: 0.13
Nodes (14): BossLineAoE, IsCasting, IEnumerator, LayerMask, Vector3, GroundTelegraphLine, LayerMask, Mesh (+6 more)

### Community 42 - "HUDController"
Cohesion: 0.12
Nodes (18): HUDController, SelectedCommander, Barracks, Button, Canvas, CanvasScaler, EventSystem, GraphicRaycaster (+10 more)

### Community 43 - "2026-09-02 ~ 2026-09-03 (세션 1)"
Cohesion: 0.15
Nodes (13): 2026-09-02 ~ 2026-09-03 (세션 1), MVP 수직 슬라이스 — 이번 세션 구현 완료, SIMUL-TeaamProject에서 추가 이식(보스 레이드 / 선택 시스템) — 2026-09-02, 개요, 기획서 시스템 요약, 랜덤맵 생성(MapGenerator) — 이전 팀 프로젝트에서 포팅, 보스 AoE/텔레그래프 시스템 — `Assets/Scripts/Boss/`, 설계 메모 (+5 more)

### Community 44 - ".Main"
Cohesion: 0.27
Nodes (8): LabUpgradeChecks, BindingFlags, Func, GameObject, List, ResearchLab, Task, Text

### Community 45 - "Barracks"
Cohesion: 0.08
Nodes (14): Barracks, CurrentTier, IsUpgrading, MaxTier, Role, UpgradeFoodCost, UpgradeRemaining, UpgradeSoilCost (+6 more)

### Community 46 - "com.unity.ext.nunit"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, version, dependencies, depth, source, url (+4 more)

### Community 47 - "BossCircleAoE"
Cohesion: 0.25
Nodes (5): BossCircleAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 48 - "AntUnitBase"
Cohesion: 0.09
Nodes (22): ObjectPool, Dictionary, GameObject, Quaternion, Queue, Vector3, UnitData, AntUnitBase (+14 more)

### Community 49 - ".Main"
Cohesion: 0.33
Nodes (4): EnemyColonyEconomyChecks, FieldInfo, Func, Task

### Community 50 - "ExpeditionTransport"
Cohesion: 0.12
Nodes (16): ExpeditionTransport, Aircraft, Capacity, Crew, HasCargo, HomePosition, IsDepositPoint, Load (+8 more)

### Community 51 - ".Main"
Cohesion: 0.17
Nodes (10): AnnexedSettlementChecks, BindingFlags, Button, Canvas, EventSystem, Func, GraphicRaycaster, ResearchLab (+2 more)

### Community 52 - "AttackMoveController"
Cohesion: 0.14
Nodes (11): AttackMoveController, ConsumesPointerInput, IsAttackMode, Camera, LayerMask, Vector2, IReadOnlyList, UnitSelectionController (+3 more)

### Community 53 - ".Capture"
Cohesion: 0.22
Nodes (22): AffinityDto, BuildingDto, CameraDto, ColonyDto, CommanderDto, DiscoveryDto, EnemyColonyDto, MonsterDto (+14 more)

### Community 54 - "com.unity.ai.navigation"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, url, version, dependencies, depth, source (+3 more)

### Community 55 - "UnitRole"
Cohesion: 0.18
Nodes (7): UnitRole, Defense, Flying, Melee, Ranged, Support, Worker

### Community 56 - "com.unity.modules.unitywebrequest"
Cohesion: 0.05
Nodes (43): dependencies, dependencies, depth, source, version, dependencies, depth, source (+35 more)

### Community 57 - "SelectionManager"
Cohesion: 0.16
Nodes (11): SelectionManager, Camera, Canvas, CanvasScaler, GraphicRaycaster, Image, LayerMask, List (+3 more)

### Community 58 - "EnemyColony"
Cohesion: 0.14
Nodes (7): EnemyColony, Buildings, IsDefeated, RemainingBuildings, List, ResourceType, Vector3

### Community 59 - "WorldMapPanel"
Cohesion: 0.22
Nodes (11): WorldMapPanel, IsOpen, PanelRect, Button, GameObject, Image, RectTransform, Text (+3 more)

### Community 60 - ".Spawn"
Cohesion: 0.24
Nodes (6): MoveMarker, Collider, Color, Material, MeshRenderer, Vector3

### Community 61 - "ScoutPost"
Cohesion: 0.14
Nodes (9): ScoutPost, CurrentChance, DispatchAnts, DispatchedAnts, DispatchFoodCost, FailureCount, IsDispatched, Remaining (+1 more)

### Community 62 - "ColonyInvasion"
Cohesion: 0.14
Nodes (9): SetupInvasion, ColonyInvasion, ActiveRaiderCount, EconomyTimer, ScaledWaveInterval, WaveIndex, WaveTimer, List (+1 more)

### Community 63 - "SelectableObject"
Cohesion: 0.17
Nodes (6): SelectableObject, IsSelected, Color, Renderer, Transform, Vector3

### Community 64 - "changelog.md"
Cohesion: 0.12
Nodes (15): 2026-09-03~04 (세션 2), 2026-09-10 SAVE — 이전 로그 이관 (2026-09-09 상태), 2026-09-11 — SAVE 연결 복구, 2026-09-11 — 이전 로그 요약 및 중단 지점, 2026-09-13 SAVE — 이전 로그(2026-09-11) 이관, 2026-09-14 SAVE — 이전 로그(2026-09-13) 이관, 2026-09-14 SAVE — 이전 로그(Support 전용 버프) 이관, 2026-09-14 SAVE — 이전 로그(적 소굴 랜덤 배치) 이관 (+7 more)

### Community 65 - "프로젝트 로그"
Cohesion: 0.29
Nodes (6): SAVE 결과, 재개 시 해결할 문제, 진행 중 작업 — 전체 UI (미완성), 초안 보존과 재개, 프로젝트 로그, 현재 상태 — 2026-09-21

### Community 66 - ".SetMaterial"
Cohesion: 0.22
Nodes (7): SetupFishing, Collider, GameObject, Material, NavMeshObstacle, Renderer, Color

### Community 67 - "ExpeditionSite"
Cohesion: 0.07
Nodes (27): ExpeditionSite, Boss, CanResolveConquest, Cleared, Colony, Defense, Difficulty, Disposition (+19 more)

### Community 68 - "DifficultyLevel"
Cohesion: 0.12
Nodes (14): DifficultyLevel, Gentle, Harsh, Normal, DifficultyProfile, DifficultyRuntime, IntervalScale, Level (+6 more)

### Community 69 - "com.unity.modules.uielements"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 70 - ".Main"
Cohesion: 0.26
Nodes (6): SettlementDefenseChecks, Func, NavMeshAgent, Task, Text, Vector3

### Community 71 - "com.unity.modules.accessibility"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.accessibility

### Community 72 - "GameMenuController"
Cohesion: 0.19
Nodes (8): GameMenuController, BlocksInput, Instance, ScreenName, GameObject, InputField, RectTransform, Text

### Community 73 - "HomeMapBuilder"
Cohesion: 0.10
Nodes (15): QueenChamber, FishingRemaining, IsDepositPoint, ProductionRemaining, UnitData, HomeMapBuilder, CurrentWorldBounds, Instance (+7 more)

### Community 74 - "2026-09-05 (세션 4)"
Cohesion: 0.29
Nodes (7): 2026-09-05 (세션 4), RTS 카메라 — 엣지스크롤 무한 패닝 버그 수정, unity-cli 관련 정리 (메모리로 이관), unity-cli 브릿지 데드락 — 이번 세션에도 2회 재발, 개요, 건물/자원노드/보스 클러스터 재배치 + NavMesh 재베이크, 일개미 수동 채집 지시 — `WorkerAnt.CommandGather` 신규

### Community 75 - "개미 소굴 RTS"
Cohesion: 0.29
Nodes (6): 개미 소굴 RTS, 게임 설명, 기술 정보, 브랜치, 조작법, 현재 구현 상태

### Community 76 - "Q: read log.md and continue task"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: read log.md and continue task, Source Nodes

### Community 77 - ".Spawn"
Cohesion: 0.46
Nodes (4): BossLoot, MeshRenderer, ResourceType, Vector3

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

### Community 91 - "ScienceLab"
Cohesion: 0.18
Nodes (8): ScienceLab, Aircraft, Busy, Constructing, PrerequisitesMet, Remaining, SpawnPosition, Vector3

### Community 92 - ".Main"
Cohesion: 0.33
Nodes (5): AcquisitionBuildingChecks, Canvas, PrisonerCamp, ScoutPost, Task

### Community 93 - "TransportRoute"
Cohesion: 0.16
Nodes (6): Renderer, TransportRoute, Destination, IsRunning, Status, WaitSeconds

### Community 94 - ".Main"
Cohesion: 0.29
Nodes (6): ActiveSkillChecks, BindingFlags, Button, MonoBehaviour, Task, Vector3

### Community 95 - "BossPatternSequenceSimple"
Cohesion: 0.18
Nodes (5): BossPatternSequenceSimple, LayerMask, Transform, Vector3, Vector3

### Community 96 - ".Main"
Cohesion: 0.26
Nodes (6): TransportRouteChecks, Button, Func, Task, Text, Vector3

### Community 101 - "WildMonster"
Cohesion: 0.13
Nodes (9): WildMonster, CurrentHealth, InCombat, IsDead, IsFlying, Position, List, NavMeshAgent (+1 more)

### Community 102 - "NewGameOptions"
Cohesion: 0.13
Nodes (9): GameBootstrap, IEnumerator, NewGameOptions, SaveSystem, Busy, PlaySeconds, LoadSceneMode, RuntimeInitializeOnLoadMethod (+1 more)

### Community 103 - ".Prepare"
Cohesion: 0.40
Nodes (3): Task, Vector3, SaveDifficultyCapture

### Community 104 - "IDamageable"
Cohesion: 0.22
Nodes (4): IDamageable, IsDead, Position, Vector3

### Community 105 - "CommanderWorkProficiency"
Cohesion: 0.25
Nodes (4): CommanderWorkProficiency, GatherMultiplier, Level, Progress

### Community 106 - "WorldMapManager"
Cohesion: 0.11
Nodes (15): WorldMapManager, AircraftResearched, HomePosition, Instance, Researcher, SettlementNotice, Sites, Transports (+7 more)

### Community 107 - ".SetupFarm"
Cohesion: 0.40
Nodes (4): GameObject, MenuItem, Transform, FarmPrototypeBootstrapper

### Community 108 - "WorkerAnt"
Cohesion: 0.12
Nodes (8): WorkerAnt, CanStartConstruction, CarryCapacity, GatherRate, IsCarrying, IsConstructing, IsWorking, Vector3

### Community 109 - "ResourceNodeStatus"
Cohesion: 0.29
Nodes (4): ResourceNodeStatus, StatusText, Camera, GUIStyle

### Community 110 - ".Setup"
Cohesion: 0.24
Nodes (8): CommanderAcquisitionBootstrapper, GameObject, MenuItem, MonoScript, PrisonerCamp, ScoutPost, Transform, Vector3

### Community 111 - "AnnexedSettlement"
Cohesion: 0.18
Nodes (7): AnnexedSettlement, DockedTransport, Elapsed, Garrison, Site, IReadOnlyList, List

### Community 113 - ".Main"
Cohesion: 0.27
Nodes (6): FishingChecks, FieldInfo, Func, MonoBehaviour, QueenChamber, Task

### Community 114 - "BuildingKind"
Cohesion: 0.13
Nodes (14): MenuItem, DataAssetBootstrapper, BuildingData, BuildingKind, Barracks, DigSite, Farm, Nursery (+6 more)

### Community 115 - ".Main"
Cohesion: 0.27
Nodes (6): InvasionChecks, FieldInfo, Func, List, NavMeshAgent, Task

### Community 116 - "CommanderRank"
Cohesion: 0.20
Nodes (7): CommanderRank, Captain, Corporal, General, Lieutenant, Sergeant, CommanderRanks

### Community 117 - "UserSettings"
Cohesion: 0.15
Nodes (8): UserSettings, Current, Path, UserSettingsData, SaveStorage, Root, RootOverride, SavesFolder

### Community 118 - "BossBasicPatternLoop"
Cohesion: 0.21
Nodes (5): BossBasicPatternLoop, LayerMask, Transform, Vector3, Collider

### Community 119 - ".Setup"
Cohesion: 0.24
Nodes (7): WorldMapBootstrapper, GameObject, MenuItem, Object, ScienceLab, Transform, Vector3

### Community 120 - ".Main"
Cohesion: 0.18
Nodes (7): Button, InputField, ScrollRect, Task, FullUIChecks, ResourceNode, SaveValidator

### Community 121 - "GameManager"
Cohesion: 0.13
Nodes (7): GameManager, FishingUnlocked, Instance, SavedBoss, SavedDefeat, SavedLoop, List

### Community 122 - ".List"
Cohesion: 0.16
Nodes (8): EnemyColonyPlacementChecks, Func, Task, SaveSlots, SlotInfo, DisplayName, List, SlotInfo

### Community 123 - ".Main"
Cohesion: 0.29
Nodes (6): AcquisitionSetupChecks, PrisonerCamp, Renderer, ScoutPost, Transform, T

### Community 124 - ".NewGameScreen"
Cohesion: 0.17
Nodes (6): ToastManager, Count, Queue, Text, expires, message

### Community 125 - "EnemyCommander"
Cohesion: 0.29
Nodes (6): EnemyCommander, CommanderName, Rank, Roles, Traits, WasCaptured

### Community 126 - "2026-09-05 (Codex 인수인계 / SAVE 연결 검증)"
Cohesion: 0.67
Nodes (3): 2026-09-05 (Codex 인수인계 / SAVE 연결 검증), 이번 작업, 이전 log.md 요약 이관

### Community 127 - ".ChangeCrew"
Cohesion: 0.47
Nodes (3): List, IReadOnlyList, List

### Community 128 - "CommanderRoster"
Cohesion: 0.15
Nodes (9): QueenChamber, SetupCommanders, CommanderRoster, Commanders, Count, Instance, IReadOnlyList, List (+1 more)

### Community 129 - "CommanderAcquisitionPanel"
Cohesion: 0.13
Nodes (16): CommanderAcquisitionPanel, Camp, Feedback, PanelRect, Scout, SelectedPrisoner, Button, Color (+8 more)

### Community 130 - "LocalIncursions"
Cohesion: 0.31
Nodes (5): LocalIncursions, SavedTimer, Visitors, IReadOnlyList, List

### Community 131 - ".Main"
Cohesion: 0.40
Nodes (4): CommanderAcquisitionChecks, PrisonerCamp, ScoutPost, Task

### Community 132 - ".Restore"
Cohesion: 0.22
Nodes (7): SaveBuildings, List, PrisonerCamp, ScoutPost, IEnumerator, NavMeshAgent, State

### Community 133 - "ConquestDisposition"
Cohesion: 0.33
Nodes (5): ConquestDisposition, Abandoned, Annexed, Lost, Undecided

### Community 134 - "Encyclopedia"
Cohesion: 0.21
Nodes (8): Book, Encyclopedia, Entries, Path, IEnumerable, IReadOnlyList, List, Book

### Community 135 - "ExpeditionState"
Cohesion: 0.40
Nodes (5): ExpeditionState, Deployed, Home, Outbound, Returning

### Community 136 - "GameSession"
Cohesion: 0.14
Nodes (8): GameSession, Exists, GameStarted, Instance, Options, PendingLoad, PlaySeconds, SaveFileV1

### Community 137 - ".Main"
Cohesion: 0.21
Nodes (6): RaidChecks, Func, MonoBehaviour, Task, CombatTargeting, Vector3

### Community 138 - ".Validate"
Cohesion: 0.36
Nodes (3): SavePreflight, List, State

### Community 139 - "SaveCatalog"
Cohesion: 0.31
Nodes (3): SaveCatalog, Ready, Transform

### Community 140 - "MenuTooltip"
Cohesion: 0.29
Nodes (4): MenuTooltip, IPointerEnterHandler, IPointerExitHandler, PointerEventData

### Community 142 - ".Main"
Cohesion: 0.40
Nodes (3): MonoBehaviour, Task, SaveProgressionCapture

### Community 147 - "com.unity.modules.wind"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.wind

## Knowledge Gaps
- **828 isolated node(s):** `AntColony.EditorTools`, `IsCasting`, `IsCasting`, `IsCasting`, `CurrentHp` (+823 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1228 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **8 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `CommanderAnt` connect `CommanderAnt` to `CommanderRoster`, `.Main`, `.Restore`, `AntColony.Core`, `SoldierAnt`, `SettlementDefense`, `ResourceNode`, `.Main`, `BuildingConstructionSite`, `.Main`, `.LateUpdate`, `.Create`, `BossHealth`, `.OnDisable`, `UpkeepManager`, `CommanderTraits`, `ResearchLab`, `.Main`, `SelectedUnitPanel`, `.Main`, `CommanderSkills`, `NurseryChamber`, `MonoBehaviour`, `HUDController`, `.Main`, `AntUnitBase`, `ExpeditionTransport`, `.Main`, `UnitRole`, `ExpeditionSite`, `.Main`, `GameMenuController`, `SupportChecks`, `.Main`, `.Main`, `.Main`, `IDamageable`, `CommanderWorkProficiency`, `WorkerAnt`, `AnnexedSettlement`, `.ConfigureCommander`, `.Main`, `.Main`, `CommanderRank`, `.ChangeCrew`?**
  _High betweenness centrality (0.104) - this node is a cross-community bridge._
- **Why does `BuildingBase` connect `BuildingBase` to `.Main`, `BuildingPlacementController`, `AntColony.Core`, `.Restore`, `.Main`, `SaveCatalog`, `.OnDisable`, `ResearchLab`, `.Main`, `.Main`, `MonoBehaviour`, `Barracks`, `.Main`, `ExpeditionTransport`, `.Main`, `.Capture`, `UnitRole`, `EnemyColony`, `ExpeditionSite`, `.Main`, `HomeMapBuilder`, `ScienceLab`, `.Main`, `BossPatternSequenceSimple`, `.Main`, `.Prepare`, `IDamageable`, `.SetupFarm`, `WorkerAnt`, `.Setup`, `BuildingKind`, `.Main`, `.Main`, `GameManager`, `.List`, `.Main`?**
  _High betweenness centrality (0.069) - this node is a cross-community bridge._
- **Why does `MapGenerator` connect `MapGenerator` to `HomeMapBuilder`, `.Main`, `MonoBehaviour`?**
  _High betweenness centrality (0.052) - this node is a cross-community bridge._
- **What connects `AntColony.EditorTools`, `IsCasting`, `IsCasting` to the rest of the system?**
  _828 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `manifest.json` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._
- **Should `BuildingPlacementController` be split into smaller, more focused modules?**
  _Cohesion score 0.11711711711711711 - nodes in this community are weakly interconnected._
- **Should `dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._