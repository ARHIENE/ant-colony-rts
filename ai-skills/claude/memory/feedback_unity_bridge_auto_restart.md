---
name: feedback-unity-bridge-auto-restart
description: unity-cli 브릿지가 도메인 리로드/Play 진입으로 데드락되면 사용자 확인 없이 바로 taskkill 후 재시작할 것
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 1053fe77-4abc-4a7e-9ee6-7081a60d3554
  modified: 2026-09-04T15:16:52.975Z
---

unity-cli 브릿지가 스크립트 재컴파일(도메인 리로드) 또는 Play 모드 진입 후 응답 없음 상태에 빠지면(기존에 알려진 미해결 버그, [[project_ant_teamimport_and_bridge_deadlock]] 참고), 매번 사용자에게 재시작 여부를 물어보지 말고 바로 taskkill로 해당 프로젝트의 Unity 프로세스를 강제 종료한 뒤 같은 프로젝트 경로로 재실행할 것.

**Why:** 2026-09-04 ant 프로젝트 세션에서 재시작 여부를 물었더니 사용자가 "앞으로 이런 일 있으면 너가 재시작해"라고 명시적으로 승인. 이 문제가 반복적으로 발생하는 것으로 확인됨(FPS Manager 세션에서도 동일 증상, watchdog.ps1 도입 이유이기도 함).

**How to apply:**
- 재시작 방법: `taskkill /PID <해당 Unity 프로세스 PID> /F` → `Start-Process -FilePath "<Unity.exe 경로>" -ArgumentList '-projectPath "<프로젝트 경로>"'` (경로에 공백 있으면 ArgumentList를 문자열 하나로 합칠 것, 배열로 나누면 인자가 깨짐)
- 여러 프로젝트의 Unity 에디터가 동시에 열려 있을 수 있으므로, 프로세스 목록에서 창 제목(`MainWindowTitle`)으로 대상 프로젝트를 정확히 식별한 뒤 그 PID만 종료할 것 — 다른 프로젝트 에디터를 실수로 죽이지 않도록 주의
- 재시작 후 `unity-cli system ping`이 성공할 때까지 폴링해서 브릿지가 살아났는지 확인 (맵이 크면 로딩에 시간이 걸릴 수 있음)
- 다만 사용자가 자리에 없는 것이 명백한 상황(예: 자동화 스크립트가 백그라운드로 도는 중)에서는 기존 규칙대로 에디터 창에 강제 포커스/입력 주입은 여전히 하지 말 것 — taskkill+재시작 자체는 이제 사용자 확인 없이 바로 진행 가능하다는 뜻

**예외 — 사용자가 직접 에디터를 조작 중일 때는 절대 재시작하지 말 것 (2026-09-04 수정)**: 사용자가 "재기동 그만해 내가 플레이 눌러도 니가 자꾸 꺼버리잖아"라고 명시적으로 항의함 — 사용자가 직접 Play 버튼을 누르거나 에디터를 조작하고 있는 도중에 Claude가 (자기 자신의 브릿지 커맨드가 데드락났다고) taskkill을 하면 사용자의 Play 세션/작업이 통째로 날아감. **How to apply(수정)**: 사용자가 방금 Play를 눌렀다거나 직접 테스트 중이라고 말했으면, 그 이후로는 Claude가 브릿지 데드락을 만나도 자동 재시작하지 말고 먼저 사용자에게 상태를 알리고 확인받을 것. 자동 재시작은 어디까지나 "Claude 혼자 스크립트 수정→컴파일 확인" 같은 순수 자동화 작업 중에만 적용되는 것이지, 사용자가 세션에 함께 개입하고 있다는 신호가 있으면 그 순간부터 다시 확인을 받아야 함.
