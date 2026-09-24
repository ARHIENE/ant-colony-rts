# Graph Report - ant  (2026-09-24)

## Corpus Check
- 161 files · ~92,314 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2984 nodes · 5704 edges · 181 communities (170 shown, 7 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 215 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `41896bd8`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- manifest.json
- .Main
- AntColony.Core
- dependencies
- CommanderTrait
- SettlementDefense
- SoldierAnt
- packages-lock.json
- ResourceNode
- MapGenerator
- BossCircleAoE
- com.unity.modules.animation
- dependencies
- BuildingConstructionSite
- CombatRolePrototypeBootstrapper
- CommanderAnt
- com.unity.render-pipelines.core
- BossHealth
- WorldMapPanel
- dependencies
- com.unity.modules.jsonserialize
- CommanderTraits
- com.unity.burst
- ResearchLab
- .Apply
- com.unity.nuget.mono-cecil
- .Main
- com.unity.test-framework
- BuildingBase
- SelectedUnitPanel
- .Main
- .Rect
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
- BuildingPlacementController
- 2026-09-02 ~ 2026-09-03 (세션 1)
- .Main
- Barracks
- com.unity.ext.nunit
- IsometricCameraController
- AntUnitBase
- .Main
- ExpeditionTransport
- .Main
- MonoBehaviour
- .Capture
- com.unity.ai.navigation
- DataAssetBootstrapper
- dependencies
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
- com.unity.modules.imgui
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
- ColonyInvasion
- 2026-09-07 (세션 8 — Defense 역할 프로토타입 / 생산 활성화 수정)
- 2026-09-08 (세션 9 — 건물 내구도·전멸 패배 / Flying 역할 프로토타입)
- 2026-09-09 (세션 11 — Flying 완성 / 농사 최소 프로토타입)
- com.unity.modules.physics2d
- AntVisual
- .Main
- .Setup
- .Main
- BossPatternSequenceSimple
- .FinishLoad
- 프로젝트 작업 규칙
- CLAUDE.md
- WildMonster
- .Main
- HUDController
- IDamageable
- CommanderWorkProficiency
- WorldMapManager
- UnitRole
- WorkerAnt
- .ConfigureCommander
- com.unity.modules.uielements
- AnnexedSettlement
- ScienceTechnology
- LocalIncursions
- QueenChamber
- UnitData
- CommanderRank
- .TryLoad
- BossBasicPatternLoop
- CommanderPersonalState
- .Main
- GameManager
- .List
- AirshipYard
- .Show
- TransportRoute
- 2026-09-05 (Codex 인수인계 / SAVE 연결 검증)
- com.unity.modules.unitywebrequest
- CommanderRoster
- CommanderAcquisitionPanel
- GroundTelegraphCircle
- .Main
- .CreateButton
- CampaignResearch
- Encyclopedia
- .Setup
- GameSession
- .Main
- .Validate
- SaveCatalog
- MenuTooltip
- .GetTemplate
- .Main
- ReadmeEditor
- .Main
- .Main
- .Spawn
- ResourceNodeStatus
- .Main
- EquipmentItem
- .BuildCanvas
- .Main
- CommanderActivity
- .Main
- .Main
- BetaProgress
- .Main
- .Prepare
- com.unity.modules.imageconversion
- .Main
- .Main
- EnemyCommander
- GameCalendar
- MentalBreak
- ExpeditionState
- .Main
- .Restore
- com.unity.nuget.newtonsoft-json
- .TryPlace
- .EvacuateCargo
- ObjectPool
- CommanderProgression
- .Main
- .SetupFarm
- .ChangeCrew
- ConquestDisposition
- DigSite
- CommanderPersonality
- .Awake

## God Nodes (most connected - your core abstractions)
1. `CommanderAnt` - 179 edges
2. `AntColony.Core` - 91 edges
3. `BuildingBase` - 76 edges
4. `ResourceNode` - 73 edges
5. `AntColony.Buildings` - 71 edges
6. `AntColony.Data` - 70 edges
7. `AntColony.Units` - 70 edges
8. `BuildingPlacementController` - 68 edges
9. `ExpeditionSite` - 62 edges
10. `AntColony.World` - 60 edges

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

## Communities (181 total, 7 thin omitted)

### Community 0 - "manifest.json"
Cohesion: 0.04
Nodes (49): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+41 more)

### Community 1 - ".Main"
Cohesion: 0.16
Nodes (11): WorldMapChecks, Barracks, BindingFlags, Button, Func, NavMeshObstacle, PrisonerCamp, QueenChamber (+3 more)

