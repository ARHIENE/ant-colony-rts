# 프로젝트 로그

## 현재 상태 — 2026-10-03 SAVE 완료
- 프로젝트: 개미 소굴 RTS, E:\Git\ant. Unity 6000.5.8f1 / URP 17.5.0 / Pipeline 0.7.0-exp.1. 개발 develop, 안정 master.
- 기존 HUD v4·Phase 2~6(2c8e919)에 이어 정찰 동행 장수, 벽 드래그, 공성/방화, 노망 특성, 바이옴 맵·계절/날씨 및 저장 v13 변경을 develop에 저장. 프로젝트 에셋을 Assets/_Project로 이동하고 에셋스토어 팩은 Assets/ThirdParty로 분리·Git 제외.
- 구조: 게임 소스·데이터·장면은 Assets/_Project/{Scripts,Data,Scenes,Resources,Editor}, 외부 에셋 Assets/ThirdParty, 별도 수입 Assets/_TeamImport. 검사 AgentScripts, 지식 그래프 graphify-out.
- 확인된 맵 변경: 6종 바이옴 지형·장식·물, 사막 모래/바위, 도시 벽돌 제거, 동굴 석탄/바위·조명 60%, 비 굵기 증가, 계절 색/입자·날씨 효과·저장 복원.
- 이번 Codex 수정: WallDragChecks 시작 시 자동 작업을 끄고 장수를 정지해 초기 자동 채집에 따른 검사 실패 제거. README 맵·날씨 설명 갱신.
- 검증: SiegeChecks 12 → WallDragChecks 11 순차 통과, MapStyleChecks 53, SeasonWeatherChecks 321 통과. Graphify 갱신 완료.
- 미해결: RegressionChecks 농장 첫 성장 항목 실패(새 게임에서도 재현). 현재 농장은 작업 인력이 필요한데 기존 검사는 성장 전에 농사 인력을 배정하지 않음. 전체 회귀 통과로 간주하지 않음. Player 빌드 미실행.
- SAVE: 이전 log 요약은 changelog.md에 이관되어 있음. README의 맵·날씨·정찰·벽·나이·저장·구조와 최신 검증 한계를 반영. 기존 Unity 캡처 도시·물가 비·동굴을 직접 확인했고 Play 모드를 종료함.
- 캡처 원본: .unity/save-2026-10-03/{city-hud,waterside-rain-hud,cave-hud}.png. 일지: https://app.notion.com/p/3eec4a0ecd3181ee9558dd368adb9c78 (기존 내용 보존 후 추가).
- Git 저장 대상은 이전 SAVE 스테이징과 README·log·changelog. 기존 graphify 산출물·디자인·별도 반입 에셋 등 비스테이징 작업은 로컬에 보존. 카카오톡 MemoChat 도구는 현재 연결되지 않아 완료 알림 불가.
- .prefab/.prefab.meta 커밋 금지. master 수정 금지. 사용자 저장 슬롯 보존.
- 공식 Unity CLI: C:\Users\Shim Hyeonyeop\AppData\Local\Unity\bin\unity.exe. 긴 run_script는 --timeout_ms와 CLI --timeout(초)을 함께 지정. 검사 순차 실행.
- 기획: https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd (상위 페이지 replace_content+allow_deleting_content 금지).
- 개발 일지 상위: https://app.notion.com/p/334c4a0ecd3181778dcaf0e6a8d57040 . 같은 한국 날짜 ant 최신 페이지 기존 내용 보존.
- SAVE 완료: d476e04를 origin/develop에 push. 같은 날짜 기존 Notion 일지에 7~10번과 물가 비·동굴 HUD 캡처 2장을 추가하고 기존 내용·이미지를 보존. master 변경 없음. 완료 기록은 후속 문서 커밋으로 저장.
