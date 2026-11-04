"""Build resident-preview.blend: review-only scenes that LINK the kit files.

facility (linked) + manual combo (local rig copy of linked parts) + tool on socket + shared action.
Nothing here is an asset to ship; open the kit files for that.
"""
import bpy, sys, json, math, importlib
from mathutils import Vector, Matrix
sys.path.insert(0, r"E:\Git\ant\art\resident-kit\scripts")
import kit_common as K, kit_combos as C
importlib.reload(K); importlib.reload(C)

POINTS = json.loads((K.KIT / 'facility-points.json').read_text(encoding='utf-8'))
CLIPS = json.loads(open(r"E:\Git\ant\art\resident-anim\clips.json", encoding="utf-8").read())
EXTRA = json.loads((K.KIT / 'clips-extra.json').read_text(encoding='utf-8'))
CLIPS.update(EXTRA)
SEATED = {'Sit', 'Eat', 'Rest', 'PlayBoard', 'Bathe', 'Swing', 'Spin', 'Captive'}

base = K.empty_file()
base.name = 'Preview_Index'


def link(path, **kinds):
    with bpy.data.libraries.load(str(path), link=True) as (a, b):
        for k, pred in kinds.items():
            setattr(b, k, [n for n in getattr(a, k) if pred(n)])
    return b


link(K.KIT / 'resident-character-kit.blend', objects=lambda n: n == 'ResidentRig' or n.startswith('Part_'))
link(K.KIT / 'resident-animations.blend', actions=lambda n: n.startswith('Resident|'))
link(K.KIT / 'resident-tools.blend', objects=lambda n: n.startswith(('Tool_', 'Prop_', 'Equip_')))
link(K.KIT / 'resident-facility-kit.blend', objects=lambda n: True)  # children are not pulled in by their parents
# palette materials come with the linked parts' library; combos look them up by name
with bpy.data.libraries.load(str(K.KIT / 'resident-character-kit.blend'), link=True) as (a, b):
    b.materials = [n for n in a.materials if n.startswith(('Fur_', 'Inner_'))]


def attach(obj, rig, bone, world_rest):
    """Parent to a socket bone so obj sits at world_rest (armature space) in the rest pose."""
    b = rig.data.bones[bone]
    obj.parent = rig; obj.parent_type = 'BONE'; obj.parent_bone = bone
    obj.matrix_parent_inverse = Matrix.Translation((0, -b.length, 0))
    obj.matrix_basis = b.matrix_local.inverted() @ world_rest


def play(rig, clip):
    rig.animation_data_create()
    act = bpy.data.actions['Resident|' + clip]
    rig.animation_data.action = act
    rig.animation_data.action_slot = act.slots[0]


def facility_instance(scene, rid):
    src = bpy.data.objects[rid]
    for o in [src, *src.children_recursive]:
        scene.collection.objects.link(o)
    return src


def camera(scene, objs, direction=(-2.3, -.9, 1.5)):
    if bpy.context.window_manager.windows:
        bpy.context.window_manager.windows[0].scene = scene
    original_frame = scene.frame_current
    pts = []
    for i in range(5):
        scene.frame_set(scene.frame_start + round((scene.frame_end - scene.frame_start) * i / 4))
        dg = scene.view_layers[0].depsgraph
        dg.update()
        for o in objs:
            if o.type != 'MESH': continue
            ev = o.evaluated_get(dg)
            pts.extend(ev.matrix_world @ Vector(v) for v in ev.bound_box)
    scene.frame_set(original_frame)
    lo = Vector([min(p[i] for p in pts) for i in range(3)]); hi = Vector([max(p[i] for p in pts) for i in range(3)])
    cam = bpy.data.objects.new(scene.name + '_Cam', bpy.data.cameras.new(scene.name + '_Cam'))
    scene.collection.objects.link(cam); scene.camera = cam
    d = Vector(direction).normalized(); cam.location = (lo + hi) / 2 + d * 30
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    basis = cam.rotation_euler.to_matrix().transposed()
    projected = [basis @ (p - (lo + hi) / 2) for p in pts]
    extent = max(max(abs(p.x), abs(p.y)) for p in projected) * 2
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = extent * 1.15 + .2; cam.data.clip_end = 100


