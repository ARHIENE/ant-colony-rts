# IMPL-C3 인계 — 위기·침공·패배 판정 연결

작성: 2026-10-24 / Codex / 격리 worktree C3. **Unity 실행·컴파일·검사 실행 안 함.** 아래 ‘완료’는 소유 범위 코드 연결을 뜻하며 실행 통과가 아니다.

## G별 상태

| 결정 | 상태 | 연결 내용 |
| --- | --- | --- |
| G39 | 소유 범위 완료 / 외교 출발·도착 연결 필요 | ColonyInvasion은 경제 틱 뒤 신규 파동 타이머만 정지. LocalIncursions의 추첨·직접 신규 생성 차단. SettlementDefense는 신규 습격만 차단하고 진행 중 전투·점령은 유지. ColonyEvents는 위기 종류 Cold~Wasps만 제외하고 추첨 체크는 소비, 기존 사건·actor·비적대 사건 진행. 밤 포식자 차단 날짜도 소비. RocketDestroyed의 3×SecondsPerDay=450초 유예 재사용. |
| G11 | 소유 범위 완료 / 패배 UI 구독자 연결 필요 | GameManager.LateUpdate의 기존 가드·탑승 갱신→발사→패배 순서 보존. HasRecoveryOrRescueRoute 및 안전 귀환 중 석방자 확인. 생존자는 있으나 경로가 없으면 DefeatReason=‘구조 불가’. 사망 설정으로 패배를 막지 않음. 고정 위기 메시지로 사유 표시. |
| G16 | 소유 범위 완료 | WildMonster의 대상 재탐색에 시민 위치 선택 추가. 기존 방어 장수/유닛 우선, 노출 시민, 침공 건물 순. 시민도 기존 접근·벽 공성·공격 주기 사용. DamageCivilians로 피해10당1명·위치 시신 집계를 재사용. 주거가 파괴되면 다음 Sync에서 보호만 상실. 기존 Housing/BuildingBase/WildMonster에 주거 파괴 즉사 코드는 없으므로 제거할 코드 없음. |
| G20/G45 | 소유 범위 완료 / 밸런스 잠정 | RocketCampaign.TickAssault에서 내장 생성기를 호출하고 외부 AssaultTick 관측 콜백 유지. Preparing/Boarding 모두 생성, 발사/파괴 뒤 중단. 고정 snapshot의 Wealth/Population만 강도 입력에 사용. 복제본만 외부 콜백에 전달. |
| G61 | 소유 범위 완료 | RocketCampaign의 이른 Update(-1000)·LateUpdate·공세 틱에서 본거지 우호 장수 holdDeployment 설정. 뒤늦게 소집·복원된 장수도 갱신. 로켓 파괴 즉시 해제, 가동 종료 후 해제. 3일 위기 유예 중에는 자동 귀환 억제 안 함. Units 수정 없음. |
| G50 | SettlementDefense 완료 / 다른 포획 경로 연결 필요 | Lose의 기존 함락 알림에 포로가 있을 때만 CaptureWarning을 한 번 추가. 외교 CapturedBy는 소유 밖. |

## 최종 공세의 잠정값과 저장

- **잠정(결정 원문에 없음), 사용자 후속 승인:** 기존 ColonyInvasion 설정을 재사용. 기본 첫90초, 이후120초, 기본2마리+파동별1, 최대6마리, 활성 상한12, 실패 재시도15초. 실제 값은 해당 컴포넌트의 기존 serialized 설정과 난이도 배수 우선. 일반 파동과 마찬가지로 실제 병력이 생성된 경우에만 다음 파동 번호로 진행하며 적 장수 템플릿이 있으면 장수 동반.
- **잠정(결정 원문에 없음):** `floor(Population/10 + Wealth/100)`을 기존 첫 병력+파동 증가량에 더하고 기존 난이도·파동 상한 적용. 숫자10/100은 ColonyInvasion의 상수 옆에도 표시. 스냅샷이 크면 기존6마리 상한에 빨리 도달한다. 가동 전 예상 강도 UI는 소유 밖이므로 아래 연결 요청 참조.
- 본거지 원본 ColonyInvasion(비활성 템플릿 포함)의 생성 위치·분산·설정을 사용한다. ExpeditionSite 밑 원격 소굴은 선택하지 않는다. 최종 공세는 소굴 자원을 소비하지 않아 적 소굴의 자원 고갈·패배로 중단되지 않는다. 본거지 생성기/LocalIncursions가 없으면 내장 공세 사용 불가이며 가동 가능으로 위장하는 빈 구독자를 추가하지 않았다.
- 최종 공세 적은 LocalIncursions 방문자 목록에도 등록한다. 기존 visitor 런타임 저장·복원 경로를 재사용한다. 활성 상한은 방문자 생존 수로 계산하므로 로드 후에도 유지된다.
- RocketCampaign.State에 `assaultStarted/assaultAt/assaultRemaining/assaultWave` 추가. 기존 SaveSnapshot의 rocket Capture/Restore, SavePreflight의 RocketCampaign.Validate에 자동 포함. 버전19 유지. 구저장 기본값 false/0 호환. 구저장에서 진행 중인 로켓은 첫 공세 틱에서 첫 파동 대기 시작. 큰 dt 한 번에 여러 밀린 파동을 몰아서 만들지 않는다.
- 시민 어댑터는 새 저장 객체를 만들지 않는다. 시민 위치·피해는 기존 인구 상태에 저장되고, 적의 위치·공격 쿨다운은 기존 런타임 상태에 보존된다. 시민 공격 대상은 로드 후 재탐색한다(아래 위험 참조).

