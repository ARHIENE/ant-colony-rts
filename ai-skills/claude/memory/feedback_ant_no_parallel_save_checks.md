---
name: feedback_ant_no_parallel_save_checks
description: ant 저장 왕복 검사(FurnitureBatch*/HygienePower 등)는 run_script를 병렬로 돌리지 말 것
metadata:
  node_type: memory
  type: feedback
  originSessionId: b2ed0f45-deae-461a-a673-bd9a793d3735
  modified: 2026-10-06T14:55:16.328Z
---

ant 프로젝트의 저장 왕복 검사들은 정적 `SaveStorage.RootOverride`를 임시 폴더로 바꿨다가 finally에서 되돌린다. 병렬 run_script로 돌리면 한 검사가 원래 값(null = 실제 persistentDataPath)으로 되돌린 사이 다른 검사가 저장해 **실제 사용자 슬롯을 덮어쓸 수 있다**. 2026-10-06 병렬 실행 중 Batch5가 60초 시간 초과(실제 저장 파일은 무사, md5 확인).

**Why:** 사용자 저장 파일 보호. 자동 승인 심사도 같은 이유로 실행을 거부한 전례 있음.
**How to apply:** 저장·로드를 하는 검사는 한 번에 하나씩 순차 실행. 시작 전 `%AppData%\..\LocalLow\DefaultCompany\ant\saves` md5 백업, 끝나고 대조. 관련: [[project_ant_goal_full_spec]]

**추가(2026-10-07):** 순차라도 같은 Play 세션에서 다른 검사(Phase4·Workforce 등) 뒤에 SaveRoundtripChecks를 돌리면 상태 오염으로 가짜 실패(`barracks tier`, `Sequence contains no matching element` 73행)가 난다. 저장 왕복 검사는 editor_stop → editor_play 새 세션에서 실행할 것. 새 세션에선 57개 반복 통과.
