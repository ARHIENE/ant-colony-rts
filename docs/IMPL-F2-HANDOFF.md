# F2 — D08 지하 탐사 인계

- 기준: `DECISIONS_1025.md` D08 Codex B 및 `DECISIONS_1025_codex.md` D08 공통 규칙. 기존 worktree의 병합 미커밋 변경을 보존했다. checkout/reset/add/commit/push 없음.
- 구현·오프라인 컴파일 완료. **Unity 실행·Play 검사·실제 저장 왕복·화면 확인은 하지 않았다.** 실행 결과로 해석하지 말 것.

## 구현 내용

- `ScienceTechnology.UndergroundSurvey`를 enum/연구 배열 끝에 추가했다. 전기 4등급, 연구량 1500, 비용 200/200/30은 기존 ScienceDefinition/GameBalance 사용. 기존 번호 유지.
- `BuildingKind.UndergroundScanner`: 강철60·구리20·부품6, 건설45초, PowerGrid.IUser 200W. BuildingData/기존 배치·예정지·확정 재료 배송 경로 재사용. 담당 장수 1명(식사·위생·오락·이동·전투 중 진행 중단), 연구 기술 배율 `0.6+0.04×연구`만 적용.
- 본거지 및 각 원정 로컬 맵당 후보12(빈4·자원8). seed/map ID로 생성하고 후보 ID를 고정한다. 가까운 미조사 후보→동거리 ID 순, 여러 활성 탐사기의 예약 중복 제외. 후보별300 작업을 보존하므로 정전·담당 교체·해체·이동 뒤 재생성하지 않는다. 이동 시 전력망을 갱신하고 전력을 재확인한다.
- 층은 3×3, 층당 자연 ResourceNode 하나로 잔량 공유. 철광석/구리광석/석탄400, 원유/가스600, 티타늄160, 성휘80. ConfigureNatural(..., regrow:0), 일반 손채집/지정으로 꺼낼 수 없다. 발견 시 창고 자원을 지급하지 않는다.
- 후보 완료 시 그 후보만 발견, 빈 후보도 영구 기록. 자원·좌표·잔량·필요 시설·표본 연구 필요를 알림에 표시. 게임시간1초 내 발견을 묶어 ToastManager.ShowAt 한 번, 버튼은 3×3 범위 강조 및 해당 로컬 맵/카메라 이동. 고갈은 층당1회.
- SaveFileV1 **v19 유지**. `underground.present`로 누락/null→빈 객체와 새 데이터를 구분. 후보/발견/누적 작업/층 잔량/고갈 통지/대기 중 발견 알림·1초 타이머를 저장한다. 구저장은 원 시드로 최초 후보 생성. `surveyPresent`로 새 탐사기의 담당 장수/예약 후보를 저장하며 기존 CommanderOrder 복원 후 재연결한다. NaN/Infinity·범위 밖·후보 중복·중복 담당/예약 등은 preflight에서 거부한다.
- 지하 노드는 일반 SaveCatalog/SaveSnapshot 노드 저장에서 제외하여 중복 생성하지 않는다. 일반 노드의 CanGather에는 캐시 bool만 추가해 반복 GetComponent 비용을 피했다.

## 변경 파일

신규:

- `Assets/_Project/Scripts/Buildings/UndergroundScanner.cs`
- `Assets/_Project/Scripts/World/UndergroundSurvey.cs`
- `Assets/_Project/Scripts/World/UndergroundLayer.cs`
- `Assets/_Project/Scripts/Core/UndergroundResearch.cs`
- `Assets/_Project/Scripts/Units/CommanderSurvey.cs`
- `Assets/_Project/Scripts/Save/SaveUnderground.cs`
- `AgentScripts/ImplD08Checks.cs`
- `docs/IMPL-F2-HANDOFF.md`

기존 파일의 D08 연결:

