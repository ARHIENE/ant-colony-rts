# EXPED-PAUSE-1103 — 원정 일시 중지 인계

- 담당: Codex / 2026-11-03 / E:\Git\ant 원본 트리(develop).
- 사용자 지시: 원정 추가 기획 전까지 신규 플레이어 출발만 막는다. 코드·저장 데이터 삭제 없음.
- 상태: 구현·오프라인 컴파일 완료. Unity 실행·화면 검증은 Claude 담당이며 아직 미실행. 실제 검사 통과로 보고하지 않는다.

## 단일 스위치와 복구

`Assets/_Project/Scripts/World/ExpeditionRules.cs`:

```csharp
public static bool Paused { get; private set; } = true;
public const string PauseReason = "원정은 추가 기획 전까지 잠시 닫혀 있습니다";
```

초기값 한 줄의 `true`를 `false`로 바꾸면 원래 조건과 UI가 복구된다. private setter는 검사에서만 리플렉션으로 접근한다. 저장 DTO·저장 버전·영구 상태에는 스위치를 추가하지 않았다.

## 차단 경로와 유지 경로

| 경로 | 처리 |
| --- | --- |
| `ExpeditionTransport.DepartureBlock` | 다른 조건보다 먼저 공통 사유 반환 |
| `ExpeditionTransport.TryDepart` | 도보·차량·항공 모두 최초 가드로 거부, 목적지/승무원/출발 상태 변경 전 |
| `TalentAssessment.TryDispatch` (`World/ExpeditionInvestigation.cs`) | 인재 조사 파견 거부, 후보 추첨·난수·미션 등록 전 |
| `GameMenuController.WalkingFormation` | 설명·편성 대신 닫힘 사유와 닫기 표시 |
| `WorldMapManager.CreateWalkingParty` | UI 우회 편성도 거부 |
| `TransportRoute.TryStart`와 Home 상태 Tick | 신규 자동 경로 시작 및 저장된 자동 경로의 본거지 재출발 거부, 사유 표시 |
| `ExpeditionTransport.TryContinueTo` | 거점 간 신규 출발 거부 |
| `StartWithdrawTravel(next)` | 구저장에 예약된 경유는 다음 거점 대신 본거지로 귀환 |
| `AirshipYard.TryDepart` | ~~로켓 발사 확정 차단~~ → **Claude가 되돌림(2026-11-03): 로켓 발사는 원정이 아님. 차단 안 함** |

자동 수송은 장수와 수송대를 월드맵 거점으로 보내는 원정의 자동화이므로 차단 범위에 포함했다. 현지에서 마무리된 자동 수송은 다음 경유지로 가지 않고 기존 TryReturn으로 귀환한다. 경로 설정은 삭제하지 않으며 귀환 후 다음 사이클만 대기한다.

`TryReturn`·철수 요청/취소·긴급 귀환·이동 Tick·착륙·하역·조사 중단/보고·저장 복원은 잠그지 않았다. 이미 Outbound인 저장은 현지 도착 후 귀환할 수 있고 Returning 저장도 귀환한다. 기존의 착륙 위협·조종사·수리·탑승 조건은 그대로 적용된다.

grep으로 `TryDepart`, `TryDispatch`, `CreateWalkingParty`, `TryContinueTo`, `ExpeditionState.Outbound`, `SetEmbarked`, `Landing`, `Garrison` 연결을 확인했다. 정찰 초소 `ScoutPost.TryDispatch`는 목적지 없는 영입 타이머이며 월드맵 거점 파견이 아니어서 유지했다. 기분 이탈·반란(`CommanderDeparture`/`DiplomacyRebels`), 거점 포로 구출·본거지 귀환은 플레이어 원정 출발이 아니므로 유지했다. `CreateTransport`는 저장 복원에서도 쓰므로 잠그지 않았다.

UI: 월드맵 출발·도보 편성·자동 수송 시작을 비활성화하고 계획/경로 안내에 닫힘 사유를 표시한다. 외부 인재 조사는 신규 담당/파견 UI를 숨기되 기존 미션 상태·중단/귀환·보고서를 유지한다. GoalChain의 저장 완료 비트는 유지하고 이후 목표/보상/다음 안내가 출정을 권장하지 않게 표시만 바꾼다. CampaignNextSteps·CommandCard·외교의 원정 권장 문구를 교체했다. (로켓 발사 확인 화면 차단은 Claude가 되돌림.) 월드맵 정보·현지 보기·회수·귀환은 유지한다.

