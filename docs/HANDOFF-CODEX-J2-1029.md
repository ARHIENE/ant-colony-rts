# Codex J2 인계 — 2026-10-29

- 기준: `e42430d617f371d4c23c13f8e0472f7a7a177b01`, `codex/j2-1029`, `E:/Git/ant-wt/J2`에서만 수정. 원본은 log/응답파일/누락 외부소스 참조만 했다.
- **22항목 대조: 17항목 구현·안내 보완, 5항목(62·63·64·83·84) 계산·현행 규칙 유지.** 이것은 Unity 실행22건 완료가 아니다.
- 게임/검사 오프라인 Roslyn 컴파일 오류0. 실제 게임 DLL을 .NET8에서 호출한 계산 검사339개(전투270·보스/원정38·최종/포로31) 통과. `Review1029CampaignRuntimeChecks`는 컴파일만 했고 실행하지 않았다.
- Unity 에디터 실행/조작·캡처·빌드·커밋·push·SAVE 일지·Notion 편집 없음. 79·81·82·97·98 기존 수정은 유지했다. 경제/식품/산업/교역 UI 파일은 수정하지 않았다. `GameMenuDiplomacy`는 포로 귀환 문구1곳만 수정했다.
- 패치: `docs/patches/J2-1029.patch`. 원본의 공용 .git/index·objects를 수정하지 않도록 J2 Temp의 `GIT_INDEX_FILE`·`GIT_OBJECT_DIRECTORY`와 원본 읽기 전용 alternates에서 `git add -N .`을 실행하고 소스/검사/문서 diff를 추출했다. 생성된 `graphify-out`와 패치 자체는 이관 diff에서 제외했다. 로컬 Graphify는11610노드·26903간선·510커뮤니티로 갱신(LLM0), 커뮤니티 이름 일부 자동 재지정 안내가 있었다.

## 항목별 판정

‘Runtime’은 아래 Unity 실행용 스크립트의 검사다. 아직 통과로 계산하지 않았다. 재미·조작감이 필요한 항목만 ‘실플레이’라고 구분한다.

