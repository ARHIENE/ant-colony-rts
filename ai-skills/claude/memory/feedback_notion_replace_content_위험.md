---
name: feedback-notion-replace-content
description: Notion 페이지에서 replace_content + allow_deleting_content:true를 쓰면 마크다운 링크로만 참조된 하위 페이지가 휴지통으로 이동될 수 있다
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 11c9d8e6-7adb-46cc-a98e-501a7be2669c
  modified: 2026-09-03T16:31:49.622Z
---

Notion `update-page`의 `replace_content` 명령을 `allow_deleting_content: true`와 함께 쓰면, 기존 콘텐츠에 있던 하위 페이지(서브페이지 블록)가 새 콘텐츠에서 실제 서브페이지 블록이 아니라 단순 마크다운 링크(`[제목](url)`)로만 참조될 경우 "참조 안 됨"으로 판단되어 휴지통으로 이동된다.

**Why:** 2026-08-27 FPS Manager 프로젝트 기획 문서 파트 분리 작업 중, 방금 만든 8개 하위 페이지를 부모 페이지 본문에 마크다운 링크로만 연결하고 `replace_content`(allow_deleting_content: true)로 부모 내용을 교체했다가 8개 페이지가 전부 휴지통으로 이동됨. `move-pages`로도 복구 불가("in trash" 상태는 이동 자체가 거부됨) — 결국 동일 내용으로 재생성해야 했음.

**How to apply:**
- 페이지에 하위 페이지(서브페이지)가 있는 상태에서 본문을 전면 교체해야 할 때는 `replace_content` 대신 `update_content`(부분 old_str/new_str 치환)를 우선 고려한다.
- `allow_deleting_content: true`는 정말 그 하위 페이지/DB를 삭제할 의도가 명확할 때만 사용한다.
- 부득이 `replace_content`를 써야 한다면, 먼저 하위 페이지 목록을 확인하고 새 콘텐츠 작업 후 즉시 하위 페이지가 살아있는지 fetch로 재확인한다.

**2차 사고(2026-09-04 확인, 복구 안 됨):** 위 08-27 사고 때는 동일 내용으로 재생성해서 복구됐었으나, 이후 세션(ant 프로젝트 기획서 생성, 2026-09-02~03 추정)이 같은 페이지(FPS Manager "기획(스펙 문서)", https://app.notion.com/p/334c4a0ecd3180c4a796e5220302a0bd)에 또 같은 실수를 저질러 이번엔 FPS Manager 원본 내용을 ant 기획 내용으로 완전히 덮어씀. 이번엔 원본 내용을 몰라서 복구 불가 — 사용자 확인 결과 그대로 유실 처리, FPS Manager는 당분간 우선순위 낮춤. **새 프로젝트의 기획 문서를 만들 때 반드시 새 페이지를 생성할 것 — 기존에 같은 제목("기획(스펙 문서)" 등)의 페이지가 있는지 search로 먼저 확인하고, 있다면 그게 어느 프로젝트 것인지 fetch로 내용 확인 후 절대 재사용/덮어쓰지 말 것.**
