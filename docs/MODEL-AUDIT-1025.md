# 모델 연결·축 감사 — F3 / 2026-10-25

- 대상: 최신 미커밋 worktree `E:/Git/ant-wt/F3`. 자산이 없을 때만 `E:/Git/ant`에서 읽었다. Unity 실행·캡처·FBX 생성·원본 자산 수정·git add/commit 없음. UI 수정 없음.
- 결론: **Hut 매핑/파일은 정상, 시설 표시 코드의 임포트 루트 회전 삭제는 오류**다. 시설 표시 공통 코드만 수정했다. 화면에서 초가집처럼 보이지 않은 현상을 설명하는 정적 원인이지만, 현재 Unity 임포트 결과와 실제 화면은 Claude 확인 전이다.
- 전수 범위: BuildingKind **121종**(120개 매핑, DigSite만 의도적 제외), Facilities FBX **118개**, Kemono FBX **85개**(캐릭터 1·손 도구 71·소품 13), Art 애니메이션 FBX **88개**. 바이너리 FBX 총 **291개**. Facilities는 worktree에 모두 있고, 나머지 173개는 원본에서 읽었다.
- 연결된 시설 FBX 누락 **0**, 해당 Resources 경로 중복 **0**, Hut 원본/키트/반입 파일 SHA-256 일치. 키 오타나 다른 시설로 자동 대체하는 분기는 없다.

## Hut 원인과 수정

1. `RuntimeBuildingTemplates`는 `BuildingKind.Hut`를 “초가집”으로 만들고, `FacilityVisual.Models`는 정확히 `CC_Hut`를 고른다. `resident-kit/facility-points.json`도 `building_kind: Hut`, `name_ko: 초가집`이다.
2. 실제 FBX Model 노드: `CC_Hut`, `CC_Hut__WorkPoint_0`, `Hut wall`, `Hut door`, `Thatch roof`. 제작 코드 `resident-kit/scripts/models_extra.py:493`은 흙 원통 벽·원뿔 초가 지붕·문을 생성한다. 현대식 기와/박공지붕 모양을 기대했다면 현재 자산은 원형 원뿔 초가집이라는 점도 확인할 것. 임의로 House로 바꾸지 않았다.
3. Hut의 FBX 루트 `Lcl Rotation=(-90.000009,0,0)`인데, 기존 `FacilityVisual.Start()`는 인스턴스 회전을 `identity`로 초기화한 뒤 건물 회전으로 다시 덮었다. 임포트가 루트 Transform에 보존한 축 변환을 잃게 되며, 그 상태의 bounds로 크기/바닥을 계산한다. 118개 시설 중 117개가 같은 FBX 루트 회전 패턴이므로 Hut만의 문제가 아니다. ManualWell은 원본 루트 0°라 같은 -90°를 강제로 넣으면 오히려 틀린다.
4. 수정: 원래 회전·배율을 저장하고 위치만 0으로 옮겨 bounds를 잰다. 최종 회전은 `building.rotation * importedRotation`, 배율도 원본 배율을 보존한다. 모든 모델에 -90°를 하드코딩하거나 meta를 일괄 변경하지 않는다.
5. `FacilityVisualChecks`에 모든 배치 모델의 최종 회전이 `building.rotation * prefab.localRotation`과 같은지 확인하는 회귀 assertion을 추가했다. 기존 90° yaw 배치·바닥·footprint 검사와 함께 실행해야 한다. **이번에는 컴파일만 했으며 검사 실행은 하지 않았다.**

Hut SHA-256: `7bd921cfc46085c7a4668ef15693b670f95546a70ec42ca861fd11bfc6a03317`.
일치 경로: worktree/원본 `Assets/Resources/Facilities/CC_Hut.fbx`, 원본 `art/resident-kit/exports/facilities/CC_Hut.fbx`.

## 축·임포트 판독 기준

- 아래 `G`는 각 FBX에서 실제 읽은 GlobalSettings: `UpAxis=1/+1 (Y)`, `FrontAxis=2/+1 (Z)`, `CoordAxis=0/+1 (X)`, `UnitScaleFactor=100`. Blender exporter 옵션은 `axis_forward=-Z`, `axis_up=Y`. FBX FrontAxis 부호를 exporter의 forward 옵션과 같은 의미로 읽으면 안 된다. 헤더만으로 Unity에서 보이는 앞면 방향을 확정하지 않는다.
- `R`은 FBX 최상위 Model의 `Lcl Rotation`(XYZ°, 생략 시 0). 파일의 G와 별개로 객체 변환을 봐야 한다. Unity가 생성하는 prefab 루트와 raw FBX Model 노드가 항상 동일하다고 가정하지 않는다.
- **291개 모두** `.meta`에서 `globalScale=1`, `bakeAxisConversion=0`, `useFileScale=1`, `useFileUnits=1`, `preserveHierarchy=0`. 아래 표의 `M`은 이 조합이다. importAnimation만 차이가 있어 개별 표기한다. 축 meta의 불일치는 발견하지 못했으며 수정하지 않았다. 단위 계수 100을 곧바로 Unity의 100배 오류라고 해석하지 않는다.
- resident 시설 exporter는 `FBX_SCALE_UNITS`, production exporter는 `FBX_SCALE_ALL`. 결과 파일의 단위 값은 같고, 본 코드는 bounds로 표시 크기를 정규화한다. 출처별 옵션 차이만으로 meta 배율 오류라고 판정하지 않았다.
- `F`: 시설 코드 **회전 삭제→임포트 회전·배율 보존으로 수정**. `P`: ProductionProp에서 원래 회전을 보존, 수정 없음. `H`: Hold가 소켓 아래에서 localRotation=identity로 덮음, **확인 필요**. `K`: 캐릭터 +90°X Upright·클립 루트 -90° 관계 **확인 필요**. `A`: Generic 클립은 ConfigureModel에서 축 관련 값을 명시하지 않으며 lockRootRotation/Height/Position=true; 실재생 확인 필요.

## 건물 연결 전수

존재의 `F3`는 worktree FBX 있음. 모든 매핑은 `Resources/Facilities/<FBX>` 경로이며 확장자는 `.fbx`. `resident`는 facility-points/제작 목록, `production`은 production-kit manifest/export-list와 대조했다. 이름이 다른 정상 별칭을 키 오타로 처리하지 않았다.

