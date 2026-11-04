"""blender -b --factory-startup --python scripts/build_motions.py → pk-motions.blend + previews/motion-report.json

1. PK|<clip> actions from motions_pk (body key poses, finger curls, events).
2. Two-handed clips: left wrist IK + exact hand rotation onto the tool grip2, baked per frame (export-safe, no constraint left).
3. Measured per clip with its prop on Socket.Tool.R (Steel tier):
   finger skin→handle gap while the fist is closed (grip_fit), left palm→grip2 error, wrist bend, tool length drift
   (rigid check), contact point offset at the hit frame.
"""
import bpy, sys, json, importlib, math
from mathutils import Vector, Matrix
sys.path.insert(0, r"E:\Git\ant\art\production-kit-20261019\scripts")
import pk, motions_pk, grip_fit, rig_adapter
for m in (pk, motions_pk, grip_fit, rig_adapter): importlib.reload(m)

bpy.ops.wm.open_mainfile(filepath=str(pk.KIT / 'pk-equipment.blend'))
scene = bpy.context.scene; vl = bpy.context.view_layer
rig = bpy.data.objects['KemonoRig']
rig.data.pose_position = 'POSE'
rig_adapter.add_sockets(rig)  # idempotent: palm sockets at the fist centre even on an older pk-equipment.blend
BODY = bpy.data.objects['Part_Body_BASE_00']
for a in [a for a in bpy.data.actions if a.name.startswith('PK|')]: bpy.data.actions.remove(a)
specs = motions_pk.create(rig)
E = bpy.data.objects
PROPS = {'Tool_Axe': 'Tool_Axe_Steel', 'Tool_Pickaxe': 'Tool_Pickaxe_Steel', 'Tool_Hammer': 'Tool_Hammer_Steel', 'Tool_Shovel': 'Tool_Shovel_Steel',
         'Tool_Hoe': 'Tool_Hoe_Steel', 'Tool_Sickle': 'Tool_Sickle_Steel', 'Weapon_Sword': 'Weapon_Sword_Steel', 'Weapon_Spear': 'Weapon_Spear_Steel',
         'Weapon_Shield': 'Weapon_Shield_Steel'}


def attach(src_name, socket):
    o = E[src_name].copy(); scene.collection.objects.link(o); o.name = 'Prop__' + src_name
    o.parent = rig; o.parent_type = 'BONE'; o.parent_bone = socket; o.matrix_parent_inverse.identity()
    o.location = (0, -rig.data.bones[socket].length, 0); o.rotation_euler = (0, 0, 0)
    return o


def use(act):
    rig.animation_data.action = act
    if act.slots: rig.animation_data.action_slot = act.slots[0]


def solve_arm(side, H):
    """Two-bone arm onto hand matrix H (armature space), no IK constraint.
    The elbow sits on the circle allowed by the bone lengths; the point with the straightest wrist wins
    (an elbow crossing into the body is penalised). The forearm twist follows the
    hand so the wrist only bends, then hand.<side> takes H exactly. Returns the wrist bend in degrees."""
    pbs = rig.pose.bones; ua, fa, hd = pbs['upper_arm.' + side], pbs['forearm.' + side], pbs['hand.' + side]
    a, b = ua.bone.length, fa.bone.length
    S = ua.head.copy(); W = H.translation; hand_y = H.to_3x3().normalized().col[1]
    d = min((W - S).length, (a + b) * .999); u = (W - S).normalized()
    x = (a * a - b * b + d * d) / (2 * d); rr = math.sqrt(max(a * a - x * x, 0)); C = S + u * x
    e1 = u.orthogonal().normalized(); e2 = u.cross(e1)
    out = -1 if side == 'R' else 1  # +X is the character's left
    best = None
    for k in range(72):
        t = math.tau * k / 72; E = C + rr * (math.cos(t) * e1 + math.sin(t) * e2)
        fdir = (W - E).normalized()
        cost = fdir.angle(hand_y) + 3 * max(0, (E.x - S.x) * out * -1 - .01) / .05
        if best is None or cost < best[0]: best = (cost, E)
    E = best[1]

    def aim(pb, head, y, x_hint):
        y = y.normalized(); xx = (x_hint - y * x_hint.dot(y)).normalized(); z = xx.cross(y)
        pb.matrix = Matrix.Translation(head) @ Matrix((xx, y, z)).transposed().to_4x4(); vl.update()
    aim(ua, S, E - S, ua.matrix.to_3x3().col[0])
    aim(fa, E, W - E, H.to_3x3().normalized().col[0])  # twist with the hand
    hd.matrix = H; vl.update()
    return math.degrees((W - E).angle(hand_y))


