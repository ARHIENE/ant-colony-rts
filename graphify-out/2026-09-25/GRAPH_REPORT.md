# Graph Report - ant  (2026-09-25)

## Corpus Check
- 174 files · ~102,577 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 3151 nodes · 6106 edges · 186 communities (172 shown, 11 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 253 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `99b4c1be`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- manifest.json
- .Main
- AntColony.Units
- dependencies
- CommanderTrait
- SettlementDefense
- SoldierAnt
- packages-lock.json
- ResourceNode
- MapGenerator
- com.unity.modules.unitywebrequest
- com.unity.modules.animation
- com.unity.nuget.newtonsoft-json
- BuildingConstructionSite
- CombatRolePrototypeBootstrapper
- CommanderAnt
- com.unity.render-pipelines.core
- BossHealth
- WorldMapPanel
- com.unity.addressables
- dependencies
- CommanderTraits
- com.unity.modules.jsonserialize
- ResearchLab
- IsometricCameraController
- com.unity.nuget.mono-cecil
- 구현 작업 지시서 — 2026-09-25
- com.unity.test-framework
- .Main
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
- EnemyColony
- AntColony.Core
- BuildingPlacementController
- 2026-09-02 ~ 2026-09-03 (세션 1)
- .Main
- ResourceManager
- com.unity.ext.nunit
- HUDController
- AntUnitBase
- .Main
- ExpeditionTransport
- .Main
- AttackMoveController
- .Capture
- com.unity.ai.navigation
- GameManager
- dependencies
- SelectionManager
- .Main
- ScienceLab
- .Spawn
- ScoutPost
- MentalBreak
- GameAction
- changelog.md
- 프로젝트 로그
- .SetMaterial
- ExpeditionSite
- HomeMapBuilder
- com.unity.modules.ui
- .Setup
- BuildingKind
- GameMenuController
- AntColony.Buildings
- 2026-09-05 (세션 4)
- 개미 소굴 RTS
- Q: read log.md and continue task
- AntColony.Boss
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
- .Prepare
- .Main
- .SnapAll
- .Main
- BossPatternSequenceSimple
- AntVisual
- 프로젝트 작업 규칙
- CLAUDE.md
- WildMonster
- .Main
- com.unity.modules.imageconversion
- Infirmary
- SaveCatalog
- WorldMapManager
- .Main
- WorkerAnt
- .Main
- com.unity.modules.uielements
- AnnexedSettlement
- ScienceTechnology
- BossConeAoE
- QueenChamber
- BuildingBase
- CommanderRoster
- NewGameOptions
- GroundTelegraphLine
- CommanderInjury
- EnemyCommander
- CommanderWorkProficiency
- .List
- AirshipYard
- MonoBehaviour
- TransportRoute
- 2026-09-05 (Codex 인수인계 / SAVE 연결 검증)
- .Apply
- ResourceType
- .Main
- BossCircleAoE
- UpkeepManager
- Barracks
- CampaignResearch
- Encyclopedia
- CommanderPersonalState
- GameSession
- .Main
- .ForScene
- CommanderActivity
- MenuTooltip
- .GetTemplate
- CommanderAcquisitionPanel
- ReadmeEditor
- .Main
- .SetupFarm
- .Spawn
- .Main
- .Main
- EquipmentItem
- ResourceNodeStatus
- IDamageable
- .Prepare
- Storage
- BetaProgress
- UnitRole
- .Main
- UnitData
- .Setup
- .Main
- AcidTower
- Readme
- GameCalendar
- BossLineAoE
- .Main
- .Main
- BossBasicPatternLoop
- com.unity.modules.accessibility
- CommanderTalents
- ObjectPool
- WorldMapBootstrapper.cs
- .WriteAtomic
- LocalIncursions
- .Main
- .LateUpdate
- .CreateSelectionBoxImage
- .Main
- .Main
- AirshipPart
- .IndexOf
- .Main
- CommanderPersonality
- .Main
- .GenerateTexture
- .Restore

## God Nodes (most connected - your core abstractions)
1. `CommanderAnt` - 200 edges
2. `AntColony.Core` - 101 edges
3. `AntColony.Units` - 79 edges
4. `BuildingBase` - 77 edges
5. `AntColony.Buildings` - 76 edges
6. `AntColony.Data` - 76 edges
7. `ResourceNode` - 74 edges
8. `BuildingPlacementController` - 71 edges
9. `AntColony.World` - 62 edges
10. `ExpeditionSite` - 62 edges

## Surprising Connections (you probably didn't know these)
- `SaveFeatureCapture` --references--> `CommanderAnt`  [EXTRACTED]
  AgentScripts/SaveFeatureCapture.cs → Assets/Scripts/Units/CommanderAnt.cs
- `StorageResearchChecks` --references--> `ResourceType`  [EXTRACTED]
  AgentScripts/StorageResearchChecks.cs → Assets/Scripts/Data/ResourceType.cs
- `BossBasicPatternLoop` --references--> `BossCircleAoE`  [EXTRACTED]
  Assets/Scripts/Boss/BossBasicPatternLoop.cs → Assets/Scripts/Boss/AoE/BossCircleAoE.cs
- `BossPatternSequenceSimple` --references--> `BossCircleAoE`  [EXTRACTED]
  Assets/Scripts/Boss/BossPatternSequenceSimple.cs → Assets/Scripts/Boss/AoE/BossCircleAoE.cs
- `BossConeAoE` --references--> `GroundTelegraphSector`  [EXTRACTED]
  Assets/Scripts/Boss/AoE/BossConeAoE.cs → Assets/Scripts/Boss/Telegraph/GroundTelegraphSector.cs

## Import Cycles
- None detected.

## Communities (186 total, 11 thin omitted)

### Community 0 - "manifest.json"
Cohesion: 0.04
Nodes (49): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+41 more)

