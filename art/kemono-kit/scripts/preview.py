"""Build kemono-preview-interactions.blend: the kemono combos playing their shared actions at the
resident-kit facilities (same facility-points.json as resident-kit/scripts/kit_preview.py).

Headless: blender -b --factory-startup --python preview.py
Review-only scenes; the facility kit and tools are LINKED from art/resident-kit, nothing there is modified."""
import bpy, json, math
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path('E:/Git/ant/art/kemono-kit')
RK = Path('E:/Git/ant/art/resident-kit')
POINTS = json.loads((RK / 'facility-points.json').read_text(encoding='utf-8'))
CLIPS = json.loads((ROOT / 'clips.json').read_text(encoding='utf-8'))
RECIPES = json.loads((ROOT / 'combinations.json').read_text(encoding='utf-8'))
PALETTES = json.loads((ROOT / 'parts-manifest.json').read_text(encoding='utf-8'))['palettes']
# Facilities were sized for the 2.07-tall resident; the kemono stands 1.19 to the hair top.
# 1.6 puts its hands at the old hand height (~.69), so work points and seat heights carry over.
K = 1.6
SEAT_HIP = .60  # thigh underside above the root when seated, scaled
APPROACH = .35  # shorter arms than the old resident: step this much closer to the facility (facility units)
SEATED = {'Sit', 'Eat', 'Rest', 'PlayBoard', 'Bathe', 'Swing', 'Spin', 'Captive'}
FACE = {'Sleep': 'SLEEP', 'Death': 'DEAD', 'Hurt': 'PAIN', 'Downed': 'PAIN', 'Eat': 'TASTY', 'Mourn': 'SAD',
        'Rest': 'SLEEPY', 'Bathe': 'SLEEPY', 'Captive': 'SAD'}

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.name = 'Preview_Index'


def link(path, **kinds):
    with bpy.data.libraries.load(str(path), link=True) as (a, b):
        for k, pred in kinds.items():
            setattr(b, k, [n for n in getattr(a, k) if pred(n)])


link(ROOT / 'kemono-character-kit.blend', objects=lambda n: n == 'KemonoRig' or n.startswith('Part_'))
link(ROOT / 'kemono-animations.blend', actions=lambda n: n.startswith('Kemono|'))
link(RK / 'resident-tools.blend', objects=lambda n: n.startswith(('Tool_', 'Prop_')))
link(RK / 'resident-facility-kit.blend', objects=lambda n: True)  # children are not pulled in by their parents
PARTS = {}
for o in bpy.data.objects:
    if o.name.startswith('Part_') and o.type == 'MESH':
        PARTS.setdefault((o['slot'], o['choice']), []).append(o)


def build_combo(name, selection, palette, collection, face=None):
    rig = bpy.data.objects['KemonoRig'].copy(); rig.name = name; rig.animation_data_clear()
    rig.data = rig.data.copy(); rig.data.pose_position = 'POSE'  # the kit file saves its armature in REST
    rig.scale = (K, K, K); rig.hide_render = True  # hide_viewport would drop it from evaluation (no pose)
    collection.objects.link(rig)
    selection = dict(selection)
    if face: selection['Face'] = face
    mats = {}
    for slot, keys in selection.items():
        for key in ([keys] if isinstance(keys, str) else keys):
            for src in PARTS[(slot, key)]:
                o = src.copy(); o.name = name + '__' + src.name; collection.objects.link(o)
                o.parent = rig; o.matrix_parent_inverse = Matrix.Identity(4); o.matrix_basis = Matrix.Identity(4)
                o.hide_viewport = o.hide_render = False
                for m in o.modifiers:
                    if m.type == 'ARMATURE': m.object = rig
                if slot in ('Hair', 'Ears', 'Tail'):  # same palette recolour as build.py copy_combo
                    for ms in o.material_slots:
                        old = ms.material
                        if not old or 'Inner' in old.name: continue
                        if old not in mats:
                            m = old.copy(); p = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
                            for sock in ('Base Color', 'Emission Color'):
                                for l in list(p.inputs[sock].links): m.node_tree.links.remove(l)
                                p.inputs[sock].default_value = (*PALETTES[palette], 1)
                            mats[old] = m
                        ms.link = 'OBJECT'; ms.material = mats[old]
    return rig


