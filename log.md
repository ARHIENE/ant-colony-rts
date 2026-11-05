# 프로젝트 로그

## 현재 상태 — 2026-11-06 SAVE
- 사용자 최신 지시: 현재 상태를 SAVE하고 컴퓨터 종료. 기존 세 채팅 완료 대기 이후 별도로 즉시 SAVE·종료를 요청했다.
- UI: 현재 시안 Unity 반영, 공식 컴파일 오류0, 격리 Play UI 23검사 및 주요6화면 native 캡처 확인. 자원 아이콘 렌더러·건설 HUD 겹침 수정, 연구 좌우 이동 보완. Graphify update 완료(35,549노드/94,188간선). 전체 상세 화면·소형 해상도·대규모 인원 전수 검증은 아님.
- 리팩토링: 담당 채팅 완료 보고. 770검사 통과, 예고 메시6종 지문 동일, 런타임 순감소70줄. 상세 docs/REFACTOR_REVIEW_1106.md.
- 애니메이션: 장비28종 반입, 파지·무기별 동작·나머지 행동 연결 미완료. 담당 채팅 Bad Request 오류로 중단. 완료로 기록하지 않는다.
- 기존 씬 누락 prefab/스크립트 경고 미해결. .prefab/.prefab.meta는 커밋 제외. develop만 저장하며 master는 변경하지 않는다.
- 노션 개발 일지: https://app.notion.com/p/40fc4a0ecd3181f8909cf5136cb0c5da / 문제 해결: https://app.notion.com/p/40fc4a0ecd3181fba9f0f8d361275d05. UI 실화면 첨부. 카카오톡·컴퓨터 종료는 원격 반영 확인 후 실행한다.
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

## 원격 저장 형식
- GitHub 연결의 대용량 blob 전송 내부 오류로 최신 씬·graph.json은 work/save-20261106/large-files의 gzip/base64 분할 백업으로 보존한다. 일반 경로는 이전 원격 파일이며 새 복제본은 안내에 따라 복원한다. 현재 PC의 두 원본은 최신 상태다.
- 전체 원본 로컬 커밋 6b273113fc8674f8afdb58e1df9714e9e33aeead, 보존 브랜치 codex/local-save-20261106. 대용량 제작 .blend/GLB와 Graphify 캐시는 로컬 보존. Blender 손 리그 파일 저장 확인.
- 원격 문서 정리 c0c8d327과 UI/리팩토링/캐릭터/초반 배송·첫 웨이브 저장 수정을 함께 보존했다. 종료 자체의 성공은 이 파일에서 선행 선언하지 않는다.
