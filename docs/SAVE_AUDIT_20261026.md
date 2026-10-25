# Claude·Codex 통합 SAVE 대조 — 2026-10-26

## 범위와 근거
- 10월25일 낮 SAVE `6b9cfb1` 이후, 10월25일~26일 이 채팅까지 이어진 작업이다. 작업 보드, HEAD 대비 실제 diff, Claude 세션 3개, Codex E1/E2/F1/F2/F3/G1/G2 인계와 사본을 대조했다.
- Claude 기록: `13fff952-aaf1-4403-8a25-e1595cbeb6dd`(E1/E2 통합·UI·G26·메뉴), `df6935fe-43ea-4908-a8f7-dd131d73587a`(H1/H2·그래픽 감사), 앞선 `c55db1a6-569b-4adb-b360-2c810c7449ec`(낮 SAVE 전 검사). 원본 대화는 로컬 .claude에 보존하며 저장소에는 복사하지 않는다.
- 과거 보고를 현재 검증으로 과장하지 않는다. 강제 Animator.Update로 통과했던 초기 검사 뒤 사용자 화면에서 전체 재생 정지가 재현됐고, 공통 리그 가시성 판정 수정 후 실제 프레임 검사로 재검증했다.

## 본 프로젝트에 반영된 작업
| 묶음 | 결과 | 근거/한계 |
|---|---|---|
| E1 D04 | 평진·삼국지풍7·현대식6, 적성·연구6·전투/도보 보정·저장 호환 | Claude 통합, 출전 UI 연결. ImplD04Checks 과거363, 이번 SAVE 재확인 결과는 아래 기록 |
| E2 D06/D07/D09 | 철수 단계·실물 짐/시신·긴급 출발·수송 파손/수리/해체·순회 저장 | Claude 통합과 UI, 과거82/208. 이번 SAVE 결과 별도 |
| Claude H1 | 특성 칩, 원정대 패널 높이/버튼 넘침, 불시착 잔해6×4 | 4개 패치 중 H1 3개 반영. 화살표는 후속 사용자 요청으로 제거 |
| Claude H2 | 지면 고정 드래그·마우스 놓침 복구, 설정 초기화/품질/해상도/스크롤/키 설명 | 적용 후17검사. 수면 장수 자동 제외는 미확정 규칙 변경이라 빼고 기존 선택 유지 |
| F3+Codex 그래픽 | 시설 축/비균일 부모 배율, 자원46 모델·생물6종·주민·공사/철거·전리품·시체, URP 머티리얼/런타임 셰이더 참조 | 시설710, 자원200, 사건/공사/시체14, 시민/전리품10. 원본 Blender art는 로컬 보존 정책 유지 |
| 입력/시작 | 맵 설정 입력차단·스크롤/재추첨, 장수 키/클릭 범위, 개인식량 보유자 이탈 방지·선택한2명 유지, 가장자리 이동 복구 | 메뉴/날씨14, 시작/클릭/보급13, 원정UI27 |
| 공통 애니메이션 후속 | 클립 경로→실제 리그, 중복90도 제거·초상 Idle_A, 메시가 리그 형제여서 생략되던 재생 AlwaysAnimate/스킨 갱신 | 강제 Animator.Update 없이 장수13동작·생물6리그·시민40검사 |
| 채집 후속 | 잔여거리0 고착 해소·도착시 경로 정지·작업동작 우선, 이동/채집/운반 대상 링/문구 | 실제5m 이동·걷는 메시·벌목·채집량·취소14검사 |
| 날씨 후속 | 생물군계×기후×계절, 사막 드문 강수/모래폭풍·숲/물가 비/안개·한랭/열대 강수형·동굴 제외, 계절꽃잎/낙엽/적설 제한·복원 보정 | 445검사. 실측 기후 자료가 아니라 게임 내 잠정 확률 |
| 즉시 재생처럼 보이던 자원 | 갈 곳 없는 바닥 물품 반복 줍기/내려놓기 차단, 실제 저장 Host·품목 허용·경로 확인 | 저장 공간만 있고 장소 없는 경우·금지/허용·실제3개 입고10검사. 식물120~300초 재성장 변경 없음 |
| 기존 G26 작업 보존 | SceneObjects 등록 캐시·호출 경로 교체·원산지 식품 등 기존 미커밋 변경 | 사용자의 후속 성능 최적화 보류 지시 이후 추가 성능 작업 안 함. 목표 성능 달성 주장 안 함 |