def attach(obj, rig, bone, armature_rest):
    """Parent to a socket bone at armature_rest (rig space), cancelling the rig's K scale so props keep kit size."""
    b = rig.data.bones[bone]
    obj.parent = rig; obj.parent_type = 'BONE'; obj.parent_bone = bone
    obj.matrix_parent_inverse = Matrix.Translation((0, -b.length, 0))
    obj.matrix_basis = b.matrix_local.inverted() @ armature_rest @ Matrix.Scale(1 / K, 4)


def play(rig, clip):
    rig.animation_data_create(); act = bpy.data.actions['Kemono|' + clip]
    rig.animation_data.action = act; rig.animation_data.action_slot = act.slots[0]


def lights(scene):
    w = bpy.data.worlds.get('KitWorld') or bpy.data.worlds.new('KitWorld')
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == 'BACKGROUND')
    bg.inputs['Color'].default_value = (.93, .90, .84, 1); bg.inputs['Strength'].default_value = .55
    scene.world = w
    sun = bpy.data.objects.get('KitSun') or bpy.data.objects.new('KitSun', bpy.data.lights.new('KitSun', 'SUN'))
    sun.data.energy = 2.2; sun.rotation_euler = (math.radians(50), 0, math.radians(-40))
    scene.collection.objects.link(sun)
    scene.render.fps = 24; scene.render.resolution_x = scene.render.resolution_y = 360
    scene.view_settings.view_transform = 'Standard'


def camera(scene, objs, direction):
    bpy.context.window_manager.windows[0].scene = scene
    pts = []
    for i in range(5):
        scene.frame_set(scene.frame_start + round((scene.frame_end - scene.frame_start) * i / 4))
        dg = scene.view_layers[0].depsgraph; dg.update()
        for o in objs:
            if o.type == 'MESH':
                ev = o.evaluated_get(dg); pts.extend(ev.matrix_world @ Vector(v) for v in ev.bound_box)
    scene.frame_set(1)
    lo = Vector([min(p[i] for p in pts) for i in range(3)]); hi = Vector([max(p[i] for p in pts) for i in range(3)])
    cam = bpy.data.objects.new(scene.name + '_Cam', bpy.data.cameras.new(scene.name + '_Cam'))
    scene.collection.objects.link(cam); scene.camera = cam
    d = Vector(direction).normalized(); cam.location = (lo + hi) / 2 + d * 30
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    basis = cam.rotation_euler.to_matrix().transposed()
    extent = max(max(abs(q.x), abs(q.y)) for q in (basis @ (p - (lo + hi) / 2) for p in pts)) * 2
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = extent * 1.15 + .2; cam.data.clip_end = 100


HANDS = ('hand', 'forearm', 'f_', 'thumb')
SEAT_GROUPS = ('pevis', 'spine', 'thigh', 'shin', 'foot', 'Root')
FOOT_GROUPS = ('shin', 'foot')
_dominant = {}


def dominant(o):
    """Per-vertex strongest deform group name (cached per mesh)."""
    if o.data.name not in _dominant:
        names = {g.index: g.name for g in o.vertex_groups}
        _dominant[o.data.name] = [names.get(max(v.groups, key=lambda g: g.weight).group, '') if v.groups else '' for v in o.data.vertices]
    return _dominant[o.data.name]


def mesh_world(objs, dg, groups=False):
    vs, fs, gs = [], [], []
    for o in objs:
        if o.type != 'MESH': continue
        ev = o.evaluated_get(dg); me = ev.to_mesh(); n = len(vs); grp = dominant(o) if groups else None
        vs += [ev.matrix_world @ v.co for v in me.vertices]
        for p in me.polygons:
            fs.append(tuple(n + i for i in p.vertices))
            if groups: gs.append(grp[p.vertices[0]] if p.vertices[0] < len(grp) else '')
        ev.to_mesh_clear()
    return vs, fs, gs


def sample(sc, rig, fac_objs, item_objs):
    """Per sampled frame: body triangles (with dominant deform group) and the facility (or carried item) triangles."""
    bpy.context.window_manager.windows[0].scene = sc
    out = []
    for f in range(sc.frame_start, sc.frame_end, 2):  # same frames the renders use, so swinging doors are not missed
        sc.frame_set(f); dg = bpy.context.evaluated_depsgraph_get()
        body = [o for o in rig.children if o.type == 'MESH' and 'Part_Face' not in o.name and o not in item_objs]
        out.append({'body': mesh_world(body, dg, True), 'other': mesh_world(fac_objs, dg), 'trees': {}})
    sc.frame_set(1)
    return out