def arm_cost(side, H, S):
    """Pure-math version of solve_arm's choice: (cost, wrist bend rad) for hand matrix H with the shoulder at S."""
    a, b = rig.data.bones['upper_arm.' + side].length, rig.data.bones['forearm.' + side].length
    W = H.translation; hand_y = H.to_3x3().normalized().col[1]; dv = W - S; L = dv.length
    over = max(0, L - (a + b) * .97) * 60  # out of reach
    d = min(L, (a + b) * .999); u = dv.normalized()
    x = (a * a - b * b + d * d) / (2 * d); rr = math.sqrt(max(a * a - x * x, 0)); C = S + u * x
    e1 = u.orthogonal().normalized(); e2 = u.cross(e1); out = -1 if side == 'R' else 1; best = 9
    for k in range(24):
        t = math.tau * k / 24; E = C + rr * (math.cos(t) * e1 + math.sin(t) * e2)
        best = min(best, (W - E).angle(hand_y) + 3 * max(0, (E.x - S.x) * out * -1 - .01) / .05)
    return best + over


TWIST = (('spine', .25), ('spine_01', .35), ('spine_02', .4))  # same split as kemono spine(y=): local Y = torso twist
TWISTS = (-.9, -.6, -.3, 0, .3, .6, .9)
CLEAR_AXIS = .15  # handle ↔ torso axis; body + breastplate reach ~.09–.11 m from the axis at chest height
CLEAR_HEAD = .29  # handle ↔ head ball centre (head bone +0.2 m); BOB hair spans ±0.28 m
CLEAR_PLATE = .035  # handle line ↔ rigid plate surface (handle radius ~1.5 cm + 2 cm)
RIGID_PLATES = ('Armor_Breastplate', 'Armor_Breastplate_Lower', 'Armor_Fauld', 'Armor_Tasset_L', 'Armor_Tasset_R')


