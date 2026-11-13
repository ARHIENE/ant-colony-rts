> 작업 전에 [GAME_DIRECTION.md](GAME_DIRECTION.md)를 먼저 읽고, [구현 작업 현황](docs/IMPLEMENTATION_BOARD.md)에서 담당 범위를 확인한다.

## 복원 환경 (2026-11-05)

- 프로젝트: F:\UnityProjects\ant-colony-rts, develop 브랜치.
- 사용자가 지정한 도구는 Unity Hub가 설치·업데이트하는 Unity 공식 CLI이다. 특정 버전에 고정하지 않고, Akiojin unity-cli와 혼동하지 않는다.
- CLI 실행: & 'F:\_tools\unity-official\unity-managed.ps1' <인수>. 매 실행마다 최신 사용자 PATH의 Unity\bin\unity.exe를 찾아 실행하므로 Hub 업데이트가 자동 반영된다. 별도 다운로드한 F:\_tools\unity-official\bin\unity.exe는 사용하지 않는다. 공식 설치가 보이지 않으면 접근 문제를 보고하고 구버전으로 대체하지 않는다.
- 에디터 연결은 기존 com.unity.pipeline 0.7.0-exp.1을 사용한다. 공식 CLI 버전과 Unity 에디터 버전은 별개이며, 프로젝트 에디터 버전은 6000.5.8f1이다.
- 예전 외부 com.akiojin.unity-cli-bridge 의존성은 공식 CLI 사용 요청에 따라 manifest/lock에서 제거했다. 원본 설정 백업: F:\_tools\recovery-backups\2026-11-05.
- Graphify 실행 파일: C:\Users\gusdu\Documents\Codex\tools\bin\graphify.exe. PATH에 없으면 절대 경로를 사용한다. Python 경로는 graphify-out/.graphify_python에 복원되어 있다.

- GitHub 저장: 연결된 GitHub 플러그인의 ARHIENE/ant-colony-rts 쓰기 권한 및 develop ref 업데이트 인증을 2026-11-05 확인했다. 플러그인 인증은 로컬 git push 자격 증명과 별개다. SAVE 시 플러그인으로 원격 커밋·브랜치를 갱신할 수 있으며, 원격 head 재확인과 기대 SHA 조건으로 동시 변경을 보호한다. 로컬 변경 보존 후 이력을 동기화하고 임의 force push나 hard reset을 하지 않는다.

- Unity 연결 검증(2026-11-05): E:\Unity\6000.5.8f1\Editor\Unity.exe로 이 프로젝트를 열고 CLI 및 MCP editor_status의 ready 응답을 확인했다. 공식 MCP 도구 155개 탐색·호출 성공, Auto-tick 활성화. 샌드박스에서 전체 에디터 목록 감지가 비어 있을 수 있으므로 연결 판단은 unity command editor_status --project-path F:\UnityProjects\ant-colony-rts 응답으로 한다.

# 프로젝트 작업 규칙