### Community 1 - ".Main"
Cohesion: 0.16
Nodes (11): WorldMapChecks, Barracks, BindingFlags, Button, Func, NavMeshObstacle, PrisonerCamp, QueenChamber (+3 more)

### Community 3 - "dependencies"
Cohesion: 0.04
Nodes (50): dependencies, com.akiojin.unity-cli-bridge, com.unity.ai.assistant, com.unity.ai.inference, com.unity.ai.navigation, com.unity.collab-proxy, com.unity.ide.rider, com.unity.ide.visualstudio (+42 more)

### Community 4 - "CommanderTrait"
Cohesion: 0.05
Nodes (38): CommanderTrait, Ambitious, Ascetic, Bloodthirsty, Brave, Cautious, Cheerful, ColdBlooded (+30 more)

### Community 5 - "SettlementDefense"
Cohesion: 0.11
Nodes (14): SettlementDefense, Attackers, CaptureProgress, CurrentRaidInterval, Prisoners, Remaining, Status, UnderAttack (+6 more)

### Community 6 - "SoldierAnt"
Cohesion: 0.14
Nodes (12): IAirborne, IsAirborne, QueenChamber, SoldierAnt, IsAirborne, IsFlying, IsInCombat, MovementSpeed (+4 more)

### Community 7 - "packages-lock.json"
Cohesion: 0.04
Nodes (44): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+36 more)

### Community 8 - "ResourceNode"
Cohesion: 0.10
Nodes (16): ResourceNode, AmountRemaining, CanGather, GatherRateMultiplier, IsDepleted, IsRaidLocked, IsRaidLoot, IsRegrowing (+8 more)

### Community 9 - "MapGenerator"
Cohesion: 0.15
Nodes (15): MapGenerator, BaseXSize, BaseZSize, SpawnObject, GameObject, List, Material, Mesh (+7 more)

### Community 10 - "com.unity.modules.unitywebrequest"
Cohesion: 0.09
Nodes (25): dependencies, depth, source, version, dependencies, depth, source, version (+17 more)

### Community 11 - "com.unity.modules.animation"
Cohesion: 0.13
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 12 - "com.unity.nuget.newtonsoft-json"
Cohesion: 0.06
Nodes (34): dependencies, depth, source, url, version, dependencies, depth, source (+26 more)

### Community 13 - "BuildingConstructionSite"
Cohesion: 0.06
Nodes (22): AcquisitionBuildingChecks, Canvas, PrisonerCamp, ScoutPost, Task, CommanderChecks, Barracks, Func (+14 more)

### Community 14 - "CombatRolePrototypeBootstrapper"
Cohesion: 0.22
Nodes (7): CombatRolePrototypeBootstrapper, Barracks, GameObject, MenuItem, MonoScript, ResearchLab, Transform

### Community 15 - "CommanderAnt"
Cohesion: 0.03
Nodes (55): CommanderAnt, Armor, AttackDamage, CanChangeAllocation, CanDefensiveStance, CanPowerStrike, CanStartConstruction, Captor (+47 more)

### Community 16 - "com.unity.render-pipelines.core"
Cohesion: 0.09
Nodes (25): depth, source, version, dependencies, depth, source, version, dependencies (+17 more)

### Community 17 - "BossHealth"
Cohesion: 0.06
Nodes (31): WorkProficiencyLootChecks, Action, BindingFlags, BoxCollider, Button, Collider, FieldInfo, Func (+23 more)

### Community 18 - "WorldMapPanel"
Cohesion: 0.22
Nodes (11): WorldMapPanel, IsOpen, PanelRect, Button, GameObject, Image, RectTransform, Text (+3 more)

### Community 19 - "com.unity.addressables"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, depth, source, url, version (+7 more)

### Community 20 - "dependencies"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, version, dependencies, depth, source, version (+18 more)

