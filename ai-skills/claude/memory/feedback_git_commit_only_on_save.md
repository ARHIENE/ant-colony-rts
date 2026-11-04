---
name: feedback-git-commit-only-on-save
description: 커밋은 SAVE 명령 시에만(또는 명시적으로 지금 커밋하라고 승인받았을 때만) 진행할 것
metadata: 
  node_type: memory
  type: feedback
  originSessionId: ca8019d3-8f1f-401a-8af1-14ea1a0695d6
  modified: 2026-09-02T13:49:13.339Z
---

작업 중 파일 변경이 끝났다고 곧바로 `git commit`을 실행하지 말 것. 커밋은 사용자가 "SAVE"라고 입력했을 때, 또는 그 세션에서 명시적으로 "지금 커밋해줘/push해줘"라고 승인했을 때만 진행한다.

**Why:** 사용자가 직접 "커밋을 왜 지금해? 내가 SAVE할때만 해야지?"라고 지적함(2026-09-02, ant-colony-rts 프로젝트에서 MapGenerator 포팅 작업 후). 전역 CLAUDE.md의 SAVE 절차는 원래 1VS1 Game/FPS Manager 프로젝트를 명시했지만, 사용자 의도는 프로젝트 불문하고 "커밋 시점은 사용자가 SAVE로 직접 통제"하는 것.

**How to apply:**
- 코드/문서 변경을 끝냈으면 `git add`까지만 하거나, 그것도 굳이 먼저 할 필요 없이 변경 내역만 만들어두고 어떤 파일이 바뀌었는지 사용자에게 알리는 선에서 멈춘다.
- 예외: 같은 턴에서 사용자가 AskUserQuestion 등으로 "지금 커밋/push해라"라고 명시적으로 답한 경우(예: 새 프로젝트 최초 Git 전환 시 "GitHub 저장소도 지금 생성" 같은 답변)는 그 승인 범위 내에서 커밋해도 됨 — 이건 SAVE와 별개로 유효한 명시적 지시.
- 애매하면 커밋하지 말고 "커밋할까요?"라고 먼저 물어볼 것.
- [[system_save_procedure]] (전역 CLAUDE.md SAVE 절차)와 연결됨 — SAVE 시 어떤 프로젝트든 그 시점까지의 누적 변경을 한 번에 커밋하는 흐름으로 이해할 것.