def bake_two_hand(name, prop, frames, one_hand=False):
    """Two-handed grip, baked per frame (export-safe, no constraint left).
    Tool rotation/path = motions_pk.TOOL_KEYS. At each key a torso twist (keyed on the spine) and a tool slide of up to
    ~15 cm (rotation untouched) are chosen together: both wrists straightest and in reach, 3°/cm penalty on the slide, and the
    whole handle kept CLEAR_AXIS from the torso axis and CLEAR_HEAD from the head. Every frame then re-searches round the
    key interpolation (twist ±0.2, slide ±3 cm) with a penalty on jumps from the previous frame.
    Both arms are solved with solve_arm; the right hand carries the tool, the left may turn round the handle.
    one_hand: only the right arm follows the path (left arm keeps its FK keys)."""
    bones = rig.data.bones; pbs = rig.pose.bones; Mw = rig.matrix_world; Mi = Mw.inverted()
    hand_in = {s: (Mw @ bones['Socket.Tool.' + s].matrix_local).inverted() @ (Mw @ bones['hand.' + s].matrix_local) for s in 'RL'}
    palmR = bones['Socket.Tool.R'].matrix_local.to_3x3().inverted() @ Vector((1, 0, 0))
    palmL = bones['Socket.Tool.L'].matrix_local.to_3x3().inverted() @ Vector((-1, 0, 0))
    src = Matrix((Vector((0, 1, 0)), palmL.normalized(), Vector((0, 1, 0)).cross(palmL).normalized())).transposed()
    g2 = Vector(prop.get('grip2') or (0, 0, 0))
    sides = 'R' if one_hand else 'RL'

    def hands(f, off):
        """Armature-space hand matrices: right, and the left at each roll round the handle."""
        P = Matrix.Translation(off) @ motions_pk.tool_matrix(name, f)
        R = P.to_3x3().normalized(); y = (R @ Vector((0, 1, 0))).normalized()
        palm = -(R @ palmR); palm = (palm - y * palm.dot(y)).normalized()
        dst = Matrix((y, palm, y.cross(palm))).transposed(); gl = P @ g2
        HL = Matrix.Translation(gl) @ (dst @ src.inverted()).to_4x4() @ hand_in['L']
        rolls = [Matrix.Translation(gl) @ Matrix.Rotation(math.radians(r), 4, y) @ Matrix.Translation(-gl) @ HL for r in range(-90, 91, 15)]
        return P @ hand_in['R'], rolls

    ys = [c[1] for c in prop.bound_box]; hy = [min(ys) + (max(ys) - min(ys)) * i / 7 for i in range(8)]

    from mathutils.bvhtree import BVHTree
    plates = [(bpy.data.objects[n]['bone'], BVHTree.FromPolygons([v.co.copy() for v in bpy.data.objects[n].data.vertices],
                                                               [q.vertices[:] for q in bpy.data.objects[n].data.polygons]))
              for n in RIGID_PLATES if n in bpy.data.objects]  # rest-pose (armature space) shells, moved with their bone

    def gap(P, body):
        """How far the handle line (butt → head) intrudes into the torso capsule (pelvis → neck), the head/hair ball or
        CLEAR_PLATE round the rigid torso plates, m."""
        a, b, hc, pl = body; d = b - a; out = 0
        for y in hy:
            p = P @ Vector((0, y, 0)); t = min(max((p - a).dot(d) / d.length_squared, 0), 1)
            out = max(out, CLEAR_AXIS - (p - a - d * t).length, CLEAR_HEAD - (p - hc).length)
            for inv, tree in pl:
                hit = tree.find_nearest(inv @ p, CLEAR_PLATE)
                if hit[0] is not None: out = max(out, CLEAR_PLATE - hit[3])
        return out

    def body_now():
        hd = pbs['head']
        pl = [((pbs[bn].matrix @ bones[bn].matrix_local.inverted()).inverted(), tree) for bn, tree in plates]
        return (pbs['pevis'].head.copy(), pbs['neck'].head.copy(), hd.head + (hd.tail - hd.head).normalized() * .2, pl)

    def cost(f, off, sh, body):
        HR, rolls = hands(f, off)
        cl = 0 if one_hand else min(arm_cost('L', h, sh['L']) for h in rolls)
        P = Matrix.Translation(off) @ motions_pk.tool_matrix(name, f)
        return max(arm_cost('R', HR, sh['R']), cl) + math.radians(3) * off.length * 100 + 20 * max(0, gap(P, body))

    def set_twist(t, base):
        for (n, w), b in zip(TWIST, base): pbs[n].rotation_euler.y = b + t * w
        vl.update()

    def search(f, twists, offs, base, extra=lambda t, o: 0):
        best = None
        for t in twists:  # torso twist brings the far shoulder toward the handle (chibi arms reach only ~0.3 m)
            set_twist(t, base)
            sh = {s: pbs['upper_arm.' + s].head.copy() for s in 'RL'}; body = body_now()
            for o in offs(t):
                c = cost(f, o, sh, body) + .3 * abs(t) + extra(t, o)
                if best is None or c < best[0]: best = (c, t, o)
        return best[1], best[2]
    grid = lambda step, n: [Vector((x * step, y * step, z * step)) for x in range(-n, n + 1) for y in range(-n, n + 1) for z in range(-n, n + 1)]
    key_off, key_twist = {}, {}
    for k in motions_pk.TOOL_KEYS[name]:  # keys: wide search (±15 cm, any twist), then refined
        f = k[0]; scene.frame_set(min(f, frames[-1])); vl.update()
        base = [pbs[n].rotation_euler.y for n, _ in TWIST]
        t, o = search(f, TWISTS, lambda t: grid(.05, 3), base)
        t, o = search(f, [t], lambda t: [o + g for g in grid(.025, 1)], base)
        key_off[f] = o; key_twist[f] = t
    kf = sorted(key_off)

    def interp(f, d):
        f0 = max(k for k in kf if k <= f); f1 = min(k for k in kf if k >= f)
        t = 0 if f1 == f0 else (f - f0) / (f1 - f0); t = t * t * (3 - 2 * t)
        return d[f0] * (1 - t) + d[f1] * t
    frame_off, frame_twist = {}, {}; prev = None
    bases = {}
    for f in frames:  # authored spine twist, read before any per-frame key below changes the curve
        scene.frame_set(f); bases[f] = [pbs[n].rotation_euler.y for n, _ in TWIST]
    for f in frames:  # every frame: local search round the key interpolation, penalising jumps from the previous frame
        scene.frame_set(f); vl.update()
        base = bases[f]
        t0, o0 = interp(f, key_twist), interp(f, key_off)
        jump = (lambda t, o: 0) if prev is None else (lambda t, o: 2 * abs(t - prev[0]) + math.radians(6) * (o - prev[1]).length * 100)
        t, o = search(f, [t0 + d for d in (-.2, -.1, 0, .1, .2)], lambda t: [o0 + g for g in grid(.03, 1)], base, jump)
        set_twist(t, base)
        for n, _ in TWIST: pbs[n].keyframe_insert('rotation_euler', index=1, frame=f, group=n)
        frame_off[f] = o; frame_twist[f] = t; prev = (t, o)

    def offset(f): return frame_off[f]
    chain = [b + '.' + s for s in sides for b in ('upper_arm', 'forearm', 'hand')]
    mats = {}; bends = {'R': [], 'L': []}; extra = []
    for f in frames:
        scene.frame_set(f); vl.update()
        sh = {s: pbs['upper_arm.' + s].head.copy() for s in 'RL'}
        reach = {s: (bones['upper_arm.' + s].length + bones['forearm.' + s].length) * .97 for s in 'RL'}
        o = offset(f); HR, rolls = hands(f, o)
        chest = (sh['R'] + sh['L']) / 2 + Vector((0, -.16, -.12)); mid = (HR.translation + rolls[6].translation) / 2

        def ok(t):  # last resort: slide further toward the chest until both wrists are within reach
            dv = (chest - mid) * t
            return (HR.translation + dv - sh['R']).length <= reach['R'] and (one_hand or min((h.translation + dv - sh['L']).length for h in rolls) <= reach['L'])
        if not ok(0):
            lo, hi = 0.0, 1.0
            for _ in range(14):
                m = (lo + hi) / 2; lo, hi = (lo, m) if ok(m) else (m, hi)
            HR, rolls = hands(f, o + (chest - mid) * hi); extra.append((f, round(((chest - mid) * hi).length, 3)))
        bends['R'].append(solve_arm('R', HR))
        if not one_hand: bends['L'].append(solve_arm('L', min(rolls, key=lambda h: arm_cost('L', h, sh['L']))))
        mats[f] = {b: pbs[b].matrix.copy() for b in chain}
    for f in frames:
        scene.frame_set(f)
        for b in chain:
            pb = pbs[b]; pb.matrix = mats[f][b]; vl.update()
            pb.keyframe_insert('rotation_euler', frame=f, group=b)
    return {'slide_at_keys_m': {f: [round(v, 3) for v in o] for f, o in key_off.items()},
            'twist_at_keys': key_twist, 'twist_range': [round(min(frame_twist.values()), 2), round(max(frame_twist.values()), 2)],
            'wrist_bend_by_frame_deg': {s: [round(v) for v in bends[s]] for s in sides},
            'reach_slide_max_m': max((e for _, e in extra), default=0), 'reach_slide_frames': extra}


