# IMPL-B9 인계 — 2026-10-24 재개 작업

- 상태: 기존 diff 보완·검사 작성·인계 정리. **전체 완료 아님. 일반 플레이 로켓 가동은 연료·경제 집계·최종 공세 연결 전까지 차단됨.**
- 원본: `docs/PLAY_CHECK_DECISIONS_1023.md`. 달력 상수 재사용: 1일150초·1계절900초·준비15일2,250초·파괴 후 유예3일450초.
- Unity 실행·MCP·컴파일·RunChecks·실제 저장 왕복·화면 검증 미수행. 원본 프로젝트 접근, 금지 파일/prefab 수정, Notion, commit/stash/push 없음.
- 기존 구현을 유지하고 이번 재개에서 탑승 후보를 반영한 확인 미리보기, 저장 검증 경계, 항공 귀환 위치의 지도 범위, 검사·인계를 보완함.

## G별 상태

| 항목 | 상태 | 구현 / 남은 부분 |
|---|---|---|
| G20 | 부분 | 건조→가동/15일 준비→탑승→발사·파괴 후 새 건물 재건. 기본10석+50석×20=1,010석, 장수와 병력 각각1석. 가동 후 개조 잠금·중단 불가·준비 완료 뒤에도 공세 콜백 유지. 실제 공세 생성·예상 강도 미연결. |
| G39 | B9 조회 API 완료 / 통합 부분 | Operating·SuppressNewCrises 제공. 파괴 후450초 유예 저장. 신규 위기 생성기 연결·밀린 추첨 폐기는 다른 소유 파일이라 미수정. |
| G43 | 부분 | ID 기준 탈출/잔류/외부/포로/사망/이탈 분류·안전 귀환/사망 중복 제거·엔딩 표시. 탑승 확인에서 후보와 병력 포함 예상 결과 표시. 일반 시민 탑승·지역 인구 총계·명부에서 제거된 과거 이탈자 ID 기록 미연결. |
| G45 | 부분 | 시간가중 직전900초 평균과 현재값의 max·가동 시 복제 고정·이력 저장·현재/평균/고정 입력 미리보기. 전 지역 실물/이동 화물 평가기와 예상 강도 공식 미연결. 임의 공식 없음. |
| G49 | 코드 구현 / 실행 미검증 | GameManager.LateUpdate에서 탑승 갱신→발사→패배. TryDepart는 요청만 저장, Die가 같은 틱 요청 취소. 수면·치료·이동·탑승도 생존에 포함. 전체 프레임 순서 통합 검증 필요. |
| G50 | 부분 | 억류 포로 제외·안전 귀환자 포함·마지막 포로 패배 유지. CaptureWarning 문구 제공, 실제 포획 알림 연결 미완료. |
| G52 | 부분 | TryLoadFuel이15일분 전량1회 소비·적재 상태 저장. 내장 전원 타이머·부분 손상 때 유지·이동/철거/개보수 잠금. 연료 종류/일 소비량·적재 UI 미확정/미연결. |
| G53 | 코드 구현 / 화면 미검증 | 기존 독립 사망 모드 재사용. 시작1명+Harsh면30% 잠정 수치와 첫 쓰러짐 즉사 시 패배 상시 HUD. 시작 인원 저장, 실제 확률은 기존 CommanderDeathRuntime 사용. |
| G69 | 부분 | 건물과 독립된 본거지 기준점 저장, Anchor/TryGround/TryAir 제공. 육상 지도 가장자리 NavMesh, 항공 기준점 인근 본거지 범위 빈터 탐색. 실제 귀환·잔류 적 대기·병력 시민 반환·창고 없는 화물 바닥 보존 미연결. |

## 수정 파일 — 전체 B9 diff

