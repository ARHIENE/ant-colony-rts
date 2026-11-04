# Codex P3 인계 — 2026-11-01

Q1 후속: 아래 금지 파일 잔여 번역 제안 및 GAMEFEEL 33~40은 Q1에서 기준선 반영분을 확인하고 남은 표시를 보완했다. 현행 판정·검사는 [HANDOFF-CODEX-Q1-1101.md](HANDOFF-CODEX-Q1-1101.md) 참조. 아래 본문은 P3 작업 당시 이력이다.

작업: `E:/Git/ant-wt/P3` / `codex/p3-1101`. 원본 `E:/Git/ant`는 로그·컴파일 참조·외부 예제 소스 읽기만 했다. Unity 실행·에디터 조작·캡처·커밋·push·Notion·개발 일지 작성 없음. 금지 파일 변경 없음. site.Title·파싱용 개체명·도감 키·저장 키·enum 이름·번호 보존.

## 항목별 판정

| 항목 | 판정 | 반영 |
| --- | --- | --- |
| 1단계 한국어화 | 허용 범위 구현, 아래 잔여는 인계 | 건설/로켓/병영/연구/포로/출생/거점 방어 토스트, 배치 실패, 영입 패널, 방어·로켓·원정·작물·특성 이름, 비용·단위, 저장 오류·슬롯 요약 |
| enum 화면 이름 | 구현 | UI/DisplayNames.cs. 기존 CommandCard.RoleName·GameMenuController.CropName·CommanderTraits.DisplayName 재사용. 단축키 키캡·A~D 보스 ID·D위험 기호는 유지 |
| 식별자 보호 | 구현 | MenuLayout 두 번째 인자, CreateButton 영입 버튼 영문 ID, SaveSlots.DisplayName의 Slot/Auto 식별자 유지. 실제 라벨만 한국어 표시. Frame/ScreenName의 영문 상태 키는 변경하지 않음 |
| 2단계 게임성 | 40건 점검 / 32 구현 / 금지 파일8 인계 | docs/GAMEFEEL_1101_P3.md. 기존 힌트·툴팁·안내를 확장. 신규 창·자동 행동·게임 수치 변경 없음 |
| 저장 | 필드·버전 변경 없음 | 저장 실패 사유와 검증 항목 설명만 번역. 구저장의 영어 수송 상태·슬롯 라벨을 표시 시 변환. 구저장 기록 원문을 소급 덮어쓰지 않음 |
| 회귀 기대값 | 수정 | Review1031L1Checks·M1Checks·N2Checks, AcquisitionBuildingChecks, ImplC6Checks, Stage2Checks의 변경된 표시 문구 |

## 수정 파일 범위

경로 기준 `Assets/_Project/Scripts/`:

- Buildings: AirshipYard, Barracks, BuildingConstructionSite, BuildingPlacementController, FarmCropRules, FarmPlot, NurseryChamber, PrisonerCamp, ResearchLab, RuntimeBuildingTemplates, ScienceLab.
- Core: Encyclopedia, GameBootstrap, GameCalendar, GameOptions.
- Data: BuildingData·UnitData의 화면용 기본 이름.
- Save: CommanderMigration, WorkshopMigration, SaveBuildings, SaveCatalog, SaveConstruction, SaveOrders, SaveOrderValidation, SavePreflight·Integration, SaveRuntime, SaveSlots, SaveSnapshot, SaveStorage, SaveSystem, SaveTransportValidation, SaveValidator. 문자열 중심 변경이며 P2 로직과 수동 병합 권장.
- UI: 신규 DisplayNames(+meta), BuildScreen, CommandCard, CommanderAcquisitionPanel, FirstHints, GameMenuArtwork, GameMenuCampaign, GameMenuDiet, GameMenuDuty, GameMenuEnding, GameMenuFront, GameMenuHealth, GameMenuKitchen, GameMenuProcessor, GameMenuResource, GameMenuResearch, GameMenuScienceBuildings, GameMenuStorage, GameMenuSupplies, GameMenuTasks, GameMenuV5, GameMenuWorkshop, HUDController, WorkTargetPanel, WorldMapSymbols.
- Units: CommanderAnt의 기본 화면 이름, CommanderSocial의 특성 변화 문구.
- World: ColonyEvents, DiplomacyArtwork, DiplomacyManager, DiplomacyTrade, EnemyCommander의 기본 화면 이름, ExpeditionSite의 정복 결과 문구, ExpeditionTransport의 중단 문구, SettlementDefense, TransportRoute.
- 검사: 신규 `AgentScripts/Review1101P3Checks.cs`와 위 회귀6개. 문서: P3 HANDOFF/DECISIONS/GAMEFEEL, 구현 보드·README.

