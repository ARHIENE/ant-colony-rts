---
name: feedback_codex_down_claude_takes_over
description: Codex가 끊기면(사용량 한도·로그인 만료 등) 기다리지 말고 Claude가 그 묶음을 직접 이어서 구현
metadata:
  node_type: memory
  type: feedback
  originSessionId: 835ff29b-d0d3-4e82-ac5e-a1983342fcc6
  modified: 2026-10-24T02:21:49.305Z
---

Codex 작업이 끊긴 것 같으면 Claude가 직접 이어서 진행한다(2026-10-24 사용자 지시).

**Why:** 사용량 한도(리셋 몇 시간 후)·계정 전환 토큰 만료로 Codex 묶음이 멈춰 진행이 막힌 적 있음.
**How to apply:** status에서 failed/멈춤 확인 → 먼저 broker 재시작(broker.json pid kill + .stale)으로 복구 시도, 안 되면 해당 worktree의 IMPL-Bx-HANDOFF.md 기준으로 Claude가 남은 G 항목을 직접 구현. 본 프로젝트 재컴파일 규칙([[project_ant_teamimport_and_bridge_deadlock]] 등)은 그대로 지킴.
