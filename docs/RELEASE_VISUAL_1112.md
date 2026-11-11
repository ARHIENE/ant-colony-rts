# RELEASE VISUAL PASS — 2026-11-12

## 구현과 직접 확인한 범위

기존 자연물/캐릭터/차콜 UI 방향을 유지했다. 외부 이미지·모델 생성이나 패키지 설치 없음. 사용자 저장 슬롯 미사용. 씬과 수정 전 소스는 `work/release-visual-pass/before`에 보존했다. SAVE/커밋/push는 하지 않았다.

- MapGenerator: 누락된 바이옴 장식 연결에 대한 기존 모델 기반 시각 보완. 중앙15m와 네 방향8m 진입 공간을 비우고 주변부에 결정적 식생/바위 배치. 시드261111/Small/Forest에서118개. 자원 컴포넌트와 켜진 콜라이더 없음. 원래 장식 프리팹이 정상인 스타일은 우선한다.
- 지면: 오래된 머티리얼의 1m 텍스처 반복값을8m로 보정. 이미 더 큰 값이 있는 재질은 유지. 지형 높이/물/자원 생성 규칙 미변경.
- CommanderOverhead: 건강한 비선택 장수도 이름 표시. 숨긴 원본 리그를 이름표 바운드에서 제외하고 런타임 리그 캐시 갱신. 작은 화면12px 이상, 선택 장수 행동 표시. 가까운 이름표는 최대3단으로 배치하고 이동한 라벨에 연결선 표시.
- BuildScreen/MenuViewport: 건설 창 최대1.24배 화면 활용. 목록3열, 카드 높이 최소66물리픽셀/글자 최소13픽셀, 마스크와 휠/드래그 스크롤바. 목록을 다시 열면 위부터 표시. 잠긴 카드의 검은색 곱셈을 완화. 건물/비용/연구 조건/단축키 순서 보존.
- GameMenuController: 건설 설명은 우측 전용 영역, 작은 화면 최소12픽셀. 담당 장수 선택 시 기존처럼 설명을 숨긴다.
- SeasonVisuals: 봄 꽃잎25→5개/초, 크기.4→.16, 알파.9→.5. 가을 낙엽도 밀도/크기 완화. 계절/기상 게임 규칙 미변경.

## 검증

공식 Hub 관리 CLI, Unity6000.5.8f1, 실제 PC_RPAsset. 단일 async Run/finally에서 임시 저장 루트·자동저장 설정·GameView 크기 복원.

- 1440×900와760×600: 공식 컴파일 성공, 새 게임118개 장식 모두 활성 콜라이더0/자원 컴포넌트0/중앙 공간 보존.
- 건설 작업 탭24칸의 실제 휠 이벤트로 마지막 화학 작업대까지 도달하고 viewport 안에 완전히 표시됨.
- 두 해상도 각각16개 건물: 스크롤로 버튼 노출→EventSystem Raycast 최상위 버튼 확인→PointerEnter→툴팁 표시/내용/잘림/화면 밖/목록 가림 검사 통과.
- 정착지 연구대 카드 글자: 수정 전760화면5.8056px→최종13.0889px. 1440에서는21.08px. 우측 도움말12px 이상.
- 일반 게임 기본 시점,12초 실제 시뮬레이션 후 화면, 장수 근접과 주변 지형, 건설 상단·하단·생활 탭 캡처 직접 확인.
- 최종 씬 재로드 후 동일 새 게임/UI 검사 재실행 통과. consoleErrors0/compilationFailed false, 기존 Animator 경고5개. 경고까지0이라고 주장하지 않는다.
- `git diff --check` 범위 내 통과.

검사 스크립트: `AgentScripts/ReleaseVisualChecks.cs`, `ReleaseVisualExtraChecks.cs`, `ReleaseSceneRepair.cs`.
결과: `work/release-visual-pass/scene-final-result.json`, `console-final.json`, `missing-final.json`.