| 키 | FBX | 존재·출처 | 축 | 코드 보정 | 판정 | 수정 |
|---|---|---|---|---|---|---|
| QueenChamber | CC_Stockpile | F3 / resident | G; R=-90,0,0 | F | 정상: 삭제된 여왕방 enum을 비축더미로 재사용 | 공통 코드 |
| Barracks | CC_Barracks | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Storage | CC_Storage | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| DigSite | — | 없음 | — | — | 의도적 제외: 미사용 DigSite, 기본 표시 유지 | 없음 |
| ResearchLab | CC_ResearchLab | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Farm | CC_FarmTile | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Nursery | CC_Nursery | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| ScoutPost | CC_ScoutPost | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| PrisonerCamp | CC_PrisonerCamp | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| ScienceLab | CC_ResearchDesk | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| AcidTower | CC_AcidTower | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| AirshipYard | CC_AirshipYard | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Infirmary | CC_CommanderBed | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| SoilWall | CC_SoilWall | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| TrapPit | CC_StickyTrap | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| AreaAcidTower | CC_AreaAcidTower | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Watchtower | CC_Watchtower | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| MineField | CC_MineField | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| DefenseLab | CC_DefenseLab | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| RestRoom | CC_RestRoom | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Workshop | CC_Workshop | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| ConscriptionPost | CC_ConscriptionPost | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Dormitory | CC_Dormitory | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Kitchen | CC_Kitchen | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| FlowerPot | CC_FlowerPot | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| ShellDecoration | CC_ShellDecoration | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| MarbleMosaic | CC_MarbleMosaic | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| BottleMobile | CC_BottleMobile | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| FireflyLamp | CC_FireflyLamp | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Campfire | CC_Campfire | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| GamblingDen | CC_GamblingDen | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Hut | CC_Hut | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| House | CC_House | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Apartment | CC_Apartment | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| LeafWall | CC_LeafWall | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| CapWall | CC_CapWall | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Door | CC_DoorFrame | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| CastleWall | CC_CastleWall | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Gate | CC_CastleGate | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Toilet | CC_Toilet | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Washbasin | CC_Washbasin | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Shower | CC_Shower | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Treadmill | CC_Treadmill | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| WoodGenerator | CC_WoodGenerator | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| PowerWire | CC_PowerWire | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Battery | CC_Battery | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| ElectricLamp | CC_Lamp | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Hearth | CC_Hearth | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| SleepingMat | CC_SleepingMat | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| DoubleBed | CC_DoubleBed | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Floor | CC_Floor | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| LockedDoor | CC_LockedDoor | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| BarredDoor | CC_BarredDoor | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| SingleBed | CC_SingleBed | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Bookshelf | CC_Bookshelf | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Bathtub | CC_Bathtub | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| MushroomFarm | CC_MushroomFarm | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| AphidPen | CC_AphidPen | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| FoodStore | CC_FoodStore | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Jar | CC_Jar | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Armory | CC_Armory | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| BunkBed | CC_BunkBed | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Hammock | CC_Hammock | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| BigTable | CC_DiningTable | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| BoardGame | CC_BoardGame | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Janggi | CC_Janggi | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Baduk | CC_Baduk | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| WrestlingRing | CC_WrestlingRing | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| DartBoard | CC_DartBoard | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| ExerciseRig | CC_ExerciseRig | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Instrument | CC_Instrument | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| WebSwing | CC_WebSwing | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Statue | CC_Statue | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Flag | CC_Flag | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Painting | CC_Painting | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Pillar | CC_Pillar | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Brazier | CC_Brazier | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Lantern | CC_Lantern | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Torch | CC_Torch | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Carpet | CC_Carpet | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Tapestry | CC_Tapestry | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Monument | CC_Monument | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| BronzeStatue | CC_BronzeStatue | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| TrophyCase | CC_TrophyCase | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| CuriosDisplay | CC_CuriosDisplay | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| HotSpring | CC_HotSpring | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| BanquetTable | CC_BanquetTable | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| SpikeTrap | CC_SpikeTrap | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| AdminDesk | CC_AdminDesk | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Carpentry | CC_Carpentry | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Stonecutter | CC_Stonecutter | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Kiln | CC_Kiln | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| SpinningWheel | CC_SpinningWheel | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Smelter | CC_Smelter | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Pen | CC_RanchFence | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| StrongPen | CC_RanchFence | F3 / resident | G; R=-90,0,0 | F | 공용 모델: 튼튼한 우리 전용 외형 없음(확인 필요) | 공통 코드 |
| Feeder | CC_Feeder | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| CareStation | CC_CareStation | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| AnimalClinic | CC_AnimalTreatment | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| ButcherTable | CC_ButcherTable | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| MedicineBench | CC_Apothecary | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Fridge | CC_Refrigerator | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Freezer | CC_Freezer | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| CompostBin | CC_CompostBin | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| Tombstone | CC_Tombstone | F3 / resident | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| CrashWreck | CC_CrashWreck | F3 / colony-core/기타 | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| CivicPublicService | CC_CivicPublicService | F3 / colony-core/기타 | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| School | CC_School | F3 / colony-core/기타 | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| CitizenClinic | CC_CitizenClinic | F3 / colony-core/기타 | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| PlanterBed | CC_PlanterBed | F3 / colony-core/기타 | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| StarterMushroomBed | CC_MushroomFarm | F3 / resident | G; R=-90,0,0 | F | 공용 모델: 시작 버섯밭/버섯밭 구분 없음(확인 필요) | 공통 코드 |
| WaterSource | CC_WaterSource | F3 / colony-core/기타 | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| OilDeposit | CC_Deposit_Oil_Normal | F3 / production | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| ManualWell | CC_ManualWell | F3 / 타사 Well 재사용 | G; R=0,0,0 | F | 연결 존재, 양식 적합성 확인 필요 | 공통 코드 |
| WaterPump | CC_WaterPump | F3 / colony-core/기타 | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| FluidPipe | CC_PipeL_Straight | F3 / production | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| FluidTank | CC_LiquidTank | F3 / production | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| SteamGenerator | CC_SteamGenerator | F3 / colony-core/기타 | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| OilDrill | CC_OilDerrick | F3 / production | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| OilRefinery | CC_Refinery | F3 / production | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |
| FuelCharger | CC_FillingStation | F3 / production | G; R=-90,0,0 | F | 연결 정상 | 공통 코드 |

## 폴백·캐릭터·소품 경로

| 경로/키 | 현재 동작 | 판정·수정 |
|---|---|---|
| FacilityVisual.Attach/Start | 매핑 없음/로드 실패면 기존 상자 유지; MeshFilter/Renderer 없음이면 모델 표시 안 함 | 다른 FBX로 자동 폴백하지 않음. 전수 FBX 존재와 씬에서 실제 모델이 표시됨은 별개 |
| KemonoModel.Available | 캐릭터와 KemonoAnimator가 둘 다 있어야 true | F3에는 Kemono 자산/컨트롤러 없음, 원본에는 존재. F3에서 실행하면 QuirkyAnt 폴백 조건에 해당; 실제 실행하지 않음 |
| AntVisual | Kemono 없으면 Resources/QuirkyAnt, 둘 다 없으면 Attach 생략 | 의도적 폴백. AntVisual.Face는 빈 pivot만 회전시켜 FBX 내부 축은 유지 |
| KemonoModel.Create | pivot → Upright(+90X) → imported character; Generic clips | 기존 주석의 실측 주장만으로 재수정하지 않음. 정지/재생/전환에 따라 이중 보정 여부 확인 필요 |
| KemonoModel.Hold | Handheld를 Tool.R/L 소켓에 연결하고 localRotation=identity | 소켓은 이미 rig 축 안에 있으므로 월드 소품과 달리 보정 제거가 필요할 수 있음. 파일 축만으로 오류 단정 불가, 미수정 |
| KemonoAppearance | 부품·표정·갑옷 Renderer 선택/색만 변경 | 회전 보정 없음. 외형 조합별 실제 동작 관통 검증 별도 |
| ProductionProp.Attach | imported rotation 유지, bounds로 정규화/접지 | F와 달리 회전 초기화 없음. 정상 정적 경로, 미수정 |
| CorpseProp | Ant→Prop_CorpseBundle, Remains→Prop_Remains, 그 외 기존 표시 | 누락 키 없음. 사망 캐릭터 복제는 pivot/내부 회전 유지; 소품으로 교체되는 시점 시각 검증 필요 |
| BossRewardVisual | index 0..3→BossCore A..D, 4..6→Detector B..D | 7키 모두 파일 존재. 계정 수량/현장 표시 조건은 변경하지 않음 |
| KemonoImport | 캐릭터·88개 클립·손 도구·소품·신규 시설15종 Configure/ConvertMaterials | 기존 resident 시설 전체 재설정 명령은 아님. 현재 118시설 중 importAnimation=1인 것들도 존재하지만 클립 없는 모델에서 이것만으로 회전 버그라 단정 못함 |

원본 소품/손 도구는 존재하지만 F3에는 없으므로 `P/H` 표의 “정상”은 경로·파일 대조 결과이며 F3 실행 성공을 의미하지 않는다. 원본 자산을 새로 복사하거나 임포트하지 않았다.

