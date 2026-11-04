# G27 실패 분석·수정안 — 2026-10-25 / Codex

수정안 작성과 오프라인 검증 완료. **본 프로젝트에는 미적용, Unity/Play 재검증 미실행.** 이번 결과를 C01~C14 런타임 PASS 또는 G27 완료로 해석하지 않는다.

## 범위

- 쓰기는 `E:/Git/ant/codex-work~/g27/` 내부만 수행했다. 본 프로젝트·UI·다른 Codex 작업 파일은 수정하지 않았다.
- `log.md`, 공동 보드, 결정 문서 및 관련 IMPL 인계를 읽었다. 담당 표시는 쓰기 제한에 따라 이 폴더의 `IMPLEMENTATION_BOARD.md`에 남겼다.
- Unity 실행·캡처·커밋·Notion·개발 일지 작성 없음. Graphify query는 수행했고, 원본 그래프를 변경하는 update는 쓰기 제한 때문에 실행하지 않았다.
- 기준: `docs/PLAY_CHECK_DECISIONS_1023.md` G07/G22, G16/G20, G09/G19. 구현 경계는 `docs/IMPL-D2-HANDOFF.md`, `IMPL-B6-HANDOFF.md`, `IMPL-B9-HANDOFF.md`, `IMPL-C2-HANDOFF.md`, `IMPL-C3-HANDOFF.md`, `IMPL-C4-HANDOFF.md`, `IMPL-I1-HANDOFF.md`를 참조했다.

## 실패별 판정

| 실패 | 분류·확정 수준 | 코드 근거·한계 |
|---|---|---|
| C10[1]/C10[3] | **확정: 검사 호출 경로 문제** | 원본 `AgentScripts/ImplG27Checks.cs:59~63` Reward가 `BossProgress.TryDeliver()`를 직접 호출한다. `Boss/BossProgress.cs:91`은 delivered/home/cargo만 변경한다. `Core/CampaignResearch.Progression.cs:48`의 `DeliverBossReward()`는 반입 성공 후 D 반입 여부를 확인해 `AcquireBlueprint()`를 호출한다. `CampaignResearch.cs:130,151`의 HasBlueprint는 별도 state.blueprint를 읽는다. D 반입 기록이 생겨도 낮은 단계 API를 직접 호출하면 blueprint는 false로 남는다. B/C 합류 조건 자체의 결함을 뜻하는 실패가 아니다. |
| C12[1]/C12[3] | **확정: 보고된 실패는 C10 연쇄** | Stage12 첫 Check가 HasBlueprint를 요구하고, 이 Check 이전에 yard를 생성하지 않는다. 해당 메시지는 TryBuild에 도달하기 전에 발생한다. 로켓 건조 코드의 독립 결함은 이 결과로 판단할 수 없다. |
| C13[1]/C13[3] | **확정: 보고된 실패는 C12 연쇄** | Stage12 첫 Check 실패로 지역 변수 yard가 null인 채 Stage13으로 넘어가 `constructed rocket required`가 발생한다. 연료/가동/공세/탑승/발사 요청의 수정 후 성공 여부는 미검증이다. |
| C14[1] | **확정: 게임 저장 옵션 누락** | 원본 `Save/SaveDtos.cs:22` OptionsDto에 startingCount가 없고 `SaveSnapshot.cs:34`와 `SaveSystem.cs:49`에도 전달이 없다. `Core/GameOptions.cs:21` 기본값은 3이다. 따라서 1명 게임을 정상 저장/로드해도 `Options.startingCount == 1` 조건은 false가 된다. 이 결함은 해당 FAIL을 설명하기에 충분하다. |
| C14[3] PASS | **확정: 기본값 3과 기대값의 일치** | 같은 누락이 있어도 3명 검사는 startingCount 조건이 맞는다. 1명·3명 모두 저장 경로가 올바르다는 근거로 쓸 수 없다. |

**추정/미확정:** C14[1]의 복합 조건 중 시작 인원 외에 ID·시민 수·생존 여부·현재 장수 수에도 문제가 있었는지는 제공된 메시지만으로 확인할 수 없다. 시작 인원 누락이 유일한 원인이라고 단정하지 않았다. 새 검사는 각 조건을 별도 메시지로 보고한다. 새 Unity 실행 없이 기존 실패의 런타임 재현 및 수정 후 해소를 확인했다고 주장하지 않는다.

실제 게임의 장수 전달은 `Units/CommanderBossWork.cs` → `CommanderAnt.DeliverBossEquipment()` → `CampaignResearch.DeliverBossReward()`이고, 수송 하역도 `World/BossRewardTransfer.cs:46`에서 같은 장비 전달 함수를 호출한다. 이번 C10 수정은 최소 API 검사의 캠페인 반입 단계만 맞춘 것이다. 실제 보스 전투·이동·하역·G22 장비 지급/효과 전체를 검사하지는 않는다.

## 수정안: src/<원래 경로>

1. `AgentScripts/ImplG27Checks.cs`
   - Reward가 CampaignResearch를 받아 정식 `DeliverBossReward()`를 호출한다. 성공 1회와 중복 반입 거부를 따로 확인한다.
   - 기존 B/C 분기·D 합류 및 실제 HasBlueprint 검증을 유지한다. 설계도를 fixture로 강제 해금하지 않는다.
   - C14에서 저장 JSON의 시작 인원을 확인하고, -1/4 거부와 구 저장의 필드 부재 기본값 이관 검사를 추가했다.
   - C14의 현재 장수 수/생존/ID/시민 수/시작 인원 조건을 분리해 실패 원인을 드러낸다.
   - internal 직접 접근을 추가하지 않았다. 기존 FinishReturn/GetTemplate 리플렉션 유지. ResourceType은 충돌하지 않는 Res 별칭을 유지한다.
