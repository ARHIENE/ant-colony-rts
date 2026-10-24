# IMPL-B7 인계 (2026-10-24, 중단 세션 재개 완료)

## 상태와 적용 범위
- 기존 staged/unstaged diff를 먼저 읽고 보존했으며, 이전 구현을 다시 만들지 않고 누락 경로와 검사를 보완했다.
- **완료는 소스 구현 판정이다. Unity 컴파일·RunChecks·실제 save/load·화면 검증은 전부 미수행이다.**
- 작업 위치는 이 worktree뿐. Unity 실행/MCP/Notion/캡처/prefab/commit/stash/push 작업 없음. log.md 개발 일지도 수정하지 않았다.
- 기존 staged 파일에는 B5/B6·저장 등 타 묶음 진행분이 섞여 있다. 이 문서의 B7 목록과 구분해서 합칠 것. 기존 index는 변경하지 않았다.

## G별 판정
| G | 최종 판정 | 구현 내용 / 남은 연결 |
|---|---|---|
| G13 | 완료(소스) | 건설 예약 재평가, 중단·소집·비활성·취소 시 미사용 재료/시민 예약 해제, 배송 실물 유지. 배송 경로/대상 소실 시 중단·반납. 공방/가공대 담당·금지·재료·도구·경로 사유 표시 및 재평가. 도구 파괴 시 장수 해제·미완성품 보존 |
| G18 | 완료(소스) | 원제작자 ID 제한, 이탈/사망/포로 시 보관 유지·취소 UI. 일반 재료 70%, Special/Component 핵심물 전량 반환. 공방은 미착수 미지불, 구저장은 materialVersion=0을 지불 완료로 이관. 가공대 파괴 후에도 취소 가능 |
| G25 | 완료(소스) | 최근 유효 작업 1800초 이력, 최초 600초 표본 이전 감소 없음, 비중 1% 미만·10 초과만 600초당 최대 0.1 감소. 보조 사용 인정·이력 JSON 보존. 600초 진입 때 과거 표본분을 소급 감소하지 않게 보완 |
| G28 | 부분(타 소유 연결) | FarmSeeds/FarmSeedSource, 야생 채집 시 개체당 1회 종자, 최초/확장/재파종 칸당 소비, 정상 수확 3회마다 종자, 작물 변경 시 수확 카운터 초기화. 넓은 밭은 2칸. 저장 필드 추가. 시작 적응 작물 1종·야생 3개 생성 및 야생 굴취 연결은 미완료 |
| G37 | 완료(소스) | 18/65/80세, 시작 18~30세, 어린 장수 1년 성년화, 65세 이후 연 1회 2%+나이당 0.2%p 노화/노망 판정, 강제 수명 사망 없음. 구 성체 1~8세→18~65세 이관. 현 소스의 옛 8세 문구 없음 |
| G42 | 부분(수송 연결) | 분노 추가 사망당 +20초/남은 120초 상한. OrderRefusalReason은 행동불능→분노→정신 붕괴 순. CanReceiveOrders에서 부위 쓰러짐도 거부. 수송 탑승/하차 대기열·사유·종료 후 실행은 미연결 |
| G55 | 완료(소스) | 의식·손 기능·간호 허용·치료 설정 검사, 무약 자가 처치/진균 자가 투약, 작업 2배·품질 70%. 수면·의식 상실 시 자가 치료 불가. 자가 수술 차단·타인 집도 유지. 움직이지 못해도 의식/손 기능이 남은 환자는 자가 처치 가능 |
| G61 | 부분(최종 공세 연결) | 편성/증원 시 하루 2끼 부대 식량 배정(소지품→창고), 소집 허기·피로 진행. 3초 식사·공격 정지·완료 시 1회 소비·중단 미소비분 보존. 30% 경고, 식사/절반 해산 UI, 기존 귀환/편성을 통한 교대. 부상 마지막 병력 회수 방지는 기존 ReturnTroops 재사용. 최종 공세 자동 귀환 금지는 holdDeployment 연결 필요 |
| G63 | 완료(소스) | 아군/적 장수 종류로 영구 유해 판정, 일반 청소/포식으로 삭제 불가. 적 사망 ID/추모 기록 생성(포로 생포는 시신 생성 안 함), 적 유해 안치/꺼내기 종류 보존. 기존 반란 장수 ID도 재사용. 시민/일반 적/사냥 사체는 300초. 아군 장비 실물 보존은 기존 사망 경로 재사용 |
| G64 | 완료(소스) | 고인 사건 ID 저장·중복 방지, 동료 -10/친구 -15/배우자 -30 중 한 단계, 다른 고인 합산 하한 -40. 목격 -3/150초(매 틱 갱신 안 함), 위로 +3/150초·고인별 달력 하루 1회, 날짜 JSON 보존 |
| G11 | 부분(패배 연결) | 출혈·질병·기아가 없고 부위 쓰러짐이 해소되면 쓰러진 뒤 150초에 개인 체력 20% 회복. 예상 잔여 시간 표시. HasRecoveryOrRescueRoute가 사본으로 자연 회복/자가 처치 시간·약품·출혈·질병·남은 포만을 비교. 난수/실제 건강·재고를 소비하지 않음. GameManager 패배 호출/문구 연결은 미완료 |

