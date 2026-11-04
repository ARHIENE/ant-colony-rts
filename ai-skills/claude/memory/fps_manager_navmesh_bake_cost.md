---
name: fps-manager-navmesh-bake-cost
description: FPS Manager AIBattle5v5 맵 크기를 키우기 전에 NavMesh 베이크 비용부터 확인할 것 - 실측으로 15분+ 응답없음 겪음
metadata: 
  node_type: memory
  type: feedback
  originSessionId: f5836aee-63ee-4b1e-a9bf-0b13cf45cd16
  modified: 2026-08-31T18:38:49.549Z
---

AIBattle5v5 씬에서 `ArenaGenerator.mapWidth/mapDepth`를 52→104(면적 4배)로 키웠더니 `NavMeshSurface.BuildNavMesh()`가 15분 넘게 안 끝나서 Unity 에디터(및 unity-cli 브릿지)가 완전히 응답 없는 상태가 됨(2026-08-31 실측, 결국 원래 크기로 되돌림).

**Why**: `cellSize: 0.16666667`(매우 촘촘한 해상도)로 설정된 상태에서 맵 면적을 키우면 NavMesh 삼각화 비용이 선형보다 훨씬 나쁘게 스케일되는 것으로 보임. 이 프로젝트는 라운드마다가 아니라 **Unity Play 세션 시작 시 딱 한 번만** 베이크하지만(`ArenaGenerator.Start()`), 그 한 번이 이 정도로 오래 걸리면 watchdog 자동 재시작 사이클마다(크래시가 잦은 프로젝트라 자주 발생) 매번 이 비용을 다시 치르게 되어 학습 처리량에 치명적.

**How to apply**: 앞으로 맵을 키우고 싶으면 (1) 먼저 `cellSize`를 늘려서(해상도를 낮춰서) 베이크 비용을 줄이는 실험을 하거나, (2) 작은 증분(예: 1.5배)으로 먼저 베이크 시간을 실측하고 판단할 것 — 한 번에 크게 키우고 반응을 기다리지 말 것. 거점을 진영 끝에 두는 설계(공격팀이 맵을 가로지르게 하는 것) 자체는 맵 크기와 무관하게 유효했음 — 필요하면 맵 크기는 그대로 두고 스폰 배치만으로 이동거리를 늘리는 방향도 고려 가능.