## 수정 파일

- `Assets/_Project/Scripts/World/ColonyInvasion.cs`: 신규 위기 차단, 기존 파동 계산 재사용, 내장 최종 공세 생성.
- `Assets/_Project/Scripts/World/LocalIncursions.cs`: 신규 위기 차단, 기존 이동대 도착 전용 TryArriveRaid API.
- `Assets/_Project/Scripts/World/ColonyEvents.cs`: 위기 추첨·야행성 적 차단. **TryEdge/AcceptMigration의 ResourceManager.HomePosition 유지.**
- `Assets/_Project/Scripts/World/WildMonster.cs`: 시민 대상 선택·피해 어댑터.
- `Assets/_Project/Scripts/World/SettlementDefense.cs`: 신규 습격 차단·포획 경고.
- `Assets/_Project/Scripts/Core/GameManager.cs`: 구조 가능성 패배 판정·DefeatReason.
- `Assets/_Project/Scripts/Core/RocketCampaign.cs`: 내장 공세·저장 타이머·소집 유지.
- `Assets/_Project/Scripts/UI/ThreatIncidents.cs`: RocketIncident의 가동/탑승 가능/파괴 전환 알림·중복 억제.
- `AgentScripts/ImplC3Checks.cs`, `docs/IMPLEMENTATION_BOARD.md`, 이 문서.
- graphify 갱신 산출물은 게임 코드 병합 대상에서 제외.

## 연결 요청 — 소유 밖 파일은 수정하지 않음

1. **G39 / World/DiplomacyRaids.cs::CheckRaids, DispatchRaid, TryDispatchIndependentRaid 및 World/DiplomacyManager.cs::Raid**
   - 신규 전쟁/정복 습격 추첨·출발에 SuppressNewCrises 연결. 외교 전체 Tick을 return하지 말 것. Data.elapsed가 계속 흐르므로 절대시각 nextRaid/nextConquestCheck도 차단 기간을 누적하지 않게 연기/재예약해야 함. 현재 C3의 최종 생성 직전 차단만으로는 외교의 이동대 대기열·출발 비용·연체 추첨까지 막을 수 없음.
   - **World/DiplomacyRaids.cs::TickRaidTravel**의 기존 `TrySpawnIndependent/TrySpawnFaction` 도착 호출은 `incursions.TryArriveRaid(c, raid.count)`로 교체. 이 API는 이미 출발한 이동대를 신규 위기로 취급하지 않고 계속 도착·전투하게 한다. 아직 호출자가 옛 API라 가동 중 기존 이동대도 도착 대기하며 유예 후 몰릴 위험이 있음. 출발 차단만 하고 이 도착 연결을 빠뜨리지 말 것.
2. **G50 / World/DiplomacyPrisoners.cs::CapturedBy 및 다른 포획 알림 호출자**
   - 기존 포획 알림 한 곳에 CampaignOutcome.CaptureWarning 추가. SettlementDefense.Lose는 이미 표시하므로 동일 포획을 두 번 알리지 말 것. 사망한 장수에는 포획 경고를 띄우지 말 것.
