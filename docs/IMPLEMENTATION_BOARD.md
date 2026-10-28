- **GRAPHICS-1024 / Codex / 연결·정적/Blender 검증 완료, Play 검증 대기** — 추가 소유: World/ResourceNode.cs·BossStronghold.cs·Corpse.cs, Core/CampaignResearch.cs(Awake 표시 연결만), 신규 KemonoAppearance·ProductionProp·CorpseProp·BossRewardVisual, AgentScripts/FacilityVisualChecks.cs·BetaChecks.cs·AntWorkVisualChecks.cs·KemonoQualityChecks.cs. UI 파일 수정 없음. 상세 docs/HANDOFF-CODEX-1024.md 및 docs/handoff-kemono/GRAPHICS-1024.patch. 공식 CLI/승인/worktree 제한은 해소되지 않음.
- **KEMONO-UNITY-1024 / Codex / 반입·정적 컴파일 완료, 원본 연결·Unity 검증 차단(2026-10-24)** — 소유: `Units/AntVisual.cs`, 신규 `Units/KemonoModel.cs`, 신규 `Editor/KemonoImport.cs`, `World/Corpse.cs`(모델 대체), `Buildings/FacilityVisual.cs`(Models 표), `Assets/Resources/Kemono/**`, `Assets/Resources/Facilities/`(새 FBX), `Assets/_Project/Art/Kemono/**`. FBX 175개 사본·신규 코드 반영. 기존 소스 3개는 쓰기 거부로 미수정, `docs/handoff-kemono/KEMONO-UNITY-1024.patch` 적용 대기(사전 검사 통과). 적용 대기 코드+임포터 정적 컴파일 오류 0, 자산 검사 통과. 공식 CLI 경로 접근 거부·Claude 잠금 유지로 Run()/공식 recompile/Play/3개 검사/캡처 미실행. **게임 교체 완료 아님.** 상세 `docs/IMPL-KEMONO-UNITY-HANDOFF.md`. UI·커밋·SAVE 없음.
- **UI-1024 / Claude / 진행 중(2026-10-24)** — 소유: `UI/**`(WorldMapPanel·WorldPlanet·MenuTheme·HudResponsiveLayout 등 UI 전부), `AgentScripts/UiButtonAudit.cs`. Codex는 UI 폴더 수정 금지. Unity 실행(Play·검사)은 한쪽씩: 사용 전 `Temp/unity-lock.txt`에 담당·시각을 쓰고, 끝나면 지운다.
- **IMPL-B3 / Codex / 패치·검사 준비, 원본 반영 대기(쓰기 거부)** — 실제 수정: `AgentScripts/ImplB3Checks.cs`, 이 보드, `docs/IMPL-B3-HANDOFF.md`, `docs/patches/IMPL-B3.patch`. 원본 `Assets/_Project/Scripts`는 셸·apply_patch 모두 Access denied라 미수정. **현재 게임에는 기존 저장 거부가 남아 있음.** 상세 파일 목록·검증·남은 연결은 인계 문서 참조.
  - 패치 소유: Save/SaveBuildings·SaveCatalog·SaveConstruction·SaveDtos·SavePreflight·SaveSnapshot·SaveSystem·SaveValidator, 신규 Save/SaveOrders·SaveOrderValidation·SaveRuntime; Units/CommanderWork·SoldierAnt·WorkerAnt(저장 상태·최종 목적지); World/ColonyInvasion·EnemyCommander·LocalIncursions·SettlementDefense·TransportRoute·WildMonster(저장 Capture/Restore·생성/목록 재연결). CommanderAnt.cs·B1/B2/B4 소유·사용자 금지 파일·전투 피해 처리 수정 없음.
  - 패치: v19 이관, 상태별 저장 거부 제거, 런타임 적·장수 작업/운반 예약·거점 방어·자동 수송 순번/집결/승무원 순서·소굴 확장 노드 보존. 비용/보상 재실행 없이 재연결, 난수 마지막 복원. 자동 저장 LateUpdate, 기존 수동 UI는 일시정지 동기 저장.
  - 검증: 패치 소스 복사본+현재 전체 게임 소스+ImplB3Checks 정적 컴파일 exit 0, 오류 0, 기존 BuildingConstructionSite.cs:33 CS0252 경고 1건. `git apply --check --whitespace=error docs/patches/IMPL-B3.patch` 통과. **Unity/검사 실행 미수행**, 왕복 성공으로 해석 금지. RootOverride 격리·설정 복원·ResourceType 전체 이름 사용.
  - **남은 연결:** 쓰기 가능한 세션에서 패치 적용 → 신규 .meta·Unity 컴파일 → ImplB3Checks/SaveRoundtripChecks/ImplA2Checks/TransportRouteChecks/SettlementDefenseChecks 실행. 실행 중 외부 동기 TrySave까지 전체 프레임 말미로 제한하려면 대기형 API와 호출자 연결 추가 필요. B1 정찰 history 필드는 기존 경로 재사용하나 실제 왕복 미검증; B4 Water 배열 확장·위생/기대/배회 실제 왕복도 확인 필요. 아직 G41/G09 완료 아님. prefab·커밋·SAVE/일지 없음. 카카오 도구 없음.