## 진행 영향 — 보고만, 대체 경로 구현 없음

1. **[정정: 발사 차단은 되돌림 — 아래 2번 보스 사슬 문제는 그대로]** ~~로켓 승리 경로: 차단됨.~~ `AirshipYard.TryDepart`는 이름과 달리 현재 코드에서 로켓 발사 확정이다. 요구사항 2에 명시되어 같은 스위치를 적용했다. 부품·핵심물이 모두 있는 기존 저장도 새 발사 확정을 할 수 없다. 이미 확정/발사된 저장의 런타임 처리는 변경하지 않았다.
2. **새 게임의 필수 보스 사슬도 차단.** 새 원정으로 A/B/C/D를 격파하고 핵심물을 가져오는 경로가 막힌다. `BossProgress.Recipe/DetectionBlock`: A 반입→B/C 탐지, B와 C 반입→D 탐지. D 반입은 `CampaignResearch.AcquireBlueprint/BlockReason(Engine)`의 엔진 연구 조건이며 D 핵심물은 로켓 가동 시 소비된다(`TryConsumeRocketCore`). 따라서 발사 메서드 차단과 별개로도 신규 게임의 정상 로켓 경로가 끊긴다. 이미 현지/화물에 있는 핵심물의 회수·반입과 기존 본거지 재고는 보존한다.
3. **보스 고유 장비·재공략 보상 공급 제한.** 새 원정으로 A 고내구 장비, B 방열 장비, C 범위 공격 장비, D 핵심물·현지 전리품을 추가 획득할 수 없다. 이미 현지에 있는 장수의 전투/회수와 기존 재고·회복 제작 조건은 변경하지 않았다.
4. **월드맵 자원·교역·정복 확장 제한.** 신규 자원지 방문, 새 현지 교역, 적 거점 공략/편입/탈환, 새 세력 도착 접촉, 거점 수송·자동 물류 증편이 막힌다. 이미 확보한 편입지의 생산/정보·기존 현지 행동·기존 외교는 유지한다.
5. **외부 인재 조사·원정 기반 영입 제한.** 새 외부 조사 보고서와 새 거점 방문을 전제로 하는 인재 접촉·포로 구출/영입 기회가 제한된다. 장수 모집 전체가 막히지는 않는다. 정찰 초소의 별도 영입 파견, 기존 포로 회유·양육 등은 유지한다.
6. **표본 연구는 조건부 병목이며 전면 차단은 아님.** 원정으로만 새로 확보하려던 미보유 표본은 가져올 수 없다. `ResourceManager.DeliversSample`은 채집/원정/교역/보상도 인정하고, `SupplyFacility.Underground`는 본거지 지하 표본을 기록한다. 기존 표본과 본거지 채집·지하 조사/채굴 연구는 유지된다. 새 게임 `StellarSupply.EnsureHome`는 성휘광석 최소161·원유·천연가스·티타늄 공급을 마련하므로 로켓용 일반 광물/연료가 모두 원정 전용이라고 단정할 수 없다. 구저장의 매장량·교역 재고에 따른 병목은 별도이다.
7. **목표 사슬 정체.** 새 게임은 첫 원정 및 이후 보스 반입 단계가 완료되지 않는다. 완료 비트를 건너뛰거나 가짜 달성시키지 않고 운영/웨이브 안내로 표시만 바꿨다. 웨이브 수성 자체는 이번 스위치와 연결하지 않았다.

## 검증

