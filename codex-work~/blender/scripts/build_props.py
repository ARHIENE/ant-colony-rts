"""blender -b --factory-startup --python scripts/build_props.py → pk-props.blend (2026-10-24 새 기획 바닥 소품, props_g.props)."""
import bpy, sys, importlib
sys.path.insert(0, r"E:\Git\ant\codex-work~\blender\scripts")
import pk, props_g
for m in (pk, props_g): importlib.reload(m)
pk.new_file(); col = pk.collection('Props')
objs = props_g.props(col)
for i, o in enumerate(objs): o.location = (i % 6 * .4, -(i // 6) * .4, 0)
bpy.ops.wm.save_as_mainfile(filepath=str(pk.KIT / 'pk-props.blend'))
print('PROPS', len(objs))