- Data: `BuildingData.cs`, `ResourceType.cs` — enum 끝 추가/한국어 자원 라벨.
- Core: `CampaignResearch.cs`, `ScienceEffects.cs`, `GameBootstrap.cs` — 연구 등록·해금·로컬 맵 초기화.
- Buildings: `BuildingPlacementController.cs`, `EarlyConstruction.cs`, `FacilityVisual.cs` — 템플릿·실물 건설비·기존 모델.
- World: `ResourceNode.cs` — 지하층 손채집 금지 캐시.
- Units: `CommanderWork.cs` — 다른 작업/정지/비활성 시 탐사 배정 해제; `CommanderDuty.cs` — 생활 욕구 이후 탐사 배정을 유지하며 자율 작업 중복 방지. 규칙은 새 CommanderSurvey.cs에 둠.
- Save: `SaveDtos.cs`, `SaveSnapshot.cs`, `SaveCatalog.cs`, `SavePreflight.cs` — 명시 상태·최소 capture/restore/preflight 연결.
- `docs/IMPLEMENTATION_BOARD.md`, `README.md` — 소유 및 현재 구현 상태. SAVE 일지/log/changelog 작성 없음.

`UI/**`, D02/D03, Formation*, TransportDamage/ExpeditionWithdraw 및 prefab/FBX 수정 없음. 위 기존 파일에는 작업 시작 전 타 담당자의 미커밋 변경도 있으므로 파일 전체 diff를 F2 단독 변경으로 취급하지 말 것.

## 자동 선택

- 탐사기 외형은 기존 `CC_ResearchDesk`를 FacilityVisual에 매핑했다. 새 FBX/모델/프리팹 없음.
- 기존 enum에 없던 `NaturalGas`, `TitaniumOre`, `StellarOre`는 ResourceType 끝에 추가했다. 기존 자원 저장 번호를 유지한다.
- 지하 전용 바이옴 표가 별도로 없어 기존 `MaterialDeposits.Multiplier(biome,type)`를 가중치로 사용한다. 원유/가스/희귀 광석은 그 함수의 기존 기본 배율1을 그대로 받는다. 모든 종류를 강제로 보장하지 않는다.
- 후보는 분리된 4×3 구역에 시드 기반 작은 위치 편차를 주어 생성. 본거지 범위는 기존 MapSizes.Scale, 원정은 기존70m 로컬 맵 안쪽 사용. 런타임 지형/접근성은 미검증.
- 기계의 미정 등급/생산 속도/건설비는 새로 정하지 않았다.

## 연결 요청

Claude — UI 소유:

1. 연구 메뉴에 `ScienceTechnology.UndergroundSurvey` 노출(실제 연구 등록 완료).
2. 건설 메뉴에 `BuildingKind.UndergroundScanner` 연결. 기존 `ScienceEffects.BuildingUnlocked`, `BeginSciencePlacement`, `EarlyConstruction.Label/Cost` 사용. 모델/건설 데이터는 준비됨.
3. 탐사기 선택 UI에서 `TryAssign(commander)`, `Release()` 및 `Target`, `Status`, `CandidateId`, `Progress/300`, `Powered`, 반경35m를 연결. TryAssign은 본거지의 평시 연구 허용 장수, 근처8m, 작업 가능한 상태가 필요하다. UI 이동 안내/먼 거리 이동 명령은 아직 없다.
4. 발견 목록은 `UndergroundSurvey.Layers.Where(l => l.discovered)`만 표시하고 `Description(layer)`/`Highlight(ids)` 사용. 미조사 후보의 자원 종류/잔량을 UI에 노출하지 말 것. Layers 전체는 저장/검사용 데이터다.
5. 건설·진행 화면과 발견 알림 클릭/범위 강조는 Claude가 Unity에서 확인. 캡처는 SAVE 때만.

생산 담당 — 별도 생산 경로:

