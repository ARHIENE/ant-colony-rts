# 프로젝트 로그

## 현재 상태 — 2026-10-26 통합 SAVE (10월 25일~26일 Claude·Codex 대조)
- 프로젝트 `E:\Git\ant`, 개발 `develop`, 안정 `master`. 기존 SAVE `6b9cfb1` 이후 Claude 3세션·Codex E/F/G 인계·실제 diff 전체 대조 완료. 근거: `docs/SAVE_AUDIT_20261026.md`.
- 이전 log는 날짜와 함께 changelog.md로 요약 이관했다. 원문은 `work/save-20261026/log-before-save.md`에 로컬 보존.
- 구조: `Assets/_Project/Scripts`의 Core/Units/World/Buildings/Map/UI/Save, `_Project/Editor` 임포터, `Assets/Resources` 런타임 자산, `_Project/Art/Kemono/Anim` 공통 클립, `AgentScripts` Pipeline 검사, `docs` 기획/인계. 목표는 Notion 기획 전체 구현이며 전체 완성 상태는 아니다.
- Claude E1 D04 진형14종/연구6·출전 UI, E2 D06/D07/D09 철수/실물 수송/파손·수리·해체/순회 저장 반영. 이번 SAVE 재실행363·82·208 통과.
- Codex 복구: 맵 설정 입력/재추첨/스크롤, 시작 인원, 선택 범위·드래그, 날씨 일시정지·꽃잎, 자원/건물/생물/시민/전리품 모델 연결. Claude H1 특성·원정대 UI·잔해 크기와 H2 드래그·설정 패치 통합.
- 후속 사용자 재현으로 정정: Animator 리그 경로와 중복90도 회전, 초상화 Idle_A 상태, 화면 가장자리 설정 복구. 전체 애니메이션 정지 원인은 리그와 형제인 메시가 CullUpdateTransforms 가시성 판정에서 빠진 것. 공통 AlwaysAnimate 및 스킨 갱신으로 수정. 도착 거리0 고착은 .5m 작업 허용 및 경로 정지로 수정.
- 채집: 좌클릭 선택/우클릭 지시 유지. 채집 이동/작업/운반을 표시하며 단순 이동 마커와 구분. 창고 없는 바닥 물품의 반복 줍기/내려놓기 차단, 실제 입고 장소·허용 품목·도달 가능성 확인.
- 날씨: 생물군계·기후·계절 가중치/입자/적설 통합. 사막 드문 강수·모래폭풍, 숲/물가 비·안개, 한랭/열대 강수형, 동굴 야외 강수 제외. 식물 재성장(120~300초)은 기존 규칙 유지.
- 최근 실행 검증: 실제 프레임 애니메이션40, 실제 접근/벌목/표시14, 운반 반복·입고10, 날씨445. H2 17·시작/클릭/보급13·시설710·자원200·원정UI27·특성25 등은 대조 문서에 구분. 앞선 강제 Animator.Update 기반 통과를 전체 재생 해결 근거로 쓰지 않는다.
- 실제 화면: 공식 Unity screen 캡처에서 잎 채집 Tool_Harvest/대상 문구·링/상단 초상, 후보 칩 화살표 제거, 원정 패널을 직접 확인했다. 초기 밤/Sleep·식사/Eat 캡처는 작업 검수에서 제외. 후보는 실행 게임 위 메뉴여서 HUD/알림이 남는다. 전 맵 장시간 육안 검수·Player 빌드는 미수행. 대형 수관의 작업자 가림은 후속 시각 점검 대상.
- 미통합: F1 D02/D03 평가·외부 조사, F2 D08 탐사. 오프라인 사본이며 Unity/UI 미완. `docs/patches/unfinished-20261026`에 소스 스냅샷/인계 보존(기준 E 변경 포함, 그대로 apply 금지). 전용 자원5종·낚시터·DigSite·원정지 지형과 과거 `docs/NOTION_APPLY_1025_codex.md` 미반영 항목은 남는다.
- 보류: 성능 최적화·림월드식 채취. 기존 G26 변경은 보존하되 목표 성능 달성 주장 없음. prefab/씬/Water/TimeManager/URP 재직렬화/서드파티/raw art/graphify/Temp는 로컬 보존·이번 커밋 제외.
- Notion [개발 일지](https://app.notion.com/p/404c4a0ecd318193a247fb8204810f3d) · [Trouble Shooting](https://app.notion.com/p/404c4a0ecd31816782e3d8d33aa6c9ee) 작성·재확인, 검수 캡처3장 업로드/첨부. 맵·UI·자원·메인 화면 기획4페이지 이번 변경도 교체·재확인했다. 과거 기획 반영 잔여 전체 완료는 아니다.
- 임시 저장 경로로 검사하고 Play 종료 확인. 카카오톡 도구는 없어 완료 알림 전송 불가. develop 커밋·push 결과는 완료 후 덧붙인다. SAVE 완료 뒤 컴퓨터 종료 요청: 2026-10-26 01:34:53 KST부터1시간 유효.