| 항목 | 원문 위험 판정·처리 | 근거/검사 | 잔여 |
| --- | --- | --- | --- |
| 61 | 병력은 장수1개체에 합쳐져 집중 공격·범위 회피·철수에 같이 참여. 출전 안내 보완 | CommanderAnt.AttackAgainst, GameMenuConscription, Combat270/Runtime 실제 배율 | Runtime 실행·부대 정보 전달성 실플레이 |
| 62 | 실제 손실 가속 성립. max(1,병력) 공격·병력1HP·개인100HP 확정 유지 | 공유 FormationDamage.AfterArmor로4개 손실 곡선, DECISIONS 계산표 | 피해 전반 밸런스를 새 공식으로 바꾸지 않음 |
| 63 | 기술/장비가1인 공격에 더해진 뒤 병력에 적용. 장수1명의 기술만으로 물량 대체 보장 없음 | DECISIONS 기본5/기술5/품질3/병력5 비교, CommandLimit 기존식 | 전 분야 PA/성장 조정은 J2 범위 밖 |
| 64 | 약한 다단 공격의 방어 억제 성립, 강한 단발 관통과 최소 피해 남음. 공식 유지 | 실제 공유 공식120조합·양수/상한240확인, TTK표 | 실제 모든 장비·진형 균형의 완료 아님 |
| 65 | 기존 필수 보스가 지상 공격만 사용해 날개로 무력화 가능. 보스 비행·착지와 비행 대공 공격 추가 | BossEncounter/BossHealth.IAirborne, Runtime 역할별 실제 보스 판정·예고 타격 | Runtime·실제 예고 육안/회피 실플레이 |
| 66 | 출전 전 선택 원칙 유지. 적·지형·철수 조건/불이익을 기존 버튼 툴팁에 표시, 장사진 ‘산’ 오표기 교정 | GameMenuConscription, FormationState 적중/타깃변경/만료 검사 | 전체 진형 실전 조합·선택 빈도 |
| 67 | 추행≤종대·쐐기≤봉시·엄호≤학익 효과 열등 관계 확인. 해당3수치만 조정 | Swift25%/Wedge정면30%/Cover철수30%, Combat270 조건 반전 | 광범위 진형 재설계 없음 |
| 68 | 장비와 회피의 역할이 미연결. 비행 예고·열·호위로 장비의 효용과 회피 창 구현 | B 실제 HeatResistance.5, C 실제 AttackRadius3 검사, Runtime 예고1회타격 | 실제 이동 회피율·고급장비 탱킹 실플레이 |
| 69 | 기존 A HP1000×난이도는 공격50 기준20~60초로 목표5~10분과 불일치. A12000으로 조정 | 무피격 근접4/5/6병력320~480초 계산. 미처치 HP 보존, 처치 후 재등장과 구분 | 실제 이동/공격 중단·준비 노동 체감 |
| 70 | 필수 사슬은 각 보스1회면 됨(A핵심물2개). 반복은 고유장비 재획득 목적.12일/잔류 보류/회차당1보상 유지·안내 | BossChecks 실제 BossProgress 경계·반복 획득·중복 차단 | 재획득 필요 횟수의 장기 체감 |
| 71 | 차량/항공 고정시간을 Notion의3띠 시간에 연결, 보급도 목적지 기준으로 계산 | TravelSecondsTo·TransportSeconds, WalkingFormation의 거리/바이옴/세력, Boss38 | 월드맵 표시1곳 Claude 연결 요청(아래) |
| 75 | 일반 탐험은 보급·표본·재료 확보, 필수 보스는 탐지로 진입. 강제 탐지 관문 유지·안내 | GameMenuWalking/BossProgression, 취득/반입·공개 분리 검사 | 표본별 연구 확장은 타 담당 |
| 76 | B 방열→C의 열·C 범위→B의 호위로 분기 순서에 준비 차이 추가 | 실제 장비 효과·B/C 양 순서 D합류 검사 | B/C 실제 현장 패턴·호위 생성 Runtime 실행 |
| 80 | 900초+거리×1000은 확정 안전 귀환. 시간 단축 안 함. 개월→일/1배속분, 기한 경과 후 도착 대기 | GameMenuPrisoners·Diplomacy 포로 문구, Finale31 | 본거지 적/경로 때문에 도착 보류되는 실제 상황 |
| 83 | 기본 파동 상한6으로 재산200/인구20부터 이미 포화. 장식 때문에 무한 강화는 기본값에서 성립 안 함 | ColonyInvasion.WaveCount 기본값 수식표, Rocket 화면 설명 | 실제 씬 직렬화 값·난이도별 파동 확인 |
| 84 | 전지역 재산 포함·예약 중복제외·현재/6일 평균 max는 확정. 원격 재산 제외 안 함 | RocketEconomy.CaptureOwned 대조·실제 Preview/Observe 계산, 가동 화면 설명 | 현지 자원 동원 체감은 실플레이 |
| 85 | 가동 후 인구 증가가 고정 입력을 올리지는 않음. 구할 인원을 가동 전 정하고 모듈을 늘림 |20/100/500/1000명 모듈1/2/10/20 계산. 시민이 장수 마지막1석을 차지하지 않게 수정 | Runtime 시민 승차·실제 좌석 소유권 |
| 86 | 신규 점수 없이 시민 탈출/잔류·남은 편입지와 관계 결과를 엔딩에 남김 | CampaignLegacy/CampaignOutcome, Runtime 배우자 분리 결과 | 엔딩 화면 읽기/감정적 의미 실플레이 |
| 87 | 장수 이동→탑승 확정, 장수별 하선·방어 재투입 지원. 시민 단독 발사 양 경로 차단 | HasLaunchCommander·TryDisembark·Unload 요청취소, Runtime 탑승/하선/재탑승 | 실제 이동 경로·UI 두 해상도 검수 |
| 88 | 기존 안전 하선은 확정 손실과 다름. 시민/병력 사망·장수 중상, 실패 시점·고정입력 기록.15일 재시작 유지 | RecordDestruction·DestroyPassengers·CommanderRocket, Runtime 동틱 파괴 우선·구조 상태 | 구조·치료→회복의 실제 플레이 |
| 89 | 핵심물이 최초 연구 해금만 하고 재사용되던 경로 수정. 가동당1소비. 바벨은 발사대 기초만 재건 | TryConsumeRocketCore·AirshipYard.Recovery·SaveConstruction, Runtime 실제 재건/전체 Preflight | 전체 건물 바벨 제공자는 별도 공백. 로켓 Runtime 실행 |
| 90 |6분류 외 실제 관계·사망 원인/날짜·세력·남은 편입지 스냅샷, 구출/귀환·실패/재건 이력 표시 | CampaignLegacy·EndingRecord, Finale31 검증·Runtime 저장된 관계 결과 | 실제 엔딩 화면·실제 저장복원 실행 |

