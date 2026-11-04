# Codex N2 인계 — 2026-10-31

## 범위·판정

- 작업 위치: `E:\Git\ant-wt\N2`, 브랜치 `codex/n2-1031`. 원본 `E:\Git\ant` 읽기 전용. 시작 시 포함된 L1/L2/M1/M2 미커밋 병합 변경 보존.
- 담당: 100항목 96(색 이외의 구분). **코드 구현·오프라인 검증 완료, Unity 실행/육안 검증 잔여.**
- 원본 쓰기·Unity 조작·캡처·커밋·push·Notion·SAVE 수행 없음. N1의 지하 광층 생성·Commander 작업/WorkPriorities 파일 수정 없음.
- 선택 사항은 추천안 자동 선택. [결정](DECISIONS_1031_N2.md), [잔여 공백](GAPS_1031_N2.md).

| 항목 | 구현 판정 | 근거 | 실행 잔여 |
| --- | --- | --- | --- |
| 월드맵 세력 | 구현 | 편입/동맹/중립/적/반란 `[=]/[+]/[-]/[!]/[X]`; 적·반란45도 마름모. C/B/R/T 종류 문자 유지. 기존 소유/외교 상태 사용, 유효 동맹 만료 반영 | 실제 외교 전환·반란·편입/상실 마커 갱신·클릭 |
| 위험도·거점 상태 | 구현 | `D숫자`와 범례(1쉬움/2보통/3어려움/4이상매우어려움), ATK/LOST/OFF 문자 병행 | 고위험·공격 중/포기/상실·작은 화면 표시 |
| 건설 미리보기 | 구현 | 단일/이동/연속 배치 큐브 윗면에 두 선 `+` 가능 / `X` 불가. 각 칸의 기존 valid값 사용, 복제된 선 재사용 | 단일/이동·벽 연속·취소·낮은 건물/경사지 가독성 |
| 유닛 선택 | 기존 구현 재사용 | AntVisual의 원형 링 두께 .055→.13을 확인. 해당 파일 수정 없음 | 실제 유닛 링 두께·지면 정렬 |
| 건물·기본 모델 선택 | 보완 | AntVisual 표시가 없는 대상만 Collider.bounds 기준 흰 .16m 사각 외곽. 선택 해제/비활성 시 숨김, 대상 이동 추적 | 실제 건물 크기·모델/지면 가림·파괴/해제 |
| 범례 | 구현 | 월드맵 범례/마커 툴팁, 설정→조작 설명. 기존 UI 글꼴/12px 기준·색 유지 | 줄바꿈/스크롤/작은 화면·회색조 대비 |
| 저장 | 변경 없음 | 신규 필드는 표시용 런타임 참조만 사용. Save DTO/Normalize/Valid/버전24 불변 | 불러온 객체 선택·배치·마커 표시의 실장면 재생성 |

## N2 수정 파일

기존 파일:
- `Assets/_Project/Scripts/UI/WorldMapPanel.cs`: 표시 연결·범례 교체. 기존 병합본의 FoodComparison 등은 보존.
- `Assets/_Project/Scripts/UI/GameMenuSettings.cs`: 조작 탭의 색 외 구분 설명.
- `Assets/_Project/Scripts/Buildings/BuildingPlacementController.cs`: 미리보기 컴포넌트 생성/단일·연속 상태 전달 세 줄.
- `Assets/_Project/Scripts/Units/SelectableObject.cs`: 기존 유닛 링 재사용·기본 모델/건물 외곽 연결.
- `README.md`, `docs/OPERATIONS_100_STATUS.md`의96행, `docs/IMPLEMENTATION_BOARD.md`의N2 담당/완료 표시.

신규 파일:
- `UI/WorldMapSymbols.cs`: 세력 분류·문자·위험도·공통 범례.
- `UI/WorldMapMarker.cs`: 기존 UI 이미지 회전·마커 이름표/툴팁 갱신.
- `Buildings/PlacementPreviewSymbol.cs`: 기존 AntSelection 재질과 LineRenderer 두 선.
- `Units/SelectionOutline.cs`: 기본 모델/건물 선택 사각 외곽.
- 위4개 스크립트의 `.cs.meta` (그래픽 에셋/prefab 추가 없음).
- `AgentScripts/Review1031N2Compile.ps1`, `AgentScripts/Review1031N2Checks.cs`.
- 본 인계·결정·공백 문서와 `docs/validation/N2-1031-offline.txt`.
- Graphify AST 산출물 갱신: 12,235노드/28,500간선/556커뮤니티. 문서 의미 분석/LLM 실행 없음. 커뮤니티 변동에 따라 일부 이름은 허브 기반 자동 이름으로 갱신되었으며 의미 라벨 재생성은 하지 않았다.

스크립트 신규 경로는 `Assets/_Project/Scripts/` 기준이다. 위 목록 외 git diff는 시작 시 포함된 병합 변경이므로 N2 작업으로 취급하지 않는다.

## 검증

실행 명령:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/Review1031N2Compile.ps1 -OfflineChecks
```

- M2 컴파일 방식을 재사용하고 N2 경로를 검사한다. Unity 설치의 Roslyn/참조 DLL만 사용하며 에디터를 조작하지 않는다. 누락된 패키지 C#43개는 원본을 읽어 N2 Temp에 복사한다.
- 실제 전체 게임 DLL·Editor 검사 DLL·오프라인 호스트 컴파일 오류 **0**. 기존 경고(사용 중단 API/참조 비교/FormatterServices)는 유지. N2 신규 파일 관련 컴파일 경고 없음.
- N2 **77**: 5세력 고유 문자/마름모, 무소속40조합, 동맹 만료/전쟁/반란/편입 우선순위, 경고 문자, 위험도 하한/상위 값 보존, 실제 +/X 선 좌표·대칭·윗면 위치·범례.
- 회귀: **L1 76 / L2 97 / M1 103 / M2 298**. 기존 검사 원본 수정 없이 같은 N2 게임 DLL로 실행했다.
- 추가 회귀: K1 **87**, K2 **107**, 보스 **38**, 최종/포로 **31** 통과.
- `git diff --check` 통과. 로그는 `docs/validation/N2-1031-offline.txt`, 재실행 산출물은 `Temp/Review1031N2`.
- `Review1031N2Checks.RunScene()` 진입점은 컴파일만 확인. 마커 라벨/회전/툴팁3, 배치·복제2, 선택·해제·비활성3 = **8개 장면 검사 미실행**. Unity의 JsonUtility/null 중첩 객체 비교를 새로 사용하지 않았다.

## 실행 인계

1. Unity를 조작할 수 있는 담당이 격리 Play에서 RunScene을 실행한다(이번 세션은 금지).
2. 실제 월드맵5세력·D1~D4이상·ATK/LOST/OFF·마커 클릭/선택/범례/툴팁을 확인한다.
3. 단일·이동·연속 배치의 가능/불가·취소, 선택 유닛/건물·해제/비활성 및 저장 후 표시 재생성을 확인한다.
4. 960/1280/1440 화면·회색조/색각 이상 조건의 글자/선/이름표 겹침을 확인한다. 확인한 화면으로만 검증 완료를 판정한다.
5. Player 빌드·성능/장기 플레이는 미실행이며 기존 담당들의 실행 잔여도 유지한다.

카카오 완료 알림 도구가 세션에 제공되지 않아 발송하지 못했다. 개발 일지/log/changelog는 SAVE 지시에 따라 이번 작업 중 갱신하지 않았다.
