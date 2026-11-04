# M2 전투·캠페인 인계 — 2026-10-31

- 작업 위치: `E:\Git\ant-wt\M2`, 브랜치 `codex/m2-1031`. 시작부터 포함된 원본 미커밋 L1·L2 병합 상태를 보존했다. 원본 `E:\Git\ant`는 컴파일 참조와 외부 demo 소스 복사 시 읽기만 했다.
- Unity 조작·캡처·커밋·push·Notion 변경 없음. 자동 전쟁/출정 판단 추가 없음. M1 담당 CampaignResearch·ScienceLab*·IndustrialRecipes·ResourceType·Data 자원 정의 수정 없음. 저장 버전 변경 없음.
- 선택은 `DECISIONS_1031_M2.md`, 후속 설계 공백은 `GAPS_1031_M2.md`. L2 과거 결정/공백 문서는 당시 기록으로 유지한다.

## 항목별 판정

| 담당 항목 | 판정 | 구현·검증 경계 |
| --- | --- | --- |
| AI 시대별 병력/시설 | 구현·오프라인 검증 / Unity 실행 잔여 | 기존 무기 종류로6시대 편성 반복 비율을 다르게 하고 실제 사거리·주기·방패 체력·공격을 연결. 본거지 침공 도착과 편입지 침공 생성에 적용. 시대 승급 시 현장 수비대 갱신, 부상·재능 보존. 소굴 건물 추가 장갑0~10을 실제 피격 경로에 적용, 편입지 제외. 외교 화면에 시대·주력·방어 등급 표시. 기존 병력 수/비용·최대5동 유지. 산업 시설 신설·실물 장비 재고는 범위 외. |
| 예약 후 증원 좌석 초과 | 구현·오프라인 검증 / 화면 실행 잔여 | 탑승+현재 예약−정원의 초과량을 시민 목적지/예약 화면 및 발사대별 예약 영역에 경고. 실제 `TroopCount` 변경으로 초과3석 발생, 확장·수동 취소로 해소하는 검사 통과. 예약·증원 자동 제한/해제 없음. |
| 원제작자 사망/영구 이탈 | **현행 유지 결정·명시** / 기존 취소·환급 회귀 통과 | 원제작자 고정, 미완성품 보관, 수동 취소·재주문. 일반재료 미착수100%/착수70%, 탐지 핵심물100%. 제작대 UI에 명시. 다른 제작자 `CanContinue` 거부, 원제작자 복귀 진행 보존 및 핵심물 중복 취소 환급 방지 검사. 자동 인계 구현으로 표시하지 않는다. |
| 같은 위치 신축 집 | 최소 무효화 구현·오프라인 검증 / 실파괴 실행 잔여 | `Housing.Die`·철거 완료에서 주민/입주 그룹의 주거 소유 bool을 해제. 인원·이동·임시 거처·다른 집 보존, 같은 좌표 신축 집의 자동 승계 차단. 기존 재개발 예외 유지. 철거 시작/취소에는 새 무효화 호출 없음. |

## 저장

- 신규 저장 필드·버전 인상 없음. 기존 `WildMonsterDto`의 maxHealth/damage/range/interval/detection 및 장수 roles가 이미 캡처/복원된다. AI 시설 장갑은 기존 `Civilization.era`에서 계산한다.
- 집 무효화 결과는 기존 `CivilianLocation.housing/rehousing/sheltered` 경로에 저장된다. 신규 영속ID나 Normalize/Valid 확장은 필요하지 않다. 기존 시민 상태 검증·SaveL2 구저장 Normalize/Valid 회귀 통과.
- 리뷰 수정 후에는 몬스터 런타임 복원 뒤 세력 소속 현장 수비대·침공대·방문대에 현재 세력 시대 편성을 재적용한다. 저장 체력은 새 상한보다 크더라도 그대로 유지한다. 아래 '리뷰 수정' 절 참조.
- 전체 Capture→JSON→복원→재캡처는 이번에 실행하지 않았다. 기존 저장 구조를 읽고 오프라인 회귀를 통과한 것과 Unity 저장 왕복 성공은 구분한다.

