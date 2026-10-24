# IMPL-B8 인계

## 상태
- 재개 작업(2026-10-24): 기존 diff/인계를 대조하여 미완료였던 학당·진료소 건설/저장 등록과 대피 잔여 피해 보존을 보완. 기존 경제 계산·UI·검사는 재작성하지 않음. **컴파일·Unity·RunChecks·화면·실제 파일 save/load 모두 미실행**. 실행 통과로 해석하지 말 것.
- 이 worktree에서만 작업. 기존 다른 묶음의 staged 변경 보존. 금지된 Units/Boss/CampaignResearch/Expedition/침공/협상/로켓/AirshipYard/FarmPlot/Workshop/Processor는 수정하지 않음.
- Unity 실행/MCP/Notion/prefab 수정/git commit·stash·push/SAVE·일지·캡처 없음. 카카오 완료 알림 도구가 이 세션에는 없음.

## G별 결과
| 항목 | 상태 | 구현 및 남은 부분 |
|---|---|---|
| G16 | 부분 | 주거·노출·대피 위치 집계, 보호 상실 자체 사망 없음, 피해 10당 1명/잔여 피해 JSON 보존, 같은 위치 기존 Corpse 수량 묶음 재사용. 최대 50명 NavMesh 실제 이동/그룹 마커, 도착 생존자만 반출. 전투 피해·대피 명령·로켓 좌석 호출 연결 필요. |
| G17 | 코드 완료·실행 미검증 | 기본 세율 20% 유지, 500명당 하루 1업무 선형 소모, 첫 500명 기본 서비스+유효 시설당 500명, 충족률을 기존 이주 D에 곱함. 빈 주거 생산력 분모 0은 1로 처리. |
| G65 | 코드 완료·실행 미검증 | CivicService School: 어린 20명/동, 성인 전환 시 교육 충족률×10% 생산·×5 민심, 1계절 후 만료. 하루 식량 .3/흙 .15 소수 비용 보존·정수 정산. 마을 탭 건설/저장 카탈로그 연결 완료. 건설 수치 잠정. |
| G66 | 코드 완료·실행 미검증 | CivicService Clinic: 시민 50명/동 의료 충족률, 민심 최대5, 기존 이주 D에 의료 충족률×.5 보정, 운영 중 시설은 공공서비스에도 포함. 하루 기초약 .15/식량 .3. 질병 생성 없음. 의료 탭 건설/저장 카탈로그 연결 완료. 건설 수치 잠정. |
| G68 | 부분 | 기존 시간별 미동원 성인/노년 세금 누적과 일일 납세 재사용. 15일=2250초/37.5분·납세15회 상수, 장수/병력 식량20일분·방어시설 재건비30% 경고 화면 구현. 연료/탄약 실제 요구량과 로켓 가동 버튼은 연결 요청. |

## 수정 파일
기존:
- `Assets/_Project/Scripts/Core/ColonyPopulation.cs`: 추가 State 필드/복원, 서비스·의료 이주 보정, 교육 과세, 틱·일일 운영비, 민심 연결.
- `Assets/_Project/Scripts/Core/ColonyPopulation.Admin.cs`: 하루 행정 수요·소모, 빈 주거 생산력 처리.
- `Assets/_Project/Scripts/Core/ColonyPopulation.Ages.cs`: `AgeYear()`의 어린→성인 전환 직전에 교육 적용 1줄. 기존 연령 이관/동원 처리 유지.
- `Assets/_Project/Scripts/Save/SaveValidator.cs`: `ColonyPopulation.ValidCivicState` 추가 검증 1줄.
- 재개 시 추가 수정: `Data/BuildingData.cs` 끝에 `CivicPublicService/School/CitizenClinic` 추가, `Buildings/BuildingPlacementController.GetTemplate`에서 신규 CivicService 템플릿 연결, `Save/SaveCatalog.Kind`에서 종류별 이름 반환, `UI/BuildScreen`의 마을/의료 탭에 학당/시민 진료소 추가. `RuntimeBuildingTemplates`와 `SaveBuildings/SaveDtos/SaveSnapshot`에는 추가 수정 불필요.
- `Assets/_Project/Scripts/UI/GameMenuPopulation.cs`: 이주 설명 최신화, 공공서비스·대피 진입 버튼.
- `README.md`, `docs/IMPLEMENTATION_BOARD.md`, 이 문서.

