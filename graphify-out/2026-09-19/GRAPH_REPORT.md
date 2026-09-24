# Graph Report - ant  (2026-09-19)

## Corpus Check
- 111 files · ~58,635 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2042 nodes · 3635 edges · 124 communities (114 shown, 8 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 97 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `d324c701`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- manifest.json
- .Main
- BuildingPlacementController
- dependencies
- AntColony.Core
- ExpeditionTransport
- SoldierAnt
- packages-lock.json
- ResourceNode
- MapGenerator
- GroundTelegraphCircle
- com.unity.modules.audio
- dependencies
- AntPool
- .Main
- CommanderAnt
- com.unity.render-pipelines.core
- BossHealth
- ResourceManager
- com.unity.nuget.newtonsoft-json
- com.unity.modules.jsonserialize
- CommanderTraits
- com.unity.burst
- ResearchLab
- UnitData
- com.unity.nuget.mono-cecil
- BuildingKind
- com.unity.test-framework
- BuildingBase
- SelectedUnitPanel
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
- MonoBehaviour
- HUDController
- 2026-09-02 ~ 2026-09-03 (세션 1)
- .Main
- ColonyInvasion
- com.unity.modules.imgui
- BossCircleAoE
- AntUnitBase
- .Main
- .Main
- UpkeepManager
- AttackMoveController
- CommanderAcquisitionPanel
- com.unity.ai.navigation
- .BuildCanvas
- com.unity.modules.unitywebrequest
- SelectionManager
- EnemyColony
- com.unity.modules.uielements
- .Spawn
- ScoutPost
- .SetMaterial
- SelectableObject
- changelog.md
- 프로젝트 로그
- .Main
- ExpeditionSite
- GameManager
- com.unity.modules.ui
- Bounds
- dependencies
- Barracks
- QueenChamber
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
- EnemyCommander
- .Main
- BossPatternSequenceSimple
- BuildingConstructionSite
- 프로젝트 작업 규칙
- CLAUDE.md
- .ConfigureCommander
- ResourceNodeStatus
- Storage
- IDamageable
- CommanderWorkProficiency
- WorldMapManager
- .SetupFarm
- WorkerAnt
- .TryPlace
- DigSite
- .CreateButton
- .Main
- IsometricCameraController
- ExpeditionSiteKind
- CommanderRoster
- .GetTemplate
- UnitRole
- BossBasicPatternLoop
- .LateUpdate
- .CommandAttackMove
- .Main
- .CreateSelectionBoxImage
- 2026-09-05 (Codex 인수인계 / SAVE 연결 검증)

## God Nodes (most connected - your core abstractions)
1. `CommanderAnt` - 111 edges
2. `AntColony.Core` - 57 edges
3. `BuildingPlacementController` - 56 edges
4. `BuildingBase` - 54 edges
5. `AntColony.Data` - 52 edges
6. `AntColony.Buildings` - 51 edges
7. `ResourceNode` - 51 edges
8. `UnitRole` - 44 edges
9. `AntColony.Units` - 44 edges
10. `SelectionManager` - 41 edges

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

## Communities (124 total, 8 thin omitted)

### Community 0 - "manifest.json"
Cohesion: 0.04
Nodes (49): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+41 more)

### Community 1 - ".Main"
Cohesion: 0.16
Nodes (11): WorldMapChecks, Barracks, BindingFlags, Button, Func, NavMeshObstacle, PrisonerCamp, QueenChamber (+3 more)

### Community 2 - "BuildingPlacementController"
Cohesion: 0.21
Nodes (5): BuildingPlacementController, ConsumesPointerInput, IsPlacing, Camera, LayerMask

### Community 3 - "dependencies"
Cohesion: 0.04
Nodes (50): dependencies, com.akiojin.unity-cli-bridge, com.unity.ai.assistant, com.unity.ai.inference, com.unity.ai.navigation, com.unity.collab-proxy, com.unity.ide.rider, com.unity.ide.visualstudio (+42 more)

