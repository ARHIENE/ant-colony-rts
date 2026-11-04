---
name: blender-mcp-launch
description: "Blender가 꺼져 있으면 직접 실행 가능 — F:\\Blender\\blender.exe + blender_mcp 애드온 enable, 9876 LISTEN 확인"
metadata:
  node_type: memory
  type: reference
  originSessionId: ac5529e4-2501-4e38-bffa-d3cc585e85e9
  modified: 2026-10-16T15:34:36.681Z
---

- Blender 5.2: `F:\Blender\blender.exe`. MCP 애드온은 원래 사용자 폴더에 없었음 → 2026-10-17 `uvx mcp-for-blender install-addon`으로 `%APPDATA%\Blender Foundation\Blender\5.2\scripts\addons\blender_mcp.py` 설치.
- 실행: `blender.exe <file.blend> --python-expr "import bpy,addon_utils; addon_utils.enable('blender_mcp', default_set=True, persistent=True); bpy.ops.wm.save_userpref()"` → 애드온이 서버 자동 시작, `netstat -ano | grep :9876` LISTEN 뜨면 MCP 연결 가능.
- 사용자가 "Blender 꺼져 있다"고 하면 묻지 말고 직접 켤 것.
