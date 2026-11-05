# Codex Q1 인계 — 2026-11-01

작업: `E:/Git/ant-wt/Q1`, `codex/q1-1101`, 기준 b45d257. 원본 `E:/Git/ant`는 로그·컴파일 rsp/DLL·SpinFree 소스를 읽기만 했다. Unity 실행/조작·캡처·커밋·push·SAVE 일지·Notion 쓰기 없음. WorldLabels*·ReserveWorldLabel 관련·FacilityVisual*·지형 메시·prefab 변경 없음.

## 1단계: 한국어 표시8건

| P3 번호 | 판정 | 수정·확인 |
| --- | --- | --- |
| 33 포상 | 구현 | DetailTabs: 식량 30, 실제 기분8/15/3, 효과·재사용 각300초=2일. 실패 시 명령/본거지 조건 표시 |
| 34 도감 | 기준선 확인+보완 | GameNotifications는 이미 한국어 제목/본문이며 키 유지 확인. GameMenuController 범주 화면 변환, 이미 저장된 알려진 영어 도감만 읽을 때 번역(PersonalDisplayNames). 구원문·키·범주 저장값 불변 |
| 35 건조 비용 | 구현 | WorldMapPanel: 식량 50/재료60, 식량 80/재료100. TransportDamage/ScienceLab 실제 비용 대조. 버튼 ID 유지 |
| 36 무기 | 기준선 확인+공유 보완 | 기존 WeaponName은 한국어, WeaponLabel은 도감 키에서만 사용. WeaponDisplayName으로 기존 switch를 공유하여 구도감도 같은 함수 사용. Bare mandibles 키 유지 |
| 37 결과 버튼 | 구현 | Continue Colony/Load Game/New Game/Back 등 GameObject 이름 그대로, 자식 Text만 한국어. 이미 한국어인 Restart/Restart Same Map 표시 유지 |
| 38 거점 | 기준선 확인+보완 | 기존 SiteNames.Name/Faction 재사용. 원정 박스·마커 생성 라벨/툴팁·외교 산업 툴팁 연결. 단독 Independent도 독립 표시. site.Title 키·마커 ID 유지. 수송수단 기본 이름 Vehicle/Aircraft도 표시만 차량/비행기로 변환 |
| 39 장비 부위 | 구현 | PersonalDisplayNames.Slot: 무기/방어구/장신구. enum/번호 유지 |
| 40 기분 | 구현 | DetailTabs·GameMenuRoster·MoodWatch·SelectedUnitPanel에서 Hunger/Rest/Catharsis·Death:/Downed: 접두사 표시 변환. 저장 mood 키·인물 이름·알 수 없는 원문 유지 |

## 2단계: 새 게임성32건

[GAMEFEEL_1101_Q1.md](GAMEFEEL_1101_Q1.md): 기존 1101 문서와 중복되지 않는32건, **상·중21건 코드 구현/하11건 후속**. 신규 자동 채집·제조법 선택·수술·전쟁·출정·발사 결정 없음. 정보 부족에서 생기는 손실·무의미한 반복 입력을 먼저 해결했다. 새 패턴/전장 목표/지형·보급 경제 확대11건은 행별 이유를 남겼다.

- 출정: 실제 TryDepart와 UI가 DepartureBlock 공유. 기존 조건을 이동했으며 수동 버튼에서만 식량 부족 재확인 추가. 확인 후 부대/보급이 달라지면 다시 확인한다.
- 계획: 감속0.75배·접안1초를 왕복·보급 예산에 반영. 기존 현지/예비 각150초 유지. 재고 부족 시 가능한 만큼 권장 보급 싣기. 운행 불가 권장량은 미정 표시.
- 이동/귀환: 실제 감속 후 남은초, 현지 접안/본거지 적/착륙 경로 대기 사유, 실물 화물·예약 분류. 수동 도착·하역 직전 결과 알림, 귀환 후 보급 핀 해제. 자동 수송 반복 알림은 제외.
- 보스: 월드맵 현재HP/재등장 기한/현지·운송·본거지 핵심물, 기존 Guide는 툴팁. 실제 HUD 예고/착지 타이밍. B 열0/호위2·C 열6/호위4에 맞춰 안내만 수정.
- 직접 전투/외교: 대공/상태상 공격 불가 클릭의 이동 대체를 알려줌. 교역소·평화 거점 상태 정확화. 협상600초 관문 남은 시간·지불/수령 방향과 버튼 활성화 실시간 갱신.

