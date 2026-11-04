# F1 — D02 인재 평가 / D03 외부 조사 파견

- 기준: `DECISIONS_1025.md` Codex B, `DECISIONS_1025_codex.md` D02/D03 원문. 작업 위치 `E:/Git/ant-wt/F1`.
- 백엔드·저장 연결·검사 작성 완료. 본체/검사 오프라인 컴파일 각각 exit 0, 오류 0. **Unity 실행·검사 실행·실제 저장 왕복·화면 검증은 미수행**이다.
- 기존 병합 미커밋 코드를 보존했다. checkout/reset/add/commit/push 없음. UI/**, FormationRules/CommanderFormation, TransportDamage/ExpeditionWithdraw 수정 없음. SAVE·일지·캡처·Notion 작업 없음.

## 구현

- D02: CA/16개 기본 기술/PA의 성인 우리 장수 중앙값(포로·사망·이탈 제외, 원정 포함), 분모 하한 32/2/40. `clamp(3+2*(값/M-1),1,5)`를 반별점으로 반올림(정중앙 위).
- 기본 조사 0~20에서 현재 상대 오차 ±(30-s)%, PA 절대 오차 ±(30-s). 평가자/대상/18지표별 고정 편향을 별도 저장한다. 최초·담당 변경 시 10초/명, 완료 기록은 150초 뒤 갱신한다. 담당자 부재·대상 부재 중 기존 별점/날짜/기준을 유지한다.
- 후긴은 `TraitRules.InvestigationMultiplier`를 현재/일반 PA 오차·p·q에 적용한다. 다이아 후긴의 PA만 정확값을 영구 보존하고 새 모집단 기준으로 환산한다. 일반 PA는 오차 구간(실제 CA~200 한계 적용)과 ‘추정’ 표시용 플래그를 제공한다.
- D03: 집중 지정 1명, 탐색 같은 세력의 미보고 대상 최대 3명 무작위·중복 없음. 0명은 출발 거부. 인원이 적어도 현지 150초, 75초 도달 시 한 번만 p=(30-s)%, 조건부 q=(50-2s)% 판정. 호위는 확률에 관여하지 않는다.
- 기존 도보/차량/항공의 탑승 큐·이동·좌석·실물 식량·철수·착륙·하역을 재사용한다. 현지 조사 중 장수는 탑승 숨김 상태로 일반 전투/작업과 분리되며, 완료/추방 시 기존 철수에 넘긴다. **150초 조사 외 실제 이동·철수 시간은 별도**다.
- 정보는 현지 전체 완료 시 임시 보관하고 본거지 착륙 성공에서만 공개한다. 중도 취소/추방은 미완료 전체 무효, 완료 후 추방은 정보 유지, 포로는 이번 정보 소실. 이전 영구 보고는 유지한다. 보고서에서 직접 영입하는 경로는 없다.
- 포로는 기존 `SettlementDefense` 억류 및 `DiplomacyManager.CapturedBy/HeldCaptives` 경로에 연결한다. 적 세력 거점에도 억류 컴포넌트를 붙일 수 있도록 최소 연결했고, 저장 복원도 같은 경로를 쓴다. 호위는 기존 철수/조종 가능 상태에 따라 돌아온다. 혼자 파견되어 평가자가 포로가 되면 조종자가 없는 수송수단은 기존 규칙대로 현장에 남는다.

## 자동 선택·기존 데이터 처리

- 평가 담당자를 자동 선정/교체하지 않는다. 내부 최초 대상은 현재 명부 순서이며 진행 중 담당자의 부재 동안 10초 진행을 보존한다(대상 자체가 부재하면 가능한 다음 대상으로 넘어간다). 새 담당자를 지정하면 최초 작업을 다시 시작하지만 이미 생성한 쌍의 편향은 다시 뽑지 않는다.
- 탐색 대상 풀은 같은 외교 세력의 실제 살아 있는 `EnemyCommander`와 기존 세력 명부(prisoners/rebels)다. 새 인재/세력을 임의 생성하지 않는다. 반란 장수는 기존 개인 ID로 중복 제거한다.
- 외교 관계는 기존 데이터에 분류 enum이 없어 전쟁/음수 호감도=적대, 동맹/양수 호감도=우호, 그 외=중립으로 연결했다. 전쟁이 우선한다. 관계 분류 기준을 바꾸려면 이 한 지점을 조정한다.
- 조사 속도·대상 수·이동 속도에 조사 기술 보정을 넣지 않았다. 보급은 기존 `ExpeditionRules.RecommendedFood`의 왕복+현지 1일+예비 1일 계산을 그대로 쓴다.
- 기술 16개를 유지했다. 잘못 들어간 마지막 `Firefighting` 기술 슬롯(15)을 `Investigation`으로 교정하고 기존 배열 값/열정 저장 번호는 보존했다. 소화 작업 자체와 소화 시간은 유지하며 독립 소화 기술 XP는 제거했다. 작업→기술 조회에서 소화는 기존 근력으로 연결한다.
- 적 장수의 영구 조사 ID를 MonsterDto에 저장하며, 포로 전환 시 같은 ID를 넘긴다. 이후 영입되어도 평가자/대상 편향과 영구 보고가 유지된다.

## 저장·구저장

- `SaveFileV1.CurrentVersion = 19` 유지. `assessment.present`가 true일 때만 새 상태로 읽는다. JsonUtility가 null/누락을 빈 객체·빈 배열로 바꾸어도 없는 상태를 새 상태로 오인하지 않는다.
- 담당/작업 대상/진행/시계/기준 버전/중앙값, 고정 편향, 보고/정확 PA/관심 등록, 파견 대상/수송 인덱스/현지 진행/판정 완료/결과/미보고 정보를 저장한다.
- 수송 인덱스는 저장 시 현재 살아 있는 수송 목록에 맞춰 갱신하고, 로드 마지막에 수송 객체와 임무를 다시 연결한다. 현지 탑승 숨김도 재연결한다. 난수 복원은 기존 최종 순서를 유지한다.
- 로드 전 음수·NaN/Infinity·배열 길이·중복 쌍/대상·별점 기준·수송/거점 범위·승무원 소유·완료량·발각 상태를 검사한다. 적 거점 포로는 기존 압수 기록을 소유 근거로 검증한다.

## 연결 요청 — Claude UI 담당

1. `WorldMapManager.Instance.Assessment.AssignEvaluator(commander/null)`로 담당을 지정/해제한다. `EvaluatorId`, `PendingTarget`, `Progress`로 담당 및 최초 평가 진행(10초)을 표시한다.
2. 장수 개인 ID 또는 외부 `InvestigationTarget.Id`로 `Assessment.Rating(id)`를 조회한다. null이면 **미평가**. `TalentRating.Current`, `Skills[0..15]`, `PotentialLow/High`, `ExactPotential`, `At`을 표시한다. 일반 잠재는 두 값이 같아도 ‘추정’을 유지한다. 숫자 CA/PA 및 `TalentReport`의 원시 값은 UI에 노출하지 않는다.
3. `BasisVersion`은 현재/분야 평가 당시 기준, `PotentialBasisVersion`은 잠재 별점 기준, `CurrentBasisVersion`은 현재 모집단 기준이다. 갱신 부재 중 일반 별점은 과거 기준을 보존하며 정확 PA만 현재 기준으로 재환산한다. 날짜는 `At`(게임 초)을 기존 달력 형식으로 변환한다.
4. `Assessment.Reports`는 보고 목록 메타데이터(이름/ID/평가일/관심)를 찾는 용도다. `SetInterested(id,bool)`로 관심 등록한다. 외부 인재 선택 목록은 `Assessment.Targets(site)`의 Id/Name을 사용한다.
5. 기존 수송수단을 고르거나 `world.CreateTransport(false, position, walking:true)`로 도보 원정대를 만든다. 평가자 1명·호위 0~1명을 기존 `TryBoard`로 실제 탑승시키고 `PendingCrewOrderCount==0`을 확인한다. 다른 승객이 있으면 조사 출발은 거부한다. 병력 없이 출발 가능하다.
6. 출발 API: `ship.TryInvestigate(site, evaluator, escort:null, focus:null, allowInsufficientFood:false)`. focus=null/빈 문자열은 탐색, 특정 외부 ID는 집중이다. `RecommendedInvestigationFood(site)`와 `AvailableFood`를 비교해 부족 경고를 표시하고 사용자가 강행을 고르면 `allowInsufficientFood:true`로 호출한다. 별도 시설/비용은 없다.
7. `ship.Investigation`(null이면 임무 없음)의 `targets.Count`, `elapsed`(0~150), `riskChecked`, `outcome`으로 현지 진행·추방·포로·보고 대기를 표시한다. 이동/귀환/철수는 기존 `State`, `Remaining`, `IsWithdrawing`, `WithdrawalProgress`를 함께 사용한다. 조사 시작 후 UI에서 수동으로 이동 상태/타이머를 덮어쓰지 않는다.
8. 중단은 `Assessment.CancelInvestigation(ship)`. 외부 사건 추방은 `ExpelInvestigation(ship)`로 완료된 정보의 보존 규칙을 적용한다. 포로가 된 장수는 기존 외교 포로 화면에서 몸값/교환/구출 경로를 사용한다. 직접 영입 버튼은 만들지 않는다.
9. 기존 UI의 소화 기술 표기/고정 기술 목록이 있다면 16번째를 조사로 연결하고, 실제 PA/CA 숫자를 별점으로 교체한다. F1에서는 UI를 수정하지 않았다.

## 변경 파일

신규:
- `Assets/_Project/Scripts/Core/TalentAssessment.cs`
- `Assets/_Project/Scripts/Core/TalentAssessmentValidation.cs`
- `Assets/_Project/Scripts/World/ExpeditionInvestigation.cs`
- `Assets/_Project/Scripts/World/InvestigationCaptivity.cs`
- `AgentScripts/ImplD02D03Checks.cs`
- `docs/IMPL-F1-HANDOFF.md`

기존 최소 연결(원래 있던 타 작업 diff는 보존):
- `Core/StartingCandidate.cs` — 조사 기술명
- `Units/CommanderTraits.cs`, `Units/CommanderWork.cs` — 마지막 기술 슬롯/소화 작업 연결
- `World/EnemyCommander.cs` — 영구 조사 ID·포로 ID 계승
- `World/ExpeditionSite.cs` — 새 억류 partial 허용
- `World/ExpeditionTransport.cs` — 조사 틱·현지 숨김·착륙 보고
- `Save/SaveDtos.cs`, `Save/SaveSnapshot.cs`, `Save/SaveRuntime.cs` — 상태·ID 저장/복원
- `Save/SaveValidator.cs`, `Save/SavePreflight.cs` — 새 상태/포로/참조 검증
- `AgentScripts/ImplB7Checks.cs` — 성장/감소 검사에서 삭제된 소화 기술 참조 4곳을 같은 슬롯의 조사로 교체
- `docs/IMPLEMENTATION_BOARD.md`, `README.md` — 담당·현재 구현 경계

`Core/`, `Units/`, `World/`, `Save/` 경로 접두사는 `Assets/_Project/Scripts/`이다. 생성물 `Temp/f1/`(rsp/dll/로그/의존 소스 사본)과 graphify-out은 별도다. Unity를 실행하지 않아 신규 .cs.meta는 에디터 재개 시 생성 확인이 필요하다.

## 컴파일 결과·검사 인계

- 원본 `E:/Git/ant/Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp`와 Editor rsp 사용. 원본 소스/out/refout/analyzer/generatedfilesout 제거, Library 참조를 원본 절대 경로로 변환, worktree 런타임 소스를 재열거했다.
- 누락된 외부 의존 소스 43개를 원본에서 `Temp/f1/dependencies`로 수정 없이 복사했다. 가짜 타입/스텁 없음.
- Unity 6000.5.8f1 내장 `DotNetSdk/dotnet.exe exec .../sdk/8.0.318/Roslyn/bincore/csc.dll -nologo @Temp/f1/body.rsp` → `Temp/f1/Assembly-CSharp.dll`: **exit 0, 오류 0**. 기존 BuildingConstructionSite.cs:33/109 CS0252 경고 2개. 기존 CS0618만 숨겼다.
- 검사 `@Temp/f1/checks.rsp` → `Temp/f1/Checks.dll`(신규 ImplD02D03Checks + 참조 교정한 ImplB7Checks): **exit 0, 오류 0**. 원본 Assembly-CSharp 참조를 빼고 위 본체 DLL을 참조한다. 본체/검사 로그는 `Temp/f1/body.log`, `Temp/f1/checks.log`.
- `ImplD02D03Checks.Main()`은 public static async Task<string>. RootOverride·자동저장/위협 자동정지 설정·TimeScale·난수를 finally로 복원하며 internal은 리플렉션, 게임 타입은 전체 이름을 쓴다.
- 검사 범위: 반별점/중앙값/오차/p/q/후긴 경계, 최초10초·일일 갱신·부재·고정 편향·정확 PA 보존·관심 상태 기반, 실제 탑승과 무보급 경고/강행·0명 거부·탐색 중복 방지·75초/150초 경계, 현지 save/load·재추첨 방지·구저장/비정상 상태 거부, 귀환 전 공개 방지·완료 후 추방 보고·초기 추방 영구 보고 보존·포로/호위 분리·포로 save/load를 작성했다.
- **위 항목들은 검사 코드 작성 및 컴파일 완료를 뜻한다. 실행 PASS나 실제 왕복 성공으로 해석하면 안 된다.** Claude가 Unity에서 이 검사를 실행하고 기존 원정/SaveRoundtrip/정찰 회귀를 확인해야 한다. UI 구현 및 화면 검증은 별도다.

- `git diff --check` 통과. 신규 파일은 BOM 없는 UTF-8/CRLF/줄 끝 공백 없음 확인. 기존 파일은 바이트 치환으로 원래 BOM·줄바꿈을 보존했다.
- `graphify update .` exit 0: AST-only, API 비용 없음. 최종 출력 10058 nodes / 22982 edges / 453 communities. 커뮤니티 구성 변경으로 기존 명칭 일부를 허브 이름으로 대체했다는 안내가 있었으며 의미 기반 라벨 갱신은 하지 않았다. `Temp/f1/graphify-final.log` 참조. 생성된 graphify-out 변경은 커밋하지 않았다.
