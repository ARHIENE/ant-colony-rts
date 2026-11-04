---
name: ant_offline_roslyn_compile
description: ant worktree 코드를 Unity 없이 3초 만에 컴파일 확인하는 법(Unity 내장 Roslyn + Bee rsp)
metadata:
  node_type: memory
  type: reference
  originSessionId: 835ff29b-d0d3-4e82-ac5e-a1983342fcc6
  modified: 2026-10-24T03:53:17.332Z
---

Unity 검사 중이거나 worktree라 Unity 재컴파일을 못 할 때 오프라인 컴파일로 오류 확인(2026-10-24 확인, 오류 0이면 Unity 컴파일도 통과).

- 컴파일러: `E:/unity/6000.5.8f1/Editor/Data/DotNetSdk/dotnet.exe exec E:/unity/6000.5.8f1/Editor/Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll -nologo @a.rsp`
- 원본 응답 파일: `E:/Git/ant/Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp` (dag 폴더명은 바뀔 수 있음). `.cs` 줄과 -out/-refout/-analyzer/-generatedfilesout을 빼고 `"Library/` → `"E:/Git/ant/Library/`로 절대경로화, -out을 Temp로, 소스는 worktree 경로로 다시 나열(새 파일은 find로 추가).
- 검사 스크립트(AgentScripts)는 Assembly-CSharp-Editor.rsp 참조 + `-r:` 위 출력 dll로 따로 컴파일. top-level 문장 파일(RunChecks.cs 등)은 제외. 옛 낡은 검사(MultiStopRoute·BiomeEvent 등)는 원래 오류 있음.
- 검사는 런타임 eval이라 internal 멤버 직접 접근 불가 → 리플렉션 사용.
- 관련: [[project_ant_teamimport_and_bridge_deadlock]]
