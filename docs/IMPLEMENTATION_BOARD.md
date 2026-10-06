# Codex · Claude Code 공동 구현 보드

기준일: 2026-10-07 (한국 시간). 작업 경로: `E:\Git\ant`, 브랜치: `develop`.
사용자 요청: 기획 반영 여부 점검 → 구현 수정 → 두 도구가 함께 보는 곳에 구현 위치·할 일을 기록.
이 문서는 현재 작업·인계를 위한 보드다. SAVE용 개발 일지나 기획 변경 승인 문서가 아니다.

## 작업 시작 전에

1. `log.md`, 이 문서, 실제 `git diff`를 확인한다. 보드보다 파일이 먼저 바뀔 수 있다.
2. 작업 행의 담당·상태·수정 파일을 먼저 갱신한다. **미배정은 자동 착수 지시가 아니다.** 승인된 수정과 후속 제안을 구분한다.
3. 같은 파일은 한 사람만 수정한다. 공유 파일이 겹치면 앞 작업 종료 후 이어받는다. 다른 사람이 작성한 변경을 되돌리지 않는다.
4. 상태는 `대기 / 결정 대기 / 작업 중 / 검증 대기 / 완료 / 막힘`으로 관리한다. 완료는 검증 결과가 있어야 한다.
5. Unity Play 모드·새 게임·저장 왕복 검사는 한 사람씩 실행한다. `SaveStorage.RootOverride`와 에디터 상태가 공유되므로 병렬 실행 금지. Graphify 갱신도 순차 실행한다.
6. SAVE 요청 전에는 개발 일지·캡처·커밋·push를 하지 않는다. `.prefab`·`.prefab.meta` 커밋 금지.
7. 이 파일은 같은 체크아웃에서 바로 공유된다. 다른 체크아웃/PC에는 자동 전파되지 않는다. 보드 수정 자체가 다른 도구에 메시지를 보내거나 작업을 실행하지는 않는다.

## 기획 기준