def keep(g, groups):
    return g.startswith(groups) if isinstance(groups, tuple) else not g.startswith(HANDS + groups[0])


def crossing(frames, groups, offset=Vector()):
    """Triangle pairs where the body (faces in `groups`; a 1-list means 'all but hands and these') crosses the
    facility/item shifted by -offset, i.e. the body shifted by +offset. Exact, so open meshes give no false hits."""
    from mathutils.bvhtree import BVHTree
    n = 0
    for f in frames:
        key = groups if isinstance(groups, tuple) else ('all-but',) + groups[0]
        if key not in f['trees']:
            vs, fs, gs = f['body']; sel = [fc for fc, g in zip(fs, gs) if keep(g, groups)]
            f['trees'][key] = BVHTree.FromPolygons(vs, sel) if sel else None
        bt = f['trees'][key]; ovs, ofs, _ = f['other']
        if bt is None or not ofs: continue
        n += len(bt.overlap(BVHTree.FromPolygons([v - offset for v in ovs], ofs)))
    return n


def lowest_clear(test, lo, hi, steps=8):
    """Smallest value in [lo, hi] with test(v) == 0 (test decreasing in v); None if hi is not clear either."""
    if test(lo) == 0: return lo
    if test(hi) != 0: return None
    for _ in range(steps):
        mid = (lo + hi) / 2
        if test(mid) == 0: hi = mid
        else: lo = mid
    return hi


def clearance(sc, rig, fac_objs, mode, item_objs=(), item=None):
    """Remove body/facility interpenetration that the shared placement constants leave behind.
    seat: slide along the seat (usually forward, so the knees clear the front edge) and settle onto it; ride: height only;
    lie: settle the torso/legs onto the bed (head and ears may press into the pillow);
    stand: lift feet onto raised floors, then step back (or aside) until the body (hands excluded, they touch the work) is clear;
    carry: push the item forward off the chest. Records what it did in scene['clear']."""
    frames = sample(sc, rig, fac_objs, list(item_objs))
    up, back = Vector((0, 0, 1)), rig.matrix_world.to_3x3() @ Vector((0, 1, 0))  # rig faces -Y locally
    back.z = 0; back.normalize()
    to_parent = rig.parent.matrix_world.inverted().to_3x3() if rig.parent else Matrix.Identity(3)
    before = crossing(frames, [()])
    moved, note = Vector(), ''
    if mode == 'seat':
        best = None
        for d in [0] + [x * .05 for x in range(-12, 7) if x]:  # -: toward the seat front, knees clear the edge
            z = lowest_clear(lambda v: crossing(frames, SEAT_GROUPS, back * d + up * v), -.15, .2)
            if z is not None and (best is None or abs(z) + .7 * abs(d) < best[0]): best = (abs(z) + .7 * abs(d), back * d + up * z)
        if best is None:  # no clean spot: least crossing on the grid
            best = min(((crossing(frames, SEAT_GROUPS, back * d + up * z), back * d + up * z) for d in (-.6, -.45, -.3, -.15, 0, .15) for z in (-.1, 0, .1, .2)), key=lambda x: x[0]); note = 'partial'
        moved = best[1]
    elif mode == 'ride':  # moving seat (swing): only settle height onto it, never slide off
        z = lowest_clear(lambda v: crossing(frames, SEAT_GROUPS, up * v), -.15, .25)
        moved = up * (z if z is not None else .25); note = '' if z is not None else 'partial'
    elif mode == 'lie':
        z = lowest_clear(lambda v: crossing(frames, SEAT_GROUPS, up * v), -.15, .35)  # head/hair may sink into the pillow
        moved = up * (z if z is not None else .35); note = '' if z is not None else 'partial'
    elif mode == 'floor':  # running inside the treadmill wheel: stay inside, only stand on the planks
        moved = up * (lowest_clear(lambda v: crossing(frames, FOOT_GROUPS, up * v), 0, .3) or 0)
    elif mode == 'stand':
        z = lowest_clear(lambda v: crossing(frames, FOOT_GROUPS, up * v), 0, .3) or 0; moved = up * z
        d = lowest_clear(lambda v: crossing(frames, [()], moved + back * v), 0, .6)
        if d is None:  # e.g. a door swinging through the spot: also try stepping aside
            side = up.cross(back)
            for sv in (.15, -.15, .3, -.3, .45, -.45, .6, -.6):
                d = lowest_clear(lambda v: crossing(frames, [()], moved + side * sv + back * v), 0, .6)
                if d is not None: moved += side * sv; break
        if d is None:  # nowhere fully clear nearby: least crossing on a wider grid, preferring small moves
            best = min(((crossing(frames, [()], moved + side * sv + back * bv) + 20 * (abs(sv) + bv), sv, bv)
                        for bv in (0, .2, .4, .6, .8, 1.0) for sv in (-.8, -.6, -.4, -.2, 0, .2, .4, .6, .8)))
            moved += side * best[1] + back * best[2]; d = None
        else: moved += back * d
        note = '' if d is not None else 'partial'
    elif mode == 'carry' and item is not None:
        fwd = -back
        d = lowest_clear(lambda v: crossing(frames, [()], -fwd * v), 0, .5)
        note = '' if d is not None else 'partial'; d = d if d is not None else .5
        loc_fwd = (rig.matrix_world.inverted().to_3x3() @ fwd).normalized() * d / K  # rig space, kit units
        item.matrix_basis = Matrix.Translation(rig.data.bones['Socket.Carry'].matrix_local.to_3x3().inverted() @ loc_fwd) @ item.matrix_basis
        sc['clear'] = f'carry fwd {d:.3f} (crossings {before}) {note}'
        return
    rig.location += to_parent @ moved
    sc['clear'] = f'{mode} move {tuple(round(x, 3) for x in moved)} (crossings {before}) {note}'