### Community 21 - "CommanderTraits"
Cohesion: 0.12
Nodes (12): CommanderTraits, ArmorBonus, AttackBonus, BaseMood, FoodMultiplier, LearningMultiplier, Loyalty, MoveMultiplier (+4 more)

### Community 22 - "com.unity.modules.jsonserialize"
Cohesion: 0.12
Nodes (16): dependencies, dependencies, depth, source, version, dependencies, depth, source (+8 more)

### Community 23 - "ResearchLab"
Cohesion: 0.15
Nodes (7): ResearchLab, IsResearching, MaxLevel, ResearchIsAttack, ResearchRemaining, Role, Target

### Community 24 - "IsometricCameraController"
Cohesion: 0.18
Nodes (7): IsometricCameraController, FocusPoint, Yaw, Bounds, Camera, Vector3, AntColony.Camera

### Community 25 - "com.unity.nuget.mono-cecil"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, version, dependencies, depth, source, url (+9 more)

### Community 26 - "구현 작업 지시서 — 2026-09-25"
Cohesion: 0.09
Nodes (21): 1단계 — 기획과 다른 곳 맞추기, 2단계 — 과학 30기술 효과 연결, 3단계 — 공방 제작 + 장비 보관함, 4단계 — 랜덤 이벤트 + 이벤트 로그, 5단계 — 충성심 사건·이탈, 사회관계, 무기 스킬, 사건 특성, 6단계 — 외교·교역·반란 세력, 7단계 — UI 마감, AI 판단 (+13 more)

### Community 27 - "com.unity.test-framework"
Cohesion: 0.10
Nodes (22): depth, source, url, version, dependencies, dependencies, depth, source (+14 more)

### Community 28 - ".Main"
Cohesion: 0.16
Nodes (13): RegressionChecks, BoxCollider, FieldInfo, Func, GameObject, List, MeshFilter, NavMeshAgent (+5 more)

### Community 29 - "SelectedUnitPanel"
Cohesion: 0.22
Nodes (11): SelectedUnitPanel, Button, Color, GameObject, Image, RectTransform, Text, Transform (+3 more)

### Community 30 - ".Main"
Cohesion: 0.14
Nodes (12): CommanderProgressionChecks, BindingFlags, MonoBehaviour, Task, Vector3, CommanderProgression, ArmorBonus, AttackBonus (+4 more)

### Community 31 - ".Button"
Cohesion: 0.13
Nodes (21): Image, MenuTheme, Action, Button, Canvas, CanvasScaler, Color, GraphicRaycaster (+13 more)

### Community 32 - "PrisonerCamp"
Cohesion: 0.12
Nodes (14): Prisoner, PrisonerCamp, Capacity, Count, EscapedCount, EscapeTimer, ExecutedCount, HasSpace (+6 more)

### Community 33 - "com.unity.collections"
Cohesion: 0.10
Nodes (22): dependencies, depth, source, url, version, dependencies, depth, source (+14 more)

### Community 34 - "com.unity.modules.physics"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, version, dependencies, depth, source, version (+18 more)

### Community 35 - "CommanderSkills"
Cohesion: 0.15
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

### Community 40 - "EnemyColony"
Cohesion: 0.13
Nodes (7): EnemyColony, Buildings, IsDefeated, RemainingBuildings, List, ResourceType, Vector3

### Community 41 - "AntColony.Core"
Cohesion: 0.09
Nodes (4): GameBalance, AntColony.Core, AntColony.Save, AntColony.UI

### Community 42 - "BuildingPlacementController"
Cohesion: 0.13
Nodes (8): BuildingPlacementController, ConsumesPointerInput, IsPlacing, Camera, Collider, LayerMask, Renderer, Vector3

### Community 43 - "2026-09-02 ~ 2026-09-03 (세션 1)"
Cohesion: 0.15
Nodes (13): 2026-09-02 ~ 2026-09-03 (세션 1), MVP 수직 슬라이스 — 이번 세션 구현 완료, SIMUL-TeaamProject에서 추가 이식(보스 레이드 / 선택 시스템) — 2026-09-02, 개요, 기획서 시스템 요약, 랜덤맵 생성(MapGenerator) — 이전 팀 프로젝트에서 포팅, 보스 AoE/텔레그래프 시스템 — `Assets/Scripts/Boss/`, 설계 메모 (+5 more)

### Community 44 - ".Main"
Cohesion: 0.27
Nodes (8): LabUpgradeChecks, BindingFlags, Func, GameObject, List, ResearchLab, Task, Text

### Community 45 - "ResourceManager"
Cohesion: 0.27
Nodes (3): ResourceManager, Instance, Dictionary

### Community 46 - "com.unity.ext.nunit"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, version, dependencies, depth, source, url (+4 more)

### Community 47 - "HUDController"
Cohesion: 0.10
Nodes (19): HUDController, SelectedCommander, Barracks, Button, Canvas, CanvasScaler, EventSystem, GraphicRaycaster (+11 more)

