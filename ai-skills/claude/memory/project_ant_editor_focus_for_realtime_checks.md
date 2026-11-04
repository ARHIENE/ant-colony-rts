---
name: project_ant_editor_focus_for_realtime_checks
description: "ant Unity 검사는 포커스 뺏지 말 것(사용자 요청 2026-11-01). runInBackground+폴링으로 포커스 없이 Play·검사 가능"
metadata:
  node_type: memory
  type: project
  originSessionId: b2ed0f45-deae-461a-a673-bd9a793d3735
  modified: 2026-11-01T12:57:22.269Z
---

2026-11-01 사용자 요청: "포커싱 안 가져가고 할 수 없나? 계속 하고 있는 게 끊긴다" → `editor_focus` 사용 금지. 2026-11-01 확인: Play 중 `Application.runInBackground = true`(eval로 설정, 프로젝트 설정은 안 바꿈)면 창이 뒤에 있어도 프레임이 진행되고(장수 3명 35fps), Play 진입도 `EditorApplication.EnterPlaymode()` 후 `QueuePlayerLoopUpdate()`를 부르는 eval 폴링으로 ~20초에 된다. 실시간 검사(밤 수면·이동, 감독, 회귀) 전부 포커스 없이 통과.
(이전 2026-10-07 기록: 포커스 없으면 프레임이 수십 개로 떨어져 원정 채집 검사가 버그처럼 실패 — 당시엔 editor_focus로 해결했었다. 느린 검사가 시간 초과로 실패하면 이 원인을 먼저 의심.)

**Why:** 포커스를 가져가면 사용자의 다른 작업이 끊김. TaskStop은 안쪽 bash 루프(editor_focus 반복)를 못 죽이므로 남은 프로세스가 계속 포커스를 뺏은 적 있음.
**How to apply:** 검사 러너에서 editor_focus 제거, 중단 시 남은 bash/unity command 프로세스를 PowerShell로 직접 종료. Play 중 코드가 바뀌면 Play 종료→AssetDatabase.Refresh→재컴파일→재진입 후 새 타입 로드 확인(예전 코드로 검사가 돈 사례). 60초 넘는 검사는 `AgentScripts/RunChecks.cs`(SessionState) 사용. 관련: [[feedback_ant_no_parallel_save_checks]], [[feedback_unity_no_editor_restart_in_tests]]