3. **G11 / UI/HUDController.cs::ShowDefeatMessage, UI/GameMenuCampaign 계열 패배 화면**
   - 기존 고정 ‘활동 가능한 장수가 없습니다’ 대신 GameManager.Instance.DefeatReason 사용. 기존 OnDefeat(Action) 시그니처는 보존했고 이벤트 호출 전에 사유 설정. 현재 C3에서 ToastManager.SetCrisis("defeat", reason)으로도 전달하지만 기존 일반 토스트/결과 화면은 여전히 옛 문구일 수 있음.
   - 이미 패배한 저장을 로드할 때 사유 표시까지 보존하려면 Save DTO의 GameManager 플래그와 함께 reason 필드를 추가/복원해야 함. 이번 소유 밖이므로 미수정.
4. **G45 / UI/GameMenuRocket.cs 가동 전 미리보기**
   - ColonyInvasion의 잠정 환산(인구10/재산100당+1, 기존 설정·난이도·상한 적용)을 예상 강도로 표시. 현재 고정 스냅샷의 재산·인구 입력은 공개되어 있으나 파동 예상 병력 표시 호출자는 소유 밖. 확정 밸런스가 정해지면 코드 상수와 UI를 함께 교체.
5. **G41 / Save/SaveRuntime·SaveOrders 및 검사 담당**
   - 새 RocketCampaign 필드 실제 저장 왕복과 최종 공세 visitor 생성/복원·활성 상한을 검증. 시민 어댑터는 SaveReferences.Key가 null이므로 현재 타깃 위치 ID는 저장되지 않으며 로드 후 재탐색. 동일 시민 대상을 정확히 이어야 한다면 WildMonsterDto에 civilianLocation ID를 추가하고 WildMonster.CaptureRuntime/RestoreRuntime에 연결(스키마 파일 소유 밖).

## 검사 목록 / 검증

- `ImplC3Checks.Run()`은 동기 string 반환 진입점. Play의 불시착 장수1~2명 상태에서 호출. `Main()`은 기존 RunChecks 로더용 async 진입이며 검사 전용 불시착1명 게임을 직접 시작한다. 비축더미 검색/가정 없음. 로켓·침공 소스·적 템플릿·주거는 검사 안에서 생성.
- 검사 항목: 위기 종류 전체 차단/비적대 허용, 기존 냉해·홍수·가뭄 지속, 추첨 타이머·밤 포식자 날짜 소비, local 타이머/직접 생성 차단, 내장 생성기 가용성·실제 NavMesh 최종 파동·방문자 등록, 스냅샷 복제, 준비/탑승 공세 지속, 공세 타이머 JSON 왕복, 가동/완료/파괴 알림 중복 억제, holdDeployment 설정/해제, 파괴 후449/450초 경계, 시민9+1 피해/보호/주거 파괴, 자연 회복 가능 시 패배 방지/구조 불가 사유, 공통 포획 경고.
- 검사는 전용 새 게임에서 실행할 것. 최종 패배 이벤트가 결과 화면을 열 수 있다. 설정·시각·인구·장수 상태·타이머·난수·생성 객체를 복원하지만 UI 결과 화면·이력 기록까지 되돌리는 범용 트랜잭션 검사는 아니다.
- **검사 작성만 했으며 PASS 결과 없음. 컴파일 성공을 주장하지 않음.** 정적 diff·참조 심볼 점검 결과는 작업 종료 시 아래에 갱신.

## 남은 위험

- 외교 연결 요청1을 합치기 전 G39 전체 완료 아님. 기존 이동대는 계속 진행해야 하므로 도착 전용 API 연결이 필수.
- 실제 NavMesh의 원본 소굴 스폰 지점이 도달 가능해야 최종 파동이 생성된다. 실패 시 기존15초 재시도이며 맵 가장자리 대체 스폰을 새로 발명하지 않았다.
- 로켓 알림 키는 EntityId+가동 snapshot 시각+단계. 같은 세션 반복 스캔/OnDisable 중복은 억제하나 로드로 EntityId가 바뀔 때 중복 방지의 영속성은 실제 왕복 확인 필요.
- 민간인 전투·최종 공세·패배가 함께 발생하는 프레임, 병력 자동 귀환20초 경계·새 소집·수동 귀환 및 실제 저장 왕복은 Claude의 통합 Play 검증 필요.
- Units/Buildings/Save 등 금지 파일·prefab·원본 프로젝트 접근·Unity·Notion·git commit/stash/push 수행 없음.
