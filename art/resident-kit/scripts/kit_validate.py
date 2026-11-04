"""Headless: blender -b --python kit_validate.py  → art/resident-kit/validation.json
Checks file separation, skinning, FBX round trips, facility points, and samples tool/face and
character/facility penetration in the preview scenes. Numbers are reported, not hidden."""
import bpy, json, math, sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

KIT = Path(r"E:\Git\ant\art\resident-kit")
R = {'separation': {}, 'skin': {}, 'fbx': {}, 'facility_points': {}, 'contacts': {}, 'errors': []}
err = R['errors'].append
EXPECTED_BONES = {'Root', 'Body', 'Head', 'Arm.L', 'Arm.R', 'Hand.L', 'Hand.R', 'Foot.L', 'Foot.R', 'Ear.L', 'Ear.R', 'Tail',
                  'Socket.Tool.R', 'Socket.Tool.L', 'Socket.Carry', 'Socket.Hat'}


def open_(name):
    bpy.ops.wm.open_mainfile(filepath=str(KIT / name))


def census():
    obs = bpy.data.objects
    return {'armatures': sum(o.type == 'ARMATURE' for o in obs), 'meshes': sum(o.type == 'MESH' for o in obs),
            'facility_objects': sum(o.name.startswith('CC_') for o in obs),
            'resident_parts': sum(o.name.startswith(('Part_', 'Combo_', 'ResidentRig')) for o in obs),
            'resident_actions': sum(a.name.startswith('Resident|') for a in bpy.data.actions)}


# ---- separation ----
open_('resident-facility-kit.blend'); c = census(); R['separation']['facility'] = c
if c['armatures'] or c['resident_parts'] or c['resident_actions']: err('facility kit contains resident data')
open_('resident-animations.blend'); c = census(); R['separation']['animations'] = c
if c['meshes'] or c['facility_objects']: err('animation file contains meshes/facilities')
open_('resident-tools.blend'); c = census(); R['separation']['tools'] = c
if c['armatures'] or c['facility_objects'] or c['resident_parts']: err('tool file contains rig/facility/character')
open_('resident-character-kit.blend'); c = census(); R['separation']['character'] = c
if c['facility_objects'] or c['resident_actions']: err('character kit contains facilities/actions')
for label in ('TEARY', 'SOBBING', 'NERVOUS', 'DEAD'):
    if 'Part_Expr_' + label not in bpy.data.objects: err('missing original expression: ' + label)

# ---- skinning (character kit) ----
bad = []
for o in bpy.data.objects:
    if o.type != 'MESH' or not o.name.startswith(('Part_', 'Combo_')): continue
    mods = [m for m in o.modifiers if m.type == 'ARMATURE']
    if len(mods) != 1 or mods[0].object != o.parent: bad.append(o.name + ':modifier'); continue
    names = {g.index: g.name for g in o.vertex_groups}
    for v in o.data.vertices:
        if abs(sum(g.weight for g in v.groups if names[g.group] in EXPECTED_BONES) - 1) > 1e-3:
            bad.append(o.name + ':weights'); break
R['skin'] = {'checked_meshes': sum(o.type == 'MESH' for o in bpy.data.objects), 'problems': bad}
if bad: err(f'{len(bad)} skinning problems')

# ---- FBX round trip: parts + every clip vs source actions ----
open_('resident-animations.blend')
src_rig = bpy.data.objects['ResidentRig']; scene = bpy.context.scene
clips = json.loads(open(r"E:\Git\ant\art\resident-anim\clips.json", encoding='utf-8').read())
clips.update(json.loads((KIT / 'clips-extra.json').read_text(encoding='utf-8')))


def hand_samples(rig, act, frames):
    rig.animation_data.action = act
    if act.slots: rig.animation_data.action_slot = act.slots[0]
    out = []
    for f in frames:
        scene.frame_set(f)
        out.append([tuple(rig.matrix_world @ rig.pose.bones[b].head) for b in ('Hand.R', 'Hand.L', 'Head', 'Root')])
    return out


