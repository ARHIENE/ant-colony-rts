# 회귀 검사 정리 인계 — 2026-11-02

- **지정한 8개 실패는 코드 대조상 낡은 픽스처·문구·기대값이다. 검사 8개를 수정했고 게임 코드는 수정하지 않았다.**
- 전체 게임 소스515개+변경 검사8개 오프라인 Roslyn 컴파일 오류0. Unity 내 검사 실행은 하지 않아 통과/해결로 확정하지 않는다.
- 별도 실제 문제: 웨이브 생성30회 실패 후 남은 예정 적을 삭제하고 정상 성공·원래 전체 마릿수로 기록한다. 아래 근거와 결정 필요 사항 참조.

## 작업 범위·보존
- 수정: AgentScripts/ImplB5Checks.cs, CampaignChecks.cs, Spec1015Checks.cs, SaveRoundtripChecks.cs, PlayableLoopChecks.cs, SiegeChecks.cs, SettlementDefenseChecks.cs, WorldMapChecks.cs.
- 신규 문서: 이 문서와 BALANCE-WAVE-1102.md.
- Claude의 ImplC3Checks·WaveScale1102Checks·웨이브/무리 게임 코드·플레이어 빌드·Unity 상태·기존 변경은 건드리지 않았다.
- 지식 그래프는 graph.json의 관련 노드/위치만 읽었다. 파일 제한에 따라 CLI의 캐시·Graphify update·담당 보드·README·log는 수정하지 않았다.
- 커밋·Notion·캡처 없음. 카카오톡 도구는 현재 세션에 없음.

## 검사별 판정과 변경
| 검사 / 보고된 실패 | 판정·원인 / 현행 근거 | 변경·유지한 검증 |
|---|---|---|
| ImplB5Checks / G21 return keeps travel time | 낡음. TryReturn은 즉시 Returning이 아니라 현장 집결·탑승을 요청한다. ExpeditionTransport.cs:161,202~206; ExpeditionWithdraw.cs:107~115,277~297. docs/DECISIONS_1025_codex.md:122~129,188의 D06 A. | 승무원을 집결 지점으로 Warp하는 픽스처 추가. 반 승차시간 동안 Deployed+IsWithdrawing, 승차 완료 후 Returning+남은시간 양수, 귀환 시간 절반 후에도 Returning, 남은 절반 후 Home을 검사. 출발/귀환 구간의 소비 시간을 분리하고 현재 TravelRemainingSeconds를 사용. |
| CampaignChecks / fill expanded command limit | 낡은 병역 인원 픽스처. Breed(100)은 모병5%=5명만 동원 가능하므로 지휘한도+장신구 한도를 다 채울 수 없다. CommanderAnt.cs:252~257, ColonyPopulation.cs의 PolicyRate, ColonyPopulation.Military.cs:10~25. docs/PLAY_CHECK_DECISIONS_1023.md:41; docs/NOTION_APPLY_1023_codex.md:855. | 지휘한도×20 이상의 성인을 마련하고 AvailableMilitary가 잔여 한도 이상인지 먼저 검사. 장신구 제거 후 초과 병력 보존·추가 편성 거부·장비 반환 검사는 그대로. 동원 제한을 우회하거나 끄지 않았다. |
| Spec1015Checks / start button for resin | 낡은 UI 문구. GameMenuResearch.cs:170~185는 내부 이름 Start Research, 표시 연구 지시/연구 재개. 이전 표시 이 연구 시작은 없다. 관련 파일 변경 이력 7113d6c(2026-11-01). | 내부 이름과 현행 한국어 문구로 버튼 선택. interactable·onClick·Resin 활성화·지불한 진행0 재개 검증은 그대로. |
| SaveRoundtripChecks / QueenChamber 없음 | 낡은 시작 전제. CrashLandingStart.cs:29~35가 시작 시설을 제거하고 카탈로그 빈 자리를 보존한다. Stockpile.cs는 구 여왕방 GUID/종류 이름 호환용. SaveCatalog.cs:53, SaveBuildings.cs:103~104. docs/IMPL-B10-HANDOFF.md:67, CrashLandingStart 이력7113d6c. | 여왕방 존재 시 종전 왕복 경로 유지. 부재 시 여왕방이 재생성되지 않음을 검사. 파괴된 창고가 없으면 시작 시설의 Destroyed 빈 자리를 사용해 tombstone 보존 검사. 구 낚시 변환은 비활성 Stockpile 픽스처로 실제 SaveBuildings.Restore를 호출하고 구 종류 이름도 검사한 뒤, 변환 결과의 저장/로드 보존을 확인. 병력·연구·스킬·신규 병영·가시함정·적/자원·운송 검증은 삭제하지 않았다. |
| PlayableLoopChecks / storage charges once | 낡음. 2026-10-12 예정지 배치→예약→실물 배송→시공 시작 규칙. BuildingPlacementController.cs:224~270, BuildingConstructionSite.cs:BeginPickup/Deliver/BeginWork. WorkerAnt.cs:125~158. | 시작 창고가 없으므로 검사 전용 Storage와 비용 재고를 준비하고 건설/운반 작업을 켬. 배송 재료는 Soil로 고정. 배치 직후 무차감→배송 후 시공 시작 때1회 차감→완공 때 추가 차감 없음 검사. 미완공 상한 불변·완공 상한 증가·시민 차출 없음·저장 후 상한 중복 없음은 유지. 창고 픽스처가 추가된 수/상한을 기준값에 포함. |
| SiegeChecks / Stockpile NRE | 낡은 시작 위치 가정. 불시착에는 활성 Stockpile이 없고 HomePosition이 기준이다(CrashLandingStart.cs:29~35, WorldMapManager.HomePosition). | 위치는 world.HomePosition 사용. 비장애물 대조군도 null Stockpile 대신 실제 시작 장수로 변경. 벽 종류·열린/닫힌 성문·번짐·NavMesh 막힘·벽 표적·점화 검사는 유지. |
| SettlementDefenseChecks / English successful | 한국어화로 낡음. SettlementDefense.cs:156은 거점 방어 성공.이며 :238에서 거점 제목을 붙인다. | 성공 문자열만 현행 한국어로 변경. 자동 전투 종료·영지 유지·주기 초기화·뒤따르는 함락 검사는 그대로. |
| WorldMapChecks / boss template health × difficulty | 캠페인 A~D 고정 체력 도입으로 낡음. ExpeditionSite는 먼저 난이도 배수를 적용하지만 BossStronghold.cs:20~21→BossEncounter.cs:34→BossHealth.cs:28이 캠페인 체력으로 덮어쓴다. BossEncounterRules.cs:9 A12000/B·C18000/D24000. docs/DECISIONS_1029_J2.md:12,31. | CampaignId가 있으면 해당 캠페인 고정값, 없으면 기존 템플릿×난이도. 두 경로 모두 시작 현재HP=최대HP 검증 유지. 캠페인 수치 자체는 사용자 미확인 잠정 구현이며, 검사 변경을 기획 승인으로 취급하지 않는다. |

