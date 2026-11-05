# 코드 리뷰·리팩토링 — 2026-11-06

## 범위와 기준

- 사용자 요청: 프로젝트 코드를 폭넓게 리뷰하고 간결화. Ponytail 사용 추가 요청 반영.
- 적용: ant-workflow, graphify, safe-refactor, ponytail(full), ponytail-audit/review 점검 기준, 공식 unity-cli·unity-pipeline.
- `Assets/_Project/Scripts`의 C# 481개·47,883줄을 대상으로 파일 규모와 8줄 연속 중복을 검색했다. 서드파티·과거 worktree·검사 코드는 이 집계에서 제외했다.
- 집중 리뷰: 저장 진입/파일 교체/선택 상태/건설 복원, 자원·식품·생산 재료, 생산 완료/취소, 보스 탐색·범위 판정·공격 예고 메시. **481개 전체의 개별 로직을 정밀 검증한 감사는 아니다.**
- 싱글 플레이·후반 수천 유닛을 기준으로 반복 탐색과 복제된 규칙을 우선했다. 현재 장비·캐릭터·UI·웨이브·배송 담당 파일의 기존 변경은 보존했다.

## 적용한 정리

| 발견 | 변경 | 보존한 동작 |
|---|---|---|
| 두 보스 패턴의 표적 탐색 본문이 동일 | 기존 `CombatTargeting`에 `FindNearestGroundTarget` 공통 처리 추가 | 3D 구형 탐색 범위, 수평 거리 비교, 레이어/트리거, 사망·비활성·공중 제외, 동률의 첫 대상 |
| 원·부채꼴·직선 예고의 격자 인덱스와 지면 투영이 동일 | `GroundTelegraphMesh`에 두 연산을 모음 | 각 컴포넌트·직렬화 필드·정점·UV·삼각형 순서·지면 오프셋 |
| 조리/일반 생산의 도착 재료 검증 복제 | `ProductionInputs.ValidStock` 재사용 | null 구저장, 종류·중복·수량 상한·신선도 검증. 조리는 최소1, 일반 생산은 최소0 |
| 가공 완료/취소의 일곱 상태 필드 초기화 복제 | `Processor.ResetBatch` 재사용 | 환급·생산물 생성·작업자 해제의 순서, 원유 잔량, 주문·일시정지·파괴 상태 |
| `FoodLots.StalestAmount`가 각 묶음마다 전체 최솟값 재계산 | 최솟값을 한 번 계산. 요리 숙련의 총량도 한 번 계산 | 동일 신선도 묶음 합계, 빈 식품, 구저장 숙련5, 가중 평균 반올림. 수량 계산 O(n²)→O(n) |
| 건설 기여도 복원 시 항목마다 전체 현장 배열을 다시 생성·탐색 | 한 번의 `ToLookup`으로 주문번호별 조회 | 중복 번호가 있으면 첫 현장, 없는 현장 무시, JSON 깊은 복사. O(항목×현장)→O(항목+현장) |

기존 런타임 파일 12개 수정·공통 유틸리티 1개 추가, 런타임 코드 순감소 70줄(검사·문서 제외). 공개 API·직렬화 필드·저장 버전·기획 수치·의존성 변경 없음. 줄 수만 줄이기보다 중복 규칙과 반복 계산을 줄였다.

## 검증

공식 Hub 관리 래퍼 `F:\_tools\unity-official\unity-managed.ps1`와 Unity 6000.5.8f1에서 실행했다.

- 변경 전/후 `Refactor1106Checks.Run`: **645/645 통과**. 식품 600건, 재료 26건, 두 보스 표적 탐색 12건, 건설 기여도 복원 3건, 생산 취소 4건.
- 예고 메시 변경 전/후 `Refactor1106Checks.MeshFingerprints`: 원·부채꼴·직선 × 지면 없음/경사면 총 **6개 SHA-256 동일**. 정점·UV·삼각형·법선을 바이트 단위로 비교했다. 기본 분할 수와 Y축 회전37도 조건이다.
- 기존 `Review1029K1Checks.Main`: **87 통과**. 식품/숙련/시공 기여도·기존 효과의 실제 게임 DLL 회귀.
- 기존 `Review1029BossChecks.Main`: **38 통과**. 보스/원정 규칙 회귀. 실제 장시간 전투를 수행한 검사는 아니다.
- 최종 공식 `recompile_status`: completed, failed=false, errors=[], compilationFailed=false.
- 해당 코드 `git diff --check` 통과. 신규 유틸리티 `.meta`는 Unity에서 생성했다.
- `graphify update .` AST 갱신 종료0: 35,340노드·93,447간선, graph.json/graph.html/GRAPH_REPORT.md 갱신. 기존 커뮤니티 이름 변경·타 작업 Python 파일의 심볼 없음 경고가 있었으며 LLM 재라벨링은 실행하지 않았다.
- 테스트는 별도 임시 additive 씬에서 실행하고 finally에서 닫았다. 원래 활성 씬으로 복귀했으며 플레이어 저장 파일·게임 씬을 저장하지 않았다. 최종 에디터 ready/stopped.

재실행:

```powershell
& 'F:\_tools\unity-official\unity-managed.ps1' command run_script --file AgentScripts/Refactor1106Checks.cs --entry Refactor1106Checks.Run --project-path F:\UnityProjects\ant-colony-rts
& 'F:\_tools\unity-official\unity-managed.ps1' command run_script --file AgentScripts/Refactor1106Checks.cs --entry Refactor1106Checks.MeshFingerprints --project-path F:\UnityProjects\ant-colony-rts
```

## 유지·미검증

- 보스 AoE 피해 루프는 비슷하지만 구/부채꼴/상자 판정과 콜라이더별 높이·각도 검사가 달라 이번에 합치지 않았다. 판정 통합은 별도의 전투 경계 검사가 필요하다.
- 저장 파일의 원자적 교체·예외 처리·롤백과 선택 상태 정규화는 유지했다. 짧게 만들기 위해 데이터 보호나 구저장 검증을 삭제하지 않았다.
- 다른 담당자의 장비/캐릭터/화면/웨이브/배송 변경은 이번 리뷰의 합격 대상으로 간주하지 않는다.
- 실제 Play 저장/로드 왕복, 전체 생산 체인의 실물 환급·완료, 장시간 전투, Player 빌드, 모든 화면의 육안 검수·프레임 시간 실측은 미실행이다. 탐색 복잡도 개선을 FPS 개선 실측으로 표현하지 않는다.
- SAVE·커밋·push·Notion 기록 없음.