### Community 4 - "AntColony.Core"
Cohesion: 0.08
Nodes (12): AntColony.Boss.AoE, AntColony.Data, AntColony.Units, AntColony.Core, AntColony.Setup, AntColony.World, AntColony.Boss.Telegraph, AntColony.EditorTools (+4 more)

### Community 5 - "ExpeditionTransport"
Cohesion: 0.06
Nodes (38): WorldMapPanel, IsOpen, PanelRect, Button, Font, GameObject, Image, List (+30 more)

### Community 6 - "SoldierAnt"
Cohesion: 0.17
Nodes (9): IAirborne, IsAirborne, Vector3, SoldierAnt, IsAirborne, IsFlying, LayerMask, Vector3 (+1 more)

### Community 7 - "packages-lock.json"
Cohesion: 0.04
Nodes (44): com.unity.inputsystem, com.unity.modules.ai, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+36 more)

### Community 8 - "ResourceNode"
Cohesion: 0.09
Nodes (15): ResourceNode, AmountRemaining, CanGather, GatherRateMultiplier, IsDepleted, IsRaidLocked, IsRaidLoot, IsRegrowing (+7 more)

### Community 9 - "MapGenerator"
Cohesion: 0.09
Nodes (23): GameObject, MenuItem, MapGeneratorEditor, Collider, MenuItem, SnapToTerrainMenu, Layer, MapGenerator (+15 more)

### Community 10 - "GroundTelegraphCircle"
Cohesion: 0.36
Nodes (5): GroundTelegraphCircle, LayerMask, Mesh, MeshFilter, Vector3

### Community 11 - "com.unity.modules.audio"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, version, dependencies, depth, source, version (+13 more)

### Community 12 - "dependencies"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, url, version, dependencies, depth, source (+14 more)

### Community 13 - "AntPool"
Cohesion: 0.13
Nodes (6): AntPool, Assigned, Free, Instance, Reserved, Total

### Community 14 - ".Main"
Cohesion: 0.27
Nodes (6): InvasionChecks, FieldInfo, Func, List, NavMeshAgent, Task

### Community 15 - "CommanderAnt"
Cohesion: 0.05
Nodes (36): CommanderAnt, AllowedRoles, Armor, AttackDamage, CanChangeAllocation, CanDefensiveStance, CanPowerStrike, CanStartConstruction (+28 more)

### Community 16 - "com.unity.render-pipelines.core"
Cohesion: 0.09
Nodes (25): depth, source, version, dependencies, depth, source, version, dependencies (+17 more)

### Community 17 - "BossHealth"
Cohesion: 0.07
Nodes (29): Action, WorkProficiencyLootChecks, BindingFlags, BoxCollider, Button, Collider, FieldInfo, Func (+21 more)

### Community 18 - "ResourceManager"
Cohesion: 0.18
Nodes (7): ResourceManager, Instance, Dictionary, ResourceType, Food, Soil, Special

### Community 19 - "com.unity.nuget.newtonsoft-json"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, url, version, dependencies, depth, source (+4 more)

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

### Community 24 - "UnitData"
Cohesion: 0.18
Nodes (10): ObjectPool, Dictionary, GameObject, Quaternion, Vector3, UnitData, GameObject, GameObject (+2 more)

### Community 25 - "com.unity.nuget.mono-cecil"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, version, dependencies, depth, source, url (+9 more)

### Community 26 - "BuildingKind"
Cohesion: 0.09
Nodes (21): CombatRolePrototypeBootstrapper, Barracks, GameObject, MenuItem, MonoScript, ResearchLab, Transform, MenuItem (+13 more)

### Community 27 - "com.unity.test-framework"
Cohesion: 0.12
Nodes (17): dependencies, dependencies, depth, source, url, version, depth, dependencies (+9 more)

