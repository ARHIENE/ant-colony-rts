# 변경 이력

이 파일은 세션별 상세 작업 기록의 아카이브입니다. 최신 요약/현재 상태는 `log.md` 참고.

---

# 2026-09-02 ~ 2026-09-03 (세션 1)

## 개요
- 개미 소굴 RTS(가제): 자원 채집 + 건설 + 유닛 강화 + 레이드 RTS
- 시점: 아이소메트릭(쿼터뷰), 싱글플레이 메인(멀티는 후순위)
- 엔진: Unity 6000.5.8f1, URP
- 프로젝트 루트(작업 디렉토리): `E:\Git\ant`
- 버전관리: Git/GitHub 전환 완료(`github.com/ARHIENE/ant-colony-rts`, public). 기존 Plastic SCM 워크스페이스는 제거함
- 기획 원본: 사용자가 전달한 "개미 소굴 RTS(가제) 게임 기획서" 전문(2026-09-02 세션 시작 시 전달)

## 한 줄 컨셉
플레이어는 개미 여왕(플레이어 소굴)의 지휘자가 되어 일개미를 뽑아 자원을 캐고 소굴을 확장하며, 개미 부대로 다른 개미집을 약탈하거나 상대의 공격을 막아내고 거대 보스를 물량으로 레이드하는 RTS.

## 핵심 루프 (기획서 원문)
```
자원 채집 → 소굴 확장 & 건물 건설 → 개미 유닛 생산 & 강화
  → 정찰 & 목표 선택 → 전투(약탈/레이드) → 전리품 획득 → 반복(난이도 상승)
```

## 기획서 시스템 요약
- **자원**: 식량(Food) / 흙-토양-돌(Soil) / 특수자원(적 소굴·보스 전용, 강화용) — MVP는 Food+Soil만 구현, 특수자원은 범위 밖
- **유닛**: 일개미(채집/건설), 병정개미(근접 전투, 계급 세분화 여지), 특수개미(정찰/은신, 미구현)
- **강화 방식**: 개체별 레벨업이 아닌 "종족 단위 진화"(연구소에서 연구 → 이후 생산되는 전체 유닛에 적용) — MVP 범위 밖(연구소 미구현)
- **건설/확장**: 여왕방/창고/병영/연구소/방어시설. 확장은 흙을 소모해 새 방을 뚫는 형태
- **전투/레이드**: 야생 개미집 약탈, 보스 레이드(물량 대 스케일), 부분 자동 전투 + 플레이어 타겟 지정 혼합
- **아키텍처 권장**: 프로토타입은 오브젝트 풀링 + 단순 State Machine(ECS는 이후 검토), NavMesh 기반 이동, ScriptableObject 데이터 드리븐

## MVP 수직 슬라이스 — 이번 세션 구현 완료
기획서 4.2절 범위(1~5) 그대로 구현. 대상: `Assets/Scripts/` 하위, 씬은 아직 사용자가 직접 구성해야 함(아래 체크리스트 참고).

### 폴더 구조
- `Data/` — `ResourceType`(enum), `UnitData`(SO: 비용/스탯/일개미·병정개미 전용 필드), `BuildingData`(SO: 비용/종류)
- `Core/` — `ResourceManager`(싱글턴, Food/Soil 보유량·저장한도), `ObjectPool`(제네릭 풀링), `GameManager`(싱글턴, 루프 완료 신호), `IDamageable`(전투 대상 공통 인터페이스)
- `Units/` — `AntUnitBase`(공통 체력/NavMeshAgent/풀 반환), `WorkerAnt`(Idle→MoveToNode→Gather→ReturnToStorage→Deposit FSM), `SoldierAnt`(Idle→MoveToTarget→Attack FSM, 근접 자동 교전), `UnitSelectionController`(드래그 박스 선택 + 우클릭 이동/공격 명령)
- `Buildings/` — `BuildingBase`(예치 지점 정적 레지스트리 공용), `QueenChamber`(본진, 시작 일개미 스폰 + 추가 일개미 생산), `Barracks`(병정개미 생산), `Storage`(저장한도 증가), `DigSite`(흙 소모로 확장 구역 활성화)
- `World/` — `ResourceNode`(Food/Soil 채집 노드, 유한량), `WildMonster`(전투 대상 더미, 사망 시 `GameManager.ReportWildMonsterDefeated()` 호출)
- `Camera/` — `IsometricCameraController`(고정 각도 X:30/Y:45 직교 카메라, WASD 팬, 스크롤 줌)
- `UI/` — `HUDController`(런타임 생성 Canvas: 자원 표시, 일개미/병정개미 생산 버튼, 확장 버튼, 승리 메시지 — 수작업 UI 제작 없음)
- `Map/` — `MapGenerator`(랜덤맵 생성: Perlin 노이즈 높이맵 메시 + 높이 기반 텍스처 블렌딩 + 오브젝트 랜덤 스폰). `Assets/Editor/MapGeneratorEditor.cs`(인스펙터 값 변경 시 자동 재생성 + Generate 버튼), `Assets/Shaders/TerrainBlend.shader`(높이 기반 텍스처 레이어 블렌딩 URP 셰이더)