### Community 2 - "AntColony.Core"
Cohesion: 0.05
Nodes (17): ResourceType, Food, Soil, Special, AntColony.Boss.AoE, AntColony.Data, AntColony.Units, AntColony.Core (+9 more)

### Community 3 - "dependencies"
Cohesion: 0.04
Nodes (50): dependencies, com.akiojin.unity-cli-bridge, com.unity.ai.assistant, com.unity.ai.inference, com.unity.ai.navigation, com.unity.collab-proxy, com.unity.ide.rider, com.unity.ide.visualstudio (+42 more)

### Community 4 - "CommanderTrait"
Cohesion: 0.05
Nodes (38): CommanderTrait, Ambitious, Ascetic, Bloodthirsty, Brave, Cautious, Cheerful, ColdBlooded (+30 more)

### Community 5 - "SettlementDefense"
Cohesion: 0.15
Nodes (10): SettlementDefense, Attackers, CaptureProgress, CurrentRaidInterval, Prisoners, Remaining, Status, UnderAttack (+2 more)

### Community 6 - "SoldierAnt"
Cohesion: 0.13
Nodes (11): IAirborne, IsAirborne, Vector3, QueenChamber, SoldierAnt, IsAirborne, IsFlying, MovementSpeed (+3 more)

### Community 7 - "packages-lock.json"
Cohesion: 0.04
Nodes (44): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+36 more)

### Community 8 - "ResourceNode"
Cohesion: 0.09
Nodes (16): ResourceNode, AmountRemaining, CanGather, GatherRateMultiplier, IsDepleted, IsRaidLocked, IsRaidLoot, IsRegrowing (+8 more)

### Community 9 - "MapGenerator"
Cohesion: 0.07
Nodes (27): MonoBehaviour, SetupFullUI, GameObject, MenuItem, MapGeneratorEditor, Collider, MenuItem, SnapToTerrainMenu (+19 more)

### Community 10 - "BossCircleAoE"
Cohesion: 0.29
Nodes (5): BossCircleAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 11 - "com.unity.modules.animation"
Cohesion: 0.13
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 12 - "dependencies"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, url, version, dependencies, depth, source (+14 more)

### Community 13 - "BuildingConstructionSite"
Cohesion: 0.06
Nodes (22): AcquisitionBuildingChecks, Canvas, PrisonerCamp, ScoutPost, Task, CommanderChecks, Barracks, Func (+14 more)

### Community 14 - "CombatRolePrototypeBootstrapper"
Cohesion: 0.22
Nodes (7): CombatRolePrototypeBootstrapper, Barracks, GameObject, MenuItem, MonoScript, ResearchLab, Transform

### Community 15 - "CommanderAnt"
Cohesion: 0.04
Nodes (48): CommanderAnt, AllowedRoles, Armor, AttackDamage, CanChangeAllocation, CanDefensiveStance, CanPowerStrike, CanStartConstruction (+40 more)

### Community 16 - "com.unity.render-pipelines.core"
Cohesion: 0.09
Nodes (25): depth, source, version, dependencies, depth, source, version, dependencies (+17 more)

### Community 17 - "BossHealth"
Cohesion: 0.08
Nodes (23): WorkProficiencyLootChecks, Action, BindingFlags, BoxCollider, Button, Collider, FieldInfo, Func (+15 more)

### Community 18 - "WorldMapPanel"
Cohesion: 0.22
Nodes (11): WorldMapPanel, IsOpen, PanelRect, Button, GameObject, Image, RectTransform, Text (+3 more)

### Community 19 - "dependencies"
Cohesion: 0.10
Nodes (20): depth, source, version, dependencies, depth, source, version, dependencies (+12 more)

### Community 20 - "com.unity.modules.jsonserialize"
Cohesion: 0.08
Nodes (25): dependencies, depth, source, version, dependencies, depth, source, version (+17 more)

### Community 21 - "CommanderTraits"
Cohesion: 0.12
Nodes (12): CommanderTraits, ArmorBonus, AttackBonus, BaseMood, FoodMultiplier, LearningMultiplier, Loyalty, MoveMultiplier (+4 more)

### Community 22 - "com.unity.burst"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, url, version, dependencies, depth, source (+14 more)

### Community 23 - "ResearchLab"
Cohesion: 0.15
Nodes (7): ResearchLab, IsResearching, MaxLevel, ResearchIsAttack, ResearchRemaining, Role, Target

### Community 24 - ".Apply"
Cohesion: 0.33
Nodes (4): UserSettings, Current, Path, UserSettingsData