## 수정 파일 — 이번 M2가 직접 편집한 범위

| 기능 | 파일 |
| --- | --- |
| AI 구성/공격 | 신규 `World/AIEraMilitary.cs`·`.meta`, `World/WildMonster.cs`, `World/EnemyCommander.cs`, `World/LocalIncursions.cs`, `World/SettlementDefense.cs`, `World/ExpeditionSite.cs`, `World/DiplomacyManager.cs` |
| 시설 방어/주거 | `Buildings/BuildingBase.cs`, `Buildings/Housing.cs`, `Buildings/Demolition.cs`, `Core/ColonyPopulation.Shelter.cs` |
| 로켓 | `Buildings/AirshipYard.Reservations.cs` |
| UI | `UI/GameMenuDiplomacy.cs`, `UI/GameMenuPassengerReservations.cs`, `UI/GameMenuWorkshop.cs` |
| 검사 | 신규 `AgentScripts/Review1031M2Compile.ps1`, `AgentScripts/Review1031M2Checks.cs`, `docs/validation/M2-1031-offline.txt` |
| 문서 | 신규 M2 HANDOFF/DECISIONS/GAPS, `README.md`, `docs/IMPLEMENTATION_BOARD.md`, `docs/OPERATIONS_100_STATUS.md` 77행 |

게임 파일 경로의 기준은 `Assets/_Project/Scripts/`다. 기존 미커밋 파일을 통째로 덮어쓰지 말고 위 파일의 M2 변경 부분만 병합한다. `Core/ColonyPopulation.Civilians.cs`는 검토 중 변경을 되돌려 이번 M2의 최종 추가 변경은 없다. 기존 L1/L2 변경은 남아 있다.

## 오프라인 검사

명령: `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/Review1031M2Compile.ps1 -OfflineChecks`

- L2 컴파일 스크립트 방식 재사용. M2 경로 가드, 원본 Bee 참조 옵션 읽기, 게임 소스 전체 재나열. 외부 demo43소스는 원본을 읽어 M2 Temp로 복사했다. Unity Editor/CLI는 호출하지 않았다.
- 게임 DLL·Editor 검사 DLL·오프라인 실행 호스트 **컴파일 오류0**. 기존 사용 중단 API/비교 경고 및 검사 FormatterServices 경고는 남는다.
- **M2 99, L1 70, L2 97 검사 통과**. 수정하지 않은 기존 L1/L2 검사 소스를 같은 M2 게임 DLL로 실행했다. 별도 Temp 사본의 경로 검사 교체는 필요하지 않았다.
- 추가 회귀: K1 87, K2 107, 보스/원정38, 최종/포로31 통과. 수를 합산한 단일 기능 커버리지로 해석하지 않는다.
- 증거: `docs/validation/M2-1031-offline.txt`. rsp·DLL·호스트는 `Temp/Review1031M2/`. 오프라인 검사는 Unity Play 또는 Player 빌드 성공 증거가 아니다.
- `git diff --check` 통과. Graphify AST 갱신 완료(LLM0):12122노드/28230간선/524커뮤니티. 커뮤니티 라벨327개 허브명 대체 경고가 남는다. 그래프·캐시·기존 그래프 백업은 M2 내부 별도 생성물이며 게임 병합 필수 파일이 아니다.

## 실행 잔여 — 통합 담당자

`Review1031M2Checks.RunScene()` 진입점만 준비했다. 시작 완료 격리 Play 장면에서 오프라인 검사→실스냅샷 JSON→TryParse→Preflight→몬스터 편성 수치 보존을 검사한다. **미실행·실행 잔여**다. 전체 복원은 수행하지 않는 진입점이다.