### 랜덤맵 생성(MapGenerator) — 이전 팀 프로젝트에서 포팅
- 출처: `github.com/ARHIENE/SIMUL-TeaamProject`(private) `hyeonyeop` 브랜치의 `TerrainGenerator.cs`/`TerrainGeneratorEditor.cs`/`TerrainShader.shader`를 참고해 그대로 포팅(네임스페이스/클래스명만 프로젝트 컨벤션에 맞게 조정: `MapGenerator`)
- 동작 방식: `xSize`×`zSize` 그리드 메시 생성 → 옥타브 Perlin 노이즈로 정점 높이 결정 → `terrainLayers`(텍스처+시작 높이 목록)를 `TerrainBlend.shader`에 전달해 높이 구간별 텍스처 블렌딩 → `spawnObjects` 목록(프리팹+높이범위+스폰확률+최소간격)에 따라 좌표별 시드 고정 랜덤으로 오브젝트 배치(같은 좌표는 항상 같은 결과 = 재현 가능한 랜덤맵)
- `ResourceNode`/`WildMonster`는 이미 일반 프리팹이라 별도 연동 코드 없이 `spawnObjects`에 그대로 등록해서 쓰면 됨(자동으로 `ResourceNode.Active`/`WildMonster` 정적 레지스트리에 등록되어 기존 채집/전투 로직이 그대로 작동)
- **원본과 다르게 단순화한 부분**: 물(Water)은 원본이 쓰던 전용 노멀맵 워터 셰이더/텍스처 에셋(외부 리소스 팩) 대신, `waterMat`에 아무 반투명 Material이나 연결하는 방식으로 단순화함 — 개미 소굴 RTS 기획서에 물 요소가 없어 시각적 디테일까지는 포팅하지 않음(필요해지면 그때 추가)
- 인스펙터 값을 바꾸면 자동으로 재생성됨(Editor 스크립트가 변경 감지) — 씬에 배치 후 `xSize`/`zSize`/`noiseScale`/`heightMultiplier` 등을 만져보면서 맵 형태 튜닝

### 설계 메모
- 확장(흙 소모)은 실제 지형 파괴 대신 `DigSite`가 흙을 소모해 미리 배치된 `expansionZone` 오브젝트를 활성화하는 방식으로 단순화(지형 메시 변형은 범위 밖)
- 일개미는 완전 자동(채집 대상 자동 탐색), 병정개미만 플레이어가 드래그 선택 + 우클릭으로 이동/공격 명령
- Input System 패키지가 이미 설치돼 있어 `Mouse.current`/`Keyboard.current` 직접 폴링 방식 사용(별도 Input Actions 자산 없음), HUD 버튼 클릭 인식을 위해 `InputSystemUIInputModule` 사용
- Cinemachine 미설치 상태라 카메라는 고정 회전 + 직접 스크립트로 구현(의존성 추가 안 함)

## SIMUL-TeaamProject에서 추가 이식(보스 레이드 / 선택 시스템) — 2026-09-02
`hyeonyeop` 브랜치를 전부 훑어서 사용자가 고른 3가지만 이식. 나머지(AntStats/AntMoving/AntAttack 등 유닛 시스템 전체, GameDataManager류 자원 시스템, InteractionUIManager, 자유회전 카메라, GameSpeed)는 우리 아키텍처와 겹치거나 안 맞아서 스킵(사유는 아래 각 항목 및 이전 대화 참고).