## Kemono·소품·클립 FBX 전수

`원본`은 해당 파일이 F3에 없고 원본 프로젝트에서만 확인됐다는 뜻이다. 축은 모두 G/M이며 R을 개별 기재했다. 클립의 상태 연결은 아래 별도 검사를 참고한다. 도구 중 코드에서 직접 쓰지 않는 등급·종류는 누락이 아니라 미연결로 표시했다.

| 키 | FBX | 존재 | 축·meta | 코드 보정 | 판정 | 수정 |
|---|---|---|---|---|---|---|
| Ammo_Arrow | Resources/Kemono/Handheld/Ammo_Arrow.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 직접 연결; 소켓 방향 확인 필요 | 없음 |
| Scabbard_Bronze | Resources/Kemono/Handheld/Scabbard_Bronze.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Scabbard_Copper | Resources/Kemono/Handheld/Scabbard_Copper.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Scabbard_Iron | Resources/Kemono/Handheld/Scabbard_Iron.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Scabbard_Starlight | Resources/Kemono/Handheld/Scabbard_Starlight.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Scabbard_Steel | Resources/Kemono/Handheld/Scabbard_Steel.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Scabbard_Titanium | Resources/Kemono/Handheld/Scabbard_Titanium.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Axe_Bronze | Resources/Kemono/Handheld/Tool_Axe_Bronze.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Axe_Copper | Resources/Kemono/Handheld/Tool_Axe_Copper.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Axe_Iron | Resources/Kemono/Handheld/Tool_Axe_Iron.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Axe_Starlight | Resources/Kemono/Handheld/Tool_Axe_Starlight.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Axe_Steel | Resources/Kemono/Handheld/Tool_Axe_Steel.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Axe_Stone | Resources/Kemono/Handheld/Tool_Axe_Stone.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 직접 연결; 소켓 방향 확인 필요 | 없음 |
| Tool_Axe_Titanium | Resources/Kemono/Handheld/Tool_Axe_Titanium.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Hammer_Bronze | Resources/Kemono/Handheld/Tool_Hammer_Bronze.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Hammer_Copper | Resources/Kemono/Handheld/Tool_Hammer_Copper.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Hammer_Iron | Resources/Kemono/Handheld/Tool_Hammer_Iron.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Hammer_Starlight | Resources/Kemono/Handheld/Tool_Hammer_Starlight.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Hammer_Steel | Resources/Kemono/Handheld/Tool_Hammer_Steel.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Hammer_Stone | Resources/Kemono/Handheld/Tool_Hammer_Stone.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 직접 연결; 소켓 방향 확인 필요 | 없음 |
| Tool_Hammer_Titanium | Resources/Kemono/Handheld/Tool_Hammer_Titanium.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Hoe_Bronze | Resources/Kemono/Handheld/Tool_Hoe_Bronze.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Hoe_Copper | Resources/Kemono/Handheld/Tool_Hoe_Copper.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Hoe_Iron | Resources/Kemono/Handheld/Tool_Hoe_Iron.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Hoe_Starlight | Resources/Kemono/Handheld/Tool_Hoe_Starlight.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Hoe_Steel | Resources/Kemono/Handheld/Tool_Hoe_Steel.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Hoe_Stone | Resources/Kemono/Handheld/Tool_Hoe_Stone.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Hoe_Titanium | Resources/Kemono/Handheld/Tool_Hoe_Titanium.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Pickaxe_Bronze | Resources/Kemono/Handheld/Tool_Pickaxe_Bronze.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Pickaxe_Copper | Resources/Kemono/Handheld/Tool_Pickaxe_Copper.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Pickaxe_Iron | Resources/Kemono/Handheld/Tool_Pickaxe_Iron.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Pickaxe_Starlight | Resources/Kemono/Handheld/Tool_Pickaxe_Starlight.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Pickaxe_Steel | Resources/Kemono/Handheld/Tool_Pickaxe_Steel.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Pickaxe_Stone | Resources/Kemono/Handheld/Tool_Pickaxe_Stone.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 직접 연결; 소켓 방향 확인 필요 | 없음 |
| Tool_Pickaxe_Titanium | Resources/Kemono/Handheld/Tool_Pickaxe_Titanium.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Shovel_Bronze | Resources/Kemono/Handheld/Tool_Shovel_Bronze.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Shovel_Copper | Resources/Kemono/Handheld/Tool_Shovel_Copper.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Shovel_Iron | Resources/Kemono/Handheld/Tool_Shovel_Iron.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Shovel_Starlight | Resources/Kemono/Handheld/Tool_Shovel_Starlight.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Shovel_Steel | Resources/Kemono/Handheld/Tool_Shovel_Steel.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Shovel_Stone | Resources/Kemono/Handheld/Tool_Shovel_Stone.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 직접 연결; 소켓 방향 확인 필요 | 없음 |
| Tool_Shovel_Titanium | Resources/Kemono/Handheld/Tool_Shovel_Titanium.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Sickle_Bronze | Resources/Kemono/Handheld/Tool_Sickle_Bronze.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Sickle_Copper | Resources/Kemono/Handheld/Tool_Sickle_Copper.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Sickle_Iron | Resources/Kemono/Handheld/Tool_Sickle_Iron.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Sickle_Starlight | Resources/Kemono/Handheld/Tool_Sickle_Starlight.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Sickle_Steel | Resources/Kemono/Handheld/Tool_Sickle_Steel.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Tool_Sickle_Stone | Resources/Kemono/Handheld/Tool_Sickle_Stone.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 직접 연결; 소켓 방향 확인 필요 | 없음 |
| Tool_Sickle_Titanium | Resources/Kemono/Handheld/Tool_Sickle_Titanium.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_BossA_Guardblade | Resources/Kemono/Handheld/Weapon_BossA_Guardblade.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_BossB_HeatShield | Resources/Kemono/Handheld/Weapon_BossB_HeatShield.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_BossC_Quakehammer | Resources/Kemono/Handheld/Weapon_BossC_Quakehammer.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Shield_Bronze | Resources/Kemono/Handheld/Weapon_Shield_Bronze.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Shield_Copper | Resources/Kemono/Handheld/Weapon_Shield_Copper.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Shield_Iron | Resources/Kemono/Handheld/Weapon_Shield_Iron.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Shield_Starlight | Resources/Kemono/Handheld/Weapon_Shield_Starlight.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Shield_Steel | Resources/Kemono/Handheld/Weapon_Shield_Steel.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Shield_Titanium | Resources/Kemono/Handheld/Weapon_Shield_Titanium.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_ShortBow | Resources/Kemono/Handheld/Weapon_ShortBow.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 직접 연결; 소켓 방향 확인 필요 | 없음 |
| Weapon_Spear_Bronze | Resources/Kemono/Handheld/Weapon_Spear_Bronze.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Spear_Copper | Resources/Kemono/Handheld/Weapon_Spear_Copper.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Spear_Iron | Resources/Kemono/Handheld/Weapon_Spear_Iron.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Spear_Starlight | Resources/Kemono/Handheld/Weapon_Spear_Starlight.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Spear_Steel | Resources/Kemono/Handheld/Weapon_Spear_Steel.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Spear_Titanium | Resources/Kemono/Handheld/Weapon_Spear_Titanium.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Sword_Bronze | Resources/Kemono/Handheld/Weapon_Sword_Bronze.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Sword_Copper | Resources/Kemono/Handheld/Weapon_Sword_Copper.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 직접 연결; 소켓 방향 확인 필요 | 없음 |
| Weapon_Sword_Iron | Resources/Kemono/Handheld/Weapon_Sword_Iron.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Sword_Starlight | Resources/Kemono/Handheld/Weapon_Sword_Starlight.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Sword_Steel | Resources/Kemono/Handheld/Weapon_Sword_Steel.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| Weapon_Sword_Titanium | Resources/Kemono/Handheld/Weapon_Sword_Titanium.fbx | 원본 | G/M; R=-90,0,0 | H; anim=0 | 현재 Hold 호출 키 없음; 확인 필요 | 없음 |
| KemonoModel.Create | Resources/Kemono/Kemono_Character.fbx | 원본 | G/M; R=-90,0,0 | K; anim=0 | 캐릭터 / Upright 확인 필요 | 없음 |
| boss A | Resources/Kemono/Props/Prop_BossCore_A.fbx | 원본 | G/M; R=-90,0,0 | P; anim=0 | 연결 정상 / 시각 미검증 | 없음 |
| boss B | Resources/Kemono/Props/Prop_BossCore_B.fbx | 원본 | G/M; R=-90,0,0 | P; anim=0 | 연결 정상 / 시각 미검증 | 없음 |
| boss C | Resources/Kemono/Props/Prop_BossCore_C.fbx | 원본 | G/M; R=-90,0,0 | P; anim=0 | 연결 정상 / 시각 미검증 | 없음 |
| boss D | Resources/Kemono/Props/Prop_BossCore_D.fbx | 원본 | G/M; R=-90,0,0 | P; anim=0 | 연결 정상 / 시각 미검증 | 없음 |
| CorpseKind.Ant | Resources/Kemono/Props/Prop_CorpseBundle.fbx | 원본 | G/M; R=-90,0,0 | P; anim=0 | 연결 정상 / 시각 미검증 | 없음 |
| detector B | Resources/Kemono/Props/Prop_Detector_B.fbx | 원본 | G/M; R=-90,0,0 | P; anim=0 | 연결 정상 / 시각 미검증 | 없음 |
| detector C | Resources/Kemono/Props/Prop_Detector_C.fbx | 원본 | G/M; R=-90,0,0 | P; anim=0 | 연결 정상 / 시각 미검증 | 없음 |
| detector D | Resources/Kemono/Props/Prop_Detector_D.fbx | 원본 | G/M; R=-90,0,0 | P; anim=0 | 연결 정상 / 시각 미검증 | 없음 |
| GasCylinder | Resources/Kemono/Props/Prop_GasCanister.fbx | 원본 | G/M; R=-90,0,0 | P; anim=0 | 연결 정상 / 시각 미검증 | 없음 |
| Drum/FilledFuelDrum | Resources/Kemono/Props/Prop_OilDrum.fbx | 원본 | G/M; R=-90,0,0 | P; anim=0 | 연결 정상 / 시각 미검증 | 없음 |
| Food | Resources/Kemono/Props/Prop_PreservedRation.fbx | 원본 | G/M; R=-90,0,0 | P; anim=0 | 연결 정상 / 시각 미검증 | 없음 |
| Remains | Resources/Kemono/Props/Prop_Remains.fbx | 원본 | G/M; R=-90,0,0 | P; anim=0 | 연결 정상 / 시각 미검증 | 없음 |
| Stone | Resources/Kemono/Props/Prop_StoneChunk.fbx | 원본 | G/M; R=-90,0,0 | P; anim=0 | 연결 정상 / 시각 미검증 | 없음 |
| Attack | _Project/Art/Kemono/Anim/Kemono@Attack.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Bathe | _Project/Art/Kemono/Anim/Kemono@Bathe.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| BenchWork | _Project/Art/Kemono/Anim/Kemono@BenchWork.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Captive | _Project/Art/Kemono/Anim/Kemono@Captive.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Carry | _Project/Art/Kemono/Anim/Kemono@Carry.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| CarryPerson | _Project/Art/Kemono/Anim/Kemono@CarryPerson.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Chop | _Project/Art/Kemono/Anim/Kemono@Chop.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Death | _Project/Art/Kemono/Anim/Kemono@Death.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Downed | _Project/Art/Kemono/Anim/Kemono@Downed.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Eat | _Project/Art/Kemono/Anim/Kemono@Eat.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Exercise | _Project/Art/Kemono/Anim/Kemono@Exercise.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Fly | _Project/Art/Kemono/Anim/Kemono@Fly.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Grind | _Project/Art/Kemono/Anim/Kemono@Grind.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| GroundWork | _Project/Art/Kemono/Anim/Kemono@GroundWork.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Hammer | _Project/Art/Kemono/Anim/Kemono@Hammer.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Hurt | _Project/Art/Kemono/Anim/Kemono@Hurt.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Idle | _Project/Art/Kemono/Anim/Kemono@Idle.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Inspect | _Project/Art/Kemono/Anim/Kemono@Inspect.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Mine | _Project/Art/Kemono/Anim/Kemono@Mine.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Mourn | _Project/Art/Kemono/Anim/Kemono@Mourn.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Operate | _Project/Art/Kemono/Anim/Kemono@Operate.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_BeatFire | _Project/Art/Kemono/Anim/Kemono@PK_BeatFire.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_BenchPickup | _Project/Art/Kemono/Anim/Kemono@PK_BenchPickup.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_BenchPlace | _Project/Art/Kemono/Anim/Kemono@PK_BenchPlace.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_BowShoot | _Project/Art/Kemono/Anim/Kemono@PK_BowShoot.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_CanisterConnect | _Project/Art/Kemono/Anim/Kemono@PK_CanisterConnect.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_CanisterDisconnect | _Project/Art/Kemono/Anim/Kemono@PK_CanisterDisconnect.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_CollapseDropWeapon | _Project/Art/Kemono/Anim/Kemono@PK_CollapseDropWeapon.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_ControlPanel | _Project/Art/Kemono/Anim/Kemono@PK_ControlPanel.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_Crave | _Project/Art/Kemono/Anim/Kemono@PK_Crave.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_DrawSword | _Project/Art/Kemono/Anim/Kemono@PK_DrawSword.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_Drink | _Project/Art/Kemono/Anim/Kemono@PK_Drink.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_DriveSeated | _Project/Art/Kemono/Anim/Kemono@PK_DriveSeated.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_EquipFromGround | _Project/Art/Kemono/Anim/Kemono@PK_EquipFromGround.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_HangoverIdle | _Project/Art/Kemono/Anim/Kemono@PK_HangoverIdle.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_Knead | _Project/Art/Kemono/Anim/Kemono@PK_Knead.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_Ladle | _Project/Art/Kemono/Anim/Kemono@PK_Ladle.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_LatrineSquat | _Project/Art/Kemono/Anim/Kemono@PK_LatrineSquat.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_Muster | _Project/Art/Kemono/Anim/Kemono@PK_Muster.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_OvenLoad | _Project/Art/Kemono/Anim/Kemono@PK_OvenLoad.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_PassOut | _Project/Art/Kemono/Anim/Kemono@PK_PassOut.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_PourSack | _Project/Art/Kemono/Anim/Kemono@PK_PourSack.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_PressLever | _Project/Art/Kemono/Anim/Kemono@PK_PressLever.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_SelfTreat | _Project/Art/Kemono/Anim/Kemono@PK_SelfTreat.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_SewRepair | _Project/Art/Kemono/Anim/Kemono@PK_SewRepair.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_SheatheSword | _Project/Art/Kemono/Anim/Kemono@PK_SheatheSword.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_ShieldBlock | _Project/Art/Kemono/Anim/Kemono@PK_ShieldBlock.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_SkyGaze | _Project/Art/Kemono/Anim/Kemono@PK_SkyGaze.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_SmithRepair | _Project/Art/Kemono/Anim/Kemono@PK_SmithRepair.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_SpearThrust | _Project/Art/Kemono/Anim/Kemono@PK_SpearThrust.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_Stagger | _Project/Art/Kemono/Anim/Kemono@PK_Stagger.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_SwordAttack | _Project/Art/Kemono/Anim/Kemono@PK_SwordAttack.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_TapFill | _Project/Art/Kemono/Anim/Kemono@PK_TapFill.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_ToolAway | _Project/Art/Kemono/Anim/Kemono@PK_ToolAway.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_ToolOut | _Project/Art/Kemono/Anim/Kemono@PK_ToolOut.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_Tool_Chop | _Project/Art/Kemono/Anim/Kemono@PK_Tool_Chop.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_Tool_Dig | _Project/Art/Kemono/Anim/Kemono@PK_Tool_Dig.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_Tool_Hammer | _Project/Art/Kemono/Anim/Kemono@PK_Tool_Hammer.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_Tool_Harvest | _Project/Art/Kemono/Anim/Kemono@PK_Tool_Harvest.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_Tool_Mine | _Project/Art/Kemono/Anim/Kemono@PK_Tool_Mine.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_Tool_Till | _Project/Art/Kemono/Anim/Kemono@PK_Tool_Till.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_UnequipToGround | _Project/Art/Kemono/Anim/Kemono@PK_UnequipToGround.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_ValveTurn | _Project/Art/Kemono/Anim/Kemono@PK_ValveTurn.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_WakeUp | _Project/Art/Kemono/Anim/Kemono@PK_WakeUp.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PK_WalkTipsy | _Project/Art/Kemono/Anim/Kemono@PK_WalkTipsy.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Pickup | _Project/Art/Kemono/Anim/Kemono@Pickup.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PlayBoard | _Project/Art/Kemono/Anim/Kemono@PlayBoard.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PlayMusic | _Project/Art/Kemono/Anim/Kemono@PlayMusic.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| PutDown | _Project/Art/Kemono/Anim/Kemono@PutDown.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Rest | _Project/Art/Kemono/Anim/Kemono@Rest.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| RockCradle | _Project/Art/Kemono/Anim/Kemono@RockCradle.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Run | _Project/Art/Kemono/Anim/Kemono@Run.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Saw | _Project/Art/Kemono/Anim/Kemono@Saw.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Sit | _Project/Art/Kemono/Anim/Kemono@Sit.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Sleep | _Project/Art/Kemono/Anim/Kemono@Sleep.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Spin | _Project/Art/Kemono/Anim/Kemono@Spin.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Stamp | _Project/Art/Kemono/Anim/Kemono@Stamp.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| StandEnter | _Project/Art/Kemono/Anim/Kemono@StandEnter.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| StandExit | _Project/Art/Kemono/Anim/Kemono@StandExit.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| StandIdle | _Project/Art/Kemono/Anim/Kemono@StandIdle.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Stir | _Project/Art/Kemono/Anim/Kemono@Stir.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Swing | _Project/Art/Kemono/Anim/Kemono@Swing.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Throw | _Project/Art/Kemono/Anim/Kemono@Throw.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Treat | _Project/Art/Kemono/Anim/Kemono@Treat.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Walk | _Project/Art/Kemono/Anim/Kemono@Walk.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Wash | _Project/Art/Kemono/Anim/Kemono@Wash.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| WaterPlant | _Project/Art/Kemono/Anim/Kemono@WaterPlant.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |
| Wrestle | _Project/Art/Kemono/Anim/Kemono@Wrestle.fbx | 원본 | G/M; R=-90,0,0 | A; anim=1 | 임포터 전체 파일 순회 / 재생 확인 필요 | 없음 |

