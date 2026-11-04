---
name: project-ant-goal-full-spec
description: "ant 프로젝트 목표가 MVP에서 기획서 전체 구현으로 확장됨, FPS Manager는 당분간 우선순위 낮음"
metadata: 
  node_type: memory
  type: project
  originSessionId: bf42b02d-a070-491a-9e79-6cc098a56a0d
  modified: 2026-09-03T16:32:00.444Z
---

ant(개미 소굴 RTS) 프로젝트의 목표가 2026-09-04부로 "MVP 수직 슬라이스 완성"에서 "Notion 기획(스펙 문서) 페이지에 적힌 내용 전체 구현"으로 바뀜.

**Why:** 사용자가 명시적으로 "다음부터 목표를 저 기획서에 적혀있는 모든 내용 구현으로 하자"고 지시. 동시에 "FPS Manager은 앞으로 잘 안 할거임, ant부터 끝내고 할거야"라고 밝혀서 당분간 여러 프로젝트 중 ant가 최우선순위임.

**How to apply:**
- ant 작업 요청을 받을 때 MVP 스코프로 임의로 제한하지 말고, 기획서(자원/유닛/건물/전투 시스템 전체, [[project_ant_teamimport_and_bridge_deadlock]] 참고)를 기준으로 확장 작업을 진행할 것. 다만 여전히 대규모 변경은 사용자 확인 후 진행하는 기존 원칙은 유지.
- 기획이 개발 중 바뀌면 Notion 기획 문서의 해당 하위 페이지를 교체(추가 아님)하고 관련 페이지도 동기화 — CLAUDE.md에 규칙 반영됨.
- FPS Manager/1VS1 Game 관련 요청이 오면 이 프로젝트가 당분간 후순위라는 사용자 코멘트를 참고하되, 명시적으로 요청하면 당연히 수행.