## 미통합·남은 작업
- F1 D02/D03 평가·외부 조사: 사본 코드/오프라인 컴파일 완료, 본 프로젝트/Unity/UI 연결 미완. `docs/patches/unfinished-20261026/F1-source-snapshot.patch`와 인계 보존.
- F2 D08 탐사·발견·저장: 사본 코드/오프라인 컴파일 완료, UI/채굴 생산·Unity 미완. 동일 폴더 F2 스냅샷/인계 보존. 스냅샷은 기준 E 변경도 포함하므로 main에 그대로 apply하지 않는다.
- 전용 자원 모델 Chitin/Wax/Component/Water/Soil 5종은 Claude에게 전달할 제작 범위를 제안했으나, 납품/통합 증거는 없다. 낚시터 표시·미사용 DigSite 모델·원정지 평판 지형도 남는다.
- `docs/NOTION_APPLY_1025_codex.md`의 미반영 기획 대조, D02/D03/D08 후속, 도구/광물/의료의 기존 미완 범위를 완료로 바꾸지 않는다.
- 성능 최적화와 림월드식 채취는 사용자 명시로 보류.

## SAVE 보존 정책
- develop에 실제 코드·직접 제작한 Unity FBX/애니메이션/텍스처/머티리얼·meta·검사·문서를 포함한다. prefab/prefab.meta는 커밋하지 않는다.
- 기존 제외 정책에 따라 씬·Water.mat·TimeManager·URP asset 재직렬화·Planet Earth Free/Resources WorldMap·raw art·graphify·Temp/캐시·개인 도구는 로컬에 보존하고 이번 커밋에서 제외한다. 원본 art와 사본은 삭제하지 않는다.
- 새 캡처는 SAVE에서만 생성. 실제 화면 확인 결과·최종 커밋·Notion 링크는 log.md에 기록한다.

## 이번 SAVE 재검증·실제 화면
- D04 363, D06/D07 82, D09 208 재실행 통과. 결과는 Temp/save-d04-result.txt, save-d06d07-result.txt, save-d09-result.txt. 앞선 실제 프레임 애니메이션40·채집14·운반10·날씨445 검사와 구분한다.
- 공식 Unity Pipeline screen 캡처를 직접 확인: 09-gather-day.png(실제 Leaf 채집·Tool_Harvest·대상 링/문구·장수/상단 초상), 05-candidates.png(빨강/실버 칩·화살표 제거), 08-expeditions.png(원정대 제목·빈 목록·버튼 배치). 캡처 파일은 Assets/Temp/save-20261026에 로컬 보존하고 Notion에 업로드한다.
- 캡처용 격리 게임이 밤이 되어 Sleep, 식사 중 Eat이 나온 초기 이미지는 작업 동작 검수로 사용하지 않았다. 게임 시간을 낮으로 맞춘 뒤 실제 Tool_Harvest 진입을 확인했다. 수면 중 명령 정책은 이번에 바꾸지 않았다.
- 후보 화면을 실행 게임 위에서 열었으므로 배경 HUD·숙소 부족 토스트가 남아 있다. UI 표시 검수이며 타이틀 시작 전체 흐름 검수로 과장하지 않는다. 대형 나무 수관의 작업자 가림은 추가 시각 점검 대상이다.
- 사막 등 모든 맵의 장시간 육안 검수와 Player 빌드는 미수행. 날씨445 검사는 확률/입자/적설/저장 복원 범위다.
- Notion 맵·UI·자원·메인 화면 4개 기획 페이지의 환경별 연출·칩 화살표 제거·우클릭 채집/피드백·바닥 운반 조건을 최신 요구로 교체하고 다시 읽어 확인했다. 과거 D01~D08 미반영 목록 전체를 완료했다는 뜻은 아니다.
- Unity Play 종료 확인. 사용자 실제 저장 대신 임시 SaveStorage 경로에서 검사했다.