## Production kit 시설 manifest 전수 대조

manifest의 75개 시설 키를 파일과 코드 매핑에 대조했다. `CC_` 접두사는 반입 파일명용이다. “미연결”은 원본 kit 제작물이 있다는 뜻이며 새 BuildingKind가 구현됐다는 뜻이 아니다. 같은 이름의 구형 resident 모델을 쓰는 경우에는 새 production 모델 연결 완료로 세지 않았다.

| 시설 키 | manifest FBX | 존재 | BuildingKind | 판정 | 수정 |
|---|---|---|---|---|---|
| SewingTable | art\production-kit-20261019\exports\facilities\SewingTable.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| CharcoalKiln | art\production-kit-20261019\exports\facilities\CharcoalKiln.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Blacksmith | art\production-kit-20261019\exports\facilities\Blacksmith.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| CokeOven | art\production-kit-20261019\exports\facilities\CokeOven.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| ElectricWorkshop | art\production-kit-20261019\exports\facilities\ElectricWorkshop.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| TitaniumRefinery | art\production-kit-20261019\exports\facilities\TitaniumRefinery.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| AlloyFurnace | art\production-kit-20261019\exports\facilities\AlloyFurnace.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| ChemistryBench | art\production-kit-20261019\exports\facilities\ChemistryBench.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Mill | art\production-kit-20261019\exports\facilities\Mill.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| OilPress | art\production-kit-20261019\exports\facilities\OilPress.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| BrewingBarrel | art\production-kit-20261019\exports\facilities\BrewingBarrel.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Still | art\production-kit-20261019\exports\facilities\Still.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Greenhouse_Wall | art\production-kit-20261019\exports\facilities\Greenhouse_Wall.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Greenhouse_Corner | art\production-kit-20261019\exports\facilities\Greenhouse_Corner.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Greenhouse_Roof | art\production-kit-20261019\exports\facilities\Greenhouse_Roof.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Greenhouse_Door | art\production-kit-20261019\exports\facilities\Greenhouse_Door.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Greenhouse_Vent | art\production-kit-20261019\exports\facilities\Greenhouse_Vent.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Prospector | art\production-kit-20261019\exports\facilities\Prospector.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| DeepDrill | art\production-kit-20261019\exports\facilities\DeepDrill.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| AdvancedDrill | art\production-kit-20261019\exports\facilities\AdvancedDrill.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| OilDerrick | art\production-kit-20261019\exports\facilities\OilDerrick.fbx | 원본 있음 | OilDrill | production FBX 일치 | 없음 |
| GasExtractor | art\production-kit-20261019\exports\facilities\GasExtractor.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Refinery | art\production-kit-20261019\exports\facilities\Refinery.fbx | 원본 있음 | OilRefinery | production FBX 일치 | 없음 |
| FillingStation | art\production-kit-20261019\exports\facilities\FillingStation.fbx | 원본 있음 | FuelCharger | production FBX 일치 | 없음 |
| GasGenerator | art\production-kit-20261019\exports\facilities\GasGenerator.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| LiquidTank | art\production-kit-20261019\exports\facilities\LiquidTank.fbx | 원본 있음 | FluidTank | production FBX 일치 | 없음 |
| GasTank | art\production-kit-20261019\exports\facilities\GasTank.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| PipeL_Straight | art\production-kit-20261019\exports\facilities\PipeL_Straight.fbx | 원본 있음 | FluidPipe | production FBX 일치 | 없음 |
| PipeL_Corner | art\production-kit-20261019\exports\facilities\PipeL_Corner.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| PipeL_Tee | art\production-kit-20261019\exports\facilities\PipeL_Tee.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| PipeL_Cross | art\production-kit-20261019\exports\facilities\PipeL_Cross.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| PipeL_End | art\production-kit-20261019\exports\facilities\PipeL_End.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| PipeL_Valve | art\production-kit-20261019\exports\facilities\PipeL_Valve.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| PipeL_Riser | art\production-kit-20261019\exports\facilities\PipeL_Riser.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| PipeG_Straight | art\production-kit-20261019\exports\facilities\PipeG_Straight.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| PipeG_Corner | art\production-kit-20261019\exports\facilities\PipeG_Corner.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| PipeG_Tee | art\production-kit-20261019\exports\facilities\PipeG_Tee.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| PipeG_Cross | art\production-kit-20261019\exports\facilities\PipeG_Cross.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| PipeG_End | art\production-kit-20261019\exports\facilities\PipeG_End.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| PipeG_Valve | art\production-kit-20261019\exports\facilities\PipeG_Valve.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| PipeG_Riser | art\production-kit-20261019\exports\facilities\PipeG_Riser.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Coal_Normal | art\production-kit-20261019\exports\facilities\Deposit_Coal_Normal.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Coal_Mining | art\production-kit-20261019\exports\facilities\Deposit_Coal_Mining.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Coal_Depleted | art\production-kit-20261019\exports\facilities\Deposit_Coal_Depleted.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Copper_Normal | art\production-kit-20261019\exports\facilities\Deposit_Copper_Normal.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Copper_Mining | art\production-kit-20261019\exports\facilities\Deposit_Copper_Mining.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Copper_Depleted | art\production-kit-20261019\exports\facilities\Deposit_Copper_Depleted.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Tin_Normal | art\production-kit-20261019\exports\facilities\Deposit_Tin_Normal.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Tin_Mining | art\production-kit-20261019\exports\facilities\Deposit_Tin_Mining.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Tin_Depleted | art\production-kit-20261019\exports\facilities\Deposit_Tin_Depleted.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Iron_Normal | art\production-kit-20261019\exports\facilities\Deposit_Iron_Normal.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Iron_Mining | art\production-kit-20261019\exports\facilities\Deposit_Iron_Mining.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Iron_Depleted | art\production-kit-20261019\exports\facilities\Deposit_Iron_Depleted.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Silver_Normal | art\production-kit-20261019\exports\facilities\Deposit_Silver_Normal.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Silver_Mining | art\production-kit-20261019\exports\facilities\Deposit_Silver_Mining.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Silver_Depleted | art\production-kit-20261019\exports\facilities\Deposit_Silver_Depleted.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Gold_Normal | art\production-kit-20261019\exports\facilities\Deposit_Gold_Normal.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Gold_Mining | art\production-kit-20261019\exports\facilities\Deposit_Gold_Mining.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Gold_Depleted | art\production-kit-20261019\exports\facilities\Deposit_Gold_Depleted.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Titanium_Normal | art\production-kit-20261019\exports\facilities\Deposit_Titanium_Normal.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Titanium_Mining | art\production-kit-20261019\exports\facilities\Deposit_Titanium_Mining.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Titanium_Depleted | art\production-kit-20261019\exports\facilities\Deposit_Titanium_Depleted.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Starlight_Normal | art\production-kit-20261019\exports\facilities\Deposit_Starlight_Normal.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Starlight_Mining | art\production-kit-20261019\exports\facilities\Deposit_Starlight_Mining.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Starlight_Depleted | art\production-kit-20261019\exports\facilities\Deposit_Starlight_Depleted.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Oil_Normal | art\production-kit-20261019\exports\facilities\Deposit_Oil_Normal.fbx | 원본 있음 | OilDeposit | production FBX 일치 | 없음 |
| Deposit_Oil_Mining | art\production-kit-20261019\exports\facilities\Deposit_Oil_Mining.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Oil_Depleted | art\production-kit-20261019\exports\facilities\Deposit_Oil_Depleted.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Gas_Normal | art\production-kit-20261019\exports\facilities\Deposit_Gas_Normal.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Gas_Mining | art\production-kit-20261019\exports\facilities\Deposit_Gas_Mining.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Deposit_Gas_Depleted | art\production-kit-20261019\exports\facilities\Deposit_Gas_Depleted.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Overlay_Edge | art\production-kit-20261019\exports\facilities\Overlay_Edge.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Overlay_Corner | art\production-kit-20261019\exports\facilities\Overlay_Corner.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Overlay_Fill | art\production-kit-20261019\exports\facilities\Overlay_Fill.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |
| Overlay_Pin | art\production-kit-20261019\exports\facilities\Overlay_Pin.fbx | 원본 있음 | — | 미연결(구현 범위 확인 필요) | 없음 |

