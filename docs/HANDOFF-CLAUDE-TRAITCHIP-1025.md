# Claude → Codex 인계: 특성 등급 칸 디자인 (2026-10-25 23:10)

Codex가 "버그 수정 후 특성 디자인"으로 미뤄둔 항목을 Claude가 사본(`E:/Git/ant-wt/H1`)에서 미리 구현했다. **특성 디자인은 새로 만들지 말고 이 패치를 적용**할 것.

- 패치: `E:/Git/ant-wt/patches/H1-trait-chip.patch` (기준 사본 `ant-wt/H1-base` = 23:00 본 프로젝트 상태)
- 적용: Play 종료 상태에서 `ant-wt/H1/Scripts/UI/{TraitChip.cs, GameMenuCrashStart.cs, GameMenuRoster.cs}`를 `Assets/_Project/Scripts/UI/`로 복사(두 기존 파일은 23:10 기준 본 프로젝트와 변경 충돌 없음 — 그 사이 수정했으면 해당 한 줄만 옮길 것). TraitChip.cs.meta는 Unity가 생성.
- 내용
  - `UI/TraitChip.cs`(신규): 팰월드 PASSIVE SKILL 칸 참고. 실버=어두운 바탕+▲1, 골드=노랑 그라데이션+▲3, 다이아=청록 각면 조각 무늬+▲3, 레드=다이아와 같은 각면 무늬(빨강)+▼3. 텍스처는 코드 생성·등급/크기별 캐시. `Chip()`, `Grid()`.
  - `GameMenuCrashStart.cs` 시작 후보 카드: "특성: a · b" 글자 → 2열 칩(240×22).
  - `GameMenuRoster.cs` 장수 상세 머리: 글자 → 4열 칩(388×18).
  - 등급 판정·효과는 변경 없음(TraitRules.Grade 그대로).
- 검증: 오프라인 Roslyn 컴파일 오류 0. 텍스처 미리보기 `C:/TESTTEMP/claude/E--Git/df6935fe-43ea-4908-a8f7-dd131d73587a/scratchpad/chips.png`(같은 알고리즘 node 렌더). **Unity 실제 화면은 미확인** — 적용 후 새 게임 시작 화면·장수 관리 캡처 필요.

---

# 추가 인계: c13 원정대 UI · c22 잔해 크기 (2026-10-25 23:25, Claude 담당 — 사용자가 Codex에 전달함)

기준 사본 `ant-wt/H1-base`(23:20 본 프로젝트), 수정본 `ant-wt/H1`. 오프라인 Roslyn 컴파일 오류 0. **Unity 화면 미확인.** 23:25 기준 아래 파일은 본 프로젝트와 충돌 없음(그 뒤 수정했으면 diff만 옮길 것).

## c13 원정대 패널 — `patches/H1-c13-expedition-ui.patch` (UI/WorldMapPanel.cs)
- 원인: 목록 칸이 높이 116 고정이라 수송 수단이 적으면 위가 비고 버튼은 아래(178~494)에 몰림. 왼쪽 위 '다음 수송 수단'만 있고 오른쪽·제목이 비어 있었음. '선택 장수 탑승' 왼쪽 칸이 비어 있었음. 박스 펼침 버튼 4개가 폭 438이라 목록(336) 밖으로 넘침.
- 수정: 맨 위 `원정대` 제목 + 오른쪽 '다음 수송 수단'. 목록 높이 = 박스 높이 합(최대 240, 넘치면 스크롤, 수단 없으면 안내 40). 버튼·출발 계획·경유지는 `Expedition Controls` 묶음으로 목록 바로 아래(`LayoutExpeditions`), 패널 높이도 맞춤. 버튼 2열 빈칸 없이 재배치. 박스 버튼 4개 72~76폭(합 304).
- 버튼 이름은 그대로 → Stage7UIChecks 등 이름 기반 검사 영향 없음.

## c22 잔해 크기 — `patches/H1-c22-wreck-size.patch`
- 원인 추정: 불시착 잔해 바닥 크기 3×2m에 FacilityVisual이 모델을 x·z로 맞춰 장수(1.6m)보다 작게 보임.
- 수정: `CrashLandingStart` 잔해 6×1×4(2배), 위치 오프셋 (-5,0,3)→(-7,0,4). `RuntimeBuildingTemplates` CrashWreck 크기도 6×1×4로 일치.
- 확인 필요: 사용자가 말한 '파편'이 이 잔해가 맞는지(다른 파편 오브젝트면 알려 줄 것), 커진 잔해가 시작 동선·물 등을 막지 않는지.
