# P1 인계 · 2026-11-01

`E:/Git/ant-wt/P1` / `codex/p1-1101`에서만 수정. 원본 develop 2ad0449는 Bee 참조·외부 소스 읽기만 했다. Unity 실행/조작/캡처·커밋·push·SAVE·Notion·prefab 생성 없음. P2 금지 파일 수정 없음.

## 판정

| 요청 | 판정 | 수정/확인 파일 | 검사·잔여 |
| --- | --- | --- | --- |
| L1 실물/전용 시대 승급 | M1 통합본에 이미 구현. 차량/비행기 반복 관문 없음 확인 | Core/EraPromotionResearch, Buildings/ScienceLabUpgrades | 현재등급에서 선행 전부 완료 가능·기존49연구 순환 검사. M1 103·L1 76 재통과 |
| M1 성휘 공급 | N1 최소161→80×3층 재사용. 치환 후 필수 산업 입력까지 보완 | World/StellarSupply | 신규 본거지 성휘≥240·티타늄≥320·원유/가스≥600. 7바이옴×3반경×128시드, ID·12층/빈4·저장 유효성·멱등 검사. 기존 저장 충전 없음 |
| 생산 밸런스 | 실제 하위 제조법까지 계산. 고급 배합/승급 비용 유지, 일반 부품 직접 생산 추가 | Buildings/Processor, Processor.Planning(신규) | 부품12번 추가로 기존0~11 보존. 전체 승급 작업21540=359분. 희귀 원료·시설 수지는 결정 문서. 장기 플레이는 미실행 |
| 빈3·5시대 | 경제/생활 연구4종 재배치 | Core/CampaignResearch | 고급 작물/치수3, 보온/부위 재생5. ID/완료/기존 지불액 유지. 진행·보류는 새 작업 목표까지 계속. 군사/AI 전체 재배치는 P2 요청 |
| 노년 병역 | 집계 모델 유지·화면 이중 집계 수정·실동원 진입점 준비 | UI/GameMenuPopulation, Review1101P1Checks.RunP1Scene | Main 순수 군역·자연사 검사. RunP1Scene 편입5→귀환3→전사2 검사 **미실행**. 과거 성체화된 노년 소급 추정 불가 |
| 비전투 다이아8종 | 기존 실제 호출 경로 확인. 효과 누락 없음 | 아래 연결표·Review1101P1Checks | 컴파일된 실제 DLL의 IL 호출 간선+75%/단독 담당/확률/운반 묶음 경계 검사. K1 87·N1 25742 재통과. 실장면 효과·저장 복원은 별도 |
| 건물 모델 |135매핑 파일 존재·Hut 초가지붕 메시·축 확인. 잘못된 매핑 확정 근거 없음 | AgentScripts/Inspect1101P1Models.cjs, MODELS_1101_P1.md, validation JSON | 모델/축 변경 없음. 사용자 외형/뒤집힘 지적은 **미해결·미재현**, Claude 실화면 확인 필요 |
| 2단계 게임성 |32건 정리. 상·중25건 중23건 구현,2건 실행/원인 확인 유보. 하7건 후속 | GAMEFEEL_1101_P1.md, 기존 과학/가공/인구 UI | 표본 연구 정지/배정/경험치, 연구소 상태·속도/ETA·부족액, 생산 묶음/원료/시간, 성체·수요 표시 등. 실화면 미확인 |

## 특성 연결 추적

| 특성 | 실제 호출 경로 | P1 검증 |
| --- | --- | --- |
| 바벨 | WorkerAnt 건설→BuildingConstructionSite.ApplyWork→CompleteAttribution→BuildingBase.Die/LeaveBabelFoundation. 로켓은 공통 기여→AirshipYard.Recovery | 공통 기여75% 경계·기존 K1/N1. 로켓은 별도 최초 복구/부품 재준비 정책 유지 |
| 낙토 | ResourceNode 파종/채집·FarmPlot 성장→NoteFarmer. Blessed→ColonyEvents.GrowthMultiplier/ResourceNode·FarmPlot | 단독/혼합 이력·실제 호출 간선. 자동 고정 담당 보호는 N1 유지 |
| 태공 | WorkerAnt 실제 어획→ResourceNode.FishingTreasure |5% 경계·시대별 보물은 K1 회귀. 미래 물자 자동 지급 없음 |
| 천기 | CampaignResearch.Tick→CreditResearch, CompleteResearch→GrantInsight, TryStart→ApplyInsight | 기여75%·직접 후속 통찰·기존 K1. 통찰 후 연구 자동 선택 없음 |
| 식선 | Kitchen.Work→SoleWorker.Note→FoodLots.inspired→운반/식사 출처→CommanderAnt.ApplyDiamondMeal | inspired 묶음 이동·단독 이력·K1 기분 비중첩 검사 |
| 화룡 | 건설/예술 시공→Artist.Note→CompleteAttribution→DiamondArtwork. GameMenuArtwork 직접선물→DiplomacyManager.OfferArtwork | 실제 UI 콜백의 컴파일러 생성 중첩 타입까지 호출 추적. +30%p·기존15일 정책 |
| 여래장 | CampaignResearch.TickPoliticalPresence→정착지 계수→ColonyPopulation 민심 하락/CommanderPersonalState.AddMood | 시민/장수 실제 호출·2인 계수0.55·기존 K1 신규 불만/잔여3일 |
| 하멜른 | CommanderHusbandry/Critter→NoteTamer→HamelinProtected→CritterBehaviour·Critter·WildMonster 선동/공격/도주 처리 | 실제 길들임/보호 호출 확인. WildMonster는 읽기만 함(P2 소유) |