- `Assets/_Project/Scripts/Buildings/AirshipYard.cs`, `AirshipYard.Rocket.cs`: 상태·연료·좌석·준비·탑승·발사·파괴·저장 검증.
- `Assets/_Project/Scripts/Buildings/Demolition.cs`: 가동 로켓 철거/이동/개보수 차단.
- `Assets/_Project/Scripts/Core/RocketCampaign.cs`, `RocketEconomy.cs`: 위기 유예·경제 이력·고정 입력·엔딩·본거지 상태.
- `Assets/_Project/Scripts/Core/CampaignOutcome.cs`, `HomeReturnPoint.cs`, `GameManager.cs`: 분류/생존·귀환 위치 API·틱 말미 발사/패배.
- `Assets/_Project/Scripts/UI/GameMenuCampaign.cs`, `GameMenuRocket.cs`, `GameMenuEnding.cs`, `RocketSafetyHud.cs`: 기존 MenuTheme 재사용·확인/결과/상시 경고.
- `Assets/_Project/Scripts/Save/SaveDtos.cs`, `SaveSnapshot.cs`, `SavePreflight.cs`: B9 상태 필드·캡처/복원·검증. CurrentVersion=19 유지. 타 묶음 필드 보존. 장수+병력 좌석 상한·미래 스냅샷 시각도 검증.
- `AgentScripts/ImplB9Checks.cs`, `README.md`, `docs/IMPLEMENTATION_BOARD.md`, 이 문서.
- `graphify-out/` 대량 diff는 직전 세션부터 존재하는 재생성 산출물이며 게임 코드 패치/커밋에서 제외할 것.
- `git status`의 staged 변경에는 다른 묶음에서 복제된 Boss/Commander/Expedition 등의 기준 변경이 포함됨. B9 인계 대상은 위 파일의 **unstaged `git diff`**이며 staged 변경 전체를 B9 작업으로 취급하지 말 것.

## 연결 요청 — 파일·메서드·의도

1. **최종 공세 담당 (G20/G45):** `Core/RocketCampaign.cs.AssaultTick(AirshipYard,RocketSnapshot,float)`에 생성기 연결. snapshot.Wealth/Population은 고정 입력이고 콜백에는 복제본 전달. Preparing/Boarding 동안 계속 호출되며 발사/파괴 뒤 중단. 구독만으로 AssaultAvailable=true가 되므로 빈 핸들러로 차단을 우회하지 말 것. 결정 문서에 적 구성·주기·강도 환산 공식이 없어 생성기와 가동 전 예상 강도 미리보기는 미구현.
2. **경제 담당 (G45):** `RocketCampaign.Instance.Economy.Observe(GameCalendar.GameSeconds,wealth,population)`에 새 게임 시작부터 변화 시점별 평가 전달. 전 지역 소유 실물·바닥 물자·이동 화물 포함, 예약 중복 제외. `Buildings/AirshipYard.Rocket.cs.ActivationPreview`는 마지막 관측을 현재값으로 사용하므로 가동 확인 직전까지 갱신할 것. `TryActivate(currentWealth,currentPopulation)`도 같은 평가기를 사용. 창고 잔액만 넣으면 사양 충족 아님.
3. **연료/로켓 UI 담당 (G52):** 규격 확정 후 `AirshipYard.TryLoadFuel(AntColony.Data.ResourceType,unitsPerDay)` 호출 UI 연결. 현재 일반 플레이 호출자 없음. 검사에서 Special/일1은 경계 확인용 가짜 규격이며 실제 사양으로 채택한 수치 아님.
4. **침공/사건 담당 (G39):** `World/ColonyInvasion.cs.Update`는 TickEconomy 유지, 새 파동 타이머/생성만 SuppressNewCrises로 차단. `World/LocalIncursions.cs.Update/Spawn` 신규 적대 무리 차단. `World/ColonyEvents.cs.Tick/Eligible/TryTrigger` 신규 질병/재해/적대 사건만 차단. 기존 actor·질병·날씨·화재 진행, 외교/비적대 사건 유지. 전체 Tick 조기 return 금지. 차단 중 추첨 누적·재개 직후 밀린 위기 연속 실행 금지. SuppressNewCrises는 이미 파괴 후3일 유예 포함.
5. **B5 알림 담당:** `UI/ThreatIncidents.cs.RocketIncident`는 B5 소유라 미수정. 가동/준비 완료/파괴 알림을 AirshipYard.Phase/PreparationRemaining에 연결. `World/SettlementDefense.cs.Lose`, `World/DiplomacyPrisoners.cs.CapturedBy` 계열 포획 알림에 `CampaignOutcome.CaptureWarning` 표시(중복 방지).
6. **G16 시민 이송/인구 담당 (G43):** `CampaignOutcome.Capture`는 ColonyPopulation.Total(없으면 AntPool.Total)에서 탑승 병력을 빼 시민 잔류 계산. 이 총계가 편입 지역·수송/배속 병력까지 포함하는지 확인하고 일반 시민 탑승과 지역별 인구를 한 번만 합산하도록 연결. 명부 제거 시 이탈자 고유ID·이름·분류 저장 경로 필요. CampaignHistory의 이름만으로 ID를 만들어 합치지 말 것.
7. **귀환/장수 담당 (G69):** `World/ExpeditionTransport.cs.Tick` Returning→Home, `TryUnloadCrew`, `UnloadCargo`에서 `HomeReturnPoint.TryGround(from,out point)`/`TryAir(out point)` 사용. false면 안전 지점을 찾을 때까지 귀환/선상 상태 보존. `World/DiplomacyPrisoners.cs.Arrive(Homecoming)` 위치도 Anchor 기준으로 연결. `Units/CommanderAnt.cs` 병력 반납은 징집소 없이 기존 인구/풀 반환 경로 사용. 잔류 적이 있으면 해산·하역 보류, 창고 없으면 DiplomacyManager.DropFloor로 실물 보존·중복 지급 방지. 해당 파일들은 수정 금지여서 API만 제공.
8. **B3 저장 담당:** SaveDtos/SaveSnapshot/SavePreflight의 B9 hunk만 B3 v19 최종본에 병합(통째 덮어쓰기 금지). 준비/탑승/발사 요청/파괴 유예·이력·본거지 실제 왕복 검증. rocket=null 구저장 호환. 옛 즉시 발사 저장의 발사대는 Construction으로 복원되므로 탑승자를 하선 후 새 가동 절차 필요. 구저장 startingCount 부재에 따른1명+가혹 경고 복원과 옛 좌석 초과 저장 이관 방침도 확인.

