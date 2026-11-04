"""Re-import every exported FBX into an empty scene and compare with what was exported (BLENDER-1019 §8).

blender -b --factory-startup --python scripts/reimport_check.py -- <kind> [...]  → exports/<kind>/reimport-report.json
Checks: object count/names, mesh dimensions (≤1 %), no empty material slot, facility point Empties present,
animation frame range, and for PK clips the right/left palm socket world position at the event frames (≤1 mm)
against pk-motions.blend.
"""
import bpy, sys, json, importlib
sys.path.insert(0, r"E:\Git\ant\art\production-kit-20261019\scripts")
import pk
importlib.reload(pk)
from mathutils import Vector

kinds = sys.argv[sys.argv.index('--') + 1:]
BONES = ('hand.R', 'hand.L', 'foot.R', 'head')


def bone_ref():
    """Source bone head positions (world) at each clip's key frames, from pk-motions.blend."""
    bpy.ops.wm.open_mainfile(filepath=str(pk.KIT / 'pk-motions.blend'))
    rig = bpy.data.objects['KemonoRig']; rig.data.pose_position = 'POSE'; sc = bpy.context.scene; ref = {}
    for act in [a for a in bpy.data.actions if a.name.startswith('PK|')]:
        rig.animation_data.action = act; rig.animation_data.action_slot = act.slots[0]
        n = int(act.get('duration_frames', act.frame_range[1])); fr = sorted({1, n // 2, n, *[int(v) for v in pk.to_py(act.get('events', {})).values()]})
        ref[act.name.split('|', 1)[1]] = {f: _heads(rig, sc, f) for f in fr if 1 <= f <= n}
    return ref


def _heads(rig, sc, f):
    sc.frame_set(int(round(f))); bpy.context.view_layer.update()
    return {b: list(rig.matrix_world @ rig.pose.bones[b].head) for b in BONES if b in rig.pose.bones}


for kind in kinds:
    d = pk.KIT / 'exports' / kind; listing = json.loads((d / 'export-list.json').read_text(encoding='utf-8'))
    ref = bone_ref() if kind == 'animations' else None
    report = {'checked': 0, 'failed': {}, 'notes': []}
    for fname, spec in listing.items():
        files = [fname] + (list(spec.get('takes', {}).keys()))
        for fn in files:
            pk.new_file(); bpy.ops.import_scene.fbx(filepath=str(d / fn)); report['checked'] += 1
            got = {o.name.split('.')[0]: o for o in bpy.data.objects}
            errs = []
            if kind == 'animations':
                arm = next((o for o in bpy.data.objects if o.type == 'ARMATURE'), None)
                act = arm and arm.animation_data and arm.animation_data.action
                if not act: errs.append('no action')
                else:
                    sc = bpy.context.scene; fr = act.frame_range
                    if abs((fr[1] - fr[0]) - (spec['frames'][1] - spec['frames'][0])) > 1: errs.append(f'frame range {tuple(fr)} vs {spec["frames"]}')
                    off = fr[0] - 1  # importer may shift the first key to its own start
                    for f, heads in ref[spec['clip']].items():
                        h = _heads(arm, sc, f + off)
                        for b, p in heads.items():
                            e = (Vector(h[b]) - Vector(p)).length
                            if e > .001: errs.append(f'{b}@{f} {e * 1000:.1f} mm')
            else:
                want = spec['objects']
                for name, w in want.items():
                    o = got.get(name.split('.')[0])
                    if o is None: errs.append(f'missing {name}'); continue
                    if w['type'] == 'MESH':
                        dw, dg = Vector(w['dims']), Vector(o.dimensions)
                        if (dw - dg).length > max(.001, .01 * dw.length): errs.append(f'{name} dims {list(dg)} vs {list(dw)}')
                        if any(s.material is None for s in o.material_slots): errs.append(f'{name} empty material slot')
                if fn in spec.get('takes', {}):
                    animated = [o for o in bpy.data.objects if o.animation_data and o.animation_data.action]
                    if not animated: errs.append('take has no animated object')
            if errs: report['failed'][fn] = errs[:8]
    pk.write_json(d / 'reimport-report.json', report)
    print('REIMPORT', kind, report['checked'], 'failed', len(report['failed']))