## 수정 파일

- 전투: `Units/FormationRules.cs`3상수, `Core/FormationDamage.cs`실제 피해식 공유 함수, `Units/CommanderAnt.cs`동일 피해식 호출·로켓 시공 기여 연결4줄.
- 보스: 신규 `Boss/BossEncounter.cs`·`BossEncounterRules.cs` 및 meta, `BossHealth.cs` 비행 판정/초기 HP/재등장·사망 제어, `World/BossStronghold.cs` 연결, `World/WildMonster.cs`호위12HP/공격2 설정1메서드.
- 원정/포로: `World/ExpeditionRules.cs`·`ExpeditionTransport.cs`·`ExpeditionSupplies.cs`, `DiplomacyPrisoners.cs`시간 표시 함수. 외교 교환/평화 규칙은 건드리지 않음.
- 로켓: `Buildings/AirshipYard.cs`·`.Rocket.cs`·`.Civilians.cs`, 신규 `.Recovery.cs`와 `Units/CommanderRocket.cs` 및 meta. 일반 장수 피격 즉사·의료 규칙은 변경하지 않음.
- 엔딩: `Core/CampaignOutcome.cs`, 신규 `CampaignLegacy.cs` 및 meta.
- 저장: `SaveDtos.cs`·`SaveSnapshot.cs`·`SavePreflight.cs`·`SaveConstruction.cs`. 필드와 구저장 호환은 아래 참고.
- 기존 UI: `GameMenuConscription/Walking/BossProgression/Campaign/Rocket/Ending/Prisoners.cs`와 `GameMenuDiplomacy.cs`포로 문구1곳. 새 창/경제·식품·산업·교역 UI 없음.
- 검사: `AgentScripts/Review1029CombatChecks.cs`, `Review1029BossChecks.cs`, `Review1029FinaleChecks.cs`, `Review1029CampaignRuntimeChecks.cs`, `Review1029J2Compile.ps1`.
- 문서: 본 인계·DECISIONS·GAPS·100항목 상태·전투 검토·README·담당 보드.

## 저장 경로·구저장

| 상태 | Capture/Restore 경로 | 사전검증·기본값 |
| --- | --- | --- |
| 보스 비행·예고·위치·잔여 | MonsterDto.bossEncounter → SaveSnapshot Capture/Restore → BossEncounter.Capture/Restore | SavePreflight → BossEncounter.Validate. `taken` 마커가 없는 구저장/JsonUtility 빈 객체는 첫 착지 상태. 비보스의 빈 객체도 오인하지 않음 |
| 바벨 시공 기여·첫 기초 사용 | AirshipYard.State.builders/constructionWork/babelEligible/foundationUsed → 기존 BuildingDto.airship | AirshipYard.Validate. 구저장 목록null/빈 목록은 기여 이력 없음, 소급 자격 부여 없음 |
| 건설 중 로켓 | ConstructionDto.hasRocketState/rocket → SaveConstruction.Capture/Restore | SavePreflight → SaveConstruction.Valid → AirshipYard.Validate. 명시 마커로 일반 건설의 null→빈 객체와 구분 |
| 핵심물 소비 | CampaignResearch.State.bosses.home[D] 기존 필드 | 획득/반입 자격·연구는 보존. 구저장의 이미 가동 중인 로켓에 소급 결제하지 않음. 이후 새 가동부터1개 요구 |
| 엔딩 서사 | RocketCampaign.State.ending.legacy → 기존 SaveFileV1.rocket | CampaignOutcome.Validate, 구저장null/빈 목록 허용. 점수/등급 없음 |

저장 버전21 유지. 실제 JsonUtility 및 전체 파일 저장복원은 Runtime 검사 실행 대상이며, .NET 계산 실행을 Unity JsonUtility 검증이라고 주장하지 않는다.

