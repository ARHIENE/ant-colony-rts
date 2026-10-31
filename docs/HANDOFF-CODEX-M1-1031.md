# M1 인계 — 2026-10-31

`E:\Git\ant-wt\M1` / `codex/m1-1031`에서만 수정했다. 시작 상태의 미커밋 L1·L2 병합 변경은 보존했다. 원본 `E:\Git\ant`는 Bee 응답 파일·참조 DLL·외부 소스를 읽기만 했다. Unity 조작·캡처·커밋·Notion·개발 일지 작성 없음. 선택은 DECISIONS_1031_M1.md, 공백은 GAPS_1031_M1.md 참조.

## 항목별 판정

| 담당 항목 | 판정·구현 | 검증·남은 실행 |
| --- | --- | --- |
| 자원3종 | 전자부품79·성휘금속80·성휘합금81 끝 추가, 한국어 표시명,82종 저장·허용/필터·재료·적재 경로 연결 | 기존 SmokedFood78 번호·신규0/허용·구창고 복원·신규 재고/필터 복원·총재료 목록 검사 통과. 실물 운반/전체 복원 실행 잔여 |
| 전자부품 제조 | 기존 화학 작업대 목록 뒤에 구리2·유리1·플라스틱1·부품1→1, 작업20 추가. 기존4등급 지하 탐사 연구·전력200W 사용 | 실제 제조법 조회·기존 인덱스·배합 검사 통과. 생산 주문·배송·전력·완제품 실제 출력 실행 잔여 |
| 성휘 획득·합금 | 기존 지하 성휘 광석→가스 제련 성휘금속→성휘합금. 광석2→금속1 작업20 / 금속2·티타늄 합금1→합금2 작업28, 기존 가스1/묶음 | 유한 매장80·합금 심층 채굴기·채굴등급5·제조법/기존 인덱스 검사 통과. 발견/표본/채굴·제련·가스 투입·공급량 실행 잔여 |
| 시대별 연구 | 기존43개 뒤에6개 전용 승급 연구(ID43~48), 각 시대 등급·이전 시대 승급 연구 선행 |49노드 enum/등록 인덱스·전체 DFS 순환·각 관문의 모든 선행이 현재 등급 이하인지 검사 통과. 실제 공동 연구 완료 실행 잔여 |
| 연구소 관문 | 1→2 철40 / 2→3 강철60 / 3→4 강철80·부품10 / 4→5 티타늄80·전자부품20 / 5→6 성휘합금80·전자부품40. 각각 직전 시대 승급 연구 완료 필요 | 각 비용·전용 관문·최고등급 검사 통과. TryPay의 기존 전량 재고 확인/차감 경로 재사용. 실차감/부족 시 무차감/실제 등급 상승 실행 잔여 |
| 연구 UI | 임시 대체 안내 제거,6개 연구의 실제 승급 효과·재료 안내 추가. L1 선행/승급 합집합 예산은 그대로 새 관문/실물을 사용 | 로켓 필수12연구·연구량29100·식량/흙 각2900·특수500 회귀 통과. 해상도·실제 화면 실행 잔여 |
| 저장24·구저장 | SaveDtos24, Validator23→24 옛79칸 적재82칸 확장. 새 DTO/비트셋 없음. 완료/진행/보류 ID 목록과 창고/재료 가변복원 재사용 | 연구49개 완료·진행/보류·잘못된ID, 구연구 보존, 이관 적재값 보존·신규0·현재값 불변·빈 적재 fallback·Preflight 짧은배열/신규음수 거부 통과. 실JSON/전체 복원 실행 잔여 |

## 저장 연결 근거

- `StoreBin.TypeCount`는 enum 개수로82, 새 칸 배열도82다.64 이상 허용/차단은 기존 extraForbidden 경로를 쓴다. `StoreBin.Apply`는 먼저 현재 배열을0으로 지우고 옛 길이까지만 복사하므로 새3종 기본0이다.
- `ResourceManager.OtherMaterials/CaptureMaterials/RestoreMaterials`가 enum 기반이므로 새3종 총량을 캡처하고 짧은 옛 배열은0으로 복원한다. 기존 재료 순서는 유지된다.
- `SaveSnapshot.Capture`의 baseBin/재료 배열 및 enum 전체 cargoAmounts 캡처, `SaveBuildings`의 storeBin, Snapshot 복원의 RestoreMaterials/RestoreBins/RestoreCargoAmounts를 재사용한다. Snapshot 필드 추가는 없다.
- `SaveTransportValidation`은 enum 개수와 현재 cargoAmounts 길이 일치·비음수를 검사하며 `SavePreflight.ValidateIntegration`이 이를 호출한다. `CampaignResearch.Validate`는 Technologies.Length를 사용한다. 두 경로는 별도 하드코딩 없이 새 범위로 연결되어 파일 수정 불필요다.
- 연구 completed는 int ID 목록이고 active/partialIds도 int다. 기존 ID0~42를 유지해 크기 이관이 없다. 구저장에 새 연구를 자동 완료하지 않는다. 고등급 연구소 강등·소급 차감도 없다.
- 저장24에서 짧은 비어 있지 않은 적재는 거부한다.23의 정확한 옛79칸만 이관하며 null/빈 적재는 기존 세자원 fallback을 보존한다. 19/20/21에서 이미 현재82칸으로 확장한 적재는 그대로 둔다.