### Community 48 - "AntUnitBase"
Cohesion: 0.14
Nodes (12): AntUnitBase, Agent, Armor, AttackDamage, CurrentHealth, Data, IsDead, Position (+4 more)

### Community 49 - ".Main"
Cohesion: 0.33
Nodes (4): EnemyColonyEconomyChecks, FieldInfo, Func, Task

### Community 50 - "ExpeditionTransport"
Cohesion: 0.07
Nodes (30): List, ExpeditionState, Deployed, Home, Outbound, Returning, ExpeditionTransport, Aircraft (+22 more)

### Community 51 - ".Main"
Cohesion: 0.17
Nodes (10): AnnexedSettlementChecks, BindingFlags, Button, Canvas, EventSystem, Func, GraphicRaycaster, ResearchLab (+2 more)

### Community 52 - "AttackMoveController"
Cohesion: 0.20
Nodes (8): AttackMoveController, ConsumesPointerInput, IsAttackMode, Camera, LayerMask, UnitSelectionController, Camera, LayerMask

### Community 53 - ".Capture"
Cohesion: 0.13
Nodes (31): SaveBuildings, List, PrisonerCamp, ScoutPost, AffinityDto, BuildingDto, CameraDto, ColonyDto (+23 more)

### Community 54 - "com.unity.ai.navigation"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, url, version, dependencies, depth, source (+3 more)

### Community 55 - "GameManager"
Cohesion: 0.12
Nodes (8): GameManager, FishingUnlocked, Instance, SavedBoss, SavedDefeat, SavedLoop, List, Vector3

### Community 56 - "dependencies"
Cohesion: 0.09
Nodes (23): dependencies, dependencies, depth, source, version, dependencies, depth, source (+15 more)

### Community 57 - "SelectionManager"
Cohesion: 0.13
Nodes (12): SelectableObject, IsSelected, Color, Renderer, Transform, Vector3, SelectionManager, Camera (+4 more)

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

### Community 62 - "MentalBreak"
Cohesion: 0.25
Nodes (8): MentalBreak, AttackBuilding, AttackCommander, Binge, Flee, Idle, None, SelfHarm

### Community 63 - "GameAction"
Cohesion: 0.14
Nodes (16): GameAction, AddTroop, CycleWeapon, Diplomacy, EventLog, Pause, RemoveTroop, Roster (+8 more)

### Community 64 - "changelog.md"
Cohesion: 0.10
Nodes (19): 2026-09-03~04 (세션 2), 2026-09-10 SAVE — 이전 로그 이관 (2026-09-09 상태), 2026-09-11 — SAVE 연결 복구, 2026-09-11 — 이전 로그 요약 및 중단 지점, 2026-09-13 SAVE — 이전 로그(2026-09-11) 이관, 2026-09-14 SAVE — 이전 로그(2026-09-13) 이관, 2026-09-14 SAVE — 이전 로그(Support 전용 버프) 이관, 2026-09-14 SAVE — 이전 로그(적 소굴 랜덤 배치) 이관 (+11 more)

### Community 65 - "프로젝트 로그"
Cohesion: 0.33
Nodes (5): 검증, 이번 작업, 제한 및 다음 작업, 프로젝트 로그, 현재 상태 — 2026-09-25 (KST)

### Community 66 - ".SetMaterial"
Cohesion: 0.15
Nodes (9): SetupFishing, Collider, GameObject, Material, NavMeshObstacle, Renderer, SetupInvasion, Color (+1 more)

### Community 67 - "ExpeditionSite"
Cohesion: 0.07
Nodes (28): ExpeditionSite, Boss, CanResolveConquest, Cleared, Colony, Defense, Difficulty, Disposition (+20 more)

### Community 68 - "HomeMapBuilder"
Cohesion: 0.06
Nodes (33): CommanderDeathMode, Gentle, Harsh, Normal, CommanderDeathRuntime, Mode, DifficultyLevel, Gentle (+25 more)

### Community 69 - "com.unity.modules.ui"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, version, dependencies, depth, source, version (+3 more)

### Community 70 - ".Setup"
Cohesion: 0.24
Nodes (8): CommanderAcquisitionBootstrapper, GameObject, MenuItem, MonoScript, PrisonerCamp, ScoutPost, Transform, Vector3

### Community 71 - "BuildingKind"
Cohesion: 0.07
Nodes (24): AcidTower, Collider, LineRenderer, Material, NavMeshObstacle, Renderer, SetupAcidTower, MenuItem (+16 more)

### Community 72 - "GameMenuController"
Cohesion: 0.13
Nodes (11): GameHotkeys, GameMenuController, Infirmary, BlocksInput, Instance, ScreenName, Button, GameObject (+3 more)

### Community 73 - "AntColony.Buildings"
Cohesion: 0.12
Nodes (3): AntColony.World, AntColony.Buildings, AntColony.Regression