### Community 25 - "com.unity.nuget.mono-cecil"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, version, dependencies, depth, source, url (+9 more)

### Community 26 - ".Main"
Cohesion: 0.16
Nodes (13): RegressionChecks, BoxCollider, FieldInfo, Func, GameObject, List, MeshFilter, NavMeshAgent (+5 more)

### Community 27 - "com.unity.test-framework"
Cohesion: 0.12
Nodes (17): dependencies, dependencies, depth, source, url, version, depth, dependencies (+9 more)

### Community 28 - "BuildingBase"
Cohesion: 0.11
Nodes (13): SceneInvasionChecks, Task, SetupRaid, BuildingBase, CountsTowardPlayerDefeat, CurrentHealth, Data, IsDead (+5 more)

### Community 29 - "SelectedUnitPanel"
Cohesion: 0.20
Nodes (11): SelectedUnitPanel, Button, Color, GameObject, Image, RectTransform, Text, Transform (+3 more)

### Community 30 - ".Main"
Cohesion: 0.27
Nodes (6): CommanderProgressionChecks, BindingFlags, MonoBehaviour, Task, Vector3, MethodInfo

### Community 31 - ".Rect"
Cohesion: 0.14
Nodes (17): Image, MenuTheme, Button, Canvas, CanvasScaler, Color, GraphicRaycaster, Image (+9 more)

### Community 32 - "PrisonerCamp"
Cohesion: 0.12
Nodes (14): Prisoner, PrisonerCamp, Capacity, Count, EscapedCount, EscapeTimer, ExecutedCount, HasSpace (+6 more)

### Community 33 - "com.unity.collections"
Cohesion: 0.06
Nodes (33): dependencies, depth, source, url, version, dependencies, depth, source (+25 more)

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
Cohesion: 0.31
Nodes (6): GroundTelegraphSector, LayerMask, Mesh, MeshFilter, Quaternion, Vector3

### Community 40 - "BossConeAoE"
Cohesion: 0.26
Nodes (5): BossConeAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 41 - "BossLineAoE"
Cohesion: 0.16
Nodes (11): BossLineAoE, IsCasting, IEnumerator, LayerMask, Vector3, GroundTelegraphLine, LayerMask, Mesh (+3 more)

### Community 42 - "BuildingPlacementController"
Cohesion: 0.16
Nodes (5): BuildingPlacementController, ConsumesPointerInput, IsPlacing, Camera, LayerMask

### Community 43 - "2026-09-02 ~ 2026-09-03 (세션 1)"
Cohesion: 0.15
Nodes (13): 2026-09-02 ~ 2026-09-03 (세션 1), MVP 수직 슬라이스 — 이번 세션 구현 완료, SIMUL-TeaamProject에서 추가 이식(보스 레이드 / 선택 시스템) — 2026-09-02, 개요, 기획서 시스템 요약, 랜덤맵 생성(MapGenerator) — 이전 팀 프로젝트에서 포팅, 보스 AoE/텔레그래프 시스템 — `Assets/Scripts/Boss/`, 설계 메모 (+5 more)

### Community 44 - ".Main"
Cohesion: 0.27
Nodes (8): LabUpgradeChecks, BindingFlags, Func, GameObject, List, ResearchLab, Task, Text

### Community 45 - "Barracks"
Cohesion: 0.05
Nodes (20): AcidTower, AttackInterval, Cooldown, Damage, Range, LineRenderer, Barracks, CurrentTier (+12 more)

### Community 46 - "com.unity.ext.nunit"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, version, dependencies, depth, source, url (+4 more)

### Community 47 - "IsometricCameraController"
Cohesion: 0.18
Nodes (7): IsometricCameraController, FocusPoint, Yaw, Bounds, Camera, Vector3, AntColony.Camera

### Community 48 - "AntUnitBase"
Cohesion: 0.15
Nodes (12): AntUnitBase, Agent, Armor, AttackDamage, CurrentHealth, Data, IsDead, Position (+4 more)

### Community 49 - ".Main"
Cohesion: 0.33
Nodes (4): EnemyColonyEconomyChecks, FieldInfo, Func, Task

### Community 50 - "ExpeditionTransport"
Cohesion: 0.10
Nodes (17): ExpeditionTransport, Aircraft, BlueprintCargo, Capacity, Crew, EquipmentCargo, HasCargo, HomePosition (+9 more)

### Community 51 - ".Main"
Cohesion: 0.17
Nodes (10): AnnexedSettlementChecks, BindingFlags, Button, Canvas, EventSystem, Func, GraphicRaycaster, ResearchLab (+2 more)

