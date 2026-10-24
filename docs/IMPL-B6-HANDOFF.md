# IMPL-B6 인계 (2026-10-24, 정적 점검 완료·통합 연결 대기)

- 범위: 현재 B6 worktree만 사용. Unity·MCP·Notion·커밋·stash·push·prefab 작업 없음.
- 이전 세션 변경과 다른 묶음 변경이 이미 index에 혼재한다. 전체 diff를 B6 패치로 취급하지 말 것.
- 초기 판정: G07/G19/G23/G47은 상태/API 구현, 실제 경로 연결 일부 누락. G22 장비 효과 미구현. G06/G51 상태/API만 존재. G40 베타 분기 구현, 실행 미검증.
- 발견: BossHealth.Die는 RecordKill을 호출하지 않고 RestoreDefeated가 호출함. 실제 처치 보상 누락·로드 부수 효과 위험. BossRewardTransfer.Deliver/Lose 호출자 없음.

## G별 최종 판정

여기서 ‘완료’는 소유 범위의 코드 구현 완료이며 실행 통과를 뜻하지 않는다. 실제 플레이 전체 완료로 표시할 수 있는 상태는 아니다.

| G | 판정 | 구현 / 남음 |
|---|---|---|
| G07 | 부분 | A→B/C→D 제조법, A 핵심물 2개, B/C 제작 각각 A1+목재5+돌5, D 제작 B1+C1, 도구 사용 영구 공개, D 반입 로켓 설계도 해금 구현. 실제 원정 하역 연결 남음. |
| G19 | 부분 | 사망/획득/반입 분리, 최초 획득부터 복구 가능·반입 전 다음 제조법 불가, 화물 소실 뒤 자격 유지. 수송 하역·소실 및 장수 직접 운반 연결 남음. |
| G22 | 부분 | 위험·권장 장비·선행 부족 안내 구현. **A 고유 내구 장비/B 방열 장비/C 범위 공격 장비의 지급·효과는 미구현**. 기존 장비 타입에는 해당 속성이 없어 가짜 대체품을 지급하지 않음. D 핵심물 반입 API 구현. |
| G23 | 부분 | 복구 개당 가공재100+부품10·60초, 동시 제작/중복 결제/공개 대상 탐지기 제작 차단. 핵심물·도구는 별도 진행 상태여서 일반 자원 거래·재활용 경로에 들어가지 않음. 고유 장비·희귀 추가 보상은 G22 연결 필요. |
| G47 | 완료(코드) | 처치 후 12×GameCalendar.SecondsPerDay=1800초. 아군 장수·병력·일꾼/수송수단/교전 시 보류. 거점 재생성 없이 보스만 재활성화, 이전 미회수 보상 보존·회차별 1회 획득. 재활성화 시 패턴 타이머·타깃 초기화. 실제 전장 보존/재등장 실행 미검증. |
| G51 | 부분 | 반입 API의 영구 기록, 표본 중복 기록 차단, 기본 소비 보호/경고 확인 후 해제/재보호/저장 API 구현. 실제 식사·제조·이동 소비 경로의 적용과 씨앗 식별은 미연결. |
| G06 | 부분 | 기초 연구대 PrerequisitesMet=true 기존 구현 확인. 표본1개 반입 기록/이전 등급 회수 가능 여부/연구 완료/본격 채굴 조건 API 구현. **표본 실제 1개 채집·연구 작업/UI·채굴 차단·석유 설비·증기 석탄 발전은 미구현/미연결**. 기존 낚시·돌 도구·보온 연구에 새 관문을 넣지 않음. |
| G40 | 완료(코드) | BetaScenario 기본 false, 정식 첫 보스는 처치 결과·보상·다음 탐지 안내, 베타 승리는 전용 플래그에서만 표시. 로켓 엔딩은 기존 분기 유지. 베타 선택은 CampaignResearch.State.betaScenario로 저장(구저장 기본 false). 전용 진입부는 CampaignResearch 생성 후 BetaScenario=true로 설정해야 함. |

## 수정 파일과 이번 세션 보완