## B7 수정 파일
경로 접두사는 `Assets/_Project/Scripts/`이다. 이전 세션 staged B7 파일도 포함한다.
- Buildings: BuildingConstructionSite.cs, Workshop.cs, Processor.cs, FarmPlot.cs, **FarmSeeds.cs(신규), FarmSeedSource.cs(신규)**, Tombstone.cs.
- Units: CommanderAge.cs, CommanderAnt.cs, CommanderBurial.cs, CommanderCorpseWork.cs, **CommanderDeploymentMeal.cs(신규), CommanderRecovery.cs(신규)**, CommanderDuty.cs, CommanderHealthCare.cs, CommanderPersonal.cs, CommanderPersonalState.cs, CommanderSocial.cs, CommanderTalents.cs, CommanderWork.cs, WorkerAnt.cs.
- World: Corpse.cs, Memorial.cs.
- UI: CommandCard.cs, GameMenuHealth.cs, GameMenuProcessor.cs, GameMenuWorkshop.cs, WorkTargetPanel.cs. 기존 UI 구성/스타일을 재사용했다.
- Save: SaveDtos.cs(`farmSeeds` 필드), SaveSnapshot.cs(Capture/Restore 및 복원 시작 전 종자 상태 검증). 저장 버전 변경 없음. 개인 JSON/건물 State 경로로 나머지 새 필드 저장.
- AgentScripts/ImplB7Checks.cs, docs/IMPLEMENTATION_BOARD.md, 이 문서. graphify-out은 명령으로 갱신한 생성 산출물이다.
- 이번 재개에서 추가 보완: 가공대 파괴/비활성 미완성품·작업자 보존, 배송 경로 소실, 소집 전 건설 예약 해제, 부위 쓰러짐 명령 차단, 식사 중 공격 대상 차단, 증원 식량 보충, 부분 해산의 기존 부상 보존 경로 재사용, 자가 회복 예측의 시간/약품 처리, 씨앗 수확 주기/구저장 상태, 적 유해 ID, 회귀 검사 확장.

## 연결 요청 (아래 타 소유 파일은 수정하지 않음)
1. **G11 / Core/GameManager.cs / LateUpdate**
   - 기존 '살아 있는 비포로 장수가 1명이라도 있으면 return' 루프를 `CommanderAnt.HasRecoveryOrRescueRoute(CommanderRoster.Instance.Commanders)` 호출로 교체.
   - 기존 quitting/SaveSystem.Busy/GameStarted/Departed 가드는 보존. 결과 false면 기존 defeated/OnDefeat 처리에 `구조 불가` 사유를 연결하고 사망 없음 설정으로 패배를 차단하지 말 것.
   - UI 패배 표시 구독자에도 같은 사유 전달 필요. 단순 장수 생존 여부와 구조 불가를 구분할 것.
2. **G28 / Core/CrashLandingStart.cs / Apply + Map/MapGenerator.cs / SpawnObjects (바이옴/시작 담당)**
   - 시작 바이옴 적응 작물 1종, 접근 가능한 야생 개체 3개 보장. 해당 ResourceNode에 FarmSeedSource를 붙여 Configure(FarmCrop) 호출.
   - 채집은 WorkerAnt가 extracted>0 때 FarmSeeds.Collect를 이미 호출한다. 야생 굴취 완료 경로에서도 같은 Collect 호출 필요. 개체 위치가 움직이면 FarmSeedSource.Key의 위치 기반 ID를 생성 고정 키로 바꿔야 한다.
3. **G42 / World/ExpeditionTransport.cs / TryBoard·TryStation·TryUnloadCrew·Tick**
   - 직접 지시를 즉시 완료시키지 말고 대기 대상으로 저장/재연결. 장수 OrderRefusalReason이 비어 있지 않으면 사유를 표시하고 대기, 비면 이동/탑승/하차 실행.
   - 행동 불능→불응(분노/붕괴/음주 충동=현재 Binge)→직접 지시→자율 작업 순. 떠나거나 취소된 승객은 큐에서 제거.
4. **G61 / 최종 공세 담당(Core/CampaignResearch* 및 침공 소유)**
   - 최종 공세 시작/끝에 대상 장수 `WorkState.holdDeployment=true/false`. 새로 소집되는 장수도 해당 상태를 받아야 함. CommanderDuty.TickDuty가 true 동안 20초 자동 귀환을 억제한다. 일반 소집은 기존 자동 귀환 유지.