## 검사와 남은 검증

- 공식 Editor/CLI를 호출하지 않고 사용자 제공 offc.sh·offcheck.sh의 Roslyn만 사용했다.
- 전체 게임 컴파일: **오류0**. 원본의 `Assets/Planet Earth Free/SpinFree.cs`가 worktree에 빠져 최초 컴파일이 실패했다. 검사 중 동일 파일을 worktree에 임시 복사했으며 패치에서는 제외한다. 원본에는 해당 파일이 있으므로 통합 시 추가하지 않는다.
- 신규 진입점: `public static async Task<string> Main()` / `RunP3Scene()`. enum·자원·특성·버튼·구저장 수송/슬롯·저장 오류·단축키 표시 285검사. 내부 ActionName은 리플렉션, AntColony 타입은 완전수식명.
- 실제 오프라인 실행: **P3 285 / L1 76 / M1 103 / N2 77 통과**. 새 표시 helper와 구저장 표시 값을 검사하며 Unity 네이티브 기능·전체 save/load 성공으로 해석하지 않는다. 원시 컴파일 출력은 `Temp/off/out.txt`, 검사 컴파일 `Temp/off/cout.txt`, 검사 재현 명령은 아래에 기록.
- 신규 검사 및 Review1031L1/M1/N2·AcquisitionBuilding·ImplC6 검사 DLL 컴파일 오류0.
- Stage2Checks는 표시 기대값을 갱신했으나 **기존 Infirmary.CanRegenerate/TryRegenerate API 제거에 따른 CS1061 두 건** 때문에 파일 전체 컴파일 불가. 이 unrelated 의료 로직은 수정하지 않았다. Stage2 성공으로 보고하지 않음.
- Unity/실제 저장 복원/새 게임·기존 게임 전체 화면/Player 빌드는 미실행. OS·Unity가 생성하는 예외 원문, 사용자 이름·과거 기록 원문은 임의 변환하지 않으므로 영어가 포함될 수 있다.

## Claude 화면 확인 요청

1. 원본 최신 상태에 문자열 hunk만 병합. 특히 P1 경제·연구, P2 Save/World 수정은 로직을 덮어쓰지 말 것. 새 DisplayNames.cs·meta도 추가.
2. 금지 파일의 아래 제안 적용. 거점은 원본 `UI/SiteNames.cs`의 `SiteNames.Name(site)`를 사용하고 **도감·세력 연결 키는 원문 유지**.
3. 격리 Play에서 새 게임·기존 저장 각각 `Review1101P3Checks.Main()` 실행. 확인할 화면을 연 다음 `RunP3Scene()` 실행. 이 진입점은 현재 화면의 알려진 영어 누출만 검사하고 화면을 자동 전환하지 않음.
4. 1280×720·1920×1080에서 영입 패널, 병영/장수 강화, 과학 시설, 로켓, 월드맵 거점 방어, 생산 방식·작업표·건강·식단·창고·자원·건설 재료를 확인. 긴 한국어 버튼·최대 이름·실패 사유의 잘림/겹침/툴팁 가림과 월드맵 상태 글자 폭을 육안 확인.
5. 저장 실패·빈 슬롯·손상 파일, 구저장 영어 수송 상태/맵 크기·난이도 표시, 정찰/회유 식량 부족, 건설 완료/재료 도착 알림 확인. 자원·연구·수술·출정·전쟁 규칙 불변 확인.

잠정값: 없음. 카카오톡 완료 알림 도구는 현재 세션에 없어 미발송.

검사 명령(Git Bash, 원본은 참조 읽기만):

