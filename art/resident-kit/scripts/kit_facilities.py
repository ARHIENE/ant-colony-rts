"""Build resident-facility-kit.blend + facility-points.json + exports/facilities/*.fbx.

Facilities only: no resident mesh, rig or action. Hinge (door/lid/gate/turret) actions are kept.
Scale is APPLIED (root scale = 1), so the exported model and the JSON share one coordinate system:
facility-root local space, kit units (resident native size, ~2.2 tall), front of facility = -Y.
"""
import bpy, sys, json, math, importlib
from mathutils import Vector, Matrix
sys.path.insert(0, r"E:\Git\ant\art\resident-kit\scripts")
import kit_common as K
importlib.reload(K)

SPEC = json.loads(K.POINTS_JSON.read_text(encoding='utf-8'))
TABLES = {'CC_Workshop', 'CC_Kitchen', 'CC_Apothecary', 'CC_ButcherTable', 'CC_ResearchDesk', 'CC_AdminDesk', 'CC_DiningTable', 'CC_Washbasin'}
BEDS = {'CC_CommanderBed', 'CC_AnimalTreatment'}
SEATS = {'CC_Chair', 'CC_RestBench', 'CC_Toilet'}
# Clip overrides kept from resident-anim/facilities.py (more specific kitchen/desk motions).
CLIP = {'CC_Kitchen': 'Stir', 'CC_Apothecary': 'Grind', 'CC_ButcherTable': 'Saw', 'CC_AdminDesk': 'Stamp', 'CC_Washbasin': 'Wash'}
DEFAULT = 1.5          # all other facilities, uniform (clickable, character-sized)
TABLE_TOP = .80        # work surface height = hands of work clips (paw .65 + clip lift .20)
BED_TOP = .62
SEAT_TOP = .42
CARRY_SPAN = .70       # carried items: longest side
STAND_BACK = .62
BODY_FRONT = .55
STAND_EXTRA = {'CC_Refrigerator': .3}  # Pickup bends forward into the tall cabinet       # other facilities: stand at least this far in front of the front edge
WALK_IN = {'CC_TreeTall', 'CC_TreeBroadleaf', 'CC_Shower', 'CC_Watchtower', 'CC_NestEntrance', 'CC_Dormitory',
           'CC_CastleGate', 'CC_DoorFrame', 'CC_RanchGate', 'CC_ConstructionPlan', 'CC_FarmTile', 'CC_StickyTrap'}       # table clips: stand this far in front of the front edge (hands land ~.22 inside)

scene = K.empty_file()
scene.name = 'Resident_Facility_Kit'
col = K.collection('Facilities')
with bpy.data.libraries.load(K.SRC_FACILITIES) as (a, b):
    b.objects = list(a.objects)
    b.actions = list(a.actions)
roots = [o for o in b.objects if o and o.name in SPEC]
keep = set()
for r in roots:
    keep |= {r, *r.children_recursive}
for o in b.objects:
    if o in keep: col.objects.link(o)
    elif o: bpy.data.objects.remove(o)
for act in b.actions:
    if act: act.name = act.name.rsplit('.', 1)[0] if act.name[-4:-3] == '.' else act.name
vl = scene.view_layers[0]
vl.update()


def local_bounds(root, pred=lambda o: True):
    inv = root.matrix_world.inverted()
    pts = [inv @ o.matrix_world @ Vector(c) for o in root.children_recursive if o.type == 'MESH' and pred(o) for c in o.bound_box]
    return Vector([min(p[i] for p in pts) for i in range(3)]), Vector([max(p[i] for p in pts) for i in range(3)])


def top_surface(root):
    """Height of the largest horizontal-ish mesh (tabletop, mattress, seat)."""
    inv = root.matrix_world.inverted(); best = None
    for o in root.children_recursive:
        if o.type != 'MESH': continue
        pts = [inv @ o.matrix_world @ Vector(c) for c in o.bound_box]
        dx = max(p.x for p in pts) - min(p.x for p in pts); dy = max(p.y for p in pts) - min(p.y for p in pts); dz = max(p.z for p in pts) - min(p.z for p in pts)
        if dz < .25 * max(dx, dy) and (best is None or dx * dy > best[0]):
            best = (dx * dy, max(p.z for p in pts))
    return best[1] if best else local_bounds(root)[1].z


def scale_for(rid, info, root):
    lo, hi = local_bounds(root)
    if info['clip'] == 'Carry':
        return Vector((CARRY_SPAN / max(hi - lo),) * 3)
    for group, target, xy in ((TABLES, TABLE_TOP, 1.5), (BEDS, BED_TOP, 1.8), (SEATS, SEAT_TOP, 2.4)):
        if rid in group:
            return Vector((xy, xy, target / top_surface(root)))
    if rid == 'CC_SleepingMat':
        return Vector((2.4, 2.4, 1.2))
    return Vector((DEFAULT,) * 3)