src = {}
for name, info in clips.items():
    frames = [1, 1 + info['frames'] // 2, info['frames'] + 1]
    src[name] = (frames, hand_samples(src_rig, bpy.data.actions['Resident|' + name], frames))
worst = 0.0; per = {}
for name, (frames, ref) in src.items():
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(KIT / 'exports' / 'anim' / f'Resident@{name}.fbx'))
    arm = next(o for o in set(bpy.data.objects) - before if o.type == 'ARMATURE')
    if set(arm.data.bones.keys()) != EXPECTED_BONES: err(f'{name}: bone names changed in FBX')
    # Blender's importer auto-connects Hand (head == Arm tail) and then ignores its location keys.
    # The FBX data is fine (engines read plain transforms); disconnect so the comparison reads the file as-is.
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    for eb in arm.data.edit_bones: eb.use_connect = False
    bpy.ops.object.mode_set(mode='OBJECT')
    act = arm.animation_data.action if arm.animation_data else None
    if not act: err(f'{name}: no action after import'); continue
    shift = int(act.frame_range[0]) - 1  # Blender's FBX importer starts takes at frame 2 (anim_offset); data is not shifted
    got = hand_samples(arm, act, [f + shift for f in frames])
    d = max((Vector(a) - Vector(b)).length for fa, fb in zip(ref, got) for a, b in zip(fa, fb))
    per[name] = round(d, 4); worst = max(worst, d)
    for o in set(bpy.data.objects) - before: bpy.data.objects.remove(o)
R['fbx']['anim_max_bone_error'] = round(worst, 4); R['fbx']['anim_per_clip'] = per
if worst > .01: err(f'FBX animation drift {worst:.4f}')
before = set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=str(KIT / 'exports' / 'character' / 'Resident_Parts.fbx'))
new = set(bpy.data.objects) - before
arm = next(o for o in new if o.type == 'ARMATURE')
meshes = [o for o in new if o.type == 'MESH']
skinned = sum(any(m.type == 'ARMATURE' and m.object == arm for m in o.modifiers) for o in meshes)
R['fbx']['parts'] = {'bones': sorted(arm.data.bones.keys()) == sorted(EXPECTED_BONES), 'meshes': len(meshes), 'skinned': skinned}
n_parts = len(json.loads((KIT / 'parts-manifest.json').read_text(encoding='utf-8'))['parts'])
R['fbx']['parts']['expected'] = n_parts
if skinned != len(meshes) or len(meshes) != n_parts: err('parts FBX mesh/skin count mismatch')

# ---- facility FBX points vs JSON ----
pts = json.loads((KIT / 'facility-points.json').read_text(encoding='utf-8'))
worst = 0.0; hinge_ok = 0
for rid, info in pts.items():
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(KIT / 'exports' / 'facilities' / f'{rid}.fbx'))
    new = set(bpy.data.objects) - before
    root = next((o for o in new if o.name.startswith(rid) and o.parent is None), None) or next(o for o in new if o.parent is None)
    inv = root.matrix_world.inverted()
    for key, p in info['points'].items():
        e = next((o for o in new if o.name.startswith(rid + '__' + key)), None)
        if not e: err(f'{rid}: point {key} missing in FBX'); continue
        worst = max(worst, ((inv @ e.matrix_world.translation) - Vector(p['pos'])).length)
    if info['hinges']:
        hinge_ok += any(o.animation_data and o.animation_data.action for o in new)
    if any(o.type == 'ARMATURE' for o in new): err(f'{rid}: armature inside facility FBX')
    for o in new: bpy.data.objects.remove(o)
R['facility_points'] = {'facilities': len(pts), 'max_point_error': round(worst, 4),
                        'hinged_with_animation': hinge_ok, 'hinged_total': sum(bool(i['hinges']) for i in pts.values())}
if worst > .01: err(f'facility point drift {worst:.4f}')

# ---- contacts in preview scenes ----
open_('resident-preview.blend')
if not any(o.name.endswith('__Expr_DEAD') for o in bpy.data.scenes['Motion__Death'].objects):
    err('Death must use the original DEAD expression, not EMPTY')
dg = None


