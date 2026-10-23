# IMPL-B3 인계: G41 저장 상태 확장 / G09 저장 검증

현재 상태: **패치·검사 준비, 원본 반영 대기**. 게임 구현 완료가 아니다.

- `Assets/_Project/Scripts/Save/SaveCatalog.cs` 쓰기가 Python/셸과 apply_patch 모두 Access denied로 거부됐다. 같은 디렉터리의 새 파일 쓰기도 거부됐다. 원본의 저장 거부 조건은 그대로다.
- 원본 수정 대신 `Temp/ImplB3/Scripts`에 소스 복사본을 작성했다. 적용 가능한 결과는 `docs/patches/IMPL-B3.patch`, 검사 스크립트는 `AgentScripts/ImplB3Checks.cs`다.
- B1/B2/B4 소유 파일, CommanderAnt.cs, 사용자 금지 Core/UI/ColonyEvents/ResourceNode, 전투 피해 처리, prefab은 수정하지 않았다. 기존 작업 트리의 다른 담당자 변경도 보존했다. 커밋/SAVE/개발 일지 작업은 하지 않았다.

## 패치 내용

- 저장 버전 18→19 이관. `hasOrder`/`hasRuntime`으로 구저장·JsonUtility의 inline null을 구분한다.
- CanSave의 이동·건설·전투·침공·거점 방어·전개 중 자동 수송 차단 제거.
- 장수: WorkerAnt 작업 단계, 건설·노드·운반·반납 대상, 가져올 창고/자원/수량, 낚시 대상/진행도, 운반·입고 예약, SoldierAnt 최종 목적지/공격 이동/공격 대상/쿨다운, 서비스·사냥·자동 시설 대기 복원.
- 건설: 기존 예정지 DTO 재사용, 남은 작업·Started·배송 완료량과 예약량 보존. BeginPickup으로 배송 원장을 재연결한 뒤 아직 집지 않은 예약량을 복원한다. 건설·수송·재료 비용을 다시 결제하는 명령은 호출하지 않는다.
- 적: 기존 카탈로그 적과 런타임 침입자/공세/반란군/이벤트/사육 생물의 위치·체력·대상·진영·습격 목표·타이머·속박·설정 보존. 반란군·이벤트·생물은 기존 Restore 결과에 전투 상태를 덧입혀 중복 생성하지 않는다. 공세/침입자는 원래 템플릿을 비용 없이 생성하고 소유 목록을 재연결한다.
- 거점: UnderAttack·공격 세력·적/동맹 목록·침공/점령 타이머 복구.
- 수송: 승무원 순서·채집 노드 순서·장수별 순번·집결 단계·화물/남은 이동 시간 보존. 파괴된 수송수단 때문에 저장 번호가 밀리는 경우도 살아 있는 수송 목록 기준으로 맞춘다.
- 소굴: 기존 EnemyColonyDto.nodes를 재사용해 확장 건물의 자원 노드까지 보존한다.
- 경로는 최종 목적지로 다시 계산한다. 보상 수령/도구 제작/탑승 명령을 재실행하지 않으며, 난수 상태는 모든 복원 이후 마지막에 적용한다.
- 자동 저장은 Update 이후 LateUpdate에서 동기 캡처·원자적 쓰기를 한다. 현재 수동 메뉴는 일시정지한 상태에서 동기 TrySave를 사용한다. **게임 진행 중 외부 TrySave 호출까지 전체 프레임 말미에 강제하려면 대기형 API와 해당 호출자의 연결이 추가로 필요하다.**

## 수정 대상 파일

- Save: SaveBuildings.cs, SaveCatalog.cs, SaveConstruction.cs, SaveDtos.cs, SavePreflight.cs, SaveSnapshot.cs, SaveSystem.cs, SaveValidator.cs.
- 신규 Save: SaveOrders.cs, SaveOrderValidation.cs, SaveRuntime.cs.
- Units: CommanderWork.cs, SoldierAnt.cs, WorkerAnt.cs.
- World: ColonyInvasion.cs, EnemyCommander.cs, LocalIncursions.cs, SettlementDefense.cs, TransportRoute.cs, WildMonster.cs.

## 확인한 결과

- Unity 6000.5.8f1 내장 Roslyn과 기존 Bee 기반 응답 파일을 사용했다. 현재 전체 게임 소스에서 위 파일들만 패치 복사본으로 대체하고, 동시 작업 신규 소스 및 ImplB3Checks.cs를 포함해 **정적 컴파일 exit 0 / 오류 0**을 확인했다.
- 기존 `BuildingConstructionSite.cs:33` CS0252 경고 1건. obsolete 경고는 응답 파일에서 억제했다.
- 응답 파일: `Temp/ImplB3/Assembly.rsp`, 출력: `Temp/ImplB3/compile.txt`.
- `git apply --check --whitespace=error docs/patches/IMPL-B3.patch` 통과. 이 명령은 원본 파일을 변경하지 않는다.
- **Unity 실행, 검사 실행, 실제 저장/불러오기, 화면 검증은 하지 않았다. 검사 통과 건수는 없다.**

## 다음 작업

1. 쓰기 가능한 세션에서 현재 작업 트리 변경을 보존한 상태로 `git apply --check docs/patches/IMPL-B3.patch`, 이어 `git apply docs/patches/IMPL-B3.patch`를 실행한다. 이후 다른 담당자가 소스를 수정했다면 충돌 부분을 검토한다. 소스 복사본으로 원본 전체를 덮어쓰지 않는다.
2. Unity 재컴파일 및 신규 .cs의 .meta 생성을 확인한다. 요청대로 prefab은 커밋하지 않는다.
3. `ImplB3Checks.Main()`을 실행한다. 임시 SaveStorage.RootOverride를 사용하며 자동 저장 설정을 비활성화했다가 복원한다. 이동·건설·전투·재료 배송 예약·전개 중 자동 수송·집결·거점 방어 왕복, 실제 원정 보상 1회 지급 후 재수령 거부, 도구 재료 중복 소비 없음, 승무원 중복 탑승 거부, 잘못된 경로 순번/전투 타이머/대상 거부를 검사한다.
4. 기존 SaveRoundtripChecks, ImplA2Checks, TransportRouteChecks, SettlementDefenseChecks를 실행한다. 반란군/공세/이벤트/생물의 목표 연결, 완료 직전 시공·제작·귀환의 실행 경계, 구버전 저장도 실제로 확인한다. 검사 fixture와 Unity 프레임 순서는 아직 실행으로 검증하지 않았다.
5. B1의 history.scoutRecruitSlot/scoutRecruitFailures는 기존 history Capture/Restore를 재사용한다. B4의 expectationTier·hygiene.temporary/travelDay·MentalBreak.Wander는 기존 personalState 경로를 재사용한다. Water는 재료 배열에 포함되지 않고 StoreBin.amounts에 저장되며, 기존 StoreBin.Apply의 길이 제한 복사는 새 슬롯을 0으로 유지한다. 이러한 구저장/실제 왕복은 별도 확인이 필요하다.

현재 환경에는 카카오톡 MemoChat 도구가 없어 완료 알림은 보내지 못했다.