2. `Assets/_Project/Scripts/Save/SaveDtos.cs`: OptionsDto에 startingCount 추가.
3. `Assets/_Project/Scripts/Save/SaveSnapshot.cs`: 원래 시작 인원을 저장한다.
4. `Assets/_Project/Scripts/Save/SaveSystem.cs`: 검증된 시작 인원을 로드 옵션에 전달한다.
5. `Assets/_Project/Scripts/Save/SaveValidator.cs`: 0은 필드가 없던 구 저장으로 간주해 기존 로드 기본값 3으로 이관한다. 이관 후 1~3만 허용한다.

스키마 버전은 유지한 필드 추가다. 과거 저장에서 누락된 실제 시작 인원은 현재 생존 장수 수로 추론하지 않는다(사망/합류 가능). 구 저장의 정확한 시작 인원 복구는 이 패치가 보장하지 않는다. 명시적 0도 구 저장 sentinel과 동일하게 처리된다. startingTeam 후보 배열은 현재 생존자의 저장 복원과 별개이며 이번 범위에 추가하지 않았다.

## 실제 수행한 검증

| 검증 | 실제 결과 |
|---|---|
| 현재 프로젝트에서 `git apply --check --whitespace=nowarn codex-work~/g27/G27-1025.patch` | exit 0 |
| `verify/` 원본 사본에 `git -c core.autocrlf=false apply --directory=codex-work~/g27/verify --whitespace=nowarn codex-work~/g27/G27-1025.patch` | exit 0, 적용 결과와 src 사본이 파일별 바이트 단위 일치 |
| 원본 5개 파일 SHA-256 재확인 | baseline.json과 일치, 미수정 |
| 줄바꿈 검사 | 검사 스크립트 LF 유지, 저장 코드 CRLF 유지. SaveDtos의 기존 혼합 줄바꿈(LF 1곳)도 유지. BOM도 유지 |
| Unity DotNetSdk Roslyn + 본체 Bee rsp, 수정 사본 우선 사용 | Assembly-CSharp: **exit 0, 오류 0, 경고 293, 소스 376개** |
| Editor Bee 참조 + 위에서 생성한 본체 DLL + 검사 스크립트 1개 단독 컴파일 | ImplG27Checks: **exit 0, 오류 0, 경고 3**. 별도 어셈블리이므로 internal 직접 접근은 허용되지 않는 조건 |
| Unity/Play, 실제 JSON 직렬화·저장/로드, 새 회귀 검사 실행 | **미실행** |

경고는 로그에 보존했다. 검사 경고 3개는 기존 Unity 검색 API의 CS0618이다. 경고 0이라고 보고하지 않는다. 본체 전체 경고의 기존/신규 비교 빌드는 수행하지 않았다.

컴파일 경로: `E:/Unity/6000.5.8f1/Editor/Data/DotNetSdk/dotnet.exe`, `sdk/8.0.318/Roslyn/bincore/csc.dll`. 기준 rsp는 `Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp` 및 `Assembly-CSharp-Editor.rsp`. analyzer/추가 파일 및 원본 출력 경로를 제거하고 출력은 모두 g27/off로 돌렸다. 현재 프로젝트 소스를 읽고 수정된 4개 저장 파일을 src로 치환했다. UI는 읽기 컴파일 대상에 포함되지만 수정하지 않았다.

증거: `off/compile-results.json`, `off/Assembly-CSharp.log`, `off/ImplG27Checks.log`, 생성된 rsp 2개, `patch-results.json`, `baseline.json`. 재현 스크립트는 `compile.py`, `verify_patch.py`다. `prepare.py`는 원본에서 사본을 다시 만드는 초기 준비용이므로 사본을 수동 편집한 뒤 실행하면 그 편집을 덮어쓴다.

## 적용 인계

아래는 권한이 있는 담당자가 이후 실행할 명령이다. **이번 세션에서는 본 프로젝트에 적용하지 않았다.**

```powershell
Set-Location E:/Git/ant
git apply --check --whitespace=nowarn codex-work~/g27/G27-1025.patch
git -c core.autocrlf=false apply --whitespace=nowarn codex-work~/g27/G27-1025.patch
```

이 저장소는 `core.autocrlf=true`다. 기본 설정으로 격리 사본에 적용했을 때 LF 검사 파일 전체가 CRLF로 바뀌는 것을 발견했다. **줄바꿈을 보존하려면 위처럼 해당 명령에만 `-c core.autocrlf=false`를 지정한다.** 저장소 설정이나 gitattributes는 변경하지 않았다. 이 설정을 지정한 격리 적용본은 원래 혼합 줄바꿈까지 src와 일치했다.

적용 전 원본이 바뀌었으면 다시 --check하고 Save 파일의 다른 작업과 hunk 단위로 병합한다. src 파일 전체 덮어쓰기로 다른 담당자의 변경을 없애지 않는다.

다음 검증: Unity 담당자가 새 Play 세션에서 ImplG27Checks 1명/3명 전체를 실행하고 C10/C12/C13/C14를 확인한다. 특히 C12/C13은 기존 실행에서 첫 guard 뒤 코드가 실행되지 않았으므로 후속 실패 가능성이 남는다. 새 C14의 저장 필드/범위/구 저장 이관 검사도 이때 실제 실행한다. 이후 SaveRoundtripChecks와 관련 보스/로켓 회귀를 확인하고, 적용 담당자가 graphify update를 수행한다. 기존 C01~C09·C11 PASS는 사용자 제공 이전 결과이며 이번 세션의 재실행 결과가 아니다.

카카오 완료 알림은 해당 도구가 세션에 없어 전송하지 못했다.