- Unity 6000.5.8f1 내장 SDK 8.0.318 Roslyn 사용. Unity 실행 없음.
- Bee `Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp`의 옵션/참조를 복사하고 출력 DLL을 Temp로 변경, `Library/` 경로를 절대 경로로 변환했다. Assets의 현재 runtime 소스 516개로 목록을 갱신했다(Editor 및 독립 asmdef/asmref 소스 제외, 외부 기본 어셈블리 소스 포함).
- 게임 응답 파일: `Temp/ExpeditionPause1103/a.rsp`; 출력 `Assembly-CSharp.dll`; 로그 `game-compile.log`.
- 검사: Bee `Assembly-CSharp-Editor.rsp`의 옵션/참조에서 소스와 기존 Assembly-CSharp 참조를 제거하고 위 출력 DLL을 `-r`로 추가. 검사 소스는 `AgentScripts/ExpeditionPause1103Checks.cs` 한 개만 포함(RunChecks.cs 등 최상위 실행 파일 제외).
- 검사 응답 파일: `Temp/ExpeditionPause1103/checks.rsp`; 출력 `ExpeditionPause1103Checks.dll`; 로그 `check-compile.log`.
- **게임 컴파일 exit 0, 오류 0 / 검사 컴파일 exit 0, 오류 0.** 게임의 기존 CS0252·CS0168·CS0618 경고 33개는 남아 있다. 검사 경고는 0개다.
- `git diff --check` 대상 변경 파일 통과. 금지 파일(WaveDefense·LocalIncursions·WildMonster*·WaveBalance1103Checks)과 prefab/scene/ProjectSettings는 수정하지 않았다. 다른 담당자의 기존/동시 변경은 보존했다.
- `AgentScripts/ExpeditionPause1103Checks.Main()`은 기존 PlayFirstWave 새 게임 패턴(임시 SaveStorage.RootOverride·설정 복구)을 따른다. 기본 스위치/정확한 문구/다른 조건보다 우선/부작용 없는 거부, 도보·차량·항공·조사·자동 경로·거점 간 경유·로켓의 차단과 해제, Outbound/Returning 복원 상태의 도착·철수·귀환, 구저장 경유의 귀환, 안내 억제를 검사한다. internal 접근은 리플렉션을 사용한다. 실제 실행은 Claude가 새 Play에서 수행할 것.
- 컴파일 성공은 Unity 검사/화면 검증 성공을 뜻하지 않는다. 캡처·일지·SAVE·커밋·Notion 수정 없음. 카카오톡 완료 알림 도구는 현재 세션에 제공되지 않아 발송하지 못했다.

## 변경 파일

- `Assets/_Project/Scripts/World/ExpeditionRules.cs`
- `Assets/_Project/Scripts/World/ExpeditionTransport.Planning.cs`
- `Assets/_Project/Scripts/World/ExpeditionTransport.cs`
- `Assets/_Project/Scripts/World/ExpeditionInvestigation.cs`
- `Assets/_Project/Scripts/World/WorldMapExpeditions.cs`
- `Assets/_Project/Scripts/World/TransportRoute.cs`
- `Assets/_Project/Scripts/World/ExpeditionWithdraw.cs`
- `Assets/_Project/Scripts/Buildings/AirshipYard.cs`
- `Assets/_Project/Scripts/UI/GameMenuWalking.cs`
- `Assets/_Project/Scripts/UI/GameMenuInvestigation.cs`
- `Assets/_Project/Scripts/UI/WorldMapPanel.cs`
- `Assets/_Project/Scripts/UI/GoalChain.cs`
- `Assets/_Project/Scripts/UI/GoalChain.Progress.cs`
- `Assets/_Project/Scripts/UI/CampaignNextSteps.cs`
- `Assets/_Project/Scripts/UI/CommandCard.cs`
- `Assets/_Project/Scripts/UI/GameMenuRocket.cs`
- `Assets/_Project/Scripts/UI/GameMenuDiplomacy.cs`
- `AgentScripts/ExpeditionPause1103Checks.cs` (신규)
- `docs/IMPLEMENTATION_BOARD.md` (최상단 소유/완료 상태)
- `README.md` (현재 차단 상태)
- `docs/HANDOFF-CODEX-EXPEDPAUSE-1103.md` (신규)

## Graphify

`graphify query`로 출정 관계를 우선 확인했고 `graphify update .` exit 0. AST 전용(LLM 토큰/API 비용 0), 18,245 nodes / 39,811 edges / 887 communities. graphify-out의 graph.json·graph.html·GRAPH_REPORT.md 및 자동 인덱스/캐시/백업을 갱신했다. 커뮤니티 구성이 바뀌어 264개 라벨이 hub 기준 이름으로 자동 변경되었다는 경고가 있다(LLM 재라벨링 미실행). 그래프 생성물은 별도 커밋하지 않았다.