- 코드 작성과 수정은 Codex와 Claude Code가 협업한다(2026-10-07 사용자 요청). 시작 전에 `docs/IMPLEMENTATION_BOARD.md`를 읽고 담당·수정 파일을 표시한다. 다른 담당자가 작업 중인 파일은 동시에 수정하지 않는다.
- `master`는 안정 버전으로 유지하고 일반 개발은 `develop`에서 진행한다.
- SAVE 작업을 시작할 때는 다른 SAVE 단계보다 먼저 현재 상태를 `log.md`에 작성한다. 이후에만 `changelog.md` 이관, README 반영, 커밋·push, Notion 기록을 진행한다.
- 사용자가 `SAVE`를 요청하면 기존 전역 SAVE 절차와 함께 `README.md`를 확인한다.
- Notion에 글을 새로 쓰거나 수정할 때는 항상 `humanizer` 스킬(`C:/Users/gusdu/.codex/skills/humanizer/SKILL.md`)을 읽고 적용한다(2026-11-13 사용자 요청). SAVE 개발 일지·트러블슈팅·기획·작업 기록 등 모든 Notion 문서에 적용하며, 게시 전에 자연스러운 한국어로 다듬는다. 사실·수치·날짜·고유명사·코드·링크·검증 결과와 미구현·미검증 상태는 보존하고, 기존 날짜 목록·아이콘·첨부 형식도 유지한다.
- 사용자 버그 수정 요청은 작업 도중이 아니라 `SAVE` 요청 시 Notion Trouble Shooting(https://app.notion.com/p/Trouble-Shooting-334c4a0ecd31804f8e04feedb95813dc) 아래에 기록한다. 한국 시간 기준 당일 날짜(`YYYY-MM-DD`)를 제목으로 하루 한 페이지를 사용하며, 같은 날짜 페이지가 있으면 기존 내용을 보존하고 이어 쓴다. 각 건에 어떤 오류·버그였는지, 사용자가 어떤 방식으로 수정을 요청했는지(추가 요청·정정 포함), 수정 내용과 검증 결과, 최종 해결 여부·남은 문제를 적는다. 미검증·미해결 사항을 해결된 것으로 기록하지 않는다. 개발 일지와 별도로 진행한다.
- 사용자가 `SAVE`를 요청하면 검증한 작업 변경사항을 `develop`에 커밋하고 `origin/develop`에 push한다. 커밋·push를 별도 요청으로 미루지 않는다. `master` 반영은 별도 요청이 있을 때만 진행한다.
- SAVE의 Notion 개발 일지는 한국 시간 기준 같은 날짜의 ant 기록이 있으면 해당 페이지의 기존 내용을 보존하고 이어서 작성한다. 같은 날짜 페이지는 새로 만들지 않는다. 같은 날짜의 ant 페이지가 여러 개면 가장 최근 페이지에 이어 쓴다.
- SAVE의 개발 일지 표정 아이콘은 그날의 실제 작업 결과·검증·남은 문제를 읽고 선택한다. 이전 아이콘을 그대로 복사하거나 `😅` 등 하나로 고정하지 않는다. 예: 순조로운 진척 `🙂`, 문제 해결 후 안정화 `😌`, 성과와 미해결 문제가 섞인 날 `😅`, 막힘·실패가 큰 날 `😓`/`😵`. 이는 선택 참고이며 사용자 지정 표정이 우선한다. 사용자가 수정한 과거 표정을 임의로 재평가·덮어쓰지 않는다.
- Notion 개발 일지의 날짜별 바로가기는 기존 사용자 형식인 `- 😌 [YYYY-MM-DD](페이지 URL)`처럼 **표정 + 일반 날짜 하이퍼링크**로 작성한다. 날짜 목록을 `<mention>`/페이지 멘션으로 바꾸지 않는다. 페이지 제목은 날짜만, 페이지 아이콘은 선택한 표정으로 두고 해당 날짜의 목록 표정과 맞춘다. Notion 도구의 기본 멘션 권장보다 이 사용자 지정 표시 형식을 우선한다.
- 날짜별 바로가기는 현재 사용자 정리본처럼 **월 섹션은 최신 월부터, 각 월 안의 날짜는 오름차순**을 유지한다. 최신 날짜를 무조건 목록 맨 위에 prepend하지 않는다. 같은 날짜 링크가 있으면 중복 추가하지 않으며, 기존 중복 날짜 기록은 삭제하거나 순서를 임의 변경하지 않는다. 원본 하위 페이지 블록은 날짜별 바로가기와 별개이며 보존한다.
- SAVE 시 부모 개발 일지와 당일 페이지를 먼저 읽고 현재 형식을 확인한다. 기록 후 당일 아이콘/제목, 일반 링크 여부, 날짜 삽입 위치, 중복 여부와 GIF·사진 첨부를 확인한다. 사용자가 직접 고친 목록은 기준으로 삼으며, 별도 지시 없이 전체 목록을 재작성하지 않는다.
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