## 시설 FBX 출처 파일 대조

118개 반입 파일을 같은 stem(`CC_` 무시)의 원본 art FBX 후보와 SHA-256 대조했다. 일치 없음은 파일 내용이 잘못됐다는 뜻이 아니라 출처 확인 필요다. ManualWell처럼 타사 원본 재사용도 포함한다. 전체 파일 축/메타는 앞의 전수표와 같다.

| FBX | 원본 art SHA-256 일치 경로 / 확인 필요 |
|---|---|
| CC_AcidTower.fbx | art/resident-kit/exports/facilities/CC_AcidTower.fbx |
| CC_AdminDesk.fbx | art/resident-kit/exports/facilities/CC_AdminDesk.fbx |
| CC_AirshipYard.fbx | art/resident-kit/exports/facilities/CC_AirshipYard.fbx |
| CC_AnimalTreatment.fbx | art/resident-kit/exports/facilities/CC_AnimalTreatment.fbx |
| CC_Apartment.fbx | art/resident-kit/exports/facilities/CC_Apartment.fbx |
| CC_AphidPen.fbx | art/resident-kit/exports/facilities/CC_AphidPen.fbx |
| CC_Apothecary.fbx | art/resident-kit/exports/facilities/CC_Apothecary.fbx |
| CC_AreaAcidTower.fbx | art/resident-kit/exports/facilities/CC_AreaAcidTower.fbx |
| CC_Armory.fbx | art/resident-kit/exports/facilities/CC_Armory.fbx |
| CC_Baduk.fbx | art/resident-kit/exports/facilities/CC_Baduk.fbx |
| CC_BanquetTable.fbx | art/resident-kit/exports/facilities/CC_BanquetTable.fbx |
| CC_Barracks.fbx | art/resident-kit/exports/facilities/CC_Barracks.fbx |
| CC_BarredDoor.fbx | art/resident-kit/exports/facilities/CC_BarredDoor.fbx |
| CC_Bathtub.fbx | art/resident-kit/exports/facilities/CC_Bathtub.fbx |
| CC_Battery.fbx | art/resident-kit/exports/facilities/CC_Battery.fbx |
| CC_BoardGame.fbx | art/resident-kit/exports/facilities/CC_BoardGame.fbx |
| CC_Bookshelf.fbx | art/resident-kit/exports/facilities/CC_Bookshelf.fbx |
| CC_BottleMobile.fbx | art/resident-kit/exports/facilities/CC_BottleMobile.fbx |
| CC_Brazier.fbx | art/resident-kit/exports/facilities/CC_Brazier.fbx |
| CC_BronzeStatue.fbx | art/resident-kit/exports/facilities/CC_BronzeStatue.fbx |
| CC_BunkBed.fbx | art/resident-kit/exports/facilities/CC_BunkBed.fbx |
| CC_ButcherTable.fbx | art/resident-kit/exports/facilities/CC_ButcherTable.fbx |
| CC_Campfire.fbx | art/resident-kit/exports/facilities/CC_Campfire.fbx |
| CC_CapWall.fbx | art/resident-kit/exports/facilities/CC_CapWall.fbx |
| CC_CareStation.fbx | art/resident-kit/exports/facilities/CC_CareStation.fbx |
| CC_Carpentry.fbx | art/resident-kit/exports/facilities/CC_Carpentry.fbx |
| CC_Carpet.fbx | art/resident-kit/exports/facilities/CC_Carpet.fbx |
| CC_CastleGate.fbx | art/resident-kit/exports/facilities/CC_CastleGate.fbx |
| CC_CastleWall.fbx | art/resident-kit/exports/facilities/CC_CastleWall.fbx |
| CC_CitizenClinic.fbx | art/production-kit-20261019/exports/facilities/CitizenClinic.fbx |
| CC_CivicPublicService.fbx | art/production-kit-20261019/exports/facilities/CivicPublicService.fbx |
| CC_CommanderBed.fbx | art/resident-kit/exports/facilities/CC_CommanderBed.fbx |
| CC_CompostBin.fbx | art/resident-kit/exports/facilities/CC_CompostBin.fbx |
| CC_ConscriptionPost.fbx | art/resident-kit/exports/facilities/CC_ConscriptionPost.fbx |
| CC_CrashWreck.fbx | art/production-kit-20261019/exports/facilities/CrashWreck.fbx |
| CC_CuriosDisplay.fbx | art/resident-kit/exports/facilities/CC_CuriosDisplay.fbx |
| CC_DartBoard.fbx | art/resident-kit/exports/facilities/CC_DartBoard.fbx |
| CC_DefenseLab.fbx | art/resident-kit/exports/facilities/CC_DefenseLab.fbx |
| CC_Deposit_Oil_Normal.fbx | art/production-kit-20261019/exports/facilities/Deposit_Oil_Normal.fbx |
| CC_DiningTable.fbx | art/resident-kit/exports/facilities/CC_DiningTable.fbx |
| CC_DoorFrame.fbx | art/resident-kit/exports/facilities/CC_DoorFrame.fbx |
| CC_Dormitory.fbx | art/resident-kit/exports/facilities/CC_Dormitory.fbx |
| CC_DoubleBed.fbx | art/resident-kit/exports/facilities/CC_DoubleBed.fbx |
| CC_ExerciseRig.fbx | art/resident-kit/exports/facilities/CC_ExerciseRig.fbx |
| CC_FarmTile.fbx | art/resident-kit/exports/facilities/CC_FarmTile.fbx |
| CC_Feeder.fbx | art/resident-kit/exports/facilities/CC_Feeder.fbx |
| CC_FillingStation.fbx | art/production-kit-20261019/exports/facilities/FillingStation.fbx |
| CC_FireflyLamp.fbx | art/resident-kit/exports/facilities/CC_FireflyLamp.fbx |
| CC_Flag.fbx | art/resident-kit/exports/facilities/CC_Flag.fbx |
| CC_Floor.fbx | art/resident-kit/exports/facilities/CC_Floor.fbx |
| CC_FlowerPot.fbx | art/resident-kit/exports/facilities/CC_FlowerPot.fbx |
| CC_FoodStore.fbx | art/resident-kit/exports/facilities/CC_FoodStore.fbx |
| CC_Freezer.fbx | art/resident-kit/exports/facilities/CC_Freezer.fbx |
| CC_GamblingDen.fbx | art/resident-kit/exports/facilities/CC_GamblingDen.fbx |
| CC_Hammock.fbx | art/resident-kit/exports/facilities/CC_Hammock.fbx |
| CC_Hearth.fbx | art/resident-kit/exports/facilities/CC_Hearth.fbx |
| CC_HotSpring.fbx | art/resident-kit/exports/facilities/CC_HotSpring.fbx |
| CC_House.fbx | art/resident-kit/exports/facilities/CC_House.fbx |
| CC_Hut.fbx | art/resident-kit/exports/facilities/CC_Hut.fbx |
| CC_Instrument.fbx | art/resident-kit/exports/facilities/CC_Instrument.fbx |
| CC_Janggi.fbx | art/resident-kit/exports/facilities/CC_Janggi.fbx |
| CC_Jar.fbx | art/resident-kit/exports/facilities/CC_Jar.fbx |
| CC_Kiln.fbx | art/resident-kit/exports/facilities/CC_Kiln.fbx |
| CC_Kitchen.fbx | art/resident-kit/exports/facilities/CC_Kitchen.fbx |
| CC_Lamp.fbx | art/resident-kit/exports/facilities/CC_Lamp.fbx |
| CC_Lantern.fbx | art/resident-kit/exports/facilities/CC_Lantern.fbx |
| CC_LeafWall.fbx | art/resident-kit/exports/facilities/CC_LeafWall.fbx |
| CC_LiquidTank.fbx | art/production-kit-20261019/exports/facilities/LiquidTank.fbx |
| CC_LockedDoor.fbx | art/resident-kit/exports/facilities/CC_LockedDoor.fbx |
| CC_ManualWell.fbx | 동명 art 파일 일치 없음: 출처 확인 필요 |
| CC_MarbleMosaic.fbx | art/resident-kit/exports/facilities/CC_MarbleMosaic.fbx |
| CC_MineField.fbx | art/resident-kit/exports/facilities/CC_MineField.fbx |
| CC_Monument.fbx | art/resident-kit/exports/facilities/CC_Monument.fbx |
| CC_MushroomFarm.fbx | art/resident-kit/exports/facilities/CC_MushroomFarm.fbx |
| CC_Nursery.fbx | art/resident-kit/exports/facilities/CC_Nursery.fbx |
| CC_OilDerrick.fbx | art/production-kit-20261019/exports/facilities/OilDerrick.fbx |
| CC_Painting.fbx | art/resident-kit/exports/facilities/CC_Painting.fbx |
| CC_Pillar.fbx | art/resident-kit/exports/facilities/CC_Pillar.fbx |
| CC_PipeL_Straight.fbx | art/production-kit-20261019/exports/facilities/PipeL_Straight.fbx |
| CC_PlanterBed.fbx | art/production-kit-20261019/exports/facilities/PlanterBed.fbx |
| CC_PowerWire.fbx | art/resident-kit/exports/facilities/CC_PowerWire.fbx |
| CC_PrisonerCamp.fbx | art/resident-kit/exports/facilities/CC_PrisonerCamp.fbx |
| CC_RanchFence.fbx | art/resident-kit/exports/facilities/CC_RanchFence.fbx |
| CC_Refinery.fbx | art/production-kit-20261019/exports/facilities/Refinery.fbx |
| CC_Refrigerator.fbx | art/resident-kit/exports/facilities/CC_Refrigerator.fbx |
| CC_ResearchDesk.fbx | art/resident-kit/exports/facilities/CC_ResearchDesk.fbx |
| CC_ResearchLab.fbx | art/resident-kit/exports/facilities/CC_ResearchLab.fbx |
| CC_RestRoom.fbx | art/resident-kit/exports/facilities/CC_RestRoom.fbx |
| CC_School.fbx | art/production-kit-20261019/exports/facilities/School.fbx |
| CC_ScoutPost.fbx | art/resident-kit/exports/facilities/CC_ScoutPost.fbx |
| CC_ShellDecoration.fbx | art/resident-kit/exports/facilities/CC_ShellDecoration.fbx |
| CC_Shower.fbx | art/resident-kit/exports/facilities/CC_Shower.fbx |
| CC_SingleBed.fbx | art/resident-kit/exports/facilities/CC_SingleBed.fbx |
| CC_SleepingMat.fbx | art/resident-kit/exports/facilities/CC_SleepingMat.fbx |
| CC_Smelter.fbx | art/resident-kit/exports/facilities/CC_Smelter.fbx |
| CC_SoilWall.fbx | art/resident-kit/exports/facilities/CC_SoilWall.fbx |
| CC_SpikeTrap.fbx | art/resident-kit/exports/facilities/CC_SpikeTrap.fbx |
| CC_SpinningWheel.fbx | art/resident-kit/exports/facilities/CC_SpinningWheel.fbx |
| CC_Statue.fbx | art/resident-kit/exports/facilities/CC_Statue.fbx |
| CC_SteamGenerator.fbx | art/production-kit-20261019/exports/facilities/SteamGenerator.fbx |
| CC_StickyTrap.fbx | art/resident-kit/exports/facilities/CC_StickyTrap.fbx |
| CC_Stockpile.fbx | art/resident-kit/exports/facilities/CC_Stockpile.fbx |
| CC_Stonecutter.fbx | art/resident-kit/exports/facilities/CC_Stonecutter.fbx |
| CC_Storage.fbx | art/resident-kit/exports/facilities/CC_Storage.fbx |
| CC_Tapestry.fbx | art/resident-kit/exports/facilities/CC_Tapestry.fbx |
| CC_Toilet.fbx | art/resident-kit/exports/facilities/CC_Toilet.fbx |
| CC_Tombstone.fbx | art/resident-kit/exports/facilities/CC_Tombstone.fbx |
| CC_Torch.fbx | art/resident-kit/exports/facilities/CC_Torch.fbx |
| CC_Treadmill.fbx | art/resident-kit/exports/facilities/CC_Treadmill.fbx |
| CC_TrophyCase.fbx | art/resident-kit/exports/facilities/CC_TrophyCase.fbx |
| CC_Washbasin.fbx | art/resident-kit/exports/facilities/CC_Washbasin.fbx |
| CC_Watchtower.fbx | art/resident-kit/exports/facilities/CC_Watchtower.fbx |
| CC_WaterPump.fbx | art/production-kit-20261019/exports/facilities/WaterPump.fbx |
| CC_WaterSource.fbx | art/production-kit-20261019/exports/facilities/WaterSource.fbx |
| CC_WebSwing.fbx | art/resident-kit/exports/facilities/CC_WebSwing.fbx |
| CC_WoodGenerator.fbx | art/resident-kit/exports/facilities/CC_WoodGenerator.fbx |
| CC_Workshop.fbx | art/resident-kit/exports/facilities/CC_Workshop.fbx |
| CC_WrestlingRing.fbx | art/resident-kit/exports/facilities/CC_WrestlingRing.fbx |

