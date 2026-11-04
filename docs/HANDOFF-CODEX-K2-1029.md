# Codex K2 인계 — 2026-10-29 배정 / 2026-10-31 작업

- 작업 폴더: `E:/Git/ant-wt/K2`, 브랜치 `codex/k2-1029`.
- 필수 3건 구현. 선택 범위인 milestones 압축·원정 식량 예상 통합은 착수하지 않음.
- 원본·K1/Claude 담당 코드는 수정하지 않음. 기존 Claude·J1·J2 미커밋 기준선 보존. Unity 실행·캡처·개발 일지·Notion·커밋·push 없음.
- 패치: `docs/patches/K2-1029.patch`. **세션 시작 미커밋 병합본에 대한 K2 추가분만** 포함한다. HEAD 전체 diff는 기존 병합본까지 중복 적용하므로 사용하지 않는다.
- 패치 검증: 22파일, 기준선에 정방향 적용 검사·적용 후 전체 파일 대조·현재본에서 역방향 적용 검사 통과. 기존 미커밋 병합분·Graphify·Temp·prefab 변경은 패치에 포함하지 않았다.

## 항목별 판정·변경

| 항목 | 판정 | 수정 | 검사·남은 것 |
| --- | --- | --- | --- |
| 1. 지상 AoE 발동 중 저장 | 실제 공백: WaitForSeconds의 진행/조준·명중 상태가 저장에 없음 | BossAoEState로 위치/수평 방향/남은 게임 시간/명중 여부/명중 후 표시를 보존. 3종 Capture/Restore, MonsterDto·SaveSnapshot·SavePreflight 연결. 명중 전 플래그 설정으로 재판정 차단. null/빈 인라인 객체는 구저장 유휴 | Main 상태 기계·오염 입력·복사 독립성 통과. RunScene 실제 경고 중 JSON·전체 저장복원/정지 중 잔여/방향·위치 보존/명중 후 복원 작성, 미실행. 실제 경고 표시·실피해 중복 여부는 Claude 확인 |
| 2. 편입지 간 이동 거리 | 실제 오류: 목적지의 본거지 띠 시간으로 동일 띠 이웃/반대편이 같음 | StartWithdrawTravel에서 Site 변경 전 실제 양 끝 좌표의 거리를 계산. 도보 진형 보정·기존 이동 배속 사용. 본거지 왕복 기존식 유지. 저장 타이머는 재계산하지 않음 | Main 거리 차이·대칭·0거리·운송수단별 계산 통과. RunScene 실제 출발 코드·Returning 7.25초 전체 저장복원 작성, 미실행 |
| 3. 포로 안전 귀환 대기 | 실제 공백: 적 잔류·경계 진입 실패 모두 원인/수동 대안 없이 대기 | 적 잔류/진입 불가/명부 준비/재개 대기 구분. 기존 포로 창에 진입 기준점과 X/Y/Z 대체 지점·초기화. 본거지 안 NavMesh(1m), 건물 점유, 기존 적 잔류+지정점25m 위협 확인. 외교 State에 설정 저장, 구저장 false는 기본 경계. 수용소 없는 진입 추가 | Main 유한 좌표/구저장 기본값 통과. RunScene 불가 지점 대기·유효 지정/JSON복원·실제 Arrive/중복 방지·수용소 없이 창 열기 작성, 미실행. NavMesh·위협 fixture/화면 폭은 Claude 실행 잔여 |

## 검증 근거

- 실행: `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/Review1029K2Compile.ps1 -OfflineChecks`.
- 공식 Unity 설치의 dotnet/csc와 원본 Bee 응답 파일 참조를 사용한다. `Assets/_Project` 소스는 전부 K2에서 새 파일 포함 열거한다. 원본 응답 파일에 새 Claude 파일이 추가되어도 K2에 섞지 않는다.
- 첫 시도는 원본 응답 파일의 새 훈련 partial을 읽어 Barracks 중복 선언 오류가 났다. 검사 도구의 소스 열거만 수정해 해소했고 Barracks 파일은 변경하지 않았다.
- 게임 DLL·K2 검사 DLL·오프라인 호스트 컴파일 오류 **0**. 기존 Unity 폐기 API 경고는 남음.
- 실제 K2 게임 DLL을 참조한 `Review1029K2Checks.Main()` **107 PASS**. Unity 스텁·복제 계산식으로 통과시킨 결과가 아니다.
- 로컬 근거: `Temp/K2-compile-run.txt`, `Temp/Review1029K2/game.rsp`, `checks.rsp`, `Assembly-CSharp.dll`.
- `git diff --check`는 K2 수정 코드에서 통과. Graphify AST 갱신 완료: 11,748노드·27,199간선·532커뮤니티, LLM 사용 없음. 그래프 산출물은 코드 패치에서 제외한다.
- 요청한 `git -C E:/Git/ant-wt/K2 add -N .`은 공용 `E:/Git/ant/.git/worktrees/K2/index.lock` 쓰기 권한 거부로 실패했다. K2 내부 임시 저장소의 기준선 인덱스를 사용해 `git add -N .`·`git diff`로 동일한 K2 추가분 패치를 만든다. 임시 저장소에서도 커밋은 하지 않는다.

