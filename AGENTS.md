# 프로젝트 작업 규칙

- 앞으로 코드 작성과 수정은 Codex로 진행한다.
- `master`는 안정 버전으로 유지하고 일반 개발은 `develop`에서 진행한다.
- 사용자가 `SAVE`를 요청하면 기존 전역 SAVE 절차와 함께 `README.md`를 확인한다.
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
