# 시설 모델 정적 점검 · P1

검사: `node AgentScripts/Inspect1101P1Models.cjs`. 결과 JSON은 `docs/validation/P1-1101-models.json`.

- `Buildings/FacilityVisual.cs`의135개 매핑에 대응하는 `Assets/Resources/Facilities/<이름>.fbx` 파일이 모두 존재한다.
- `Hut → CC_Hut`이고, FBX Model 이름은 `CC_Hut`, `Hut door`, `Hut wall`, **`Thatch roof`**다. `House → CC_House`에는 House walls·Roof slab·Window가 있다. 이름상 서로 바뀐 매핑은 발견하지 못했다.
- `RuntimeBuildingTemplates`는 Hut를 초가집·Housing으로 생성하고 kind를 전달한다. Hut가 HouseData를 명시적으로 사용하는 경로는 발견하지 못했다.
- Hut/House/Dormitory/Apartment 모두 FBX Y-up(UpAxis1/Sign1) 선언이고 시설 Null 루트에 약 **-90° X** 회전이 있다. Hut 문에는 별도의 약-180° Z가 있으며 이것을 시설 전체의 뒤집힘으로 해석해서는 안 된다. FBX는 메시 정점·오브젝트 변환·부모 축 변환을 함께 적용한다.
- 현재 FacilityVisual은 `Instantiate(prefab)` 직후 `importedRotation`·`importedScale`을 보존하고, bounds 크기로 균일 배율을 계산한 뒤 `transform.rotation * importedRotation`, `SetParent(..., true)`를 사용한다. 축 보정을 강제로 지우거나 모든 FBX에 +90°를 더할 근거가 없어 변경하지 않았다.
- JSON의 localMeshBounds는 서로 다른 메시의 **로컬 정점 범위**이며, 실제 계층 변환 후 월드 bounds/정상 자세 검증 결과가 아니다.

| 모델 재사용 | 정적 판정 | Claude 화면 확인 |
| --- | --- | --- |
| Hut/House/Dormitory/Apartment | 각각 전용 파일·내부 용도 이름 존재 | 초가집 지붕 실루엣, 집과 혼동되는지, 앞/뒤/좌/우 자세 |
| BigTable→DiningTable, MedicineBench→Apothecary | 용도 이름 대응 | 식사/약제 작업점·가구 방향 |
| StrongPen/Pen→RanchFence | 의도적 동일 모델 재사용 | 강화 축사 식별력은 전용 모델 후속 |
| ScienceLab/UndergroundScanner→ResearchDesk | 과학/탐사 역할의 임시 공용 모델 | 두 시설 식별력·연구소 Data.kind 확인 |
| DeepDrill/AdvancedDeepDrill/GasExtractor/OilDrill→OilDerrick | 파일 오류가 아닌 의도적 공용 모델 | 광물/합금/가스 채굴기의 구별 필요성 |
| GasSmelter→Smelter, GasGenerator→SteamGenerator | 공용 외형 | 가스 설비 식별력·배관 위치 |
| GasPipe→PipeL_Straight, GasTank→LiquidTank | 기존 유체 모델 재사용 | 기체/액체 구별 및 방향 |
| QueenChamber→Stockpile | 시작 비축 더미의 기존 표시 재사용 | 불시착 시작에는 비축 더미가 없으므로 실물 존재 조건으로 확인 |

현재 사용자 지적은 **미해결/미재현**이다. '초가집 모델 수정 완료' 또는 '뒤집힘 해결'로 보고하지 않는다. Claude는 격리 Play에서 Hut/House를 실제로 완공하여 Data.kind·Resources.Load 대상·FacilityVisual.Model 계층의 회전/손실 스케일·렌더러 bounds를 확인하고 원본 FBX 프리뷰와 비교해야 한다. 가져오기 캐시와 기존 씬 인스턴스 차이도 확인한다. 필요하면 FBX/임포터를 고치되, 이번 정적 검사는 `.fbx.meta`의 `bakeAxisConversion:0`을 오류로 단정하지 않는다.

P1은 Unity 실행/조작/캡처·FBX 재생성·prefab 생성을 하지 않았다.
