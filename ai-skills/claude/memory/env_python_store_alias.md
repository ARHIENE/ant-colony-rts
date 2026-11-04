---
name: env-python-store-alias
description: "Bash의 python은 Windows 스토어 별칭이라 아무것도 실행 안 하고 \"Python\"만 출력 — 스크립트 처리는 node 사용"
metadata:
  node_type: memory
  type: reference
  originSessionId: 967359c9-6524-4976-a253-371e6ef46fc9
  modified: 2026-10-31T06:00:58.945Z
---

이 PC의 Bash에서 `python`/`python -`는 실행되지 않고 "Python"만 찍고 끝난다(에러 없이 조용히 실패, 2026-10-31 확인). 파일 가공 스크립트는 `node -e`로 작성할 것.
또 ant의 run_script(Unity 에디터 안)에서는 전역 `ResourceType` 타입과 충돌하므로 검사 코드에 `AntColony.Data.ResourceType` 완전수식명을 쓸 것(별칭 using도 CS0576로 실패). TeamImport 어셈블리에 전역 `SelectableObject` 등도 있어 같은 문제 — 에디터 run_script 검사 코드는 AntColony.* 타입을 완전수식명으로 쓸 것.