### 보스 AoE/텔레그래프 시스템 — `Assets/Scripts/Boss/`
기획서 3.4절 "보스 레이드"(광역 공격, 페이즈 전환)를 채우는 재사용 가능한 컴포넌트 세트. 지금 씬에는 배치 안 함(보스 몬스터 자체가 아직 없음) — 나중에 보스 레이드 기능 만들 때 바로 쓸 수 있게 스크립트만 준비.
- `BossHealth.cs` — `AntColony.Core.IDamageable` 구현(원본은 자체 IDamageable을 따로 뒀지만, 우리 프로젝트 공용 인터페이스로 통합해서 `SoldierAnt.CommandAttack`이 보스도 그대로 타겟팅 가능해짐)
- `AoE/BossCircleAoE.cs`, `BossConeAoE.cs`, `BossLineAoE.cs` — 원형/부채꼴/직선 범위 공격, 시전 시간(텔레그래프) 후 판정
- `Telegraph/GroundTelegraphCircle.cs`, `GroundTelegraphLine.cs`, `GroundTelegraphSector.cs` — 바닥에 달라붙는 경고 메시 런타임 생성(레이캐스트로 지형 높낮이 따라감 — MapGenerator가 만든 언덕 지형에도 정상 작동)
- `BossBasicPatternLoop.cs`(원형 패턴 단순 반복), `BossPatternSequenceSimple.cs`(원형→부채꼴→직선 순환) — `antLayerMask`로 주변 개미(IDamageable 보유) 자동 탐색
- **아직 안 된 것**: `BossHealth.onDead`가 패턴 루프 스크립트를 자동으로 멈추지 않음(원본 그대로 포팅) — 보스 죽어도 공격 시도는 계속될 수 있음, 실제로 보스 배치할 때 `onDead` 이벤트에 패턴 루프 `enabled = false` 연결 필요
- 사용하려면 개미 유닛들이 속한 Unity Layer를 하나 만들어서(예: "Ants") `antLayerMask`/각 AoE의 `targetMask`에 지정해야 함(지금은 전부 기본값 0이라 아무것도 안 맞음)

### 유닛 선택 시스템 업그레이드 — `Assets/Scripts/Units/`
기존 `UnitSelectionController`(단순 드래그+우클릭)를 역할 분리 구조로 교체:
- `SelectableObject.cs` — 선택 시 렌더러 색상 변경으로 시각 피드백(신규)
- `SelectionManager.cs` — 드래그 박스 선택 + 클릭 단일선택 + Shift 추가선택(신규). 원본은 레거시 `Input` 클래스를 썼지만 이 프로젝트는 전부 새 Input System이라 `Mouse.current`/`Keyboard.current`로 교체함. 선택 박스 UI 이미지는 `HUDController`와 같은 방식으로 런타임 자동 생성(수작업 UI 불필요)
- `UnitSelectionController.cs` — 이제 순수 "명령 전달자" 역할만: `SelectionManager`에서 선택 목록을 받아 우클릭 시 이동/공격 명령만 내림. 클릭한 곳에 **어떤 `IDamageable`이든**(야생 몬스터, 나중의 보스 포함) 있으면 공격, 없으면 이동 — 다중 선택 시 겹쳐서 이동하지 않도록 그리드 포메이션으로 분산 배치(원본 `AntMoveController`의 아이디어 이식)
- **씬에 필요한 추가 작업**: 병정개미 프리팹에는 `SelectableObject`가 `[RequireComponent]`로 자동 부착됨(코드 반영 완료). 다만 `UnitSelectionController`의 `selectionManager` 필드에 씬의 `SelectionManager` 오브젝트를 연결해야 함(비워두면 `FindFirstObjectByType`로 자동 탐색은 되지만 명시 연결 권장)

### 어택무브(Attack-move) UX — `SoldierAnt.cs`에 통합
원본은 A키로 별도 "공격 모드"를 켜는 방식이었지만, 우리는 우클릭 하나로 "적 클릭=공격, 땅 클릭=이동"이 이미 구분되므로 별도 모드 키 없이 **이동 중에도 주기적으로 주변을 살펴 야생 몬스터를 만나면 자동 교전**하도록 `TickMovingToTarget()`에 기존 `autoEngageRadius`/`autoEngageTimer`를 재사용해 추가함(원본의 "AttackMove 중 Chasing 전환" 아이디어를 우리 NavMesh 구조에 맞게 단순화 이식)

