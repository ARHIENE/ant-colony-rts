# L2 인계 — 2026-10-31 재개 완료

`E:\Git\ant-wt\L2` / `codex/l2-1031` 안에서만 수정했다. 종료로 중단된 기존 diff를 보존하고 이어서 검증·보완했다. 원본 `E:\Git\ant`는 Bee 응답 파일·Unity 라이브러리·외부 아트 소스의 읽기 참조에만 사용했다. Unity 조작/실행/캡처, 커밋, Notion 변경, SAVE·개발 일지 작성 없음.

## 항목별 판정

아래 “구현”은 소스 연결 완료이며 실제 Unity 동작 완료를 뜻하지 않는다.

| 항목 | 판정·수정 파일(모두 Scripts 아래, 검사 제외) | 오프라인 검증 / 실행 잔여 |
| --- | --- | --- |
| 100항목77 | 구현. World/AIEraProgress·DiplomacyRules·DiplomacyManager·ExpeditionSite·WildMonster, UI/GameMenuDiplomacy | 1→6시대·1회 결제·비용 부족·무건물 정지/재개·오버플로 검사 / 실전 생산·전투·연구 저장 왕복·장기 경쟁 |
| 바벨 공통 | 구현. Buildings/BuildingConstructionSite.Attribution·AirshipYard.Recovery, Units/CommanderAnt의 중복 기여 제거 | 실제 공통 기여74.9/75%·원시공자 ID·재건 이력·로켓 상태 유효 / 일반 건물·벽·가구와 로켓의 최초 파괴/재건·저장 왕복 |
| 보스 복구 제작 | 구현. Boss/BossProgress, Core/CampaignResearch, Buildings/Workshop·Workshop.Materials·Workshop.Boss, UI/GameMenuBossProgression·GameMenuWorkshop | 배송 요구·결제 후 중복 요구 없음·탐지 재료 분리·진행 일치/복사·완료1회·취소 핵심물1회 반환 / 실제 배송·담당 배정·정지/부재·파괴 후 취소·구저장 이어받기 |
| 호위 XP 예산 | 구현. Boss/BossEncounter·BossProgress, World/WildMonster, Units/CommanderProgression·CommanderPersonal·CommanderAnt·CommanderAdvancedSkills·CommanderCombatDiamonds | 처치·시간 공유200·다른 회차 거부·잔류 시 갱신 금지·실제 재등장만 갱신·손상 예산 거부 / 근접·범위·관통·특수 스킬 실제 처치·성장 |
| 호위 시신 | 구현. World/WildMonster, UI/GameMenuBossProgression | 기존 Wildlife/food0 경로 소스 대조 / 실제 식용 금지·청소·부패·저장 |
| milestones 압축 | 구현. Core/CampaignHistory, UI/GameMenuEvents·GameMenuEnding | 100000→상세1500+집계98500·개수/종류/최초최종 시각·재압축 중복 없음 / 실제 기록·엔딩 표시·JSON 왕복 |
| 현지 장비 전달 | 구현. World/ExpeditionEquipment·EquipmentLoot, UI/GameMenuFieldEquipment·GameMenuEquipment | 기존 슬롯/비행 검증·소유권 동기 이동·도보 초과 롤백 소스 대조, 게임 컴파일 / 실제 전달·교환·화물 수거·도보 한도·비행 해제·저장 소유권 |
| 시민 임시 거주 | 구현. Core/ColonyPopulation·ColonyPopulation.Civilians·ColonyPopulation.Evacuation·ColonyPopulation.Shelter, UI/GameMenuCivicServices·GameMenuPassengerReservations | 임시/영구 중복 거부·이동/도착 저장·손상 주거 목적지 거부 / 전소 후 실제50명 대피·임시 보호·빈자리 예약·입주/집 파괴·민심 회복 |
| 다중 로켓 예약 | 구현. Buildings/AirshipYard·AirshipYard.Civilians·AirshipYard.Rocket·AirshipYard.Reservations, UI/GameMenuCampaign·GameMenuPassengerReservations | 장수/시민 중복·사망/원정·다른 로켓 기탑승·파괴/발사 후 예약·소실 그룹 거부 / 실제 두 발사대 이동·탑승·정원/증원 변동·발사/파괴/취소 |
| 하: 보스 부위 표현 | 규칙 명시. UI/GameMenuBossProgression | 단일HP/비행착지, 장수 신체의료 복제 없음 소스 대조 / 화면 검수 |
| 하: 원정 식량 비교 | 구현. World/ExpeditionSupplies, UI/WorldMapPanel | 식량 부채·일일 소비 올림 검사 / 출발·취소·이동·진입대기·철수·귀환의 실제 비교/화면 |
| 하: 시민 사망 경고 | 구현. UI/GameMenuCrashStart·GameMenuRocket | 시작 설정/가동 안내 소스 대조 / 두 화면의 폭·스크롤·문구 확인 |