def apply_scale(root, s):
    root.scale = s
    vl.update()
    objs = [root, *root.children_recursive]
    with bpy.context.temp_override(view_layer=vl, active_object=root, selected_editable_objects=objs, selected_objects=objs):
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    vl.update()
    # Clean parent-inverse (identity) so child transforms are plain facility-local values.
    for o in objs[1:]:
        mw = o.matrix_world.copy()
        o.matrix_parent_inverse.identity()
        o.matrix_world = mw
        vl.update()


points_out = {}
for rid, info in SPEC.items():
    root = bpy.data.objects[rid]
    s = scale_for(rid, info, root)
    apply_scale(root, s)
    vl.update()
    inv = root.matrix_world.inverted()
    pts = {}
    for e in root.children_recursive:
        if e.type != 'EMPTY' or e.animation_data: continue
        key = e.name.split('.')[0]
        if key not in info['points']: continue
        e.name = rid + '__' + key; e['point'] = key
        fwd = Vector(info['points'][key]['forward'])
        fwd = Vector((fwd.x / s.x, fwd.y / s.y, fwd.z / s.z)).normalized() if fwd.length else fwd
        pts[key] = {'pos': [round(v, 4) for v in inv @ e.matrix_world.translation], 'forward': [round(v, 4) for v in fwd]}
    lo, hi = local_bounds(root)
    clip = CLIP.get(rid, info['clip'])
    work = {'clip': clip, 'tool': info['tool']}
    if clip and clip != 'Carry':
        wp = Vector(pts.get('WorkPoint_0', {'pos': [0, lo.y - .8, 0]})['pos'])
        if rid in TABLES or rid in BEDS:
            top = top_surface(root)
            cx = (lo.x + hi.x) / 2
            work['stand'] = [round(cx, 4), round(lo.y - STAND_BACK, 4), 0]
            work['hand_target'] = [round(cx, 4), round(lo.y + .22, 4), round(top, 4)]
        else:
            # Keep the round body (front at -0.59) out of solid facilities; trees/doors/shower keep their spot.
            y = wp.y if rid in WALK_IN else min(wp.y, lo.y - BODY_FRONT - STAND_EXTRA.get(rid, 0))
            work['stand'] = [round(wp.x, 4), round(y, 4), 0]
        if rid in SEATS or rid in BEDS or clip == 'Sleep':
            sp = pts.get('SeatPoint') or pts.get('PatientPoint') or {'pos': [0, 0, top_surface(root)]}
            work['seat' if clip != 'Sleep' else 'lie'] = sp['pos']
        if info['tool']:
            work['tool_contact'] = work.get('hand_target')
        work['facing'] = [0, 1, 0]   # character turns to face the facility (+Y in facility space)
    root['kit_scale'] = list(s)
    points_out[rid] = {'scale_applied': [round(v, 4) for v in s], 'bounds_min': [round(v, 3) for v in lo],
                       'bounds_max': [round(v, 3) for v in hi], 'points': pts, 'work': work, 'hinges': info['hinges']}

for o in [o for o in bpy.data.objects if not o.users_collection]:
    bpy.data.objects.remove(o)
bpy.ops.outliner.orphans_purge(do_recursive=True)
bpy.ops.wm.save_as_mainfile(filepath=str(K.KIT / 'resident-facility-kit.blend'))
(K.KIT / 'facility-points.json').write_text(json.dumps(points_out, ensure_ascii=False, indent=1), encoding='utf-8')

out = K.KIT / 'exports' / 'facilities'
out.mkdir(parents=True, exist_ok=True)
for rid in SPEC:
    root = bpy.data.objects[rid]
    keep_loc = root.location.copy(); root.location = (0, 0, 0)
    objs = [root, *root.children_recursive]
    for x in scene.objects: x.select_set(x in objs, view_layer=vl)
    with bpy.context.temp_override(view_layer=vl, active_object=root, selected_objects=objs):
        bpy.ops.export_scene.fbx(filepath=str(out / (rid + '.fbx')), use_selection=True, object_types={'MESH', 'EMPTY'},
                                 bake_anim=bool(points_out[rid]['hinges']), bake_anim_use_all_actions=False,
                                 bake_anim_use_nla_strips=False, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS')
    root.location = keep_loc
print('facilities', len(points_out))
for rid in ['CC_Workshop', 'CC_Kitchen', 'CC_DiningTable', 'CC_CommanderBed', 'CC_Chair', 'CC_SleepingMat', 'CC_ResourceWood', 'CC_TreeTall']:
    print(rid, points_out[rid]['scale_applied'], points_out[rid]['bounds_max'], points_out[rid]['work'])