### Community 74 - "2026-09-05 (세션 4)"
Cohesion: 0.29
Nodes (7): 2026-09-05 (세션 4), RTS 카메라 — 엣지스크롤 무한 패닝 버그 수정, unity-cli 관련 정리 (메모리로 이관), unity-cli 브릿지 데드락 — 이번 세션에도 2회 재발, 개요, 건물/자원노드/보스 클러스터 재배치 + NavMesh 재베이크, 일개미 수동 채집 지시 — `WorkerAnt.CommandGather` 신규

### Community 75 - "개미 소굴 RTS"
Cohesion: 0.25
Nodes (7): 개미 소굴 RTS, 게임 설명, 과학 기술 효과 적용 범위, 기술 정보, 브랜치, 조작법, 현재 구현 상태

### Community 76 - "Q: read log.md and continue task"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: read log.md and continue task, Source Nodes

### Community 77 - "AntColony.Boss"
Cohesion: 0.10
Nodes (4): AntColony.Boss.AoE, AntColony.Boss.Telegraph, AntColony.Map, AntColony.Boss

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
Cohesion: 0.14
Nodes (8): ColonyInvasion, ActiveRaiderCount, EconomyTimer, ScaledWaveInterval, WaveIndex, WaveTimer, List, Transform

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

### Community 91 - ".Prepare"
Cohesion: 0.39
Nodes (4): Infirmary, ScrollRect, Task, SaveFeatureCapture

### Community 92 - ".Main"
Cohesion: 0.18
Nodes (9): PlayableLoopChecks, Barracks, BindingFlags, Button, Func, QueenChamber, Storage, Task (+1 more)

### Community 93 - ".SnapAll"
Cohesion: 0.19
Nodes (7): GameObject, MenuItem, MapGeneratorEditor, Collider, MenuItem, SnapToTerrainMenu, Editor

### Community 94 - ".Main"
Cohesion: 0.29
Nodes (6): ActiveSkillChecks, BindingFlags, Button, MonoBehaviour, Task, Vector3

### Community 95 - "BossPatternSequenceSimple"
Cohesion: 0.25
Nodes (4): BossPatternSequenceSimple, LayerMask, Transform, Vector3

### Community 96 - "AntVisual"
Cohesion: 0.18
Nodes (7): AntVisual, StateName, Animator, LineRenderer, Material, MeshRenderer, Vector3

### Community 101 - "WildMonster"
Cohesion: 0.13
Nodes (10): GameObject, WildMonster, CurrentHealth, InCombat, IsDead, IsFlying, Position, List (+2 more)

### Community 102 - ".Main"
Cohesion: 0.24
Nodes (8): AcidTowerChecks, AcidTower, BindingFlags, Button, LineRenderer, NavMeshAgent, NavMeshObstacle, Task

### Community 103 - "com.unity.modules.imageconversion"
Cohesion: 0.10
Nodes (20): dependencies, depth, source, version, dependencies, depth, source, version (+12 more)

### Community 104 - "Infirmary"
Cohesion: 0.24
Nodes (5): Infirmary, Patients, Unlocked, IReadOnlyList, List

### Community 105 - "SaveCatalog"
Cohesion: 0.28
Nodes (3): SaveCatalog, Ready, Transform

### Community 106 - "WorldMapManager"
Cohesion: 0.11
Nodes (15): WorldMapManager, AircraftResearched, HomePosition, Instance, Researcher, SettlementNotice, Sites, Transports (+7 more)

### Community 107 - ".Main"
Cohesion: 0.29
Nodes (7): BetaChecks, Animator, Button, Collider, MeshRenderer, SkinnedMeshRenderer, Task

### Community 108 - "WorkerAnt"
Cohesion: 0.08
Nodes (14): Vector2, WorkerAnt, CanStartConstruction, CarryCapacity, CurrentResourceNode, GatherRate, IsBuildingAnimation, IsCarrying (+6 more)

### Community 109 - ".Main"
Cohesion: 0.36
Nodes (4): Button, Infirmary, Task, InfirmaryChecks

### Community 110 - "com.unity.modules.uielements"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, url, version, depth, source, version (+7 more)

### Community 111 - "AnnexedSettlement"
Cohesion: 0.12
Nodes (12): AnnexedSettlement, DockedTransport, Elapsed, Garrison, Site, IReadOnlyList, List, ConquestDisposition (+4 more)

### Community 112 - "ScienceTechnology"
Cohesion: 0.06
Nodes (31): ScienceTechnology, AcidRefining, AdvancedCrops, AdvancedWeapons, Aircraft, ArmorPlates, Blades, Cocoons (+23 more)

### Community 113 - "BossConeAoE"
Cohesion: 0.29
Nodes (5): BossConeAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 114 - "QueenChamber"
Cohesion: 0.18
Nodes (5): QueenChamber, FishingRemaining, IsDepositPoint, ProductionRemaining, UnitData