## 검사와 한계

`powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/Review1101P1Compile.ps1 -OfflineChecks`

- 게임 Roslyn 오류0, 검사 Roslyn 오류0, 오프라인 실행 호스트 오류0.
- P1 **21584**, M1 **103**, L1 **76**, K1 **87**, N1 **25742** 통과. 같은 동작을 중첩 검사하므로 단일 기능 커버리지 합계로 해석하지 않는다.
- 지정 offc.sh는 worktree에 없는 외부 art/demo43소스를 생략하여 SpinFree 참조 오류가 났다. P1Compile은 **P1 Temp/off/a.rsp만** 보완하여 해당 외부 원본 소스를 읽기 참조한 뒤 같은6000.5.8f1 Roslyn으로 오류0을 확인한다. 원본 게임 코드/파일 수정 없음. offcheck.sh로 검사도 오류0.
- 오프라인 IL 추적에서 처음에는 UnityEngine.UIModule 의존성과 컴파일러 생성 UI 콜백을 빠뜨려 실패했다. 실행 폴더에 설치된 managed 모듈을 복사하고 중첩 메서드까지 추적한 최종 검사에서8종 모두 통과했다. Unity 엔진 네이티브 실행으로 해석하지 않는다.
- 검증 요약 `docs/validation/P1-1101-offline.txt`. 재실행 산출물은 P1 `Temp/off/`에만 생성. 기존 CS0618/CS0252 및 테스트 FormatterServices obsolete 경고 유지.
- `public static async Task<string> Main()` / `RunP1Scene()` 제공. internal API는 리플렉션 사용, 게임 타입은 AntColony 전체 이름으로 지정.
- RunP1Scene은 시작 완료 격리 Play에서 실제 Snapshot→JSON→Validator/Preflight, 표본 정지, 노년 편입/귀환/전사 검사. 변형한 연구/인구/풀/통계는 finally로 복구. 전체 씬 복원·실물 제조 완료·UI 확인 검사는 아니다. **P1은 실행하지 않았다.**

## Claude 실행/화면 요청

1. P1 패치 적용 뒤 Unity 재컴파일→`Review1101P1Checks.Main()`→시작 완료 격리 장면에서 `RunP1Scene()`. 기존 M1 RunScene, SaveRoundtripChecks도 실행. 새2스크립트 meta 포함.
2. 제련로의 기존0~11 주문·진행/보류를 저장24에서 복원하고12번 부품 수동 주문. 원료 철/구리 배송→작업→바닥 출력→운반, 전자부품/금속/합금 같은 흐름. 전력·가스 단절/재공급·정지/취소·가득 참에서 비용/재료 중복 없는지 확인.
3. 새 게임의 성휘/티타늄/가스/원유 **실제 지형**에서 탐사기 배치·이동/전력·발견→표본 반입/연구→채굴기 배치·채굴·배관을 확인. 최소 수량은 원료를 다른 용도로 전부 써도 보충한다는 정책이 아니다. 기존 저장의 층/잔량은 유지되어야 한다.
4. 표본만 진행할 때 연구 담당 지정→XP 증가→정지/재개. 생활/잠·작업표 금지·먼 거리·담당 연결 변경/복귀와 ETA0/참여 상태가 일치하는지 확인. 일반 연구 완료 직전 큰 delta·복수 연구소에도 초과 XP가 없어야 한다.
5. 1→6 실물 승급·재료 부족 전량 미차감. 재배치4연구 기존 완료 유지·진행 보존·추가 비용 없음·새 게임 시대 조건 확인. 3·5시대 군사 연구/AI는 P2 담당과 합의 필요.
6.1920×1080/1280×720/축소창에서 과학의49노드·표본 정지 버튼·연구소 배정/속도·승급 재료 줄, 가공의13번째 부품·긴 전자/성휘 이름·원료/시간 툴팁, 인구의 성체/노년·수요200 게이지 확인. 새 창 없음. 화면을 보지 않았으므로 넘침 해결 완료로 표시하지 않는다.
7. Hut/House를 실제 완공·회전하고 Data.kind/Resources 모델/루트·메시 회전/손실 스케일을 확인. 상세 체크리스트는 MODELS 문서. P1 정적 점검만으로 사용자 지적을 해결 처리하지 말 것.
8. PACING_1029의10시간/대기20%·시대 목표는 실측 잔여. 공급/경제 선택을 검증한 뒤 필요하면 결정 문서의 잠정 제조 수치만 조정한다. 자동 제조법/자동 채집/전쟁·수술·발사 결정은 추가하지 않음.