def lights(scene):
    w = bpy.data.worlds.get('KitWorld') or bpy.data.worlds.new('KitWorld')
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == 'BACKGROUND')
    bg.inputs['Color'].default_value = (.93, .90, .84, 1); bg.inputs['Strength'].default_value = .55
    scene.world = w
    sun = bpy.data.objects.get('KitSun')
    if not sun:
        sun = bpy.data.objects.new('KitSun', bpy.data.lights.new('KitSun', 'SUN'))
        sun.data.energy = 2.2; sun.rotation_euler = (math.radians(50), 0, math.radians(-40))
    scene.collection.objects.link(sun)


def preview_scene(rid, combo_name, face=None, name=None):
    info = POINTS[rid]; work = info['work']; clip = work['clip']
    scene = bpy.data.scenes.new(name or 'Preview__' + rid)
    scene.render.fps = 24
    lights(scene)
    fac = facility_instance(scene, rid)
    scene.view_layers[0].update()
    at = fac.matrix_world.copy()  # linked: stays at its kit-file layout spot; points are facility-local
    face = face or C.STATE_FACE.get(clip) or (EXTRA.get(clip) or {}).get('face')
    rig = C.build_combo(scene.name + '__' + combo_name, C.COMBOS[combo_name], (0, 0, 0), scene.collection, face)
    visible = [fac, *fac.children_recursive, rig, *rig.children]
    if clip == 'Carry':
        # The item itself rides the carry socket; the facility root is a linked object, so carry a local copy.
        item = [o for o in [fac, *fac.children_recursive]]
        for o in item: scene.collection.objects.unlink(o)
        copy = {o: o.copy() for o in item}
        for o, c in copy.items():
            scene.collection.objects.link(c)
            if o.parent in copy: c.parent = copy[o.parent]
        root = copy[fac]
        lo, hi = Vector(info['bounds_min']), Vector(info['bounds_max'])
        sock = rig.data.bones['Socket.Carry'].matrix_local.translation
        attach(root, rig, 'Socket.Carry', Matrix.Translation(sock - Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z + (hi.z - lo.z) * .35))))
        visible = list(copy.values()) + [rig, *rig.children]
    else:
        if clip in SEATED and 'seat' in work:
            s = Vector(work['seat']); rig.location = at @ Vector((s.x, s.y, s.z - .30)); rig.rotation_euler.z = 0
        elif clip == 'Sleep' and 'lie' in work:
            s = Vector(work['lie']); rig.location = at @ Vector((s.x, s.y - 1.05, s.z - .05)); rig.rotation_euler.z = 0  # Sleep lays the body along +Y
        else:
            rig.location = at @ Vector(work['stand']); rig.rotation_euler.z = math.pi  # face +Y (the facility)
        if 'turn' in work and clip != 'Sleep':  # FURNITURE-97 entries state their facing explicitly
            rig.rotation_euler.z = work['turn']
        if work.get('ride'):  # swing seat: the resident moves with the facility's own pivot animation
            scene.frame_set(1); scene.view_layers[0].update()
            seat = bpy.data.objects[work['ride']]
            mw = rig.matrix_world.copy(); rig.parent = seat; rig.matrix_parent_inverse = seat.matrix_world.inverted(); rig.matrix_world = mw
    if work.get('tool'):
        t = bpy.data.objects[work['tool']].copy(); scene.collection.objects.link(t)
        attach(t, rig, 'Socket.Tool.R', rig.data.bones['Socket.Tool.R'].matrix_local); visible.append(t)
    if clip == 'Attack':
        t = bpy.data.objects['Prop_Spear'].copy(); scene.collection.objects.link(t)
        attach(t, rig, 'Socket.Tool.R', rig.data.bones['Socket.Tool.R'].matrix_local); visible.append(t)
    play(rig, clip)
    frames = CLIPS[clip]['frames']
    hinge = max((int(a.frame_range[1]) for o in fac.children_recursive if o.animation_data and o.animation_data.action
                 for a in [o.animation_data.action]), default=0)
    scene.frame_start, scene.frame_end = 1, max(frames + 1, hinge)
    # From the facility's front-right so the facility never hides the character.
    view = (1.5, -1.5, 1.25) if rig.rotation_euler.z == 0 else (2.4, .25, 1.35)  # side profile: face + facility
    if work.get('turn'):
        view = (-2.0, 1.2, 1.4)  # FURNITURE-97 facing-in: front-left, face and right (tool) arm toward the camera
    camera(scene, visible, view)
    scene.render.resolution_x = scene.render.resolution_y = 360
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    scene['facility'] = rid; scene['combo'] = combo_name; scene['clip'] = clip
    return scene