### Community 115 - "BuildingBase"
Cohesion: 0.09
Nodes (16): SceneInvasionChecks, Task, SetupRaid, BuildingBase, CountsTowardPlayerDefeat, CurrentHealth, Data, IsDead (+8 more)

### Community 116 - "CommanderRoster"
Cohesion: 0.14
Nodes (11): QueenChamber, SetupCommanders, CommanderRoster, Commanders, Count, Instance, IEnumerable, IReadOnlyList (+3 more)

### Community 117 - "NewGameOptions"
Cohesion: 0.16
Nodes (9): ScienceLab, Storage, Task, StorageResearchChecks, IEnumerator, NewGameOptions, SaveSystem, Busy (+1 more)

### Community 118 - "GroundTelegraphLine"
Cohesion: 0.27
Nodes (6): GroundTelegraphLine, LayerMask, Mesh, MeshFilter, Quaternion, Vector3

### Community 119 - "CommanderInjury"
Cohesion: 0.16
Nodes (12): CommanderInjury, InjuryPart, Antenna, Head, Legs, Mandible, Thorax, Wings (+4 more)

### Community 120 - "EnemyCommander"
Cohesion: 0.18
Nodes (11): AntWorkVisualChecks, Animator, Func, Task, EnemyCommander, CommanderName, Rank, Roles (+3 more)

### Community 121 - "CommanderWorkProficiency"
Cohesion: 0.17
Nodes (7): CommanderWorkProficiency, CraftLevel, GatherMultiplier, Level, Progress, ResearchLevel, ResearchMultiplier

### Community 122 - ".List"
Cohesion: 0.13
Nodes (8): FoundationChecks, CommanderMigration, SaveSlots, SlotInfo, DisplayName, List, SaveValidator, SlotInfo

### Community 123 - "AirshipYard"
Cohesion: 0.16
Nodes (11): AirshipYard, Cocoons, Engine, Hull, Passengers, Ready, Remaining, State (+3 more)

### Community 124 - "MonoBehaviour"
Cohesion: 0.15
Nodes (9): GameBootstrap, ToastManager, Count, Queue, Text, expires, message, MonoBehaviour (+1 more)

### Community 125 - "TransportRoute"
Cohesion: 0.16
Nodes (6): Renderer, TransportRoute, Destination, IsRunning, Status, WaitSeconds

### Community 126 - "2026-09-05 (Codex 인수인계 / SAVE 연결 검증)"
Cohesion: 0.67
Nodes (3): 2026-09-05 (Codex 인수인계 / SAVE 연결 검증), 이번 작업, 이전 log.md 요약 이관

### Community 127 - ".Apply"
Cohesion: 0.24
Nodes (5): UserSettings, Current, Path, UserSettingsData, List

### Community 128 - "ResourceType"
Cohesion: 0.17
Nodes (4): ResourceType, Food, Soil, Special

### Community 129 - ".Main"
Cohesion: 0.27
Nodes (6): FishingChecks, FieldInfo, Func, MonoBehaviour, QueenChamber, Task

### Community 130 - "BossCircleAoE"
Cohesion: 0.16
Nodes (10): BossCircleAoE, IsCasting, IEnumerator, LayerMask, Vector3, GroundTelegraphCircle, LayerMask, Mesh (+2 more)

### Community 131 - "UpkeepManager"
Cohesion: 0.13
Nodes (10): CommanderEdgeChecks, Button, Task, MonoBehaviour, Task, SaveProgressionCapture, UpkeepManager, ConsecutiveFailures (+2 more)

### Community 132 - "Barracks"
Cohesion: 0.14
Nodes (9): Barracks, CurrentTier, IsUpgrading, MaxTier, Role, UpgradeFoodCost, UpgradeRemaining, UpgradeSoilCost (+1 more)

### Community 133 - "CampaignResearch"
Cohesion: 0.11
Nodes (14): CampaignResearch, Active, Departed, EndingGameSeconds, HasBlueprint, Instance, LeftBehind, Passengers (+6 more)

### Community 134 - "Encyclopedia"
Cohesion: 0.18
Nodes (11): Book, Encyclopedia, Entries, Path, IEnumerable, IReadOnlyList, List, DiscoveryDto (+3 more)

### Community 135 - "CommanderPersonalState"
Cohesion: 0.15
Nodes (6): CommanderPersonalState, HasSeriousInjury, HasTreatableInjury, CommanderRelation, MoodFactor, List

### Community 136 - "GameSession"
Cohesion: 0.13
Nodes (9): GameSession, Exists, GameSeconds, GameStarted, Instance, Options, PendingLoad, PlaySeconds (+1 more)

### Community 137 - ".Main"
Cohesion: 0.29
Nodes (6): AcquisitionSetupChecks, PrisonerCamp, Renderer, ScoutPost, Transform, T

### Community 138 - ".ForScene"
Cohesion: 0.28
Nodes (4): SavePreflight, List, ResourceNode, State