## 수정 파일

- 게임: `Buildings/Processor.cs`, 신규 `Buildings/Processor.Planning.cs`+meta, `Buildings/ScienceLabUpgrades.cs`; `Core/CampaignResearch.cs`, `Core/CampaignResearch.Resources.cs`, 신규 `Core/CampaignResearch.Planning.cs`+meta; `World/StellarSupply.cs`.
- UI: `GameMenuResearch.cs`, `GameMenuProcessor.cs`, `GameMenuPopulation.cs`만 최소 수정.
- 검사: 신규 `AgentScripts/Review1101P1Checks.cs`, `Review1101P1Compile.ps1`, `Inspect1101P1Models.cjs`.
- 문서: 신규 결정·게임성·모델·인계·validation; README/IMPLEMENTATION_BOARD/OPERATIONS_100_STATUS/PACING_1029/GAPS_1031_L1·M1에 현재 후속 연결. log.md/changelog.md 변경 없음.
- SaveDtos/SaveSnapshot/SavePreflight는 새 필드가 없어서 수정 없이 기존 중첩 상태 경로 검증. SaveRuntime·P2 World/Boss/AI 파일 수정 없음. 신규 데이터나 prefab 없음.
- Graphify 생성물은 게임 통합 파일과 별도 취급. 갱신 결과/패치 위치는 아래 최종 항목에 기록한다.

카카오톡 완료 도구가 제공되지 않아 알림을 발송할 수 없다. 사용자 요청의 개발 일지·Notion·Unity 캡처는 SAVE 단계가 아니므로 수행하지 않았다.

## 최종 산출물·환경 제한

- 최종 게임/검사/호스트 컴파일과 P1/M1/L1/K1/N1 회귀 전부 통과. 원시 콘솔 출력은 `docs/validation/P1-1101-console.txt`(공용 offc의 외부 SpinFree 누락 실패와 보충 후 성공이 함께 기록됨). `git diff --check` 통과.
- Graphify AST 첫 갱신은 **12339노드·28733간선·549커뮤니티**, LLM0으로 성공. 마지막 UI 변경 뒤 증분 갱신은39파일 추출 후 `Invalid argument: graphify-out\\.graphify_labels.json.sig`로 실패했다. 최종 UI의 코드 그래프가 최신이라고 보장하지 않는다. 라벨 수 변경/허브명 대체 경고도 있다. 게임 컴파일/실행에는 영향 없는 별도 생성물이다. 통합 후 Graphify를 다시 갱신할 것.
- 요청한 실제 `git add -N .`는 `E:/Git/ant/.git/worktrees/P1/index.lock` 쓰기 거부, 객체 추가도 원본 `.git/objects` 쓰기 거부로 실패했다. P1 `Temp` 안의 임시 인덱스·객체 저장소를 사용하고 원본 objects를 읽기 전용 alternate로 참조하여 같은 intent-to-add/diff를 만들었다. 원본 Git 인덱스·객체·브랜치는 수정하지 않았다.
- 요청 위치 `E:/Git/ant-wt/patches/P1-1101.patch` 복사는 EPERM으로 차단됐다. 전체 diff 대체 위치: **`E:/Git/ant-wt/P1/Temp/P1-1101.patch`**. Graphify 생성물까지 포함하므로 크기가 크다.
- **권장 통합 패치:** `E:/Git/ant-wt/P1/Temp/P1-1101-code.patch`. 전체 diff에서 `graphify-out/**`만 제외한 같은 게임·검사·문서 변경이다. P1 기준선에서 역방향 `git apply --check`로 현재 파일과 일치하는지 확인한다. 원본의 동시 작업은 덮어쓰지 말고 Claude가 충돌을 검토할 것.