- 앞의 5건이 오늘 변경을 제외한 기준 실행에서도 실패했다는 사실은 사용자 제공 결과다. 이 세션에서 stash/기준 실행을 반복하지 않았다.
- 나머지3건은 기준 실행이 없지만 각 코드 경로와 현재 실패 위치로 낡음을 판단했다. 실제 게임 오류가 없다는 전역 결론은 아니다.
- SaveRoundtrip의 불시착 분기는 옛 여왕방이 실제 씬 키를 차지한 전체 구저장 파일의 종단간 로드를 재현하지 않는다. 실제 변환 함수+현재 형식의 해금 재저장 왕복을 검사한다. 옛 형식의 전체 씬 키 호환은 별도 구저장 파일로 확인할 것.
- LocalIncursions.enabled=false를 독립 습격 타이머 정지로 해석하면 안 된다. 현재 일정은 WaveDefense가 담당한다. 짧은 검사 중 평화 만료·앞당기기가 끼지 않도록 실행 환경을 확인한다. ImplC3Checks의 Claude 변경은 보존했다.

## 실제 문제·게임 코드 미수정
### 1. 미생성 적을 버리고 정상 웨이브 성공으로 기록
- WaveDefense.cs:96~106: 생성 실패30회가 되면 s.pending=0. GroupInterval=.5이므로 연속 실패 시 약15초 규모다.
- :77~79는 pending==0 && AliveEnemies==0이면 End 호출. :109~115는 정상 방어 성공·적 s.total마리로 CampaignHistory/달성 연출을 기록한다.
- 따라서 NavMesh 경로 문제로 적이 단 한 마리도 생성되지 않은 경우에도 정상 성공할 수 있으며, 성공 기록의 마릿수는 실제 생성/처치 수와 일치하지 않는다.
- GAME_DIRECTION.md 6.1/사용자 요청의 정해진 적 전멸 종료와 충돌한다. 이번8개 실패의 원인은 아니며 코드에서 별도로 확인한 결함이다.
- 수정하지 않았다. 실패 때 정상 성공을 막고 어떻게 재시도/중단할지 방침을 Claude·사용자와 정해야 한다. 수치만 낮춰 은폐하거나 검사를 성공 기대값으로 바꾸지 말 것.

