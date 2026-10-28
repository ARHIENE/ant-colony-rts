"""Fishing-bank prop using the existing production-kit mesh and export conventions."""
import bpy, sys, math, shutil
from pathlib import Path

root = Path(__file__).resolve().parents[3]
kit = root / 'art/production-kit-20261019'
sys.path.insert(0, str(kit / 'scripts'))
import pk

pk.new_file()
col = pk.collection('FishingBank1028')
m = pk.Mesh('Fishing_Bank')
# A low, walkable bank marker; the rod and basket leave the centre free.
for i in range(7):
    m.box((-.78 + i * .26, 0, .13), (.24, 1.25, .12), '#957047' if i % 2 else '#806043')
for x in [-.69, .69]:
    m.box((x, 0, .045), (.13, 1.15, .09), '#594635')
    for y in [-.49, .49]:
        m.cyl((x, y, .19), (x, y, .2), .018, '#454747', seg=6)
for x in [-.84, .84]:
    m.cyl((x, .44, 0), (x, .44, .43), .055, '#624b32', seg=8)
    m.torus((x, .44, .35), .059, .012, '#b8a27a', seg=8, tseg=4)
m.cyl((.67, .3, .32), (.37, 1.02, 1.35), .018, '#bfa36c', seg=8)
m.cyl((.37, 1.02, 1.35), (.37, 1.13, .3), .003, '#c4c3b4', seg=5)
m.ball((.37, 1.13, .32), (.035, .035, .065), '#b45e46', seg=8, rings=4, smooth=False)
m.lathe([(.17, .19), (.21, .24), (.23, .47), (.22, .5)], '#b09868', seg=12, c=(-.61, -.31, 0))
m.torus((-.61, -.31, .5), .22, .022, '#6e5438', seg=12, tseg=4)
for z in [.27, .35, .43]:
    m.torus((-.61, -.31, z), .195 + (z - .27) * .18, .008, '#7e6544', seg=12, tseg=3)
ob = m.to_object(col)
assert len(ob.data.vertices) > 100
assert all(math.isfinite(v) for p in ob.data.vertices for v in p.co)
bpy.context.view_layer.objects.active = ob
ob.select_set(True)
out = kit / 'exports/fishing-bank-1028'
out.mkdir(parents=True, exist_ok=True)
path = out / 'Fishing_Bank.fbx'
bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={'MESH'}, bake_anim=False, axis_forward='-Z', axis_up='Y')
shutil.copy2(path, root / 'Assets/Resources/ResourceModels/Fishing_Bank.fbx')
bpy.ops.wm.save_as_mainfile(filepath=str(kit / 'fishing-bank-1028.blend'))
print('FISHING_BANK_OK', len(ob.data.vertices), len(ob.data.polygons), flush=True)