combos = list(C.COMBOS)
scenes = []
for i, rid in enumerate(POINTS):
    if POINTS[rid]['work'].get('clip'):
        scenes.append(preview_scene(rid, combos[i % len(combos)]).name)
# Same facility, appearance swapped: hand/tool placement must not change.
for c in combos:
    scenes.append(preview_scene('CC_Workshop', c, name='Swap__CC_Workshop__' + c).name)
# Basic motions on an empty stage.
for clip, combo in [('Idle', 'Combo_A'), ('Walk', 'Combo_B'), ('Run', 'Combo_C'), ('Pickup', 'Combo_D'),
                    ('Attack', 'Combo_B'), ('Hurt', 'Combo_C'), ('Death', 'Combo_A'),
                    ('Fly', 'Combo_D'), ('Downed', 'Combo_B'), ('Captive', 'Combo_C'), ('CarryPerson', 'Combo_A')]:
    sc = bpy.data.scenes.new('Motion__' + clip); sc.render.fps = 24; lights(sc)
    face = C.STATE_FACE.get(clip) or (EXTRA.get(clip) or {}).get('face')
    spec = dict(C.COMBOS[combo]); spec['parts'] = spec['parts'] + (['Equip_FlightGear'] if clip == 'Fly' else [])
    rig = C.build_combo(sc.name + '__' + combo, spec, (0, 0, 0), sc.collection, face)
    vis = [rig, *rig.children]
    if clip == 'CarryPerson':  # the rescued resident lies across the carrier's arms
        hurt = C.build_combo(sc.name + '__Rescued', C.COMBOS['Combo_D'], (0, 0, 0), sc.collection, 'Expr_PAIN')
        play(hurt, 'Downed')
        sock = rig.data.bones['Socket.Carry'].matrix_local.translation
        attach(hurt, rig, 'Socket.Carry', Matrix.Translation(sock + Vector((.7, -.05, -.32))) @ Matrix.Scale(.8, 4))  # across the arms; Downed lays the body along -X
        vis += [hurt, *hurt.children]
    if clip == 'Attack':
        t = bpy.data.objects['Prop_Spear'].copy(); sc.collection.objects.link(t)
        attach(t, rig, 'Socket.Tool.R', rig.data.bones['Socket.Tool.R'].matrix_local); vis.append(t)
    play(rig, clip); sc.frame_start, sc.frame_end = 1, CLIPS[clip]['frames'] + 1
    camera(sc, vis + [rig], (-2.3, -1.6, 1.2))
    sc.render.resolution_x = sc.render.resolution_y = 360; sc.view_settings.view_transform = 'Standard'
    sc['clip'] = clip; scenes.append(sc.name)
# Equipment showcase: worn parts + hand equipment (spec 장비 목록).
for name, combo, extra, hand, clip in [('Equip__Ranged', 'Combo_B', ['Equip_Armor', 'Trinket_SensorGoggles', 'Trinket_Anklet'], 'Equip_AcidSprayer', 'Idle'),
                                       ('Equip__Support', 'Combo_C', ['Trinket_Insignia', 'Trinket_ScentPouch'], 'Equip_Signaller', 'Idle'),
                                       ('Equip__Defense', 'Combo_D', ['Equip_Armor'], 'Prop_Shield', 'StandIdle')]:
    sc = bpy.data.scenes.new(name); sc.render.fps = 24; lights(sc)
    spec = dict(C.COMBOS[combo]); spec['parts'] = spec['parts'] + extra
    rig = C.build_combo(sc.name + '__' + combo, spec, (0, 0, 0), sc.collection)
    t = bpy.data.objects[hand].copy(); sc.collection.objects.link(t)
    sock = t.get('socket', 'Socket.Tool.R')
    attach(t, rig, sock, rig.data.bones[sock].matrix_local)
    play(rig, clip); sc.frame_start, sc.frame_end = 1, CLIPS[clip]['frames'] + 1
    camera(sc, [rig, *rig.children, t], (-1.2, -2.0, 1.0))
    sc.render.resolution_x = sc.render.resolution_y = 360; sc.view_settings.view_transform = 'Standard'
    sc['clip'] = clip; scenes.append(sc.name)
bpy.ops.wm.save_as_mainfile(filepath=str(K.KIT / 'resident-preview.blend'))
print('preview scenes', len(scenes))