## Claude Unity 육안 확인 목록 (이번 세션 실행 금지 준수)

Animator 정적 연결도 대조했다. 원본 KemonoAnimator의 90개 상태에서 88개 클립 파일 이름에 대응하는 상태가 모두 있고, motion GUID가 알 수 없는 FBX를 가리키는 항목은 0개다. `Idle_A→Idle`, `Hit→Hurt`, `Attack→PK_SwordAttack` 별칭도 정상이다. 각 motion의 내부 fileID 유효성과 실제 애니메이션 출력은 Unity 확인 대상이다.

1. 실제 원본 프로젝트에 수정 코드 반영·컴파일 후 `FacilityVisualChecks.Main()` 실행. Hut/House/Apartment를 나란히 0°·90° yaw로 배치하고 정면과 약간 높은 사선에서 확인. 원형 벽 위 초가 지붕이 위에 있고 문이 수직인지, 기와집/아파트와 실루엣이 구분되는지 확인. Inspector에서 prefab 루트 회전과 최종 Model 회전도 함께 기록.
2. 시설 축 대표: ManualWell(유일한 raw root 0°), ResearchLab, Kitchen/Hearth, SingleBed/DoubleBed, AcidTower/Watchtower, LeafWall/Door/Gate, Farm/MushroomFarm, OilDrill/OilRefinery/FluidTank/FluidPipe/WaterPump/SteamGenerator. 접지·앞면·문·통로 방향·회전 후 footprint 확인. 초가집만 수정됐다고 종료하지 말 것.
3. 공용 외형: Pen/StrongPen, MushroomFarm/StarterMushroomBed가 동일 모델이라는 점을 확인하고 별도 외형 필요 여부 판단. QueenChamber는 여왕방이 아니라 Stockpile. 씬 프리팹/상자 없는 시설에 FacilityVisual이 생략되는지도 구분.
4. 캐릭터: Idle/Walk/Run → Tool_Chop/Mine/Dig/Hammer/Harvest → SwordAttack/BowShoot → Sleep/Downed/Death. +90° Upright와 Generic 루트 보정이 재생 전/후 모두 일관된지 확인. 네 방향 이동에서 머리가 위·발이 아래, 이동 방향과 얼굴 방향이 일치하는지 확인. 갑옷·머리·귀·꼬리·표정도 포함.
5. 손 도구: 오른손 도끼·곡괭이·삽·낫·망치·구리검, 왼손 활/오른손 화살. 손잡이·작업 끝·활 앞면을 확인. H를 바꾸려면 소켓의 월드/로컬 회전과 도구 prefab의 임포트 회전을 먼저 비교. 독립 소품처럼 일괄 -90°를 주지 말 것.
6. 바닥 소품: Food/Stone/Drum/FilledFuelDrum/GasCylinder, 시신→유해, BossCore A/B/C/D·Detector B/C/D 7종. 바닥 관통·누움·접지·개체 방향 확인. 도구 소켓과 다른 P 경로임에 주의.
7. 캡처는 **사용자 SAVE 때만**. 위 상태를 준비해 변경 모델이 실제로 보이는 화면을 캡처하고 직접 확인. 이번 문서는 검수 대상을 제시하며 화면 검증 완료를 주장하지 않는다.