```sh
bash C:/TESTTEMP/claude/E--Git/9cd1f401-5158-48e6-8468-2180ba139fd8/scratchpad/offc.sh /e/Git/ant-wt/P3
bash C:/TESTTEMP/claude/E--Git/9cd1f401-5158-48e6-8468-2180ba139fd8/scratchpad/offcheck.sh /e/Git/ant-wt/P3 /e/Git/ant-wt/P3/AgentScripts/Review1101P3Checks.cs
```

오프라인 실행 호스트는 `Temp/P3/run.ps1`(P3·L1·M1·N2)이며 임시 산출물이라 패치에 넣지 않는다. Unity에서는 인계된 cs의 Main을 직접 실행 가능하다.

## 금지 파일의 잔여 번역 제안

아래 줄 번호는 P3 작업 기준이다. 원본 Claude 변경으로 달라질 수 있다. 명시된 화면 문자열만 변경하며 버튼 ID·ScreenName·도감 키·enum·개체 이름은 유지한다.

| 파일:줄 | 남은 문구/표시 | 추천 번역·연결 |
| --- | --- | --- |
| UI/DetailTabs.cs:29 | Food 30 | 식량 30 |
| UI/DetailTabs.cs:54 | Food 30 포상 도움말 | 식량 30, 한 달의 정확한 기준은 현행 냉각값에 맞춰 표시 |
| UI/DetailTabs.cs:115 | e.slot enum | 무기 / 방어구 / 장신구 표시명 |
| UI/GameMenuController.cs:175 | Back 버튼 라벨 | 뒤로 (버튼 ID는 Back 유지) |
| UI/GameMenuController.cs:186 | Continue Colony | 정착지 계속하기 |
| UI/GameMenuController.cs:187 | Load Game | 불러오기 |
| UI/GameMenuController.cs:188 | Restart Same Map | 같은 지도에서 다시 시작 |
| UI/GameMenuController.cs:191 | Restart | 다시 시작 |
| UI/GameMenuController.cs:193 | Back | 뒤로 |
| UI/GameMenuController.cs:196 | New Game | 새 게임 |
| UI/GameMenuController.cs:207 | e.category Ants/World/Bosses | 화면에만 장수/세계/보스, 기존 도감 범주·키 보존 |
| UI/GameMenuController.cs:208 | Back | 뒤로 |
| UI/GameNotifications.cs:21 | skill improved. | 기술이 향상되었습니다. |
| UI/GameNotifications.cs:23 | All commanders can work and fight… | 모든 장수는 작업과 전투를 할 수 있습니다. 장비가 전투 방식을, 지휘 기술이 병력 한도를 정합니다. |
| UI/GameNotifications.cs:36 | Faction/Kind enum, Fixed difficulty | 세력/거점 종류의 한국어 표시명 · 고정 위험도 {값}. site: 키는 원문 유지 |
| UI/GameNotifications.cs:37 | MiniBird / Encountered at… | 미니버드 / {거점}에서 발견. 지면의 공격 예고 범위를 확인하고 공격 전에 피하세요. 구체적인 약점은 아직 발견하지 못했습니다. boss:MiniBird 키 유지 |
| UI/WorldMapPanel.cs:144 | 차량 건조 식50 재료60 | 차량 건조 식량 50 · 재료 60 |
| UI/WorldMapPanel.cs:145 | 비행기 건조 식80 재료100 | 비행기 건조 식량 80 · 재료 100 |
| UI/WorldMapPanel.cs:348 | enum.ToString() fallback | DisplayNames.Expedition(state) 재사용 가능. 현재 정상 enum 4종은 기존 switch가 이미 처리 |
| UI/WorldMapPanel.cs:365 | site.Faction | 화면용 세력명으로 변환. Amber/ Azure/ Jade/ Crimson → 호박/하늘/비취/진홍 세력, Independent/Neutral/Trade/Wildlife → 독립/중립/교역/야생. 내부 ID 불변 |
| Units/CommanderEquipment.cs:92 | weapon.ToString(), Bare mandibles | 기존 EquipmentRecipes의 무기 한국어 이름 재사용 / 맨손. WeaponLabel을 키에 쓰는 Discover 경로도 함께 점검해 기존 키 유지 |

