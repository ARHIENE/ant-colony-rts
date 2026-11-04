"""blender -b --factory-startup --python scripts/build_handheld.py → pk-handheld.blend (tools + weapons library)."""
import bpy, sys, importlib
sys.path.insert(0, r"E:\Git\ant\codex-work~\blender\scripts")
import pk, equip_spec, handheld, props_g
for m in (pk, equip_spec, handheld, props_g): importlib.reload(m)
s = pk.new_file(); col = pk.collection('Handheld')
objs = handheld.build(col)
# grid: rows = kind, columns = tier
kinds = []
for o in objs:
    k = o['kind']
    if k not in kinds: kinds.append(k)
for o in objs:
    r = kinds.index(o['kind']); c = [t[0] for t in equip_spec.TIERS].index(o['tier'])
    o.location = (c * .3, 0, -r * .55)
    o.rotation_euler = (0, 0, 0)
for i, o in enumerate(props_g.handheld_extra(col)):  # 2026-10-24 단궁·화살·보스 보상 장비: 맨 아래 한 줄
    o.location = (i * .3, 0, -len(kinds) * .55); o.rotation_euler = (0, 0, 0)
bpy.ops.wm.save_as_mainfile(filepath=str(pk.KIT / 'pk-handheld.blend'))
print('HANDHELD', len(objs))