### 2. 웨이브 수 주석의 산술 불일치
- WaveDefense.cs:35~37 주석의 20/25/28회=348/1290/2835는 실제 공식의 보통 난이도 산술346/1285/2823과 다르다.
- 작은 차이지만 50몸체 경계에서는 호스트 수가 달라질 수 있다. BALANCE-WAVE-1102.md에서는 공식 계산값을 사용했다.
- 게임 코드/기존 주석은 수정하지 않았다. Unity에서 Count 직접 호출로 값을 기록한 뒤 설명과 표기를 맞출 것.

## 검증·재실행 인계
- 최종 오프라인 컴파일: 게임 소스515개+변경 검사8개, Unity6000.5.8f1 내장 Roslyn, Bee define/Unity DLL+Library/ScriptAssemblies 참조 사용.
- 결과: exit0 / 오류0 / 경고54(기존 obsolete API·미사용 변수 등 포함). 출력은 프로젝트 밖 임시 폴더에만 작성.
- 로그: C:\TESTTEMP\ant-checks1102-6e7020f97da8457389ba685e1af58d1a\compile.log.
- 변경8개 공백 검사 통과. 실행 검사는 미수행. 컴파일 성공을 Unity 회귀 통과로 해석하지 않는다.
- Claude: 현재 플레이어 빌드/플레이 검증을 마친 뒤 최신 스크립트 리컴파일 → 각 검사 별도 실행. 새 게임/로드로 상태를 바꾸는 검사이므로 플레이 중인 판을 보존한 실행 환경에서만 할 것.
- 보고된 첫 실패 이후 새로 드러나는 실패는 별도 원인으로 기록할 것. 예를 들어 Campaign의 오래된 연구소 즉시 승급 가정, PlayableLoop 후반의 인구0 편성/즉시 귀환 기대는 현행 선행연구·군역·철수 작업과 추가 대조가 필요하다. 이번 수정으로 검사 전체가 끝까지 통과했다고 보고하지 않는다.

## CHECKS-1102 후속 전체 대조 — 2026-11-03 KST

- 사용자 제공 Play 결과의 다음 실패 5건을 받아 **각 검사 파일을 끝까지 읽고 후반 기대값도 현재 호출 경로와 대조했다.** 기존 인계 내용은 보존하고 이 절을 추가했다.
- 이번 수정은 `AgentScripts/ImplB5Checks.cs`, `CampaignChecks.cs`, `Spec1015Checks.cs`, `SaveRoundtripChecks.cs`, `PlayableLoopChecks.cs`와 이 문서의 추가 내용뿐이다. 게임 코드·BALANCE·나머지 검사·담당 보드·Graphify 출력·log/README는 수정하지 않았다. Unity·커밋·Notion 실행 없음.
- 작업 전 5개 검사 모두 기존 미커밋 변경이 있었다. 그 내용을 유지한 채 후속 수정했으며, 작업 전 사본은 `C:\TESTTEMP\ant-checks1102-followup-si9t9j_8`에 있다.

### 1. ImplB5Checks — 낡은 귀환 완료 전제 + 새 웨이브 도착 규칙

- `G62 independent army departs`는 제거된 침입 타이머를 기다리는 검사가 아니다. 직접 `TryDispatchIndependentRaid()`를 호출한다. `DiplomacyRaids.cs:81`은 본거지 장수 0명이면 false다.
- 바로 앞 차량/항공기 귀환에서 `TryReturn()` 뒤 `Tick(TravelSeconds)` 한 번만 실행했다. `ExpeditionTransport.cs:202~204`는 철수 중이면 `TickWithdrawal` 뒤 return하므로, 같은 호출이 탑승과 이동을 모두 끝내지 않는다. 집결 위치도 보장하지 않았다. 따라서 검사상 귀환 요청 성공과 달리 실제 본거지 장수가 없을 수 있다.
- 두 수송의 승무원을 실제 집결 위치로 Warp → 탑승 Tick → Returning 확인 → 실제 `TravelRemainingSeconds` 이동 Tick → Home/본거지 장수 2명 확인으로 변경했다. 출발·원본 소굴 지불·중복 출발/도착·접촉 유예·계절 검사·외교 JSON·협상·자동 정지 검증은 유지했다.
- `DiplomacyRaids.cs:100`의 `!WaveDefense.AcceptsArrivals`에 맞춰, 수비 장수가 복귀해도 평화 중에는 도착하지 않음 → `StartNow()` 후에만 한 번 도착함을 추가했다. 이후 문명군 도착도 같은 웨이브 안에서 검사한다. 자동 Update는 잠시 꺼서 생성/종료가 직접 Tick 검사에 끼지 않게 하고 finally에서 원래 웨이브 상태·enabled를 복구한다. 독립 주기나 새 문명 공격 기획을 추가한 것이 아니다.
- **게임 버그 판정:** 보고된 실패는 검사 픽스처의 미완료 귀환으로 설명된다. `TryDispatchIndependentRaid`의 실제 결함을 확인한 것은 아니다. 물리 집결/NavMesh·도착 생성 성공은 Unity 재실행 필요.