### 브릿지 불안정 — 이번 배치 작업 중 재현 및 해결
Boss(8개 파일)+Selection(2개 파일)+SoldierAnt 수정 후 컴파일 확인 중, `Camera` 네임스페이스 충돌 에러(`SelectionManager.cs`/`UnitSelectionController.cs`가 bare `Camera`를 써서 `AntColony.Camera` 네임스페이스로 잘못 해석됨 — `IsometricCameraController.cs` 때와 동일 패턴)를 발견해 `UnityEngine.Camera`로 전부 고침. 그 직후 `refresh_assets`를 반복 호출하며 도메인 리로드가 여러 번 겹쳤는지 브릿지가 응답 불가 상태(`Timed out while waiting for Unity response header`)에 빠짐 — [[unity_cli_bridge_local_patch]]에 기록된 기존 알려진 이슈와 같은 패턴으로 추정.
- **해결**: 사용자 승인 받아 Unity 프로세스(PID 11408, AssetImportWorker 자식 프로세스 2개 포함) 강제 종료 후 `E:\Git\ant` 프로젝트로 재실행 → 브릿지 정상 복구, 씬(`isDirty: false`, `MapGenerator` 등 기존 오브젝트 그대로) 손실 없음
- **최종 확인 완료**: `get_compilation_state` 기준 컴파일 에러 0개. 재시작 직후 뜬 콘솔 에러 1개(`Failed to perform selection on text`)는 에디터 내부 GUI 텍스트 선택 관련 잡음이라 우리 코드와 무관

## unity-cli 브릿지 — 이 프로젝트에도 도입 완료 (2026-09-02)
- 1VS1 Game/FPS Manager와 동일한 로컬 패치본(`E:\Git\_tools\unity-cli`)을 `Packages/manifest.json`에 `file:` 참조로 추가
- **포트 16401**(`ProjectSettings/UnityCliBridgeSettings.asset`) — FPS Manager가 쓰는 16400과 겹치지 않게 다르게 설정. CLI 사용 시 `--port 16401` 지정
- `MapGenerator.cs`에서 `GetInstanceID()`를 썼다가 Unity 6000.5의 obsolete-error(CS0619)로 컴파일 실패 — `GetEntityId().GetHashCode()`로 수정(unity-cli-bridge 패치 때와 동일 이슈, 이 버전 Unity에서 새 스크립트 쓸 때 항상 주의)
- Claude Code가 직접 GameObject 생성/컴포넌트 부착/필드 설정/씬 저장까지 가능함을 확인(아래 체크리스트 2번 MapGenerator 배치는 이 브릿지로 직접 완료함)
- **알아둘 제약**: `set_component_field`가 `List<T>` 필드의 배열 크기 조절(`Array.size`)은 지원하지 않음("Unsupported SerializedPropertyType: ArraySize") — 이미 존재하는 원소의 하위 필드(`Array.data[0].xxx`)만 수정 가능. 리스트에 기본 원소를 미리 채워두거나(코드 쪽 기본값), 사용자가 에디터에서 직접 +버튼으로 늘려야 함
- Inspector 커스텀 버튼(`OnInspectorGUI`의 `GUILayout.Button`)은 브릿지로 클릭 불가 — 대신 `[MenuItem]` 정적 메서드를 만들어서 `execute_menu_item`으로 실행하는 방식 사용(`MapGeneratorEditor.cs`의 "Tools/Ant Colony/Regenerate Map" 메뉴가 그 예시, 앞으로도 이 패턴 재사용 가능)

## 씬 셋업 — 체크리스트 전체 완료 (2026-09-02, Claude Code가 unity-cli 브릿지로 직접 처리)
아래 1~10번 전부 완료. `Assets/Scenes/AntColony.unity`에 총 16개 루트 GameObject:

| GameObject | 내용 |
|---|---|
| MapGenerator | 랜덤 지형(언덕 있음, `heightMultiplier=7`) + `NavMeshSurface`(베이크 완료) |
| GameSystems | `ResourceManager`+`GameManager`+`ObjectPool` |
| SelectionSystem | `SelectionManager`+`UnitSelectionController` |
| Main Camera | `IsometricCameraController` 부착 |
| QueenChamber/Barracks/Storage/DigSite | 각각 컴포넌트 + SO/프리팹 참조 연결 완료 |
| ExpansionZone | `DigSite`의 확장 구역, 처음엔 비활성 |
| FoodNode1/2, SoilNode1/2 | `ResourceNode`(Food/Soil), 갈색 머티리얼 |
| WildMonster | 빨간 머티리얼 |
| HUD | `HUDController` 부착(단, 아래 "수동 연결 필요" 참고) |