## 저장 경로

- 새 게임 C#8개에 `.meta`8개를 기존 `fileFormatVersion: 2`/GUID 형식으로 생성했다. AgentScripts는 Unity Assets 밖 검사 소스다.
- SaveDtos의 MonsterDto: guardVersion/guardBoss/guardCycle/guardXpClaimed → SaveRuntime 캡처/복원. 구저장 버전0은 비호위 기본값, 기존 이름이 보스 호위인 런타임은 복원 시 현재 회차 연결·처치 보상 소급 없음.
- Civilization의 era/eraWork/eraPaid, BossProgress.State.guardRewards, Workshop.Job 보스 주문, AirshipYard.State 예약, CivilianLocation 임시/입주/housingDestination, CampaignHistory.State.summaries는 기존 상태 DTO·캡처/복원 경로 재사용. Workshop.Job.Copy가 신규 주문 필드를 모두 복사한다.
- SaveSnapshot의 기존 campaign/history/diplomacy/population/building 캡처와 SaveRuntime을 사용한다. 신규 항목 때문에 기존 전체 캡처 구현을 복제하지 않았다.
- SaveValidator에서 SaveL2.Normalize를 호출해 era0→1(진행 없는 구저장), 누락 예산/집계/예약 기본값, 구호위 기본값을 연결. 유효한 구기록만 집계한다. 기존 v22의 선택 필드 확장으로 유지했다.
- SavePreflight 및 Integration: 작업량·재료/제작자, SaveL2.Valid의 발사대 간 예약·기탑승·그룹 상태·제작대/전역 진행 일치·호위 회차 검증. 손상 입력을 정상 상태로 덮어쓰지 않는다.
- 주거 입주의 `housingDestination`은 집 좌표, 기존 destination은 NavMesh가 샘플한 실제 이동점이다. 도착 후 집 존재를 다시 확인하며, 집이 사라졌으면 노출 상태로 남긴다.

## 검사·증거

명령: `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/Review1031L2Compile.ps1 -OfflineChecks`

- 원본 Bee rsp의 참조 옵션을 읽어 L2 게임 소스를 전부 재나열해 Unity6000.5.8f1 Roslyn으로 컴파일. 원본에만 있는 외부 아트/demo43소스는 L2 Temp로 복사해 사용(원본 쓰기 없음).
- 게임 DLL·별도 Editor 검사 DLL·오프라인 실행 호스트 **컴파일 오류0**. 기존 API/참조비교 경고32, 오프라인 FormatterServices 경고3은 남는다.
- 실제 L2 게임 DLL로 L2 **97**, K1 **87**, K2 **107**, 보스/원정 **38**, 최종/포로 **31** 검사 통과. 합계360이지만 기능별 검증 범위는 서로 중첩된다.
- 로그: `docs/validation/L2-1031-offline.txt`. rsp·DLL·실행호스트: `Temp/Review1031L2/`. Unity 플레이·Player 빌드 성공 증거가 아니다.
- `Review1031L2Checks.RunScene()` 진입점: 격리된 새 게임 Play에서 실제 Capture→JSON→TryParse→Restore→Preflight 및 AI/보스/집계 비교, finally로 이전 스냅샷 복원. **미실행·실행 잔여**. 실행 시 현재 씬을 재구성하므로 통합 담당자의 격리 장면에서만 호출한다. 위 표의 실제 기능 시나리오는 별도 수동 확인해야 한다.
- `git diff --check` 통과. Graphify AST update 완료(LLM0),12009노드/27954간선/529커뮤니티. 기존 커뮤니티 라벨321개 허브명 대체 경고; 그래프 출력은 게임 병합 필수 파일이 아니다.

## 통합 시 주의

- 시작 시 존재하던 부분 수정까지 포함한 파일별 diff를 확인할 것. 원본/다른 담당 파일을 통째로 덮어쓰지 말 것. L2 수정 대상은 위 표와 아래 목록을 기준으로 병합한다.
- README·100항목77·IMPLEMENTATION_BOARD는 현재 검증 범위를 반영했다. 과거 J2/K1/K2 결정/공백 문서는 당시 기록으로 보존하고 이번 결정/새 공백으로 연결했다.
- 선택·잠정값은 `DECISIONS_1031_L2.md`, 새 설계 공백은 `GAPS_1031_L2.md`. 특히 AI 산업/기술별 구성·예약 증원·원제작자 사망 인계는 후속 공백이다.
- Unity·캡처·Notion·커밋 금지 준수. 카카오톡 완료 알림 도구는 제공되지 않아 발송하지 못했다. 자동 전쟁/자동 출정 추가 없음. 유체 유출·전면 그래픽·대규모 성능 제외.

