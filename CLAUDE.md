## 작업 규칙

- 새 세션 시작 시 `log.md`를 먼저 읽고, 거기서 연결한 인계 문서와 미완료 검사·미커밋 변경을 확인한 뒤 이어서 작업한다.
- Codex와 공동 구현 시 `docs/IMPLEMENTATION_BOARD.md`를 먼저 읽고 담당·수정 파일을 표시한다. 다른 담당자가 작업 중인 파일은 동시에 수정하지 않는다(2026-10-07 사용자 요청).
- SAVE 작업을 시작할 때는 다른 SAVE 단계보다 먼저 현재 상태를 `log.md`에 작성한다. 이후에만 `changelog.md` 이관, README 반영, 커밋·push, Notion 기록을 진행한다.
- 사용자 버그 수정 요청은 작업 도중이 아니라 `SAVE` 요청 시 Notion Trouble Shooting(https://app.notion.com/p/Trouble-Shooting-334c4a0ecd31804f8e04feedb95813dc) 아래에 기록한다. 한국 시간 기준 당일 날짜(`YYYY-MM-DD`)를 제목으로 하루 한 페이지를 사용하며, 같은 날짜 페이지가 있으면 기존 내용을 보존하고 이어 쓴다. 각 건에 어떤 오류·버그였는지, 사용자가 어떤 방식으로 수정을 요청했는지(추가 요청·정정 포함), 수정 내용과 검증 결과, 최종 해결 여부·남은 문제를 적는다. 미검증·미해결 사항을 해결된 것으로 기록하지 않는다. 개발 일지와 별도로 진행한다.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
