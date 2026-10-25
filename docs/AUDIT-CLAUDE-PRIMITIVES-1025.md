# Claude 읽기 전용 점검: 기본 도형 노출 경로 (2026-10-25 23:50)

범위: `Assets/_Project/Scripts`의 `GameObject.CreatePrimitive` 31곳 + 자원·건물 모델 연결 코드(ResourceVisual·ProductionProp·FacilityVisual·CorpseProp·BossRewardVisual·AntVisual). 코드 수정·Unity 실행·캡처 없음.
제외: 숨김 충돌 판정용 도형(모델 연결 후 `forceRenderingOff`/renderer 끔), 장수 리그, 진딧물·누에·거미·벌(Critter.cs), c21 흙 표시(Codex 담당).
**한계**: `AntColony.unity`가 바이너리 직렬화라 씬에 직접 배치된 도형은 읽지 못함 → Unity에서 확인 필요(맨 아래).

## 연결 정상(점검 결과 문제 없음)
- 건물: BuildingKind 121종 중 120종이 `FacilityVisual` 매핑, 매핑 ID 전부 `Resources/Facilities`에 존재. 템플릿 큐브(RuntimeBuildingTemplates·CivicService·SupplyBuildingTemplates·Infirmary·AirshipYard·CrashWreck·SaveConstruction 복원)는 BuildingBase→FacilityVisual로 대체.
- 자원: `ResourceVisual`이 참조하는 모델 45개 전부 `Resources/ResourceModels`에 존재. ProductionProp 4종(`Prop_PreservedRation/StoneChunk/OilDrum/GasCanister`) 존재.
- 바닥 물품(WorkerAnt 내려놓기·외교 수령·저장 복원 Saved Loot): ResourceNode→ResourceVisual로 대체(아래 미매핑 종류 제외).
- 시체: CorpseProp(`Prop_Remains`/`Prop_CorpseBundle`). 보스 보상 표시: BossRewardVisual(`Prop_BossCore_*`, `Prop_Detector_*`).
- 의도된 표시용 원판(대체 불필요로 판단): MoveMarker 원기둥, SettlementDefense 착륙 방어 구역 원판, 산성비 범위 원판, 배치 미리보기 큐브, WorldPlanet 대체 구(지구 모델 없을 때만).

## 연결 누락 — 기본 도형이 보이는 경로