### 2. CampaignChecks — 낡은 장비 수·승급·보스 보상·로켓 절차

- 시작 장수는 이미 나무칼을 보유한다(`CommanderRoster.cs:71`). 장신구 교체 뒤 전체 장비 `Count == 1`은 잘못된 전제다. 장신구 슬롯 1개·새 장신구 장착·옛 장신구 ID 재고 정확히 1개·나머지 시작 장비 ID 불변을 각각 검사한다. 반환 중복 검증을 줄이지 않았다.
- 연구소는 승급 연구와 등급별 실물 비용이 필요하다(`ScienceLabUpgrades.RequiredResearch/Cost/TryPay`). 선행 승급 연구 완료 전 실패와 완료 후 실제 `TryUpgrade`를 검사하며 2→6등급까지 순서대로 진행한다. 연구 기여는 두 연구소 모두 양수임을 확인하고 현재 `LabRate` 기준으로 합산한다. 진행 보존·1개 활성·연구자 소유·해제·최고 등급 검사는 유지했다.
- 캠페인 A 보상은 핵심물 2개와 고유 갑옷이고, D 핵심물 반입만 로켓 설계도를 해금한다(`BossProgress`, `BossRewardTransfer`, `CommanderBossEquipment`, `CampaignResearch.AcquireBlueprint`). A 보스를 명시적으로 선택하고 보상 회수 전 거부·현장 화물·중복 회수 거부·본거지 반입 때만 핵심물/장비 지급·A만으로 Engine 해금 불가를 검사한다. 귀환도 집결/탑승/이동으로 분리했다.
- 후반 Engine·로켓 저장 검사를 위한 **D 화물은 검사 전용 상태 픽스처**다. D를 공개한 상태에서 실제 `RecordKill`/`TryAcquire`/`DeliverBossEquipment` API를 사용한다. D 전투·탐지 사슬을 실플레이한 것으로 보고하면 안 된다.
- 로켓은 조립만으로 탑승/발사되지 않는다(`AirshipYard.cs`, `.Rocket.cs`). 연료·D 핵심물 반입 후 실제 가동을 검사한다. **15일 공세는 실행하지 않고** 준비 종료 직전의 유효한 저장 상태로 remaining만 줄인 뒤 Tick하여 Boarding 전환을 검사한다. 승객 소유·중복 탑승 거부·준비/승객 저장 왕복·발사 요청 후 프레임 말미 확정·구출/잔류 명부·승리 정지·중복 발사 거부는 유지한다.
- 과학 시설 화면 이름은 현재 `로켓 · 과학 시설`이다(`GameMenuResearch.ScienceFacilities`). 정확한 연구 진행 왕복을 위해 저장 전 수동 정지도 함께 저장한다.
- **게임 버그 판정:** 보고된 장비 반환 실패와 위 후반 항목은 낡은 기대값/픽스처다. 로켓 공세 전체 검증은 이 검사의 범위 밖이며, 게임 오류가 없다는 포괄적 결론은 아니다.

### 3. Spec1015Checks — 시작 3명에 옛 다인원 검사 사용