def bvh(objs):
    global dg
    verts, polys = [], []
    for o in objs:
        ev = o.evaluated_get(dg); m = ev.to_mesh()
        base = len(verts); verts += [o.matrix_world @ v.co for v in m.vertices]
        polys += [[base + i for i in p.vertices] for p in m.polygons]
        ev.to_mesh_clear()
    return BVHTree.FromPolygons(verts, polys) if polys else None


def inside_depth(tree, points, limit=.05):
    n = 0
    for p in points:
        loc, normal, _, dist = tree.find_nearest(p)
        if loc is not None and (p - loc).dot(normal) < 0 and dist > limit: n += 1
    return n


for sc in [s for s in bpy.data.scenes if s.name.startswith(('Preview__', 'Swap__'))]:
    with bpy.context.temp_override(scene=sc, view_layer=sc.view_layers[0]):
        dg = bpy.context.evaluated_depsgraph_get()  # module-level loop: bvh() reads this
    rig = next(o for o in sc.objects if o.type == 'ARMATURE')
    body = [o for o in rig.children if o.type == 'MESH' and o.name.endswith('Body_01')]
    tools = [o for o in sc.objects if o.type == 'MESH' and o.name.startswith(('Tool_', 'Prop_'))]
    fac = [o for o in sc.objects if o.type == 'MESH' and o.library and o not in rig.children]
    span = sc.frame_end - sc.frame_start
    rows = []
    for k in range(4):
        sc.frame_set(sc.frame_start + round(k * span / 4)); dg.update()
        bt = bvh(body); row = {}
        if tools and bt:
            tv = [t.matrix_world @ v.co for t in tools for v in t.data.vertices]
            row['tool_in_body'] = inside_depth(bt, tv)
            sock = rig.matrix_world @ rig.pose.bones['Socket.Tool.R'].head
            hand = rig.matrix_world @ rig.pose.bones['Hand.R'].head
            row['grip_to_hand'] = round((sock - hand).length, 3)
        # Seated/lying/inside-by-design clips sit on or in the facility (stool, tub, cage, wheel).
        if fac and sc.get('clip') not in ('Carry', 'Sleep', 'Sit', 'Rest', 'PlayBoard', 'Bathe', 'Swing', 'Spin', 'Captive', 'Run'):
            ft = bvh(fac)
            if ft and body:
                ev = body[0].evaluated_get(dg); m = ev.to_mesh()
                bv = [body[0].matrix_world @ v.co for v in list(m.vertices)[::6]]
                m_count = bv
                ev.to_mesh_clear()
                # Only points over the facility footprint: open faces (floor plates, grass blades) give false
                # "inside" hits for points beyond their edges.
                fp = [o.matrix_world @ Vector(c) for o in fac for c in o.bound_box]
                lo = Vector([min(q[i] for q in fp) for i in range(3)]); hi = Vector([max(q[i] for q in fp) for i in range(3)])
                bv = [p for p in bv if lo.x < p.x < hi.x and lo.y < p.y < hi.y and p.z < hi.z]
                row['body_in_facility_pct'] = round(100 * inside_depth(ft, bv) / max(1, len(m_count)), 1) if bv else 0.0
        rows.append(row)
    R['contacts'][sc.name] = {'clip': sc.get('clip'), 'samples': rows}

flag = {n: v for n, v in R['contacts'].items()
        if any(r.get('tool_in_body', 0) > 20 or r.get('body_in_facility_pct', 0) > 3 for r in v['samples'])}
R['contacts_flagged'] = sorted(flag)
R['scope'] = ('Blender-side checks only. Unity import/Animator playback NOT tested. Penetration = sampled 4 frames/scene, '
              'tool vertices >5cm inside the body mesh, or body vertices >5cm inside facility meshes.')
(KIT / 'validation.json').write_text(json.dumps(R, indent=1, ensure_ascii=False), encoding='utf-8')
print('VALIDATION', json.dumps({'errors': R['errors'], 'flagged': R['contacts_flagged'], 'fbx': {k: v for k, v in R['fbx'].items() if k != 'anim_per_clip'},
                                 'points': R['facility_points'], 'skin': len(R['skin']['problems'])}))
