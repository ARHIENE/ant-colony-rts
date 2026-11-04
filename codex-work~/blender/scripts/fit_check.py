"""Penetration check + carry placement search (BLENDER-1019 §3/§8).

blender -b --factory-startup --python scripts/fit_check.py -- [search]
Rigid items follow one bone (pose matrix × rest-relative verts), deforming parts are evaluated per frame.
Overlap = BVH triangle pairs between the item and a part. Counts are evidence, not proof: 0 pairs at sampled
frames does not rule out contact between samples, and touching surfaces count as overlap.
Writes previews/fit-report.json (+ previews/carry-placement.json with 'search').
"""
import bpy, sys, json, math, time
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
sys.path.insert(0, r"E:\Git\ant\codex-work~\blender\scripts")
import pk

ARGS = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
# 'motions' = final pass on pk-motions.blend: the existing kemono clips AND the new PK| clips (after build_motions)
MOTIONS = 'motions' in ARGS
bpy.ops.wm.open_mainfile(filepath=str(pk.KIT / ('pk-motions.blend' if MOTIONS else 'pk-equipment.blend')))
rig = bpy.data.objects['KemonoRig']
rig.data.pose_position = 'POSE'  # kit files are saved in REST; without this every frame is the rest pose
ACTS = {a.name.split('|', 1)[1]: a for a in bpy.data.actions if a.name.startswith(('Kemono|', 'PK|') if MOTIONS else 'Kemono|')}
PARTS = [o for o in bpy.data.objects if o.type == 'MESH' and o.name.startswith(('Part_Body', 'Part_Outfit', 'Part_Hair', 'Part_Ears', 'Part_Tail'))]
for o in PARTS: o.hide_viewport = False; o.hide_render = False
for o in bpy.context.view_layer.objects: o.hide_set(False)
REST = {b.name: rig.matrix_world @ b.matrix_local for b in rig.data.bones}
STEP = 3


def cat(part):  # Part_Ears_RABBIT_00 → ('Ears', 'RABBIT')
    s = part.name.split('_'); return s[1], s[2]


class Rigid:
    def __init__(self, name, obj_or_mesh, bone, world=None):
        self.name, self.bone = name, bone
        me = obj_or_mesh.data if hasattr(obj_or_mesh, 'data') else obj_or_mesh
        W = world if world is not None else obj_or_mesh.matrix_world
        inv = REST[bone].inverted()
        self.local = [inv @ (W @ v.co) for v in me.vertices]
        self.polys = [tuple(p.vertices) for p in me.polygons]

    def tree(self, pose):
        M = rig.matrix_world @ pose[self.bone]
        return BVHTree.FromPolygons([M @ v for v in self.local], self.polys)


def pose_mats():
    return {pb.name: pb.matrix.copy() for pb in rig.pose.bones}


def part_trees():
    dg = bpy.context.evaluated_depsgraph_get(); out = {}
    for o in PARTS:
        ev = o.evaluated_get(dg); me = ev.to_mesh(); W = ev.matrix_world
        out[o.name] = BVHTree.FromPolygons([W @ v.co for v in me.vertices], [tuple(p.vertices) for p in me.polygons])
        ev.to_mesh_clear()
    return out


def frames(act):
    n = int(act.get('duration_frames', act.frame_range[1]))
    return list(range(1, n + 1, STEP))


def play(clips):
    """Yield (clip, frame, pose matrices, part trees) for every sampled frame."""
    rig.animation_data_create()
    for name in clips:
        act = ACTS[name]; rig.animation_data.action = act
        if act.slots: rig.animation_data.action_slot = act.slots[0]
        for f in frames(act):
            bpy.context.scene.frame_set(f)
            yield name, f, pose_mats(), part_trees()


# ---------------------------------------------------------------- what to test against what
VARIANTS = {cat(o)[1] for o in PARTS if o.name.endswith('_ARMOR')}  # hair styles / outfits with an armour-wearing variant


def targets(item_name, ears=None):
    out = []
    for o in PARTS:
        c, v = cat(o)
        if c == 'Ears' and ears and v != ears: continue
        if item_name.startswith(('Helm_', 'HelmTrim')) and c in ('Tail',): continue
        # with body armour the hair style is swapped for its _ARMOR variant, so the original is not worn with it
        if c in ('Hair', 'Outfit', 'Body') and v in VARIANTS and not item_name.startswith(('Helm_', 'HelmTrim', 'Carry_')) and not o.name.endswith('_ARMOR'): continue
        out.append(o.name)
    return out