### Community 139 - "CommanderActivity"
Cohesion: 0.18
Nodes (11): CommanderActivity, Building, Command, Crafting, Farming, Fishing, Gathering, Melee (+3 more)

### Community 140 - "MenuTooltip"
Cohesion: 0.19
Nodes (9): Button, RectTransform, Text, TooltipChecks, MenuTooltip, Graphic, IPointerEnterHandler, IPointerExitHandler (+1 more)

### Community 141 - ".GetTemplate"
Cohesion: 0.18
Nodes (11): AcidTower, AirshipYard, Barracks, GameObject, Infirmary, NavMeshObstacle, PrisonerCamp, ResearchLab (+3 more)

### Community 142 - "CommanderAcquisitionPanel"
Cohesion: 0.16
Nodes (14): CommanderAcquisitionPanel, Camp, Feedback, PanelRect, Scout, SelectedPrisoner, Button, Color (+6 more)

### Community 143 - "ReadmeEditor"
Cohesion: 0.18
Nodes (8): GUIStyle, ReadmeEditor, BodyStyle, ButtonStyle, HeadingStyle, LinkStyle, TitleStyle, GUIContent

### Community 144 - ".Main"
Cohesion: 0.47
Nodes (3): EnemyColonyPlacementChecks, Func, Task

### Community 145 - ".SetupFarm"
Cohesion: 0.40
Nodes (4): GameObject, MenuItem, Transform, FarmPrototypeBootstrapper

### Community 146 - ".Spawn"
Cohesion: 0.46
Nodes (4): BossLoot, MeshRenderer, ResourceType, Vector3

### Community 147 - ".Main"
Cohesion: 0.26
Nodes (6): SettlementDefenseChecks, Func, NavMeshAgent, Task, Text, Vector3

### Community 148 - ".Main"
Cohesion: 0.26
Nodes (6): TransportRouteChecks, Button, Func, Task, Text, Vector3

### Community 149 - "EquipmentItem"
Cohesion: 0.13
Nodes (11): EquipmentInventory, Instance, EquipmentItem, IsValid, Label, WeaponKind, AcidSprayer, Mandible (+3 more)

### Community 150 - "ResourceNodeStatus"
Cohesion: 0.29
Nodes (4): ResourceNodeStatus, StatusText, Camera, GUIStyle

### Community 151 - "IDamageable"
Cohesion: 0.12
Nodes (7): CombatTargeting, Vector3, IDamageable, IsDead, Position, Vector3, Vector2

### Community 152 - ".Prepare"
Cohesion: 0.40
Nodes (3): Task, Vector3, SaveDifficultyCapture

### Community 153 - "Storage"
Cohesion: 0.25
Nodes (4): Storage, IsDepositPoint, ResearchBonus, Vector3Int

### Community 155 - "BetaProgress"
Cohesion: 0.20
Nodes (6): BetaProgress, CurrentObjective, Barracks, Image, ScienceLab, Text

### Community 156 - "UnitRole"
Cohesion: 0.16
Nodes (7): UnitRole, Defense, Flying, Melee, Ranged, Support, Worker

### Community 157 - ".Main"
Cohesion: 0.36
Nodes (5): AirshipYard, ScienceLab, Task, Vector3, Stage1Checks

### Community 158 - "UnitData"
Cohesion: 0.13
Nodes (9): CommanderAcquisitionChecks, PrisonerCamp, ScoutPost, Task, MenuItem, DataAssetBootstrapper, UnitData, GameObject (+1 more)

### Community 159 - ".Setup"
Cohesion: 0.24
Nodes (7): WorldMapBootstrapper, GameObject, MenuItem, Object, ScienceLab, Transform, Vector3

### Community 160 - ".Main"
Cohesion: 0.22
Nodes (7): Animator, Collider, GameObject, Material, SkinnedMeshRenderer, SetupQuirkyAnt, Rigidbody

### Community 161 - "AcidTower"
Cohesion: 0.17
Nodes (6): AcidTower, AttackInterval, Cooldown, Damage, Range, LineRenderer

### Community 162 - "Readme"
Cohesion: 0.33
Nodes (5): Texture2D, Readme, Section, ScriptableObject, Section

### Community 163 - "GameCalendar"
Cohesion: 0.15
Nodes (13): GameCalendar, CurrentSeason, GameSeconds, Label, Month, MonthProgress, TotalMonths, Year (+5 more)

### Community 164 - "BossLineAoE"
Cohesion: 0.33
Nodes (5): BossLineAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 165 - ".Main"
Cohesion: 0.39
Nodes (5): Button, InputField, ScrollRect, Task, FullUIChecks

### Community 166 - ".Main"
Cohesion: 0.35
Nodes (5): CampaignChecks, AirshipYard, ScienceLab, Task, Vector3

### Community 167 - "BossBasicPatternLoop"
Cohesion: 0.31
Nodes (4): BossBasicPatternLoop, LayerMask, Transform, Vector3