### Community 52 - "MonoBehaviour"
Cohesion: 0.14
Nodes (12): UpkeepManager, ConsecutiveFailures, SavedTimer, AttackMoveController, ConsumesPointerInput, IsAttackMode, Camera, LayerMask (+4 more)

### Community 53 - ".Capture"
Cohesion: 0.18
Nodes (26): AffinityDto, BuildingDto, CameraDto, ColonyDto, CommanderDto, EnemyColonyDto, MonsterDto, OptionsDto (+18 more)

### Community 54 - "com.unity.ai.navigation"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, url, version, dependencies, depth, source (+3 more)

### Community 56 - "dependencies"
Cohesion: 0.09
Nodes (23): dependencies, dependencies, depth, source, version, dependencies, depth, source (+15 more)

### Community 57 - "SelectionManager"
Cohesion: 0.16
Nodes (11): SelectionManager, Camera, Canvas, CanvasScaler, GraphicRaycaster, Image, LayerMask, List (+3 more)

### Community 58 - ".Main"
Cohesion: 0.27
Nodes (6): InvasionChecks, FieldInfo, Func, List, NavMeshAgent, Task

### Community 59 - "ScienceLab"
Cohesion: 0.12
Nodes (10): ScienceLab, Aircraft, Busy, Constructing, PrerequisitesMet, Remaining, SpawnPosition, Target (+2 more)

### Community 60 - ".Spawn"
Cohesion: 0.24
Nodes (6): MoveMarker, Collider, Color, Material, MeshRenderer, Vector3

### Community 61 - "ScoutPost"
Cohesion: 0.15
Nodes (9): ScoutPost, CurrentChance, DispatchAnts, DispatchedAnts, DispatchFoodCost, FailureCount, IsDispatched, Remaining (+1 more)

### Community 62 - "EnemyColony"
Cohesion: 0.16
Nodes (6): EnemyColony, Buildings, IsDefeated, RemainingBuildings, List, Vector3

### Community 63 - "SelectableObject"
Cohesion: 0.19
Nodes (6): SelectableObject, IsSelected, Color, Renderer, Transform, Vector3

### Community 64 - "changelog.md"
Cohesion: 0.11
Nodes (17): 2026-09-03~04 (세션 2), 2026-09-10 SAVE — 이전 로그 이관 (2026-09-09 상태), 2026-09-11 — SAVE 연결 복구, 2026-09-11 — 이전 로그 요약 및 중단 지점, 2026-09-13 SAVE — 이전 로그(2026-09-11) 이관, 2026-09-14 SAVE — 이전 로그(2026-09-13) 이관, 2026-09-14 SAVE — 이전 로그(Support 전용 버프) 이관, 2026-09-14 SAVE — 이전 로그(적 소굴 랜덤 배치) 이관 (+9 more)

### Community 65 - "프로젝트 로그"
Cohesion: 0.29
Nodes (6): SAVE 진행 — 2026-09-24, 검증 및 제한, 구현 상태, 다음 작업, 프로젝트 로그, 현재 상태 — 2026-09-24 (KST)

### Community 66 - ".SetMaterial"
Cohesion: 0.15
Nodes (9): SetupFishing, Collider, GameObject, Material, NavMeshObstacle, Renderer, SetupInvasion, Color (+1 more)

### Community 67 - "ExpeditionSite"
Cohesion: 0.07
Nodes (28): ExpeditionSite, Boss, CanResolveConquest, Cleared, Colony, Defense, Difficulty, Disposition (+20 more)

### Community 68 - "DifficultyLevel"
Cohesion: 0.09
Nodes (20): CommanderDeathMode, Gentle, Harsh, Normal, CommanderDeathRuntime, Mode, DifficultyLevel, Gentle (+12 more)

### Community 69 - "com.unity.modules.imgui"
Cohesion: 0.08
Nodes (27): dependencies, depth, source, version, dependencies, depth, source, version (+19 more)

### Community 70 - ".Setup"
Cohesion: 0.24
Nodes (8): CommanderAcquisitionBootstrapper, GameObject, MenuItem, MonoScript, PrisonerCamp, ScoutPost, Transform, Vector3

### Community 71 - "BuildingKind"
Cohesion: 0.13
Nodes (14): BuildingData, BuildingKind, AcidTower, AirshipYard, Barracks, DigSite, Farm, Nursery (+6 more)

### Community 72 - "GameMenuController"
Cohesion: 0.15
Nodes (12): GameMenuController, BlocksInput, Instance, ScreenName, Button, GameObject, InputField, RectTransform (+4 more)