### Community 28 - "BuildingBase"
Cohesion: 0.14
Nodes (11): SetupRaid, BuildingBase, CountsTowardPlayerDefeat, CurrentHealth, Data, IsDead, IsDepositPoint, MaxHealth (+3 more)

### Community 29 - "SelectedUnitPanel"
Cohesion: 0.23
Nodes (11): SelectedUnitPanel, Button, Color, Font, GameObject, Image, RectTransform, Text (+3 more)

### Community 30 - ".Main"
Cohesion: 0.15
Nodes (12): CommanderProgressionChecks, BindingFlags, MonoBehaviour, Task, Vector3, CommanderProgression, ArmorBonus, AttackBonus (+4 more)

### Community 31 - "ReadmeEditor"
Cohesion: 0.12
Nodes (13): GUIStyle, ReadmeEditor, BodyStyle, ButtonStyle, HeadingStyle, LinkStyle, TitleStyle, Texture2D (+5 more)

### Community 32 - "PrisonerCamp"
Cohesion: 0.12
Nodes (14): Prisoner, PrisonerCamp, Capacity, Count, EscapedCount, ExecutedCount, HasSpace, Instance (+6 more)

### Community 33 - "com.unity.collections"
Cohesion: 0.06
Nodes (33): dependencies, depth, source, url, version, dependencies, depth, source (+25 more)

### Community 34 - "com.unity.modules.physics"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 35 - "CommanderSkills"
Cohesion: 0.13
Nodes (6): CommanderSkills, DefensiveStanceActive, DefensiveStanceCooldownLeft, DefensiveStanceTimeLeft, PowerStrikeArmed, PowerStrikeCooldownLeft

### Community 36 - "NurseryChamber"
Cohesion: 0.13
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
Cohesion: 0.15
Nodes (12): BossLineAoE, IsCasting, IEnumerator, LayerMask, Vector3, GroundTelegraphLine, LayerMask, Mesh (+4 more)

### Community 42 - "HUDController"
Cohesion: 0.18
Nodes (6): HUDController, SelectedCommander, Barracks, QueenChamber, ResearchLab, DigSite

### Community 43 - "2026-09-02 ~ 2026-09-03 (세션 1)"
Cohesion: 0.15
Nodes (13): 2026-09-02 ~ 2026-09-03 (세션 1), MVP 수직 슬라이스 — 이번 세션 구현 완료, SIMUL-TeaamProject에서 추가 이식(보스 레이드 / 선택 시스템) — 2026-09-02, 개요, 기획서 시스템 요약, 랜덤맵 생성(MapGenerator) — 이전 팀 프로젝트에서 포팅, 보스 AoE/텔레그래프 시스템 — `Assets/Scripts/Boss/`, 설계 메모 (+5 more)

### Community 44 - ".Main"
Cohesion: 0.07
Nodes (23): RegressionChecks, BoxCollider, FieldInfo, Func, GameObject, List, MeshFilter, NavMeshAgent (+15 more)

### Community 45 - "ColonyInvasion"
Cohesion: 0.20
Nodes (4): ColonyInvasion, List, Transform, ResourceType

### Community 46 - "com.unity.modules.imgui"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, version, dependencies, depth, source, url (+14 more)

### Community 47 - "BossCircleAoE"
Cohesion: 0.25
Nodes (5): BossCircleAoE, IsCasting, IEnumerator, LayerMask, Vector3

### Community 48 - "AntUnitBase"
Cohesion: 0.14
Nodes (12): AntUnitBase, Agent, Armor, AttackDamage, CurrentHealth, Data, IsDead, Position (+4 more)

### Community 49 - ".Main"
Cohesion: 0.33
Nodes (4): EnemyColonyEconomyChecks, FieldInfo, Func, Task

### Community 50 - ".Main"
Cohesion: 0.40
Nodes (4): CommanderAcquisitionChecks, PrisonerCamp, ScoutPost, Task