### Community 168 - "com.unity.modules.accessibility"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.accessibility

### Community 170 - "ObjectPool"
Cohesion: 0.31
Nodes (6): ObjectPool, Dictionary, GameObject, Quaternion, Queue, Vector3

### Community 172 - ".WriteAtomic"
Cohesion: 0.25
Nodes (4): SaveStorage, Root, RootOverride, SavesFolder

### Community 173 - "LocalIncursions"
Cohesion: 0.31
Nodes (5): LocalIncursions, SavedTimer, Visitors, IReadOnlyList, List

### Community 174 - ".Main"
Cohesion: 0.43
Nodes (4): RaidChecks, Func, MonoBehaviour, Task

### Community 176 - ".CreateSelectionBoxImage"
Cohesion: 0.29
Nodes (5): Canvas, CanvasScaler, GraphicRaycaster, Image, RectTransform

### Community 179 - "AirshipPart"
Cohesion: 0.40
Nodes (4): AirshipPart, Cocoon, Engine, Hull

### Community 182 - "CommanderPersonality"
Cohesion: 0.40
Nodes (5): CommanderPersonality, Balanced, Brave, Cautious, Devoted

### Community 185 - ".Main"
Cohesion: 0.12
Nodes (15): Task, Text, WeaponTalentChecks, ArmorKind, Coating, Wings, EquipmentSlot, Armor (+7 more)

### Community 188 - ".GenerateTexture"
Cohesion: 0.50
Nodes (3): Layer, Texture2D, Texture2DArray

### Community 194 - ".Restore"
Cohesion: 0.33
Nodes (4): IEnumerator, NavMeshAgent, State, Storage

## Knowledge Gaps
- **1059 isolated node(s):** `IsCasting`, `IsCasting`, `IsCasting`, `CurrentHp`, `MaxHp` (+1054 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1549 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **11 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `CommanderAnt` connect `CommanderAnt` to `.Main`, `.Main`, `UpkeepManager`, `AntColony.Units`, `CampaignResearch`, `Encyclopedia`, `CommanderPersonalState`, `SoldierAnt`, `SettlementDefense`, `CommanderActivity`, `BuildingConstructionSite`, `BossHealth`, `.Main`, `.Main`, `CommanderTraits`, `EquipmentItem`, `ResearchLab`, `IDamageable`, `.Main`, `.Main`, `.Main`, `SelectedUnitPanel`, `UnitData`, `UnitRole`, `CommanderSkills`, `NurseryChamber`, `.Main`, `CommanderTalents`, `.Main`, `.Main`, `.LateUpdate`, `HUDController`, `ExpeditionTransport`, `.Main`, `.Capture`, `.Main`, `.Main`, `ScienceLab`, `ExpeditionSite`, `GameMenuController`, `SupportChecks`, `.Prepare`, `.Main`, `.Main`, `AntVisual`, `Infirmary`, `WorkerAnt`, `.Main`, `AnnexedSettlement`, `CommanderRoster`, `AirshipYard`?**
  _High betweenness centrality (0.123) - this node is a cross-community bridge._
- **Why does `BuildingBase` connect `BuildingBase` to `.Main`, `Barracks`, `SoldierAnt`, `.Main`, `.ForScene`, `BuildingConstructionSite`, `.Main`, `.SetupFarm`, `.Main`, `.Main`, `IDamageable`, `.Prepare`, `Storage`, `ResearchLab`, `.Main`, `UnitRole`, `.Main`, `AcidTower`, `EnemyColony`, `AntColony.Core`, `BuildingPlacementController`, `.Main`, `.Main`, `ExpeditionTransport`, `.Main`, `.Capture`, `GameManager`, `.Main`, `ScienceLab`, `ExpeditionSite`, `HomeMapBuilder`, `.Setup`, `BuildingKind`, `.Main`, `Infirmary`, `SaveCatalog`, `.Main`, `WorkerAnt`, `QueenChamber`, `AirshipYard`, `MonoBehaviour`?**
  _High betweenness centrality (0.078) - this node is a cross-community bridge._
- **Why does `AntColony.Core` connect `AntColony.Core` to `ResourceType`, `AntColony.Units`, `GameCalendar`, `HomeMapBuilder`, `GameSession`, `AntColony.Buildings`, `ObjectPool`, `WorldMapBootstrapper.cs`, `AntColony.Boss`, `BuildingConstructionSite`, `IDamageable`, `GameAction`?**
  _High betweenness centrality (0.049) - this node is a cross-community bridge._
- **What connects `IsCasting`, `IsCasting`, `IsCasting` to the rest of the system?**
  _1059 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `manifest.json` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._
- **Should `AntColony.Units` be split into smaller, more focused modules?**
  _Cohesion score 0.09885057471264368 - nodes in this community are weakly interconnected._
- **Should `dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._