### Community 73 - "HomeMapBuilder"
Cohesion: 0.18
Nodes (11): HomeMapBuilder, CurrentWorldBounds, Instance, Rebuilt, Status, WorldBounds, Bounds, NavMeshAgent (+3 more)

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

### Community 86 - "ColonyInvasion"
Cohesion: 0.13
Nodes (9): ColonyInvasion, ActiveRaiderCount, EconomyTimer, ScaledWaveInterval, WaveIndex, WaveTimer, List, Transform (+1 more)

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
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 91 - "AntVisual"
Cohesion: 0.17
Nodes (8): AntVisual, StateName, Animator, GameObject, LineRenderer, Material, MeshRenderer, Vector3

### Community 92 - ".Main"
Cohesion: 0.18
Nodes (9): PlayableLoopChecks, Barracks, BindingFlags, Button, Func, QueenChamber, Storage, Task (+1 more)

### Community 93 - ".Setup"
Cohesion: 0.24
Nodes (7): WorldMapBootstrapper, GameObject, MenuItem, Object, ScienceLab, Transform, Vector3

### Community 94 - ".Main"
Cohesion: 0.29
Nodes (6): ActiveSkillChecks, BindingFlags, Button, MonoBehaviour, Task, Vector3

### Community 95 - "BossPatternSequenceSimple"
Cohesion: 0.29
Nodes (4): BossPatternSequenceSimple, LayerMask, Transform, Vector3

### Community 96 - ".FinishLoad"
Cohesion: 0.22
Nodes (5): GameBootstrap, IEnumerator, LoadSceneMode, RuntimeInitializeOnLoadMethod, Scene

### Community 101 - "WildMonster"
Cohesion: 0.13
Nodes (9): WildMonster, CurrentHealth, InCombat, IsDead, IsFlying, Position, List, NavMeshAgent (+1 more)

### Community 102 - ".Main"
Cohesion: 0.22
Nodes (8): AcidTowerChecks, AcidTower, BindingFlags, Button, LineRenderer, NavMeshAgent, NavMeshObstacle, Task

### Community 103 - "HUDController"
Cohesion: 0.18
Nodes (6): HUDController, SelectedCommander, Barracks, QueenChamber, ResearchLab, DigSite

### Community 104 - "IDamageable"
Cohesion: 0.13
Nodes (7): CombatTargeting, IDamageable, IsDead, Position, Vector3, Vector2, Vector2

### Community 105 - "CommanderWorkProficiency"
Cohesion: 0.14
Nodes (7): CommanderWorkProficiency, CraftLevel, GatherMultiplier, Level, Progress, ResearchLevel, ResearchMultiplier

### Community 106 - "WorldMapManager"
Cohesion: 0.11
Nodes (15): WorldMapManager, AircraftResearched, HomePosition, Instance, Researcher, SettlementNotice, Sites, Transports (+7 more)

### Community 107 - "UnitRole"
Cohesion: 0.29
Nodes (7): UnitRole, Defense, Flying, Melee, Ranged, Support, Worker

### Community 108 - "WorkerAnt"
Cohesion: 0.13
Nodes (11): WorkerAnt, CanStartConstruction, CarryCapacity, CurrentResourceNode, GatherRate, IsBuildingAnimation, IsCarrying, IsConstructing (+3 more)

### Community 109 - ".ConfigureCommander"
Cohesion: 0.14
Nodes (7): IEnumerable, List, TrinketEffect, Command, Gather, Mood, Move

### Community 110 - "com.unity.modules.uielements"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, url, version, depth, source, version (+7 more)

### Community 111 - "AnnexedSettlement"
Cohesion: 0.18
Nodes (7): AnnexedSettlement, DockedTransport, Elapsed, Garrison, Site, IReadOnlyList, List

### Community 112 - "ScienceTechnology"
Cohesion: 0.06
Nodes (31): ScienceTechnology, AcidRefining, AdvancedCrops, AdvancedWeapons, Aircraft, ArmorPlates, Blades, Cocoons (+23 more)

### Community 113 - "LocalIncursions"
Cohesion: 0.31
Nodes (5): LocalIncursions, SavedTimer, Visitors, IReadOnlyList, List

### Community 114 - "QueenChamber"
Cohesion: 0.18
Nodes (5): QueenChamber, FishingRemaining, IsDepositPoint, ProductionRemaining, UnitData

### Community 115 - "UnitData"
Cohesion: 0.17
Nodes (8): UnitData, GameObject, GameObject, Texture2D, Readme, Section, ScriptableObject, Section