## 게임/검사 수정 파일 전체
- `AgentScripts/Review1029CampaignRuntimeChecks.cs`
- `AgentScripts/Review1031L2Checks.cs`
- `AgentScripts/Review1031L2Compile.ps1`
- `Assets/_Project/Scripts/Boss/BossEncounter.cs`
- `Assets/_Project/Scripts/Boss/BossProgress.cs`
- `Assets/_Project/Scripts/Buildings/AirshipYard.Civilians.cs`
- `Assets/_Project/Scripts/Buildings/AirshipYard.cs`
- `Assets/_Project/Scripts/Buildings/AirshipYard.Recovery.cs`
- `Assets/_Project/Scripts/Buildings/AirshipYard.Reservations.cs`
- `Assets/_Project/Scripts/Buildings/AirshipYard.Reservations.cs.meta`
- `Assets/_Project/Scripts/Buildings/AirshipYard.Rocket.cs`
- `Assets/_Project/Scripts/Buildings/BuildingConstructionSite.Attribution.cs`
- `Assets/_Project/Scripts/Buildings/Workshop.Boss.cs`
- `Assets/_Project/Scripts/Buildings/Workshop.Boss.cs.meta`
- `Assets/_Project/Scripts/Buildings/Workshop.cs`
- `Assets/_Project/Scripts/Buildings/Workshop.Materials.cs`
- `Assets/_Project/Scripts/Core/CampaignHistory.cs`
- `Assets/_Project/Scripts/Core/CampaignResearch.cs`
- `Assets/_Project/Scripts/Core/ColonyPopulation.Civilians.cs`
- `Assets/_Project/Scripts/Core/ColonyPopulation.cs`
- `Assets/_Project/Scripts/Core/ColonyPopulation.Evacuation.cs`
- `Assets/_Project/Scripts/Core/ColonyPopulation.Shelter.cs`
- `Assets/_Project/Scripts/Core/ColonyPopulation.Shelter.cs.meta`
- `Assets/_Project/Scripts/Save/SaveDtos.cs`
- `Assets/_Project/Scripts/Save/SaveL2.cs`
- `Assets/_Project/Scripts/Save/SaveL2.cs.meta`
- `Assets/_Project/Scripts/Save/SavePreflight.cs`
- `Assets/_Project/Scripts/Save/SavePreflight.Integration.cs`
- `Assets/_Project/Scripts/Save/SaveRuntime.cs`
- `Assets/_Project/Scripts/Save/SaveSnapshot.cs`
- `Assets/_Project/Scripts/Save/SaveValidator.cs`
- `Assets/_Project/Scripts/UI/GameMenuBossProgression.cs`
- `Assets/_Project/Scripts/UI/GameMenuCampaign.cs`
- `Assets/_Project/Scripts/UI/GameMenuCivicServices.cs`
- `Assets/_Project/Scripts/UI/GameMenuCrashStart.cs`
- `Assets/_Project/Scripts/UI/GameMenuDiplomacy.cs`
- `Assets/_Project/Scripts/UI/GameMenuEnding.cs`
- `Assets/_Project/Scripts/UI/GameMenuEquipment.cs`
- `Assets/_Project/Scripts/UI/GameMenuEvents.cs`
- `Assets/_Project/Scripts/UI/GameMenuFieldEquipment.cs`
- `Assets/_Project/Scripts/UI/GameMenuFieldEquipment.cs.meta`
- `Assets/_Project/Scripts/UI/GameMenuPassengerReservations.cs`
- `Assets/_Project/Scripts/UI/GameMenuPassengerReservations.cs.meta`
- `Assets/_Project/Scripts/UI/GameMenuRocket.cs`
- `Assets/_Project/Scripts/UI/GameMenuWorkshop.cs`
- `Assets/_Project/Scripts/UI/WorldMapPanel.cs`
- `Assets/_Project/Scripts/Units/CommanderAdvancedSkills.cs`
- `Assets/_Project/Scripts/Units/CommanderAnt.cs`
- `Assets/_Project/Scripts/Units/CommanderCombatDiamonds.cs`
- `Assets/_Project/Scripts/Units/CommanderPersonal.cs`
- `Assets/_Project/Scripts/Units/CommanderProgression.cs`
- `Assets/_Project/Scripts/World/AIEraProgress.cs`
- `Assets/_Project/Scripts/World/AIEraProgress.cs.meta`
- `Assets/_Project/Scripts/World/DiplomacyManager.cs`
- `Assets/_Project/Scripts/World/DiplomacyRules.cs`
- `Assets/_Project/Scripts/World/EquipmentLoot.cs`
- `Assets/_Project/Scripts/World/ExpeditionEquipment.cs`
- `Assets/_Project/Scripts/World/ExpeditionEquipment.cs.meta`
- `Assets/_Project/Scripts/World/ExpeditionSite.cs`
- `Assets/_Project/Scripts/World/ExpeditionSupplies.cs`
- `Assets/_Project/Scripts/World/WildMonster.cs`
