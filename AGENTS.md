# 프로젝트 작업 규칙

- 앞으로 코드 작성과 수정은 Codex로 진행한다.
- `master`는 안정 버전으로 유지하고 일반 개발은 `develop`에서 진행한다.
- SAVE 작업을 시작할 때는 다른 SAVE 단계보다 먼저 현재 상태를 `log.md`에 작성한다. 이후에만 `changelog.md` 이관, README 반영, 커밋·push, Notion 기록을 진행한다.
- 사용자가 `SAVE`를 요청하면 기존 전역 SAVE 절차와 함께 `README.md`를 확인한다.
- 사용자가 `SAVE`를 요청하면 검증한 작업 변경사항을 `develop`에 커밋하고 `origin/develop`에 push한다. 커밋·push를 별도 요청으로 미루지 않는다. `master` 반영은 별도 요청이 있을 때만 진행한다.
- SAVE의 Notion 개발 일지는 한국 시간 기준 같은 날짜의 ant 기록이 있으면 해당 페이지의 기존 내용을 보존하고 이어서 작성한다. 같은 날짜 페이지는 새로 만들지 않는다. 같은 날짜의 ant 페이지가 여러 개면 가장 최근 페이지에 이어 쓴다.
- SAVE 스크린샷은 그날 추가·수정한 기능이 실제로 보이는 Unity 화면을 캡처해 개발 일지에 첨부한다. 해당 기능이 드러나는 실행 상태와 화면 구도를 준비하고, 캡처를 직접 확인한다. UI 수정은 해당 UI가 포함된 화면으로 찍으며, 변경 부분이 보이지 않는 일반 게임 화면이나 HUD가 빠진 카메라 캡처로 대신하지 않는다.
- 세션 중 게임 설명, 구현 상태, 조작법, 기술 정보 또는 브랜치 운영 방식이 변경되었다면 `README.md`에 현재 상태를 반영한다.
- 아직 구현하지 않은 기능을 구현된 기능처럼 `README.md`에 적지 않는다.
- `.prefab`과 `.prefab.meta` 파일은 커밋하지 않는다.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
