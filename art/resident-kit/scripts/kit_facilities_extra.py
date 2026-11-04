"""FURNITURE-97: add the new furniture to resident-facility-kit.blend + facility-points.json + exports/facilities.

Run after kit_facilities.py (which rebuilds the kit file from colony-core and would drop these):
  blender -b --python scripts/kit_facilities_extra.py
Re-runnable: previous FURNITURE-97 roots/actions are removed first. Models are authored in kit units,
so scale_applied = 1 and JSON coordinates = facility-root local = FBX Empty coordinates.
"""
import bpy, sys, json, math, importlib
from mathutils import Vector
sys.path.insert(0, r"E:\Git\ant\art\resident-kit\scripts")
sys.path.insert(0, r"E:\Git\ant\art\colony-core\scripts")
import kit_common as K, geometry as g, extra_spec as S, models_extra as M
for m in (K, S, M): importlib.reload(m)

POINT_KEYS = ('WorkPoint_0', 'SeatPoint', 'LiePoint', 'HandTarget', 'InputPoint')

bpy.ops.wm.open_mainfile(filepath=str(K.KIT / 'resident-facility-kit.blend'))
scene = bpy.context.scene; vl = scene.view_layers[0]
col = bpy.data.collections['Facilities']
for rid in S.SPEC:
    r = bpy.data.objects.get(rid)
    if r:
        for o in [*r.children_recursive, r]: bpy.data.objects.remove(o)
for a in [a for a in bpy.data.actions if a.name.split('|')[0] in S.SPEC]:
    bpy.data.actions.remove(a)
old_roots = [o for o in col.objects if o.parent is None]

g.MATS = {n: bpy.data.materials['CC_' + n] for n in g.PALETTE}
before = set(bpy.data.objects)
M.build()
for o in set(bpy.data.objects) - before:
    for c in list(o.users_collection): c.objects.unlink(o)
    col.objects.link(o)
vl.update()


def bounds(root):
    inv = root.matrix_world.inverted()
    pts = [inv @ o.matrix_world @ Vector(c) for o in root.children_recursive if o.type == 'MESH' for c in o.bound_box]
    return Vector([min(p[i] for p in pts) for i in range(3)]), Vector([max(p[i] for p in pts) for i in range(3)])


def keyframes(o, act, axis, deg, mode):
    o.rotation_mode = 'XYZ'; o.animation_data_clear(); o.animation_data_create(); o.animation_data.action = act
    base = o.rotation_euler.copy()
    keys = {'swing': [(1, 0), (13, 1), (29, 1), (41, 0)], 'spin': [(1, 0), (49, 1)],
            'rock': [(1, 0), (13, 1), (37, -1), (49, 0)]}[mode]
    for f, t in keys:
        r = base.copy(); r[axis] = base[axis] + math.radians(deg) * t; o.rotation_euler = r
        o.keyframe_insert('rotation_euler', index=axis, frame=f)
    o.rotation_euler = base
    if mode != 'swing':
        for layer in act.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for fc in bag.fcurves:
                        for kp in fc.keyframe_points: kp.interpolation = 'LINEAR' if mode == 'spin' else 'BEZIER'
                        fc.modifiers.new('CYCLES')


points = json.loads((K.KIT / 'facility-points.json').read_text(encoding='utf-8'))
for rid in S.SPEC: points.pop(rid, None)
y0 = max(o.location.y for o in old_roots) + 14
for i, (rid, (ko, kind, section, clip, tool, face, memo)) in enumerate(S.SPEC.items()):
    root = bpy.data.objects[rid]
    root.location = (0, 0, 0); vl.update()
    lo, hi = bounds(root)
    for o in root.children: o.location.z -= lo.z
    vl.update()
    for o in root.children_recursive:  # plain facility-local child transforms
        mw = o.matrix_world.copy(); o.matrix_parent_inverse.identity(); o.matrix_world = mw
    vl.update()
    pts = {}
    pivots = {p for p, *_ in S.HINGES.get(rid, [])}
    for e in root.children_recursive:
        base = e.name.split('.')[0]
        if e.type == 'EMPTY' and base in POINT_KEYS:
            e.name = rid + '__' + base; e['point'] = base
            pts[base] = {'pos': [round(v, 4) for v in e.matrix_world.translation], 'forward': [0.0, 1.0, 0.0]}
        elif base in pivots or base == S.RIDE.get(rid):
            e.name = rid + '__' + base
    hinges = []
    for pv, axis, deg, mode in S.HINGES.get(rid, []):
        act = bpy.data.actions.new(f'{rid}|' + {'swing': 'OpenClose', 'spin': 'Spin', 'rock': 'Rock'}[mode])
        act.use_fake_user = True
        keyframes(bpy.data.objects[rid + '__' + pv], act, axis, deg, mode)
        hinges.append({'pivot': rid + '__' + pv, 'axis': 'XYZ'[axis], 'open_deg': deg, 'mode': mode, 'frames': 48})
    lo, hi = bounds(root)
    work = {'clip': clip, 'tool': tool, 'stand': pts['WorkPoint_0']['pos'],
            'facing': [0, 1, 0] if face == 'in' else [0, -1, 0], 'turn': round(math.pi if face == 'in' else 0, 4)}
    if 'HandTarget' in pts:
        work['hand_target'] = pts['HandTarget']['pos']
        if tool: work['tool_contact'] = work['hand_target']
    if 'SeatPoint' in pts: work['seat'] = pts['SeatPoint']['pos']
    if 'LiePoint' in pts: work['lie'] = pts['LiePoint']['pos']
    if rid in S.RIDE: work['ride'] = rid + '__' + S.RIDE[rid]
    root['kit_scale'] = [1, 1, 1]; root['furniture97'] = True
    points[rid] = {'scale_applied': [1, 1, 1], 'bounds_min': [round(v, 3) for v in lo], 'bounds_max': [round(v, 3) for v in hi],
                   'points': pts, 'work': work, 'hinges': hinges,
                   'name_ko': ko, 'building_kind': kind, 'catalog_section': section, 'memo': memo, 'source': 'FURNITURE-97'}
    root.location = (i % 8 * 9, y0 + i // 8 * 10, 0)

bpy.ops.outliner.orphans_purge(do_recursive=True)
bpy.ops.wm.save_mainfile()
(K.KIT / 'facility-points.json').write_text(json.dumps(points, ensure_ascii=False, indent=1), encoding='utf-8')

out = K.KIT / 'exports' / 'facilities'
for rid in S.SPEC:
    root = bpy.data.objects[rid]
    keep = root.location.copy(); root.location = (0, 0, 0); vl.update()
    objs = [root, *root.children_recursive]
    for x in scene.objects: x.select_set(x in objs, view_layer=vl)
    with bpy.context.temp_override(view_layer=vl, active_object=root, selected_objects=objs):
        bpy.ops.export_scene.fbx(filepath=str(out / (rid + '.fbx')), use_selection=True, object_types={'MESH', 'EMPTY'},
                                 bake_anim=bool(points[rid]['hinges']), bake_anim_use_all_actions=False,
                                 bake_anim_use_nla_strips=False, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS')
    root.location = keep
tris = {rid: sum(len(o.data.polygons) for o in bpy.data.objects[rid].children_recursive if o.type == 'MESH') for rid in S.SPEC}
print('EXTRA', len(S.SPEC), 'TOTAL', len(points), 'faces', sum(tris.values()))