신규:
- `Assets/_Project/Scripts/Buildings/CivicService.cs`: 공공서비스/학당/시민 진료소 종류와 활성 시설 집계, 종류별 런타임 템플릿/저장 이름.
- `Assets/_Project/Scripts/Core/ColonyPopulation.Economy.cs`: 충족률·교육·소수 운영비·상태 검증.
- `Assets/_Project/Scripts/Core/ColonyPopulation.Civilians.cs`: 시민 위치 집계·피해·수량 시신·상태 검증.
- `Assets/_Project/Scripts/Core/ColonyPopulation.Evacuation.cs`: 그룹 분리·NavMesh 이동·도착 반출·런타임 그룹 마커. 재개 시 전원 출발의 잔여 피해를 새 그룹에 이관하고 전원 반출 시 빈 그룹 피해 정리.
- `Assets/_Project/Scripts/Core/FinalBattleEconomy.cs`: 15일 방어/20일 비축/재건비 계산과 경고 문구.
- `Assets/_Project/Scripts/UI/GameMenuCivicServices.cs`: 서비스·지점 목록·최종전 경고.
- `AgentScripts/ImplB8Checks.cs`: RunChecks 진입점.
- graphify update가 만든 `graphify-out/` 갱신·캐시·백업은 소스 패치/커밋에서 제외할 것.

## 저장
- 버전 올리지 않음. `SaveDtos.SaveFileV1.population`은 이미 `ColonyPopulation.State` 타입이며 `SaveSnapshot.Capture()`가 `Capture()`, 복원이 `Restore()`를 호출하므로 추가 교육·비용·중단·시민 위치·목적지·잔여 피해가 기존 JSON에 포함됨. SaveDtos/SaveSnapshot 중복 필드는 추가하지 않음.
- 구저장: 교육/비용/중단 기본 0/false, null 시민 목록은 복원에서 생성. 다음 지점 ID 최소1. NavMesh 경로·그룹 마커는 저장하지 않고 틱에서 재생성.
- 시신은 기존 `SaveSnapshot.corpses`/`Corpse.Capture()` 경로 그대로 사용. 시민 전용 중복 시신 DTO 없음.
- CivicService 시설도 `SaveCatalog.Kind`가 `School/CitizenClinic/CivicPublicService`를 반환하여 기존 `SaveBuildings.Resolve → GetTemplate → Instantiate` 경로에서 동일 Kind로 재생성한다. 전용 상태가 없으므로 별도 DTO 필드/저장 버전 증가는 없음. `SavePreflight.ForScene`은 기존 Enum 파싱·템플릿 종류/체력 검사를 재사용한다. 실제 파일 왕복은 미검증.

## 연결 요청 (Claude 통합 담당)
1. 건설/저장 담당(코드 연결 완료, 통합 확인):
   - 공용 파일 충돌 시 `Data/BuildingData.cs` enum 끝의 신규 3종, `Buildings/BuildingPlacementController.GetTemplate` switch 1항목, `Save/SaveCatalog.Kind` 1줄, `UI/BuildScreen` 탭 항목 2개를 보존. 다른 묶음도 enum 끝에 추가했다면 통합 시 번호 충돌 확인.
   - 건설 수치는 사양에 없어 **기존 BuildingData 기본값(식량0/흙20/특수0/시민5/3초/체력300), 연구 제한 없음**을 잠정 재사용. 확인 질문에 응답이 없어 적용한 가정이며 확정 기획 아님. 전용 밸런스 확정 시 CivicService.CreateTemplate에서 변경. 별도 일반 공공서비스는 저장/API용 종류만 등록하고 건설 메뉴에는 추가하지 않음.
   - 새 .cs.meta는 Unity 재개 시 생성 확인. prefab 수정/커밋 금지 유지.