## 검증 재현

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/Review1029J2Compile.ps1 -OfflineChecks
```

- 원본 Bee 응답파일에서 source/out/refout/analyzer/generatedfilesout을 분리한다. 기존 응답파일의 worktree 소스와 신규 소스를 컴파일한다. worktree에 없는 외부 소스44개는 원본에서 **읽어 J2 Temp에 복사**한 참조 사본을 사용한다(SpinFree 등). 원본은 수정하지 않는다.
- 게임 DLL과4개 검사 파일을 별도로 컴파일한다. 검사는 Editor.rsp+J2 DLL을 사용하며 internal은 리플렉션, 게임 타입은 `AntColony.*` 전체 이름으로 참조한다.
- 오프라인 실행은3개 계산 검사만 호출한다. 전투270·보스/원정38·최종/포로31=339 PASS. 기존 API/참조비교 경고는 남음. 실제 GUI/물리·NavMesh·Unity JsonUtility를 .NET에서 흉내내지 않는다.
- `Review1029CampaignRuntimeChecks.Main()`은 **장수2명의 격리된 새 게임**에서 Claude가 실행해야 한다. 기존 플레이의 작업 지시/씬 배치를 완벽히 되돌리는 테스트가 아니므로 실제 사용자 플레이에서 실행하지 않는다. 사용자 저장파일은 쓰지 않는다. 실제 장수 배율·로켓 가동 핵심물·시민9+장수1석·개별 하선·시민 단독 발사 거부·배우자 분리 엔딩·바벨75%경계·동틱 파괴·중상·시민 미환급·기초 재건·전체 SaveSnapshot/Preflight·실제 보스 대공/예고1회 타격·잘못된 보스 상태 거부 검사 작성.
- 실행 후 추가로 전체 저장 파일→씬 재구성→불러오기를 수행할 것. Runtime 스크립트의 JSON/Preflight 통과만으로 실제 디스크 저장/로드·NavMesh 복원을 완료 처리하지 않는다.
- 최신 로컬 결과: `Temp/Review1029J2/validation.txt`. `git diff --check` 통과. Unity Player 빌드는 하지 않았다.

## Claude UI·통합 요청

1. 허용 범위 밖이어서 `WorldMapPanel.cs`는 수정하지 않았다. 약194행 출정 계획의 왕복 시간을 `(selectedShip.Walking ? ... : selectedShip.TravelSeconds)` 대신 `selectedShip.TravelSecondsTo(selectedSite)`로 표시해야 한다. 도보 진형 보정과 새 목적지3띠를 모두 반영한다. 현재 보급 예상은 이미 새 함수 사용.
2. `GameMenuManagement.cs`의 `case "진형"`은 아직 미구현 문구로 연결되어 있다. 기존 `OpenConscription()`으로 안내하거나 해당 출전 편성으로 연결할 것. 여러 파트를 포함하는 공통 화면이므로 J2가 수정하지 않았다.
3. `GameMenuWalking`의 목적지 목록·징집 툴팁·보스 설명·로켓 시민/장수 하선 버튼·포로 귀환 줄·엔딩 서사를1920×1080/1280×720에서 확인할 것. SAVE 전 Unity 캡처는 하지 말 것. 신규 내용은 기존13/12px·표면/강조 규칙을 사용했다.
4. `CommanderAnt.OnWorked`의 로켓 기여 연결4줄, `SaveConstruction`의 로켓 상태, `BossHealth` 사망/재등장, `SaveSnapshot/Preflight/Dtos`는 공통 파일 충돌 가능성이 크다. J1/J3/Claude 변경과 통합 시 이 작은 연결을 보존할 것. 전체 건물 바벨 구현이 들어오면 로켓 전용 기여 기록을 공통 제공자와 중복 계산하지 말 것.
5. 기존 `ImplB9Checks` 등은 새 가동에 D핵심물1개를 준비해야 한다. 시민 단독 승리·파괴 후 건강한 하선·핵심물 무제한 재사용을 기대하는 옛 검사는 새 확정 규칙에 맞춰 교정할 것. 기존 검사 전부 통과로 보고하지 않았다.
6. Notion 추천안 반영은 통합 후 담당자가 수행할 것. 전투·보스/건물·방어시설/월드맵·원정·외교/맵·진행/미정/참조 UI 페이지를 함께 교정하고, 하위 페이지가 있는 기획 루트에 replace_content+allow_deleting_content를 사용하지 말 것. 추천안과 미실행 상태를 확정·실행 완료로 쓰지 않는다.

추가 기획15건은 `GAPS_1029_J2.md`, 모든 잠정 수치·선택 이유·기획 출처는 `DECISIONS_1029_J2.md`에 있다. 카카오 완료 도구는 세션에 제공되지 않아 전송할 수 없다.