def facility_scene(rid, recipe, name=None):
    info = POINTS[rid]; work = info['work']; clip = work['clip']
    sc = bpy.data.scenes.new(name or 'Preview__' + rid); lights(sc)
    fac = bpy.data.objects[rid]
    for o in [fac, *fac.children_recursive]: sc.collection.objects.link(o)
    sc.view_layers[0].update(); at = fac.matrix_world.copy()
    cname, selection, palette = recipe
    rig = build_combo(sc.name + '__' + cname, selection, palette, sc.collection, FACE.get(clip))
    visible = [fac, *fac.children_recursive, *rig.children]
    if clip == 'Carry':  # the item rides the carry socket as a local copy of the linked facility
        items = [fac, *fac.children_recursive]
        for o in items: sc.collection.objects.unlink(o)
        copy = {o: o.copy() for o in items}
        for o, c in copy.items():
            sc.collection.objects.link(c)
            if o.parent in copy: c.parent = copy[o.parent]
        lo, hi = Vector(info['bounds_min']), Vector(info['bounds_max'])
        grip = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z + (hi.z - lo.z) * .35))
        sock = rig.data.bones['Socket.Carry'].matrix_local.translation
        # rig space: grip point on the socket at kit size (attach appends the 1/K)
        attach(copy[fac], rig, 'Socket.Carry', Matrix.Translation(sock) @ Matrix.Scale(1 / K, 4) @ Matrix.Translation(-grip) @ Matrix.Scale(K, 4))
        visible = list(copy.values()) + list(rig.children)
    else:
        if clip in SEATED and 'seat' in work:
            s = Vector(work['seat']); rig.location = at @ Vector((s.x, s.y, s.z - SEAT_HIP)); rig.rotation_euler.z = 0
        elif clip == 'Sleep' and 'lie' in work:
            s = Vector(work['lie']); rig.location = at @ Vector((s.x, s.y - .95, s.z - .12)); rig.rotation_euler.z = 0  # Sleep lays the body along +Y
        else:
            stand = Vector(work['stand']); lo, hi = Vector(info['bounds_min']), Vector(info['bounds_max'])
            to = ((lo + hi) / 2 - stand).to_2d()
            if to.length > .8: stand += (to.normalized() * APPROACH).to_3d()
            rig.location = at @ stand; rig.rotation_euler.z = math.pi  # face +Y (the facility)
        if 'turn' in work and clip != 'Sleep': rig.rotation_euler.z = work['turn']
        if work.get('ride'):  # swing seat moves with the facility's own pivot animation
            sc.frame_set(1); sc.view_layers[0].update()
            seat = bpy.data.objects[work['ride']]
            mw = rig.matrix_world.copy(); rig.parent = seat; rig.matrix_parent_inverse = seat.matrix_world.inverted(); rig.matrix_world = mw
    tool = work.get('tool') or ('Prop_Spear' if clip == 'Attack' else None)
    if tool:
        t = bpy.data.objects[tool].copy(); sc.collection.objects.link(t)
        attach(t, rig, 'Socket.Tool.R', rig.data.bones['Socket.Tool.R'].matrix_local); visible.append(t)
    play(rig, clip)
    hinge = max((int(o.animation_data.action.frame_range[1]) for o in fac.children_recursive
                 if o.animation_data and o.animation_data.action), default=0)
    sc.frame_start, sc.frame_end = 1, max(CLIPS[clip]['frames'] + 1, hinge)
    if clip == 'Carry':
        clearance(sc, rig, list(copy.values()), 'carry', item_objs=list(copy.values()), item=copy[fac])
    else:
        mode = 'ride' if work.get('ride') else 'floor' if clip == 'Run' else 'seat' if clip in SEATED and 'seat' in work else 'lie' if clip == 'Sleep' else 'stand'
        objs = [bpy.data.objects[work['ride']]] if mode == 'ride' else [o for o in [fac, *fac.children_recursive] if 'water' not in o.name.lower()]  # bathers sit in the water
        clearance(sc, rig, objs, mode)
    view =(1.5, -1.5, 1.25) if rig.rotation_euler.z == 0 else (2.4, .25, 1.35)
    if work.get('turn'): view = (-2.0, 1.2, 1.4)
    camera(sc, visible, view)
    sc['facility'] = rid; sc['combo'] = cname; sc['clip'] = clip
    return sc