- `UndergroundSurvey.Extract(id, facilityPosition, type, amount)`는 **internal 공유 잔량 연결점**이다. 발견/같은 맵/3×3/자원 일치/표본 연구를 확인하고 한 ResourceNode에서만 차감한다. 생산 호출자가 실제 기계 종류·등급·작업·전력·입출고 공간을 검증한 다음에 호출해야 한다. 현재 게임 생산 틱에 연결되지 않았다.
- 현재 코드에는 고체용 심층 채굴기와 가스 추출기/가스 유체망이 없다. 기존 SupplyFacility.OilDrill도 OilDeposit 주변 생산 경로이며 지하층 잔량을 사용하지 않는다. 이 경로를 임의로 바꾸거나 새 생산 시설을 만들지 않았다. 따라서 **이번 작업은 탐사/발견/유한층 상태까지이며 실제 지하 채굴 생산은 미연결**이다. 알림도 이 한계를 표시한다.
- 후속 생산 연결에서는 3×3 안 기계들이 위 API를 공유해야 한다. 일반 ResourceNode.ExtractFor 또는 새 층별 사본으로 우회하지 말 것. 새 등급 요구사항이 정해지면 Description/시설 판정도 함께 연결.

## 검사·컴파일 결과

- `ImplD08Checks.Main()`은 public static async Task<string>. SaveStorage.RootOverride를 임시 경로로 격리하고 자동저장 설정·Unity Random·Time.timeScale을 finally에서 복원한다. internal API는 reflection, 게임 타입은 AntColony 전체 이름 사용.
- 검사 작성: 시드 동일성/후보12·4·8/자원총량/연구·건설비/연구0·10·20 배율/실제 전력망200W 소비·정전/동거리ID순/중복 배정·후보 예약/정전·일시정지·이동/진행·담당·예약 실제 save/load/발견 무지급/일반 채집 금지/표본 연구·3×3/공유 잔량/알림 묶음·대기 중 save/load/고갈 재통지 방지·왕복/누락 필드·빈 객체 구저장/잘못된 값 거부.
- 오프라인 Roslyn: `E:/unity/6000.5.8f1/Editor/Data/NetCoreRuntime/dotnet.exe` + `DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll`. `E:/Git/ant/Library/Bee/artifacts/1900b0aE.dag`의 본체/Editor rsp 기반. 원본 소스/out/refout/analyzer/generatedfilesout 제외, 참조 절대경로화, worktree runtime 소스 재열거. worktree에 없는 기존 외부 소스는 Temp/f2/dependencies로 수정 없이 복사. 가짜 타입/스텁 없음.
- `Temp/f2/a.rsp` → `Temp/f2/Assembly-CSharp.dll`: **exit0, 오류0**. 기존 BuildingConstructionSite.cs:33/109 CS0252 경고2건. 기존 obsolete CS0618 경고는 숨김.
- `Temp/f2/check.rsp` → `Temp/f2/Checks.dll`: **exit0, 오류0**. 본체 출력 dll 참조, ImplD08Checks만 검사 소스로 포함. 재생성 방법은 `Temp/f2/compile.py`.
- Unity 에디터/CLI 실행, 검사의 PASS, 실제 저장 왕복 성공, 모델/화면 검증은 **미수행**. 새 .cs.meta는 Unity 재개 시 생성 확인 필요.
- 카카오 MemoChat 도구가 세션에 없어 완료 알림은 전송할 수 없었다.

## ?? ?? ??

- ??/?? ?? Roslyn exit0???0, ?? CS0252 ??2?.
- git diff --check ??(CRLF ?? ?? ??), ??8?? UTF-8 BOM ???CRLF?? ? ?? ?? ??.
- ResourceType ?52?? ?? StoreBin ulong ?? ???64? ?? ?. ?? ?? ??/?? ?? ??? ?? ?? ?? ?? ???.
- graphify update . ?? ??(AST-only?API ??0): 10080 nodes / 23020 edges / 442 communities. ?? graphify-out ??? ?? ???? ?? ?? ???? ??. ???? ??? ??/hub ?? ?? ???? ?? LLM ??? ??.