| # | 경로(파일:줄) | 보이는 도형 | 원인 | 대체 FBX 후보 | 비고 |
|---|---|---|---|---|---|
| 1 | 자원 종류 `Special`·`Chitin`·`Wax`·`Component`·`Water` (ResourceVisual.ModelFor/CargoModel → null) | 큐브(바닥 물품)·원기둥/큐브(갑각 광맥) | 매핑 없음 | 미발견. `art/production-kit-20261019/exports/resources`에 갑각·밀랍·부품 전용 FBX 없음. 갑각 광맥은 `CC_OreOutcrop`(art만) 임시 후보 | Chitin은 `MaterialDeposits.cs:33` 자연 광맥으로 맵에 생성됨 → 실제 노출 확률 높음. Special은 보스 전리품(#2) |
| 2 | `Boss/BossLoot.cs:60` 보스 전리품 Food/Special | 큐브 | ConfigureLoot→ResourceVisual. Food는 `Prop_PreservedRation`으로 대체되지만 **Special은 매핑 없음** | `Prop_BossCore_A~D`(이미 Resources/Kemono/Props) | |
| 3 | `World/EquipmentLoot.cs:16` 장비 전리품 | 주황 큐브 | 모델 연결 코드 없음 | 장비 FBX(`art/.../exports/equipment`·`handheld`) 또는 상자류 — 전리품 상자 전용 모델 없음 | ResourceNode 아님 → ResourceVisual 미적용 |
| 4 | `Buildings/BuildingPlacementController.cs:249` 공사 예정지 + `Save/SaveConstruction.cs:63` 복원 | 납작 큐브 2.5×0.2×2.5 | 모델 연결 없음 | `CC_ConstructionFrame`·`CC_ConstructionPlan` (**art에만 있고 Resources에 없음**) | 임포트 + 연결 필요 |
| 5 | `Buildings/Demolition.cs:66` 철거·이동·개보수 예정지 | 색 큐브(빨강/파랑/노랑) | 모델 연결 없음 | `CC_DemolitionRubble`(art만) — 철거만. 이동·개보수는 표시판 성격이라 원판 유지도 가능 | |
| 6 | `World/EventActor.cs:30` 사건 Food/Soil 더미 | 큐브 1.2 | EventActor는 ResourceNode 아님(ResourceVisual 미적용) | Food=`CC_ResourceGrain`, Soil=c21 결정 따름 | |
| 7 | `World/EventActor.cs:30` 사건 Wanderer(방랑자)·NightPredator(밤 포식자)·Wasp(말벌) | 캡슐(청록/보라/주황) | AntVisual.Attach 호출 없음(WildMonster 중 EnemyCommander·습격대만 Attach, `WildMonster.cs:144/338/344`) | Wanderer=Kemono 캐릭터(장수와 같은 KemonoModel), 포식자=`Resources/Critters/Spider`, 말벌=`Resources/Critters/Bee` | 벌·거미 연결 작업과 겹칠 수 있음 → Codex 판단 |
| 8 | `Save/SaveRuntime.cs:67` 저장 복원 런타임 몬스터 | 캡슐 | 위와 같음(일반 WildMonster는 AntVisual 없음) | 몬스터 종류별(QuirkyAnt/Critters) | 새 게임엔 씬 몬스터 → 씬 확인 필요 |
| 9 | `Core/ColonyPopulation.Evacuation.cs:65` 피난 시민 묶음 | 캡슐 0.6 | 모델 연결 없음 | `art/resident-kit` 주민 모델(**Resources에 없음 — 임포트 필요**) | |
| 10 | `World/ExpeditionSite.cs:76` 원정지 바닥 70×0.5×70 | 큰 큐브 판 | 지형 생성 없음 | 지형 재질/MapGenerator 재사용 — FBX 대상 아님 | |
| 11 | `World/ExpeditionSite.cs:96` 원정지 Food/Soil 노드 | Food→`CC_WildFungus`로 대체, **Soil 노드는 렌더 꺼짐 → 아무것도 안 보이는데 채집 대상** | ResourceVisual 자연 Soil = forceRenderingOff | c21 결정 따름 | Codex c21과 함께 볼 것 |
| 12 | 낚시터(`ResourceNode.RequiresFishing`) | 씬 배치 도형 그대로(추정) | `ResourceVisual.Attach` 첫 줄에서 낚시터 제외 | 전용 FBX 미발견. `Resources/Facilities/WaterSource`류 또는 물가 표지 | 씬 배치라 Unity 확인 필요 |
| 13 | `World/Corpse.cs:109` 시체 대체(QuirkyAnt 없을 때·야생동물 시체) | 납작 큐브 | 야생동물 시체는 CorpseProp id=null | `Prop_Remains` | |
| 14 | BuildingKind `DigSite` | 템플릿 큐브 | FacilityVisual 매핑 없음(유일) | 미발견(`CC_NestEntrance`·`CC_PebbleScatter` art만) | |

## 자연 광맥 모양 불일치(도형은 아니지만 참고)
`MaterialDeposits`의 IronOre·CopperOre·Coal·Sulfur·Sand·Clay 자연 광맥은 `CargoModel`(단품 광석/주괴용, 폭 1.2)로 표시 → 광맥이 아니라 바닥에 떨어진 물건처럼 보일 수 있음. 광맥용 `CC_OreOutcrop`은 art에만 있음.

## art에만 있고 Resources에 없는 colony-core 모델(연결 후보)
`CC_OreOutcrop`, `CC_ConstructionFrame`, `CC_ConstructionPlan`, `CC_DemolitionRubble`, `CC_FallenBranch`, `CC_Stump`, `CC_PebbleScatter`, `CC_NestEntrance` (`art/colony-core/exports/`).

## Unity에서 확인 필요(읽기 점검 불가)
- 씬(`AntColony.unity`)에 직접 놓인 낚시터·야생 몬스터·기타 오브젝트의 메시가 기본 도형인지.
- `MapGenerator.cs:259/262` `spawnObj.prefab`(prefab은 커밋 제외라 사본에 없음)이 기본 도형 프리팹인지 — ResourceNode면 ResourceVisual로 대체됨.
- 우선순위 제안: 노출 빈도 순 1(갑각 광맥) → 4(공사 예정지) → 7·8(몬스터 캡슐) → 6 → 2·3 → 나머지.