### Community 116 - "CommanderRank"
Cohesion: 0.22
Nodes (7): CommanderRank, Captain, Corporal, General, Lieutenant, Sergeant, CommanderRanks

### Community 117 - ".TryLoad"
Cohesion: 0.14
Nodes (8): ResourceNode, SaveStorage, Root, RootOverride, SavesFolder, SaveSystem, Busy, PlaySeconds

### Community 118 - "BossBasicPatternLoop"
Cohesion: 0.20
Nodes (5): BossBasicPatternLoop, LayerMask, Transform, Vector3, Collider

### Community 119 - "CommanderPersonalState"
Cohesion: 0.09
Nodes (18): CommanderInjury, CommanderPersonalState, HasSeriousInjury, HasTreatableInjury, CommanderRelation, InjuryPart, Antenna, Head (+10 more)

### Community 120 - ".Main"
Cohesion: 0.21
Nodes (7): FoundationChecks, Button, InputField, ScrollRect, Task, FullUIChecks, SaveValidator

### Community 121 - "GameManager"
Cohesion: 0.12
Nodes (8): GameManager, FishingUnlocked, Instance, SavedBoss, SavedDefeat, SavedLoop, List, Vector3

### Community 122 - ".List"
Cohesion: 0.24
Nodes (5): SaveSlots, SlotInfo, DisplayName, List, SlotInfo

### Community 123 - "AirshipYard"
Cohesion: 0.12
Nodes (15): AirshipPart, Cocoon, Engine, Hull, AirshipYard, Cocoons, Engine, Hull (+7 more)

### Community 124 - ".Show"
Cohesion: 0.18
Nodes (7): ToastManager, Count, GraphicRaycaster, Queue, Text, expires, message

### Community 125 - "TransportRoute"
Cohesion: 0.16
Nodes (6): Renderer, TransportRoute, Destination, IsRunning, Status, WaitSeconds

### Community 126 - "2026-09-05 (Codex 인수인계 / SAVE 연결 검증)"
Cohesion: 0.67
Nodes (3): 2026-09-05 (Codex 인수인계 / SAVE 연결 검증), 이번 작업, 이전 log.md 요약 이관

### Community 127 - "com.unity.modules.unitywebrequest"
Cohesion: 0.09
Nodes (25): dependencies, depth, source, version, dependencies, depth, source, version (+17 more)

### Community 128 - "CommanderRoster"
Cohesion: 0.15
Nodes (11): QueenChamber, SetupCommanders, CommanderRoster, Commanders, Count, Instance, IEnumerable, IReadOnlyList (+3 more)

### Community 129 - "CommanderAcquisitionPanel"
Cohesion: 0.13
Nodes (10): CommanderAcquisitionPanel, Camp, Feedback, PanelRect, Scout, SelectedPrisoner, Button, IReadOnlyList (+2 more)

### Community 130 - "GroundTelegraphCircle"
Cohesion: 0.32
Nodes (5): GroundTelegraphCircle, LayerMask, Mesh, MeshFilter, Vector3

### Community 131 - ".Main"
Cohesion: 0.50
Nodes (3): CommanderEdgeChecks, Button, Task

### Community 132 - ".CreateButton"
Cohesion: 0.31
Nodes (7): Color, GameObject, Image, Text, Transform, UnityAction, Font

### Community 133 - "CampaignResearch"
Cohesion: 0.11
Nodes (14): CampaignResearch, Active, Departed, EndingGameSeconds, HasBlueprint, Instance, LeftBehind, Passengers (+6 more)

### Community 134 - "Encyclopedia"
Cohesion: 0.20
Nodes (11): Book, Encyclopedia, Entries, Path, IEnumerable, IReadOnlyList, List, DiscoveryDto (+3 more)

### Community 135 - ".Setup"
Cohesion: 0.50
Nodes (3): StorageBootstrapper, MenuItem, Storage

### Community 136 - "GameSession"
Cohesion: 0.13
Nodes (9): GameSession, Exists, GameSeconds, GameStarted, Instance, Options, PendingLoad, PlaySeconds (+1 more)

### Community 137 - ".Main"
Cohesion: 0.43
Nodes (4): RaidChecks, Func, MonoBehaviour, Task

### Community 138 - ".Validate"
Cohesion: 0.38
Nodes (3): SavePreflight, List, State

### Community 139 - "SaveCatalog"
Cohesion: 0.24
Nodes (3): SaveCatalog, Ready, Transform

### Community 140 - "MenuTooltip"
Cohesion: 0.19
Nodes (9): Button, RectTransform, Text, TooltipChecks, MenuTooltip, Graphic, IPointerEnterHandler, IPointerExitHandler (+1 more)