- `NewGameOptions.startingCount` 기본값은 3이다. Research는 `[0]`·`[1]`, Equipment는 `[2]` 뒤 `inv.Equip(list[3], w2)`를 사용한다. **사용자 보고의 인덱스 예외는 이 `list[3]`이 최초로 범위를 벗어나는 위치로 추론된다.** Unity 스택으로 확인한 결과는 아니다. Logistics의 Skip(4)와 의료 사망/추모 뒤 포로 담당 2명 선발까지도 옛 다인원 명부에 의존한다.
- 시작 제한을 바꾸지 않고 3명으로 새 게임 → 공개 `CommanderRoster.Create` 영입 경로로 검사 전용 총 8명을 준비했다. 연구/의료/운반 불가 같은 무작위 특성이 검사의 전제가 되지 않게 기본 성격을 사용한다. 타인의 장비를 가져올 수 없다는 `[3]` 검사는 삭제하지 않았다.
- 새 밭/다시 심기는 종자 대기 → 종자 소비 → 파종 순서다(`FarmPlot.Configure/Replant/Tick`). 종자 픽스처와 각 단계 검증을 추가하고 병충해 대조 밭에도 파종 전제 상태를 맞췄다. 실제 농부 파종·수확·뿌리뽑기·빈 밭 유지·상하좌우 전염·성장 저하는 유지했다.
- 요리 완료 후 주문이 없는데 `Deliver(..., 7)`로 재료 7개가 임의로 쌓인다고 기대했던 제거 검사를 고쳤다. `ProductionInputs.Deliver`는 현재 주문의 필요량만 받는다. 새 주문·예약·레시피 필요량의 실제 배송을 확인한 뒤 전량 바닥 반환을 검사한다.
- `CareWaitReason`의 의료 장수 없음 문구에는 자격·경로 안내가 붙는다. 원인 접두어로 검사하고 후속 실제 치료·약 소비·구조·급식·수술 성공/생존 실패/사망·건강 이관·퇴비·추모·포로·저장 검증은 유지했다.
- 긴 비동기 의료 검사에서 실제 쓰러짐 알림이 검사 자체를 정지시키지 않도록 전용 설정에서 autoPauseThreat/autoPauseDowned를 끄고 원래 설정·Random·MedicalRules.Roll을 finally에서 복구한다. 건강/연구 JSON 정확 왕복 전에 수동 정지도 저장한다.
- **게임 버그 판정:** 시작 명부·종자·배송·문구의 낡은 검사 전제다. NavMesh/실제 작업 타이밍·레이아웃 검사는 Unity에서 확인해야 한다.

### 4. SaveRoundtripChecks — 병력 장비 소유 실패의 근거

- 검사는 캡처한 첫 `monster:`의 `health`만 0으로 바꿨다. 현재 그 적은 시대 장비를 보유한다. `SavePreflight.Military`(`SavePreflight.cs:30~40`)는 장비 소유자가 생존(`health > 0`)해야 하고, 올바른 시대/슬롯/무기이며 ID가 유일해야 한다고 검사한다.
- 실제 사망 경로는 `WildMonster.cs:355~357 Die()` → `WildMonster.Military.cs:40 DropMilitaryEquipment()`다. 장비 참조를 null로 만든 뒤 같은 장비를 `EquipmentLoot.Drop`으로 옮긴다. `EnemyCommander.Die`도 마지막에 base.Die를 호출한다. `SaveRuntime.Capture`는 실제 소유 여부를 `militaryEquipped`로 함께 기록한다. 즉 정상 사망 후 캡처는 이 검사의 HP만 수정한 DTO와 다르다.
- **판정: 이번 보고된 실패는 낡은 픽스처.** HP 0 설정과 함께 동일 ID 장비를 `equipmentLoot`로 이동하고 `militaryEquipped=false`/장비 null로 만든다. 장비를 삭제하거나 preflight 조건을 낮추지 않았다. 두 번의 실제 로드 후에도 그 장비가 전리품에 정확히 한 번 있고 적이 소유하지 않음을 추가 검사한다.
- 중요한 보호: 픽스처를 수정하기 **전** 무수정 실제 `SaveSnapshot.Capture()`의 Validate, 실제 저장 직렬화/`SaveValidator.TryParse`, `SavePreflight.ForScene`을 먼저 검사한다. 여기서 실패하면 더 이상 픽스처 문제로 우회하지 않으며 실제 캡처/로드 문제로 보고해야 한다.
- 픽스처 파일도 게임의 `SaveOptionalStates.ToJson`을 재사용한다. JsonUtility의 null inline 객체 구별은 실제 저장 경로와 같게 유지하고, 메뉴 pause가 로드 때 해제되어 타이머가 흐르는 것을 막도록 수동 정지를 함께 저장한다.
- 실제 컴파일된 게임 DLL의 **원본 private Military 메서드**를 reflection으로 호출한 .NET 오프라인 검사 5건 통과(수학/메시 stub이나 재작성한 판정식 아님): 생존 장비 허용 / HP만 0으로 만든 상태 거부 / 장비를 전리품 ID 집합으로 옮긴 사망 상태 허용 / 중복 ID 거부 / 버전0 객체 정규화.
- 이 증거는 해당 소유권 오류의 원인을 분리한다. 기존 플레이어 저장 파일 전체·모든 구버전 저장의 호환성을 입증하지는 않는다. 이번 작업에서 게임의 저장/로드·버전 이관 코드는 수정하지 않았다.