- `Assets/Prefabs/WorkerAnt.prefab`, `SoldierAnt.prefab` — 캡슐 + NavMeshAgent + 스크립트, **검정 머티리얼**(`Assets/Materials/AntBlack.mat`) 적용
- 색상 머티리얼 3종 생성: `AntBlack.mat`(개미), `EnemyRed.mat`(적/야생몬스터), `ResourceBrown.mat`(자원 노드) — 전부 `Universal Render Pipeline/Lit`, `_BaseColor` 프로퍼티로 지정(사용자 요청: 개미=검정/적=빨강/자원=갈색)
- 지형이 랜덤 높낮이가 있어서 건물/자원/몬스터가 처음엔 지형 아래에 파묻히는 문제 발생 → `Assets/Editor/SnapToTerrainMenu.cs`(`Tools/Ant Colony/Snap Scene Objects To Terrain` 메뉴) 신규 작성해서 레이캐스트로 전부 지형 표면에 스냅시킴(9/10 성공, `ExpansionZone`은 비활성 상태라 `GameObject.Find`가 못 찾아서 스킵됨 — 활성화되는 시점에 위치 재조정 필요할 수 있음)
- `Assets/Editor/NavMeshBakeMenu.cs`(`Tools/Ant Colony/Bake All NavMesh Surfaces`) — 인스펙터 Bake 버튼을 브릿지로 못 눌러서 만든 메뉴 유틸리티, 앞으로 지형/오브젝트 배치 바뀔 때마다 이걸로 재베이크

### 수동 연결이 남은 것 (브릿지가 씬-내부 오브젝트 참조는 못 걸어줌 — assetPath/guid 기반 에셋 참조만 가능, 자산 아닌 씬 오브젝트/컴포넌트 참조는 안 됨)
- **`HUD`의 `HUDController`**: `queenChamber`/`barracks`/`digSite` 필드가 비어 있음 — **이건 꼭 연결해야 버튼이 동작함**. 인스펙터에서 씬의 QueenChamber/Barracks/DigSite 오브젝트를 각각 드래그해서 연결
- `QueenChamber`/`Barracks`의 `pool` 필드: 비어 있어도 동작함(null이면 그냥 Instantiate로 폴백, 풀링만 안 쓰임) — 원하면 `GameSystems`의 ObjectPool 연결
- `DigSite`의 `expansionZone`: 비어 있어도 `TryExpand()`는 자원 소모하며 정상 동작하지만, 확장 구역이 시각적으로 안 켜짐 — 연결하려면 씬의 `ExpansionZone` 드래그
- `UnitSelectionController`의 `selectionManager`: 비어 있어도 `FindFirstObjectByType`로 자동 탐색되니 안 해도 됨(원하면 명시 연결 권장)

### 남은 작업
11. Play 모드로 전체 루프(채집→저장→생산→전투→승리 메시지) 실제 테스트, 밸런스(비용/시간/데미지) 체감 튜닝 — 위 HUD 참조 연결부터 먼저 할 것
- `Assets/Data/*.asset` 6개(WorkerAntData/SoldierAntData/QueenChamberData/BarracksData/StorageData/DigSiteData)는 `Assets/Editor/DataAssetBootstrapper.cs`(`Tools/Ant Colony/Create Default Data Assets` 메뉴)로 생성됨 — 기본값은 코드에 있는 값 그대로, 밸런스 조정은 인스펙터에서 직접

## unity-cli 브릿지 불안정 — 이번 세션에 반복 재현, 대응 패턴 정리 (2026-09-02~03)
씬 셋업 작업 중 브릿지가 **총 4차례** `Timed out while waiting for Unity response header` 상태로 멈춤(주로 `refresh_assets`로 도메인 리로드를 유발한 직후, 또는 사용자가 인스펙터에서 직접 값을 바꿔 `MapGeneratorEditor`가 재생성을 트리거한 직후). 매번 Unity 프로세스 자체는 `Responding: True`였지만 **CPU 사용량이 0%로 완전히 멎어있어서 데드락으로 판단**, 사용자 승인("안되면 껐다 켜") 하에 프로세스 강제 종료 후 재시작으로 4번 다 해결됨.
- **판별법**: `Get-Process -Id <pid>`로 CPU 값을 몇 초 간격으로 두 번 재서 델타가 0이면 데드락(단순히 느린 게 아님) — `Responding` 필드만으로는 구분 안 됨(데드락 상태에서도 True로 나옴)
- **주의**: Unity 재시작 시 **마지막 `save_scene` 이후의 씬 변경사항은 전부 유실됨**(도메인 리로드는 메모리 상태를 보존하지만, 프로세스 강제종료는 저장 안 된 건 그냥 날아감). 이번 세션엔 전부 스크립트로 재현 가능한 작업이라 다시 실행하는 식으로 대응했지만, **앞으로는 GameObject 생성/컴포넌트 부착 등 씬 변경 작업을 몇 단계마다 `save_scene`으로 끊어서 저장할 것** — 특히 `refresh_assets`나 대량 컴파일을 유발하는 작업 직전엔 반드시 저장
- 데드락 여부가 의심스러울 때 씬이 dirty한 상태(저장 안 한 변경 있음)라면, 에디터 창 타이틀에 `*` 표시로 확인 가능(`WinEnum`으로 GetWindowText, 포커스/입력 주입 없이 읽기만 가능) — 저장 안 된 게 있으면 강제종료 전에 한 번 더 신중하게 판단할 것