def bake_hands(name, frames):
    """Hands-only contact clips: each listed hand's palm socket is put on its HAND_KEYS target (finger direction + palm
    normal give the hand rotation), the arm solved with solve_arm and baked per frame. Reports the reach error per side."""
    bones = rig.data.bones; pbs = rig.pose.bones
    keyed = [s for s in 'RL' if motions_pk.hand_target(name, s, 1)]
    rest = {s: (bones['hand.' + s].matrix_local, bones['Socket.Tool.' + s].matrix_local) for s in keyed}
    chain = [b + '.' + s for s in keyed for b in ('upper_arm', 'forearm', 'hand')]
    mats, err, bend = {}, {s: [] for s in keyed}, {s: [] for s in keyed}
    for f in frames:
        scene.frame_set(f); vl.update()
        for s in keyed:
            p, fing, palm = motions_pk.hand_target(name, s, f)
            Hr, Sr = rest[s]; R0 = Hr.to_3x3().normalized()
            pl = R0.inverted() @ Vector((1, 0, 0) if s == 'R' else (-1, 0, 0))  # rest palm normal in hand space
            y0 = Vector((0, 1, 0)); x0 = (pl - y0 * pl.dot(y0)).normalized()
            n = (palm - fing * palm.dot(fing)).normalized()
            Rh = Matrix((fing, n, fing.cross(n))).transposed() @ Matrix((y0, x0, y0.cross(x0))).transposed().inverted()
            o = (Hr.inverted() @ Sr).translation
            H = Matrix.Translation(p - Rh @ o) @ Rh.to_4x4()
            bend[s].append(solve_arm(s, H))
        mats[f] = {b: pbs[b].matrix.copy() for b in chain}
    for f in frames:
        scene.frame_set(f)
        for b in chain:
            pb = pbs[b]; pb.matrix = mats[f][b]; vl.update()
            pb.keyframe_insert('rotation_euler', frame=f, group=b)
    for f in frames:  # measured on the baked keys (location is not keyed, so an out-of-reach hand shows its real gap)
        scene.frame_set(f); vl.update()
        for s in keyed: err[s].append((pbs['Socket.Tool.' + s].head - motions_pk.hand_target(name, s, f)[0]).length)
    return {s: {'palm_error_max_m': round(max(err[s]), 3), 'palm_error_mean_m': round(sum(err[s]) / len(err[s]), 3),
                'wrist_bend_max_deg': round(max(bend[s]))} for s in keyed}