- [최신 우선 문서: 메인 게임 화면 구체화](https://app.notion.com/p/3e7c4a0ecd3181e38211f6be9c44637d)
- [기획 루트](https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd)
- [자원](https://app.notion.com/p/3d0c4a0ecd31812d80fbeceabb70193e) · [건물·방어](https://app.notion.com/p/3d0c4a0ecd3181c28b59e26337c15aba) · [연구](https://app.notion.com/p/3e5c4a0ecd3181aa8714fcb45a675818)
- 최신 우선 문서 안에도 오래된 결정이 남아 있다. 날짜·폐기/대체 명시를 확인하고 가장 최근 확정안을 적용한다. 모순이 해소되지 않으면 질문한다.
- `docs/IMPLEMENTATION_PLAN_2026-09-25.md`는 과거 지시서다. 4시대·유지비·비행선 승리 등 구내용을 현재 코드에 다시 적용하지 않는다.
- 아래 발견 사항은 **정적 코드 점검 결과**다. 전체 플레이·회귀·Player 빌드 검증 결과가 아니다.

## 현재 작업 / 충돌 방지

| ID | 우선순위 | 할 일 | 담당 | 상태 | 수정 소유 파일 |
|---|---|---|---|---|---|
| FIX-01 | 높음 | 휴식 명령을 숙소로 연결 | Codex | 막힘(검사 실패) | `Assets/_Project/Scripts/Units/CommanderDuty.cs`, `AgentScripts/DayNightChecks.cs` |
| FIX-02 | 높음 | 성체 전체 기준 노화와 인력 수량 일치 | 미배정 | 결정 대기 | 아래 후보 파일, 아직 수정 없음 |
| FIX-03 | 높음 | 가시·끈끈이 함정 구분 | 미배정 | 결정 대기 | 아래 후보 파일, 아직 수정 없음 |
| DOC-01 | 높음 | 현재 기획·README의 폐기된 설명 정리 | 미배정 | 대기 | README, 관련 Notion 하위 문서 — 범위 확인 후 |
| VERIFY-01 | 높음 | 수정 회귀 및 실제 성장 과정 점검 | 미배정 | 대기 | 검사 스크립트; Unity 실행은 단독 사용 |

### 현재 인계 상태

- **2026-10-07 SAVE 재개 결과:** 공식 Unity MCP로 연결했고 컴파일 상태 `up_to_date`, `failed=false`, 오류 없음. `DayNightChecks.Main` 실행은 65행 `rest destination is dormitory, not recreation room`에서 실패했다. 아래 연결 불가·미검증 기록은 이전 시점 기록이며, 현재는 실행 검증 실패/원인 미분류 상태다. 두 코드 파일은 미커밋 보존한다. Unity Play는 종료했으며 검사 담당은 없음.
- SAVE 문서 담당: Codex. `log.md`, `changelog.md`, `README.md`, 이 보드와 기존 공동 작업 규칙을 저장한다. 미해결 노화/함정의 새 구현은 착수하지 않았다.

- Codex가 FIX-01의 `SendToRest()`에서 `RestRoom` 검색을 `Dormitory.Assign(this)`로 교체했다. 숙소 없음/접근 불가 시 기존 제자리 휴식 경로를 유지한다.
- `DayNightChecks`에 숙소 배정·휴식 이동 목적지·숙소 없는 경우 검사를 추가했다. **컴파일·실행 미검증**, 통과로 취급하지 않는다.
- 공식 CLI는 `unity pipeline list`에서 프로젝트 에디터 PID 19552를 발견했지만 서버 포트가 없고 연결 불가다. 샌드박스 밖에서도 같은 결과였다. 에디터 프로세스는 응답 중으로 확인됐다. 재시작·패키지 변경은 하지 않았다.
- Unity 검사 실행 담당: 없음(연결 복구 전). Graphify 갱신 결과는 아래 검증 기록 참고.
- 사용자에게 아래 두 사항을 질문했으며 아직 답을 받지 않았다. **추천 선택지는 승인으로 간주하지 않는다.**
  1. 여러 파일을 바꾸는 3건 전체 수정인지, 휴식+함정 이름 연결만 우선인지.
  2. 출전·작업 중 노화는 즉시 인력 차감인지, 노화 예정 기록 후 반환 시 전환인지.
- 사용자는 이후 공동 구현 보드 작성을 요청했다. 위 질문에 대한 답으로 해석하지 않는다.

## FIX-01 — 휴식 목적지

- 기획/UI: 평시 휴식은 숙소로, 부상이 있으면 의무실로.
- 이전 구현: `CommanderDuty.SendToRest()`가 옛 휴게실(`RestRoom`)을 검색.
- 위치: `Units/CommanderDuty.cs`, `Buildings/Dormitory.cs`, `Units/CommanderSleep.cs`, `UI/CommandCard.cs`의 `RestOrTreat()`와 `UI/GameHotkeys.cs`의 직접 호출. 경로 접두사는 `Assets/_Project/Scripts/`.
- 최소 수정은 기존 숙소 배정 로직 재사용. 별도 수면/휴식 시스템을 새로 만들지 않는다.
- 완료 조건: 휴게실이 더 가까워도 배정 숙소로 이동, 숙소 없음/만석/접근 불가에서 멈춤·오류 없음, 부상 시 기존 치료 흐름 유지, 휴식 후 자율 작업 복귀.
- 검사: `AgentScripts/DayNightChecks.cs` + 필요한 휴식/치료 경계 사례. 현재 추가 검사는 전체 완료 조건을 모두 증명하지 않는다.

## FIX-02 — 노화와 일반개미 수량

- 기획: 매달 성체 3% → 늙은 개미. 현재 `ColonyPopulation.Monthly()`는 `pool.Free`만 계산한다.
- `AntPool.Total = Free + Assigned + Reserved + Working`. 분모만 `Total`로 바꾸면 대기 인력 부족 시 여전히 일부가 누락될 수 있다.
- 후보 위치:
  - `Core/ColonyPopulation.cs`, `Core/AntPool.cs`: 노화·인구/납세·동원 상한.
  - `Units/CommanderAnt.cs`, `Units/CommanderDuty.cs`: 실제 부대 병력·반환·부분 피해.
  - `Units/Workforce.cs`, `Buildings/BuildingConstructionSite.cs`: 작업 인력·건설 예약의 반환.
  - `Save/SaveSnapshot.cs`, `Save/SaveValidator.cs` 및 관련 저장 모델: 새 상태가 필요할 때만 변경.
- **선결 결정:** 동원 중인 개미가 늙는 순간 처리 방식. 임의로 전투 손실·사망·충성/기분 사건으로 처리하지 않는다.
- 완료 조건: 대기·출전·작업·건설 예약이 섞여도 승인된 노화 수만큼 한 번만 전환. 인구 보존(별도 노인 사망 제외), 음수·반환 복제 없음, 저장 왕복 일치. 원정 중 병력도 검사.
- 검사: `AgentScripts/Phase4Checks.cs`, `WorkforceChecks.cs`, `SaveRoundtripChecks.cs`의 관련 부분. 현재 Phase4의 `free0` 기반 노화 기대값도 바뀌어야 한다.

## FIX-03 — 가시/끈끈이 함정

- 확정 역할: 가시 = 반복 피해, 끈끈이 = 느려짐/속박, 폭발 = 후반 1회용.
- 현재 `FurnitureCatalog`의 가시 함정이 `BuildingKind.TrapPit`에 연결됐지만 `TrapPit.Tick()`은 피해 없이 속박 후 파손. 끈끈이는 카탈로그 이름만 있음.
- 먼저 기존 `TrapPit`을 끈끈이로 정확하게 표시한다. 기존 저장의 함정을 갑자기 피해형으로 바꾸지 않는다.
- 새 가시 기능은 범위 승인 후 구현. 피해량·반복 간격·파손/재장전 규칙은 아직 이 점검에서 확인하지 못했으므로 확정 기획을 찾거나 사용자에게 확인. 임의로 확정 수치를 만들지 않는다.
- 후보 위치: `Data/FurnitureCatalog.cs`, `Data/BuildingData.cs`, `Buildings/TrapPit.cs`, `Buildings/RuntimeBuildingTemplates.cs`, `Buildings/BuildingPlacementController.cs`, `Core/ScienceEffects.cs`, `UI/BuildScreen.cs`, `UI/GameMenuScienceBuildings.cs`.
- 저장 점검: `Save/SaveCatalog.cs`, `Save/SaveBuildings.cs`. 종류를 추가하면 enum 뒤에 붙이고 기존 번호를 유지. 같은 컴포넌트를 재사용할 경우 종류별 저장 식별이 되는지 확인.
- 완료 조건: UI 이름·카탈로그·행동 일치, 피해형/속박형 분리, 아군·공중 제외와 보스 처리, 연구 해금·건설·저장 왕복 검증.
- 검사: `AgentScripts/Stage2Checks.cs`, `FurnitureCatalogChecks.cs`, `BuildCategoryChecks.cs` + 가시 반복 피해 검사.

## DOC-01 — 기획과 현황 설명 정리

- 자원 문서: 폐기된 ‘자동 채집 없음’ 설명 제거/교체. 실제 기준은 작업표 기반 자율 채집.
- README: 일반개미 유지비·충성심·여왕방 관련 옛 설명, 연구 수와 시대, 침공 미구현 설명을 실제 코드와 재대조.
- 로켓 건조 침공은 `AirshipYard.TryBuild()` → `DiplomacyManager.AirshipConstructionStarted()` 연결이 존재한다. README의 미구현 문구만 보고 새 기능을 중복 작성하지 않는다.
- 카탈로그 항목 수·메뉴 종류 수·실제 기능 완료 수는 서로 다르다. `log.md`의 81/239 숫자로 전체 구현률을 계산하지 않는다.
- 다른 페이지에서 옛 규칙을 참조하는 곳도 함께 확인. 하위 페이지가 있는 Notion 루트를 통째로 교체하지 않는다.
- 버그 수정 과정·검증 결과는 **SAVE 때** Trouble Shooting에 기록. 지금 개발 일지를 쓰지 않는다.

## 후속 시스템 — 이번 3건 수정과 별도, 자동 착수 금지

| ID | 구현할 내용 / 현재 빈틈 | 먼저 볼 위치 | 선결 조건 · 완료 기준 |
|---|---|---|---|
| NEXT-01 | 석유·원자 시대 연구와 성장 연결 | `Core/CampaignResearch.cs`, `Buildings/ScienceLab.cs`, `Core/GameBalance.cs` | 시대별 연구 목록·승급 조건 승인. 이름만 6시대로 나누지 말고 해금/비용/생산/원정 연결 검증 |
| NEXT-02 | 배관·자동화 | `Data/FurnitureCatalog.cs`, `UI/BuildScreen.cs`, 기존 `Buildings/PowerGrid.cs` 구조 참고 | 물/석유 흐름·저장·신호 규칙 결정 후 기능별 모듈 구현. 현재 두 메뉴는 비어 있음 |
| NEXT-03 | 위생의 더러움·청소, 식량 부패/보존 | `Units/CommanderHygiene.cs`, `Buildings/HygieneFixture.cs`, `Buildings/Storage.cs`, `Units/CommanderCorpseWork.cs` | 발생·제거·부패 속도와 저장 단위 결정. 가구 없음의 결과와 저장고 효과까지 연결 |
| NEXT-04 | 전력 소비 시설·난방·조명 효과 | `Buildings/PowerNode.cs`, `Buildings/Decoration.cs`, `Units/CommanderSleep.cs` | 현재 전력 소비는 전등. 난방 반경/추위 제거, 반딧불 램프 효과의 기획 충돌 확인 후 구현 |
| NEXT-05 | 가구 재료 업그레이드·상위 방 | `Buildings/BuildingBase.cs`, `Data/FurnitureCatalog.cs`, `Buildings/RoomSystem.cs` | 재료 목록·단계·효과와 방 이름은 미정. 승인 전 임의 구현 금지 |
| NEXT-06 | 나머지 가구·마을 시설·바이옴 전용 적 | 카탈로그·건설 메뉴·바이옴/적 시스템 | 목록 등록과 기능 완성을 분리. 공공시설 효과·에셋/적 상세 확인 후 작은 묶음씩 |

## VERIFY-01 — 기능 수보다 연결 검증

- 초반 정착 → 식량/주거 → 인구 증가/세금 → 병역 → 연구 → 수송/원정 → 설계도/로켓 발사 흐름을 실제 실행한다.
- 특히 ‘한 판 10시간’은 연구량 상수만으로 증명되지 않는다. 인력 배율·다중 연구소·이주/식량 수지·시대 승급을 포함해 측정한다.
- 변경과 직접 관련된 검사부터 실행한다. 실패를 수정한 뒤 저장 왕복·핵심 회귀를 순차 실행. Player 빌드와 장시간 플레이 미실행은 그대로 표시한다.
- 실행 중 사용자 저장 슬롯을 덮어쓰지 않는다. 검사 임시 저장 경로를 쓰고 종료 시 원상 복구한다.
- 컴파일 성공, 검사 통과 수, 실제 명령/대상, 남은 제한을 아래에 적는다. 과거 log/README의 통과 기록을 이번 결과로 복사하지 않는다.

## 검증·인계 기록

| 날짜 | 담당 | 대상 | 결과 | 다음 단계 |
|---|---|---|---|---|
| 2026-10-07 | Codex | FIX-01 | 코드 1곳 변경·DayNight 회귀 검사 추가. Unity 연결 불가로 컴파일/실행 미검증 | Pipeline 연결 복구 후 검사 |
| 2026-10-07 | Codex | 공동 보드 | 기획 대비 불일치·파일 위치·완료 조건·결정 대기 구분 | 작업자는 착수 전에 담당을 표시 |