### 4번째 데드락의 실제 원인 — `MapGenerator` 인스펙터 값 폭주 (2026-09-03)
사용자가 인스펙터에서 `MapGenerator`의 `octavesCount`를 72까지, `xSize`/`zSize`를 100까지 직접 조작(스크롤/드래그로 보임). `GenerateMesh()`의 프랙탈 노이즈 계산이 `frequency = lacunarity^o`라 옥타브가 커질수록 인접 정점 사이 주파수 차이가 지수적으로 벌어지고, 그 결과 이웃 정점 높이가 폭주해 "삼각형 정점 간 거리 500유닛 초과" PhysX 콜라이더 에러가 발생함. 사용자는 이걸 "OFFSET을 만지면 에러난다"고 인지했는데, 실제로는 이미 망가진 상태에서 **아무 필드나 바꾸면**(오프셋 포함) 커스텀 에디터가 재생성을 트리거해서 에러가 그때 드러난 것뿐이었음.
- **코드 수정**: `MapGenerator.cs`의 `xSize`/`zSize`(2~200), `noiseScale`(0.001~1), `heightMultiplier`(0~50), `octavesCount`(1~8), `lacunarity`(1~4), `persistance`(0~1)에 `[Range]` 추가 — 앞으로 인스펙터 슬라이더로 조작하면 이 범위를 못 벗어남
- **주의**: `[Range]`는 **새로 입력하는 값만** 막아준다 — 이미 저장된 값(72, 100 등)은 자동으로 안 고쳐짐. 이번엔 브릿지로 7개 필드(`xSize`/`zSize`/`octavesCount`/`noiseScale`/`heightMultiplier`/`lacunarity`/`persistance`/`xOffset`/`zOffset`)를 안전 기본값(10/10/1/0.03/7/2/0.5/0/0)으로 직접 리셋 후 재생성·재베이크·재스냅·저장까지 완료함
- **사용자에게**: 앞으로 `octavesCount`는 3~4 이상 올리면 사실상 의미 없고(고주파 디테일이 안 보임) 값만 커지므로 낮게 유지 권장. `xSize`/`zSize`를 키우면 오브젝트가 지형에서 떨어져 보일 수 있는데, 그럴 땐 `Tools/Ant Colony/Snap Scene Objects To Terrain` 메뉴로 다시 스냅시키면 됨(자동으로 따라붙지는 않음 — 지형 재생성 후 수동 실행 필요)

### 추가 버그 3건 수정 (2026-09-03, 5번째 데드락 이후)
1. **`Texture2D.GetPixels` 예외 — 인스펙터 값 바꿀 때마다 에러 발생**: `Assets/TutorialInfo/Icons/URP.png`의 Read/Write가 꺼져있는데, `manage_asset_import_settings`(브릿지 도구)로 켜려 해도 실제로는 안 먹힘(재현 확인, `newSettings: {}`로 조용히 실패 — 이 브릿지 도구의 한계로 보임). **코드로 방어**: `MapGenerator.GenerateTexture()`에서 `texture.isReadable` 체크 후 아니면 경고 로그만 남기고 스킵하도록 수정 — 앞으로 Read/Write 꺼진 텍스처를 넣어도 에러 없이 그냥 그 레이어만 비워짐
2. **필드 바꿀 때마다 오브젝트가 지형에서 떨어짐**: `MapGeneratorEditor.cs`의 `OnInspectorGUI`/`Generate` 버튼/`Regenerate Map` 메뉴 전부 `GenerateTerrain()` 직후 `SnapToTerrainMenu.SnapAll()`을 자동으로 같이 호출하도록 수정 — 이제 인스펙터에서 아무 값이나 바꿔도 자동으로 재스냅됨(사용자가 메뉴 따로 실행 안 해도 됨)
3. **"모든 개미가 선택되게 해야됨" (사용자 요청)**: `SelectableObject` 요구를 `SoldierAnt`에서 공통 베이스 `AntUnitBase`로 이동 — 이제 `WorkerAnt`도 드래그박스/클릭으로 선택(하이라이트)됨. **단, 이동/공격 명령은 여전히 `SoldierAnt`만 받음**(일개미는 자동 채집 유지, 선택은 되지만 명령은 안 먹음 — 순수 시각적 확인용). `Assets/Prefabs/WorkerAnt.prefab`도 재생성해서 반영 완료

