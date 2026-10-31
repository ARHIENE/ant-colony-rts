# L1 인계 — 2026-10-31

`E:\Git\ant-wt\L1` / `codex/l1-1031`에서만 수정했다. 원본 `E:\Git\ant`는 컴파일 참조·외부 소스 읽기만 했다. Unity 조작/캡처/커밋/Notion/개발 일지 작성 없음. 자동 제조법 선택·자동 환전·자동 전쟁 결정 추가 없음. 선택 근거는 DECISIONS_1031_L1.md, 새 공백은 GAPS_1031_L1.md.

## 항목별 판정

| 항목 | 판정·반영 내용 | 수정 파일 | 검사·실행 잔여 |
| --- | --- | --- | --- |
| 100항목31 | 실물 승급 연결·기존 연구 임시 관문. 임시 식량/흙 비용 교체. 1→6 철40·강철140·부품70·티타늄80·티타늄 합금80. 연구 상세에 승급 연구 합집합/1동 재료비 표시 | Buildings/ScienceLab.cs·ScienceLabUpgrades.cs, UI/GameMenuResearch.cs | 단계별 비용·순환 없는 선행 경로·로켓8연구/연구량23600·식량/흙2150·특수390 계산 통과. 실재고 차감/실제 승급/화면 실행 잔여. 전용 전자부품·성휘합금·시대 승급 연구는 미구현, 대체안 화면 명시 |
| 사막 세수↔학당 흙 | 기존 흙 고정 유지·학당에 수입 의존/방법 안내. 모래·석재·점토 대체 없음 | UI/GameMenuCivicServices.cs | 기초 상점 흙 미판매 코드 검사. 실제 타지역 채집·수송·외교 흙 공급 및 안내 화면 실행 잔여 |
| 재고 유지 운반분 | 본거지 창고/바닥/실제 보유 완제품 합산. 원료 배송·수송선 적재·원정/타지역/탑승·사망 제외. 예약을 추가하지 않음 | Core/ProductionStock.cs, Buildings/Kitchen.cs, Units/WorkerAnt.cs | 실제 운반 속성/합산 메서드로 다른품목·원료배송·수송적재·외부범위 제외, 입고 후 운반0, 소수 총량 검사. 창고→운반/바닥→운반→입고 실프레임/병렬 생산은 실행 잔여 |
| 어육·과실 원산지 | K1 범용 Food 최소안 유지·요리 화면에 원산지 제한 없음 명시 | UI/GameMenuKitchen.cs | 훈연식이 통합 Food를 쓰는 기존 제조법 검사. 원산지 분화/생선 전용 가공은 미구현으로 명시. UI 실행 잔여 |
| 시민 노년 자연사 | 연간 OldDeath15%·소수 이월. 올해 노년 전환 전부터 있던 미동원 노년 대상. 노년 병역 편입 시 old 유지·agingDue 풀 정산 재사용 | Core/ColonyPopulation.cs·Ages.cs·NaturalDeaths.cs·Economy.cs·Military.cs, UI/GameMenuPopulation.cs, Save4개 파일 | 100명 중 정산대기20→12명 사망, 소수 노년7회차 사망, 전부 동원 시 보존/복귀 후 판정, 군역인구 중복차감 방지, 이월 비정상값 거부 통과. 실제 동원/복귀/손실·시간분할·구저장 씬 복원은 실행 잔여 |
| 교육 수혜 집단 | 졸업자 개인이 아닌 정착지 전체 납세자1계절 보너스. 다음 성년 전환 때 교체·비중첩·만료 명시 | UI/GameMenuCivicServices.cs | 기존 보너스/저장/교육 계산 변경 없음. 실제 성년 전환→납세/민심→만료·UI는 실행 잔여 |
| 산업 대기전력 | 소비 정책 유지·산업 가공대200W는 무주문/정지/원료 대기에도 고정, 정유기도 대기200W 표시 | UI/GameMenuProcessor.cs·WorkTargetPanel.cs | 기존 Demand/전력 우선순위/유체 선투입/발효 코드 변경 없음. 실제 화면·정지/정전 상태는 실행 잔여 |
| 운영비 전체 중단 | 기존 종류별 전체 중단 유지. 합산 복구 재고·현재 부족액 직접 표시, 원래 소수 부채/매일 아침 재시도·학당 우선은 툴팁 | Core/ColonyPopulation.Economy.cs, UI/GameMenuCivicServices.cs | 식량 학교+진료소 합산·부족액·소수 부채 비올림 검사. 복수동 실제 중단/재고 회복/다음 아침 재개·화면 실행 잔여 |
| 캐러밴 두 방문 상태 | 같은 행렬의 기초 상점/외교 상점으로 구분. 기초 상점에서 두 개방 상태·남은 시간 별도 표시 | World/CaravanSupplies.cs, UI/GameMenuDiplomacy.cs | 저장 시계/실제 개방/상품 규칙 변경 없음. 한쪽만 열린 저장·월드맵 전/후 상태·화면은 실행 잔여 |
| 교역 협정 정기 교환 중단 | 문명별 중단/재개 토글·설정 저장. 협정/정보 공개/협상 가격 혜택 보존. 성공 거래가 없으면 호감도+1도 없음. 미거래 회차 소급 없음 | World/DiplomacyRules.cs·DiplomacyManager.cs·DiplomacyPeriodicTrade.cs, UI/GameMenuDiplomacy.cs, Save4개 파일 | 실제 설정·가격 계산 경로/중단·재개·만료·전쟁·소멸·미등록 상대 거부 검사. 실제 월별 자원 변화/저장 복원/UI는 실행 잔여 |