### Community 51 - "UpkeepManager"
Cohesion: 0.10
Nodes (17): FishingChecks, FieldInfo, Func, MonoBehaviour, QueenChamber, Task, LabUpgradeChecks, BindingFlags (+9 more)

### Community 52 - "AttackMoveController"
Cohesion: 0.13
Nodes (11): AttackMoveController, ConsumesPointerInput, IsAttackMode, Camera, LayerMask, Vector2, IReadOnlyList, UnitSelectionController (+3 more)

### Community 53 - "CommanderAcquisitionPanel"
Cohesion: 0.15
Nodes (15): CommanderAcquisitionPanel, Camp, Feedback, PanelRect, Scout, SelectedPrisoner, Button, Color (+7 more)

### Community 54 - "com.unity.ai.navigation"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, url, version, dependencies, depth, source (+3 more)

### Community 55 - ".BuildCanvas"
Cohesion: 0.22
Nodes (5): Canvas, CanvasScaler, GraphicRaycaster, EventSystem, InputSystemUIInputModule

### Community 56 - "com.unity.modules.unitywebrequest"
Cohesion: 0.05
Nodes (48): dependencies, dependencies, depth, source, version, dependencies, depth, source (+40 more)

### Community 57 - "SelectionManager"
Cohesion: 0.24
Nodes (6): SelectionManager, Camera, LayerMask, List, Vector2, Rect

### Community 58 - "EnemyColony"
Cohesion: 0.20
Nodes (5): EnemyColony, IsDefeated, RemainingBuildings, List, Vector3

### Community 59 - "com.unity.modules.uielements"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, url, version, depth, source, version (+7 more)

### Community 60 - ".Spawn"
Cohesion: 0.24
Nodes (6): MoveMarker, Collider, Color, Material, MeshRenderer, Vector3

### Community 61 - "ScoutPost"
Cohesion: 0.17
Nodes (8): ScoutPost, CurrentChance, DispatchAnts, DispatchFoodCost, FailureCount, IsDispatched, Remaining, SuccessCount

### Community 62 - ".SetMaterial"
Cohesion: 0.22
Nodes (7): SetupFishing, Collider, GameObject, Material, NavMeshObstacle, Renderer, Color

### Community 63 - "SelectableObject"
Cohesion: 0.13
Nodes (9): MonoBehaviour, Task, SaveProgressionCapture, SelectableObject, IsSelected, Color, Renderer, Transform (+1 more)

### Community 64 - "changelog.md"
Cohesion: 0.17
Nodes (11): 2026-09-03~04 (세션 2), 2026-09-10 SAVE — 이전 로그 이관 (2026-09-09 상태), 2026-09-11 — SAVE 연결 복구, 2026-09-11 — 이전 로그 요약 및 중단 지점, 2026-09-13 SAVE — 이전 로그(2026-09-11) 이관, 2026-09-14 SAVE — 이전 로그(2026-09-13) 이관, 2026-09-14 SAVE — 이전 로그(Support 전용 버프) 이관, 2026-09-14 SAVE — 이전 로그(적 소굴 랜덤 배치) 이관 (+3 more)

### Community 65 - "프로젝트 로그"
Cohesion: 0.29
Nodes (6): 2026-09-19 현재 상태 — 월드맵 30거점 확장 완료, 검증, 다음 작업과 미정 사항, 이번 기록의 저장 범위, 프로젝트 구조와 규칙, 프로젝트 로그

### Community 66 - ".Main"
Cohesion: 0.43
Nodes (4): RaidChecks, Func, MonoBehaviour, Task

### Community 67 - "ExpeditionSite"
Cohesion: 0.10
Nodes (18): ExpeditionSite, Boss, CanResolveConquest, Cleared, Colony, Difficulty, Disposition, Faction (+10 more)

### Community 68 - "GameManager"
Cohesion: 0.15
Nodes (5): GameManager, FishingUnlocked, Instance, List, Vector3