## 미정/논의 필요 항목 (기획서 5번 그대로 이관)
- [ ] 개미 종족(불개미/베짜기개미 등) 다양화 여부
- [ ] 보스 종류 및 패턴 상세 설계
- [ ] 아트 스타일(로우폴리 / 픽셀 / 스타일라이즈드 등)
- [ ] 세션 길이 목표 (1회 플레이 몇 분~몇 시간)
- [ ] 세션 내 성장과 세션 간 영구 성장 분리 여부(기획서 3.5)

## 다음 세션 참고
- 세션 시작 시 이 파일을 먼저 읽고 구조/진행상황 파악할 것
- 스크립트는 컴파일 통과 여부만 이번 세션에 확인 가능(씬 미구성) — 위 체크리스트 진행 후 실제 플레이 테스트는 다음 세션에서 확인
- MVP 이후 우선순위(기획서 대비 아직 없음): 연구소/종족 진화 시스템, 특수자원, 야생 개미집 약탈, 보스 레이드, 정찰/시야, 지형 파괴형 확장, 멀티플레이 대비 구조 분리
- 다른 두 프로젝트(1VS1 Game, FPS Manager)처럼 unity-cli 브릿지를 이 프로젝트에도 도입할지는 아직 미정 — 필요해지면(Claude Code가 직접 Play/스크린샷 확인해야 할 때) 논의

---

# 2026-09-03~04 (세션 2)

## 개요
세션 1의 MVP 수직 슬라이스를 이어받아: HUD 참조 연결 마무리 → 실제 팀 프로젝트(SIMUL-TeaamProject) 에셋으로 맵 전체 교체 → NavMesh 재베이크 → 카메라 전면 재작성.

## HUD 참조 자동 연결
`HUDController.Start()`에서 `queenChamber`/`barracks`/`digSite`를 `FindFirstObjectByType`로 자동 탐색하도록 수정 — unity-cli 브릿지가 씬 내부 오브젝트 참조를 못 걸어주는 한계를 코드로 우회, 인스펙터 수동 연결 불필요해짐.