## 수정 파일

경로 기준 `Assets/_Project/Scripts/`:

- 신규(+meta): `UI/PersonalDisplayNames.cs`, `UI/ExpeditionPlanning.cs`, `UI/DiplomacyPlanning.cs`, `World/ExpeditionTransport.Planning.cs`, `World/BossStronghold.Planning.cs`.
- UI: `DetailTabs`, `GameMenuController`, `GameMenuDiplomacy`, `GameMenuRoster`, `MoodWatch`, `SelectedUnitPanel`, `SiteNames`, `WorldMapPanel`, `HUDController`.
- 전투/원정: `Units/CommanderEquipment`(기존 무기 표시 switch 공유만), `UnitSelectionController`(거부 피드백), `World/ExpeditionTransport`·`ExpeditionSupplies`·`BossStronghold`, `Boss/BossProgress`·`BossEncounter`.
- 검사: `AgentScripts/Review1101Q1Checks.cs`, 기존 `ImplB6Checks.cs`의 B→C 열 위험 기대값.
- 문서: Q1 GAMEFEEL/DECISIONS/본 인계/validation, IMPLEMENTATION_BOARD·OPERATIONS_100_STATUS·README, P3 GAMEFEEL/HANDOFF에 현행 후속 링크. log/changelog 변경 없음.

저장: **추가 필드 없음**. SaveDtos/SaveSnapshot/SavePreflight의 기존 상태 경로(killedAt/unclaimed/cargo/home, transport health/Remaining/crew/withdraw)에서 계산만 한다. 구저장 원문/식별자/도감·기분 키를 이관하거나 덮어쓰지 않는다. 기존 완전 save/load를 이번에 통과했다고 주장하지 않는다.

## 검증

- 사용자 지정 offc.sh: 전체 게임 Roslyn **오류 0**. offcheck.sh: Q1/P3/ImplB6 검사 DLL **오류 0**.
- Unity6000.5.8f1 내장 .NET8 호스트에서 Q1 **86** / 기존 P3 **285** 통과. 현재 필드만 읽는 수송 계획 검사(정상41초·손상54.333초·감속 남은40초·운행 불가), 구도감 읽기 변환/원문 보존, 부분 보급 경계, 정전600초 경계·배상 방향,4보스 미회수→운송→반입·재등장 대기 검사 포함.
- `Review1101Q1Checks.Main()` / `RunQ1Scene()`은 public static async Task<string>. internal 경로는 리플렉션, 게임 타입은 AntColony 전체 이름. Main의 가짜 Unity 객체는 엔진 객체를 만들지 않고 필드 기반 계산만 검사한다.
- `RunQ1Scene()` **컴파일만 완료, 미실행**. 시작 완료 격리 Play에서 실제 수송 컴포넌트의 출정 불가 판정/실속도 예산·화물/화면 버튼을 읽는다. 성공 출정을 강제로 실행하거나 게임을 초기화하지 않으므로 실제 출정→전투→귀환은 아래 수동 확인 필요.
- 원시 로그: `Temp/off/out.txt`, `cout.txt`. 요약: `docs/validation/Q1-1101-offline.txt`. 오프라인 호스트 `Temp/off/RunQ1.ps1`는 임시 산출물이며 패치에서 제외.
- 처음 offc는 worktree에 없는 외부 `Assets/Planet Earth Free/SpinFree.cs` 및 새 코드 GetInstanceID 금지 API 때문에 실패했다. 원본 외부 소스를 Q1에 임시 읽기 복사하고 GetEntityId로 수정한 최종 결과가 오류 0이다. 임시 외부 소스는 패치에서 제외·제거했다. 기존 obsolete 경고는 유지.

재현(Git Bash, 원본은 읽기만):

```sh
bash C:/TESTTEMP/claude/E--Git/9cd1f401-5158-48e6-8468-2180ba139fd8/scratchpad/offc.sh /e/Git/ant-wt/Q1
bash C:/TESTTEMP/claude/E--Git/9cd1f401-5158-48e6-8468-2180ba139fd8/scratchpad/offcheck.sh /e/Git/ant-wt/Q1 /e/Git/ant-wt/Q1/AgentScripts/Review1101Q1Checks.cs /e/Git/ant-wt/Q1/AgentScripts/Review1101P3Checks.cs /e/Git/ant-wt/Q1/AgentScripts/ImplB6Checks.cs
```