- **IMPL-B4 / Codex / 구현·정적 컴파일 완료, 연결 요청 남음** — 수정 소유: Units/CommanderPersonalState.cs·CommanderPersonal.cs·CommanderHygiene.cs·CommanderDuty.cs, Buildings/HygieneFixture.cs·RecreationSpot.cs·BuildingPlacementController.cs, UI/MoodWatch.cs·GameMenuRoster.cs, Data/ResourceType.cs(Water 끝 추가·물 라벨), 신규 Core/GameBalanceNeeds.cs·Buildings/BasicLivingTemplates.cs, AgentScripts/ImplB4Checks.cs. CommanderSleep/Meal·GameCalendar·GameBalance·ColonyPopulation*·FoodLots·AntPool·Save/*·CommanderAnt/SoldierAnt·HudClock·ColonyEvents·ResourceNode 및 B1/B2/B3 소유 파일 수정 없음. 기존 타 작업 diff는 보존.
  - G60: 개인 틱에서 150초당 위생 25 감소(수면·식사에도 진행), 구덩이 변소 3초/+50/상한70/물0, 세면대 물1/+30, 샤워 물3/100. 물은 사용 시작 시 1회 소비. 위생 <30/-3, <10/-6; 위생으로 질병·부상·사망 추가 없음. 시설 없음/급수 부족/접근 실패면 3초 임시 용변→30·기분 -3 150초(재시도는 기존 30초 간격 재사용). 원정 Outbound/Returning 이동 시 달력 날짜당 한 번 최소30. temporary·travelDay는 기존 개인 JSON 상태에 추가.
  - G31/G10: 기본 기분60 재사용 + 기대 수준 15/5/0(expectationTier 저장·범위 검증). 35/20/5 단계별 경고·부정 원인·이탈 잔여초 표시. 마지막 비포로 우호 장수의 Flee는 새 Wander(기존 enum 번호 보존) 60초 배회로 대체, 기존 Idle 유지. 잠자리 무료·즉시 지정(재료/인력/작업0), 기본 침상·변소·세면대 시민 차출0, 모닥불 1인. 무연구·무전력 해금/기존 식탁·소지 식량·생식·바닥 수면 재사용. 식사→수면→위생→시체 작업/오락 순, 생활 욕구 시 짐 내려놓기·오락 중단.
  - 정적 검증: Unity 6000.5.8f1 내장 Roslyn + Bee 응답 파일(동시 작업 신규 소스 포함)로 전체 게임 소스 및 ImplB4Checks 컴파일 exit 0, diff --check 통과(기존 LF/CRLF 안내·obsolete 경고 존재). ImplB4Checks.Main은 public static async Task<string>, 임시 SaveStorage.RootOverride·자동저장 비활성/복원, ResourceType 전체 이름 사용. 규칙 경계·시설 물 소비/부족·중복 소비 방지·JSON 상태·생식/바닥수면·마지막 장수 검사 작성. **Unity 실행·검사 실행·UI 육안·실제 save/load 미수행**, 실행 통과로 해석 금지. .meta는 Unity 재개 시 생성 확인. prefab·커밋·SAVE/일지 기록 없음.
  - **연결 요청 — 재산/생활 담당:** 현 코드에 정착지 재산 등급 판정·금액 경계가 없어 expectationTier는 기본0(저재산) 유지. 정착지 공통 판정 확정 시 전 장수 expectationTier=0/1/2 연결 필요(수치 임의 발명 안 함). 독립 의자 BuildingKind/템플릿/건설 항목도 없어 기존 BigTable을 재사용했으며 독립 의자 추가는 미구현.
  - **연결 요청 — 급수 담당/B3:** 기존 Water 자원·급수/채수 경로가 없어 ResourceType 끝에 Water만 추가해 기존 창고 자원 소비 API에 연결. 실제 물 확보 경로는 미구현. B3는 구저장 자원 배열 길이 확장/이관 및 Water·expectationTier·hygiene.temporary/travelDay·MentalBreak.Wander 실제 save/load 확인 필요(Save/* 수정 안 함). 기존 JSON CapturePersonalState/RestorePersonalState 경로 재사용.
  - **검사 인계:** AgentScripts/HygienePowerChecks.cs의 옛 규칙(시설 없으면 참음, 화장실70 초과)은 G60과 의도적으로 달라져 갱신 필요. Graphify update 완료(10219 nodes/21494 edges). 카카오 완료 알림 도구는 세션에 없음.
- **IMPL-B1 / Codex / 구현·정적 컴파일 완료, 연결 대기** — 소유: Buildings/ConscriptionPost.cs·ScoutPost.cs·EarlyConstruction.cs·RuntimeBuildingTemplates.cs, Core/ColonyPopulation.Military.cs(신규)·CampaignHistory.cs, Units/CommanderAnt.cs(증원 상한 검사만), UI/GameMenuPopulation.cs·HudOverview.cs, Data/ScoutPostData.asset, AgentScripts/ImplB1Checks.cs. G32/G04/G36 최소 구현·정적 확인. Claude 금지 파일·Save/*·prefab 수정 및 커밋 없음.
  - 완료: G32 징집소·정찰 초소 무연구/목재 20·건설 시민 차출 0(첫 동원 관문 제거 방지), G04 성인 기준 최소 동원 5·제도 상한/지휘 잔여 검사·증원 우회 차단/납세 제외 유지, G36 동일 빈 슬롯 실패 +20%p·4회차 확정·성공/슬롯 변경 초기화·초소 간 공유. 막사/훈련장 상한 추가 및 징집소 제거 시 병력 회수 로직 없음(기존 유지).
  - 검증: Unity 6000.5.8f1 내장 Roslyn + Bee 응답 파일/동시 작업 신규 소스 포함 전체 소스와 AgentScripts/ImplB1Checks.cs 정적 컴파일 exit 0, diff 공백 검사 통과. 검사 스크립트의 public static async Task<string> Main() 작성(ResourceType 전체 이름 사용). Unity 실행·검사 실행·save/load·화면 검증은 미수행. 새 partial .meta는 에디터 재개 시 생성 확인. Graphify update 최종 완료(10168 nodes/21324 edges). 커밋·prefab·SAVE 기록 없음.
  - **Claude 연결 요청(ColonyPopulation.cs 소유 B2도 확인):** 기존 MaxSoldiers(homeThreat) 본문은 MobilizationLimit(homeThreat)로, TryDraft(count, homeThreat) 본문은 TryMilitaryDraft(count, homeThreat)로 위임해 구 API 중복 계산을 제거할 것. B1은 금지 파일을 수정하지 않으며 실제 편성·증원·HUD·인구 UI는 새 partial 계산 사용.
  - **Claude 저장 연결 확인 요청(B3도 확인):** CampaignHistory.State.scoutRecruitSlot(기본 -1), scoutRecruitFailures(0~3) 추가. 기존 history Capture/Restore 전체 경로로 저장되므로 SaveSnapshot 변경 불필요. 구저장 기본값 및 실제 save/load roundtrip 검증 필요.
  - **Claude UI 연결 요청:** GameMenuConscription.cs 쓰기가 도구에서 거부되어 미수정. 각 행 post.AvailableTroops(c)로 slider.maxValue·토글 가능 여부·표시를, 전체 합계·출전 버튼은 population.AvailableMilitary(EnemyAlert.CrisisActive)로 연결할 것. 실제 TryDeploy는 새 상한 검증을 이미 적용.

- **IMPL-B2 / Codex / 구현·정적 컴파일 완료, 연결 대기** — 수정: Core/ColonyPopulation.cs·AntPool.cs·FoodLots.cs, 신규 Core/GameBalancePopulation.cs·ColonyPopulation.Ages.cs, AgentScripts/ImplB2Checks.cs·Phase4Checks.cs·Spec1010SpaceChecks.cs. B1 소유·GameCalendar·GameBalance·Save/*·사용자 금지 파일·prefab 수정 및 커밋 없음.
  - G33: 450초마다 빈 정원×0.1×D, D 0~2·빈 인구 보정 0, 소수 누적·정수 입주·정원 제한. immigrationStopped=true이면 타이머/소수 누적 모두 정지(기존 소수 보존). 기존 월간 민심·탈주 300초 유지. 행정 분모 0 처리는 기존 Admin partial 재사용. 보정 계수: 서비스 FacilityDemand/100×0.5, 안전 (SafetyDemand-100)/100×0.5, 민심 (sentiment-60)/100, 행정 기존 AdminDemand×Administration/100. 미관은 민심에만 반영, 별도 이주 보정 제거.
  - G38: ages[0..64] 집계·매년 3600초에 18/65세 전환. 이주 10/80/10 각각 ±5%p 안에서 변동·최대 나머지 배분, 그룹 안 나이는 균등 정수 배분. 개체 추적 없이 외부 Breed/손실 차이는 다음 연령 처리 때 집계 보정. 월간 전원 성년·비율 노화/사망 폐기. agingDue는 이미 old에 포함한 풀 정산 대기 노년 인원으로 변경; 복귀 시 풀에서만 제거·전사는 즉시 인구 차감. ages=null 구 저장은 Restore에서 old에 기존 agingDue를 1회 더해 이관.
  - G30: FoodPreservation 종류별 450/900/3600초 상온 기간·조리식 두 등급 450초, 기존 lots 이동 시 잔여 신선도 유지. 기존 일반 창고 DecayRate=1·냉장/냉동만 예외 확인. 현재 통합 ResourceType.Food는 원산지 정보가 없어 안전한 기본 450초 사용. **원산지별 실제 생산 연결은 미완료**.
  - **Claude/B3 저장 검증 요청:** State.immigrationSeconds [0,450), immigrationRemainder [0,1), ageSeconds [0,3600) 유한수, ages=null(구 저장) 또는 길이65·각 항목 비음수, immigrationStopped bool 자동 JSON 왕복, agingDue≤old 및 풀 동원+예약 인원 검증(구 저장은 이관 전 old 포함 여부 구분). SaveRoundtripChecks agingDue fixture도 새 의미로 검증. 실제 save/load 미수행.
  - **B1 연결 요청:** ColonyPopulation.Military.cs의 elderlyService 가용/추가 차출은 S.old 전체 대신 Max(0,S.old-S.agingDue)를 사용해야 이미 풀에 있는 노년의 중복 차출을 막는다. 기존 MaxSoldiers/TryDraft의 새 partial 위임은 이 수정과 함께 적용 필요(보드 기존 B1 요청 유지). B2는 B1 파일 수정하지 않음.
  - **Claude 생산/UI 연결 요청:** ResourceNode·낚시/사냥·농장·시작 보존식 생성 시 FoodLots.ShelfLife(FoodPreservation.종류)로 lot을 생성해 기존 FoodLots 운반 경로에 전달. 생고기/생선/조리3일·열매/버섯6일·곡물/견과/시작식24일. 출처를 모르는 Food 일괄 추가는 현재450초. 수용 중단 UI는 SetImmigrationStopped(bool)에 연결 필요. 실제 Source별 연결 전 G30 전체 완료로 기록하지 말 것.
  - 검증: Unity 실행 없이 Roslyn 본체 및 ImplB2Checks/Phase4Checks/Spec1010SpaceChecks 정적 컴파일 exit 0. B2 규칙별 경계·정수배분·동원/전사·상온 만료·JSON 상태 검사 작성(실행 안 함). 기존 검사의 월간 이주/노화·별도 미관 수요 가정 교체, G08 일일 세액 기대값 수정. graphify update 완료. 런타임·화면·실제 저장 검증은 미수행. 새 .cs.meta는 에디터 재개 시 생성 확인. 카카오톡 도구 미제공으로 알림 미전송.



# Codex · Claude Code 공동 구현 보드

- **VERIFY-1025 / Claude / 완료(2026-10-25)** — Unity 미실행 검사 C2·C3·C5·C6·Phase4·Fishing·Stage1·SettlementDefense·TransportRoute·G06 실행·실패 수정, 이후 G26. Unity 잠금 `Temp/unity-lock.txt` 사용.
- **IMPL-1025-CODEX / Codex / 코드·정적 컴파일 완료, Claude 연결·Unity 검증 대기(2026-10-25)** — 결정 `docs/DECISIONS_1025.md`. ① R07/R08 식사 실제 접근 가능 식량·신선도 순·청결 보정 제거 ② D01 장수용 약품 3등급 생산(약제대 레시피·연구) ③ D05 특성 등급 생성 25/60/14/1%·개수 95/4.5/0.5%·비전투 다이아 +30% 보정. 소유: Units/CommanderMeal*·Core/FoodLots·Buildings/Processor*·약제 관련 Data·Units/CommanderTraits*·Core/Trait*·신규 AgentScripts/Impl1025CodexChecks.cs. Unity 실행 없음(Claude가 검사).
  - 추가 소유(사용자 승인): Core/StartingCandidate.cs, Units/CommanderWork.cs·CommanderStartingSupplies.cs·WorkerAnt.cs, World/Critter.cs, Buildings/Kitchen.cs(구저장 식사 식단 선택), Data/ResourceType.cs(약초 끝 추가), Buildings/EarlyConstruction.cs·RuntimeBuildingTemplates.cs(약제대 확정 건설비/시간), 신규 Core/TraitRules.cs·MedicineResearch.cs, 신규 Units/CommanderMealSources.cs. 프로젝트 규칙에 따른 생성 인덱스: graphify-out/(graphify update .). 특성 등급은 enum 조회표로 보존해 SaveDtos 변경 없이 저장. CampaignResearch·CommanderAnt·ResourceNode·SaveDtos는 수정 금지, 필요한 연결은 이 항목에 인계.
  - 실제 변경(경로 접두사 `Assets/_Project/Scripts/`): `Units/CommanderMeal.cs`, 신규 `Units/CommanderMealSources.cs`, `Core/FoodLots.cs`, `Buildings/Kitchen.cs`, `Buildings/Processor.cs`, `Data/ResourceType.cs`, 신규 `Core/MedicineResearch.cs`, `Buildings/EarlyConstruction.cs`, `Buildings/RuntimeBuildingTemplates.cs`, `Units/CommanderTraits.cs`, 신규 `Core/TraitRules.cs`, `Core/StartingCandidate.cs`, `Units/CommanderWork.cs`, `Units/WorkerAnt.cs`, `World/Critter.cs`; 추가 `AgentScripts/Impl1025CodexChecks.cs`, 이 보드. `CommanderStartingSupplies.cs`는 apply_patch·일반 파일 쓰기 모두 Access denied로 **미수정**.
  - R07/R08: 완전 경로 가능한 창고만 후보, 실제 접근점 도착 후 집기 → 접근 가능한 식탁 이동. 같은 품목은 창고 우선순위와 무관하게 전체 최저 신선도 lot부터(날것 한 끼가 여러 창고에 나뉜 경우도 보존). 좋은→간단→날것→썩은 순·식단 유지. 구저장 부엌 식사도 접근성/식단 검사, 병상 급식도 같은 접근성/신선도 선택을 재사용(환자 이동 없음). 집은 양/종류·식탁 이동 중 식사는 기존 개인 JSON에 저장. 식중독 바닥/청결 보정 제거, 기존 경과 회복 유지.
  - D01 A: 기존 동물약 recipe 0/1 보존, 장수약 2/3/4 추가(약초2→기초1/8초, 약초2+직물1→일반1/12초, 일반약1+약초2+부품1→고급1/20초). 의료 작업/의료 기술만, 산출·품질 고정. 약초 `Herb`는 ResourceType 끝에 추가, 잎 대체 없음. 약제대 목재20+돌10·15초·무특수/무전력. **처치 배율·공식은 기존 MedicalRules.MedicineFactor/TendQuality에 이미 연결되어 있어 수정 없이 검사로 확인할 것.** 상위약 주문·진행은 연구 완료 검사, 연구 미등록 상태에서도 잠금 유지.
  - **Claude 필수 연결 — Core/CampaignResearch.cs만(직접 미수정):** ScienceTechnology enum 끝에 `Pharmacy, PrecisionPharmacy`를 순서대로 추가. Technologies 배열 초기화 끝의 `};`를 `}.Concat(MedicineResearch.Definitions(ScienceTechnology.Pharmacy, ScienceTechnology.PrecisionPharmacy)).ToArray();`로 변경(기존/새 enum 순서와 배열 index 일치 유지). Topic switch에 `ScienceTechnology.Pharmacy or ScienceTechnology.PrecisionPharmacy => CommanderActivity.Medicine,` 한 줄 추가. helper가 증기 연구400/식100+재료100+Special10, 전기1500/200+200+30·일반약 연구 선행을 제공. 등록 전 일반/고급약은 제조 불가.
  - D05 A: 기존 values enum 끝에 비전투 전문성 골드/다이아 13쌍만 추가(기존 저장 번호 보존, SaveDtos 변경 불필요). Grade 조회표·동일 전문성 등급 배타, 슬롯별 25/60/14/1, 개수95/4.5/0.5, 시작 다이아 제외99로 정규화. 지정한 지표만 골드+.2/다이아+.3; 일반 작업의 같은 지표 가산 보정에 더함. 채집/농사/낚시 산출은 실제 생산 때만 증폭(바닥 재운반·미연구 표본 제외), 적재 한도 준수, 보너스 산출로 XP 재증폭 없음. 장비 제작 속도는 공방만, 사육은 돌봄 지속시간만, 운반은 기존 CarryMultiplier로 연결. 전투 다이아5종/전용 효과 추가 구현은 하지 않음.
  - **남은 연결/한계:** 후긴은 기존 평가/파견 판정 시스템 자체가 없어 `TraitRules.InvestigationMultiplier(c.Traits)`(골드 .8/다이아 .7)를 제공. 평가 오차폭·발각 p·포로 q가 생기는 각 지점에 각각 `errorWidth *= TraitRules.InvestigationMultiplier(c.Traits);`, `p *= TraitRules.InvestigationMultiplier(c.Traits);`, `q *= TraitRules.InvestigationMultiplier(c.Traits);`를 적용할 것(조사 속도에는 적용 금지). 약초의 자연 채집/재배 공급 경로는 현재 없고 임의 출현량·재성장 수치를 만들지 않았으므로 별도 연결 필요. 쓰기 거부된 CommanderStartingSupplies.TickCarriedMeal의 `var m = PersonalState.meal;` 다음에 `if (m.served || m.pickedFood > 0) return false;` 한 줄을 추가해 집은 식사/부분 식량이 있는 동안 소지 식량 중복 시작을 막을 것.
  - 검증: Unity 6000.5.8f1 내장 Roslyn+Bee rsp를 `Temp/off1025/a.rsp`로 복사해 신규 runtime 소스 포함 전체 379개 컴파일 exit0, 오류0(기존 BuildingConstructionSite.cs CS0252 경고2; obsolete 경고는 두 번째 실행부터 숨김). 별도 `Temp/off1025/check.rsp`로 새 검사 스크립트 컴파일 exit0, 오류0. 검사 Main은 public static async Task<string>, ResourceType 전체 이름, 임시 저장 경로/자동저장/시간/난수 복원. 독립 NavMesh 두 섬으로 막힌 창고·도착 전 미소비·전체 최저 신선도/분할 식량, 식중독 확률·기존 치료 품질, 약제 레시피/의료 전담/연구 잠금, 확률 경계·시작 생성/배타·보정/JSON을 검사하도록 작성. **Unity 실행·검사 실행은 하지 않았음. 연구 등록 전에는 마지막 등록 검사가 의도적으로 실패하므로 연결 후 실행할 것.** diff --check 통과. graphify update . exit0(12832 nodes/27484 edges), 생성 인덱스 갱신 및 기존 그래프 자동 백업; 커뮤니티 변경으로 일부 라벨 이름은 hub 기준 자동 명명됨(LLM 재라벨링 미실행). .meta는 Unity 재개 시 생성 확인. prefab/scene·Notion·커밋·SAVE 기록 없음.
- **IMPL-1025-CLAUDE / Claude / D09 완료·Codex 연결(약제 연구 등록·소지 식량 가드) 완료, 나머지 다음 세션(2026-10-25 SAVE)** — D04 진형(출전 편성 선택·능력 보정·현대식 연구), D06/D07 철수·수송 파손·수리, D09 자동 수송 다거점, G26 성능. 소유: UI/GameMenuConscription·UI/GameMenuWalking·Units/CommanderAnt(전투 보정)·World/ExpeditionTransport*·World/TransportRoute·UI/WorldMapPanel·World/ResourceNode*·Core/CampaignResearch(연구 항목 추가).
- **B3-FREEZE-1025 / Codex / 완료(2026-10-25)** — ImplB3Checks 로드 30초 정지 원인 정적 분석. 읽기 전용, 소스 수정 없음. 결과는 `docs/B3-FREEZE-1025.md`에만 작성.

- **IMPL-G08·G01 / Claude / 코드 완료, 통합 검증 대기(2026-10-24):** G08 달력 `Core/GameCalendar.cs`(1일 150초·낮100/밤50·계절 6일·1년 24일, 옛 SecondsPerMonth=300은 비율 환산용), `Map/SeasonVisuals.cs`·`UI/HudClock.cs`·`UI/GameMenuEnding.cs`·`UI/GameMenuEvents.cs` 일 단위 표시, `World/ColonyEvents.cs` 밤 포식자 계절 첫날 밤만, `World/ResourceNode.cs`·`Core/GameBalance.cs` 낚시 계절 한도 300, `Core/ColonyPopulation.cs` 매일 아침 납세(TaxSeconds/DailyFood/DailySoil), `Units/CommanderMeal.cs`·GameBalance 하루 2끼. G01 `Units/CommanderAnt.cs` 병력 0도 1인분 교전·`Units/SoldierAnt.cs` AutoEngages(민간 무병력 장수는 대기 중 자동 교전 안 함). B1 연결: `UI/GameMenuConscription.cs` AvailableTroops/AvailableMilitary 표시·한도. 검사: FoundationChecks 17 통과, 나머지(DayNight·Phase4·ResourceRule·Fishing·Biome·NightThreat·Stage1·SeasonWeather·ImplA2·SaveRoundtrip·ImplG01·ImplB1~B4)는 Codex 작업 종료 후 일괄 실행(동시 수정 중 재컴파일로 실행 중단됨).

- **IMPL-A2 1·3·4번 / Claude / 완료(ImplA2 208·SaveRoundtrip 57 통과, 2026-10-23):** `Save/SaveSystem.cs` NewGame에 `CrashLandingStart.Prepare`; `Save/SaveDtos.cs`·`SaveBuildings.cs`·`SaveSnapshot.cs`·`SavePreflight.cs` 불타는 건물 `WallFire.State` + 서비스 지시(serviceCommander/serviceJob, 건물 복원 뒤 StartService 재연결) 저장/복원; `Save/SaveValidator.cs` v15 이관 14→15칸(소화 16칸은 v17 척도 변환 뒤 `MigrateLegacyJobs`가 9로 추가). 버그 수정: `BuildingPlacementController.GetTemplate(CrashWreck)`가 Data 없는 연구소 템플릿을 잡아 저장 검증 실패 → `FindTemplate<CrashWreck>`. 검사 수정: ImplA2Checks ResourceType 전체 이름·바닥 식량 범위(집 지도)·정리 null 처리, SaveRoundtripChecks 런타임 건물/노드를 순번 키 대신 종류·내용으로 매칭(새 시작의 잔해·돌 20 때문). 남음: 인계 5번(나이 ageMonths ↔ G08 달력), 6번(사망·이탈 시 개인 보존식 처리), 화면 넘침 육안 확인.
- **IMPL-A2 2번 / Codex / 구현·정적 확인 완료:** `Units/CommanderAnt.cs` 기존 무기 매핑·장수 전용 UnitData 사본 재사용, 단궁 원거리 12m·2초/목검 근접 유지; `AgentScripts/ImplA2Checks.cs` 목검↔단궁 재장착 검사 1개 추가. diff·정적 조건 확인 통과, Unity 실행·검사 실행·커밋 없음; Save/*·G01·prefab 수정 없음.

- **IMPL-A2-1023 / Codex / 중단 상태 통합 SAVE:** 불시착 시작·후보 화면(G03/G24), G02/G29/G34/G54/G56/G57 구현. 소유: SaveSystem.NewGame 및 새 게임 생성 경로, GameMenuFront.NewGameScreen(TitleBackdrop 유지), Duty/자율 작업, 건설·제작 데이터, 연구 조건, 휴게·화재, 신규 파일·AgentScripts/ImplA2Checks.cs. Claude 소유 파일은 수정하지 않음. 사용자 통합 SAVE 승인으로 소스·검사 스냅샷 저장. Unity 전체 소스 컴파일 오류 없음, 새 Play 기능 검증 미완료. 필수 연결 요청은 미해결로 유지.
  - Claude 요청(저장/생활 소유): A2의 새 상태가 생기면 이 항목에 DTO/SaveSnapshot 연결 지점을 추가한다. 기존 15칸 작업표는 16칸으로 기본 소화 9 이관 필요. 초기 시민 0·기본 수용 50 제거는 Codex가 시작 경로에서 처리한다.
  - **Claude 필수 연결 요청:** `SaveSystem.cs` NewGame 패치가 파일 쓰기 거부됨(우회하지 않음). NewGame 시작에 `CrashLandingStart.Prepare(options)`를 호출할 것. 후보1~3명·유효성/G03 검증은 해당 메서드가 처리. `CommanderAnt.Role`에서 ShortBow→Ranged, 사거리12·공격간격2초, WoodenSword→Melee 및 병력0 개인전투(G01) 연결 부탁. 시작 ageMonths=18~30*12가 즉시 노환사하지 않게 Claude의 Age/G37 이관과 맞출 것.

- **HUD-V6-WINDOWS-1023 / Codex / 코드 구현·정적 컴파일 완료, Unity 실행 검증 Claude 인계:** 사용자 최신 지시(작업 영역 CLI도 연결 불가면 코드만 마무리 후 인계)에 따라 에디터 조작 중단. `docs/DESIGN_RULES.md` 적용. 커밋/SAVE 없음.
  - 수정: GameMenuController/Duty/Research/V5/Roster/Storage/Diet/Health/Memorial, MenuLayout/MenuTheme/BuildScreen, AgentScripts/HudV6LayoutChecks.cs. 신규: UI/GameMenuDock.cs(하단 메뉴 최종 순서·폭), GameMenuManagement.cs(묶음 탭·기존 기능 연결·미구현 표시), GameMenuSelect.cs(실제 Dropdown). Claude HUD·지도 소유 파일과 GameMenuFront는 수정하지 않음. 신규 .cs의 Unity .meta 생성은 에디터 검증 때 확인할 것.
  - 구현: ≡·작업표·과학·월드맵·물류·식품·의료·관리·생산·자원·인재·편성·＋ 건설. 기록은 의료·관리 안, 장수 관리는 인재·편성 안. 한 줄 제목/부제·밑줄 탭·2단 본문·하단 닫기, 중성 불투명 표면과 20/15/13/12 글자 토큰. 과학 1100×676: 6시대 탭·연구 상태 우측 표시·진행 막대·지시/정지·하단 연구소별 담당 Dropdown. 배정된 연구소는 미지정으로 해제한 뒤 다른 장수를 선택(기존 게임 배정 API 제약 유지).
  - 연결: 창고/식단/작업/퇴비, 의료/동물 의료·목장/묘비/포로/인구/기록, 실제 자원 재고/공방·가공·주방/장비/수리 작업표, 장수/기존 정찰 초소/보유 특성. 평가·14종 진형·매장지·배관·음주·별도 도구 슬롯·통합 수리/위생 대시보드 등 없는 기능은 미구현 표시, 새 게임 로직 없음. 일부 탭은 시설 목록에서 기존 상세 화면으로 이동하는 구조이며 시안의 통합 화면과 콘텐츠 차이 남음.
  - 검증: Unity 6000.5.8f1 내장 Roslyn + 현재 Bee Editor 응답 파일에 신규 3파일을 포함한 전체 소스 정적 컴파일 exit 0. HudV6LayoutChecks/HudV5UnityChecks 두 검사 스크립트 정적 컴파일 exit 0. 기존 BuildingConstructionSite.cs:33 CS0252 경고 1개. 검사 강화: 1920×1080 추가, 9개 하단 메뉴 순서, 네 묶음 전체 탭/닫기, Dropdown 템플릿·펼침, 명시적 미구현 페이지. **Play 검사 통과를 의미하지 않음.**
  - 실행 차단: 기본 CLI 경로 Access denied, 작업 영역 Temp/unity-cli/unity.exe 실행은 가능하나 editor_status가 `No Unity Editor instances found with reachable Pipeline servers` 반환. 공식 MCP도 승인 정책 never로 거부. Codex는 Play·재시작·씬 저장·캡처를 하지 않음. Codex 잠금 해제 완료(이후 Claude 소유 잠금은 건드리지 않음).
  - Claude 다음 단계: 잠금 후 공식 CLI 재컴파일 오류 0 확인 → Temp/play.sh → Temp/check.sh AgentScripts/HudV6LayoutChecks.cs 및 HudV5UnityChecks.cs(후자는 임시 저장 경로의 새 게임 생성) → 각 창 1920×1080/1280×720 캡처를 Temp/v6ref/hd-*.png와 비교 → editor_stop·잠금 해제. 연구소 실제 배정·해제, Dropdown 펼침/선택/화면 밖 잘림, Roster 좁힌 2단 본문 넘침, 공유 MenuTheme 글자 변경의 HUD 영향 우선 검수. 실제 캡처 비교가 없으므로 ‘거의 동일’ 시각 검증은 미완료. 기록(EventLog) 자체는 기존 목록 화면 유지(GameMenuEvents.cs 쓰기가 도구에서 거부되어 변경하지 않음). 완료 카카오 도구 미제공.

- **MAP-HUD-1023 / Codex / 인계·SAVE 미완료:** 사용자 요청: 미니맵 확대·좌표 이동·읽기 쉬운 지형, 가장자리 카메라 속도, 언덕 높이 구분, 월드 이름표/HUD 겹침. 담당 HudConsole.cs, HudResponsiveLayout.cs, IsometricCameraController.cs, TerrainBlend.shader(기존 변경 보존), ResourceNodeStatus.cs, CommanderOverhead.cs, GatherDesignation.cs, 신규 MinimapTerrain.cs 및 관련 AgentScripts. 추가 요청: 수평 테두리 시야, 눈 색·날씨 화면 범위, MapGenerator.cs 층 정보 유실 복구와 Precipitation.cs 입자 전환. SAVE 요청 후 log/changelog 작성, 최신 절벽 실제 캡처 확인. HudV6 17·MapHud 128·SeasonWeather 323·Layout 2070 통과. 비/겨울 실제 검수·Notion·커밋/push·Unity 편집 저장 남음. Claude 한도 소진 후 사용자 요청으로 상세 log 인계. 컴퓨터 종료는 새 작업 요청으로 취소.

- **2026-10-23 SAVE / Codex / 완료:** UI·V6 검증 파일 a483141 develop push. 개발 일지 https://app.notion.com/p/401c4a0ecd31816fb1f2ea2360701ccb (오늘 작업·실제 HUD/과학 캡처), Trouble Shooting https://app.notion.com/p/401c4a0ecd31811b99e4c6aebb4fe995. 자원 이름표/HUD 겹침·관리 창 배경 지형 회색 프레임, 길찾기 큰 물가 4곳은 미해결로 보존. 미완료 지형·길찾기와 출처 불명 씬·설정·기타 미추적 파일은 제외. Play 종료.

- **HUD-V6-UNITY-REFINED / Codex / 구현·실행 검사 완료:** 사용자 요청으로 최신 절제된 V6 시안을 Unity 기존 기능에 적용. 담당: MenuTheme/MenuLayout/HudResponsiveLayout/CommandCard/GameMenuV5/GameMenuController/BuildScreen, 새 MenuViewport, HUD 검사. 배율·폰트·중성 테마·평면 버튼·관리 창 배치, UI 보조 글씨 #968976→#b1b3a7(대비 3.82→6.16). 건설 담당자 0명에서 RefreshPicker가 숨긴 행을 읽는 오류 재현 후 해당 행 건너뛰기. 최종 검증: HudV6Checks 17, HudV5UnityChecks 90, HudV6LayoutChecks 2070(뷰포트 모의 960/1280/1440, 과학·작업표·장수·장비·인구·건설 및 알림 스크롤), BuilderPickerCheck 0명 재현→통과. 2026-10-23 SAVE 캡처: HUD·과학·작업표 실제 화면 확인. 자원 이름표/HUD 겹침·관리 창 배경 지형 회색 프레임 후속 필요. Player 빌드 미수행. 게임 규칙·지형 변경 없음.

- **HUD-V6-TAXONOMY / Codex / 시안 수정·검사 완료:** 사용자 제공 ai-slop-taxonomy.md 검토. 시안만 수정: hud-v6.css/html, hud-v6-people.js, 시안 검사. 중성 표면·등급 문자·행 구분·글자 위계·대비·클릭 피드백. Unity 추가 수정 없음.

- **HUD-V6-TYPE / Codex / 시안 변경 완료·후속 Unity 적용:** 사용자 지적: 글자 크기 불일치, 색상·버튼·보석 장식 과함. 사용자 정정: 시안만 수정. Unity의 이번 폰트·배율·색상 변경은 되돌림. 범위: hud-v6.css. 색상·버튼·보석 장식 절제. 이전 Unity HUD 적용분은 보존.

- **HUD-V6-UNITY / Codex / HUD 동작 검사 16 통과:** 사용자 승인: V6 화면 전체 + 기존 기능 연결. 담당: HudResponsiveLayout.cs, MenuTheme.cs, GameMenuController.cs, 관련 UI 검사. 평가·조사 등 신규 게임 시스템은 제외. 기존 지형 작업 파일 수정 없음. 검사: AgentScripts/HudV6Checks.cs. 새 게임 전환 시 영속 메뉴 소유권 보존, 선택/해제·지도 접기·상세·건설·장수 관리 연결 확인. 기존 관리 창 내부 구성은 유지되며 V6 전 화면 이식 완료 아님. 육안·해상도별 화면 비교 미수행.

- **HUD-V6-GEMS / Codex / 완료:** V6 다이아·골드·실버·레드 각진 보석 카드와 등급 외형 비교. 다이아 도감/보유 특성 공통 적용, 미정 등급 임의 배정 없음. 브라우저 검사 및 1280×720 육안 확인.

- **HUD-V6-1022 / Codex / HTML 검증 완료:** 디자인 색상·수리 카드·중복 상세·포커스 개선. 조사/평가·특성 4칸/다이아 18종·진형 14종 UI 반영. 기존 V6 검사 및 인재 관리 검사 통과(960/1280/1440 폭). 실제 효과·확률·게임 시뮬레이션 미연동. 검수: design/hud/HUD_V6_DESIGN_REVIEW.md. V5·Unity 미수정.

- **HUD-V6 / Codex / HTML 1차 시안·대표 검사 통과:** design/hud/hud-v6*. V5 보존, 하단 52px·선택 카드·상세 창·접이식 미니맵. 최신 자원 목록·생산·장비/도구·수리·매장지/배관·음주 관리 화면 추가. 960/1280/1440 폭·대표 상호작용 검사 통과. 수동 예시 진행이며 실제 재고/AI 미연동; 전체 제조법·재활용 화면 후속. 범위: design/hud/HUD_V6.md. Unity 미수정.

- **TERRACE-1020 추가 검증 / Claude(2026-10-22) / 버그 1건 수정, 1건 사용자 결정 대기:** `TerraceHeights.cs` 수정: Reach 출발칸을 본거지 평지의 안쪽 칸으로(가장자리 칸은 1칸만 닿아 반복 1회 낭비), FillUnreached를 덩어리 단위로 바꿔 같은 높이 땅에 폭 1칸 띠로만 붙은 넓은 땅도 메움(옛 코드는 같은 높이로 메워 무변화인데 true를 돌려 반복 한도 4회에 걸림). 검사 추가(`TerraceTerrainChecks`: Single 크기 인자, SaveLoad, WalkUp). 통과: 작은 맵 6바이옴×2시드·SaveLoad 3·WalkUp 3·보통 숲/사막. **미해결:** 보통 물가 2/300·큰 맵 59/12/19 지점 PathPartial — 지형은 이어져 있고(구간별 경로 Complete) Unity CalculatePath 탐색 노드 한도 초과(절벽 막다른 곳). tileSize 256→1024 효과 없음. 게임은 Partial을 '못 감'으로 처리(WorkerAnt.CanReach·운반 중 짐 내려놓기·건물 배치). 시각: 절벽면 톱니 모양, 층 경계 높이색(풀) 톱니 삼각형.
- **TERRACE-1020 / Claude 검증 완료(2026-10-22):** 컴파일 에러 0, `TerraceTerrainChecks.Single` 6맵(물가 261019·11, 숲 77·1234, 사막 4242·9001) 맵별 실행 전부 PASS(경사로·웅덩이 NavMesh 제외·건설 불가·도달 300/300), 콘솔 에러 0. 코드 추가 수정 없음. 맵별 약 20초라 `Main`(6맵 한 번에) 대신 `Single` 사용.
- **TERRACE-1020 / Codex / 수정 완료·실행 검증 미완료(위 Claude 검증으로 해소):** `MapGenerator.cs`, `HomeMapBuilder.cs`, `AgentScripts/TerraceTerrainChecks.cs`. 실행 시 지형 하나만 선택하고 사본 전체 비활성화, NavMesh 물 소스는 Active 사용. 검사 우회 비활성화·재베이크 제거, 장애물 없는 평지의 NavMesh 누락 실패 처리, 3바이옴 6시드로 확대. 컴파일 후 실행했으나 run_script 내부 30초 제한 초과, 이어 Unity 네이티브 크래시(ProfilerFrameDataDeserializer 스택) 발생. 재실행 시 `--timeout_ms 180000 --timeout 240` 사용 필요. 씬 원본 보존, 강제 종료·SAVE 안 함. graphify update는 기존 `.graphify_labels.json.sig` 오류로 실패.

- **TOOL-GRIP-1018 후속 / Codex:** 도구 잡는 방향을 타격 프레임 기준으로 보정, kemono 미리보기 29장면 저장. 장면 활성화 후 포즈 평가로 보정하며 재생성 스크립트에도 연결. 창 공격 전방 유지 시각 확인. 대표 4장면 PNG·개별 GIF 갱신 완료. 손가락 쥐기·시설 표면 접촉·준비 자세 머리 관통은 완전 해결 아님. Notion GIF는 이번 변경 미반영.

- **TOOL-GRIP-1018 / Codex / 모델 수정·Blender 확인 완료:** `art/resident-kit/scripts/tool_shapes.py`, `repair_tool_assets.py`, `kit_tools.py`, `resident-tools.blend`, 도구 FBX 4개. 창 잡는 원점 정렬, 도끼·망치·곡괭이 머리를 자루 끝에 재제작. 기존 메시 이름 유지하여 kemono 미리보기 링크 재열기 확인. 창 5프레임 소켓→표면 거리 1.34→0.0238(자루 반경), 형태 앞·사선 확인. 기존 라이브러리 before-grip-fix 백업 보존. GIF·Notion 이미지 재렌더 미실행, 손가락 쥐기·작업 대상 정밀 접촉은 별도. Unity 미수정.

- **CATALOG-KEMONO-1018 / Codex / Notion 후속 반영 완료:** Claude가 갱신한 시설 상세 23개 분류의 저녁 수정본 안내·GIF 확인. 캐릭터 종합 페이지 2곳에 최신 묶음 GIF 16종(동작 4 + 시설·조합 12)을 반영하고 동작 재작성·겹침 보정·검증 한계 설명 갱신. 5MiB 초과 GIF 6개는 원본 보존, `art/kemono-kit/previews/notion/upload/`에 색상 수를 줄인 업로드 사본 생성. 도감 소개 79→141종 정정. 재조회: 두 상세 페이지 이미지 26/6개, 도감 하위 페이지 13개 보존. Unity·Blender 모델·리그는 이번 작업에서 수정하지 않음.

- **KEMONO-MODULAR / Codex → Claude 인수(2026-10-18 사용자 지시) / 리그 정리·동작·시설 상호작용 미리보기 완료, Unity 미연결:** 리그 98→58뼈(튀어나온 뼈 제거·치마 가중치 다리 이관·귀/꼬리 뿌리 메시 기준·소켓 재배치), 걷기 보폭·무릎 수정, 상호작용 188장면(`scripts/preview.py`). 이하 Codex 기록: `art/kemono-kit/**`. 미간 보정 원본 얼굴 유지(정점 오차 약 3e-8), 천옷 LINEN 한 종류. 머리 6·귀 5·꼬리 3·색상 6·표정 17·겹침 효과 3·상처 24. 60개 선택지/103개 독립 메시, 조합 예시 6개와 분야별 보드 9장. 표정 찌그러짐·꼬리 끝 보정, 기본 캐릭터가 보이는 blend 재열기 확인. 최신 사용자 정정대로 Unity 생성 기능·모델 적용 안 함. 임시 Editor 연결 복구 파일 제거. 초기 FBX·동작은 실험본이며 최종 표정 보정과 불일치하므로 적용용 아님(README 명시).

- **KEMONO-CON-FACE / Codex / 정적 시안·사용자 검토 대기:** `art/characters/kemono-con-face/**`. V2를 복사해 눈 비율·볼·턱을 조정하고 작은 미소·홍조선을 추가. 머리·의상 보존. 확대 렌더에서 눈 변형과 입 위치를 보정, 정면·사선·확대 PNG 및 blend 저장. 원본 기반 얼굴의 인상이 남으며 리그·Unity 미적용.

- **KEMONO-FACE-02 / Codex / 외형 수정·사용자 검토 대기:** `art/characters/kemono-kiki-v2/**`. Kiki 파생 얼굴의 눈 간격·눈매·볼·턱·입과 앞머리 조정, 청록색 홍채 제작. 정면·사선 렌더 확인 및 별도 blend 저장. 원본 메시 기반이며 독자 제작 얼굴은 아님. 기존 v1 및 Unity 에셋 보존, 리그·애니메이션 미적용.

- **KEMONO-KIKI / Codex / 정적 시안 완료·사용자 검토 대기:** `art/characters/kemono-kiki-v1/rabbit-settler.blend`·정면/사선 PNG. 사용자 제공 `Assets/Kiki/Xmas Girl` 얼굴·머리·몸 메시/텍스처를 복사해 몸 비율 조정·토끼귀·의상 변경. 독자 제작 얼굴이 아님. 원본 에셋·Unity 씬 보존. 정적 메시로 원본 리그/표정은 시안에 이관하지 않음, Unity 미검증.

- **KEMONO-03 / Codex / 1차 외형·사용자 검토 대기:** `art/characters/kemono-rabbit-v1/rabbit03.blend`, 얼굴 텍스처·정면/사선 PNG. 흰 토끼 수인 03번을 새로 모델링, 턱·앞머리 깊이 보완. 기존 캐릭터·Unity 보존. 리깅·애니메이션·게임 카메라 검증 미실행.

- **RESIDENT-SIMPLE-01 / Codex / 새 외형 20종 제작·사용자 검토 대기:** `art/characters/simple-resident-v1/variants-20.blend`·비교 PNG·목록 JSON. 단순한 몸 형태·비율·귀를 달리한 독립 모델 20개, 비교 렌더 확인. 최초 기본형만 5본·24프레임 걷기/12프레임 렌더, 20종 전체 리깅·Unity 연결은 미실행. 기존 키트·Unity 보존.

- **DOC-LANGUAGE-1018 / Codex / 검토·문장 교정 완료:** Notion 현행 기획 10개 페이지의 조사·중복 표현·비행 장비 용어를 교정하고 재조회 확인. 주민 키트와 기획 사이의 부위 상실 표현, 외형/건강 상태 분리, 장식/장비 구분 및 나이·크기 미정 사항을 확인. 로컬 수정: `README.md`, `docs/CHARACTER_TRANSITION_SPEC.md`, `docs/CLAUDE_RESIDENT_ANIMATION_HANDOFF.md`, `docs/COLONY_INTERACTION_ANIMATION_REQUEST.md`, `docs/AUTONOMOUS_DUTY_API.md`, `art/resident-kit/README.md`(사망 표정 안내 정정). 코드·모델·게임 규칙은 수정하지 않음. 커밋·push 미실행.

- **RESIDENT-KIT-REVIEW / Codex / 검수·보완 완료, 접촉 일부 남음:** 사용자 확인으로 Claude 작업 종료. `art/resident-kit` 원본 표정 4종·사망 연결 복구, 표면 깊이·동작 카메라·물뿌리개 손잡이/형상 수정. 부품 94/동작 44/시설 141 FBX 검사 오류 0. 관통 표본 표시 3(화분 22~23정점, 냉장고, 샤워); 완전 접촉 검증 아님. 원본 체형 보존, Unity 미수정. 최신 자료는 blend 및 README, 기존 GIF 미갱신.

- **FURNITURE-97 / Claude / Blender 완료·Unity 미연결(2026-10-18 사용자 요청):** 건설 가능 97종(`BuildingKind`) 중 모델 없던 62종을 resident-kit 규격(배율 1)으로 제작, 가구용 새 주민 동작 10개, 시설 동작 8종. 시설 141·동작 44·GIF 156(장면별 `-S` 렌더로 정지 GIF 버그 수정, Codex 검수본 기준 재렌더). 장비 착용 사진 10. Notion 도감에 분야 3개(휴게·장식·군사·마을·장비) 신설·등록. 파일: `art/resident-kit/scripts/extra_spec.py`·`models_extra.py`·`kit_facilities_extra.py`·`render_equipment.py`, `kit_extra_clips.py`·`kit_preview.py`·`render_all.py` 수정. colony-core 원본·Unity 미수정.

- **CHARACTER-CODE-01 / Claude / 구현 완료·검사 일부(2026-10-18 사용자 승인 A~D):** `docs/CHARACTER_TRANSITION_SPEC.md`를 Unity 코드에 반영. A 신체 부위(두 팔·두 다리, 감지 삭제, 저장 v18 이관) B 장비·연구 이름 C 시민·정착지 용어 D 꿀단지 개미방 이름. 모델 교체(E)는 새 모델 확정까지 보류. 적 지도자 '여왕'·게임 제목 유지. 소유: `Assets/_Project/Scripts/**`, `AgentScripts/*`.

- **RESIDENT-KIT / Claude / Blender 완료·Unity 미연결(2026-10-18 요청서 '조합형 캐릭터와 가구 작업 애니메이션 분리'):** `art/resident-kit/` — character-kit(공통 리그+소켓 4, 부품 84, 수동 조합 4) / animations(30) / tools(8+소품 6) / facility-kit(79, 배율 적용, `facility-points.json`) / preview(링크 조립 87장면, GIF 87) + FBX 전부. 검증 `validation.json`: 분리·가중치·부품/동작/시설 FBX 왕복 통과, 관통 표시 2(냉장고 꺼내기·샤워 의도). 상세 `art/resident-kit/README.md`. 원본 미수정. Unity·HUD 미수정.
- **CRITTER-KIT / Claude / Blender 완료·Unity 미연결(2026-10-18):** 목장 4종(진딧물·누에·거미·벌, 현재 Unity 캡슐) 모델·Generic 리그·동작 7~9개·FBX·GIF 31. `art/critter-kit/README.md`. FBX 수치 왕복 검사·Unity 적용 미실행.
- **RESIDENT-ANIM / Codex → Claude 인수(2026-10-18) / 원본 보존:** `art/resident-anim/**` 새 캐릭터 공통 리그·기본/작업 클립·79 시설 연출 교체본. 기존 개미 파일과 시설 원본 보존, 신규 Blender 연출에서 개미 제외. Unity 미수정.

- **RESIDENT-GENERATOR / Codex / 사용자 정정으로 중단:** 생성 기능은 요청 범위 아님. `art/characters/resident-generator/**` 초안 보존, 추가 개발·게임 적용 안 함.
- **RESIDENT-PARTS / Codex / 정적 부품 보드 완료·사용자 검토 대기:** `art/characters/parts-board/appearance-parts.blend`, 보드 PNG·카탈로그·README. 기본 부품 보드 + 울먹임·울음·긴장·사망 + 추가 표정 11종(공허함 포함), 독립 땀·눈물·홍조 및 조합 예시. 머리 6종, 흉터·긁힘·멍·반창고 각 6부위=24개 부품. 렌더 확인. 다른 체형에 부착 시 표면 조정 필요, 리깅·자동생성·Unity 연결 없음.

- **CAST-24 / Codex / 정적 모델 24명·사용자 검토 대기:** `art/characters/residents-24/residents-24.blend`, `overview.png`, `manifest.json`, `build.py`·`parts.py`·`finish.py`. 24개 번호 루트, 귀·무늬·솜털·망토·소품 분리, 전투형 눈썹·윙크·웃는 고정 표정. 전체 렌더 및 08번 옆·뒤 확인. 원화 잔털·주름·일부 귀 변형은 단순화. 기존 6종 보존. 리깅·움직이는 표정·Unity·무작위 생성기 미구현.

- **CAST-05 / Codex / 정적 모델 6종·검토 대기:** `art/characters/cast-v5/mascot-cast-v5.blend`, `lineup.png`, `build.py`. 토토 머리의 넓은 볼 부분 약 10% 축소·상부 윤곽 보강, 동일 비율로 포포·콩지·루루·피코·냠냠 추가. 개별 루트·메시·몸 재질 분리, 6종 렌더 및 옆·뒤 확인. 기존 씬 보존. 리깅·애니메이션·Unity 적용 미실행.

- **TOTO-04 / Codex / 정적 시안 저장·사용자 검토 대기:** 새 첨부 동물형 시트의 A 토토 재모델링. `art/characters/toto-v4/toto-v4.blend`, `preview.png`, `build.py`. 작은 점눈·아래로 넓은 볼·휘어진 귀·짧은 몸·가방. 정면·옆·뒤 및 렌더 확인, 기존 씬 보존. 나머지 5종·리깅·Unity 적용 미실행.

- **HUMAN-SD-02 / Codex / 정적 시안 저장·사용자 검토 대기:** `art/characters/human-sd-v2/human-sd-v2.blend`, `preview.png`, 제작 스크립트 3개. 사람형 시안의 청록 단발·크림 튜닉·짧은 망토 재제작, 표면 투영 눈·웃는 입·매끈한 볼 수정. 66메시, 정면·옆·뒤 시각 확인. 기존 Human_SD_Model 및 Claude TotoD 별도 씬 보존. 머리카락은 원화보다 두툼하고 단순한 시안 단계. 리깅·Unity 적용 미실행.

- **FACILITY-VISUAL-01 / Claude / 검증 대기(2026-10-19 사용자 승인):** 모델 없던 BuildingKind 5종(비축더미·무기 연구소·방어시설 연구소·휴게실·범위형 분사탑) Blender 제작(`art/resident-kit/scripts/extra_spec.py`·`models_extra.py` structures) → Unity 건물 상자를 시설 모델로 표시(`Buildings/FacilityVisual.cs` 신규, `BuildingBase.OnEnable` 연결, `Assets/Resources/Facilities/CC_*.fbx` 103개). FacilityVisualChecks 513·SaveRoundtrip 57·Spec1012 159 통과. DigSite(옛 확장 지점, 기획 삭제)는 모델 없음.
- **HUMAN-SD-01 / Codex / 작업 중:** 사용자 선택 사람형 SD 시안(청록 단발·크림 튜닉·테라코타 망토) 모델링. 소유 `art/characters/human-sd-v1/**`. 기존 캐릭터 시안과 Unity 자산 보존. 정적 모델·FBX·시각 검증 범위.

- **CHARACTER-MODEL-01 / Codex / 토토 정적 시안 완료:** `art/characters/toto-v1/**`에 Blender 원본·FBX·렌더·제작 스크립트. 21메시/2,914정점/5,700삼각형. 앞·옆·뒤 시각 확인, FBX 왕복 메시·삼각형·크기·재질 검사 통과. 기존 시설·개미 리그 보존. 리깅·애니메이션·Unity 교체 미실행, 나머지 5종 미제작. 중복 Blender는 정상 종료하고 연결된 1개 유지.

- **CHARACTER-SPEC-1017 / Codex / 기획 전환 완료(구현 대기):** 사용자 첨부 캐릭터 시트 기준 두 팔·두 다리 캐릭터로 Notion 현행 기획 13개 문서의 신체·장비·특성·시설·HUD 용어 수정. 도감 루트·7분야·14분류에 기존 개미 GIF와 새 캐릭터 목표 구분, 사진·하위 링크 보존 재조회 확인. 문서: `docs/CHARACTER_TRANSITION_SPEC.md`, `docs/COLONY_INTERACTION_ANIMATION_REQUEST.md`, README 전환 안내. Unity 코드·HUD HTML·씬·기존 Blender 자산은 이번에 교체하지 않음. ART-ANIM-01의 개미 클립 17개는 기존 제작 이력이며 새 캐릭터 호환 검증 전 자동 연결하지 않는다. 정확한 크기·종족 구분·자녀 외형은 미정.

- **ART-ANIM-01 / Claude / Blender 클립·도구·작업 지점·경첩 완료, Unity 미연결(2026-10-17):** `docs/COLONY_INTERACTION_ANIMATION_REQUEST.md` 1단계. QuirkyAnt 리그(원본 미수정)에 작업 클립 17개 + 도구 8종, FBX 왕복 검사 통과, Blender MCP로 17개 재생 확인. 79개 모델별 작업 지점(WorkPoint_0/Input/Output/Seat/Patient/CarryAnchor)·클립·도구 매핑 `art/colony-core/interaction_points.json`, 경첩 6종 개폐/조준 클립 `exports-interaction/*.fbx`, 원본 대신 별도 `colony-interaction.blend`(colony-core.blend 미수정). 미리보기 GIF `previews/interaction/`. 소유 `art/ant-anim/**`, `art/colony-core/` 안 신규 파일(`interaction_*`, `scripts/build_interaction.py`·`render_interaction.py`·`make_gifs.py`·`interaction_spec.py`). 다음: Unity 임포트·Animator 상태·`AntVisual` 작업 매핑·도구 부착·지점 연결. 상세 `art/ant-anim/README.md`.

- **ART-CORE-02 / Codex / 모델 제작·검사 완료:** 사용자 승인 1~6 전체에 46개 추가: 자연물 10·물품 14·건설 단계 3·작물 상태 4·생활 가구 9·방어 시설 6. 기존 33개 포함 총 79 FBX, 26,736 삼각형. 소유 `art/colony-core/**`. 전체 FBX 왕복 검사 통과(크기/바닥/메시 수/재질/flat shading/삼각형 수), 분야별 미리보기 6장 직접 확인. **애니메이션 클립·Unity 상호작용 연결 미구현, Unity 임포트 미검증.** 적용 문서는 사용자 지시로 계속 보류. 기존 Unity 코드·씬 미수정.

- **ART-CORE-01 / Codex / 33개 모델 제작·FBX 왕복 검사 완료:** 사용자 승인: 개미 제외, 자연 재료+작은 문명 로우폴리. 소유 `art/colony-core/**`. `colony-core.blend`, 개별 FBX 33개, `materials.blend`, `palette.json`, `manifest.json`, `previews/overview.png`, `validation.json`. 소굴·창고·농사·목장·의료·조리·퇴비·묘비·자원 + 숙소·공방·연구/행정·냉각·전력·벽/문. 전체 13,996 삼각형. FBX 재불러오기 크기·바닥·메시 수·재질·flat shading·삼각형 수 검사 통과. **사용자 추가 지시로 적용 문서는 보류. Unity 임포트·기능 연결 미실행.** 기존 코드·씬 미수정.

- **HUD-V5-REAL / Claude / 작업 중(2026-10-15 사용자 승인, 전체·권장 순서):** HUD V5 기능을 Unity 실제 게임에 구현(연구·농사·설비·장비·장수 의료/수술·동물 위생/퇴비·사망/묘비·포로/귀환·원정 수송·물류 잔여). **SPEC-1013-14(Codex) 소유 파일은 사용자 지시로 Claude가 인수**(Codex 미작업 확인, 미커밋 변경 보존 후 이어서 구현). 소유: `Assets/_Project/Scripts/**`, `Save/*`, `UI/GameMenu*.cs`, `AgentScripts/*`, README, 이 보드. HTML `design/hud/*`는 참조만. Unity Play 검사 담당: Claude.
  - **진행 상태(2026-10-16 SAVE):** 연구·장비·농사·식단/요리 운반·냉각·장수 의료/수술·동물 위생/퇴비·사망/묘비·포로 구현 및 **Spec1015Checks 318 통과**. 실제 버그 2건 수정(처치가 수술 예약 약품 사용, 장수 유해 저장 거부). 회귀 통과: Campaign 86·ResourceRule 30·Spec1011Ranch 165·Spec1012 159·Spec1013AnimalHealth 11·Clinic 20·SaveRoundtrip 57·HudV5Unity 90. InfirmaryChecks는 옛 치료 모델이라 폐기 추천(미삭제). **미착수: C 배관·신호선, J 수송 확장 — 대규모라 착수 전 사용자 확인.** 간헐 실패: Spec1015 Hygiene 배설물 수거 1회.

- **HUD-V5-GAPS / Codex / 완료(HTML 요청 범위, 2026-10-15):** 직전 점검의 HUD 누락 9항목 구현. 소유: `design/hud/hud-v5*`, README, 이 보드. 연구·농사·전력/배관/신호·장비 화면, 의료·수술·귀환·추모 연결. Unity 작업은 보류. 기존 225개 검사는 전체 기획 완성도 검증이 아니며, 기본 73·물류 58·동물 건강 8·관리 87·누락 보완 86 = 312개 브라우저 검사 통과. 새 화면 1440/1280/1000/720 넘침 없음. 미정 항목은 시안 예시로 구분하고 실제 Unity·전체 콘텐츠 완료를 뜻하지 않는다.

기준일: 2026-10-07 (한국 시간). 작업 경로: `E:\Git\ant`, 브랜치: `develop`.
사용자 요청: 기획 반영 여부 점검 → 구현 수정 → 두 도구가 함께 보는 곳에 구현 위치·할 일을 기록.
이 문서는 현재 작업·인계를 위한 보드다. SAVE용 개발 일지나 기획 변경 승인 문서가 아니다.

## 작업 시작 전에

1. `log.md`, 이 문서, 실제 `git diff`를 확인한다. 보드보다 파일이 먼저 바뀔 수 있다.
2. 작업 행의 담당·상태·수정 파일을 먼저 갱신한다. **미배정은 자동 착수 지시가 아니다.** 승인된 수정과 후속 제안을 구분한다.
3. 같은 파일은 한 사람만 수정한다. 공유 파일이 겹치면 앞 작업 종료 후 이어받는다. 다른 사람이 작성한 변경을 되돌리지 않는다.
4. 상태는 `대기 / 결정 대기 / 작업 중 / 검증 대기 / 완료 / 막힘`으로 관리한다. 완료는 검증 결과가 있어야 한다.
5. Unity Play 모드·새 게임·저장 왕복 검사는 한 사람씩 실행한다. `SaveStorage.RootOverride`와 에디터 상태가 공유되므로 병렬 실행 금지. Graphify 갱신도 순차 실행한다.
6. SAVE 요청 전에는 개발 일지·캡처·커밋·push를 하지 않는다. `.prefab`·`.prefab.meta` 커밋 금지.
7. 이 파일은 같은 체크아웃에서 바로 공유된다. 다른 체크아웃/PC에는 자동 전파되지 않는다. 보드 수정 자체가 다른 도구에 메시지를 보내거나 작업을 실행하지는 않는다.

## 기획 기준

- [최신 우선 문서: 메인 게임 화면 구체화](https://app.notion.com/p/3e7c4a0ecd3181e38211f6be9c44637d)
- [기획 루트](https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd)
- [자원](https://app.notion.com/p/3d0c4a0ecd31812d80fbeceabb70193e) · [건물·방어](https://app.notion.com/p/3d0c4a0ecd3181c28b59e26337c15aba) · [연구](https://app.notion.com/p/3e5c4a0ecd3181aa8714fcb45a675818)
- 최신 우선 문서 안에도 오래된 결정이 남아 있다. 날짜·폐기/대체 명시를 확인하고 가장 최근 확정안을 적용한다. 모순이 해소되지 않으면 질문한다.
- `docs/IMPLEMENTATION_PLAN_2026-09-25.md`는 과거 지시서다. 4시대·유지비·비행선 승리 등 구내용을 현재 코드에 다시 적용하지 않는다.
- 아래 발견 사항은 **정적 코드 점검 결과**다. 전체 플레이·회귀·Player 빌드 검증 결과가 아니다.

## 현재 작업 / 충돌 방지

- **HUD-V5-1013 / Codex / 완료(HTML UI 범위, 2026-10-15):** 사용자 지시로 Unity 작업을 보류하고 HUD HTML 우선. 장수 의료·동물 위생/퇴비·묘비/추모·원정/장비/포로 관리 화면과 시안 조작 구현. 소유: `design/hud/hud-v5*`, README, 이 보드. Unity 기존 변경은 보존하며 이번 작업에서 수정·실행하지 않는다. 새 기능은 HTML 예시 상태이며 Unity 구현 완료를 뜻하지 않는다. 장수 의료·위생/퇴비·묘비/추모·포로·수송/장비/지역 화면과 명령 연결, 취소/대기/실패 상태를 추가했다. 사용자 제공 emil-design-eng·review-animations 및 DESIGN.md 적용. 브라우저 72+58+8+87=225개 통과, 새 7개 화면 1440/1280/1000/720 폭 본문 넘침 없음. 상세 `design/hud/hud-v5-design-review.md`. 미정 밸런스·실제 시뮬레이션·Unity 연결은 범위 밖.

- **SPEC-1013-14 / Codex / 작업 중(2026-10-14 사용자 전체 구현 승인):** V5 HTML 및 Unity 실제 구현에 의료·동물 질병/위생·사망/묘비·원정 장비/수송·포로 관리 반영. 먼저 동물 수술 누적 벌점 제거와 의료/질병 상태 연결, 이후 저장·UI·원정 연계. 소유: `World/Critter*.cs`, `Units/CommanderPersonalState.cs` 및 의료 신규 모듈, 관련 `UI/GameMenu*.cs`, `Save/*`, `design/hud/hud-v5*.js`, 신규 검사. 기존 타 작업 변경 보존. Pipeline 연결 복구 후 동물 의료 11·목장 165 검사 통과. 2026-10-15 사용자 지시로 Unity 후속 작업은 대기, HUD 우선 반영은 위 HUD-V5-1013 행 참고. 전체 완료 전에는 SPEC-1013을 완료로 표시하지 않는다.

- **HUD-V5-1012 / Codex / 완료(HTML 브라우저 검사 72+58 통과):** 사용자 승인: V5 HTML 시안만 최신 창고·식품·건설·우선순위·일괄 작업 기획 반영. 소유: `design/hud/hud-v5.html`, `hud-v5.js`, `hud-v5.css`, `hud-v5-logistics.js`, `hud-v5-logistics.check.cjs`, `hud-v5.check.cjs`, README. 창고·냉각·식단·배송·일괄 작업·퇴비 UI와 우선순위 1~9 반영. 1440/1280/1000/720 폭 검사 통과. 물류 진행은 수동 예시이며 Unity 연동·실제 게임 시뮬레이션은 범위 밖. 디스크 부족 후 재개하여 초기 오류 복구. Claude의 SPEC-1012 Unity 파일은 수정하지 않음.

- **HUD-V5-PARITY / Codex / 완료(확인된 UI 누락 연결, 검사 90개 통과):** 개체 개요·의료 탭/치료 상태·약제대 연결, 생산 방식 직접 선택·수량 입력(실제 한도 검증), 건설 재료 목록 비교, 동물 전용 9칸 명령창을 실제 데이터에 연결. 수정: `UI/GameMenuRanch.cs`, `GameMenuV5.cs`, `GameMenuProcessor.cs`, `GameMenuWorkshop.cs`, `BuildScreen.cs`, `CommandCard.cs`, `CommandCardAnimal.cs`(신규), `AgentScripts/HudV5UnityChecks.cs`, README. Unity 컴파일 오류 0, 임시 저장 경로에서 UI 검사 90개 통과(명령창 전환·포획/취소·운반/치료 제한·방생 확인·탭 전환·약제대 연결·수량/한도/생산 방식·재료 선택). Play 종료, 씬 저장 안 함. HTML·게임 규칙은 변경하지 않음. 픽셀 단위 시각 일치·전체 게임 회귀는 미검증.

- **HUD-V5-UNITY / Codex / 완료(대상 화면 UI 검사 67개 통과):** 사용자 승인에 따라 V5 기준 Unity 관리 화면 이식. 소유: `UI/GameMenuV5.cs`, `GameMenuRanch.cs`, `GameMenuRanchBulk.cs`, `GameMenuProcessor.cs`, `GameMenuWorkshop.cs`, `GameMenuRenovate.cs`, `GameMenuDuty.cs`, `BuildScreen.cs`, `UI/CommanderOverhead.cs`, `World/ResourceNodeStatus.cs`, 관련 검사·README. HTML V5는 참조용으로 유지. 건설 창·재료 비교, 작업표 좌/우 클릭 0~5, 목장·개체/의료·먹이통·도축대·생산·개보수 영역 분리 적용. `AgentScripts/HudV5UnityChecks.cs`에서 실제 UI 연결 67개 통과, Unity 컴파일 오류 0. 기존 저장과 분리된 임시 게임으로 검사한 뒤 Play 종료. 픽셀 단위 시각 일치·전체 게임 회귀는 미검증. Graphify 갱신 완료. SAVE·커밋·캡처 없음.

- **2026-10-07 포텐·재분배 기획 반영 완료(Notion·README·보드, 구현 대기):** 전체 능력 총한도(PA 최대 200), 반복 작업 성장과 미사용 능력 감소를 사용자 확정으로 반영한다. FM 근거 조사 후 Notion 관련 기획·README의 구현 대기 안내·이 보드만 수정한다. 게임 코드는 수정하지 않는다. 연구 산출물: `research_notes/개미 포텐과 능력 재분배/`, `reports/개미 포텐과 능력 재분배.md`.

- **2026-10-07 특성·포텐 기획(구현 대기):** 특성은 빨강(부정)·실버·골드·다이아 4등급, 장수당 합계 최대 4개, 플레이 중 획득 가능. 등급별 목록·시작 개수·확률·획득 조건·만석 처리는 미정. 사용자 확정: 포텐 최대 200은 장수 전체 능력의 한도. 계속 시키는 실제 작업 비중에 따라 관련 능력들이 함께 성장·유지되고 계속 덜 쓰는 능력은 감소·재분배된다. **경과일 기반 감소안은 폐기**, 휴식·수면·대기·이동만으로 감소하지 않는다. 적용 설계: 기존 13종 기본 기술(각 0~20)의 합계 CA ≤ 개체 고정 PA ≤ 200. 특성·장비·기분의 실효 보정은 제외하고 시작 기본 기술 보정은 포함한다. 한도 후에는 감소분만큼만 관련 능력이 성장하며 중복 차감하지 않는다. 13종 전체 작업별 주/보조 관계를 명시하고 한 작업의 성장 예산을 분배한다(주80%/보조20% 잠정, 중복 지급 금지). 최근 작업 이력 범위·유지 비중·작업량당 변동률·CA/PA 공개 UI는 조정 대상. 게임 코드는 수정하지 않았다.

- **2026-10-07 농사 기획 확정(구현 대기):** 장수가 작업표 우선순위에 따라 파종·수확하고 작물은 환경 조건에 따라 스스로 성장한다. 성장 중 장수는 다른 일을 하며 일반개미도 다른 작업에 투입 가능. 일반개미는 파종·수확 속도만 보조한다. 밭별 지원 수를 기억하고 파종·수확 때만 투입하는 방식으로 잠정 결정. 설정 수는 변경 가능하며 부족 인력 배분 규칙·세부 UI는 미정. 연구는 사용자가 항목·장수·지원 수를 정해 지시하며 자동 반복하지 않는다. 이번에는 Notion 기획만 갱신했고 게임 코드·FIX-01은 변경하지 않았다.

- **2026-10-07 농사 기획 확정(구현 대기):** 장수가 작업표 우선순위에 따라 파종·수확하고 작물은 환경 조건에 따라 스스로 성장한다. 성장 중 장수는 다른 일을 하며 일반개미도 다른 작업에 투입 가능. 일반개미는 파종·수확 속도만 보조한다. 밭별 지원 수를 기억하고 파종·수확 때만 투입하는 방식으로 잠정 결정. 설정 수는 변경 가능하며 부족 인력 배분 규칙·세부 UI는 미정. 연구는 사용자가 항목·장수·지원 수를 정해 지시하며 자동 반복하지 않는다. 이번에는 Notion 기획만 갱신했고 게임 코드·FIX-01은 변경하지 않았다.

| ID | 우선순위 | 할 일 | 담당 | 상태 | 수정 소유 파일 |
|---|---|---|---|---|---|
| FIX-01 | 높음 | 휴식 명령을 숙소로 연결 | Claude(2026-10-07 사용자 지시로 Codex에서 인수) | 완료(DayNightChecks 42개 2회 통과) | `Assets/_Project/Scripts/Units/CommanderDuty.cs`, `AgentScripts/DayNightChecks.cs` |
| FIX-02 | 높음 | 성체 전체 기준 노화와 인력 수량 일치 | Claude | 완료(Phase4 42·Workforce 80 통과) | `Core/ColonyPopulation.cs`, `Core/AntPool.cs`, `Save/SaveValidator.cs`, `AgentScripts/Phase4Checks.cs` |
| FIX-03 | 높음 | 가시·끈끈이 함정 구분 | Claude | 완료(Stage2 185·FurnitureCatalog 40·BuildCategory 62 통과) | `Buildings/TrapPit.cs`, `Data/BuildingData.cs`·`FurnitureCatalog.cs`, `Buildings/RuntimeBuildingTemplates.cs`·`BuildingPlacementController.cs`, `Core/ScienceEffects.cs`·`GameBalance.cs`, `UI/BuildScreen.cs`·`GameMenuScienceBuildings.cs`, `Save/SaveDtos.cs`·`SaveBuildings.cs`·`SaveCatalog.cs`, `AgentScripts/Stage2Checks.cs` |
| DOC-01 | 높음 | 최신 확정 기획 충돌 정리·문법 및 표기 교정 | Codex(문구 교정 채팅) | 완료(실행 화면 미검증) | 문구 교정·README·Notion 참조 정리, 독립 컴파일 통과. 영어 허용, 일괄 번역 중단. FIX-01 파일·Unity 실행 상태는 다른 Codex 담당이므로 관여하지 않음 |
| VERIFY-01 | 높음 | 수정 회귀 및 실제 성장 과정 점검 | 미배정 | 대기 | 검사 스크립트; Unity 실행은 단독 사용 |
| SPEC-A | 높음 | 2026-10-08 기획: 일반개미 평시 작업 지원 폐기 | Claude | 완료(검사 통과, log.md 참고) | `Units/Workforce.cs`·`CommanderWork.cs`, `Buildings/BuildingConstructionSite.cs`·`BuildingPlacementController.cs`, `UI/WorkTargetPanel.cs`·`SelectedUnitPanel.cs`, `Save/*`, 관련 검사 |
| SPEC-B | 높음 | 세금 주간 현물·방침 3종·1% / 식량 부족 시민 소멸 폐기 | Claude | 완료(검사 통과, log.md 참고) | `Core/ColonyPopulation.cs`·`UpkeepManager.cs`, 인구 UI |
| SPEC-C | 중간 | 연속 배치(좌클릭 1개 / Shift 유지 / Shift+드래그) | Claude | 완료(검사 통과, log.md 참고) | `Buildings/BuildingPlacementController.cs` |
| SPEC-D | 중간 | 농사 파종→자체 성장→수확 | Claude | 완료(검사 통과, log.md 참고) | `Buildings/FarmPlot.cs`, 장수 작업 선택 |
| SPEC-E | 중간 | 연구 항목·장수 지정, 자동 반복 없음 | Claude | 완료(검사 통과, log.md 참고) | `Core/CampaignResearch.cs`, `Buildings/ScienceLab.cs` |
| SPEC-F | 높음 | 정치 기술·작업표 행정·행정 책상·행정 성과(징수·민심·이주·재개발 보정) | Claude | 완료(Spec1008FIChecks, 수치 잠정) | `Units/CommanderTalents.cs`·`CommanderTraits.cs`·`CommanderDuty.cs`·`CommanderWork.cs`, `Buildings/AdminDesk.cs`, `Core/ColonyPopulation.Admin.cs`, `UI/GameMenuDuty.cs` |
| SPEC-G | 높음 | PA/CA 포텐·실제 작업 비중 성장·감소·재분배(주80/보조20) | Claude | 완료(Spec1008FIChecks, 수치 잠정) | `Units/CommanderTalents.cs`, `Units/CommanderAnt.cs`(GainExperience), `UI/GameMenuRoster.cs` |
| SPEC-H | 중간 | 특성 최대 4개(등급 배정·확률·만석 처리는 미정이라 보류) | Claude | 부분 완료 | `Units/CommanderTraits.cs` |
| SPEC-I | 중간 | 주거 재개발(기존 집 위 배치, 보상비·불만·지연) | Claude | 완료(견적 검사, 수치 잠정) | `Buildings/Redevelopment.cs`·`BuildingPlacementController.cs`·`BuildingConstructionSite.cs`, `UI/BuildScreen.cs`·`GameMenuPopulation.cs` |
| FIX-04 | 높음 | Codex 점검 반영(10-09): 정지 시 공사 삭제 → 보존, 공동 건설, 예정지 취소 100% 환급, 작업표 5단계 우선순위, PA 한도 주·보조 비례 성장, 최고 세율 이탈 | Claude | 완료(Spec1009Checks 64) | `Units/WorkerAnt.cs`·`CommanderDuty.cs`·`CommanderTalents.cs`, `Buildings/BuildingConstructionSite.cs`·`BuildingPlacementController.cs`, `Core/ColonyPopulation.cs`·`GameBalance.cs`, `UI/GameMenuDuty.cs`·`WorkTargetPanel.cs` |
| SPEC-J | 높음 | 10-08/09 외교: 담당 장수·반응·역제안·인내·결렬/재개·보류 알림·협상 중 정지·초과분 바닥 보관·포로 몸값/교환(제3자 금지)·포획 부상/장비 압수·거리 비례 귀환·무효 처리·최후통첩·외교/위협 신뢰·평판 | Claude | 완료(Spec1009Checks, 수치 잠정) | `World/DiplomacyNegotiation.cs`·`DiplomacyPrisoners.cs`·`DiplomacyTrade.cs`·`DiplomacyManager.cs`·`DiplomacyRules.cs`·`SettlementDefense.cs`, `UI/GameMenuNegotiation.cs`·`GameMenuDiplomacy.cs`·`ToastManager.cs` |
| SPEC-K | 높음 | 10-11 목장 기획 불일치 수정(2026-10-11 사용자 승인): 자동 도축·도축 금지 제거 → 수동 도축 요청(확인·일괄·취소, 시작 후 취소 불가, 강제 중단 부분 피해), 치료 시설 안 도축·방생 금지, 도축대 사체별 산출·등록 순서·허용 종·일시정지, 약제대 = 간호(의료) 작업·의료 능력만, 의약품·키트 예약, 치료 우선, 수술 성공률·실패·후유증·회복·수동 재수술·기여도 가중·재입원, 투약은 투약 당시 경상만, 개체 표시명 = 종 이름. 미정 수치는 사용자 선택(수술 잠정안 A, 고위험 50% 미만, 회복 최대 1개월, 새끼 산출 50%, 중단 피해 10%, 허용 해제 사체 바닥, 간호 = 의료 작업) | Claude | 검증 대기(Codex 2차 리뷰) — 1차 리뷰 반영(10-11: 도축대 사체별 남은 부패 시간 보존·진행, 회복 중 시설 안 새 경상 자연 회복·투약, V5 실패 후 복귀 부상 유지·투약 중복 방지) 후 Spec1011Ranch 151 · 2차(10-11 사용자 지시 '구현되는 부분 구현'): 포획·운반·도축·방생 일괄 요청/취소(제외 사유·두 번 확인), 여러 종 운반 공통 목적지, 개체 이름 변경(이름(종)), 목록 필터·정렬, 운반은 결박·쓰러짐만(길들인 개체도 저항 없이 포획) → Spec1011Ranch 165 · Spec1010Material 44 · Spec1010 53 · AutonomousDuty 56 · Spec1008FI 29 통과, InfirmaryChecks는 기존 문제(`Treat at` 버튼 f0f0fa0에서 제거)로 실패 | `World/Critter.cs`·`CritterSurgery.cs`(신규)·`CritterSlaughter.cs`(신규)·`CritterCarcass.cs`, `Buildings/Ranch.cs`·`RanchFacility.cs`·`RanchButcher.cs`(신규)·`Processor.cs`·`Demolition.cs`, `Units/CommanderHusbandry.cs`·`CommanderDuty.cs`·`CommanderWork.cs`·`CommanderTalents.cs`·`AnimalMedicalStock.cs`(신규), `Core/GameBalance.cs`, `UI/GameMenuRanch.cs`·`GameMenuRanchBulk.cs`(신규)·`GameMenuDuty.cs`·`GameMenuProcessor.cs`·`WorkTargetPanel.cs`·`CommandCard.cs`, `AgentScripts/Spec1011RanchChecks.cs` |
| HUD-V5 | 중간 | HUD HTML V5 인터랙티브 시안(V4 보존). 디자인 기준: impeccable(Operate·craft floor)·emil-design-eng·DESIGN.md 토큰 | Claude | 검증 대기 — Playwright 72개 통과(1차 리뷰 재현 3개 포함)(`node design/hud/hud-v5.check.cjs`, 콘솔 오류 0, 1440/1280/1000/720 넘침 없음) | `design/hud/hud-v5.html`·`hud-v5.css`·`hud-v5.js`·`hud-v5.check.cjs`(신규), 스크린샷 `reports/hud-v5/` |
| FIX-05 | 높음 | 리뷰 반영(10-10): 포로 있는 수용소 철거 금지, 적 포로 대칭 귀환, 같은 단계 작업은 대상 우선순위·거리로 비교, 정지 중 생산 목록 예약 금지, 이동은 방 가구·장식만, 철거·이동 예정지 접근 판정(가구 중심 → 주변 접근 지점) | Claude | 완료(Spec1010 53·WorkAlert 30·Spec1009 75·AutonomousDuty 56·SaveRoundtrip 57) | `Buildings/Demolition.cs`·`Workshop.cs`·`BuildingPlacementController.cs`, `World/DiplomacyPrisoners.cs`·`DiplomacyTrade.cs`, `Units/CommanderDuty.cs`·`CommanderCorpseWork.cs`·`WorkPriorities.cs`, `UI/CommandCard.cs` |
| SPEC-1012 | 높음 | 2026-10-12 기획(사용자 승인, 창고는 산소미포함식 실물 재고): ①작업표 0~9·실제 이동 거리·오래 기다린 요청 ②창고별 재고·허용 품목·우선순위·꺼내기/비우기·운반 규칙 ③건설 예약·배송·시공 시작 소비 ④철거·이동·개보수 ⑤식품 신선도·냉장고·냉동고 ⑥요리·생산 ⑦작업 지시 UI | Claude | 완료(10-13) — ①~⑦ 구현·Spec1012Checks 153 통과. 10-13: ⑤ 바닥·운반·꺼내기 신선도 유지·바닥 식품 저장·썩은 식품 식사(기분 −12·식중독 25% 잠정)·냉장고/냉동고 건설 템플릿·저장 종류 누락 수정 / ⑥ 요리 생산 목록(지정 횟수·재고 유지·계속, 레시피 등급, 시작 시 재료 소비, 시작 후 취소 손실·두 번 확인, 진행 인계, 완성 요리는 바닥→운반, 좋은→간단→날것→썩은 순 식사, 식탁 없이 식사 기분 −3) / ⑦ 작업 관리 창(작업 선택 드래그·전체 작업, 막힌 사유 개수, 필요/예약/도착, 일괄 우선순위·정지·재개·취소 두 번 확인, 담당 장수 위치 이동). 남은 것: 식단(공통·장수별 덮어쓰기)·레시피별 재료 허용, 요리 재료 실제 운반(원격 차감), 냉각 정도 저장, 요리사 특성 Chef 효과 재정의 | `Units/CommanderDuty.cs`·`WorkPriorities.cs`·`WorkerAnt.cs`·`CommanderMeal.cs`, `Core/ResourceManager.cs`·신규 `Core/StoreLedger.cs`, `Buildings/Storage.cs`·`Stockpile.cs`·`BuildingConstructionSite.cs`·`BuildingPlacementController.cs`·`Demolition.cs`·`Kitchen.cs`·`Processor.cs`·`Workshop.cs`, `Save/*`, `UI/GameMenuDuty.cs`·`WorkTargetPanel.cs`, 신규 검사 `AgentScripts/Spec1012Checks.cs` |
| SPEC-1013 | 높음 | 2026-10-13 Codex 대화 기획(Notion 반영 완료): 장수 부상·질병·의료 림월드식(부위별 상처·처치/투약/수술·약품 3등급·병상 휴식·구조·건강 화면), 동물 의료 완화(수술 후유증 누적 폐지)·동물 질병·전염병·배설물 위생·퇴비 원료, 장수 사망·묘비·추모, 원정 사망/포로 장비 압수·반환, 수송수단 패배·철수·수리·남겨진 아군·다지역 시간·출정 준비·이동·하역 | 미정 | 구현 대기(기존 코드의 동물 수술 후유증 누적은 기획과 불일치) | — |

### 현재 인계 상태

- 2026-10-07 DOC-01 재개: 사용자가 영어를 허용하고 문법·글자 교정이 목적임을 정정했다. 아래 이전의 '한국어로 통일' 지시는 대체한다. FIX-01은 별도 Codex가 진행 중이며 이 채팅은 해당 코드·검사를 수정하지 않는다.

- DOC-01 교정 결과: 침공 코드의 문자열 치환 손상 복구 확인, 미정의 `AirshipYard.PartName` 호출을 기존 영어 부품명으로 복구. 연구소 등급 안내·특성별 탈주 기준·양육실 표기·엔딩 자원 기록을 교정했다. 사용하지 않는 버튼 번역 인수는 제거했다. README의 인구·세금·기분·자율 작업·34개 연구 설명을 정리했다. 이전 Notion 8개 페이지 재조회 후 메인 구체화·UI·맵·과거 현황 문서의 남은 참조를 교정했다.
- DOC-01 검증: Unity 내장 Roslyn + 기존 Bee 컴파일 옵션으로 `.unity/text-audit-validation/`에 독립 출력, exit 0(오류 0, 기존 CS0618 경고 191). 변경 소스 전부가 컴파일 대상임을 확인했고 diff 공백 검사 통과. Unity Play·화면 검사는 실행하지 않았다. Graphify AST 갱신 완료(6113 노드 / 12653 엣지). FIX-01 파일 수정·실행 제어 없음. 카카오톡 MemoChat 도구 미연결로 완료 알림 전송 불가. SAVE 전이므로 로그·개발 일지·캡처·커밋·push는 미실행.

- 2026-10-07 사용자 요청으로 DOC-01 중단, 로그 기록 후 컴퓨터 종료 예정. 코드·Notion 모두 부분 수정 상태이며 검증 완료로 취급하지 않는다. 상세 완료 범위·잔여 순서는 `log.md` 최상단 참고.

- 2026-10-07 사용자 승인: 기획·참조 문구를 최신 확정안으로 교체하고 게임 표시 문구의 오탈자·띄어쓰기·남은 영어를 한국어로 통일한다. 게임 규칙 변경·미구현 기능 개발은 제외. DOC-01은 Codex가 담당하며 FIX-01 코드 두 파일은 수정하지 않는다.

- **2026-10-07 SAVE 재개 결과:** 공식 Unity MCP로 연결했고 컴파일 상태 `up_to_date`, `failed=false`, 오류 없음. `DayNightChecks.Main` 실행은 65행 `rest destination is dormitory, not recreation room`에서 실패했다. 아래 연결 불가·미검증 기록은 이전 시점 기록이며, 현재는 실행 검증 실패/원인 미분류 상태다. 두 코드 파일은 미커밋 보존한다. Unity Play는 종료했으며 검사 담당은 없음.
- SAVE 문서 담당: Codex. `log.md`, `changelog.md`, `README.md`, 이 보드와 기존 공동 작업 규칙을 저장한다. 미해결 노화/함정의 새 구현은 착수하지 않았다.

- Codex가 FIX-01의 `SendToRest()`에서 `RestRoom` 검색을 `Dormitory.Assign(this)`로 교체했다. 숙소 없음/접근 불가 시 기존 제자리 휴식 경로를 유지한다.
- `DayNightChecks`에 숙소 배정·휴식 이동 목적지·숙소 없는 경우 검사를 추가했다. **컴파일·실행 미검증**, 통과로 취급하지 않는다.
- 공식 CLI는 `unity pipeline list`에서 프로젝트 에디터 PID 19552를 발견했지만 서버 포트가 없고 연결 불가다. 샌드박스 밖에서도 같은 결과였다. 에디터 프로세스는 응답 중으로 확인됐다. 재시작·패키지 변경은 하지 않았다.
- Unity 검사 실행 담당: 없음(연결 복구 전). Graphify 갱신 결과는 아래 검증 기록 참고.
- 사용자에게 아래 두 사항을 질문했으며 아직 답을 받지 않았다. **추천 선택지는 승인으로 간주하지 않는다.**
  1. 여러 파일을 바꾸는 3건 전체 수정인지, 휴식+함정 이름 연결만 우선인지.
  2. 출전·작업 중 노화는 즉시 인력 차감인지, 노화 예정 기록 후 반환 시 전환인지.
- 사용자는 이후 공동 구현 보드 작성을 요청했다. 위 질문에 대한 답으로 해석하지 않는다.

## FIX-01 — 휴식 목적지

- 기획/UI: 평시 휴식은 숙소로, 부상이 있으면 의무실로.
- 이전 구현: `CommanderDuty.SendToRest()`가 옛 휴게실(`RestRoom`)을 검색.
- 위치: `Units/CommanderDuty.cs`, `Buildings/Dormitory.cs`, `Units/CommanderSleep.cs`, `UI/CommandCard.cs`의 `RestOrTreat()`와 `UI/GameHotkeys.cs`의 직접 호출. 경로 접두사는 `Assets/_Project/Scripts/`.
- 최소 수정은 기존 숙소 배정 로직 재사용. 별도 수면/휴식 시스템을 새로 만들지 않는다.
- 완료 조건: 휴게실이 더 가까워도 배정 숙소로 이동, 숙소 없음/만석/접근 불가에서 멈춤·오류 없음, 부상 시 기존 치료 흐름 유지, 휴식 후 자율 작업 복귀.
- 검사: `AgentScripts/DayNightChecks.cs` + 필요한 휴식/치료 경계 사례. 현재 추가 검사는 전체 완료 조건을 모두 증명하지 않는다.

## FIX-02 — 노화와 일반개미 수량

- 기획: 매달 성체 3% → 늙은 개미. 현재 `ColonyPopulation.Monthly()`는 `pool.Free`만 계산한다.
- `AntPool.Total = Free + Assigned + Reserved + Working`. 분모만 `Total`로 바꾸면 대기 인력 부족 시 여전히 일부가 누락될 수 있다.
- 후보 위치:
  - `Core/ColonyPopulation.cs`, `Core/AntPool.cs`: 노화·인구/납세·동원 상한.
  - `Units/CommanderAnt.cs`, `Units/CommanderDuty.cs`: 실제 부대 병력·반환·부분 피해.
  - `Units/Workforce.cs`, `Buildings/BuildingConstructionSite.cs`: 작업 인력·건설 예약의 반환.
  - `Save/SaveSnapshot.cs`, `Save/SaveValidator.cs` 및 관련 저장 모델: 새 상태가 필요할 때만 변경.
- **선결 결정:** 동원 중인 개미가 늙는 순간 처리 방식. 임의로 전투 손실·사망·충성/기분 사건으로 처리하지 않는다.
- 완료 조건: 대기·출전·작업·건설 예약이 섞여도 승인된 노화 수만큼 한 번만 전환. 인구 보존(별도 노인 사망 제외), 음수·반환 복제 없음, 저장 왕복 일치. 원정 중 병력도 검사.
- 검사: `AgentScripts/Phase4Checks.cs`, `WorkforceChecks.cs`, `SaveRoundtripChecks.cs`의 관련 부분. 현재 Phase4의 `free0` 기반 노화 기대값도 바뀌어야 한다.

## FIX-03 — 가시/끈끈이 함정

- 확정 역할: 가시 = 반복 피해, 끈끈이 = 느려짐/속박, 폭발 = 후반 1회용.
- 현재 `FurnitureCatalog`의 가시 함정이 `BuildingKind.TrapPit`에 연결됐지만 `TrapPit.Tick()`은 피해 없이 속박 후 파손. 끈끈이는 카탈로그 이름만 있음.
- 먼저 기존 `TrapPit`을 끈끈이로 정확하게 표시한다. 기존 저장의 함정을 갑자기 피해형으로 바꾸지 않는다.
- 새 가시 기능은 범위 승인 후 구현. 피해량·반복 간격·파손/재장전 규칙은 아직 이 점검에서 확인하지 못했으므로 확정 기획을 찾거나 사용자에게 확인. 임의로 확정 수치를 만들지 않는다.
- 후보 위치: `Data/FurnitureCatalog.cs`, `Data/BuildingData.cs`, `Buildings/TrapPit.cs`, `Buildings/RuntimeBuildingTemplates.cs`, `Buildings/BuildingPlacementController.cs`, `Core/ScienceEffects.cs`, `UI/BuildScreen.cs`, `UI/GameMenuScienceBuildings.cs`.
- 저장 점검: `Save/SaveCatalog.cs`, `Save/SaveBuildings.cs`. 종류를 추가하면 enum 뒤에 붙이고 기존 번호를 유지. 같은 컴포넌트를 재사용할 경우 종류별 저장 식별이 되는지 확인.
- 완료 조건: UI 이름·카탈로그·행동 일치, 피해형/속박형 분리, 아군·공중 제외와 보스 처리, 연구 해금·건설·저장 왕복 검증.
- 검사: `AgentScripts/Stage2Checks.cs`, `FurnitureCatalogChecks.cs`, `BuildCategoryChecks.cs` + 가시 반복 피해 검사.

## DOC-01 — 기획과 현황 설명 정리

- 자원 문서: 폐기된 ‘자동 채집 없음’ 설명 제거/교체. 실제 기준은 작업표 기반 자율 채집.
- README: 일반개미 유지비·충성심·여왕방 관련 옛 설명, 연구 수와 시대, 침공 미구현 설명을 실제 코드와 재대조.
- 로켓 건조 침공은 `AirshipYard.TryBuild()` → `DiplomacyManager.AirshipConstructionStarted()` 연결이 존재한다. README의 미구현 문구만 보고 새 기능을 중복 작성하지 않는다.
- 카탈로그 항목 수·메뉴 종류 수·실제 기능 완료 수는 서로 다르다. `log.md`의 81/239 숫자로 전체 구현률을 계산하지 않는다.
- 다른 페이지에서 옛 규칙을 참조하는 곳도 함께 확인. 하위 페이지가 있는 Notion 루트를 통째로 교체하지 않는다.
- 버그 수정 과정·검증 결과는 **SAVE 때** Trouble Shooting에 기록. 지금 개발 일지를 쓰지 않는다.

## 후속 시스템 — 이번 3건 수정과 별도, 자동 착수 금지

| ID | 구현할 내용 / 현재 빈틈 | 먼저 볼 위치 | 선결 조건 · 완료 기준 |
|---|---|---|---|
| NEXT-01 | 석유·원자 시대 연구와 성장 연결 | `Core/CampaignResearch.cs`, `Buildings/ScienceLab.cs`, `Core/GameBalance.cs` | 시대별 연구 목록·승급 조건 승인. 이름만 6시대로 나누지 말고 해금/비용/생산/원정 연결 검증 |
| NEXT-02 | 배관·자동화 | `Data/FurnitureCatalog.cs`, `UI/BuildScreen.cs`, 기존 `Buildings/PowerGrid.cs` 구조 참고 | 물/석유 흐름·저장·신호 규칙 결정 후 기능별 모듈 구현. 현재 두 메뉴는 비어 있음 |
| NEXT-03 | 위생의 더러움·청소, 식량 부패/보존 | `Units/CommanderHygiene.cs`, `Buildings/HygieneFixture.cs`, `Buildings/Storage.cs`, `Units/CommanderCorpseWork.cs` | 발생·제거·부패 속도와 저장 단위 결정. 가구 없음의 결과와 저장고 효과까지 연결 |
| NEXT-04 | 전력 소비 시설·난방·조명 효과 | `Buildings/PowerNode.cs`, `Buildings/Decoration.cs`, `Units/CommanderSleep.cs` | 현재 전력 소비는 전등. 난방 반경/추위 제거, 반딧불 램프 효과의 기획 충돌 확인 후 구현 |
| NEXT-05 | 가구 재료 업그레이드·상위 방 | `Buildings/BuildingBase.cs`, `Data/FurnitureCatalog.cs`, `Buildings/RoomSystem.cs` | 재료 목록·단계·효과와 방 이름은 미정. 승인 전 임의 구현 금지 |
| NEXT-06 | 나머지 가구·마을 시설·바이옴 전용 적 | 카탈로그·건설 메뉴·바이옴/적 시스템 | 목록 등록과 기능 완성을 분리. 공공시설 효과·에셋/적 상세 확인 후 작은 묶음씩 |

## VERIFY-01 — 기능 수보다 연결 검증

- 초반 정착 → 식량/주거 → 인구 증가/세금 → 병역 → 연구 → 수송/원정 → 설계도/로켓 발사 흐름을 실제 실행한다.
- 특히 ‘한 판 10시간’은 연구량 상수만으로 증명되지 않는다. 인력 배율·다중 연구소·이주/식량 수지·시대 승급을 포함해 측정한다.
- 변경과 직접 관련된 검사부터 실행한다. 실패를 수정한 뒤 저장 왕복·핵심 회귀를 순차 실행. Player 빌드와 장시간 플레이 미실행은 그대로 표시한다.
- 실행 중 사용자 저장 슬롯을 덮어쓰지 않는다. 검사 임시 저장 경로를 쓰고 종료 시 원상 복구한다.
- 컴파일 성공, 검사 통과 수, 실제 명령/대상, 남은 제한을 아래에 적는다. 과거 log/README의 통과 기록을 이번 결과로 복사하지 않는다.

## 검증·인계 기록

| 날짜 | 담당 | 대상 | 결과 | 다음 단계 |
|---|---|---|---|---|
| 2026-10-07 | Codex | FIX-01 | 코드 1곳 변경·DayNight 회귀 검사 추가. Unity 연결 불가로 컴파일/실행 미검증 | Pipeline 연결 복구 후 검사 |
| 2026-10-07 | Claude | FIX-01 | 컴파일 복구 후 공식 MCP로 DayNightChecks 42개 2회 연속 통과(에디터 포커스 상태). 이전 65행 실패는 재현 안 됨, 원인 미확정 → 실패 메시지에 목적지·경로 진단값 추가. 치료(`RestOrTreat`)·휴식 해제(피로 회복 시) 코드는 변경 없음 | 커밋은 SAVE 때 |
| 2026-10-07 | Claude | FIX-02 | 사용자 결정: 동원 중 노화는 복귀 시 전환. 매달 성체 전체(`AntPool.Total`) 3% 중 대기 몫(대기 3%)만 즉시 늙고 나머지는 `State.agingDue`(저장됨)로 미룸 → `ReturnAssigned/ReturnWorkers/ReleaseReserved`에서 돌아온 수만큼 전환, 전사 시 남은 동원 수로 상한. Phase4 42(미룸·복귀 전환·상한 검사 추가)·Workforce 80 통과. SaveRoundtrip 57 통과(새 Play 세션에서 2회+, agingDue 왕복 검사 추가). 앞선 `barracks tier` 실패는 Phase4·Workforce 직후 같은 Play 세션에서 돌린 상태 오염으로 판단(새 세션 재현 안 됨) — 저장 왕복 검사는 매번 Play 재시작 후 실행 | - |
| 2026-10-07 | Claude | FIX-03 | 사용자 승인: 기존 TrapPit = 끈끈이 함정(표시명만 변경, 저장 호환 유지). 새 `BuildingKind.SpikeTrap`(enum 끝) = 같은 TrapPit 컴포넌트의 가시 모드: 반경 1.2 지상 적(몬스터·보스·적 장수) 1초마다 피해 6, 8회 후 파손, 수리·자동 복구·함정 연구 해금 공유(수치 잠정). 저장은 종류 이름 `SpikeTrap` + `trapSpikeHits`. 건설 방어 탭 8칸. Stage2 185(가시 5개 추가)·FurnitureCatalog 40·BuildCategory 62 통과. 가시 함정 저장 왕복(종류·피해 횟수) SaveRoundtrip에 추가해 통과 | SAVE 때 Notion 건물·방어 기획 반영 |
| 2026-10-07 | Codex | 공동 보드 | 기획 대비 불일치·파일 위치·완료 조건·결정 대기 구분 | 작업자는 착수 전에 담당을 표시 |

### SPEC-1013-14 현재 구현 경계 (Codex)
- 반영: 동물 수술 실패 횟수에 따른 성공률/사망률 벌점 제거(V5·Unity), 실패 뒤 직접 재수술 지정은 유지. 이전 저장의 sequelae는 확인 이력으로만 사용.
- 반영: 동물 질병 상태/잠복·증상/같은 우리·같은 종 전염/치료대 전염 제외/투약(식중독 제외)/경과 회복/경증 생산 감소·중증 중단, 질병 의료 표시·필터·우리별 알림. 질병 이름·수치·종별 감수성은 잠정. 개체 저장에 질병 진행/약효 포함, 손상 값 검사.
- 반영: 치료대 대기·중단 중 의료 장수가 먹이를 가져오는 작업. 먹이 예약과 기존 운반 화물 저장 사용. 실제 이동·취소·불러오기 동작은 실행 검증 대기.
- V5: 동물 의료에 질병 표시·투약·시안 발생 조작, 게임 시간 진행·일시정지. 모든 Unity 동작이 V5에 연결된 것은 아님.
- 검증: V5 기본72 + 물류58 + 의료8 통과. Unity의 기존 Bee 컴파일 응답 파일과 공식 설치 Roslyn으로 현재 소스 정적 컴파일 통과(에디터/Play 검사 아님). 신규 Spec1013AnimalHealthChecks는 작성했지만 미실행.
- 연결 복구: 기존 Unity 에디터는 실행 중이었음. sandbox 안 프로세스 조회가 누락되었고 Pipeline 포트 파일이 없어 CLI 검색 실패. 공식 Unity MCP 재연결 후 정상 응답, 신규 의료 상태11·기존 목장165 실제 실행 통과. 급식/저장 추가 검사 진행 중.
- 남음: 동물 위생·배설물·퇴비·질병 발생 경로/진행 중 임신 상세·통합 의료 우선순위·실행 회귀, 장수 의료 전면 개편, 시체/묘비, 원정 장비 압수/실물 수송/철수, 포로 실물 관리. 전체 기획 구현 완료 아님.
- 추가 담당: `Buildings/RanchFacility.cs` 먹이통 우선순위 기본5/상한9 기획 일치. 급식 실물 창고 인출과 운반 식품 신선도 보존 관련 `Units/WorkerAnt.cs`, `Save/SaveDtos.cs`, `Save/SaveSnapshot.cs`, `Save/SavePreflight.cs`, `AgentScripts/Spec1013ClinicChecks.cs`.
- SAVE 미수행: log/changelog/개발 일지/캡처/커밋/push 하지 않음.
- **RECOVERY-1025 / Codex / 진행 중(사용자 인계 요청)** — Claude·Codex G1/G2 중단 기록 확인. 기존 변경 보존. 우선 소유: Map/Precipitation.cs·SeasonVisuals.cs, UI/GameMenuCrashStart.cs·MenuTheme.cs·GameMenuController.cs, World/ResourceNodeStatus.cs; 필요 시 G1 입력·시작 인원 경로 및 F3 시설 회전 수정 통합. 기능 확장보다 실플레이 오류 우선. Unity 검증 잠금 Codex 인계, SAVE 전 로그·캡처·커밋 없음.
- **협업 소유권 정정(2026-10-25 사용자 전달): c13 원정대 UI·c22 파편 크기는 Claude 담당.** Codex는 `UI/WorldMapPanel.cs`·`World/ProductionProp.cs` 추가 수정 중단. 이미 들어간 변경: 원정대 다음 수송 버튼 y=10·빈 목록 안내, 기존 바닥 소품 최소 너비 1.2. `ProductionProp.Attach`의 ResourceModels 경로/변형 선택과 `ResourceVisual.FitHitbox` 연결은 그래픽 연결 수정이므로 유지·협의. Codex 현재: `World/ResourceVisual.cs`, `Units/KemonoModel.cs`, `Buildings/FacilityVisual.cs`, `World/CritterVisual.cs`·`Critter.cs`·`WildMonster.cs`, Resources 모델·URP 머티리얼. H1 특성 디자인은 `docs/HANDOFF-CLAUDE-TRAITCHIP-1025.md`·H1 패치를 그래픽/기존 버그 완료 뒤 적용. 성능 최적화·림월드식 채취 변경 보류.

- **RECOVERY-1025 통합 상태 (2026-10-26 / Codex)** — H1 특성·c13·c22, H2 c10·설정 패치 적용. H2의 수면 장수 자동 제외는 적용하지 않고 기존 선택 규칙 유지. Unity: H2 17, 시작/클릭/보급 13, 원정 UI 27, 시설 710, 자원 200, 리그 41, 사건/공사/시체 14, 시민/전리품/사체 10, 특성 25, 메뉴/날씨 14 통과(각 검사 최신 결과 기준). 장수/생물의 실제 리그로 Animator 바인딩, Wasp/NightBeetle·Resident·공사/철거/광맥·전리품 모델 연결. Light의 Unity null 처리 수정. 새 캡처·SAVE 미수행. Claude 추가 전달 범위: Chitin/Wax/Component/Water/Soil 전용 자원 모델 제작(별도 사본, 본 프로젝트·Unity 수정 금지); 아직 납품/통합되지 않음. 미해결 감사 항목: 이 자원 모델, 낚시터 표시, 미사용 DigSite 모델, 원정지 평판 지형. 성능/림월드식 채취 보류.

- **REOPEN-1026 / Codex / 진행 중** — 사용자 Game 화면 재현: 엎드린 장수·정지 애니메이션·상단 초상화·가장자리 카메라 이동·우클릭 채집. 담당 Units/KemonoModel·RigAnimator·AntVisual·UnitSelectionController, UI/TraitChip·CharacterPortraits, Camera/IsometricCameraController, Core/UserSettings 및 관련 검사. 특성 화살표 제거. 강제 Animator.Update만의 검증 대신 실제 프레임 진행 상태·채집/운반을 검증. SAVE 전 캡처/일지/커밋 없음.

- **REOPEN-1026 수정 검증 / Codex** — 리그 이전 뒤 남은 KemonoModel 부모 +90도 제거, Idle_A 실제 포즈 평가 후 크기/클릭 범위 계산. CharacterPortraits의 잘못된 Idle 상태를 Idle_A로 교체·AlwaysAnimate 평가. TraitChip의 화살표 그리기·빈 영역 제거. 가장자리 이동 저장 설정 0→18 복구, H2 검사 종료 시 실제 카메라 값 원복 추가. LiveGraphicsProbe로 기존 눕는 자세(up≈0.09) 재현 후 LiveRepairChecks에서 실제 프레임상 upright/걷기/뼈대/상단 가장자리팬/우클릭목재 채집량감소·운반량증가 12 통과. 기존 리그41·특성16 통과. 초상화 정면모델 수직up=1 수치 확인, 육안 캡처 검수 미실시(SAVE 규칙). SAVE/commit 없음.

- **GATHER-1026 / Codex / 진행 중** — 실제 사용자 Play에서 자원 접근 잔여거리 0.003~0.47m·stoppingDistance=0으로 MovingToNode 고착 재현. WorkerAnt 기존 NearDestination(.5m) 허용·도착 시 경로 정지(채집/반납/건설), AntVisual은 작업 상태를 미세 이동보다 우선. UnitSelectionController에서 채집 지시 성공/실패와 이동 마커 구분, 신규 GatherOrderFeedback·ResourceNodeStatus로 대상 링 및 이동/채집 상태 표시. 좌클릭 선택·우클릭 명령 규칙 유지.

- **GATHER-1026 공통 재생 원인 수정 / Codex** — Animator 아래 SkinnedMeshRenderer 0개(메시가 형제 노드)인데 CullUpdateTransforms로 설정되어 전체 재생이 생략됨. GatherArrivalChecks에서 Tool_Chop 상태 진입 후 실제 뼈대 정지 FAIL 재현. RigAnimator 공통 AlwaysAnimate + 스킨 updateWhenOffscreen, Kemono/Critter/Civilian의 Cull 덮어쓰기 제거. 수정 뒤 실제 5m 접근·메시 걷기·도착/벌목 전환·채집량·경로정지·취소표시 14 통과. 강제 Animator.Update 없이 장수 13동작·생물6리그·시민 실제 메시변형 40검사 통과. 표시상태는 채집 이동/채집/자원운반 구분. 새 캡처·SAVE 없음.

- **WEATHER-1026 / Codex / 진행 중** — 사용자 사막 벚꽃 오류 및 전 맵 생물군계·기후·계절 반영 요청. 담당 Map/SeasonVisuals·WeatherSystem, 관련 검사. 사막 드문 강수/모래폭풍, 물가·숲 강수/안개, 한랭/열대 강수형, 꽃잎·낙엽·적설 제한 통합검증. 성능/림월드 채취 변경 아님.

- **LOOT-LOOP-1026 / Codex / 진행 중** — 사용자 '캐자마자 동일 물건 재생' 확인. 운반 목적지 없는 바닥 더미를 다시 줍고 DropCargo로 재생성하는 경로 차단: WorkerAnt.HasCargoDestination, 수동 바닥 줍기 및 CommanderDuty 자동 운반에 같은 도달/허용/공간 조건 적용. 자연 식물의 기존 재성장 규칙은 변경하지 않음. WEATHER-1026: 445 매트릭스/실제 입자·셰이더·저장 복원 검사 통과.

- LOOT-LOOP 추가 원인: 비축더미가 삭제된 시작에서도 BaseBin의 추상 공간이 남고, 그 입고 지점을 임의의 창고로 대신 골라 품목 금지를 우회함. WorkerAnt 입고/배송 목적지 해석은 실제 BaseBin=Stockpile, 다른 칸=해당 Host로 통일. 새 창고의 품목 금지/허용 검사에서 발견해 수정 중.

- **WEATHER/LOOT-1026 실행 검증 / Codex** — 날씨 445검사(7biome×3climate×4season 난수 및 6biome×3climate×4season 실제 계절 입자/셰이더/복원), 자연 채집14 회귀 통과. 운반 루프10: 추상 공간만 있는 상태 수동/자동 줍기 거부·바닥 동일 더미 보존, 창고 품목 금지 준수·허용 뒤 실제3개 한 번 입고. 초기 돌 픽스처는 근처 다른 돌 더미까지 묶어 운반하는 정상 로직 때문에 정확3 검사와 충돌하여 독립 부품 더미로 검증. 기존 식물 재성장 시간은 보존. SAVE/캡처/커밋 없음.

- **SAVE-1026 / Codex** — Claude 3세션·Codex E/F/G 인계·현재 diff 전체 대조 완료. D04 363·D06/D07 82·D09 208 재검증. 실제 채집 Tool_Harvest/표시·후보 특성·원정UI 캡처 확인. 본 프로젝트 반영과 F1/F2 미통합 스냅샷을 분리 보존. 전체 근거/잔여는 docs/SAVE_AUDIT_20261026.md, 최종 커밋·Notion 링크는 log.md. 성능/림월드식 채취 보류 유지.

- **AUTO-WORK-1026 / Codex / 진행 중(사용자 구현 요청)** — 자연 자원 지정·채취/운반 분리·작업 관리/대기 사유·저장 연결과 기존 시설 자동 작업 회귀. 소유 ResourceNode·WorkerAnt·CommanderDuty·UI/GatherDesignation·GameMenuTasks/Duty·WorkTargetPanel·Save 노드 DTO/왕복·관련 검사. 추가 사용자 요청: 장비 FBX 형상/부착점 전수 점검, 작업 애니메이션·건설 다중 작업 위치·Unity 연결. 소유 KemonoModel/AntVisual·건설 접근 경로·Blender 제작 스크립트/FBX. 성능 최적화 보류 유지, SAVE/캡처/커밋 없음.

- **AUTO-WORK / EQUIPMENT-ANIMATION 적용·검증** — 자연 작업 클릭/범위 지정·취소/정지·재성장 후 미지정·작업표0 준수·요청 순서 저장. 채취/실물 운반 분리, 가공대/공방 재료 배송 및 바닥 산출, 재고 유지의 진행/바닥 산출 중복 방지, 배송/대기 식품 신선도, 취소/주문 삭제 시 도착 재료 반환. 연구 담당 생활 중 유지·연구 중복 진행 차단·복귀, 원제작자 유지. 작업 목록 종류/대기 필터·담당/대상 위치 이동·범위 미리보기 연결.
- **FBX 범위·근거** — 기존 손 장비71개 배치용 오프셋 제거(도끼 머리/자루는 같은 강체 메시), 톱1개 추가, 캐릭터 동작88개 전신 움직임 재수출·Unity 재연결. 건설 세 지점 실제 경로 이동·망치/톱 교대, 작업/생활/운반 동작 연결·맨손 공격 별도 상태·방패 왼손. 원본 백업 `work/auto1026/equipment-before`, `animation-before`; 제작/감사 스크립트 `art/production-kit-20261019/scripts/*1026.py`, `work/auto1026`. Blender 렌더에서 도끼/톱 포즈 확인. 임의의 다른 프로젝트 FBX 전체 변경은 아님.
- **현재 검사672개 통과** — AutoWork1026 32(실제 채취·배송·가공·3지점 건설), AutoWorkSave1026 20(씬 재로드·신선도·중복환불·주문 삭제), ResearchCraft1026 22, AutoWorkUI1026 16, VividEquipment1026 572(88 실제 프레임 스킨 변화·72 손 장비/갑옷 연결), LootLoop10. 결과 `Temp/*1026-result.txt`, `Temp/loot-loop-result.txt`. 컴파일 오류0·diff check 통과. Blender FBX88 이름/계층/유한 포즈 검사; armature-only FBX 첫 포즈 차이 때문에 rest matrix 동일성/Blender 재구성 뼈 길이 동일성은 증명하지 않으며 실제 Unity 스킨 검사로 별도 검증.
- **검증 한계/보존** — UI 버튼·경계/스크롤은 실행 검사, Unity 새 캡처·전 장비/전 작업 조합 육안 검수·Player 빌드는 미수행. F1/F2 미통합과 기존 프로젝트 잔여/성능 보류는 종전 그대로이며 이 항목으로 전체 기획 완성을 선언하지 않는다. 검사 중 시계 역행으로 RocketEconomy 예외가 발생해 검사 시간을 앞으로 진행하도록 픽스처 보정(런타임 미변경). 최종 SAVE/개발 일지/캡처/커밋/push 없음. 카카오톡 도구 없음.

- **F-INTEGRATION-1027 / Codex / 적용·실행 검사 완료** — F1 인재 평가·외부 조사와 F2 지하 탐사를 본 프로젝트에 통합. 평가/조사/탐사 메뉴·장수 상세 별점·건설 목록·선택 대상 패널·원정 진입 버튼 연결. 숫자 CA/PA 숨김, 담당자의 생활 중 평가 정지, 탐사 담당 실제 이동·생활 후 복귀·정전 중 작업 연출 정지, 파견 식량 부족/본거지 무인 확인 연결. 기존 자동 작업/그래픽 변경 보존.
- **F 통합 검증273개 통과** — ImplD02D03 100(평가·외부 조사·발각/포로·귀환 보고·저장 복원), ImplD08 79(결정적 후보·전력/진행·유한 매장층·저장), FIntegration1027 20(메뉴 조작·정보 비공개·탐사 실제 이동/수면), AutoWorkSave1026 20, ResearchCraft1026 22, AutoWork1026 32. `Temp/*-result.txt` 근거. 컴파일 오류0·diff check 통과. 포로 검사 후 메뉴 검사를 이어 돌릴 때 포로를 담당자로 고르던 검사 픽스처는 배정 가능한 장수를 고르도록 보정. Player 빌드·새 Unity 화면 캡처·전체 화면 육안 검수는 미수행.
- **남은 범위** — 지하 탐사는 발견/공유 유한 매장층 API까지이며 심층 채굴기·가스 추출기 등의 실제 생산 연결은 미구현. README에 명시. 전체 기획 완성을 뜻하지 않음. SAVE/개발 일지/Notion/캡처/커밋/push 없음. 카카오톡 완료 알림 도구 없음.

- **UNDERGROUND-PRODUCTION / Codex / 적용·실행 검사 완료** — 심층 채굴기·원유 시추기·가스 추출기를 공유 유한 매장층에 연결. 고체는 채집 작업표·실제 접근·작업 동작·바닥 산출·별도 운반, 원유/가스는 자동 추출·액체/기체 전용 배관/탱크. 정전·출력 포화·배관 단절·생활 중 정지, 작업 진행/담당·잔량·탱크 저장 복원, 지점당 한 시설/예정지 설치 판정. 표본 중복 생산 차단·실제 반입 후 연구. 지표 OilDeposit도 잔량600·구저장 초기화 플래그로 유한화. 설비는 기존 FBX 재사용, 발견 목록의 시설 배치·설치 전 잔량/속도/전력·선택 상태 연결. 경험치는 실제 처리한 작업 시간만 지급.
- **생산 연결 검증171개 통과** — UndergroundProduction1027 57(실제 표본 운반·층 차감·출력 포화·액체/기체 분리·정제소·재설치/저장), UndergroundPowerOrder1027 15(실제 전력망 소비/정전·작업 중 저장/복원·실제 프레임 재개·기계 고갈), ImplD08 79(탐사/층/구저장 회귀), AutoWorkSave1026 20. 결과 `Temp/underground-production1027-result.txt`, `Temp/underground-power1027-result.txt`, `Temp/ImplD08Checks-result.txt`, `Temp/autowork-save1026-result.txt`. 컴파일 오류0·diff check 통과. Graphify 코드 갱신 완료(15449노드/33344간선, LLM 토큰0). 검사 픽스처는 초기 로딩 대기·창고/메뉴 재개·작업 스캔 주기·격자 중심을 맞춰 실제 경로/배관과 같은 조건으로 검증.
- **생산 범위/잔여** — 잠정 100W·기본5초당10, 신규 심층/가스 설비 강철10·구리5. 강철급 기계로 티타늄 광석까지 채굴, 성휘 광석은 표본까지만 가능하며 티타늄 합금급 기계/합금 생산은 미구현. 가스 소비 산업·원유/가스 화물 용기 충전/반출·유출 처리는 별도 잔여다. 새 Unity 캡처·전체 화면 육안 검수·Player 빌드 미수행. README 갱신, SAVE/개발 일지/Notion/캡처/커밋/push 없음.

- **ADVANCED-INDUSTRY / Codex / 적용·실행 검사 완료** — 티타늄 광석→가스 제련로의 티타늄/합금 제조→합금 심층 채굴기→성휘 광석 연결. 가스 발전기는 기체 배관 연료를 실제 전력망에 공급하며 배관 단절 시 투입한 연료까지 사용, 수요 없으면 연료 시간 보존·출력0W. 제련로는 전력/제작 담당/실물 배송·생산 목록/출력 공간·원제작자 유지, 가스 묶음 선투입 플래그를 저장해 중복 차감 방지. 기존 일반 Processor·PowerNode·채굴 경로 보존.
- **산업 검증151개 통과** — AdvancedIndustry1027 52(우선순위 자율 배송·전력/가스 조건·연료 투입 후 단절/정전·잘못된 미지급 저장 거부·실제 씬 저장 왕복·합금/성휘 산출·발전/전력망·수요0·메뉴), UndergroundProduction1027 57, ResearchCraft1026 22, AutoWorkSave1026 20. `Temp/advanced-industry1027-result.txt`, `Temp/underground-production1027-result.txt`, `Temp/research-craft1026-result.txt`, `Temp/autowork-save1026-result.txt` 근거. 컴파일 오류0·diff check 통과. Graphify 코드 갱신15996노드/34514간선(LLM 토큰0). 검사 중 기존 바닥 운반 요청과 순서를 겨루지 않도록 제련로 대상 우선순위9로 고정해 실제 자율 배송을 검증했다.
- **산업 UI/모델·범위** — 건설 작업 탭의 가스 제련로/합금 심층 채굴기, 전력 탭의 가스 발전기·과학 시설/매장층 배치·선택 상태 연결. 자원 enum 끝에 Titanium/TitaniumAlloy 추가(54종·기존 번호/64비트 허용 마스크 보존). 기존 제작 FBX4개를 Resources로 복사·연결(티타늄/성휘 광석·티타늄/합금 주괴); 시설은 기존 모델 재사용. 제련로200W·묶음당가스1, 광석2→금속1/작업14·티타늄2+강철1→합금2/작업20, 상위기계합금10+구리5, 가스발전400W·가스1당10초는 잠정값이며 README에 명시. 용기 충전/반출·유출 처리·추가 화학 제조법은 남음. 전체 화면 육안/새 캡처·Player 빌드 미수행. SAVE/개발 일지/Notion/커밋/push 없음. 카카오 완료 알림 도구 없음.

- **SAVE-1027 / Codex** — 위 항목은 각 작업 당시 기록이며 최신 상태는 log.md와 docs/SAVE_AUDIT_20261027.md 기준. F1/F2·실제 지하 생산·상위 산업 연결까지 통합했고 메뉴5장 직접 검수·Notion 첨부. SAVE 중 제련 조건 안내/메뉴 위 월드 표식 겹침 보완, UI16 재통과. 전체 장비 조합 육안/Player 빌드·기획 전체는 미완료. 기존 로컬 변경 보존·검증 파일 선택 저장.

- **INDUSTRY-FOLLOWUP / Codex / 진행 중** — 사용자 남은 작업 진행 요청. 소유 SupplyFacility.Refining 신규·SupplyFacility Tick, CommanderSupplyWork/CommanderWork/Duty·UnitSelectionController, Demolition·UndergroundProduction·배치/UI 선택 안내, AgentScripts 산업 후속 검사. 정유 작업자/실제 접근·전력/입출력 대기·저장과 고갈 채굴기 이전/목적지 예약을 먼저 연결. 미정 유출 규칙은 추가하지 않음. SAVE/캡처 없음.
- **INDUSTRY-FOLLOWUP 용기 추가 / Codex** — SupplyFacility.Containers·GameMenuContainers 신규, ResourceType 충전 원유/가스 2종·ProductionProp 기존 모델 재사용, IMaterialSink 배송·제작/운반 작업표·작업 관리·저장 검증 연결. 기존 즉시 원격 차감 충전은 지시→실물 배송→현장 작업으로 교체. 용기 10단위/5초는 기존 연료 드럼통·시설 값을 재사용한 잠정값.
- **INDUSTRY-FOLLOWUP 적용·실행 검사 완료(2026-10-28) / Codex** — 정유 현장 제작 담당/전력·입출력 대기·진행·서비스 저장, 고갈 기본/합금 채굴기 이전·목적지 예약·포장/설치·저장, 원유/정제유/가스 용기 실제 배송·제작·충전/배출·빈 용기 반환·정지/취소·저장/UI 연결. AntVisual.Work는 공급 시설 도착 전 작업 연출을 막고 이동 동작 유지. IndustryFollowup37(걷기 포함), ContainerIndustry61(최종 재실행76초), UndergroundProduction59·AdvancedIndustry52·AutoWorkUI16 통과. AdvancedIndustry CLI 시간 제한 뒤 작업이 이어져 결과 파일52 완료를 확인한 후 다음 검사 시작. 기존 B10/C5 즉시 정유/충전 기대를 작업자/배송·설정 화면 방식에 맞게 갱신(그 전체 레거시 묶음은 미재실행). 컴파일 오류0·소스/문서 diff check 통과, Graphify16096노드/34800간선(LLM0). 컨테이너 신규2종은 기존 enum 끝에 추가(56종), 기존 소품 FBX 재사용. 유출·화학 제조법·일부 모델·전 장비 육안·Player 빌드 등 잔여/성능 보류 유지. 캡처·개발 일지·Notion·커밋/push·SAVE 없음.

- **CHEMICAL-1028 / Codex / 진행 중** — 사용자 계속 진행 요청. 소유 IndustrialRecipes·Processor.Industrial/취소 환급·SavePreflight.Integration, BuildingKind/ResourceType 끝 추가·SupplyBuildingTemplates·ScienceEffects·BuildScreen·GameMenuProcessor·WorkTargetPanel, FacilityVisual/ResourceVisual·신규 제작 FBX2개 및 임포트 검사. 확정 원유→플라스틱, 실제 배관·제작 담당·전력·유한 재료·저장 연결. 기존 산업 유체 선투입 상태를 재사용하고 취소 시 미소비/소비 원칙 확인. 미정 유출·신규 임의 제조법은 추가하지 않음. SAVE/캡처 없음.
- **CHEMICAL-1028 적용·실행 검사 완료 / Codex** — 원유 배관→화학 작업대→플라스틱 바닥 산출/운반 연결. 제작 담당 실제 접근·원제작자 유지·전력/공급/출력 대기·유체 선투입과 저장 복원, 취소한 일반 원료70% 내부 회수·다음 묶음 재사용·중복 반환 방지. 기존 gasPaid 저장 필드를 산업 유체 선투입으로 재사용하고 bufferedOil 검증 추가(활성 묶음2, 대기0~2, 다른 제작대0). ChemistryBench/Plastic은 enum 끝 추가(자원57종). 건설 메뉴·기존 지하 탐사 연구 해금·일반 생산 지시/UI·작업 연출 연결. 새 Resources/Facilities/CC_ChemistryBench.fbx와 Resources/ResourceModels/Proc_Plastic.fbx 및 URP 머티리얼을 반입했고 임포터 목록에 등록. 잠정 강철10/구리5·200W·원유2→플라스틱1/작업14, 최종 연구/시대 분리 미정. ChemicalIndustry48(실제 복원 후 재개 포함), FBX2/렌더러3 임포트, AdvancedIndustry52·AutoWorkUI16·AutoWorkSave20 회귀 통과. 컴파일 오류0·diff check 통과. 장시간 검사는1000J 배터리만으로 부족해 저장 가능한 실제 발전기+석탄 전원으로 픽스처 보완; 배터리 소진 정지는 정상 동작. Graphify16125노드/34881간선(LLM0). 새 Unity 캡처/전 화면 육안·Player 빌드·SAVE/개발 일지/Notion/커밋·push 없음. 미정 유출·기타 제조법/가공시설·일부 모델 등 잔여, 최적화 보류 유지.

- **RESOURCE-GRAPHICS-1028 / Codex / 진행 중** — 갑각/밀랍/부품/물 화물 + 갑각 자연 매장지 전용 모델5개 생성·URP 임포트·ResourceVisual 연결 및 크기/충돌/저장 검사. 소유 scripts/fill_resource_models_1028.py·모델5개·ResourceVisual·ResourceGraphics1028Checks. 설치 Blender 독립 실행 사용, 기존 사용자 씬 미수정. 현재 SAVE+종료 요청에 포함해 검증·캡처·기록 후 선택 저장.
- **RESOURCE-GRAPHICS-1028 / SAVE 검수 완료** — 신규 자원 모델6개 Blender 생성/원본 보존·URP 임포트, 현재57종 바닥 화물 모델·클릭·수량과 실제 저장 복원 및 자연 갑각/흙239 검사 통과. 물통/부품 크기 제한, 천연가스 화물은 기존 용기 재사용. 운반 루프10 회귀. 실제 자원/화학/용기/정유/작업 관리 화면5장 직접 검수·Notion 첨부. 검수 카메라만 고정한 뒤 복원/Play 종료. 자세한 SAVE 보존/잔여는 docs/SAVE_AUDIT_20261028.md·최신 log 기준. 사용자 SAVE 후 종료 요청은00:41~01:41 KST만 유효하며 최종 원격 확인 뒤 수행한다.

- **FISHING-VISUAL-1028 / Codex / 진행 중** — 남은 작업 요청: 기본 큐브 낚시터를 전용 발판·낚싯대·바구니 FBX로 연결. 소유 ResourceVisual·신규 Fishing_Bank 모델/생성 스크립트·FishingVisual1028Checks. 낚시 해금/계절 한도·이동/실물 운반·저장 의미 유지, 신규 기획/유출 규칙 추가 없음. SAVE/캡처 없음.
- **FISHING-VISUAL-1028 적용·실행 검사 완료 / Codex** — 전용 Fishing_Bank FBX/원본 blend/생성 스크립트, 기존 URP 임포터 재사용. ResourceVisual의 낚시터 제외를 제거하고 2.2m 모델·선택 범위 연결. 첫 크기 검사에서 낚싯대 높이를 낮춰 보정. 신규26개 검사(모델·중복 부착 방지·클릭·연구 잠금·어획·고갈·계절·실제 저장/복원) 통과. 기존 FishingChecks의 전 작업 금지 픽스처에 낚시 허용을 명시하고 임시 저장 경로로 격리; 실제 물가 이동·어획/반납·연구/계절 회귀 PASS. 컴파일 오류0·diff check 통과, README 잔여 갱신. Game 화면 육안·캡처/Player 빌드 미실시, SAVE/개발 일지/Notion/커밋·push 없음. 원정지 지형·기타 소품/미정 유출/제조법 남음, 성능 보류. 카카오 완료 알림 도구 없음.

- **EXPEDITION-TERRAIN-1028 / Codex / 진행 중** — 남은 원정지 그래픽: 동일 큐브 지면을 바이옴 텍스처·외곽 경사 메시·전용 자연물/잔해 소품으로 연결. 소유 ExpeditionTerrain 신규·ExpeditionSite 지면 생성·SeasonVisuals 바닥 색조 공통 함수·AgentScripts 원정 지형 검사. 70m 이동/착륙/저장 좌표 유지, 장식은 외곽/충돌·자원 없음. 계절 바닥은 거점 바이옴/기존 기후 규칙 재사용. SAVE/캡처 없음.
- **EXPEDITION-TERRAIN-1028 적용·실행 검사 완료 / Codex** — ExpeditionTerrain 메시/기존 TerrainBlend 셰이더·바이옴 텍스처·기존 FBX 소품 재사용. 중앙70m 평지/충돌/내비 소스 유지, 외곽78m 경사·거점당16개 충돌/자원 없는 장식. 거점 바이옴+기존 기후/계절로 바닥 색·눈·낙엽 제한; SeasonVisuals 기존 색조 계산만 공통 함수로 추출. 실제 생성 수는 구기록30과 달리 현재3띠33개임을 확인. 2008검사(33곳 전후·메시/클릭 지면/경로/소품·6바이옴 계절/기후·셰이더 재바인딩·실제 저장 복원), 실제 착륙/식량·흙 회수/수송 적재12, 기존 날씨445 PASS. 검사 중 초기33개/자원 모델 이름 기대 정정, 착륙 NavMesh 등록/경로 준비 후 주문하도록 픽스처 보완. 컴파일 오류0·diff check 통과. Temp/expedition-terrain1028-result.txt·expedition-terrain-gather1028-result.txt 근거. Game 화면 육안/캡처·Player 빌드 미실시. SAVE/개발 일지/Notion/커밋·push 없음, 미정 유체/제조법·기타 소품/전체 검수 남음. 카카오 완료 알림 도구 없음.

- **SETTINGS-PERFORMANCE-1028 / Codex / 진행 중** — 사용자 설정 점검/이상 수정 뒤 빌드 심한 랙 최적화 요청. 기존 성능 보류를 이번 요청 범위에서 해제. 우선 UserSettings·GameMenuSettings/Keys·기존 H2 검사로 실제 적용/복원 점검; 성능은 독립 임시 게임·동일 조건 Editor/Player 측정 후 병목 파일에만 착수. 기존 Quality/URP/씬 로컬 변경 보존, SAVE/캡처 없음.

- **MOVE-MARKER-1028 / Codex / 진행 중** — 사용자 우클릭 초록 흔적이 사라지지 않음. MoveMarker의 게임 시간 기반 Destroy 지연/축소를 실제 시간 수명으로 변경. 일시정지/자동 정지에서도 정리되도록 수정·재질 반환·연속32클릭 검사. 설정/성능 작업 유지, SAVE 없음.

- **SETTINGS-PERFORMANCE-1028 / 범위 갱신** — 사용자 지시: 게임 기능·예정 자산을 삭제하지 않고 최적화. 원본 FBX/예정 기능 보존. 화면 밖 캐릭터 실험은 철회해 work/performance1028-experiments에 보관했으며 RigAnimator/KemonoModel/CharacterPortraits는 원래 처리로 복구. 확정 수정: 품질 변경 시 vSync 덮어쓰기·해상도 개별 복귀, 닫힌 건설 UI 재배치와 SceneObjects.Any의 불필요한 배열 생성. 에디터 GC.GetAllocatedBytesForCurrentThread는 1MB 대조 할당에도0을 반환하므로 앞선0바이트 근거는 폐기, 실제 Player ProfilerRecorder로 비교한다.

- **SETTINGS-PERFORMANCE-1028 / 1차 수정·검증 완료** — 품질 먼저 적용 후 vSync/프레임 제한 복원(4조합), 모니터 기본 해상도 버튼·설정 UI15검사 통과. Mobile 품질에서 Standalone 제외만 제거해 PC 낮음 선택을 빌드에도 포함(기본 PC/높음 유지, 기존 Quality/URP 로컬 변경 보존). MoveMarker는 unscaledDeltaTime 수명·재질 정리로 일시정지/배속/32연속 클릭8검사 통과. 애니메이션 FBX 위치 값이 아바타 대비1/100인 공통 원인을 재현해 88개 임포트 globalScale100 및 KemonoImport 재발 방지 적용(원본 FBX 보존). 실제 뼈/스킨 크기·발/지형·선택 충돌체·걷기·일시정지 저장 복원71검사, 실제 프레임 동작40검사 통과. AntVisual.Ground는 표시/선택만 지면에 맞추며 경로·저장 좌표는 유지. BakeMesh(useScale:true)로 측정 보정, Idle 포함 기존1mm 동작 기준 유지. 화면 밖 애니메이션/부품 비활성 실험은 최종본에서 철회.
- **SETTINGS-PERFORMANCE-1028 / 성능·배포 근거** — 건설 창 닫힘/크기 불변 때 전체 재배치 생략(열기/크기/탭18검사), SceneObjects.Any 등록 목록 직접 조회(기존 첫 활성 결과6검사). 같은 개발 Player에서 기존 경로/수정 경로만 전환해 각각2회 번갈아 실행: 1920×1080·PC높음·보통숲 seed281028·장수3·1배속·무제한, 5초 준비+25초 측정, RTX2070/i7-8700. 평균 프레임11.3326→10.0752ms(88.24→99.25FPS), 스크립트2.7072→1.8311ms, 유효 ProfilerRecorder 임시 할당240578→227397bytes/frame. 각각11.1%/32.4%/5.5% 감소, Temp/performance1028/comparison.json 및 controlled-*.json 근거. 대규모/장시간 플레이는 미검증. 일반 배포 Builds/Windows/ant.exe 빌드 build_b0f9af6e8966 성공(오류0·기존 패키지/셰이더 경고518), 시작 응답·사용자 설정/저장 보존 확인. 일반 빌드는 측정 자동 실행 훅 제외. 사용자 기능/예정 자산 삭제 없음. Graphify16308노드/35282간선. SAVE/캡처/개발 일지/Notion/커밋·push 없음, 카카오 알림 도구 미제공으로 전송 불가.

- **REMAINING-1028 / Codex / 진행 중** — 사용자 추가 시설·제조법/소품/화면·대규모 장시간 성능 검증 승인. 유체 유출은 사용자 답변으로 보류. 우선 숯가마·코크스로·도자기 가공과 숯/코크스 사용을 기존 Processor에 연결. 소유 IndustrialRecipes·Processor 제조법·BuildingData/ResourceType 끝 추가·SupplyBuildingTemplates/SupplyFacility 분류·PowerNode·FacilityVisual/ResourceVisual·BuildScreen·임포터·관련 검사. 기존 애니메이션/발 위치 화면 검수와 별도 임시 저장 기반 성능 검사 진행. 기존 자산/기능 보존, SAVE/일지/캡처/커밋 없음.

- **REMAINING-1028 / 기능 검사 완료·배포 빌드 중** — 숯가마·코크스로2종, 숯/코크스/도자기3종(enum 끝 추가, 현재60종), 기존 가마 도자기·석탄 대신 숯/코크스 명시 제조법과 장작/증기 발전기 대체 연료 연결. 목재2→숯1/작업12, 석탄2→코크스1/작업14, 점토2+연료1→도자기1/작업12, 숯가마 돌10+점토5·코크스로 벽돌10+철5·기존 연료당 시간 재사용은 잠정값. 일반 Processor 생산/실물 배송/원제작자/취소·환급/저장·건설 비용·UI 흐름 재사용, 고정 제조법 목록을 종류별1회 생성해 반복 배열 생성 제거. 기존 제조법 번호 유지(가마0/1, 제련0~5), 대체 연료는 뒤에 추가. art의 원본 FBX5개를 Resources에 반입·URP 재질 변환(원본 보존). RemainingIndustry66(실제 숯 배송/작업·정지/인계·연료 소비/발전·실제 저장 복원·모델 연결), AdvancedIndustry52·BuildScreen18·AutoWorkUI16 재통과. 화면 육안 검수와 FPS/대규모·장시간 성능 측정은 Palworld 동시 실행 관련 사용자 답변으로 보류, 유출도 사용자 명시 보류. 나머지 기획 제조법/시설 전체 완료로 간주하지 않는다. 캡처/SAVE/일지/Notion/커밋·push 없음, 카카오 알림 도구 미제공.

- **REMAINING-1028 / 배포 확인 완료** — 일반 Windows 빌드 build_60484a4226e4 Succeeded, 오류0/기존 패키지·셰이더 경고518. Builds/Windows/ant.exe 및 Data/Managed 갱신, 개발용 측정 훅 제외 확인. 별도 실행 시작/응답 확인, 사용자 settings/discoveries 해시 변경0. 검수용으로 실행한 프로세스만 경로+전용 로그 인수 확인 후 종료, Editor Play 종료. Graphify16335노드/35347간선(LLM0) 갱신, 관련 diff check 통과. 새 제조법 전체 목록 중 이번 완료는 숯·코크스·도자기와 대체 연료이며 제분/착유/양조·증류/고무 등 나머지 기획 생산은 후속 범위로 남는다. 유출·육안·FPS/대규모 장시간은 사용자 보류. SAVE/커밋·push 없음.

- **FOOD-INDUSTRY-1028 / Codex / 진행 중** — 사용자 남은 제분·착유·양조·증류·고무 진행 요청. 저장 호환을 보존한64종 초과 허용 필터, 필요한 원료/작물/자원·제조법·새 시설4종·발효 단계/자동 작업·저장/UI 연결. 소유 StoreLedger/ResourceManager/ColdStorage·FoodProcessingRecipes 신규·ResourceType/FarmCrop/FarmSeeds/MaterialDeposits·Processor/Fermentation 신규·SupplyTemplates/ScienceEffects/UI·Save 호환/검증·관련 검사. 유출·그래픽 육안/FPS/장시간은 보류 유지. SAVE/캡처 없음.

- **FOOD-INDUSTRY-1028 / 기능·호환 검증 완료·빌드 중** — 자원15종 끝 추가(총75), 제분기/착유기/양조통/증류기4종·기존 화학 고무 제조법 연결. 야생 원료·작물 종자/재배5종, 고무 수액 자연물, 넓은 밭 종자2개 확보용 두 번째 원료 개체를 기존 mat 키를 바꾸지 않고 뒤에 추가. 밀가루/해바라기유·양조6제조법(술5종)·발효주5종 증류/고체연료 대안·고무. 양조 원료 배송→투입 작업→무인 발효→장수 회수, 취소 확인/원료 소실·중단/목표 재고·저장; 화학은 제조법별 원유 선투입을 구분해 고무가 원유를 소비하거나 기존 환급 원유를 지우지 않게 함. 고체연료/화학 배합 기준 재사용한 잠정 비용·시간·게임1일 발효, 상세 README. 64이후 허용 필터는 별도 금지 ID 목록으로 저장해 기존 ulong/품목 비트 보존, 냉장/퇴비 예외 유지. 저장20: 이전19의 운송 화물 배열0패딩·옛 창고 필터/재고 보존, 잘못된 고품목/과대 화물 거부. 개별 식품은 창고별 값만 저장하는데 총량0으로 덮어 지워지던 RestoreBins 공통 문제 수정(밀가루/기름/술/기존 완성 요리 보존). 원본 art 보존하고 FBX19개 URP 연결/검사. FoodIndustry96(실제 파종·밀 성장·제분 배송/작업, 종자, 발효/취소/회수·재고/필터·실제 저장 복원·증류/고무), 실프레임 무인 발효/일시정지3, UI13, 구19 창고/화물 호환6, 기존 Chemical48·AdvancedIndustry52·AutoWorkSave20·BuildScreen18 통과. UI 경계 검사는 Rect.Contains의 상단 경계 제외를 .5px 허용으로 보정, 원료/실물 검증 기준은 유지. 음주 행동/효과·전체 식품 세부 목록은 미구현이며 이번 완료로 표시하지 않음. 유출·그래픽 육안·FPS/대규모 장시간 측정은 사용자 보류. SAVE/캡처/일지/Notion/커밋·push 없음.

- **FOOD-INDUSTRY-1028 / 배포 확인 완료** — Builds/Windows/ant.exe 일반 빌드 build_fbaa181ca726 성공(오류0·기존 패키지/셰이더 경고518, 114.4초). 시작/응답 확인, 시작 검사 전후 사용자 settings/discoveries 해시 변경0, 기존 auto1.json은10/25 수정 시각 유지. 테스트용 실행만 경로/전용 로그 인수로 식별해 종료. 배포에 개발용 성능 자동 실행 훅 없음, Editor Play 종료·컴파일 오류0·관련 diff check 통과. 최종 Graphify16416노드/35539간선(LLM0). 실제 화면/장시간 성능은 보류 유지. SAVE/캡처/일지/Notion/커밋·push 없음. 카카오 완료 알림 도구 미제공으로 전송 불가.

- **SAVE-1028-FOLLOWUP / 검증·문서 완료, 원격 보존 단계** — 최종 일반 빌드 build_39682295663e 오류0/증분 경고33·시작 응답 확인(메뉴 초기화 기존 NavMesh 경고 기록). UI13 재통과·실제 화면7장 검수/Notion 첨부, 발효0% 중복/밀 영어/고무 취소 안내 수정. 사용자 설정/저장 경로/카메라 복원·Play 종료. Graphify16434/35589, LLM0. 일부 용기 비율·전체 그래픽/전 동작·대규모 장시간 성능은 미완/보류. 기존 첫 SAVE/구현 단계 기록은 당시 상태이며 후속 audit/일지를 현재 기준으로 사용.

- **SAVE-1028-FOLLOWUP / 완료** — f12e6ba 구현498파일 origin/develop push·원격 일치 확인. log/changelog/README·검증 사본·당일 Notion 일지/오류·캡처7장 보존 완료. 기존 로컬 변경 보존. 내부 checkpoint 자동 repack 오류는 기록하고 참조 삭제 안 함.
- **COOKED-FOOD-1028 / Codex / 진행 중** — 사용자 승인: 쌀→밥·밀가루→납작빵·쌀+해바라기유→볶음밥의 실물 배송/조리/식사/저장 연결. 기존 ProductionInputs·요리/식단/신선도 재사용, 구20 저장/기존2제조법 보존. 소유 Kitchen/Kitchen.Food·CookedFoods 신규·식사/간호/자원분류·저장이관·요리 UI·관련 검사/모델 임포트. 유출/음주/그래픽 전반/대규모 성능 보류 유지. SAVE/개발 일지/캡처/커밋 없음.

- **COOKED-FOOD-1028 / 기능·회귀·빌드 완료** — 밥/납작빵/볶음밥3종(enum끝, 현재78종), 실제 쌀/밀가루/기름 배송·조리·바닥 산출/운반·식단/포만감·냉장/부패·병상 식사 판정 연결. Kitchen은 기존 ProductionInputs 재사용·구 staged/stagedLots 필드 보존 이관, 저장21에서 구20 운송75칸→78칸 확장. 재료 일부 배송/조리 진행 저장·취소 전후 구분·원래2요리 유지. 신규88(실제 볶음밥 자동 배송/작업·5요리·신선도/삭제/취소·실저장·섭취·구20호환·FBX3/URP·UI), 기존 AutoWorkSave20/FoodIndustry96/구19호환6 통과. 최신컴파일0·코드문서 diff check0. 일반 Windows build_fe38ccfa997e 성공 오류0/기존 패키지·셰이더경고518,104.8초/304746665bytes. 실제 화면 육안/캡처는 미실시(SAVE전용), 예정 자산 삭제 없음. 다음은 사용자 승인한100항목 중 운영 막힘/가용 식량/중요 경고/기획 정합성1차.
- **OPERATIONS-1028 / Codex / 진행 중** — 사용자100항목 중 승인1차: 자동운영 막힘 안내/가용·배정·운반 식량/숨은 중요 알림/기획 정합성. 새 화면은 강제가 아닌 허용이므로 기존 작업관리·식단·알림 스크롤 재사용. 소유 WorkDiagnostics/FoodAvailability 신규·Kitchen/Processor 읽기 속성·GameMenuTasks/Kitchen/Processor/Diet·HUDController·ToastManager·관련 검사. 경제·성장·전투 수치나 기존 자동작업 규칙 변경 없음. 유출/전면그래픽/대규모장시간 보류 유지.
- **OPERATIONS-1028 / 기능·실행 검사 완료, 최종 빌드 중** — 기존 식단·작업관리·부엌/가공·HUD·알림 스크롤 재사용. WorkDiagnostics는 실제 자동작업/예약 규칙을 바꾸지 않고 금지/담당/경로/재료 상태와 대응 안내. FoodAvailability는 장수별 식단/경로·창고/건설배정/바닥/실운반/조리대/수송/타지역/부패 임박·휴대 보존식 금지 구분; 건설 소프트배정을 허위 차감하지 않음. 알림 의료/위협 우선·전체펼침/접기·숨은 경고 수명 보존28검사 PASS. Notion 자원/건물/UI/자동작업/구현점검/미정/기술 및 참조 유닛/특성 확인·진형 시대/비용/합산 미정 표기 교정. docs/OPERATIONS_REVIEW_1028.md에100항목 분류/1차와 후속 범위. 전100개 해결이나 경제/성장/전투 수치 조정 완료는 아님. SAVE/개발 일지/캡처/커밋·push 없음.
- **OPERATIONS-1028 / 최종 빌드 완료** — build_cae2028aed56 일반 Windows 빌드 성공(오류0·증분 기존 API경고33,29.8초/304762025bytes). 운영28·요리88·기존 생산96/저장20/구호환6, 코드/문서 diff check0. Play 종료·테스트 설정/저장루트 복원, 개발 일지/캡처/커밋·push 없음. 전100항목 완성은 아니며 경제/성장/전투 수치 계산은 후속.
- **OPERATIONS-1028 / 후속 확인** — 최종 실행 파일 시작/응답 확인 후 정상 종료. 시작 메뉴 초기화의 기존 NavMesh 경고1건은 남음. Graphify16535노드/35830간선·LLM0. 최종 플레이 화면 육안/캡처 및 대규모장시간 검수는 수행하지 않음. 기존 씬/Water/URP/Quality/TimeManager·raw art·prefab 변경은 그대로 보존, 인덱스 비어 있음.
- **REVIEW100-ECONOMY-1028 / Codex / 진행 중** — 사용자100항목 연속 수행 승인·산미포/림월드 우선 참조. 11~20/41~50 실제 수치 표·성장/노화/전투 경험치 검증. 소유 ColonyPopulation 세금 예측/행정 적분·인구/HUD의 납세 표시·경제/성장 검사·100항목 상태표. 확인된 시간분할 세입 오차부터 재현하고 수정; 세율·기술 경험치 수치를 근거 없이 변경하지 않음. 기존정책/저장/보류 유지.
- **REVIEW100-TRADE-1028 / Codex / 진행 중** — 74번 실제 왕복 교역에서 식량150 순증 재현. 소유 DiplomacyTrade/DiplomacyNegotiation·교역 회귀 검사. 일반 교역만 공정 가치 하한 적용, 정치 설득은 추가 요구액을 완화. 요구/원조/조약·포로 규칙과 저장 형태는 유지.
- **REVIEW100-BIRTH-1028 / Codex / 진행 중** — 43번 출산 간격 공백. 소유 NurseryChamber·CommanderPersonalState/CommanderAge·GameBalance의 회복 상수·양육 검사. 부모별1계절 회복(잠정)으로 건물 교체/다른 짝/저장 복원 우회 방지, 어린 장수 출산 제외. 기존 호감/식량/정원/성장 규칙 유지.
- **REVIEW100-CROP-1028 / Codex / 진행 중** — 21/22 작물 선택 근거: 신규5종 모두 균류와 같은180초/40개였음. 소유 FarmCropRules 신규·FarmPlot 성장 설정·GameMenuFarm 기존 작물 버튼 툴팁·관련 검사. 잠정 성장/수확량을 조리/양조/착유 역할에 맞게 구분하고 실제 값·보존·가공 필요를 표시. 겨울·생물군계·기후 규칙, 원래3작물 값·식품 보존 규칙 유지.
- **REVIEW100-RESEARCH-1028 / Codex / 진행 중** — 31~34 필수43노드 정합성/최소7연구 비용·노동 계산. 소유 GameMenuResearch의 기존 상세 예산 표시·계산 검사. 선택 연구의 미완 선행 포함 비용/남은 연구량을 표시하고 이미 지불한 중단 연구 비용 중복 계산 방지. 자동 다음 연구/연구소 승급 스펙 임의 대체 없음.
- **REVIEW100-POWER-1028 / Codex / 진행 중** — 39번 가용20J·냉동고20W+전등10W에서 모두 정전/20J 미사용 재현. 소유 PowerGrid·BuildingBase 전력 우선순위·WorkTargetPanel 기존 빈 버튼줄·SaveDtos/Buildings/Preflight·검사. 부족하면 순위1~9대로 공급, 기본 냉장8/생산5/전등3(잠정). 건물 상태 저장, 구저장0은 종류별 기본값. 공급 못 한 큰 부하 뒤의 작은 부하도 검토해 잔여 전력 활용.
- **REVIEW100-BOSS-SPEED-1028 / Codex / 진행 중** — 97번 5배속의1.2/1.5초 예고가 실제.24/.3초. 소유 AoE3종·BossTelegraphTiming 신규·기존 게임 설정 명칭·검사. 기존 위협 자동정지 켬에서 범위 예고 시작 시1배속, 끔은 사용자 속도 유지. 기존 SetSpeed 경로 재사용·일시정지 해제 안 함·적/장수/생활을 함께 감속. 예고마다 자동 정지 추가 없음.
- **REVIEW100-ASSESSMENT-1028 / Codex / 진행 중** — 48/49 기존 평가 상세에 상대평가/개인성장 차이와 직무별 별점 사용 기준 명시. 소유 GameMenuAssessment·읽기 UI 검사. CA/PA 숫자는 공개하지 않고 기존 분야별 별점/개인 기술표를 유지. 영입 자동결정·평가점수 변경 없음.
- **REVIEW100-PEACE-1028 / Codex / 진행 중** — 79번 평화 대기 규칙은 유지하되, 창고가 가득 차면 승전 배상금을 못 받아 평화가 거부되는 실제 버그 재현. 소유 DiplomacyManager.MakePeace·GameMenuDiplomacy 오류문구·교역 검사 추가. 기존 수령 StoreResource를 재사용해 초과 배상금을 바닥 보존. 최소전쟁시간·배상수치/상대재고 검증 유지.
- **REVIEW100-FINAL-FRONTS-1028 / Codex / 진행 중** — 81/82 최종방어15일을5일씩 단일/양방향/다방향 접근으로 구분. 소유 FinalAssaultPlan 신규·ColonyInvasion 최종생성 위치·GameMenuCampaign 기존 로켓 상태·검사. 총병력/상한/간격·확정 스냅샷·15일 유지, 저장된 시각/파동에서 단계 재계산. 경로가 검증된 외곽 접근만 사용하고 불가하면 기존 진입점 사용, 기지 안 즉시 생성/병력 중복 없음. 적 구성/새 목표 추가 완료로 표시하지 않음.
- **REVIEW100-START-RISK-1028 / Codex / 진행 중** — 98번1인/침공/사망 조합의 시작 전 안내. 소유 GameMenuCrashStart 기존 상단 부제·시작 버튼 설명·검사. 기존 선택값/실제 난이도 배수와 마지막장수 포로/가혹즉사30% 규칙 표시. 새 확인창·자동 옵션변경·시작차단 없음.
- **REVIEW100-SPECIALTY-1028 / Codex / 진행 중** — 사용자 잠정수치 승인: 실제 기술10 최초 골드20%·20 최초 다이아5%. 현재13계열 연결. 소유 SpecialtyAcquisition/Progress 신규·CommanderAnt 성장 연결·CommanderTraits 후천기록/유전 제외·SaveDto/Catalog/Preflight·GameMenuRoster 기술 툴팁·검사. 평생1회·만석소모·같은계열상위교체·영구유지, 결과/실패사유 저장·알림/기록. 기존/시작기술 도달은 소급추첨 없음. 생성/유전 전문성 후보의 별도 미정 규칙·미존재 전투 전문성은 확정하지 않음.
- **REVIEW100-TOOLTIPS-1028 / Codex / 진행 중** — 새 작물/전문성의 긴 설명이 기존 공통76px 툴팁에서 잘리는 연결 문제. 소유 GameMenuController.Tooltip의 내용별 높이1줄·실제 UI 검사. 글꼴/기본위치/창 사용은 유지.
- **REVIEW100-WINTER-BREAD-1028 / Codex / 진행 중** — 24번 겨울3명/36끼의 기존 납작빵은 밀144·제분/조리1008초로 겨울900초를 넘김. 소유 Kitchen.Recipes 납작빵 잠정밀가루2→1·요리 회귀/옛2재료대기 호환 검사·겨울 계산. 완성품3일·밥/볶음밥·기존2요리 유지. 순수작업720초로 줄여도 생활/배송 제외이므로1명겨울완전자급 보장으로 표시하지 않음.

- **REVIEW100-STORAGE-1029 / Codex / 진행 중** — 27번 반복 필터 설정을 기존 창고 화면 복사/붙여넣기로 단축. 소유 GameMenuStorage·관련 검사. 일반 창고끼리/냉장냉동끼리 허용 목록만 복사하고 재고/우선순위·자동 재배치 규칙 유지. 기존 SetAllowed 반출·신선도 경로 재사용, 퇴비 전용 설정 제외. 화면 캡처/SAVE 없음.

- **REVIEW100-STORAGE-1029 / 기능·저장 검증 완료** — 실제 버튼/78필터/복사 시점/우선순위 불변·쌀7/식량5 반출과70초 신선도 보존·냉장냉동 분류·실제 저장/복원21검사 통과. 전문성은 실제 옛 JSON 필드 제거까지51검사 통과, 기존 요리91·최종 공세 경로 대체/상한22 통과. 최신 빌드 진행 중. 전체100항목·대규모 이전/장기 성능/육안 완료 아님. SAVE/일지/캡처/커밋 없음.

- **REVIEW100-1028 / 후속 기능 검증 완료·빌드 중** — 세금 시간분할21, 왕복교역/평화23, 부모회복13, 작물19/생산96, 연구예산UI8/43노드, 전력우선17, 보스배속17, 상대평가8, 최종공세22/기존C3 43, 시작위험57, 전문성51, 요리91, 겨울계산4, 창고필터21 통과. 각 검사 범위는 docs 개별 검토 문서·100행 상태표 참조(고유 검증 수 합계 아님). Notion 개별 기획/참조의 현재 구현·미정 표기도 맞췄다. 연구소 승급/전체시대연구·미정전문성·10시간/15일 실전·전면육안/장기성능은 잔여. 기존 로컬 변경 보존, SAVE 전용 작업 없음.

- **REVIEW100-1029 / 최신 배포 검증 완료** — Windows build_227d7c36b46e Succeeded·오류0/기존 패키지·셰이더 경고518·142527ms/304777241bytes. 최신 실행 파일 시작/응답 확인, 전후 사용자 JSON3개 해시 변경0·신규0. 전용 로그 인수와 절대 실행 경로로 시험 프로세스만 식별해 종료. 시작 로그에 D3D12 info queue 조회 실패 및 기존 메뉴 NavMesh 경고는 남음(전 플레이 검수 완료 아님). Graphify16696노드/36251간선/804커뮤니티·LLM0. 전문성51·창고21·요리91·최종 공세22 등 위 검사 범위 유지. Play 종료·기존 로컬 파일 보존·스테이징/SAVE/캡처/개발 일지/커밋·push 없음. 카카오 완료 도구 미제공. 100항목 전체/장기·육안 검증은 미완.

- **SAVE-1029 / 검증·문서·캡처 완료** — log/changelog/README 최신 상태, 공식CLI 실제 Game 화면9장 직접 검수/Notion 일지 첨부·Trouble 기록. 격리 검수시설/전문성 예시 상태이며 전체 플레이 검수 아님. 설정/Root/카메라 복원·Play 종료, 최신 build_227d7c36b46e 유지. docs/SAVE_AUDIT_20261029.md·검증 사본 참조. 선택 커밋/push 진행, 기존 로컬 씬/그래픽/설정/임시·prefab 보존.

- **SAVE-1029 / 원격 보존 완료** —155파일 e0a65d3 origin/develop push·원격 동일 SHA 확인. 실행 FBX3/재질24·prefab0, 공식CLI 화면9장 검수/Notion 첨부·일지/Trouble 완료. 기존 씬/Water/그래픽·프로젝트 설정/임시/graphify 보존, Play 종료. 전체100항목/보류 검수 완료 아님. 완료 기록 후속 문서 커밋만 수행.
