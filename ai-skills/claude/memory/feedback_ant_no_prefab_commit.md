---
name: feedback-ant-no-prefab-commit
description: ant-colony-rts(개미 소굴 RTS) 프로젝트에서는 .prefab 파일을 git에 커밋하지 말 것
metadata: 
  node_type: memory
  type: feedback
  originSessionId: ca8019d3-8f1f-401a-8af1-14ea1a0695d6
  modified: 2026-09-02T15:42:07.709Z
---

`E:\Git\ant`(ant-colony-rts) 프로젝트에서 `.prefab`/`.prefab.meta` 파일은 앞으로 git에 커밋하지 않는다.

**Why:** 사용자가 직접 요청함(2026-09-03). 정확한 이유는 안 밝혔지만, 지금 프리팹들이 캡슐/구체 등 임시 플레이스홀더 메시라 자주 갈아엎힐 걸로 보임 — 계속 갈아엎이는 바이너리성 에셋을 git 히스토리에 안 쌓겠다는 의도로 추정.

**How to apply:**
- `.gitignore`에 `*.prefab`/`*.prefab.meta` 패턴 추가해둠(2026-09-03)
- 기존에 커밋돼있던 `Assets/Prefabs/WorkerAnt.prefab`/`SoldierAnt.prefab`(+.meta)은 `git rm --cached`로 추적 해제(디스크 파일은 유지) — 다음 SAVE 때 커밋될 예정
- 앞으로 새 프리팹을 만들어도 자동으로 git에서 제외됨(gitignore 패턴이 커버) — 별도 조치 불필요
- 이 규칙이 다른 프로젝트(1VS1 Game, FPS Manager)에도 적용되는지는 불명 — 명시적으로 요청받지 않았으니 그 프로젝트들은 기존 방침(프리팹도 커밋) 유지