### 5. PlayableLoopChecks — 옛 목표 문자열·시민/수송 전제

- BetaProgress.CurrentObjective는 현재 `CampaignNextSteps.Text → GoalChain.CurrentText`다. `GoalChain.Progress.Observe`는 단계 목표를 유지하고 Context에서 운송 상태를 표시한다. 빈 차 생성/탑승만으로 기존 목표를 `탑승`/`출정 준비`로 교체하는 옛 전제는 제거된 UI 동작이므로 대체했다.
- 빈 차·탑승만으로 원정 완료 비트가 켜지지 않음 → 실제 출발 때 원정 단계 비트가 켜짐 + `원정 이동` 문맥 표시 → 실제 귀환 이동 중 귀환/하역 문맥 표시를 검사한다. 자동 갱신 타이밍에 의존하지 않고 `GoalChain.Refresh`를 호출한다.
- 불시착의 시민 0명/모병제5%에 맞춰 시민 100명을 마련하고 실제 `TryAssign(2)`를 검사한다. 탑승 전에 거리를 맞추고 접수만 아니라 승무원 연결/대기 0까지 확인한다.
- `TryReturn`은 손에 든 자원을 이유로 요청 자체를 거부하는 대신 현장 철수 작업을 받는다. 화물 운반 중에는 Deployed로 남음 → 철수 취소 후 실제 반납/적재 → 재요청 → 집결/병력·장수 탑승 → Returning → 실제 귀환 시간 경과를 검사한다. 자원이 본거지에 조기 지급되지 않는다는 검증은 유지했다.
- 불시착에 Barracks/Stockpile이 없어서 null 인자만 검사하던 부분도 실제 비납입 건물 픽스처·로드된 실제 본거지 Storage 대상으로 바꿨다. 건설비/용량/실제 운반/저장 검증은 유지한다. 저장 중만 수동 정지하고 로드 후 운반 실행을 재개한다.
- **게임 버그 판정:** 현재 단계 목표·병역·물리 철수와 다른 낡은 전제다. 실제 입력/운반/적재/화면은 미실행이다.

### 후속 검증과 Claude 재실행

- 전체 게임 소스 515개 + 변경 검사 5개(총 520개), Unity 6000.5.8f1 Roslyn/Bee 참조·define: **오류 0 / 경고 38 / exit 0**. Unity 프로세스는 실행하지 않았다.
- 출력/소유권 검사 로그: `C:\TESTTEMP\ant-checks1102-followup-si9t9j_8\compile.log`, `military-test.log`. 결과 DLL/보조 harness는 모두 프로젝트 밖 임시 폴더에 있다.
- 5개 검사 공백 검사 통과. 이번 5개 사용자 보고에서 별도의 게임 코드 결함을 확정하지 않았으며, 이전 인계의 웨이브 생성 실패 결함 기록을 취소하는 뜻은 아니다.
- **Unity 검사 전체 통과는 아직 아니다.** 각 검사를 최신 컴파일 후 별도의 폐기 가능한 게임에서 재실행한다. Spec은 의료/사망/영입·자원 픽스처로 판을 바꾸고, Campaign은 D 화물/로켓 준비 종료 상태를 축약한다. 실제 15일 공세·D 전투를 검증한 것으로 합산하지 않는다.
- SiegeChecks / SettlementDefenseChecks / WorldMapChecks는 이번 후속 결과를 받지 않아 수정하지 않았다. 카카오톡 완료 알림 도구는 이 세션에 없어 전송하지 못했다.

## CHECKS-1102 거점 방어·월드맵 후속 — 2026-11-03 KST

- 두 파일을 처음부터 끝까지 현재 연구/수송/거점/UI 코드와 대조했다. 이번 수정은 `AgentScripts/SettlementDefenseChecks.cs`, `AgentScripts/WorldMapChecks.cs`, 이 절의 추가 기록뿐이다. Claude가 실행 중인 앞의 5개 검사는 수정하지 않았다(작업 전후 SHA-256 동일 확인).
- 기존 미커밋 변경을 보존했다. 시작 사본과 오프라인 출력은 `C:\TESTTEMP\ant-checks1102-world-defense-e2j3xbcm`에 있다. Unity·커밋·Notion·게임 코드 변경 없음. 파일 제한에 따라 담당 보드와 Graphify 출력도 수정하지 않았다.

### SettlementDefenseChecks