def wrist_bend(side):
    """Angle between forearm and hand directions (deg) — flags contact bought with a broken wrist."""
    f, h = rig.pose.bones['forearm.' + side], rig.pose.bones['hand.' + side]
    return math.degrees((f.tail - f.head).angle(h.tail - h.head))


# Hand-held prop vs the character (review combo BOB hair / RABBIT ears / FOX tail + body armour): triangle overlaps per
# held frame (every 2nd). The gripping hand's own skin (palm + fingers of the holding side(s)) is left out: that contact is
# the grip itself. Everything else — arms, body, clothes, hair, ears, tail, plates — counts.
CLEAR_PARTS = ['Part_Body_BASE_ARMOR', 'Part_Outfit_LINEN_ARMOR', 'Part_Hair_BOB_00', 'Part_Ears_RABBIT_00', 'Part_Tail_FOX_00']
ARMOUR_PARTS = ['Armor_Breastplate', 'Armor_Breastplate_Lower', 'Armor_Bracer_L', 'Armor_Bracer_R', 'Armor_Fauld', 'Armor_Tasset_L', 'Armor_Tasset_R']


def hand_verts(body, sides):
    names = {g.index: g.name for g in body.vertex_groups}; out = set()
    for v in body.data.vertices:
        if not v.groups: continue
        n = names[max(v.groups, key=lambda g: g.weight).group]
        if n.startswith(('f_', 'thumb', 'hand.')) and n[-1] in sides: out.add(v.index)
    return out


def clearance(prop, frames, sides):
    from mathutils.bvhtree import BVHTree
    dg = bpy.context.evaluated_depsgraph_get(); hits = {}
    names = [n for n in CLEAR_PARTS + ARMOUR_PARTS if n in bpy.data.objects]
    for n in names: bpy.data.objects[n].hide_viewport = False; bpy.data.objects[n].hide_set(False)  # hidden objects are not deformed
    skip = hand_verts(bpy.data.objects['Part_Body_BASE_ARMOR'], sides)
    tv = [v.co.copy() for v in prop.data.vertices]; tp = [p.vertices[:] for p in prop.data.polygons]
    for f in frames[::2]:
        scene.frame_set(f); vl.update()
        W = prop.matrix_world; tt = BVHTree.FromPolygons([W @ v for v in tv], tp)
        for n in names:
            o = bpy.data.objects[n]
            if n in ARMOUR_PARTS:  # rigid plates follow their bone
                M = rig.matrix_world @ rig.pose.bones[o['bone']].matrix @ rig.data.bones[o['bone']].matrix_local.inverted()
                pt = BVHTree.FromPolygons([M @ v.co for v in o.data.vertices], [p.vertices[:] for p in o.data.polygons])
            else:
                ev = o.evaluated_get(dg); me = ev.to_mesh()
                polys = [p.vertices[:] for p in me.polygons if not (n == 'Part_Body_BASE_ARMOR' and any(i in skip for i in p.vertices))]
                pt = BVHTree.FromPolygons([ev.matrix_world @ v.co for v in me.vertices], polys); ev.to_mesh_clear()
            if tt.overlap(pt): hits.setdefault(n, []).append(f)
    return {n: {'frames': len(fr), 'of': len(frames[::2]), 'first': fr[:6]} for n, fr in hits.items()}