## 최종 저장
- develop `36fc08a903de853cec294f1cf0efad957e5ff02a`, 2010파일 커밋·origin/develop push·원격 해시 일치 확인.
- Git 자동 geometric repack의 기존 Codex checkpoint bad object 오류는 남았다. 커밋/푸시는 성공했으며 내부 참조를 임의 삭제하지 않았다.
- Notion 개발 일지/Trouble Shooting에 캡처3장·결과·커밋 링크 반영. graphify update 완료. Player 빌드 미수행, 카카오톡 도구 없음.

## 전체 소스 대조 목록

### AgentScripts
- `AgentScripts/AutomaticAnimationChecks.cs`
- `AgentScripts/BetaChecks.cs`
- `AgentScripts/BiomeClimateWeatherChecks.cs`
- `AgentScripts/CharacterGraphicsChecks.cs`
- `AgentScripts/ClaudeH2Checks.cs`
- `AgentScripts/CorpseChecks.cs`
- `AgentScripts/ExpeditionLayoutChecks.cs`
- `AgentScripts/FacilityVisualChecks.cs`
- `AgentScripts/GatherArrivalChecks.cs`
- `AgentScripts/ImplD04Checks.cs`
- `AgentScripts/ImplD06D07Checks.cs`
- `AgentScripts/ImplD09Checks.cs`
- `AgentScripts/ImportCritterModels.cs`
- `AgentScripts/ImportGraphicsProps.cs`
- `AgentScripts/ImportResidentModel.cs`
- `AgentScripts/ImportResourceModels.cs`
- `AgentScripts/LiveGraphicsProbe.cs`
- `AgentScripts/LiveRepairChecks.cs`
- `AgentScripts/LootLoopChecks.cs`
- `AgentScripts/PerfG26Checks.cs`
- `AgentScripts/PrepareRuntimeShaders.cs`
- `AgentScripts/PrimitiveGraphicsChecks.cs`
- `AgentScripts/Recovery1025Checks.cs`
- `AgentScripts/RecoveryPlayProbe.cs`
- `AgentScripts/RecoveryPlayProfile.cs`
- `AgentScripts/RecoveryUnitChecks.cs`
- `AgentScripts/RegressionChecks.cs`
- `AgentScripts/RemainingGraphicsChecks.cs`
- `AgentScripts/ResourceVisualChecks.cs`
- `AgentScripts/SeasonWeatherChecks.cs`
- `AgentScripts/TraitChipChecks.cs`

### Assets/_Project/Editor/KemonoImport.cs
- `Assets/_Project/Editor/KemonoImport.cs`

### Assets/_Project/Scripts/Boss
- `Assets/_Project/Scripts/Boss/AoE/BossCircleAoE.cs`
- `Assets/_Project/Scripts/Boss/AoE/BossConeAoE.cs`
- `Assets/_Project/Scripts/Boss/AoE/BossLineAoE.cs`
- `Assets/_Project/Scripts/Boss/BossHealth.cs`

