---
name: feedback_reference_github_repos
description: 기능·UI 구현 전에 GitHub에서 비슷한 구현 레포(별 많은 순)를 찾아 기준으로 삼고 반영
metadata:
  node_type: memory
  type: feedback
  originSessionId: 13fff952-aaf1-4403-8a25-e1595cbeb6dd
  modified: 2026-10-25T12:30:01.363Z
---

기능·UI(설정창, 선택 하이라이트, RTS 조작 등)를 구현할 때는 먼저 GitHub에서 비슷한 내용을 구현한 레포지토리를 별(star) 많은 순으로 찾아 기준으로 삼고, 반영할 수 있는 부분은 반영한다(2026-10-25 사용자 지시).

**Why:** 기획 문서만 보고 자체 판단으로 만든 UI·조작이 "게임답지 않다", "설정창에 있어야 할 게 없다"는 불만을 낳음. 검증된 구현을 기준으로 삼으라는 요구.
**How to apply:** 구현 착수 전에 GitHub 검색(별순 상위) → 구조·항목·동작을 요약 → 우리 코드에 맞게 반영. 참고한 레포는 핸드오프·개발 일지에 기록. 관련: [[feedback_auto_pick_recommended]]