Camera/IsometricCameraController, Core/CrashLandingStart, UI/BetaProgress, GameMenuConscription, GameMenuCrashStart, GameMenuDiplomacy, GameMenuInvestigation, GameMenuManagement, GameMenuRoster, GameMenuScout, GameMenuSurvey, GameMenuWalking, SelectedUnitPanel, ThreatIncidents, ToastManager, WorldMapMarker, WorldPlanet에서는 식별자·단축키·단위·site.Title·파생 무기/세력 이름 외에 새로 번역할 직접 영어 문장은 확인하지 못했다. 기분 원인 키의 파생 노출은 GAMEFEEL 40번 참고.


## site.Title 참조 목록 (변경하지 않음)

표시 부분은 SiteNames.Name(site)로 연결하되, 도감 키·세력 연결·저장 식별자·게임오브젝트 name에 쓰인 부분은 원문을 보존한다. 모든 Title 참조를 기계적으로 교체하지 말 것. Title을 직접 쓰지 않는 Sites.Select(s => s.Title)도 포함한다.

| 파일:줄 | 현재 참조 |
| --- | --- |
| Boss/BossHealth.cs:158 | `CampaignHistory.Record("보스 처치", gameObject.name, GetComponentInParent<AntColony.World.ExpeditionSite>()?.Title ?? "본거지");` |
| UI/BetaProgress.cs:74 | `return $"이동 중 · {Mathf.CeilToInt(ship.Remaining)}초\n{ship.Site.Title} · 도착 후 전장 보기";` |
| UI/BetaProgress.cs:79 | `: $"전투 · {ship.Site.Title}\n전장 보기에서 부대를 지휘하세요";` |
| UI/GameMenuDiplomacy.cs:62 | `L.Button(p, site.Title + " 거래", site.Title + " 거래", x, 624, 170, 40, () => TradeAt(post));` |
| UI/GameMenuDiplomacy.cs:249 | `Item((offer.sites.Contains(i) ? "✓ " : "") + site.Title, () => { if (!offer.sites.Remove(index)) offer.sites.Add(index); rebuildTrade(); });` |
| UI/GameMenuInvestigation.cs:34 | `{ var pick = s; MenuTheme.Button(left, (s == investigationSite ? "선택 · " : "") + s.Title, () => { investigationSite = pick; investigationFocus = null; investigationFoodConfirmed = investigationUnattendedConfirmed = false; ShowInvestigation(); }); }` |
| UI/GameMenuInvestigation.cs:50 | `MenuTheme.Text(right, "대상: " + (investigationSite?.Title ?? "세력 선택 필요") + "\n평가자: " + (investigationEvaluator?.CommanderName ?? "미지정")` |
| UI/GameMenuSurvey.cs:45 | `MenuTheme.Text(right, (layer.map == 0 ? "본거지" : WorldMapManager.Instance.Sites[layer.map - 1].Title) + " · " + survey.Description(layer), 13, 80);` |
| UI/GameMenuWalking.cs:36 | `MenuTheme.Text(content, $"{site.Title} · {AntColony.Core.BiomeRules.Name(site.Biome)} · 도보 편도 {ExpeditionRules.WalkSeconds(site) / AntColony.Core.GameCalendar.SecondsPerDay:0.0}일"` |
| UI/GameNotifications.cs:36 | `Encyclopedia.Discover("World", "site:" + site.Title, site.Title, site.Faction + " / " + site.Kind + ". Fixed difficulty " + site.Difficulty + ".");` |
| UI/GameNotifications.cs:37 | `if (site.Boss != null) Encyclopedia.Discover("Bosses", "boss:MiniBird", "MiniBird", "Encountered at " + site.Title + ". Watch the marked ground attack areas and move away before impact. Specific weakness information is not yet discovered.");` |
| UI/ThreatIncidents.cs:22 | `if ((site.transform.position - position).sqrMagnitude < 70 * 70) return site.Title;` |
| UI/ThreatIncidents.cs:39 | `active["threat:" + site.Title] = site.Title + ": 보스 위협";` |
| UI/WorkTargetPanel.cs:87 | `if (Target is BuildingBase placed && !(Target is Gate)) { var room = RoomSystem.RoomAt(placed.Position); instruction.text = (room == null ? "방 밖(효과 적음)" : $"방: {room.Title} · 등급 {room.GradeName} ({room.Cells.Count}칸)") + " · " + instruction.text; }` |
| UI/WorkTargetPanel.cs:244 | `: $"방: {room.Title} · 등급 {room.GradeName} · {room.Cells.Count}칸") + "\n" + SpaceQuality.Describe(building.Position);` |
| UI/WorldMapMarker.cs:28 | `label.text = $"[{WorldMapSymbols.Symbol(affiliation)}] {site.Title} · {WorldMapSymbols.Risk(site.Difficulty)}{WorldMapSymbols.State(site.Disposition, site.Defense != null && site.Defense.UnderAttack)}";` |
| UI/WorldMapPanel.cs:197 | `? $"<b>자동 수송</b>: {string.Join(" → ", route.Stops.Select(s => s.Title))}\n{route.Status} · 집에서 {TransportRoute.IntervalSeconds:0}초 뒤 반복"` |
| UI/WorldMapPanel.cs:198 | `: routeStops.Count > 0 ? $"<b>경유지</b>: {string.Join(" → ", routeStops.Select(s => s.Title))}\n일꾼 장수를 태우고 자동 수송 시작."` |
| UI/WorldMapPanel.cs:323 | `var text = $"<b>{ship.name}</b> · {(ship.Walking ? "도보" : ship.Aircraft ? "비행기" : "차량")} · {StateName(ship.State)}{(ship.Remaining > 0 ? $" {ship.Remaining:0}초" : "")} → {(ship.Site != null ? ship.Site.Title : "정착지")}\n"` |
| UI/WorldMapPanel.cs:365 | `var text = $"<b><size=16>{site.Title}</size></b>\n{site.Faction} · {kind} · {AntColony.Core.BiomeRules.Name(site.Biome)} · 상태 <b>{state}</b>\n난이도 "` |
| UI/WorldMapPanel.cs:387 | `var marker = L.Button(mapRoot, site.Title, symbol, 0, 0, MarkerSize, MarkerSize, () => selectedSite = site, site.Title, false, 11);` |
| UI/WorldMapPanel.cs:388 | `var tag = L.Label(marker.transform, site.Title, 12, MarkerSize + 4, 0, 160, MarkerSize, MenuTheme.TextColor);` |
| Units/CommanderFormation.cs:79 | `var biome = site != null ? BiomeRules.ForSite(site.Title) : BiomeRules.Current;` |
| World/DiplomacyManager.cs:233 | `if (building != null) { building.TakeDamage(float.MaxValue); CampaignHistory.Record("동맹 지원", c.name, target.Title + " 건물 1동 파괴", true); }` |
| World/DiplomacyManager.cs:249 | `CampaignHistory.Record("침공", c.name, destination?.Title ?? "본거지", true); return true;` |
| World/DiplomacyRaids.cs:75 | `ToastManager.Show($"{c?.name ?? source.Title} 원정대 {count}명 출발 · 본거지 도착까지 {ExpeditionRules.WalkSeconds(source) / GameCalendar.SecondsPerDay:0.0}일", ToastKind.Warning);` |
| World/DiplomacyRaids.cs:88 | `CampaignHistory.Record("원정대 출발", source.Title, "본거지로 이동 중", true);` |
| World/DiplomacyRebels.cs:43 | `CampaignHistory.Record("반란 세력", c.name, site.Title + " 점거", true);` |
| World/DiplomacyTrade.cs:31 | `market = new Civilization { id = id, name = index < 0 ? "교역 캐러밴" : world.Sites[index].Title, contacted = true };` |
| World/ExpeditionTerrain.cs:20 | `var ground = new GameObject(site.Title + " Ground");` |
| World/SettlementDefense.cs:108 | `enemy.name = site.Title + " Invader " + (i + 1);` |
| World/SettlementDefense.cs:147 | `AntColony.UI.ToastManager.Show(site.Title + $": 감시탑 경고 · {Remaining:0}초 뒤 습격.");` |
| World/SettlementDefense.cs:190 | `CampaignHistory.Record("포로", c.CommanderName, site.Title + " 함락");` |
| World/SettlementDefense.cs:220 | `CampaignHistory.Record("구출", c.CommanderName, site.Title);` |
| World/SettlementDefense.cs:238 | `private void Notify(string message) => WorldMapManager.Instance.SettlementNotice = site.Title + ": " + message;` |