### Assets/_Project/Scripts/Buildings
- `Assets/_Project/Scripts/Buildings/AcidTower.cs`
- `Assets/_Project/Scripts/Buildings/AreaAcidTower.cs`
- `Assets/_Project/Scripts/Buildings/BuildingBase.cs`
- `Assets/_Project/Scripts/Buildings/BuildingConstructionSite.cs`
- `Assets/_Project/Scripts/Buildings/BuildingPlacementController.cs`
- `Assets/_Project/Scripts/Buildings/BuildingRepair.cs`
- `Assets/_Project/Scripts/Buildings/ConscriptionPost.cs`
- `Assets/_Project/Scripts/Buildings/Demolition.cs`
- `Assets/_Project/Scripts/Buildings/FacilityVisual.cs`
- `Assets/_Project/Scripts/Buildings/MineField.cs`
- `Assets/_Project/Scripts/Buildings/PowerNode.cs`
- `Assets/_Project/Scripts/Buildings/PrisonerCamp.cs`
- `Assets/_Project/Scripts/Buildings/Ranch.cs`
- `Assets/_Project/Scripts/Buildings/Redevelopment.cs`
- `Assets/_Project/Scripts/Buildings/RoomSystem.cs`
- `Assets/_Project/Scripts/Buildings/RuntimeBuildingTemplates.cs`
- `Assets/_Project/Scripts/Buildings/ScienceLab.cs`
- `Assets/_Project/Scripts/Buildings/SpaceQuality.cs`
- `Assets/_Project/Scripts/Buildings/StartGroundRules.cs`
- `Assets/_Project/Scripts/Buildings/TrapPit.cs`
- `Assets/_Project/Scripts/Buildings/Wall.cs`

### Assets/_Project/Scripts/Camera
- `Assets/_Project/Scripts/Camera/IsometricCameraController.cs`

### Assets/_Project/Scripts/Core
- `Assets/_Project/Scripts/Core/CampaignOutcome.cs`
- `Assets/_Project/Scripts/Core/CampaignResearch.Resources.cs`
- `Assets/_Project/Scripts/Core/CampaignResearch.cs`
- `Assets/_Project/Scripts/Core/ColonyPopulation.Civilians.cs`
- `Assets/_Project/Scripts/Core/ColonyPopulation.Evacuation.cs`
- `Assets/_Project/Scripts/Core/ColonyPopulation.cs`
- `Assets/_Project/Scripts/Core/CommanderRoster.cs`
- `Assets/_Project/Scripts/Core/CrashLandingStart.cs`
- `Assets/_Project/Scripts/Core/FinalBattleEconomy.cs`
- `Assets/_Project/Scripts/Core/FormationDamage.cs`
- `Assets/_Project/Scripts/Core/FormationResearch.cs`
- `Assets/_Project/Scripts/Core/GameBootstrap.cs`
- `Assets/_Project/Scripts/Core/GameManager.cs`
- `Assets/_Project/Scripts/Core/ResourceManager.cs`
- `Assets/_Project/Scripts/Core/RocketCampaign.cs`
- `Assets/_Project/Scripts/Core/RocketEconomy.cs`
- `Assets/_Project/Scripts/Core/SceneObjects.cs`
- `Assets/_Project/Scripts/Core/TraitRules.cs`
- `Assets/_Project/Scripts/Core/UpkeepManager.cs`
- `Assets/_Project/Scripts/Core/UserSettings.cs`

### Assets/_Project/Scripts/Map
- `Assets/_Project/Scripts/Map/DayNightLighting.cs`
- `Assets/_Project/Scripts/Map/HomeMapBuilder.cs`
- `Assets/_Project/Scripts/Map/Precipitation.cs`
- `Assets/_Project/Scripts/Map/SeasonVisuals.cs`
- `Assets/_Project/Scripts/Map/WeatherSystem.cs`