### Community 141 - ".GetTemplate"
Cohesion: 0.19
Nodes (10): AcidTower, AirshipYard, Barracks, GameObject, NavMeshObstacle, PrisonerCamp, ResearchLab, ScienceLab (+2 more)

### Community 142 - ".Main"
Cohesion: 0.40
Nodes (3): MonoBehaviour, Task, SaveProgressionCapture

### Community 143 - "ReadmeEditor"
Cohesion: 0.18
Nodes (8): GUIStyle, ReadmeEditor, BodyStyle, ButtonStyle, HeadingStyle, LinkStyle, TitleStyle, GUIContent

### Community 144 - ".Main"
Cohesion: 0.29
Nodes (6): AcquisitionSetupChecks, PrisonerCamp, Renderer, ScoutPost, Transform, T

### Community 145 - ".Main"
Cohesion: 0.25
Nodes (7): AcidTower, Collider, LineRenderer, Material, NavMeshObstacle, Renderer, SetupAcidTower

### Community 146 - ".Spawn"
Cohesion: 0.39
Nodes (5): BossLoot, MeshRenderer, ResourceType, Vector3, MaterialPropertyBlock

### Community 147 - "ResourceNodeStatus"
Cohesion: 0.29
Nodes (4): ResourceNodeStatus, StatusText, Camera, GUIStyle

### Community 148 - ".Main"
Cohesion: 0.47
Nodes (3): EnemyColonyPlacementChecks, Func, Task

### Community 149 - "EquipmentItem"
Cohesion: 0.17
Nodes (10): EquipmentInventory, Instance, EquipmentItem, IsValid, Label, EquipmentSlot, Armor, Trinket (+2 more)

### Community 150 - ".BuildCanvas"
Cohesion: 0.19
Nodes (12): Button, Canvas, CanvasScaler, EventSystem, GraphicRaycaster, Image, RectTransform, Text (+4 more)

### Community 152 - "CommanderActivity"
Cohesion: 0.14
Nodes (13): CommanderActivity, Building, Crafting, Defense, Farming, Fishing, Flying, Gathering (+5 more)

### Community 153 - ".Main"
Cohesion: 0.27
Nodes (6): FishingChecks, FieldInfo, Func, MonoBehaviour, QueenChamber, Task

### Community 154 - ".Main"
Cohesion: 0.26
Nodes (6): TransportRouteChecks, Button, Func, Task, Text, Vector3

### Community 155 - "BetaProgress"
Cohesion: 0.20
Nodes (6): BetaProgress, CurrentObjective, Barracks, Image, ScienceLab, Text

### Community 157 - ".Main"
Cohesion: 0.19
Nodes (8): BetaChecks, Animator, Button, Collider, MeshRenderer, SkinnedMeshRenderer, Task, NewGameOptions

### Community 158 - ".Prepare"
Cohesion: 0.40
Nodes (3): Task, Vector3, SaveDifficultyCapture

### Community 159 - "com.unity.modules.imageconversion"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 160 - ".Main"
Cohesion: 0.22
Nodes (7): Animator, Collider, GameObject, Material, SkinnedMeshRenderer, SetupQuirkyAnt, Rigidbody

### Community 162 - "EnemyCommander"
Cohesion: 0.18
Nodes (10): CommanderAcquisitionChecks, PrisonerCamp, ScoutPost, Task, EnemyCommander, CommanderName, Rank, Roles (+2 more)

### Community 163 - "GameCalendar"
Cohesion: 0.15
Nodes (13): GameCalendar, CurrentSeason, GameSeconds, Label, Month, MonthProgress, TotalMonths, Year (+5 more)

### Community 164 - "MentalBreak"
Cohesion: 0.14
Nodes (8): MentalBreak, AttackBuilding, AttackCommander, Binge, Flee, Idle, None, SelfHarm

### Community 165 - "ExpeditionState"
Cohesion: 0.40
Nodes (5): ExpeditionState, Deployed, Home, Outbound, Returning

### Community 166 - ".Main"
Cohesion: 0.35
Nodes (5): CampaignChecks, AirshipYard, ScienceLab, Task, Vector3

### Community 167 - ".Restore"
Cohesion: 0.18
Nodes (7): SaveBuildings, List, PrisonerCamp, ScoutPost, IEnumerator, NavMeshAgent, State

### Community 168 - "com.unity.nuget.newtonsoft-json"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, url, version, dependencies, depth, source (+4 more)

### Community 169 - ".TryPlace"
Cohesion: 0.36
Nodes (3): Collider, Renderer, Vector3