외부 SpinFree 소스가 없는 이 worktree에서는 위 재현 시 같은 임시 복사 또는 **Q1 Temp/off/a.rsp에 원본 소스를 읽기 참조로 추가**해야 한다. 해당 art 파일을 통합하거나 원본 파일에 쓰지 않는다.

## Claude 실행·화면 확인 요청

1. 최신 원본에 코드 hunk·신규5파일/meta만 병합. UI/WorldMapPanel·HUDController·SiteNames의 Claude 동시 변경은 덮어쓰지 말고 개별 병합. 금지 파일은 Q1 패치에 없음.
2. 격리 Play에서 Main/P3 Main/ImplB6 실행. 원정 준비·출정 중·손상 이동·귀환 착륙 대기·보스 전장을 각각 열고 RunQ1Scene 반복 실행. 기존 수송/도보/보급/저장 왕복 검사도 통합 후 실행.
3. 건강한 차량→식량 부분 적재→부족 경고→다시 눌러 출정, 확인 후 병력·식량 변경 시 재경고. 보급·탑승·화물 비용 중복 없음을 확인. 자동 수송은 종전 운영·자동 반복 알림 없음 확인.
4. 체력25~50% 차량의 실이동/왕복 보급/표시시간, 운행 불가 수리, 다중 접안, 본거지 적 때문에 착륙 불가→격퇴 후 귀환을 실제로 수행. 귀환 결과에 장수·병력·장비·핵심물·예약 집계와 실물 하역이 맞는지 확인.
5. A 보스 미처치 후 철수→HP 유지 확인, 처치→미회수/적재/반입 각 상태,12일 후 잔류 보류/전원 철수 후 재등장 확인. 보스 HUD 착탄·비행 예고와 실제 판정, 혼합 부대 대공 불가 메시지 확인. B/C 위험 안내가 실제 공격과 일치해야 함.
6. 1920×1080/1280×720: 보스 HUD(440×42, 상단바 아래), 월드맵3행/긴 숫자/전리품 분류, 포상 긴 툴팁, 버튼 ID 유지·한글, 기분 접두사, 옛 영어 도감 표시, 평화 협상 카운트다운 잘림·겹침·툴팁 접근 확인. **실화면 미검증이며 캡처는 SAVE 때만**.
7. Notion 확정 기획/참조 일지 반영은 통합 담당자가 잠정 결정표와 대조 후 수행. 부모 페이지 전체 교체 금지. 카카오 완료 도구는 이 세션에 없어 미발송.

## 패치·환경

- 공용 요청 경로 `E:/Git/ant-wt/patches/Q1-1101.patch` 복사는 **Access denied로 거부됐다**. 이 세션 쓰기 루트는 Q1뿐이므로 아래 Q1 내부 패치로 전달한다.
- 통합용: **`E:/Git/ant-wt/Q1/docs/patches/Q1-1101.patch`**. Graphify 생성물만 제외한 코드·검사·문서 변경. 전체 diff는 `Temp/Q1-1101-full.patch`.
- 실제 `git add -N .`는 원본 `.git/worktrees/Q1/index.lock` 쓰기 거부. Q1 Temp/off의 임시 인덱스·객체 디렉터리와 원본 objects 읽기 alternate를 사용해 같은 intent-to-add/diff를 생성한다. 원본 Git 인덱스·브랜치/객체는 수정하지 않으며 커밋하지 않는다.
- Graphify AST 갱신 완료: **12518노드/29180간선/576커뮤니티**, LLM0. 새 DiscoveryTitle/DepartureWarningKey/TravelRemainingSeconds 노드도 출력에 존재한다. 커뮤니티 수 변경으로228개 이름이 허브 기준으로 바뀌었다는 경고가 있고 의미 라벨 재작성은 안 했다. PowerShell이 이 stderr 경고를 NativeCommandError/exit1로 표시하지만 원시 출력에 Rebuilt·Code graph updated가 있고 graph.json/html/보고서가 갱신됨을 확인했다. Graphify 생성물은 게임 통합 패치에서 제외한다.
- `git diff --check` 및 통합 패치의 `git apply --reverse --check` 통과. 패치에 원본 임시 SpinFree·금지 파일·prefab이 없는지 경로 목록 대조. 공용 경로 대신 **Q1/docs/patches/Q1-1101.patch**를 Claude가 읽기 반입하면 된다.