### Community 69 - "com.unity.modules.ui"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 70 - "Bounds"
Cohesion: 0.32
Nodes (4): EnemyColonyPlacementChecks, Func, Task, Bounds

### Community 71 - "dependencies"
Cohesion: 0.10
Nodes (20): depth, source, version, dependencies, depth, source, version, dependencies (+12 more)

### Community 72 - "Barracks"
Cohesion: 0.17
Nodes (9): Barracks, CurrentTier, IsUpgrading, MaxTier, Role, UpgradeFoodCost, UpgradeSoilCost, IEnumerator (+1 more)

### Community 73 - "QueenChamber"
Cohesion: 0.27
Nodes (4): QueenChamber, IsDepositPoint, IEnumerator, UnitData

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
Cohesion: 0.17
Nodes (5): ScienceLab, Busy, PrerequisitesMet, Remaining, Vector3

### Community 92 - ".Main"
Cohesion: 0.33
Nodes (5): AcquisitionBuildingChecks, Canvas, PrisonerCamp, ScoutPost, Task

### Community 93 - "EnemyCommander"
Cohesion: 0.05
Nodes (31): AcquisitionSetupChecks, PrisonerCamp, Renderer, ScoutPost, Transform, CommanderAcquisitionBootstrapper, GameObject, MenuItem (+23 more)

### Community 94 - ".Main"
Cohesion: 0.29
Nodes (6): ActiveSkillChecks, BindingFlags, Button, MonoBehaviour, Task, Vector3

### Community 95 - "BossPatternSequenceSimple"
Cohesion: 0.29
Nodes (4): BossPatternSequenceSimple, LayerMask, Transform, Vector3

### Community 96 - "BuildingConstructionSite"
Cohesion: 0.23
Nodes (5): BuildingConstructionSite, BuildTimeSeconds, Position, GameObject, Vector3

### Community 102 - "ResourceNodeStatus"
Cohesion: 0.29
Nodes (4): ResourceNodeStatus, StatusText, Camera, GUIStyle

### Community 104 - "IDamageable"
Cohesion: 0.20
Nodes (4): IDamageable, IsDead, Position, Vector3

### Community 105 - "CommanderWorkProficiency"
Cohesion: 0.29
Nodes (4): CommanderWorkProficiency, GatherMultiplier, Level, Progress

### Community 106 - "WorldMapManager"
Cohesion: 0.13
Nodes (13): WorldMapManager, AircraftResearched, HomePosition, Instance, Researcher, Sites, Transports, Unlocked (+5 more)

### Community 107 - ".SetupFarm"
Cohesion: 0.40
Nodes (4): GameObject, MenuItem, Transform, FarmPrototypeBootstrapper

### Community 108 - "WorkerAnt"
Cohesion: 0.17
Nodes (6): WorkerAnt, CanStartConstruction, CarryCapacity, GatherRate, IsCarrying, IsConstructing

### Community 109 - ".TryPlace"
Cohesion: 0.36
Nodes (3): Collider, Renderer, Vector3

### Community 110 - "DigSite"
Cohesion: 0.50
Nodes (3): DigSite, IsExpanded, GameObject

### Community 111 - ".CreateButton"
Cohesion: 0.29
Nodes (8): Button, Font, Image, RectTransform, Text, Transform, UnityAction, Vector2

### Community 112 - ".Main"
Cohesion: 0.31
Nodes (6): CommanderChecks, Barracks, Func, MonoBehaviour, QueenChamber, Task

### Community 113 - "IsometricCameraController"
Cohesion: 0.23
Nodes (5): IsometricCameraController, FocusPoint, Camera, Vector3, AntColony.Camera

### Community 114 - "ExpeditionSiteKind"
Cohesion: 0.33
Nodes (5): ExpeditionSiteKind, BossNest, ResourceSite, Settlement, Vector2