2. 전투/침공 담당:
   - 현 소스의 실제 공격 지점은 `Assets/_Project/Scripts/World/WildMonster.cs::Update()`의 대상 재탐색과 `currentTarget.TakeDamage(attackDamage)` 분기. `ColonyInvasion.Spawn/SpawnCommander`, `LocalIncursions.Spawn`은 공격자 생성만 담당한다. 침공 담당은 WildMonster.Update에 시민 위치 선택/접근/공격 분기를 추가하고 `ColonyPopulation.SyncCivilianLocations()` 후 `CivilianLocations`의 노출 지점을 골라 `DamageCivilians(location.id, attackDamage)`를 공격 주기당 한 번 호출할 것. 보호 중 주거에는 건물 피해만 적용하고 파괴 자체 인구 즉사 없이 다음 공격부터 노출 시민을 타격한다.
   - 주거 파괴 때 기존 인구 즉사/중복 Corpse 생성이 있다면 제거. 집이 비활성/파괴된 다음 Sync에서 보호만 잃음. `SetCivilianProtection(id, bool)`은 비주거 대피 지점 보호 연결용(주거 보호는 실제 Housing 생존 여부가 결정).
   - 대피 명령 UI 연결 지점은 `Assets/_Project/Scripts/UI/GameMenuCivicServices.cs::CivicServices()`의 지점 목록. 통합 입력 담당은 지점 ID/월드 목적지 선택 후 `BeginCivilianEvacuation(sourceId, destination, requested)`를 반복 호출하여 분리(현재 목록/상태 표시는 있으나 목적지 선택 명령은 미연결). 반환 0은 출발 불가. 각 성공 그룹 최대50명, NavMesh 완전 경로 필요. 이동 중 피해도 해당 group.id로 호출.
3. 로켓/AirshipYard 담당:
   - `Buildings/AirshipYard.TryBoard`의 시민 대응 경로/가동 상태 담당 API에서 **도착 전 인구를 미리 탑승시키지 말 것**. `TakeArrivedCivilians(groupId, freeSeats)` 반환값만 로켓 시민 승객에 더함. 호출은 좌석 승인 후 같은 틱에 한 번 수행. 이 API는 반환 인구를 시민 풀에서 제거하며 사망으로 기록하지 않음.
   - 대피소 도착은 `CivilianLocation.arrived`로 확인. `TakeArrivedCivilians`는 탑승/정착지 반출에만 사용. 일반 안전 대피소에 도착했으면 시민 수를 유지하고 `SetCivilianProtection`으로 보호만 부여.
   - `Assets/_Project/Scripts/UI/GameMenuCampaign.cs::RocketSection(CampaignResearch)`에 G20/G52 가동 버튼을 추가할 때 그 직전에 `GameMenuController.FinalBattleReserveWarning(activateAction, fuelAndAmmo)` 호출. **현 worktree의 AirshipYard에는 TryActivate가 없고 TryBuild/TryBoard/TryDepart만 있으므로 존재하는 가동 API로 오인하지 말 것**. activateAction에는 로켓 담당이 구현한 15일 가동 API를 전달하며 기존 즉시 발사 TryDepart로 대체하지 않는다. 경고 자체는 비축 부족을 차단하지 않음.
   - `fuelAndAmmo`에는 실제 자원별 필요한 총량을 `ReserveRequirement`로 전달. null은 ‘연결 대기’, 빈 배열은 필요량0을 확인했다는 뜻. 로켓15일 연료 전량 적재/G52 판정은 로켓 담당에서 수행.
   - 승객 시민 수의 저장·파괴/하차/발사/엔딩 분류도 로켓 담당 소유. 반출만 연결하고 승객 저장을 빠뜨리면 인구가 유실됨.
4. B1/통합 회귀 담당:
   - 기존 Military partial의 `S.old-S.agingDue` 가용 노년 보정 요청(B2 인계)은 여전히 미해결이며 이 묶음에서는 건드리지 않았음.
   - 행정 수요가 500명당 하루1로 바뀌어 옛 AdminWorkPerAnt=6/지수감소를 가정한 검사는 갱신 필요. 원래 상수는 다른 파일 소유이므로 제거하지 않음.