def tool_length(prop):
    vs = [prop.matrix_world @ v.co for v in prop.data.vertices]
    return max((vs[i] - vs[0]).length for i in range(0, len(vs), max(1, len(vs) // 40)))


report = {}
for name, c in specs.items():
    act = bpy.data.actions['PK|' + name]; use(act)
    frames = list(range(1, c['frames'] + 2))
    pid = PROPS.get(c.get('prop') or '')
    r = {'frames': c['frames'], 'loop': c['loop'], 'events': c.get('events', {}), 'prop': c.get('prop'), 'target': c.get('target', ''),
         'two_hand': bool(c.get('two_hand'))}
    if name in motions_pk.HAND_KEYS: r['hand_solve'] = bake_hands(name, frames)
    if pid:
        sock = 'Socket.Tool.L' if pid.startswith('Weapon_Shield') else 'Socket.Tool.R'
        prop = attach(pid, sock)
        if c.get('two_hand') and prop.get('grip2'): r['two_hand_solve'] = bake_two_hand(name, prop, frames)
        elif name in motions_pk.TOOL_KEYS: r['one_hand_solve'] = bake_two_hand(name, prop, frames, one_hand=True)
        side = sock[-1]

        def fist(sd, f):  # constant-interpolated grip curl of that hand at frame f is a closed fist
            keys = [k for k in c.get('grip', {}).get(sd, []) if k[0] <= f]
            return bool(keys) and keys[-1][1] >= motions_pk.FIST

        L0 = None; drift = 0; skin = {'R': [], 'L': []}; errL = []; bend = {'R': 0, 'L': 0}
        for f in frames:
            scene.frame_set(f); vl.update()
            L = tool_length(prop); L0 = L0 or L; drift = max(drift, abs(L - L0))
            for sd in 'RL': bend[sd] = max(bend[sd], wrist_bend(sd))
            if fist(side, f): skin[side].append(min(grip_fit.skin_gaps(BODY, prop, side, y_range=(-.04, .04)).values(), default=9))
            if c.get('two_hand') and prop.get('grip2'):
                gy = prop['grip2'][1]
                if fist('L', f): skin['L'].append(min(grip_fit.skin_gaps(BODY, prop, 'L', y_range=(gy - .04, gy + .04)).values(), default=9))  # 9 = hand off the handle
                palm = rig.matrix_world @ rig.pose.bones['Socket.Tool.L'].head
                errL.append((palm - prop.matrix_world @ Vector(prop['grip2'])).length)
        r['rigid_length_drift_m'] = round(drift, 6)
        r['prop_clearance'] = clearance(prop, [f for f in frames if fist(side, f)], side + ('L' if c.get('two_hand') else ''))  # {} = no overlap
        r['grip_skin_gap_m'] = {sd: {'min': round(min(v), 4), 'max': round(max(v), 4), 'frames': len(v)} for sd, v in skin.items() if v}
        r['wrist_bend_max_deg'] = {sd: round(v, 1) for sd, v in bend.items()}
        if errL: r['left_hand_grip2_error_m'] = {'max': round(max(errL), 4), 'mean': round(sum(errL) / len(errL), 4)}
        hit = c.get('events', {}).get('hit')
        if hit and prop.get('contact'):
            scene.frame_set(hit); vl.update()
            p = prop.matrix_world @ Vector(prop['contact'])
            r['contact_point_at_hit'] = [round(x, 3) for x in (rig.matrix_world.inverted() @ p)]
            r['contact_frame'] = hit
        bpy.data.objects.remove(prop)
    report[name] = r
rig.animation_data.action = None
for pb in rig.pose.bones: pb.matrix_basis.identity()
bpy.ops.wm.save_as_mainfile(filepath=str(pk.KIT / 'pk-motions.blend'))
pk.write_json(pk.KIT / 'previews' / 'motion-report.json', {'clips': report, 'reuse': motions_pk.REUSE})
print('MOTIONS', len(report))