- **보고된 실패는 낡은 귀환 픽스처다.** 접촉용 `contactShip.TryReturn()` 뒤 `Tick(TravelSeconds)` 한 번만 호출했다. 현행 `ExpeditionTransport.Tick`은 철수 작업 중이면 탑승 작업 뒤 return하므로 같은 호출로 이동까지 끝내지 않는다. collector는 `Transport != null`인 채 남고 `CommanderAnt.IsAwayFromHome`은 true다. 본거지의 새 차량은 원정 소유권이 남은 장수를 태울 수 없다.
- 집결·실제 탑승·이동·본거지 하차를 완료하고 collector의 Transport null/본거지 복귀를 확인한 뒤 새 차량에 태운다. 새 탑승도 접수뿐 아니라 대기 지시 0과 실제 소유권 연결을 확인한다. 나머지 귀환 구간도 같은 완료 경로로 갱신했다. 인공 Tick은 보급 부채를 0으로 두고 한 번에 1초씩 진행한다(보급 규칙은 별도 G05 검사).
- D06/D07 승인 근거: `docs/DECISIONS_1025_codex.md:122~134`, `docs/IMPL-E2-HANDOFF.md:46~53`. 현장에서는 승차/적재 완료 시점에만 탑승·화물로 확정하고 조종 장수가 있어야 출발한다. 따라서 함락 즉시 손의 화물이 실리고 승무원이 순간 귀환한다는 옛 전제를 물리 보존→직접 철수→실제 적재/귀환으로 바꿨다. 목재17·손의 흙3·수비자의 소수점 식량7.25 보존, 귀환 중복 지급 거부는 유지했다.
- 현장 `DepositResources(Wood,17)`도 즉시 적재가 아니라 예약이다(`ExpeditionTransport.cs:248~264`). 예약과 실제 LoadSeconds 완료 후 17개 적재를 먼저 확인한다.
- 재편성 전에 출전 보직을 명시한다. 현재 UI의 방어 대기/구출 문구로 고쳤고 방어 문구는 실제 `defense.Status`와도 같아야 한다. 네 번째 검사 장수 생성 성공을 명시적으로 검사한다.
- 무인 차량 출발은 D07의 조종자 조건으로 폐기된 전제다(`TransportDamage.HasPilot`, `DepartureBlock`). 빈 차가 출발하지 않는 음성 검사를 남기고, 후속 목적지 함락 검사는 실제 조종자를 태운 차량으로 수행한다. 두 번째 포획 시에는 포획 확률1뿐 아니라 피해로 사망하지 않는 Gentle 상태를 해당 호출 동안만 설정하고 복구한다. 포획/탈출/구출/성장 보존/재탈환/포기 후 구조 검증은 유지했다.
- 본거지 웨이브는 자동 Update를 꺼서 별도 거점 습격 검사와 섞이지 않게 한다. `LocalIncursions.enabled=false`로 옛 타이머를 멈춘다고 해석하지 않는다. 이 검사는 `TickRaidTravel`을 실행하지 않으므로 도착 게이트를 강제로 열지 않는다.

### 게임 쪽 회귀: 이미 탑승한 원정대도 목적지 함락 시 회항하지 않음

- `SettlementDefense.cs:202`는 `site.Visitor?.EvacuateLostSite()`를 호출한다. `ExpeditionSite.Visitor`는 Deployed가 없으면 해당 목적지로 가는 Outbound 차량도 반환한다.
- `ExpeditionTransport.cs:177~180`은 비도보 차량/항공기에 대해 `TryEmergencyReturn()`만 호출하고 결과와 무관하게 return한다.
- `ExpeditionWithdraw.cs:138~141`의 `TryEmergencyReturn`은 `withdraw.active`와 `withdraw.jobs`의 승차 완료 장수가 필요하다. 정상 `TryDepart`로 출발한 Outbound 수송은 승무원이 이미 IsEmbarked=true지만 현장 철수 작업은 active가 아니다. 따라서 이 호출은 false이며 State/Remaining/Site를 바꾸지 않는다. **이미 탑승한 정상 조종자가 있어도 목적지 상실 훅이 아무 동작을 하지 않는 경로가 코드상 확인된다.**
- 현장의 미탑승자를 자동으로 태우지 않는 D06 규칙과 이 이동 중 사례는 구분해야 한다. 원래 검사의 함락 중 회항 회귀 검증을 삭제하지 않았다. 무인 출발이라는 불가능한 전제만 실제 조종자 탑승으로 고쳤고, `piloted outbound transport turns back on site loss (known evacuation regression; see handoff)`는 Returning/승무원1명/탑승 상태를 계속 요구한다.
- **이 게임 경로가 수정되지 않으면 SettlementDefenseChecks는 이 후반 assertion에서 계속 실패할 것으로 예상한다. 전체 PASS를 보고하면 안 된다.** 이번 범위에서는 게임 코드를 수정하거나 Outbound 유지 상태를 성공 기대값으로 바꾸지 않았다. Unity에서 새로 재현한 결과가 아니라 호출 조건으로 판정한 회귀이며, 회항 경계/플레이 의도가 달라졌다면 해당 계약부터 확인해야 한다. 뒤의 포기 후 구출 코드는 대조·갱신했지만 이 실패를 통과한 실제 실행은 없다.

