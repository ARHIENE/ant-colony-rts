"""Individual PNGs for every library asset → previews/png/<kind>/<name>.png (256 px, 3/4 view, transparent).

blender -b --factory-startup --python scripts/render_items.py -- <resources|crops|facilities|handheld|equipment>
Crops render one strip per crop (S1–S5 side by side). Facilities hide their point/select Empties (not renderable anyway).
"""
import bpy, sys, importlib
sys.path.insert(0, r"E:\Git\ant\codex-work~\blender\scripts")
import pk, render_common as RC
for m in (pk, RC): importlib.reload(m)
from mathutils import Vector

kind = sys.argv[sys.argv.index('--') + 1]
ONLY = set(sys.argv[sys.argv.index('--') + 2:])  # 선택: 이 id만 렌더
FILES = {'resources': 'pk-resources.blend', 'crops': 'pk-crops.blend', 'facilities': 'pk-facilities.blend',
         'handheld': 'pk-handheld.blend', 'equipment': 'pk-equipment.blend', 'props': 'pk-props.blend'}
bpy.ops.wm.open_mainfile(filepath=str(pk.KIT / FILES[kind]))
cam = RC.setup(600, res_y=240) if kind == 'crops' else RC.setup(256)
out = pk.KIT / 'previews' / 'png' / kind; out.mkdir(parents=True, exist_ok=True)
for o in bpy.data.objects: o.hide_viewport = False
HIDDEN = {o.name for o in bpy.data.objects if o.hide_render}  # authored state; RC.only() overwrites hide_render per shot
done = 0


def render(objs, name):
    global done
    RC.only(objs); RC.fit(cam, objs, view=RC.Vector((.12, -1, .32)).normalized(), wide=1.05) if kind == 'crops' else RC.fit(cam, objs); RC.shot(out / f'{name}.png')
    if kind == 'facilities': RC.shot(out / f'{name}.png')  # 2nd pass: EEVEE can return an empty frame while new materials compile
    done += 1


if kind == 'resources':
    for o in bpy.data.objects:
        if o.type == 'MESH' and o.get('id'): render([o], o.name)
elif kind == 'crops':
    by = {}
    for o in bpy.data.objects:
        if o.type == 'MESH' and o.get('id'): by.setdefault(o['id'], []).append(o)
    for cid, objs in by.items():
        objs.sort(key=lambda o: o['stage'])
        base = objs[0].location.copy()
        for i, o in enumerate(objs): o.location = base + Vector((i * (.95 if objs[0]['form'] == 'tree' else .4), 0, 0))
        bpy.context.view_layer.update(); render(objs, f'Crop_{cid}')
elif kind == 'facilities':
    for r in [o for o in bpy.data.objects if o.parent is None and o.get('id') and (not ONLY or o['id'] in ONLY)]:
        render([r] + [c for c in r.children_recursive if c.type == 'MESH' and c.name not in HIDDEN
                      and not (c.name.split('_')[-1] in ('Empty', 'Full'))], r['id'])
elif kind in ('handheld', 'props'):
    for o in bpy.data.collections['Handheld' if kind == 'handheld' else 'Props'].objects:
        if o.type == 'MESH' and (not ONLY or o.name in ONLY): render([o], o.name)
elif kind == 'equipment':
    for o in bpy.data.collections['Equipment'].objects:
        if o.type == 'MESH' and not o.name.endswith('_ARMOR'): render([o], o.name)
print('RENDERED', kind, done)
