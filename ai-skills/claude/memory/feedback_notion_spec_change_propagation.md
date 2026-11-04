---
name: feedback-notion-spec-change-propagation
description: 기획/스펙이 바뀌면 Notion에서 해당 문서만 고치지 말고 관련된 다른 페이지도 전부 같이 수정할 것
metadata: 
  node_type: memory
  type: feedback
  originSessionId: ca8019d3-8f1f-401a-8af1-14ea1a0695d6
  modified: 2026-09-02T15:43:33.299Z
---

기획사항이나 스펙이 변경되어 Notion에 반영할 때, 그 기획 문서 하나만 고치고 끝내지 않는다. 같은 내용을 참조/중복 서술하는 다른 페이지(개발 일지 항목, 다른 파트의 하위 페이지, 관련 체크리스트 등)가 있으면 전부 찾아서 같이 수정한다.

**Why:** 사용자가 직접 요청함(2026-09-03, ant-colony-rts 세션 중이지만 프로젝트 한정 언급 없이 일반적으로 지시함 — 전역 규칙으로 취급). [[feedback_notion_replace_content_위험]], FPS Manager의 "옛 내용 남기지 말고 교체" 규칙과 같은 맥락이지만 이건 "한 문서 내에서 교체"가 아니라 "관련된 여러 문서에 걸쳐 전파"하라는 게 핵심.

**How to apply:**
- 기획 변경 작업을 받으면, 먼저 Notion search/fetch로 그 내용을 언급하는 다른 페이지가 있는지 확인한다(같은 프로젝트의 개발 일지, 다른 스펙 하위 페이지 등).
- 찾은 페이지들도 옛 내용을 새 내용으로 교체(추가 아님, FPS Manager 규칙과 동일)한다.
- 이 규칙은 CLAUDE.md 전역 규칙에도 반영됨(2026-09-03) — 여기 메모리는 배경/사유 기록용.