## SIMUL-TeaamProject 맵 통째로 반입 → AntColony.unity 교체
처음엔 지형 텍스처/장식 프리팹만 뽑아 기존 10x10 맵에 붙이는 방향으로 진행했으나, 사용자가 "팀 프로젝트 씬(3DScene.unity) 자체를 베이스로 써서 그 위에 AntColony 게임을 얹어라"고 정정해 방향 전환.
- `github.com/ARHIENE/SIMUL-TeaamProject`(private, hyeonyeop 브랜치, 1.65GB, 유료 에셋스토어 패키지 다수 — 사용자 본인 구매분)를 클론해 `Assets/_TeamImport/`에 반입(작업 디렉토리 전용, `.gitignore`로 커밋 절대 금지)
- `_TeamImport/Scripts`는 별도 어셈블리(`_TeamImport.asmdef`, autoReferenced:false)로 격리 — 안 그러면 네임스페이스 없는 원본 `ResourceType`/`IDamageable` 등이 우리 `AntColony.*` 코드와 충돌해 컴파일 에러남
- 기존 `Assets/Scenes/AntColony.unity`(10x10 자체 맵)는 `AntColony_MVP_backup.unity`로 백업 보존, `Assets/_TeamImport/Scenes/3DScene.unity`(400x400, 팀이 만든 실제 지형+텍스처+장식 프리팹 1만개+ 이미 배치됨) 내용으로 교체
- 우리 게임 오브젝트(QueenChamber/Barracks/Storage/DigSite/FoodNode1-2/SoilNode1-2/WildMonster/GameSystems/SelectionSystem/HUD/Main Camera)는 백업 씬을 Additive 로드 후 `modify_gameobject`로 다른 씬의 루트 오브젝트(`/TerrainGenerator`)에 reparent했다가 다시 unparent하는 방식으로 씬 간 이동(unity-cli에 전용 씬 이동 툴이 없어서 찾은 우회법)
- 팀 원본의 자체 게임플레이 오브젝트(Ant/Canvas/GameManager/SpawnPoint/GameDataManager/Sparrow/팀 자체 카메라)는 우리 게임과 무관해 삭제
- **버그 실측**: 씬 간 이동 직후 바로 저장 안 하고 여러 단계(재컴파일 등) 거치고 저장하면 이동시킨 오브젝트가 파일에서 통째로 누락됨(Main Camera 1회 유실) — 백업에서 재이동 + 즉시 저장·grep 검증으로 복구. **앞으로 씬 간 오브젝트 이동 직후엔 바로 저장하고 검증할 것**
- `SnapToTerrainMenu.cs`가 지형 오브젝트 이름을 `"MapGenerator"`로 하드코딩한 버그 발견·수정(`"TerrainGenerator"`도 찾도록) — 씬 교체로 지형 이름이 바뀌어 스냅이 조용히 실패하고 있었음
- `MapGenerator.terrainLayers[0].texture`를 placeholder(URP 로고)에서 실제 `Grass_A_BaseColor.tif`로, `spawnObjects`에 `Tree_01`/`Tree_02`/`Rock_05`(로우폴리, 확정 아트 스타일과 일치) 등록 — 씬 교체로 화면상 안 쓰이게 됐지만 코드/데이터는 남겨둠

## NavMesh 베이크 — Play 시 "GetRemainingDistance" 에러 수정
3DScene 교체 후 Play하면 일개미 전원이 NavMesh 에러(`WorkerAnt.cs:136`) — 새 지형엔 NavMeshSurface 자체가 없었음(팀 씬엔 원래 없었고, 우리 옛 MapGenerator에 있던 건 씬 이동 대상에서 빠졌음). `NavMeshBakeArea` 오브젝트를 새로 만들어 `NavMeshSurface`를 Volume 모드로 건물 밀집 구역만(60x60, 400x400 전체 아님) 제한해서 베이크 — 5초 완료(전체를 구웠으면 FPS Manager 프로젝트 때처럼 15분+ 걸렸을 위험이 있었음, [[fps_manager_navmesh_bake_cost]] 참고).

## 카메라 전면 재작성 — 스타크래프트식 RTS 카메라
기존 `IsometricCameraController`(WASD 팬)를 완전히 새로 씀: WASD 제거, 마우스 화면 가장자리(18px) 스크롤 팬 + Q/E로 현재 보는 지점(focusPoint) 기준 90도 궤도 회전(0.25초 스무스). 시작 위치를 건물 실제 좌표 중심(약 5,5)으로 재계산 배치(기존엔 (0,1,-10) 지면에 박혀있어서 맵이 반만 보였음). Q/E 방향이 반대로 느껴진다는 피드백 받아 즉시 반전.

## unity-cli 브릿지 데드락 — 맵 크기/코드와 무관함을 최종 확인
Play 모드 진입 및 스크립트 재컴파일(도메인 리로드) 시마다 브릿지가 CPU 유휴 상태로 멎는 현상이 이번 세션에 8회 이상 재현됨. 맵 크기를 안전값으로 되돌려도 동일하게 재현되는 걸 직접 확인해서, **맵 크기 문제가 아니라 브릿지 자체의 미해결 버그**(세션 1에 이미 기록된 이슈와 동일 패턴)로 최종 결론. Play 모드 자동 검증은 이 브릿지로 계속 불가능 — 코드/씬 수정 후 컴파일만 확인하고 실제 동작 확인은 사용자가 직접 Play로 하는 흐름 정착.

## Notion 연동 — ant 프로젝트도 SAVE 시 개발일지 기록하도록 CLAUDE.md에 추가
FPS Manager와 같은 "개발 일지" 페이지(프로젝트 공유) 아래 날짜 하위 페이지 생성 + unity-cli 스크린샷/영상(mp4/webm — GIF 직접 출력은 안 되지만 노션이 영상을 그대로 인라인 재생하므로 변환 불필요) 첨부 루틴을 ant에도 적용하기로 확정.