### Assets/_Project/Scripts/Save
- `Assets/_Project/Scripts/Save/SaveCatalog.cs`
- `Assets/_Project/Scripts/Save/SaveConstruction.cs`
- `Assets/_Project/Scripts/Save/SaveDtos.cs`
- `Assets/_Project/Scripts/Save/SaveOrderValidation.cs`
- `Assets/_Project/Scripts/Save/SaveOrders.cs`
- `Assets/_Project/Scripts/Save/SavePreflight.Integration.cs`
- `Assets/_Project/Scripts/Save/SavePreflight.cs`
- `Assets/_Project/Scripts/Save/SaveRuntime.cs`
- `Assets/_Project/Scripts/Save/SaveSnapshot.cs`
- `Assets/_Project/Scripts/Save/SaveTransportValidation.cs`

### Assets/_Project/Scripts/UI
- `Assets/_Project/Scripts/UI/BetaProgress.cs`
- `Assets/_Project/Scripts/UI/BuildScreen.cs`
- `Assets/_Project/Scripts/UI/CharacterPortraits.cs`
- `Assets/_Project/Scripts/UI/CommandCard.cs`
- `Assets/_Project/Scripts/UI/CommanderAcquisitionPanel.cs`
- `Assets/_Project/Scripts/UI/DetailTabs.cs`
- `Assets/_Project/Scripts/UI/EnemyAlert.cs`
- `Assets/_Project/Scripts/UI/GameHotkeys.cs`
- `Assets/_Project/Scripts/UI/GameMenuCampaign.cs`
- `Assets/_Project/Scripts/UI/GameMenuCivicServices.cs`
- `Assets/_Project/Scripts/UI/GameMenuConscription.cs`
- `Assets/_Project/Scripts/UI/GameMenuController.cs`
- `Assets/_Project/Scripts/UI/GameMenuCrashStart.cs`
- `Assets/_Project/Scripts/UI/GameMenuDock.cs`
- `Assets/_Project/Scripts/UI/GameMenuFront.cs`
- `Assets/_Project/Scripts/UI/GameMenuKeys.cs`
- `Assets/_Project/Scripts/UI/GameMenuManagement.cs`
- `Assets/_Project/Scripts/UI/GameMenuResearch.cs`
- `Assets/_Project/Scripts/UI/GameMenuRoster.cs`
- `Assets/_Project/Scripts/UI/GameMenuScienceBuildings.cs`
- `Assets/_Project/Scripts/UI/GameMenuSettings.cs`
- `Assets/_Project/Scripts/UI/GameMenuTasks.cs`
- `Assets/_Project/Scripts/UI/GameNotifications.cs`
- `Assets/_Project/Scripts/UI/GatherDesignation.cs`
- `Assets/_Project/Scripts/UI/HUDController.cs`
- `Assets/_Project/Scripts/UI/HudClock.cs`
- `Assets/_Project/Scripts/UI/HudConsole.cs`
- `Assets/_Project/Scripts/UI/HudOverview.cs`
- `Assets/_Project/Scripts/UI/HudResponsiveLayout.cs`
- `Assets/_Project/Scripts/UI/MenuLayout.cs`
- `Assets/_Project/Scripts/UI/MenuTheme.cs`
- `Assets/_Project/Scripts/UI/RosterBar.cs`
- `Assets/_Project/Scripts/UI/SelectedUnitPanel.cs`
- `Assets/_Project/Scripts/UI/ThreatIncidents.cs`
- `Assets/_Project/Scripts/UI/TraitChip.cs`
- `Assets/_Project/Scripts/UI/WorkTargetPanel.cs`
- `Assets/_Project/Scripts/UI/WorldMapPanel.cs`
- `Assets/_Project/Scripts/UI/WorldPlanet.cs`