- 이전 B6 구현 포함 이관 대상: `Boss/BossProgress.cs(.meta)`, `Boss/BossHealth.cs`, `Core/CampaignResearch.cs`, `Core/CampaignResearch.Progression.cs(.meta)`, `World/BossRewardTransfer.cs(.meta)`, `World/BossStronghold.cs(.meta)`, `World/WorldMapManager.cs`의 보스 등록·공개 검사 부분, `UI/GameMenuBossProgression.cs(.meta)`, `UI/GameMenuCampaign.cs`, `UI/BetaProgress.cs`, `AgentScripts/ImplB6Checks.cs`.
- 이번 추가: `Boss/BossCargoCarrier.cs(.meta)` — 수송 목록 인덱스 대신 저장되는 고유 ID. 도보 원정대 해산으로 앞 인덱스가 없어져도 다른 원정대에 보상이 넘어가지 않음.
- `Save/SaveDtos.cs`: **TransportDto.bossCarrierId 필드 1개 추가**. `Save/SaveSnapshot.cs`: Capture에 ID 저장, Restore에서 수송 생성 직후 복원. null/누락은 이전 B6의 `transport:<index>` 키로 호환. `CurrentVersion=19` 유지. 두 파일의 기존 staged 변경은 다른 묶음 것이므로 이번 B6 변경만 추출할 것.
- `BossHealth`: RecordKill을 RestoreDefeated에서 제거하고 실제 Die로 이동. RestoreHp는 deathProcessed 초기화. 로드가 새로운 처치/보상을 만들지 않음.
- `BossProgress`: 제작 시작을 결제 전에 예약해 자원 변경 이벤트 재진입 차단. 종류별 TrySpend 성공을 확인하고 부분 결제 실패 시 환불. 저장 상태의 공개된 도구/중복 도구/획득 기록/회차 유효성 보강.
- `BossBasicPatternLoop.cs`, `BossPatternSequenceSimple.cs`: OnEnable에서 타이머·타깃(순환 패턴은 순번도) 초기화하여 재등장 후 이전 공격 대기 상태를 재사용하지 않음.
- `BossStronghold`: 장수만 검사하던 현지 아군 판정을 AntUnitBase.Active로 넓힘. `BossRewardTransfer`: 보스가 다시 살아도 남아 있는 이전 회차 보상을 회수할 수 있게 함.
- `CampaignResearch.cs`·`BetaProgress.cs`: 베타 시나리오 선택을 캠페인 상태에 저장하고 정식 기본값 false 유지.
- `CampaignResearch.Progression`: 정의되지 않은 ResourceType 및 음수 채굴 등급 거부. ResourceType 전체 이름 사용.
- 수정 금지 Units/Buildings/원정·수송/침공·협상/로켓/인구/바이옴 소스는 이번 세션에 수정하지 않음. 이미 staged된 타 묶음 변경은 유지.

## 연결 요청 (수정 금지 범위)

- `World/ExpeditionTransport.cs::UnloadCargo`: 일반 화물·장비 소비보다 먼저 `BossRewardTransfer.Deliver(this)` 호출. 소실·파괴 시 등록 목록에서 제거하기 전에 `BossRewardTransfer.Lose(this)` 호출.
- `World/ExpeditionTransport.cs::TryCollectRewards`: **BossStronghold가 있는 경우에만** `BossRewardTransfer.TryCollect(this)`로 분기하고 기존 무조건 BlueprintCargo/무작위 장비 지급 경로 우회. BossRewardTransfer의 비보스 fallback은 기존 TryCollectRewards를 호출하므로 무조건 위임하면 재귀함. UI/WorldMapPanel의 회수 버튼은 이미 BossRewardTransfer.TryCollect에 연결되어 있음(이전 staged 변경, 이번에는 수정 안 함).
- `World/ExpeditionTransport.cs::CargoLoad/HasCargo/AddCargo` 및 `World/TransportRoute.cs::Tick`: 핵심물 적재량 `BossRewardTransfer.CargoLoad(this)`를 용량과 화물 보유 여부에 포함. **CargoLoad에 포함한 뒤에는 BossRewardTransfer.TryCollect의 `- CargoLoad(ship)` 별도 차감을 제거해 이중 계산을 막을 것.** 현재 일반 적재는 진행 핵심물의 무게를 모르므로 후속 일반 화물 적재로 한도를 넘길 수 있음.
- `World/ExpeditionTransport.cs::OnDisable`의 확정 상실 경로에 Lose를 연결하되 저장 복원/씬 재구성과 정상 귀환 해산을 상실로 처리하지 말 것. `World/WorldMapExpeditions.cs::CreateWalkingParty`의 현재 transports.Remove는 편성 실패 시이며, 아직 화물이 없는 경로다. SaveSnapshot은 null 수송을 제외하므로 목록 인덱스는 저장 왕복에서도 바뀔 수 있어 고유 ID가 필요하다.
- `Units/CommanderWork.cs`의 직접 줍기/본거지 전달/소실 경로: 보스 회차 보상을 장수 소지로 회수할 때 `BossProgress.TryAcquire(id, "commander:" + commander.PersonalState.id, out parcel)`, 본거지 전달 직전에 `CampaignResearch.DeliverBossReward(parcel.id)`, 확정 소실 때 `LoseCargo(parcel.id)` 연결. 일반 식량 화물로 대체하지 말 것.
- `Units/CommanderEquipment.cs`: A 고유 내구 장비, B 방열 장비, C 범위 공격 장비의 고유 필드·효과 및 라벨 추가 필요. 현재 장비 모델에는 내구·방열·범위 공격 속성이 없다. 임의의 기존 장비로 대체 지급하지 않음.
- `World/ResourceNode.cs`, `Units/CommanderWork.cs`: `CampaignResearch.CanRecoverSample/CanMine`을 실제 채집에 적용. 이전 등급으로 표본 1개 회수하고 연구 전 본격 채굴 차단.
- `Core/ResourceManager.cs::Add/AddTo`는 이전 staged 구현에서 RecordSampleDelivery를 자원 이벤트 전에 호출하지만 **Add는 실입고 이전 요청 수량으로 기록**하고 있음. 본거지 바닥 전달도 반입으로 볼지 구분하고, 저장소 실입고는 실제 n 기준으로 호출해야 함. 원격 거점 생산/환불/복원까지 반입으로 오인하지 않도록 호출 원인을 분리할 것. 이 파일은 소유 범위 밖이라 수정 안 함.
- 자원 연구 담당: `CampaignResearch.TryCompleteResourceResearch(type)`는 완료 시 기록하는 API일 뿐 연구 작업/시간/UI가 아님. 실제 연구 완료 경로에서 호출하고 자원별 연구·도구 등급을 연결할 것. 정의되지 않은 석유 자원/가공재를 임의로 추가하지 않음.
- 식사·제조·이동 소비 호출자: `CampaignResearch.CanConsumeProgressionItem` 및 경고 확인을 거친 `SetConsumptionOverride` 적용. 미반입 진행물·필수 보상·씨앗의 안정 ID 전달 필요.
- `Buildings/PowerNode.cs`, 자원/설비 데이터: 석유 설비와 증기 석탄 발전기가 현재 없음. 해당 담당자가 자원·건물 정의와 전원 경로 구현해야 G06 전체 완료 가능.
- B3 `Save/SavePreflight.cs`의 수송/캠페인 교차 검증: bossCarrierId 중복과 boss cargo.carrier의 존재 여부, 핵심물 포함 적재 한도 검증 추가. 실제 save/load 시 화물 전달·손실·재등장·제작을 재실행하지 않는지 확인. 새 필드는 null 구저장 호환 허용.

