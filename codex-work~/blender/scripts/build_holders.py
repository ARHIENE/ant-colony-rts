"""Step after `fit_check.py -- search`: add weapon holders (straps/loops) to pk-equipment.blend."""
import bpy, sys, importlib
sys.path.insert(0, r"E:\Git\ant\codex-work~\blender\scripts")
import pk, holders
for m in (pk, holders): importlib.reload(m)
bpy.ops.wm.open_mainfile(filepath=str(pk.KIT / 'pk-equipment.blend'))
for o in [o for o in bpy.data.objects if o.name.startswith('Holder_')]: bpy.data.objects.remove(o)
hs = holders.build(bpy.data.collections['Equipment'])
bpy.ops.wm.save_mainfile()
print('HOLDERS', [o.name for o in hs])