## 검사 목록 / 검증

- Claude가 Play에서 `AgentScripts/RunChecks.cs` 경유 `ImplB9Checks` 실행. `public static async Task<string> Main()`, 실패 예외·성공 PASS n. **작성만 했고 실행하지 않아 PASS 결과 없음.**
- 검사 전제: 불시착2명 새 게임 직접 시작·비축더미 검색 없음. 로켓/BuildingData/포로 대상 객체 직접 생성. 기존 ResourceManager 복원/추가 API로 자원 준비. 임시 저장 루트·자동저장 비활성·설정/난수 복구.
- 검사: 달력·시간가중 평균/max/계절 만료/NaN, 저장 단계/연료 모순·파괴 탑승자,10~1,010석, 준비 전 탑승 차단, 연료 부족 무소비/전량1회 소비, 가동/개조 잠금, 손상/외부 무전력 타이머, 스냅샷 복제, 로켓 JSON 왕복, 준비 완료 뒤 공세 지속, 후보 포함 미리보기·무부작용, 탑승/하차/치료 생존, 분류 중복/시민 합계, 마지막 포로·포획 분류 우선, 안전/외국/사망 귀환자,1명+가혹, 본거지 JSON, v19/구저장 null, 발사 요청 중복/저장, 같은 틱 파괴 우선,450초 유예 경계, 재건 후 발사.
- JSON 복제 검사는 실제 디스크 왕복을 대체하지 않음. 마지막 검사가 발사로 게임을 끝내므로 검사 전용 새 게임에서 실행할 것.
- 정적 확인: 참조 심볼을 rg로 검색해 존재 확인(외부 프로젝트 주요 심볼46개 및 추가 참조), `git diff --check` exit0. 검사61개 assertion 지점 작성(실행 횟수/PASS 수 아님). `graphify update .` 수행 후 graph.json JSON 파싱·8,634노드 확인. 컴파일 성공을 의미하지 않음.

## 남은 위험 / 다음 순서

1. 병합 후 Unity 컴파일·신규 .cs.meta 생성 → ImplB9Checks → ImplA2Checks/SaveRoundtripChecks/TransportRouteChecks/SettlementDefenseChecks 회귀.
2. GameManager 실행 순서10000의 LateUpdate는 Update 피해 후 판정하나 다른 LateUpdate 피해 및 SaveSystem 자동 저장과 순서 통합 확인 필요. ResolveLaunch 직접 호출 검사는 전체 프레임 검증을 대체하지 않음.
3. NavMesh 귀환 탐색은 실제 전소 지형에서 미검증. 육상은 지도 바깥2m 이내 경계, 항공은 반경20m·2m 간격/16방향 후보. 길이 없으면 false, 좁은 빈터를 놓칠 수 있음.
4. 이탈자·지역 인구/일반 시민 탑승·위기 차단·귀환·공세/연료/경제 연결 전 G20/G39/G43/G45/G50/G52/G69 전체 완료로 보고하지 말 것. UI 육안과 실제 저장 검증도 남음.
