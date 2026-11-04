"""FBX exports (same conventions as art/kemono-kit/scripts/export.py: axis -Z forward / Y up, no leaf bones).

blender -b --factory-startup --python scripts/export_fbx.py -- <animations|handheld|equipment|resources|crops|facilities>
→ exports/<kind>/... + exports/<kind>/export-list.json (what each file holds, for reimport_check.py)
animations: armature only, one file per PK clip (Kemono@PK_<clip>.fbx), constraints already baked by build_motions.
facilities: root + parts + point/select Empties; the parts' primary actions play together in one take.
"""
import bpy, sys, json, importlib
sys.path.insert(0, r"E:\Git\ant\codex-work~\blender\scripts")
import pk
importlib.reload(pk)

kind = sys.argv[sys.argv.index('--') + 1]
SRC = {'animations': 'pk-motions.blend', 'handheld': 'pk-handheld.blend', 'equipment': 'pk-equipment.blend', 'resources': 'pk-resources.blend',
       'crops': 'pk-crops.blend', 'facilities': 'pk-facilities.blend', 'props': 'pk-props.blend'}
bpy.ops.wm.open_mainfile(filepath=str(pk.KIT / SRC[kind]))
out = pk.KIT / 'exports' / kind; out.mkdir(parents=True, exist_ok=True)
COMMON = dict(axis_forward='-Z', axis_up='Y', add_leaf_bones=False, use_selection=True, apply_scale_options='FBX_SCALE_ALL')
vl = bpy.context.view_layer; listing = {}


def select(objs):
    for o in vl.objects: o.select_set(False)
    for o in objs:
        o.hide_set(False); o.hide_viewport = False; o.select_set(True)
    vl.objects.active = objs[0]


def info(objs):
    return {o.name: {'type': o.type, 'dims': [round(x, 4) for x in o.dimensions], 'mats': len(o.material_slots) if o.type == 'MESH' else 0}
            for o in objs}


if kind == 'animations':
    rig = bpy.data.objects['KemonoRig']; rig.data.pose_position = 'POSE'; sc = bpy.context.scene
    for act in [a for a in bpy.data.actions if a.name.startswith('PK|')]:
        name = act.name.split('|', 1)[1]
        rig.animation_data.action = act; rig.animation_data.action_slot = act.slots[0]
        sc.frame_start, sc.frame_end = 1, int(act.get('duration_frames', act.frame_range[1]))
        select([rig]); f = out / f'Kemono@PK_{name}.fbx'
        bpy.ops.export_scene.fbx(filepath=str(f), object_types={'ARMATURE'}, bake_anim=True, bake_anim_use_all_actions=False,
                                 bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0, **COMMON)
        listing[f.name] = {'clip': name, 'frames': [sc.frame_start, sc.frame_end], 'events': pk.to_py(act.get('events', {})), 'loop': bool(act.get('loop'))}
elif kind in ('handheld', 'equipment', 'props'):
    col = bpy.data.collections[{'handheld': 'Handheld', 'equipment': 'Equipment', 'props': 'Props'}[kind]]
    for o in [o for o in col.objects if o.type == 'MESH']:
        select([o]); f = out / f'{o.name}.fbx'
        bpy.ops.export_scene.fbx(filepath=str(f), object_types={'MESH'}, bake_anim=False, **COMMON)
        listing[f.name] = {'objects': info([o]), 'props': {k: pk.to_py(o[k]) for k in o.keys() if not k.startswith('_')}}
elif kind == 'resources':
    by = {}
    for o in bpy.data.objects:
        if o.type == 'MESH' and o.get('id'): by.setdefault(o['id'], []).append(o)
    for iid, objs in by.items():
        saved = [o.location.copy() for o in objs]
        for o in objs: o.location = (0, 0, 0)  # each variant at the origin (bottom centre) in its own file slot
        select(objs); f = out / f'{iid}.fbx'
        bpy.ops.export_scene.fbx(filepath=str(f), object_types={'MESH'}, bake_anim=False, **COMMON)
        for o, l in zip(objs, saved): o.location = l
        listing[f.name] = {'objects': info(objs)}
elif kind == 'crops':
    by = {}
    for o in bpy.data.objects:
        if o.type == 'MESH' and o.get('id'): by.setdefault(o['id'], []).append(o)
    for cid, objs in by.items():
        for o in objs: o.location = (0, 0, 0)
        select(objs); f = out / f'Crop_{cid}.fbx'
        bpy.ops.export_scene.fbx(filepath=str(f), object_types={'MESH'}, bake_anim=False, **COMMON)
        listing[f.name] = {'objects': info(objs)}
elif kind == 'facilities':
    sc = bpy.context.scene
    for r in [o for o in bpy.data.objects if o.parent is None and o.get('id')]:
        r.location = (0, 0, 0); objs = [r] + list(r.children_recursive); names = {o.name for o in objs}
        select(objs); f = out / f"{r['id']}.fbx"
        bpy.ops.export_scene.fbx(filepath=str(f), object_types={'MESH', 'EMPTY'}, bake_anim=False, **COMMON)
        takes = {}
        for a in bpy.data.actions:
            if a.get('part') in names: takes.setdefault(a.name.split('|', 1)[1], []).append(a)
        entry = {'objects': info(objs), 'points': pk.to_py(r.get('points', {})), 'select_box': pk.to_py(r.get('select_box')), 'takes': {}}
        for take, acts in takes.items():  # one file per take: every part keyed by that take plays together
            for a in acts:
                o = bpy.data.objects[a['part']]; o.animation_data_create(); o.animation_data.action = a
                if a.slots: o.animation_data.action_slot = a.slots[0]
            sc.frame_start, sc.frame_end = 1, int(max(a.frame_range[1] for a in acts))
            select(objs); ft = out / f"{r['id']}@{take}.fbx"
            bpy.ops.export_scene.fbx(filepath=str(ft), object_types={'MESH', 'EMPTY'}, bake_anim=True, bake_anim_use_all_actions=False,
                                     bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0, **COMMON)
            entry['takes'][ft.name] = {'parts': [a['part'] for a in acts], 'frames': [1, sc.frame_end]}
            for a in acts: bpy.data.objects[a['part']].animation_data.action = None
        listing[f.name] = entry
pk.write_json(out / 'export-list.json', listing)
print('EXPORTED', kind, len(listing))
