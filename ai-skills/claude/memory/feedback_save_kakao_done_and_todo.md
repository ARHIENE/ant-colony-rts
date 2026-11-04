---
name: feedback_save_kakao_done_and_todo
description: "사용량 부족 시 SAVE→컴퓨터 종료 필수, Codex 한 일/못한 일도 SAVE에 포함, 카톡=완료+남은 일"
metadata:
  node_type: memory
  type: feedback
  originSessionId: c55db1a6-569b-4adb-b360-2c810c7449ec
  modified: 2026-10-24T17:39:27.900Z
---

1. 작업 중 남은 사용량(토큰/한도)이 얼마 없으면 하던 작업을 멈추고 SAVE 후 컴퓨터를 종료한다. **SAVE와 컴퓨터 종료는 필수**(생략 금지).
2. SAVE 때 Codex 작업도 회수해 "한 일 / 못한 일"을 정리해 같은 SAVE(log.md·커밋·Notion)에 포함한다.
3. SAVE 완료 카카오톡에는 진행한 것과 남은 것을 둘 다 적는다(200자 안).
4. Codex와 병행 시: Codex에 할 일을 요청하고 Claude도 동시에 자기 몫을 진행한다(Codex에 전부 넘기지 않음).

**Why:** 2026-10-25 사용자 지시("남은 사용량이 별로 없다 그럼 save하고 컴퓨터 종료", "codex도 한일, 못한일 정리해서 같이", "꼭 save와 컴퓨터 종료는 필수", "codex에 요청하고 너도 할일 하라").
**How to apply:** Codex 중지·산출물 회수 → 적용/미적용 정리 → log.md → changelog → 커밋·push → Notion 개발 일지 → 카톡 → Unity 정상 종료 → `shutdown /s /t 60`. [[feedback_codex_down_claude_takes_over]]