### WorldMapChecks

- **보고된 실패는 낡은 승급 전제다.** `lab.TryUpgrade()`에는 `ScienceLabUpgrades.RequiredResearch(Tier)` 완료와 실물 비용이 필요하다. 과거 검사는 연구자 배정 전, 승급 연구 없이 즉시 2등급으로 올리려 했다.
- 연구자 실제 배정→승급 전 거부→현재 등급의 승급 연구 시작/기여 양수/완료→실제 `TryUpgrade`→차량 연구 1회 시작/중복 거부로 고쳤다. 이후 3·4등급과 비행기 선행 Gliding도 각 승급 연구를 거친다. 비활성 연구소의 연구자 해제·진행 정지·재배정, 건조, 차량40/항공100 정원은 유지했다.
- 재방문·항공 원정·중립 자원 귀환도 실제 철수/탑승/이동/하차 완료를 기다리도록 고쳤다. 기존 ReturnHome은 Returning만 진행시켰고 Wait의 false도 무시했다. 이제 철수 단계도 진행하고 제한 시간 안의 Home/승무원0/지시 대기0을 assertion으로 검사한다. 현장 자원 반입 전 본거지 수량 불변·정확한 화물 반입·중복 지급 거부·다른 원정 독립성은 유지한다.
- 두 파일 모두 자동 저장을 끄고 전용 임시 RootOverride를 사용하며 원래 설정을 finally에서 복구한다. 위협·쓰러짐 자동 정지도 이 검사 동안만 끈다. 정상 운송·실제 교전·채집·UI 화면/마커 영역 검증은 Unity에서 수행해야 한다.
- 기존 보스 고정 체력 수정, 사이트 구성/색/난이도/재고/카메라, 병력·성장·소유권, 정복/포기, 베타 승리, 기존 자원 전리품, 독립 침입자 직접 생성/포로, 33개 마커/패널 경계 검사는 삭제하지 않았다. 직접 `TrySpawnIndependent`는 생성 API 검사이며 옛 일정 또는 평화 도중의 예약 도착을 검증하는 것이 아니다. 자동 웨이브만 꺼서 이 생성 검사와 겹치지 않게 한다.

### 별도 게임 UI 오류: 월드맵 과학 안내가 실제 규칙과 다름

- `UI/WorldMapPanel.cs:146`의 건설 실패 문구는 시민60·낚시·2단계 병영을 요구한다. `:206`의 상태 문구도 식량100/재료100/시민8 및 항공 연구소3단계를 안내한다.
- 현재 `ScienceLab.PrerequisitesMet`는 true이고 실제 기본 연구소 건설은 `EarlyConstruction.Cost`를 사용한다. Aircraft 연구는 CampaignResearch 정의에서 4등급/Gliding이 필요하다. 실행 조건과 화면 안내가 서로 달라 실제 플레이어에게 잘못된 준비 목표를 줄 수 있다.
- 이 UI 불일치는 이번 `research starts once` assertion의 원인은 아니다. 검사 기대값으로 옛 문구를 승인하지 않고, 게임/UI 코드도 수정하지 않았다.

### 검증·남은 실행

- 현재 전체 게임 소스515개 + 지정 검사2개 = 총517개, Unity6000.5.8f1 내장 Roslyn/Bee 참조·define: **exit0 / 오류0 / 경고49**. 로그: `C:\TESTTEMP\ant-checks1102-world-defense-e2j3xbcm\compile.log`.
- 두 검사 공백 검사 통과. 인계 문서의 기존 바이트 전체를 보존하고 이 절만 덧붙였다. 보호한 앞의 5개 검사의 내용 해시도 동일하다.
- **Unity 실행은 하지 않았다.** WorldMap 전체 PASS는 미확인이고, SettlementDefense는 위 실제 회항 경로 회귀를 탐지하는 후반 실패가 남는다. 미검증/예상 실패를 해결 완료로 기록하지 않는다. 카카오톡 알림 도구는 이 세션에 없어 전송하지 못했다.
