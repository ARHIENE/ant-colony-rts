# 프로젝트 로그

## 현재 상태 — 2026-11-05 SAVE (포맷 후 복원·작업 환경 연결)
- 프로젝트: `F:\UnityProjects\ant-colony-rts`, 일반 개발 `develop`, 안정 `master`. GitHub 복원 기준 `3618e5c4`(2026-11-04 포맷 전 백업). 작업 전 `AGENTS.md`, `GAME_DIRECTION.md`, `docs/IMPLEMENTATION_BOARD.md`를 읽는다.
- 오늘은 게임 기능·기획을 변경하지 않았다. Unity Hub에 기존 프로젝트를 등록하고 실제 씬 `Assets/_Project/Scenes/AntColony.unity`를 열어 저장했다. 최초 `Untitled`는 수정되지 않은 기본 빈 씬이었으므로 별도 저장하지 않았다.
- 에디터: `E:\Unity\6000.5.8f1\Editor\Unity.exe`. Unity Hub 관리 공식 CLI `1.0.0-beta.16` 사용 확인. MSIX 설치 경로: `%LOCALAPPDATA%\Packages\UnityTechnologies.UnityHub_2vrhnee42bhxm\LocalCache\Local\Unity\bin\unity.exe`. 로컬 실행 래퍼 `F:\_tools\unity-official\unity-managed.ps1`는 일반 설치와 MSIX 경로를 매번 찾아 Hub 업데이트를 따른다.
- 공식 `com.unity.pipeline 0.7.0-exp.1` 유지, 예전 PC 외부 Akiojin Bridge 로컬 의존성 제거. Codex Unity MCP 등록 완료. CLI와 MCP 양쪽 `editor_status`가 ready/compiling=false/playMode=stopped로 응답하고 MCP 도구 155개 탐색·호출 성공, Auto-tick 활성화. 전체 에디터 목록 감지는 샌드박스에서 비어 있을 수 있으므로 반드시 `--project-path`를 붙여 직접 상태를 확인한다.
- GitHub 플러그인: `ARHIENE/ant-colony-rts` 쓰기 권한 및 develop의 동일 SHA ref 갱신 성공으로 인증 검증. 터미널 git push 자격 증명과는 별개다. SAVE는 플러그인으로 원격 head를 재확인하고 기대 SHA 조건으로 갱신한다. master·prefab은 커밋하지 않는다.
- 프로젝트 CLAUDE.md/AGENTS.md와 ai-skills 전역 규칙·메모리 백업이 남아 있다. Codex 사용자 스킬 및 Claude Code 사용자/프로젝트 스킬을 설치했고 Graphify 실제 조회를 검증했다. Graphify 실행: `C:\Users\gusdu\Documents\Codex\tools\bin\graphify.exe`. 전역 규칙 파일 자체는 아직 사용자 홈에 복원하지 않았으며, SAVE는 프로젝트 규칙과 ai-skills 백업을 함께 참조한다.
- Notion·GitHub·Lovable·Higgsfield의 Codex 계정 접근 확인. Blender 본체·MCP와 Claude 설정 작업은 별도 범위이며 Claude 앱 스킬 업로드·OAuth·Effortless 후속 확인은 사용자가 Claude에 직접 인계했다. 해당 후속 작업의 완료를 이 SAVE에서 보장하지 않는다.
- 검증 범위: 복원 Git 연결성 검사, 설정 JSON/TOML 파싱, 변경 diff 검사, Graphify 조회, Unity C# 컴파일 오류 미발견 및 CLI/MCP 연결, AntColony 씬 저장 완료. 게임 Play 회귀·게임 기능 검증은 오늘 실행하지 않았다. 사용자 지시로 Unity 스크린샷·영상은 생략한다.
- 보존: 첫 임포트/씬 저장으로 생긴 씬·재질·URP·ProjectSettings·graphify 로컬 경로 변경은 자동 생성 변경으로 구분한다. 게임 기능 작업으로 보고하지 않는다. `.prefab`/`.prefab.meta`는 커밋 금지.
- 다음 작업: 기존 웨이브 수성 미해결 사항을 그대로 이어간다. 적 41마리 벽 밖 13~15m 정지로 웨이브 미종료 문제 우선 확인. 원정 중단에 따른 로켓 승리 경로, 공성형 적, 첫 평화 6일 실플레이, 노후 검사 잔여는 미해결 유지. 이전 상태 원문 `work/save-20261105/log-before-save.md`, 요약은 changelog 상단 참고.
