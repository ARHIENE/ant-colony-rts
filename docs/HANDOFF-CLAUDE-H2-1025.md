# Claude → Codex 인계: H2 = c10 드래그 선택 + 설정창 점검 (2026-10-25 23:35)

Codex 분배(23:20) 범위: SelectionManager.cs·UnitSelectionController.cs / GameMenuSettings.cs·GameMenuKeys.cs·UserSettings.cs. 본 프로젝트 직접 수정·Unity 실행·커밋 안 함.

- 기준 사본 `E:/Git/ant-wt/H2-base`(23:28 본 프로젝트 Scripts 전체), 수정본 `E:/Git/ant-wt/H2`.
- 패치 `E:/Git/ant-wt/patches/H2-c10-settings.patch` (Scripts 4개 + 신규 `AgentScripts/ClaudeH2Checks.cs`). 23:33 기준 5개 대상 파일 모두 본 프로젝트와 충돌 없음.
- 오프라인 Roslyn: Assembly-CSharp 오류 0, 검사 스크립트 오류 0. **Unity 실행 검사 미실행** → 적용 후 `ClaudeH2Checks.Main` 실행 부탁(RecoveryUnitChecks와 같은 방식, Play 모드·격리 저장 경로).

## c10 드래그 선택 (Units/SelectionManager.cs) — UnitSelectionController는 변경 없음
- 기존 'UI 위 지나가도 드래그 유지'(`!isMouseDown &&` 조건) 보존.
- 끊김 원인 1: 드래그 시작점을 화면 좌표로만 저장 → 가장자리 스크롤로 카메라가 움직이면 박스가 땅에서 미끄러짐. 수정: 누른 지점의 땅 좌표(`TryGroundPoint`, 실패 시 y=0 평면, 그것도 실패면 기존 화면 고정)를 저장하고 매 프레임 화면으로 재투영.
- 끊김 원인 2: 창 포커스를 잃는 등으로 놓은 프레임(`wasReleasedThisFrame`)을 놓치면 박스가 멈춘 채 남음. 수정: `isMouseDown && !isPressed`도 놓음으로 처리.
- 자는 장수: 선택 자체는 장수를 움직이지 않음(코드상 선택 → 명령 경로 없음, Codex 'selection does not wake sleeper' 검사와 일치). 박스 선택에 자는 장수가 포함돼 이어진 우클릭 이동에 깨어 움직이는 것으로 판단 → 박스 선택은 자는 장수를 빼고 고름(박스에 자는 장수만 있으면 그대로 고름, 단일 클릭 선택은 그대로).
- 확인 필요: 사용자 증상이 '선택만 했는데 즉시 움직임'이면 다른 원인(예: D 키 = RemoveTroop → 평시 RestOrTreat으로 침대 이동). 검사에서 재현되면 알려 줄 것.

## 설정창 (UI/GameMenuSettings.cs, UI/GameMenuKeys.cs, Core/UserSettings.cs)
점검 결과 설정값 저장·불러오기·소비처(카메라·자동저장·알림·자동정지·도움말·단축키 15개 전부)는 연결돼 있었음. 끊긴 부분만 수정:
1. '설정 초기화'가 카메라 속도·가장자리 스크롤을 카메라에 다시 넣지 않음 → `Commit()`으로 모든 변경이 같은 적용 경로.
2. 그래픽 품질 '기본' 선택 시 프로젝트 기본 품질로 안 돌아감 → 처음 적용 때 기본 품질 기억(`defaultQuality`) 후 복원. 같은 레벨이면 SetQualityLevel 생략.
3. 해상도 '모니터 기본'일 때 ◀/▶ 모두 최대 해상도로 감 → 현재 모니터 해상도 칸에서 한 칸 이동.
4. 값을 바꿀 때마다 화면을 다시 그려 목록이 맨 위로 튐(조작 탭은 길어 누른 게 안 된 것처럼 보임) → 스크롤 위치 유지(탭 바꿀 땐 맨 위).
5. '귀환' 키 이름이 실제 동작과 다름 → '귀환 (평시: 휴식·치료)'.
6. `PollRebind` Keyboard.current null 방어.
- 새 설정 항목 추가 없음.

## ClaudeH2Checks 항목
박스 선택 자는 장수 제외/자는 장수만이면 선택 · 땅 고정 시작점 재투영·카메라 이동 추종 · 기본 품질 기억·복원 · 설정 초기화가 카메라 panSpeed에 반영 · 조작 탭 스크롤 위치 유지(재그리기·옵션 클릭 후) · 가장자리 스크롤 끄기 저장.