## 검사 중 추가 발견과 보정

첫 수정 화면에서 굵은 글씨 번짐, 비선택 이름표 충돌 누락, 작아서 눈에 띄지 않는 장식, 과도한 꽃잎을 발견해 재보정했다. 밀집 목록의 최소11px만으로는 부족해3열 스크롤과13px 카드로 다시 수정했다. 마지막 건물 접근과16종 도움말 클릭 가능성을 새 검증에 추가했다.

콘솔 검수에서 기존 AntColony 씬의 Missing Prefab 오류가 재발했다. 읽기 전용 조사로 Tree_01 7010개/Tree_02 7018개/Rock_05 1362개가 Transform만 있는 빈 장식임을 확인했다. 기존 dirty 씬 파일 전체를 백업한 뒤 이15390개만 제거했다. MiniBirdBoss는 삭제하지 않고 완전 unpack했으며5개 행동 컴포넌트의 인스턴스와 직렬화된 게임 데이터를 전후 비교해 보존했다. 보스의 외형을 새로 복구한 작업은 아니다.

씬 크기23,189,712→8,005,248바이트. 씬 저장→다시 열기→Play→새 게임 검사→Edit 재검사에서 MissingAsset0, 콘솔 오류0. 정리 스크립트 첫 시도는 Transform의 JsonUtility 직렬화 제한으로 변경 전에 실패했고, MonoBehaviour만 비교하도록 수정한 두 번째 실행이 성공했다.

## 최종 화면

- [일반 플레이](../work/release-visual-pass/final/screens/gameplay-running-1440x900.png)
- [건설1440](../work/release-visual-pass/final/screens/build-work-1440x900.png)
- [작은 창 건설](../work/release-visual-pass/final/screens/build-work-760x600.png)
- [스크롤 하단](../work/release-visual-pass/final/screens/build-bottom-760x600.png)
- [장수 근접](../work/release-visual-pass/final/screens/commander-close.png)
- [주변 지형](../work/release-visual-pass/final/screens/landscape-edge.png)

## 화면 평가 — 이번 수정 표면에 한정

palette: 3/5 — 기존 흙·식생·차콜 색 유지, 잠긴 버튼의 검은 덩어리 완화.
style: 3/5 — 기존 환경 모델 재사용. 시설 모델 전체 통일은 별도 제작 중.
lighting: 3/5 — 지면 주광 그림자 유지, 반복 얼룩 감소. 시작 새벽과12초 뒤 낮 상태 모두 확인.
silhouette: 3/5 — 기본 시점 세 장수 이름과 선택 상태 구분. 얼굴·장비 세부는 근접 화면/초상에 의존.
UI: 3/5 — 작은 창 카드13px/설명12px, 상단·하단 실제 포인터 접근과 가림 검사.
feedback: 3/5 — 선택 강조/행동명/호버 설명/스크롤 반응 확인. 전투 피드백 전체 평가는 아님.

3점은 기본 완성도 기준이며 상용 출시 인증이 아니다. 이번에는 첫 방어 전투 전체, 시설 전종 외형, 작은 창의 하단 전체 HUD 확대, 모든 바이옴/수백 유닛 밀집 성능을 검증하지 않았다. 보스의 잃어버린 원본 시각 에셋도 이 정리로 복원되지 않는다.

참고한 스킬: craft-game-floors의 art-direction/placeholder-assets/scene-lighting/visual-scorecard, ui-ugui의 scrollview-setup, 기존 unity-cli/unity-pipeline.
API 확인: https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-ScrollRect.html 및 https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/prefabutility/unpackprefabinstance (6000.5 주소 접근 실패 후 동일 API 문서 참조).

Graphify update 완료(종료0):46,493노드/122,128관계/3,329커뮤니티. 로그 work/release-visual-pass/graphify-final.log. Unity는 Edit/ready로 애니메이션 작업에 반환했다.