def worn_items():
    items = []
    for o in bpy.data.collections['Equipment'].objects:
        if o.get('soft') or not o.get('bone'): continue
        if '_Trim_' in o.name: continue  # trims lie on their plate; the plate is tested
        items.append((Rigid(o.name, o, o['bone']), o.get('ears')))
    return items


# ---------------------------------------------------------------- carried weapons
def frame_for(d_down, outward_x, flat_axis=None):
    """Rotation whose local +Y points along -d (tip→mouth) for a hanging scabbard."""
    y = (-d_down).normalized(); x = Vector((outward_x, 0, 0)) if flat_axis is None else flat_axis
    x = (x - y * x.dot(y)).normalized(); z = x.cross(y)
    return Matrix((x, y, z)).transposed().to_4x4()


def scabbard_world(mouth, a_back, b_out):
    d = Vector((-math.sin(math.radians(b_out)), math.sin(math.radians(a_back)), -math.cos(math.radians(a_back)))).normalized()
    Rm = frame_for(d, -1)
    tip = mouth + d * .30
    sc = Matrix.Translation(tip) @ Rm
    sw = Matrix.Translation(mouth - d * .035) @ frame_for(-d, -1)  # sword +Y toward its own tip = d
    return sc, sw


def combined(a_obj, a_W, b_obj, b_W):
    me = bpy.data.meshes.new('cmb'); vs = []; ps = []
    for o, W in ((a_obj, a_W), (b_obj, b_W)):
        off = len(vs); vs += [W @ v.co for v in o.data.vertices]; ps += [[i + off for i in p.vertices] for p in o.data.polygons]
    me.from_pydata(vs, [], ps); return me


def carry_candidates():
    E = bpy.data.objects; out = []
    hip = REST['Socket.Hip.R'].translation
    for dx in (0, -.02, -.04):
        for dy in (-.02, 0, .02, .04):
            for a in (15, 30, 45, 60):
                for b in (0, 10, 20):
                    mouth = hip + Vector((dx, dy, 0))
                    sc, sw = scabbard_world(mouth, a, b)
                    out.append(('Sword', dict(dx=dx, dy=dy, back=a, out=b), 'pevis', combined(E['Scabbard_Steel'], sc, E['Weapon_Sword_Steel'], sw), [sc, sw]))
    sp = E['Weapon_Spear_Steel']
    for parent, base in (('spine_02', Vector((0, .16, .56))), ('pevis', Vector((-.21, .10, .52)))):
        for dy in (0, .03, .06):
            for dz in (-.04, 0, .04):
                for th in ((40, 55, 70) if parent == 'spine_02' else (5, 15, 25)):
                    for lean in (0, 10, 20):
                        t = math.radians(th); l = math.radians(lean)
                        up = Vector((-math.sin(t), math.sin(l), math.cos(t))).normalized()  # spear +Y (head) direction
                        side = Vector((0, 1, 0)) if parent == 'spine_02' else Vector((1, 0, 0))
                        Rm = frame_for(-up, 1, side)
                        centre = base + Vector((0, dy, dz))
                        W = Matrix.Translation(centre - up * .02) @ Rm
                        me = bpy.data.meshes.new('sp'); me.from_pydata([W @ v.co for v in sp.data.vertices], [], [list(p.vertices) for p in sp.data.polygons])
                        out.append(('Spear', dict(parent=parent, dy=dy, dz=dz, angle=th, lean=lean), parent, me, [W]))
    sh = E['Weapon_Shield_Steel']
    for parent, base, face in (('spine_02', Vector((0, .17, .55)), Vector((0, 1, 0))), ('pevis', Vector((.24, .02, .40)), Vector((1, 0, 0))),
                               ('spine_02', Vector((0, .2, .66)), Vector((0, 1, 0)))):
        for dy in (0, .03, .06):
            for dz in (-.05, 0, .05):
                for tilt in (0, 15, 30):
                    n = face.normalized()  # shield front (local −Y) faces away from the body
                    t = math.radians(tilt); up = Vector((0, 0, 1)) * math.cos(t) + n * math.sin(t)
                    y = -n; x = up.cross(y).normalized(); z = x.cross(y)
                    Rm = Matrix((x, y, z)).transposed().to_4x4()
                    W = Matrix.Translation(base + n * dy + Vector((0, 0, dz))) @ Rm @ SHIELD_INV  # placement authored in the old (face −Y) frame
                    me = bpy.data.meshes.new('sh'); me.from_pydata([W @ v.co for v in sh.data.vertices], [], [list(p.vertices) for p in sh.data.polygons])
                    out.append(('Shield', dict(parent=parent, base=list(base), dy=dy, dz=dz, tilt=tilt), parent, me, [W]))
    return out