## 저장23·구저장

- 새 필드는 `ColonyPopulation.State.oldDeathRemainder`(double)와 `Civilization.periodicTradePaused`(bool). 별도 DTO 중복 필드 없이 SaveDtos의 중첩 상태로 저장한다.
- SaveDtos.CurrentVersion23, SaveSnapshot의 기존 Capture/Restore 경로가 두 필드를 포함한다. SaveValidator22→23에서 이월0·정기교환 허용(false)으로 이관한다. 현재23에서는 값이 덮이지 않는다.
- SaveValidator의 ValidCivicState·SavePreflight가 자연사 이월 `[0,1)`을 검사한다(NaN/무한대 포함 거부). bool은 추가 수치 검증이 필요 없다. 실제 설정은 소속 문명에만 허용한다.
- 기존 ages 배열 없는 저장의 Restore 이관을 유지한다. old에 포함된 agingDue는 자연사에서 제외하며 복귀/손실 때 기존 풀 정산을 재사용한다. 과거 저장에 이미 성체로 전환된 노년의 출처는 복원할 정보가 없으므로 소급 추정하지 않는다.
- 기존 고등급 연구소는 강등·소급 차감하지 않는다. 다음 승급만 새 조건을 적용한다.

## 검증 결과

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/Review1031L1Compile.ps1 -OfflineChecks
```

- Unity 에디터를 실행하지 않고 설치된6000.5.8f1의 Roslyn/dotnet만 사용. 원본 Bee 응답 파일/참조 DLL을 읽고 L1 게임 소스를 전부 재나열했다. 원본 `_Project` 후속 소스는 섞지 않는다. 외부 art/demo 소스43개만 원본에서 L1 Temp로 복사했다.
- 게임·별도 검사 DLL·오프라인 실행 호스트 **컴파일 오류0**, **실제 게임 DLL 오프라인70검사 통과**. 기존 게임 경고32개(CS0618/CS0252), 검사 호스트 FormatterServices 경고1개. 새 의존성 설치 없음.
- 검사 대상은 비용/선행 경로/합산·실제 운반 속성·정기교환 설정 및 가격 계산·노년/군역 순수 계산·실제 SaveValidator 이관과 SavePreflight 잘못된 이월 거부다. Unity 네이티브 경로를 검사한 것으로 표시하지 않는다.
- `Review1031L1Checks.RunScene()` 진입점은 준비만 했다. 격리된 시작 완료 게임·접촉한 문명1개가 필요하며 실스냅샷→JSON→SaveValidator→SavePreflight 및 신규 두 필드 보존을 검사한다. **실행 잔여**. 전체 씬 복원/실제 작업/화면은 이 진입점과 별개다.
- 초기 오프라인 검사에서 네이티브 생성 없는 Unity 객체의 동일 식별값 때문에 불필요한 Distinct가 다른 운반자를 합쳤다. 실제 Active 목록은 개체별1회 등록이므로 중복 제거를 제거하고 서로 다른 실물 합산·원료/수송 제외·입고 후 운반0을 확인했다. 실패 시 Windows 오류 보고 창에 머무르지 않도록 오프라인 호스트는 예외를 잡아 종료코드1을 반환한다.
- Windows PowerShell5.1에서 한국어 스크립트를 읽도록 새 ps1은 UTF-8 BOM으로 저장했다.
- 원시 로그 `docs/validation/L1-1031-offline.txt`, 임시 응답/실행 DLL `Temp/Review1031L1/`. `git diff --check` 통과. Graphify AST 갱신은 별도 생성물이며 게임 통합 필수 파일이 아니다.
- Graphify 최종11974노드/27818간선/554커뮤니티·LLM0. 갱신 전544개 라벨과 커뮤니티 수가 달라231개 이름을 허브 기준으로 바꿨다는 경고가 있다. 의미 라벨 재작성은 하지 않았다. 집계 HTML·그래프·캐시/백업도 L1 안에서만 생성됐으며 게임 파일 통합 목록과 구분한다.

## 통합 시 실행 잔여

1. 새 게임에서 흙벽 공법 전/후1→2 승급, 각 단계 재료 일부 부족 시 전량 미차감, 충분할 때 정확히1회 차감·등급 상승, 구저장 고등급 유지. 과학 상세/시설 화면의 긴 재료 이름·현재 최고 연구소·완료 연구 예산 확인.
2. 부엌/가공대 완제품 바닥→장수 보유→입고 전 과정과 복수 생산대에서 KeepStock 중복 생산 확인. 원료 배송/원정 적재/다른 지역/부패/사망 뒤 실물 이동을 함께 확인.
3. 시민 연간 경계 직전/직후, 큰 Tick/분할 Tick/일시정지·배속, 노년 병역 편입→귀환/전사/예약 손실, 저장 중 agingDue+이월 보존. 연간 노년 전환·교육 갱신/계절 만료·납세를 함께 확인.
4. 사막 학당 흙 공급·소수 운영비 축적·복수동 중단/재개, 두 서비스의 공유 식량 복구 안내와 툴팁 확인. 산업/정유 정지·무주문·원료 대기 시 표시와 실제 Demand 일치 확인.
5. 기초 상점만 개방/외교 상점만 개방/둘 다 대기, 월드맵 해금 전·후 확인. 정기교환 중단→월 경계→재개→다음 월 경계, 전쟁/만료·재협정 및 v22/v23 저장 JSON/전체 씬 복원 확인.
6. 1920×1080·1280×720 및 축소 창에서 연구 상세/시설 버튼·공공서비스 복구 줄·툴팁·캐러밴 상태·외교 토글/보류협상 목록 넘침 확인. 이번에 화면을 보거나 검증하지 않았다.

## 파일 목록

- Buildings: `ScienceLab.cs`, 신규 `ScienceLabUpgrades.cs`, `Kitchen.cs`.
- Core: 신규 `ProductionStock.cs`, 신규 `ColonyPopulation.NaturalDeaths.cs`, `ColonyPopulation.cs`, `ColonyPopulation.Ages.cs`, `ColonyPopulation.Economy.cs`, `ColonyPopulation.Military.cs`.
- Units: `WorkerAnt.cs`(본거지 재고 귀속 읽기 속성).
- World: `DiplomacyRules.cs`, `DiplomacyManager.cs`, 신규 `DiplomacyPeriodicTrade.cs`, `CaravanSupplies.cs`.
- Save: `SaveDtos.cs`, `SaveSnapshot.cs`, `SaveValidator.cs`, `SavePreflight.cs`.
- UI: `GameMenuResearch.cs`, `GameMenuCivicServices.cs`, `GameMenuKitchen.cs`, `GameMenuPopulation.cs`, `GameMenuProcessor.cs`, `WorkTargetPanel.cs`, `GameMenuDiplomacy.cs`.
- 신규 C#4개에 각각 `.meta`4개 추가. `.prefab`/`.prefab.meta` 변경 없음.
- 검사: 신규 `AgentScripts/Review1031L1Compile.ps1`, `AgentScripts/Review1031L1Checks.cs`, 검증 로그.
- 문서: `README.md`, `docs/IMPLEMENTATION_BOARD.md`, `OPERATIONS_100_STATUS.md`31행, `PRODUCTION_RESEARCH_REVIEW_1028.md`연구 예산, `GAPS_1029_J1.md`현재 후속 참조, 신규 결정/공백/인계3개. log/changelog 수정 없음.

카카오톡 완료 도구가 세션에 제공되지 않아 알림은 전송하지 못했다. Notion 최신화/개발 일지/Unity 캡처는 이번 사용자 금지에 따라 수행하지 않는다.
