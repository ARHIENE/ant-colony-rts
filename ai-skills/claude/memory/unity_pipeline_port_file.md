---
name: unity-pipeline-port-file
description: ant Unity 공식 CLI 연결 불가 시 Library/Pipeline/.unity-pipeline-port 누락 확인 → Window/Pipeline Stop→Start로 복구
metadata:
  node_type: memory
  type: reference
  originSessionId: d03d479b-a2e4-44c0-a05a-23a75aa059fb
  modified: 2026-10-11T11:50:22.363Z
---

2026-10-11 진단: `unity pipeline list`에서 서버 포트가 비고 연결 불가였지만 실제 서버는 7800에서 리슨 중(PID=Unity). 원인은 포트 디스크립터 `Library/Pipeline/.unity-pipeline-port` 누락(콘솔에 "Sharing violation on .unity-pipeline-port" — 다른 CLI/에이전트가 동시에 파일을 잡은 것으로 추정). 토큰은 SessionState 메모리에만 있어 파일 없이는 접속 불가.

**How to apply:** 서버가 떠 있으면 Start Server 메뉴가 비활성이라 사용자에게 Window → Pipeline → **Stop Server → Start Server**를 요청(에디터 재시작·패키지 변경 불필요). 이후 `mcp__unity-editor-mcp__*`로 정상 접속. [[unity-cli-bridge-local-patch]]와 별개.