### Community 115 - "CommanderRoster"
Cohesion: 0.16
Nodes (9): CommanderRoster, Commanders, Count, Instance, IEnumerable, IReadOnlyList, List, Transform (+1 more)

### Community 116 - ".GetTemplate"
Cohesion: 0.28
Nodes (6): Barracks, GameObject, PrisonerCamp, ResearchLab, ScienceLab, ScoutPost

### Community 117 - "UnitRole"
Cohesion: 0.25
Nodes (7): UnitRole, Defense, Flying, Melee, Ranged, Support, Worker

### Community 118 - "BossBasicPatternLoop"
Cohesion: 0.21
Nodes (5): BossBasicPatternLoop, LayerMask, Transform, Vector3, Collider

### Community 122 - ".CreateSelectionBoxImage"
Cohesion: 0.29
Nodes (5): Canvas, CanvasScaler, GraphicRaycaster, Image, RectTransform

### Community 126 - "2026-09-05 (Codex 인수인계 / SAVE 연결 검증)"
Cohesion: 0.67
Nodes (3): 2026-09-05 (Codex 인수인계 / SAVE 연결 검증), 이번 작업, 이전 log.md 요약 이관

## Knowledge Gaps
- **732 isolated node(s):** `AntColony.EditorTools`, `IsCasting`, `IsCasting`, `IsCasting`, `CurrentHp` (+727 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1034 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **8 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `CommanderAnt` connect `CommanderAnt` to `.Main`, `AntColony.Core`, `ExpeditionTransport`, `ResourceNode`, `.Main`, `BossHealth`, `CommanderTraits`, `ResearchLab`, `UnitData`, `SelectedUnitPanel`, `.Main`, `CommanderSkills`, `NurseryChamber`, `HUDController`, `.Main`, `UpkeepManager`, `SelectableObject`, `.Main`, `SupportChecks`, `.Main`, `EnemyCommander`, `.Main`, `BuildingConstructionSite`, `.ConfigureCommander`, `IDamageable`, `CommanderWorkProficiency`, `WorkerAnt`, `.Main`, `CommanderRoster`, `UnitRole`, `.LateUpdate`?**
  _High betweenness centrality (0.081) - this node is a cross-community bridge._
- **Why does `BuildingBase` connect `BuildingBase` to `.Main`, `BuildingPlacementController`, `AntColony.Core`, `ExpeditionTransport`, `.Main`, `ResourceManager`, `ResearchLab`, `BuildingKind`, `.Main`, `MonoBehaviour`, `.Main`, `ColonyInvasion`, `.Main`, `UpkeepManager`, `.BuildCanvas`, `EnemyColony`, `.Main`, `ExpeditionSite`, `GameManager`, `Bounds`, `Barracks`, `QueenChamber`, `ScienceLab`, `.Main`, `EnemyCommander`, `Storage`, `IDamageable`, `.SetupFarm`, `WorkerAnt`, `.TryPlace`, `DigSite`?**
  _High betweenness centrality (0.078) - this node is a cross-community bridge._
- **Why does `ResourceNode` connect `ResourceNode` to `.Main`, `AntColony.Core`, `.Main`, `BossHealth`, `BuildingBase`, `MonoBehaviour`, `.Main`, `ColonyInvasion`, `.Main`, `UpkeepManager`, `AttackMoveController`, `EnemyColony`, `.SetMaterial`, `.Main`, `ExpeditionSite`, `.Spawn`, `.Main`, `ResourceNodeStatus`, `.SetupFarm`, `WorkerAnt`, `.GetTemplate`?**
  _High betweenness centrality (0.038) - this node is a cross-community bridge._
- **What connects `AntColony.EditorTools`, `IsCasting`, `IsCasting` to the rest of the system?**
  _732 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `manifest.json` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._
- **Should `dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.04 - nodes in this community are weakly interconnected._
- **Should `AntColony.Core` be split into smaller, more focused modules?**
  _Cohesion score 0.0761904761904762 - nodes in this community are weakly interconnected._