## 수정 파일(M1 범위)

- `Assets/_Project/Scripts/Data/ResourceType.cs`
- `Assets/_Project/Scripts/Core/CampaignResearch.cs`, 신규 `Core/EraPromotionResearch.cs`·`.meta`
- `Assets/_Project/Scripts/Buildings/IndustrialRecipes.cs`, `Buildings/ScienceLabUpgrades.cs`
- `Assets/_Project/Scripts/UI/GameMenuResearch.cs`
- `Assets/_Project/Scripts/Save/SaveDtos.cs`, `Save/SaveValidator.cs`
- 신규 `AgentScripts/Review1031M1Compile.ps1`, `Review1031M1Checks.cs`; 기존 `Review1031L1Checks.cs`의 변경 기획 기대값만 수정
- `README.md`, `docs/IMPLEMENTATION_BOARD.md`, `OPERATIONS_100_STATUS.md`31번, `PRODUCTION_RESEARCH_REVIEW_1028.md` 예산; 신규 M1 결정·공백·인계·검증로그
- Graphify 생성물은 별도다. prefab·log.md·changelog.md 수정 없음. Git diff에는 시작 전 L1·L2 미커밋 변경도 포함되므로 위 M1 목록과 구분한다.

## 검증 결과

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/Review1031M1Compile.ps1 -OfflineChecks
powershell -NoProfile -ExecutionPolicy Bypass -File Temp/Review1031L1Compile.ps1 -OfflineChecks
powershell -NoProfile -ExecutionPolicy Bypass -File Temp/Review1031L2Compile.ps1 -OfflineChecks
```

- Unity를 실행하지 않고 설치된6000.5.8f1의 dotnet/Roslyn만 실행했다. 원본 Bee 참조·외부 art/demo 소스43개는 읽고 M1 Temp에 복사했다. 원본 `_Project` 후속 소스는 섞지 않는다.
- 게임/검사DLL/오프라인 호스트 컴파일 오류0. **M1 103·L1 76·L2 97 오프라인 검사 통과.** 기존 게임 경고32개(CS0618/CS0252), L1/L2의 FormatterServices obsolete 경고 유지. M1 검사 ResourceType은 전부 AntColony.Data.ResourceType 완전수식명이다.
- L1의 기존 검사 삭제 없음. 임시 부품/합금 총량·로켓8→12연구 예산·현재 버전 기대값만 갱신했다. 승급 연구 선행 순회가 늘어70→76회다. L2 97검사 소스는 변경하지 않았다. Temp 사본은 worktree 검사·검사소스 기준 경로만 M1로 바꿨다.
- M1 초기 추가 실차감 검사는 네이티브 객체 없는 MonoBehaviour의 Unity null 판정 때문에 TryPay를 호출할 수 없었다. 해당 실물 검사를 오프라인 통과로 세지 않고 제거했으며 아래 장면 실행 잔여에 남겼다. 게임 코드의 null 안전성은 변경하지 않았다.
- 원시 로그: `docs/validation/M1-1031-offline.txt`, `M1-1031-L1-regression.txt`, `M1-1031-L2-regression.txt`. 임시 실행 DLL/응답 파일: `Temp/Review1031M1`, `Review1031L1`, `Review1031L2`.
- `git diff --check` 통과. Graphify AST 갱신 수행(LLM0). 최종12100노드·28155간선·532커뮤니티. 최종 갱신에서569개 이전 라벨 대비225개 이름 재지정 경고. 코드 그래프 생성물은 게임 코드와 별도로 취급한다.

## Unity 실행 잔여

`Review1031M1Checks.RunScene()`은 **진입점만 준비·미실행**이다. 시작 완료 격리 장면에서 새3자원/필터·승급 완료/진행 연구의 JSON 왕복과 실제 스냅샷 JSON→Validator→Preflight를 검사한다. 이것이 전체 씬 복원이나 실생산 성공을 뜻하지 않는다.

1. 새 게임에서 각 시대 승급 연구를 현재등급 연구소로 완료→일부 재료 부족 시 전량 미차감→충분할 때 정확한 차감·등급1상승. 고등급 구저장 유지·신규 승급 연구 연쇄 조건 확인.
2. 전자부품 주문→원료 배송→전력/정지·취소→바닥 출력·운반. 성휘층 발견→표본→합금 채굴기→금속/합금 직접 주문·가스/전력 선투입·고갈 및 타지역 공급 확인.
3. v23 구저장과 v24에서 창고3종·허용필터·운반·적재·진행 제조법·완료/진행/보류 승급 연구를 실제 저장/로드. 기존 주문 인덱스·실물/비용 중복 여부 확인.
4. 연구 화면6시대/49노드·승급 재료와 예산을1920×1080/1280×720/축소창에서 검수. 순수 연구 약3.954시간/연구비중50% 약7.908시간은 계산일 뿐 실제10시간 진행 검증은 아니다.

자동 제조법 선택·자동 환전·유체 유출·전면 그래픽·대규모 성능은 추가하지 않았다. 카카오톡 완료 도구가 세션에 없어 알림은 전송하지 못했다.