## Claude 실행·UI 연결 요청

1. 미커밋 병합본에 K2 패치를 적용하고 게임/검사 컴파일을 다시 확인한다. SaveSnapshot·SavePreflight·DiplomacyManager·README·IMPLEMENTATION_BOARD는 다른 담당 변경과 해당 줄만 병합한다.
2. **격리 새 게임**에서 본거지에 장수1명(건물과 떨어진 빈 지점)·수송대1개·살아 있는 보스가 있는 상태로 `Review1029K2Checks.RunScene()`을 호출한다. 메모리 스냅샷을 finally에서 복원하며 저장 파일 I/O는 없다. Unity CLI용 진입점이 Main만 허용되면 임시 래퍼에서 RunScene을 호출한다.
3. 3종 경고 중 저장 → 로드 → 원래 위치/방향·잔여·단1회 실제 피해를 확인한다. Runtime 함수는 경고 및 명중 플래그 왕복을 검사하며 실제 피해량·물리 콜라이더 검수는 별도로 한다.
4. 적 잔류 fixture에서 귀환이 막히는지, 지정점 주변 위협/건물 설치 뒤 대기로 돌아가는지, 적 제거 후 기존 귀환 장수가 한 번만 도착하는지 확인한다. 경계 NavMesh가 없는 fixture에서도 내부의 빈 NavMesh 지점 지정으로 회복되는지 확인한다. 장시간 플레이가 필요한 검사가 아니다.
5. 메뉴 연결은 구현됨: **의료·관리 → 포로 → 안전 귀환·진입점 관리**, 수용소 선택 창에도 같은 귀환 구역. 별도 새 창 추가 불필요. 좌표 입력3개와 대기 행은 기존 스크롤 구역에 배치한다. 1920×1080·1280×720에서 스크롤 도달·입력/버튼·문구 겹침 검수 필요. 캡처는 사용자 SAVE 때만 수행한다.
6. `docs/DECISIONS_1029_K2.md`의 추천 수치/정책은 사용자 미확인이다. SAVE 때 Notion의 관련 기획·참조 페이지를 함께 대조/반영하며 부모 페이지 replace_content는 금지한다.

## 수정·신규 파일 전체 목록

기존 수정 15개:

- `Assets/_Project/Scripts/Boss/AoE/BossCircleAoE.cs`
- `Assets/_Project/Scripts/Boss/AoE/BossConeAoE.cs`
- `Assets/_Project/Scripts/Boss/AoE/BossLineAoE.cs`
- `Assets/_Project/Scripts/Save/SaveDtos.cs`
- `Assets/_Project/Scripts/Save/SaveSnapshot.cs`
- `Assets/_Project/Scripts/Save/SavePreflight.cs`
- `Assets/_Project/Scripts/World/ExpeditionRules.cs`
- `Assets/_Project/Scripts/World/ExpeditionWithdraw.cs`
- `Assets/_Project/Scripts/World/DiplomacyManager.cs`
- `Assets/_Project/Scripts/World/DiplomacyPrisoners.cs`
- `Assets/_Project/Scripts/UI/GameMenuPrisoners.cs`
- `Assets/_Project/Scripts/UI/GameMenuManagement.cs`
- `README.md`
- `docs/IMPLEMENTATION_BOARD.md`
- `docs/GAPS_1029_J2.md`

신규 7개(패치 자체 포함 8개):

- `Assets/_Project/Scripts/Boss/AoE/BossAoEState.cs`
- `Assets/_Project/Scripts/World/DiplomacyHomecomingEntry.cs`
- `Assets/_Project/Scripts/UI/GameMenuHomecomings.cs`
- `AgentScripts/Review1029K2Checks.cs`
- `AgentScripts/Review1029K2Compile.ps1`
- `docs/DECISIONS_1029_K2.md`
- `docs/HANDOFF-CODEX-K2-1029.md`
- `docs/patches/K2-1029.patch` (자기 자신은 패치 내용에 미포함)

생성된 `Temp/K2Baseline`, `Temp/Review1029K2`, 검증 로그와 Graphify 산출물은 인계 코드 패치에서 제외한다. prefab·기존 meta 파일은 만들거나 수정하지 않았다. Unity 임포트 시 신규 .cs의 meta 생성은 Claude가 처리한다.