def motion_scene(clip, recipe):
    sc = bpy.data.scenes.new('Motion__' + clip); lights(sc)
    cname, selection, palette = recipe
    rig = build_combo(sc.name + '__' + cname, selection, palette, sc.collection, FACE.get(clip))
    vis = list(rig.children)
    if clip == 'CarryPerson':  # the rescued resident lies across the carrier's arms
        hurt = build_combo(sc.name + '__Rescued', RECIPES[3][1], RECIPES[3][2], sc.collection, 'PAIN')
        play(hurt, 'Downed')
        sock = rig.data.bones['Socket.Carry'].matrix_local.translation
        attach(hurt, rig, 'Socket.Carry', Matrix.Translation(sock + Vector((.42, -.06, -.08))) @ Matrix.Scale(.7 * K, 4))  # Downed lies along -X
        vis += list(hurt.children)
    if clip == 'Attack':
        t = bpy.data.objects['Prop_Spear'].copy(); sc.collection.objects.link(t)
        attach(t, rig, 'Socket.Tool.R', rig.data.bones['Socket.Tool.R'].matrix_local); vis.append(t)
    play(rig, clip); sc.frame_start, sc.frame_end = 1, CLIPS[clip]['frames'] + 1
    camera(sc, vis, (-2.3, -1.6, 1.2))
    sc['clip'] = clip
    return sc


for i, rid in enumerate(POINTS):
    if POINTS[rid]['work'].get('clip'):
        facility_scene(rid, RECIPES[i % len(RECIPES)])
for r in RECIPES:  # same facility, appearance swapped: hand/tool placement must not change
    facility_scene('CC_Workshop', r, name='Swap__CC_Workshop__' + r[0])
for i, clip in enumerate(CLIPS):
    motion_scene(clip, RECIPES[i % len(RECIPES)])
from tool_grips import fit_tool
from tool_contact import apply_contact
for scene in bpy.data.scenes:
    with bpy.context.temp_override(scene=scene, view_layer=scene.view_layers[0]):
        for tool in scene.objects:
            if tool.parent_type == 'BONE':
                fit_tool(scene, tool)
                if tool.name.split('.')[0] in ('Tool_Axe', 'Tool_Hammer', 'Tool_Pickaxe', 'Prop_Spear'):
                    apply_contact(scene, tool)
bpy.context.window_manager.windows[0].scene = bpy.data.scenes['Preview_Index']
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'kemono-preview-interactions.blend'))
print('scenes', len(bpy.data.scenes) - 1)