### Assets/_Project/Scripts/Units
- `Assets/_Project/Scripts/Units/AntVisual.cs`
- `Assets/_Project/Scripts/Units/AttackMoveController.cs`
- `Assets/_Project/Scripts/Units/CivilianVisual.cs`
- `Assets/_Project/Scripts/Units/CommanderAdvancedSkills.cs`
- `Assets/_Project/Scripts/Units/CommanderAnt.cs`
- `Assets/_Project/Scripts/Units/CommanderBossEquipment.cs`
- `Assets/_Project/Scripts/Units/CommanderBossWork.cs`
- `Assets/_Project/Scripts/Units/CommanderDeparture.cs`
- `Assets/_Project/Scripts/Units/CommanderDuty.cs`
- `Assets/_Project/Scripts/Units/CommanderFormation.cs`
- `Assets/_Project/Scripts/Units/CommanderHealthCare.cs`
- `Assets/_Project/Scripts/Units/CommanderHusbandry.cs`
- `Assets/_Project/Scripts/Units/CommanderMeal.cs`
- `Assets/_Project/Scripts/Units/CommanderMealSources.cs`
- `Assets/_Project/Scripts/Units/CommanderPersonal.cs`
- `Assets/_Project/Scripts/Units/CommanderPersonalState.cs`
- `Assets/_Project/Scripts/Units/CommanderTraits.cs`
- `Assets/_Project/Scripts/Units/FormationRules.cs`
- `Assets/_Project/Scripts/Units/GatherOrderFeedback.cs`
- `Assets/_Project/Scripts/Units/KemonoModel.cs`
- `Assets/_Project/Scripts/Units/RigAnimator.cs`
- `Assets/_Project/Scripts/Units/SelectionManager.cs`
- `Assets/_Project/Scripts/Units/SoldierAnt.cs`
- `Assets/_Project/Scripts/Units/StartingAppearance.cs`
- `Assets/_Project/Scripts/Units/UnitSelectionController.cs`
- `Assets/_Project/Scripts/Units/WorkPriorities.cs`
- `Assets/_Project/Scripts/Units/WorkerAnt.cs`

### Assets/_Project/Scripts/World
- `Assets/_Project/Scripts/World/BossRewardTransfer.cs`
- `Assets/_Project/Scripts/World/BossStronghold.cs`
- `Assets/_Project/Scripts/World/ColonyEvents.cs`
- `Assets/_Project/Scripts/World/ColonyInvasion.cs`
- `Assets/_Project/Scripts/World/Corpse.cs`
- `Assets/_Project/Scripts/World/CorpseProp.cs`
- `Assets/_Project/Scripts/World/Critter.cs`
- `Assets/_Project/Scripts/World/CritterBehaviour.cs`
- `Assets/_Project/Scripts/World/CritterVisual.cs`
- `Assets/_Project/Scripts/World/DiplomacyManager.cs`
- `Assets/_Project/Scripts/World/DiplomacyPrisoners.cs`
- `Assets/_Project/Scripts/World/DiplomacyRaids.cs`
- `Assets/_Project/Scripts/World/DiplomacyTrade.cs`
- `Assets/_Project/Scripts/World/EnemyColony.cs`
- `Assets/_Project/Scripts/World/EquipmentLoot.cs`
- `Assets/_Project/Scripts/World/EventActor.cs`
- `Assets/_Project/Scripts/World/ExpeditionBodies.cs`
- `Assets/_Project/Scripts/World/ExpeditionSite.cs`
- `Assets/_Project/Scripts/World/ExpeditionSupplies.cs`
- `Assets/_Project/Scripts/World/ExpeditionTransport.Orders.cs`
- `Assets/_Project/Scripts/World/ExpeditionTransport.cs`
- `Assets/_Project/Scripts/World/ExpeditionWithdraw.cs`
- `Assets/_Project/Scripts/World/ProductionProp.cs`
- `Assets/_Project/Scripts/World/ResourceNode.cs`
- `Assets/_Project/Scripts/World/ResourceNodeStatus.cs`
- `Assets/_Project/Scripts/World/ResourceVisual.cs`
- `Assets/_Project/Scripts/World/TransportDamage.cs`
- `Assets/_Project/Scripts/World/TransportRoute.cs`
- `Assets/_Project/Scripts/World/WildMonster.cs`
- `Assets/_Project/Scripts/World/WorldMapManager.cs`