## 검증·범위

- FBX binary 7400 노드 파싱: GlobalSettings의 Properties70, Objects/Model의 Lcl Rotation, Connections/OO로 최상위 모델 판별. 배열 payload는 길이로 건너뛰므로 메시 좌표·스키닝·애니메이션 키의 변형 결과까지 증명하지 않는다. 291개 파일의 축/메타 판독 성공, BuildingKind 121/매핑120/DigSite 제외 assertion 통과.
- Unity 6000.5.8f1 내장 SDK 8.0.318 Roslyn, 원본 Bee `1900b0aE.dag/Assembly-CSharp.rsp`를 기반으로 소스/out/refout/analyzer/generatedfilesout 제거, 참조 절대경로화, worktree Assets runtime 소스 재열거. 무시 자산 `SpinFree.cs`만 원본에서 읽기 입력. **본체 346개 소스 컴파일 exit 0·오류 0**, 기존 BuildingConstructionSite.cs:33/109의 CS0252 경고 2개(관련 없는 코드, 미수정). obsolete CS0618 경고는 숨김.
- Editor rsp의 기존 본체 참조를 새 DLL로 교체해 `FacilityVisualChecks.cs` + `Assets/_Project/Editor/KemonoImport.cs` **컴파일 exit 0·오류 0**. 재현용 입력/로그: `Temp/f3/compile.py`, `Assembly-CSharp.rsp/.log`, `Checks.rsp/.log` (무시 경로, 커밋 대상 아님).
- 감사 재현 중간 파일: `Temp/f3/audit.py`, `assets.json`, `report.py`. **정적 컴파일 성공은 Unity 재생/시각 검사 성공이 아니다.** 캐릭터/도구 축, 현재 씬의 실제 fallback 사용 여부, 모델의 앞면 의미는 확인 필요.
- production manifest의 자원311·작물82·장비18개 목록도 분류/이름 대조 자료로 읽었지만 Assets에 반입되지 않은 전체 제작 자산을 게임 연결 완료로 계산하지 않았다. 신규 자산 반입·기능 구현·FBX 변경은 이번 수정 범위 밖이다.
- `git diff --check` 통과. 기존 수정 파일의 UTF-8(BOM 없음)·CRLF 유지. Graphify AST 갱신 exit 0: 9981 nodes/22783 edges/453 communities. 기존 431개 커뮤니티 라벨과 구조가 달라 허브 이름으로 대체했다는 경고가 있으며 LLM 라벨 재생성은 하지 않았다. graphify 출력/캐시는 자동 생성 변경으로 별도 유지한다.
- 완료 알림용 카카오톡 MemoChat 도구가 이 세션에 없어 전송하지 못했다.
