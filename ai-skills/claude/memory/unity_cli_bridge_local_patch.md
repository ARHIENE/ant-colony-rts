---
name: unity-cli-bridge-local-patch
description: "FPS Manager에서 akiojin/unity-cli-bridge를 Unity 6000.5용으로 직접 패치해서 로컬 file: 패키지로 쓰는 이유와 위치"
metadata: 
  node_type: memory
  type: project
  originSessionId: b81ebb2b-d9c0-4715-94de-57cf1ed0618a
  modified: 2026-09-05T03:14:11.137Z
---

FPS Manager 프로젝트(Unity 6000.5.8f1)에서 Claude Code가 Unity 에디터를 직접 조작(Play 진입, 콘솔 읽기, 프리팹 편집, 스크린샷)할 수 있게 해주는 `unity-cli`(akiojin/unity-cli-bridge)를 설정함(2026-08-29).

**문제**: 공식 git 패키지(`https://github.com/akiojin/unity-cli.git?path=...`)를 그대로 추가하면 `Object.GetInstanceID()`가 Unity 6000.5부터 obsolete-error라 컴파일이 안 됨. 업스트림 이슈([akiojin/unity-cli#231](https://github.com/akiojin/unity-cli/issues/231))는 미해결 상태.

**해결**: `E:\Git\_tools\unity-cli`에 저장소를 클론해두고, `GetInstanceID()` 7군데를 `GetEntityId().GetHashCode()`로 직접 수정한 로컬 사본을 유지. FPS Manager의 `Packages/manifest.json`은 이 로컬 경로를 `file:../../_tools/unity-cli/UnityCliBridge/Packages/unity-cli-bridge`로 참조함(git 패키지가 아니라 로컬 패키지).

**CLI 바이너리**: `~/.local/bin/unity-cli.exe` (Windows용, `unity-cli-win-x64` 릴리스에서 받음). 실행 시 `UNITY_PROJECT_ROOT` 환경변수를 프로젝트 경로로 설정해야 함.

**주의**: `E:\Git\_tools`는 별도 git 저장소(FPS Manager 저장소 밖)라 FPS Manager 커밋에는 안 잡힘. 업스트림이 고쳐지면 공식 git 패키지로 되돌리는 걸 고려할 것. [[fps_manager_save_mlagents_report]]에서 이 브릿지로 스크린샷을 찍는 흐름과 연결됨.

**추가 패치 (2026-08-29 세션 2)**: "Play 모드 진입 후 자주 멈춤" 버그의 근본 원인을 규명 — `UnityCliBridgeHost.cs`의 `StartTcpListener()`가 도메인 리로드 직후 포트 재바인딩 실패(`AddressAlreadyInUse`)시 재시도 없이 그냥 포기하고 영구 Error 상태로 남는 게 원인이었음. 같은 파일에 논블로킹 재시도(0.5초→최대 5초 백오프, 무제한 재시도) 로직을 uncommitted로 추가 패치함(기존 GetInstanceID 패치와 같은 방식/같은 파일 트리, `git diff`로 전체 로컬 patch 확인 가능). **다음 세션에서 확인할 것**: Editor.log에 `Status changed to: Connected`가 안정적으로 나오는지 — 패치 직후 세션에선 스크립트를 연달아 여러 개 고치며 도메인 리로드가 계속 겹쳐서 재시도 로직이 도는 것까진 확인했지만 완전히 안정된 상태까지는 못 봤음.

**ant-colony-rts 프로젝트에도 동일 브릿지 적용 (2026-09-02)**: `E:\Git\ant`의 `Packages/manifest.json`에 같은 `file:../../_tools/unity-cli/UnityCliBridge/Packages/unity-cli-bridge` 참조 추가. **포트는 16401**로 설정(`ProjectSettings/UnityCliBridgeSettings.asset`) — FPS Manager의 16400과 겹치지 않게, 두 프로젝트 에디터가 동시에 떠 있어도 충돌 안 나도록. CLI 사용 시 `--port 16401` 지정.

**교훈 — `GetInstanceID()`는 브릿지 패치만의 문제가 아니라 Unity 6000.5 전반의 obsolete-error임**: ant-colony-rts에서 직접 작성한 게임 스크립트(`MapGenerator.cs`, SIMUL-TeaamProject 참고 포팅)에서도 `prefab.GetInstanceID()`를 썼다가 동일하게 `error CS0619`로 컴파일 실패함. **앞으로 Unity 6000.5+ 프로젝트에 새 스크립트를 쓸 때는 처음부터 `GetInstanceID()` 대신 `GetEntityId().GetHashCode()`(또는 상황에 맞는 `EntityId` 비교)를 쓸 것** — 발생 후 고치지 말고 미리 피할 것.

**재컴파일 시 브릿지 데드락의 실제 근본 원인 규명 + 수정 (2026-09-04, ant 세션)**: "스크립트 재컴파일/Play 진입마다 데드락"이 막연히 "브릿지 자체 버그"로만 기록돼 있었는데, 이번에 소스(`Editor/Core/BridgeCommandRouter.cs`)를 직접 읽어 정확한 메커니즘을 확인함.
- **원인**: `refresh_assets` 커맨드 핸들러가 `AssetDatabase.Refresh()`를 커맨드 처리 호출 스택 안에서 바로(동기) 호출하고 있었음. 변경된 스크립트가 있으면 Unity가 이 호출 안에서 도메인 리로드까지 전부 동기적으로 수행하는데, 그 리로드가 지금 이 응답을 만들어 보내는 중이던 바로 그 호출 스택 자체를 중간에 끊어버려서 클라이언트가 응답을 영영 못 받고 자기 타임아웃까지 멈춤. **재현 확인됨**(같은 세션에서 반복 재현 후 수정 후 재현 안 됨 확인).
- **1차 시도(실패)**: `EditorApplication.delayCall`로 `AssetDatabase.Refresh()`를 다음 틱으로 미루는 방식은 컴파일은 됐지만 **유니티 창이 백그라운드(포커스 없음)일 때 delayCall이 아예 실행되지 않는 현상 확인**(25초+ 폴링해도 재컴파일 자체가 안 일어남 — `lastCompilationTime`이 안 바뀜). 사용자가 "포커스 안 줘서 그런거 아니냐"고 물은 게 이 부분은 정확히 맞았음.
- **최종 수정**: `delayCall` 대신 `EditorApplication.update`에 1회성으로 거는 방식(`ScheduleDeferredAssetRefresh`/`RunDeferredAssetRefreshOnce`, 기존 `UnityCliBridgeHost.cs`의 `BindRetryUpdate` 패턴과 동일)으로 교체. `EditorApplication.update`는 포커스 없어도 계속 도는 게 확인됨(브릿지 커맨드 큐 처리 자체가 이걸로 돌아가고 있었고 실제로 백그라운드에서도 잘 동작 중이었음). 수정 후 검증: 실제 스크립트 변경 → `refresh_assets` 호출 → 12회 연속 폴링 전부 정상 응답 + `lastCompilationTime` 실제 갱신 확인, 재시작 불필요.
- **적용 범위**: `E:\Git\_tools\unity-cli\UnityCliBridge\Packages\unity-cli-bridge\Editor\Core\BridgeCommandRouter.cs`를 직접 고침(uncommitted, 기존 GetInstanceID 패치와 같은 로컬 트리) — ant/FPS Manager 둘 다 이 경로를 `file:` 참조하므로 두 프로젝트 다 이 수정의 혜택을 받음(각 프로젝트 에디터가 이 패키지 변경을 인식하고 재컴파일하는 시점부터 적용).
- **남은 잔여 리스크**: `play_game` 핸들러(`PlayModeHandler.cs`)는 원래부터 `EditorApplication.isPlaying = true`를 기본 300ms 지연 후 `EditorApplication.update` 콜백으로 설정하고 있어서 자기 자신은 이미 안전했음. 다만 Play 진입 직후 상태를 반복 폴링(`get_editor_state`/`get_compilation_state`)하는 호출이 마침 그 지연된 리로드 타이밍과 겹치면 그 폴링 호출 하나가 여전히 멈출 수 있음 — `refresh_assets`처럼 "커맨드 핸들러 자신이 리로드를 직접 유발"하는 패턴은 못 찾았지만, Unity가 백그라운드에서 자체적으로(Auto Refresh) 리로드를 트리거하는 타이밍과 우연히 겹치는 경우까지는 구조적으로 막을 수 없음. 그래도 [[feedback_unity_bridge_auto_restart]]의 자동 재시작 워크플로가 폴백으로 남아있어 실무상 문제 없음.

**"브릿지는 응답하는데 게임 상태(좌표 등)가 안 바뀐다" 증상은 데드락이 아니라 `runInBackground` 문제일 수 있음 (2026-09-04, ant 세션에서 발견)**: `ProjectSettings/ProjectSettings.asset`의 `runInBackground: 0`이면, 유니티 창에 OS 포커스가 없을 때 Play 모드의 실제 시뮬레이션(Time.deltaTime 기반 Update 루프)이 사실상 멈춘다. 브릿지의 커맨드 처리는 `EditorApplication.update` 기반이라 이것과 별개로 계속 정상 응답하기 때문에, 겉으로는 "브릿지는 멀쩡한데 게임만 멈춘" 것처럼 보임 — 도메인 리로드 데드락과 증상이 다르니 혼동하지 말 것(전자는 ping/get_editor_state 자체가 타임아웃, 후자는 그것들은 응답하는데 오브젝트 좌표만 폴링마다 그대로임). `runInBackground: 1`로 켜면 해결. unity-cli로 자동 Play 테스트를 하는 프로젝트라면(FPS Manager 포함) 처음부터 이 값을 켜두는 걸 권장 — 자세한 진단 과정은 [[project_ant_teamimport_and_bridge_deadlock]] 참고.

**`unity-cli`는 MCP 도구가 아니라 순수 로컬 CLI 바이너리다 (2026-09-05 재확인)**: Claude Code의 MCP 도구 목록(ToolSearch)에는 절대 안 뜬다 — 애초에 MCP 서버로 등록된 적이 없고(`.claude.json`의 어떤 project entry에도 unity 관련 mcpServers 없음), Bash에서 `unity-cli` 커맨드를 직접 실행하는 방식으로 써야 한다("연결 안 됨"이 아니라 "그런 MCP 자체가 없는 게 정상"). 포트는 프로젝트마다 다르므로(위 참고: ant=16401, FPS Manager=16400) 매번 `--port` 지정 필수 — 기본값 6400으로 찌르면 연결 거부됨. 유용한 서브커맨드: `system ping`(생존확인), `tool list`/`tool schema <name>`(툴 목록/스키마), `tool call <name> --json '{...}'`(실행 — `--args` 아니라 `--json`), `get_compilation_state`(에러/경고 카운트), `get_hierarchy`/`find_gameobject`/`get_gameobject_details`(씬 조회), `get_component_values`/`set_component_field`(필드 read/write, `includePrivateFields:true`로 private 필드도 조회 가능), `play_game`/`stop_game`, `capture_screenshot`(`captureMode:"game"`, Read 툴로 결과 png를 바로 볼 수 있음).

**브릿지가 데드락났는데 씬에 미저장 변경사항이 있으면, taskkill 전에 OS 레벨 Ctrl+S를 먼저 시도할 것 (2026-09-05)**: 브릿지(TCP 통신)가 죽어도 Unity 에디터 자체(OS 메시지 펌프)는 살아있는 경우가 있음 — Win32 `IsHungAppWindow(hwnd)`로 확인 가능(False면 메인 스레드는 정상). 이 경우 PowerShell에서 `SetForegroundWindow(hwnd)` → `[System.Windows.Forms.SendKeys]::SendWait("^s")`로 Ctrl+S를 직접 주입하면 브릿지를 안 거치고도 저장됨. 저장 성공 여부는 `.unity` 씬 파일의 mtime(파일시스템 타임스탬프)으로 확인할 것(창 제목의 `*`이 사라지는 것도 보조 신호). 이 방법으로 실제 데이터 손실 없이 taskkill+재시작 가능했음. `Add-Type`으로 정의한 커스텀 P/Invoke 타입은 PowerShell 툴 호출마다 새 프로세스라 안 남는다는 점 주의(매번 재정의 필요하거나, `System.Windows.Forms.SendKeys`처럼 표준 어셈블리만 쓰면 재정의 불필요).

**Play 모드에서 `input_mouse`(click/move/drag)로 실제 게임 클릭을 시뮬레이션하는 건 이 환경에서 신뢰할 수 없었다 (2026-09-05)**: `get_current_input_state`가 보고하는 마우스 좌표는 갱신되지만, 게임 오브젝트가 실제로 참조하는 `Mouse.current`(새 Input System)에는 클릭이 전달 안 되는 것으로 보임 — `SelectionManager`의 `_selectedObjects`/`_dragStartScreen`을 반복 확인해도 이전 값에서 전혀 안 바뀜(에디터가 OS 포커스를 실제로 못 받는 게 원인으로 추정, `manage_windows`의 `focus` 액션은 Unity 내부 포커스만 바꾸고 실제 OS 창 포커스는 못 바꾸는 듯). **UX 클릭 흐름(우클릭 명령, 드래그 선택 등)을 자동 검증하고 싶으면 마우스 시뮬레이션 대신 `get_component_values`/`set_component_field`로 대상 컴포넌트의 상태(예: private `_state` enum, `_targetNode` 참조)를 직접 읽어 로직을 검증하거나, 사용자에게 직접 클릭해서 확인해달라고 요청할 것.** 참고로 `set_component_field`의 `objectReference`는 **프로젝트 에셋(assetPath/guid)만** 연결 가능하고 씬 안의 다른 GameObject/컴포넌트 인스턴스는 참조로 못 넣음(씬-투-씬 참조 주입 불가) — `ComponentHandler.cs`의 `ResolveObjectReference` 참고.