5. **B3 저장 담당 / Save/SavePreflight.cs / Validate·ForScene**
   - FarmSeeds.Valid(f.farmSeeds), FarmPlot.State의 harvests(0~2)/seedVersion/nextCrop, 공방 materialsPaid/materialVersion/makerId, Processor.State의 makerId/ruined를 기존 경계 검증에 연결. Snapshot 복원 앞에서 종자 검사하지만 파일 선택 시점의 사전 검증도 필요.
   - 개인 work.rations/holdDeployment/deploymentMealSeconds/unconsciousSeconds, ageScaleVersion/deathEvents/lastComfortDay, 공방·가공대 제작자·잔해, 농사 종자/카운터의 실제 전체 save/load 왕복 확인.
   - Buildings/BuildingBase.cs의 RestoreHealth는 Workshop만 0 허용한다. Processor 잔해도 체력 0을 유지하도록 조건 확장 필요(현재 state.ruined로 비활성/재작업 금지는 유지되지만 로드 시 체력 0.01로 바뀜).
6. **적/포로 장비 담당 / World/EnemyCommander.cs / Die, Buildings/PrisonerCamp.cs / 처형 경로**
   - 현재 적 장수는 플레이어식 장비 목록이 없어 새 장비를 임의 생성하지 않았다. 적 장비 실물 소유가 도입되는 경로에서는 사망 시 EquipmentLoot로 보존 필요.
   - 포로 처형 시신도 임시 ID로 묘비 안치 가능. 포로의 원래 PersonalState.id와 추모 기록을 동일 ID로 유지하려면 호출자가 ID를 전달하는 연결을 추가할 것.

## 검사와 검증 결과
- `AgentScripts/ImplB7Checks.cs`: public static class / public static async Task<string> Main() / 실패 예외 / 성공 PASS n. **작성만 했으며 실행 통과 수치는 없다.** Claude가 RunChecks로 실행할 것.
- 불시착 2명 새 게임으로 시작하고 필요한 비축더미/밭/제작대/묘비/시신/야생 종자 원천을 검사에서 생성한다. 자동저장 해제·임시 SaveStorage.RootOverride·난수/설정/배속/약품 Roll 복원. 검사 자체는 기존 B1 형식처럼 새 게임으로 현재 플레이 씬을 교체하므로 검사용 세션에서 실행할 것.
- 검사 범위: G13 소집/비활성 예약 해제·배송분 유지, G18 원제작자/환급/중복 취소/가공대 파괴·State 왕복, G25 표본/상한/하한/보조/이력, G37 나이 이관/연령 확률/성년화/수명, G42 분노 상한·식사 명령 불응, G55 무약 처치/진균 투약·약품 차감/자가 수술·수면·의식 상실 거부, G61 식량/3초·중단·부분 해산/부상 유지, G63 영구 유해·일반 만료·안치·꺼내기·적 ID, G64 중복·관계·합산 하한·위로 날짜/목격 갱신, G11 20% 회복·회복 경로·자가 처치만으로 구조 경로 인정하지 않기·예측 상태 보존.
- git diff --check 통과(LF→CRLF 안내만 있음). 참조 타입/메서드/반사 호출 대상은 worktree 소스에서 rg로 확인(주요 35개 심볼 존재 확인). C# 컴파일 성공으로 해석하지 말 것.
- graphify query로 범위 탐색 후 graphify update . 실행 완료. 8585 nodes / 19443 edges / 395 communities. 기존 커뮤니티 라벨 불일치 안내가 있어 코드 탐색용 생성 결과이며 의미 라벨 재생성은 하지 않음.

## 남은 위험 / 합친 후 우선 검증
- 우선순위: 금지/타 소유 연결 적용 → 신규 .meta 생성 → Unity 컴파일 → RunChecks ImplB7Checks → B3 SaveRoundtripChecks 및 불시착/건설/의료/수송 회귀 → 실제 UI 확인. 신규 파일의 .meta는 Unity를 실행하지 않아 생성하지 않았다.
- G11 예측은 본거지 환자의 가장 낙관적인 국소 회복을 검사한다. 원정/탑승 장수는 외부 구조 가능 경로로 보수적으로 남긴다. 수송선 자체가 구조 불능인지까지 통합하려면 수송 담당 판정이 필요하다.
- 달력 허기 속도상 포만 100은 150초보다 먼저 고갈된다. 전원 쓰러짐은 남은 회복 시간/급식·구조 가능성에 따라 실제로 구조 불가가 될 수 있다. 검사에서는 149초 경과·포만 충분 조건과 포만 0 조건을 구분한다.
- G28 시작 생성 연결 전에는 새 밭이 '종자 대기'에 머문다. 이를 임시 무료 종자로 우회하지 않았다.
- 직렬화 JSON/건물 State 단위 검사와 전체 저장 파일 왕복은 별개다. 전체 왕복 성공·UI 레이아웃·실제 경로 재개를 검증 완료로 보고하지 말 것.
- 카카오 MemoChat 도구는 검색 결과 이 세션에 없어 완료 알림을 전송하지 못했다.
