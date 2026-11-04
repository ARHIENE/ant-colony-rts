---
name: feedback_unity_no_editor_restart_in_tests
description: "ant 회귀 검사 중 Unity 에디터를 끄거나 재시작하지 말 것 — 씬 백업 대화상자 반복, 사용자 강하게 불만"
metadata:
  node_type: memory
  type: feedback
  originSessionId: 1e6fc266-99e2-42db-bfce-5c87738e9068
  modified: 2026-09-26T07:10:03.011Z
---

회귀 검사를 돌릴 때 Unity 에디터를 강제 종료·재시작하지 말고 한 인스턴스에서 editor_stop→editor_play만 반복한다.

**Why:** 2026-09-26 taskkill/EditorApplication.Exit 재시작마다 "Scene backups ... Assets/_Recovery" 대화상자가 떠서 사용자가 "껐다 키지 말라고"라고 강하게 불만. 또 TaskStop은 바깥 셸만 끝내고 안쪽 `bash script.sh`는 살아남아 멈춘 줄 알았던 러너가 계속 에디터를 재시작했다. 토큰 낭비에도 민감함("토큰아까워") — 실패마다 알림 모니터 두지 말고 끝나면 한 번에 정리.

**How to apply:** 백그라운드 러너를 멈출 땐 TaskStop 후 `Get-CimInstance Win32_Process`로 남은 bash 프로세스까지 확인·종료. 메모리 부족 시에도 재시작 대신 사용자에게 먼저 알린다. [[feedback_unity_bridge_auto_restart]]는 브릿지 데드락 한정.