SHIELD_INV = Matrix(((0, 1, 0, 0), (0, 0, 1, 0), (1, 0, 0, 0), (0, 0, 0, 1)))  # inverse of handheld.SHIELD_ROT


def search():
    clips = [c for c in ('Walk', 'Run', 'Idle', 'StandIdle', 'Attack', 'Chop', 'Mine', 'Hammer', 'Carry', 'Pickup', 'PutDown',
                         'GroundWork', 'BenchWork', 'Hurt', 'Throw', 'Exercise', 'Sit', 'Eat') if c in ACTS]
    cands = carry_candidates()
    rigids = [(k, p, Rigid(f'{k}{i}', me, b, Matrix())) for i, (k, p, b, me, _) in enumerate(cands)]
    score = [0] * len(rigids); worst = [{} for _ in rigids]
    for clip, f, pose, trees in play(clips):
        for i, (k, p, r) in enumerate(rigids):
            t = r.tree(pose)
            for name, pt in trees.items():
                n = len(t.overlap(pt))
                if n: score[i] += n; worst[i][name] = worst[i].get(name, 0) + 1
    best = {}
    for i, (k, p, r) in enumerate(rigids):
        if k not in best or score[i] < score[best[k]]: best[k] = i
    res = {k: {'params': rigids[i][1], 'parent': cands[i][2], 'overlap_pairs': score[i], 'frames_by_part': worst[i],
               'world_rest': [[list(r) for r in M] for M in cands[i][4]]} for k, i in best.items()}
    res['_clips'] = clips; res['_candidates'] = {k: sum(1 for c in cands if c[0] == k) for k in best}
    pk.write_json(pk.KIT / 'previews' / 'carry-placement.json', res)
    print('CARRY', {k: (v['params'], v['overlap_pairs']) for k, v in res.items() if not k.startswith('_')})


def full():
    items = worn_items()
    carry = json.loads((pk.KIT / 'previews' / 'carry-placement.json').read_text(encoding='utf-8')) if (pk.KIT / 'previews' / 'carry-placement.json').exists() else {}
    E = bpy.data.objects
    for k, v in carry.items():
        if k.startswith('_'): continue
        Ms = [Matrix(m) for m in v['world_rest']]
        if k == 'Sword': me = combined(E['Scabbard_Steel'], Ms[0], E['Weapon_Sword_Steel'], Ms[1])
        else:
            src = E['Weapon_Spear_Steel' if k == 'Spear' else 'Weapon_Shield_Steel']
            me = bpy.data.meshes.new(k); me.from_pydata([Ms[0] @ x.co for x in src.data.vertices], [], [list(p.vertices) for p in src.data.polygons])
        items.append((Rigid(f'Carry_{k}', me, v['parent'], Matrix()), None))
    rep = {}; t0 = time.time(); nframes = 0
    for clip, f, pose, trees in play(sorted(ACTS)):
        nframes += 1
        for r, ears in items:
            t = r.tree(pose)
            for name in targets(r.name, ears):
                n = len(t.overlap(trees[name]))
                if n:
                    d = rep.setdefault(r.name, {}).setdefault(name, {})
                    d[clip] = max(d.get(clip, 0), n)
    summary = {}
    for item, parts in rep.items():
        for part, clips in parts.items():
            summary.setdefault(item, {})[part] = {'clips': len(clips), 'worst_pairs': max(clips.values()), 'list': clips}
    pk.write_json(pk.KIT / 'previews' / ('fit-report-motions.json' if MOTIONS else 'fit-report.json'), {'frames_checked': nframes, 'step': STEP, 'seconds': round(time.time() - t0),
                                                             'items': [r.name for r, _ in items], 'overlaps': summary})
    print('FIT', nframes, 'frames', {k: len(v) for k, v in summary.items()})


if 'search' in ARGS: search()
full()