### Community 171 - "ObjectPool"
Cohesion: 0.31
Nodes (6): ObjectPool, Dictionary, GameObject, Quaternion, Queue, Vector3

### Community 172 - "CommanderProgression"
Cohesion: 0.28
Nodes (6): CommanderProgression, ArmorBonus, AttackBonus, Level, Xp, XpToNext

### Community 173 - ".Main"
Cohesion: 0.47
Nodes (4): AntWorkVisualChecks, Animator, Func, Task

### Community 174 - ".SetupFarm"
Cohesion: 0.40
Nodes (4): GameObject, MenuItem, Transform, FarmPrototypeBootstrapper

### Community 175 - ".ChangeCrew"
Cohesion: 0.47
Nodes (3): List, IReadOnlyList, List

### Community 176 - "ConquestDisposition"
Cohesion: 0.33
Nodes (5): ConquestDisposition, Abandoned, Annexed, Lost, Undecided

### Community 177 - "DigSite"
Cohesion: 0.40
Nodes (3): DigSite, IsExpanded, GameObject

### Community 178 - "CommanderPersonality"
Cohesion: 0.40
Nodes (5): CommanderPersonality, Balanced, Brave, Cautious, Devoted

### Community 179 - ".Awake"
Cohesion: 0.50
Nodes (3): Collider, Renderer, Vector3

## Knowledge Gaps
- **1004 isolated node(s):** `IsCasting`, `IsCasting`, `IsCasting`, `CurrentHp`, `MaxHp` (+999 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1478 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `CommanderAnt` connect `CommanderAnt` to `CommanderRoster`, `.Main`, `AntColony.Core`, `.Main`, `CampaignResearch`, `Encyclopedia`, `SoldierAnt`, `SettlementDefense`, `.Main`, `BuildingConstructionSite`, `.Main`, `BossHealth`, `CommanderTraits`, `EquipmentItem`, `ResearchLab`, `CommanderActivity`, `.Main`, `.Main`, `.Main`, `SelectedUnitPanel`, `.Main`, `CommanderSkills`, `NurseryChamber`, `MentalBreak`, `.Main`, `.Restore`, `.EvacuateCargo`, `.Main`, `CommanderProgression`, `.ChangeCrew`, `ExpeditionTransport`, `.Main`, `.Capture`, `.Main`, `ScienceLab`, `ExpeditionSite`, `GameMenuController`, `.Main`, `SupportChecks`, `AntVisual`, `.Main`, `.Main`, `HUDController`, `IDamageable`, `CommanderWorkProficiency`, `UnitRole`, `WorkerAnt`, `.ConfigureCommander`, `AnnexedSettlement`, `UnitData`, `CommanderRank`, `CommanderPersonalState`, `AirshipYard`?**
  _High betweenness centrality (0.117) - this node is a cross-community bridge._
- **Why does `BuildingBase` connect `BuildingBase` to `.Main`, `AntColony.Core`, `SoldierAnt`, `.Main`, `SaveCatalog`, `BuildingConstructionSite`, `.Main`, `.Main`, `ResearchLab`, `.Main`, `.Main`, `.GetBuildLabel`, `.Main`, `.Prepare`, `.Main`, `.Restore`, `.TryPlace`, `BuildingPlacementController`, `.EvacuateCargo`, `Barracks`, `.SetupFarm`, `.Main`, `DigSite`, `.Main`, `MonoBehaviour`, `.Capture`, `ExpeditionTransport`, `.Main`, `ScienceLab`, `EnemyColony`, `ExpeditionSite`, `.Setup`, `BuildingKind`, `HomeMapBuilder`, `.Main`, `.Main`, `IDamageable`, `WorkerAnt`, `QueenChamber`, `GameManager`, `AirshipYard`?**
  _High betweenness centrality (0.070) - this node is a cross-community bridge._
- **Why does `CommanderTraits` connect `CommanderTraits` to `PrisonerCamp`, `CommanderRoster`, `EnemyCommander`, `CommanderTrait`, `.Main`, `.Validate`, `BuildingConstructionSite`, `CommanderAnt`, `CommanderPersonality`, `CommanderRank`, `.Capture`, `CommanderActivity`?**
  _High betweenness centrality (0.045) - this node is a cross-community bridge._
- **What connects `IsCasting`, `IsCasting`, `IsCasting` to the rest of the system?**
  _1004 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `manifest.json` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._
- **Should `AntColony.Core` be split into smaller, more focused modules?**
  _Cohesion score 0.05425688976377953 - nodes in this community are weakly interconnected._
- **Should `dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._