## 검사 목록·검증 결과
- `AgentScripts/ImplB8Checks.cs`: `public static async Task<string> Main()`, 실패 시 예외, 성공 `PASS n`.
- Play 모드에서 `RunChecks`로 실행. 1명 불시착 새 게임으로 초기화하고 검사 안에서 비축더미·창고·주거·공공서비스·학당·진료소·운영 물자·별도 NavMesh 패치를 직접 만듦. 기존 Stockpile 존재를 가정하지 않음. 자동저장 비활성/임시 RootOverride·설정/난수 복원.
- G17 기본 세율/빈 인구/500·1000명 서비스와 행정/서비스 비활성/이주 곱셈.
- G65 성인 전환/교육10%·민심5/1계절 만료/JSON. G66 50명당 의료·민심5/운영비 부족 중단·재납부 재개.
- 일일 .3/.15 소수 비용과 7일 누적 정산. 동원 변경 전후 시간별 과세·15번 일일 납세와 누적 생산량 보존.
- G16 보호·주거 상실 무사망/9+1 피해/20 피해/위치별 시신 수량/NaN·무한대 차단/최대50 출발/도착 전 탑승 차단/실제 위치 이동/이동 중 사망/이동 상태 JSON 복원/좌석 제한·중복 반출 방지/인구 보존.
- 재개 시 추가: School/Clinic/PublicService 각각 `GetTemplate`, 건설 잠금 없음, 생성 Kind, 실제 `SaveBuildings.Capture` DTO JSON 왕복, `Resolve/Restore` 종류·체력 복원 검사. 전원 대피 전 9 피해 → 이동 그룹 JSON 왕복 후 1 피해 시 1명 사망, 일부 출발 시 잔여 피해 유지 검사. 실제 파일 저장/로드 검사가 아님.
- 구저장 null 필드·잘못된 위치/타이머 검증.
- 프로젝트 참조 심볼을 rg로 확인. `git diff --check` 통과. `graphify update .` AST 갱신 성공(별도 실행 검증 아님).
- 재개 후에도 diff 공백 검사/추가 참조 심볼 확인 및 graphify AST 갱신 완료(8623 nodes / 19472 edges). 커뮤니티 수 변경으로 일부 라벨이 허브 이름으로 자동 대체되었다는 graphify 안내가 있었으며 LLM 재라벨은 수행하지 않음. graphify 출력은 소스 전달 대상에서 제외.
- **모든 위 검사는 작성만 했고 실행하지 않았다. PASS 숫자/컴파일 성공/화면 검증/실제 save-load 성공을 보고하지 말 것.**

## 남은 위험·해석
- 교육 효과는 성인 전환 순간의 학당/어린 인구 충족률을 정착지 공통 보정으로 1계절 적용한다. 교육받은 성인 개별 코호트를 장기 추적하지 않음.
- 학당·진료소는 기존 기본 Cube 템플릿 표현만 사용하며 전용 모델/아이콘은 없음. 건설 수치는 위 잠정 기본값이며 확정된 밸런스로 해석하지 말 것.
- 학당의 ‘재료’ 비용은 기존 기본 재료 Soil로 연결. 공공서비스는 기존 Facilities(장수 의무실/휴게/장식/오락/식당) + CivicService를 합산하며, 전문 진료소의 의료50명과 일반 서비스500명을 별도 계산함.
- 소수 비용은 누적 정수 결제이며 정수 미납 시 해당 종류 시설 전체가 다음 납세 정산까지 중단. 물량 부족에 따른 시설별 부분 가동 정책은 사양에 없어 추가하지 않음.
- 시민 연령은 지점별로 분해하지 않고 전체 집계에서 어린→대기 성인→미동원 노년 순으로 차감. 동원/예약/탈주로 시민 합계가 바뀌면 Sync가 비이동 지점부터 조정. **대피 중 인구를 기존 동원 시스템에서 잠그는 기능은 없음**. 동원 partial 변경 없이 보존한 한계이므로 실제 플레이 회귀에서 확인 필요.
- 이동 속도3m/s·그룹 캡슐 마커는 최소 표시. 먼 지형에서 NavMesh 부분 경로는 출발 거부/이동 대기하며 순간이동하지 않음. 기존 NavRoute의 장거리 경유지 처리까지는 연결하지 않았고, 이동 그룹당 매 틱 경로 재계산하므로 대규모 대피 성능 검증 필요.
- 비축 UI는 현재 본거지 장수/소속 병력의 기존 ExpeditionRules.FoodPerDay와 창고/개인 보존식을 사용. 별도 원정·화물·로켓 내 재고는 해당 소유 담당이 추가 집계해야 함. 주요 방어시설은 분사탑·범위탑·흙벽·성벽·성문·함정·매설지·감시탑의 현재 BuildingData 비용×30%로 계산.
- UI 목록 스크롤/버튼은 기존 MenuLayout/MenuTheme·DESIGN_RULES 재사용. 실제 1920×1080/1280×720 화면·동작은 미확인이며 SAVE 전용 캡처 규칙으로 캡처하지 않음.