## 검사·남은 위험

- `AgentScripts/ImplB6Checks.cs`: `public static class`, `public static async Task<string> Main()`, 실패 예외·성공 `PASS n`. 자동저장 비활성/임시 RootOverride, 기존 연구·자원 칸·자원 이력·게임 플래그·시간 배율 복원. Stockpile 탐색 없이 필요한 보스/패턴/수송 fixture를 직접 만들며 자원 fixture는 BaseBin 사용(없으면 ResourceManager도 생성).
- 작성된 검사: 최초 획득/반입 분리, A2개·분기/D 제조법, 실제 제작비·60초 경계·동시/재진입 결제 차단·중복 도구 금지, A→B/C→D 실제 제작, 전 보스 복구비, 1800초 경계·아군/수송/전투 보류, 이전 회차 보상 보존/중복 회수 금지, D 전달 로켓 해금, 화물/제작/표본/보호 설정 JSON 왕복, 잘못된 상태 거부, 운반자 고유 ID/옛 인덱스 키 복원, 다른 수송의 반입 차단/소실 처리, 로드 사망의 무보상·실제 사망 1회, 패턴 재활성 초기화, 정식 기본 베타 승리 미표시·베타 선택 JSON 보존.
- **검사 실행 안 함.** 전체 회수→귀환→실제 UnloadCargo, 실제 스냅샷 파일 왕복, 실장비 지급/효과, 최초 표본 1개 채굴, 씨앗 소비 차단, 베타 true 승리 화면은 이 검사로 검증되지 않음. JSON 왕복은 SaveSystem roundtrip과 다름.
- 컴파일·Play·실제 save/load 검증 미수행. Claude가 RunChecks로 실행해야 함.
- 보스/연구 상태는 `CampaignResearch.State` 안에 직렬화되어 기존 `SaveDtos.SaveFile.campaign` → `SaveSnapshot` 경로 재사용, 저장 버전 변경 불필요.
- 참조 심볼은 소스 rg로 존재 확인(상태/제작/반입 API, TrySpend(ResourceType,int,reason), StoreBin.amounts/Capture/Apply, GameManager.RestoreFlags, 패턴 필드, 수송 상태/용량, BetaProgress.Victory, ScreenName, DTO 필드). 컴파일을 대신하는 검증은 아님. `git diff --check` 통과.
- `graphify query`와 `graphify update .` 수행. graphify-out의 그래프·캐시·백업 변경은 코드 이관 대상에서 제외. 최종 갱신은 8537 nodes/19324 edges, 커뮤니티 이름 일부 자동 변경 안내가 있었음.
- 이전 세션 `BossProgress.EraMaterial`의 A=Plank/B=Iron/C·D=Steel 매핑은 보스별 시대/석유·원자 가공재 정의가 없는 현재 데이터에 의존함. 특히 C/D 시대 가공재의 정식 연결은 확인 필요. 숫자 100/10/60과 12일은 결정 원본 그대로.
- G22 고유 장비/희귀 보상 상태가 없으므로 지금 회수 API는 핵심물만 싣는다. 장비 지급을 임의로 구현 완료로 보고하지 말 것. 베타 시나리오 플래그는 이번 세션에 CampaignResearch.State.betaScenario 필드로 영속화함. BetaProgress 프로퍼티는 CampaignResearch가 생성된 후 설정해야 함.
- 카카오 MemoChat 도구가 세션에 없어 완료 알림 미전송.