1. 시대1~6 새 침공대의 실제 사거리/공격 주기·방패 생존력, 현장 수비 승급 후 부상·재능 유지, 편입 전후 소굴 장갑 차이 확인.
2. 장수를 예약한 뒤 증원해 초과를 만들고 시민 목적지/발사대 화면의 경고·초과량, 직접 예약 취소·좌석 확장 후 재진입 표시 확인. 기존 탑승 거부 유지 확인.
3. 보스 제작 중 원제작자 사망/영구 이탈 후 보관·타 장수 작업 불가·직접 취소 환급·새 주문 확인. 실물 일반재료 환급은 바닥 자원까지 대조.
4. 시민 거주 및 입주 이동 중 집 파괴/철거 완료→동일 좌표 신축→자동 입주 없음→수동 재입주 확인. 철거 취소 및 재개발 예외 확인.
5. 위 상태에서 실제 저장 전체 복원, 외교 주력 문구 줄바꿈·목록 스크롤·로켓 경고 가독성 확인. 화면을 직접 확인한 것으로 보고하지 않는다.

카카오톡 완료 알림 도구 `KakaotalkChat-MemoChat`는 이번 세션 도구 목록에 없어 발송하지 못했다. 유체 유출·전면 그래픽·대규모 성능은 제외했다.

## 리뷰 수정

- 범위: M2 worktree 내부만 수정. 원본은 컴파일 참조 읽기만 사용. Unity 조작·캡처·커밋·push·Notion 변경 없음. M1 담당 파일과 저장 DTO·버전 변경 없음.
- 저장/불러오기: `Save/SaveRuntime.cs`의 기존 `RestoreMonsters`에서 런타임 상태·대상 복원 뒤 현재 세력 시대 편성을 재적용한다. 현장 수비대는 현장 소유 세력, 침공대·방문대는 `diplomaticFactionId`를 사용한다. 보스 호위·동맹 지원군·반란·야생 개체는 제외한다.
- 체력: `WildMonster.RestoreAIMilitary`가 편성을 적용한 뒤 저장 체력을 마지막에 대입한다. 새 `maxHealth`보다 큰 저장 체력도 절삭하지 않으며 죽은 개체·부상자도 회복하지 않는다. 공격 타이머·재능은 유지하고 NavMesh 정지 거리는 재적용된 사거리에 맞춘다.
- 순번: 수비 장수의 기존 `Commander N`, 편입지 침공대의 `Invader N`을 0부터 시작하는 생성 순번으로 사용한다. `LocalIncursions`의 세력 방문대도 기존 이름 필드에 `침공군 N`을 기록해 앞 멤버 사망 뒤에도 순번을 유지한다. 저장 필드는 늘리지 않는다. 번호 없는 구저장 방문대는 저장 키의 숫자 순서로 세력별 집계하고 마지막 장수에서 파동을 구분한다. 이미 사라진 번호 없는 멤버의 원래 순번은 구저장에 남아 있지 않다.
- 현장 생성: `ExpeditionSite.MilitaryEra`에서 해당 현장의 현재 `Faction.era`를 조회해 수비대를 생성한다. 외교 관리자나 현장 세력이 없으면 1시대를 사용한다.
- 회귀 검사: `Review1031M2Checks.cs`에 1~6시대 편성 복구·0/70/175/220 체력 보존·공격 수치/역할/재능·두 자리 생성 번호·방문대 파동/생존자·제외 대상·현장 시대/승급/세력 없음 검사를 추가했다. `RunScene`에는 격리된 수비대·침공대·방문대를 실제 `RestoreMonsters`로 복원해 체력·역할·타이머를 대조하고 참조/싱글턴/방문 목록을 되돌리는 검사를 추가했다.
- 검증: `Review1031M2Compile.ps1 -OfflineChecks` 통과. 게임·Editor 검사·오프라인 호스트 컴파일 오류0. M2 **298**, L1 **70**, L2 **97**, K1 **87**, K2 **107**, 보스 **38**, 최종 **31** 통과. 기존 obsolete/FormatterServices 경고 유지. 로그: `Temp/Review1031M2/review-fixes-offline.txt`.
- `git diff --check` 통과. Graphify AST 갱신 완료(LLM0): 12139노드/28298간선/540커뮤니티. 기존 라벨과 커뮤니티가 달라 237개 라벨을 허브 이름으로 대체했다는 경고가 남는다.
- 실행 잔여: 새 `RunScene` 검사는 컴파일만 확인했고 실행하지 않았다. 실제 Unity 저장 왕복·NavMesh·화면 검증 완료를 의미하지 않는다.
