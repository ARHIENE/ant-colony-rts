"""Add spec-required resident clips missing from the original 30 (in resident-animations.blend + FBX).

Fly: flight gear (armor slot) movement. Downed: knocked out but alive (Expr_PAIN). Captive: prisoner seated,
arms behind back. CarryPerson: rescue carrying a downed resident (target rides Socket.Carry).
Same authoring convention as art/resident-anim/clips.py: world-axis rotations per bone, linear keys.
"""
import bpy, sys, json, math, importlib
from mathutils import Vector, Euler
sys.path.insert(0, r"E:\Git\ant\art\resident-kit\scripts")
import kit_common as K
importlib.reload(K)

TAU = math.tau
EXTRA = {'Fly': (16, True), 'Downed': (48, True), 'Captive': (48, True), 'CarryPerson': (24, True),
         # FURNITURE-97: furniture that no existing clip fits (play, bathe, sport, music, crafts)
         'PlayBoard': (48, True), 'Bathe': (48, True), 'Wrestle': (32, True), 'Throw': (24, True), 'PlayMusic': (32, True),
         'Exercise': (48, True), 'Swing': (48, True), 'RockCradle': (48, True), 'Spin': (32, True), 'WaterPlant': (48, True)}
FACE = {'Downed': 'Expr_PAIN', 'Captive': 'Expr_SAD', 'Fly': None, 'CarryPerson': None,
        'PlayBoard': None, 'Bathe': 'Expr_SLEEPY', 'Wrestle': None, 'Throw': None, 'PlayMusic': None,
        'Exercise': None, 'Swing': None, 'RockCradle': None, 'Spin': None, 'WaterPlant': None}


def keys(t, pts):
    """Piecewise-linear value through (t, v) points over one cycle."""
    for (t0, v0), (t1, v1) in zip(pts, pts[1:]):
        if t <= t1: return v0 + (v1 - v0) * (t - t0) / (t1 - t0)
    return pts[-1][1]


def pose(name, t):
    d = {}; s = math.sin(TAU * t); c = math.cos(TAU * t)
    def rot(n, x=0, y=0, z=0): d[n] = (x, y, z)
    def loc(n, x=0, y=0, z=0): d[n + '_loc'] = (x, y, z)
    if name == 'Fly':
        loc('Root', z=.55 + .06 * s); rot('Body', .16); rot('Head', -.08)
        rot('Foot.L', .45 + .12 * s); rot('Foot.R', .45 - .12 * s)
        rot('Arm.L', 0, .35 + .12 * s, 0); rot('Arm.R', 0, -.35 - .12 * s, 0)
        rot('Ear.L', 0, 0, .18 * s); rot('Ear.R', 0, 0, -.18 * s); rot('Tail', .25 * s)
    elif name == 'Downed':
        rot('Root', 0, -math.pi / 2, 0); loc('Root', z=.65)
        rot('Head', .12 + .02 * s); loc('Body', z=.012 * s); rot('Arm.L', .2); rot('Arm.R', .35); rot('Foot.L', -.2)
    elif name == 'Captive':
        loc('Body', z=-.10); rot('Foot.L', -.35); rot('Foot.R', -.35)
        rot('Arm.L', -.55, 0, .25); rot('Arm.R', -.55, 0, -.25)
        rot('Head', .22 + .04 * s, 0, .06 * math.sin(TAU * t * .5)); rot('Body', .06 + .02 * s)
    elif name == 'CarryPerson':
        rot('Foot.L', .26 * s); rot('Foot.R', -.26 * s)
        loc('Foot.L', z=max(0, s) * .05); loc('Foot.R', z=max(0, -s) * .05)
        loc('Body', z=.02 * (1 - math.cos(2 * TAU * t))); rot('Body', .14, 0, .03 * s)
        loc('Hand.L', y=.18, z=.16); loc('Hand.R', y=.18, z=.16); rot('Head', -.05)
    elif name in ('PlayBoard', 'Bathe', 'Swing', 'Spin'):   # seated family (same base as Sit)
        loc('Body', z=-.10); rot('Foot.L', -.35); rot('Foot.R', -.35)
        if name == 'PlayBoard':
            a = max(0, math.sin(TAU * t)) ** 2
            rot('Body', .12); rot('Head', .12, 0, .05 * s); loc('Hand.L', y=.10, z=.10)
            loc('Hand.R', y=.15 + .15 * a, z=.12 + .06 * a); rot('Arm.R', -.3 * a)
        elif name == 'Bathe':
            p = (1 - c) / 2
            loc('Body', z=-.12); rot('Body', -.08); rot('Head', -.06, 0, .04 * s)
            rot('Arm.L', -.25 * p); rot('Arm.R', -.25 * (1 - p))
        elif name == 'Swing':
            rot('Foot.L', -.35 - .35 * s); rot('Foot.R', -.35 - .35 * s); rot('Body', -.12 * s); rot('Head', -.05 * s)
            rot('Arm.L', -.9); rot('Arm.R', -.9); loc('Hand.L', z=.25); loc('Hand.R', z=.25)
        else:  # Spin: crank the wheel, feed yarn, tap the pedal
            rot('Body', .10); loc('Hand.R', y=.2 + .08 * c, z=.18 + .08 * s); loc('Hand.L', y=.15, z=.12 + .03 * s)
            rot('Foot.R', -.35 + .15 * s)
    elif name == 'Wrestle':
        loc('Body', z=-.12); rot('Body', .32 + .05 * s); rot('Head', -.15)
        loc('Hand.L', y=.28 + .05 * s, z=.18); loc('Hand.R', y=.28 + .05 * s, z=.18)
        rot('Foot.L', -.30); rot('Foot.R', .25)
    elif name == 'Throw':
        rot('Arm.R', keys(t, [(0, 0), (.4, -1.3), (.55, .35), (1, 0)]))
        rot('Body', .05, 0, keys(t, [(0, 0), (.4, .25), (.55, -.15), (1, 0)])); rot('Arm.L', -.3)
    elif name == 'PlayMusic':
        loc('Hand.L', y=.20, z=.15 + .12 * max(0, s)); loc('Hand.R', y=.20, z=.15 + .12 * max(0, -s))
        rot('Arm.L', -.3 * max(0, s)); rot('Arm.R', -.3 * max(0, -s)); rot('Head', .06 * abs(s)); loc('Body', z=.02 * abs(s))
    elif name == 'Exercise':
        a = (1 - c) / 2   # 0 = arms up, 1 = squat with the bar at the shoulders
        rot('Arm.L', -1.8 + 1.1 * a); rot('Arm.R', -1.8 + 1.1 * a); loc('Body', z=-.16 * a); rot('Foot.L', -.15 * a); rot('Foot.R', -.15 * a)
    elif name == 'RockCradle':
        rot('Body', .25, 0, .05 * s); rot('Head', .2); loc('Hand.L', x=.1 * s, y=.3, z=.05); loc('Hand.R', x=.1 * s, y=.3, z=.05)
    elif name == 'WaterPlant':
        rot('Body', .12); rot('Head', .15); rot('Arm.R', -.5); rot('Hand.R', 1.2 + .15 * s); loc('Hand.R', x=.28, y=.50, z=.10)  # net +0.7 tips the upright can forward to pour
    return d


def build(rig):
    acts = {}
    for name, (frames, loop) in EXTRA.items():
        act = bpy.data.actions.get('Resident|' + name)
        if act: bpy.data.actions.remove(act)
        act = bpy.data.actions.new('Resident|' + name); act.use_fake_user = True
        rig.animation_data.action = act
        for f in range(1, frames + 2):
            v = pose(name, (f - 1) / frames)
            for pb in rig.pose.bones:
                if pb.name.startswith('Socket'): continue
                basis = pb.bone.matrix_local.to_3x3()
                pb.rotation_euler = (basis.inverted() @ Euler(v.get(pb.name, (0, 0, 0))).to_matrix() @ basis).to_euler()
                move = Vector(v.get(pb.name + '_loc', (0, 0, 0)))
                if pb.name.startswith('Hand'): move.y = -move.y   # same convention as clips.py
                pb.location = basis.inverted() @ move
                pb.keyframe_insert('rotation_euler', frame=f, group=pb.name); pb.keyframe_insert('location', frame=f, group=pb.name)
        for layer in act.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for fc in bag.fcurves:
                        for kp in fc.keyframe_points: kp.interpolation = 'LINEAR'
                        if loop: fc.modifiers.new('CYCLES')
        act['loop'] = loop; act['frames'] = frames; act['face'] = FACE[name] or ''
        acts[name] = act
    return acts


bpy.ops.wm.open_mainfile(filepath=str(K.KIT / 'resident-animations.blend'))
scene = bpy.context.scene
rig = bpy.data.objects['ResidentRig']
idle = rig.animation_data.action
acts = build(rig)
rig.animation_data.action = idle; rig.animation_data.action_slot = idle.slots[0]
bpy.ops.wm.save_mainfile()
out = K.KIT / 'exports' / 'anim'
vl = scene.view_layers[0]
for name, act in acts.items():
    rig.animation_data.action = act; rig.animation_data.action_slot = act.slots[0]
    scene.frame_start, scene.frame_end = 1, EXTRA[name][0] + 1
    for o in scene.objects: o.select_set(o == rig, view_layer=vl)
    with bpy.context.temp_override(view_layer=vl, active_object=rig, selected_objects=[rig]):
        bpy.ops.export_scene.fbx(filepath=str(out / f'Resident@{name}.fbx'), use_selection=True, object_types={'ARMATURE'},
                                 add_leaf_bones=False, use_armature_deform_only=False, bake_anim=True,
                                 bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0,
                                 axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS')
rig.animation_data.action = idle; rig.animation_data.action_slot = idle.slots[0]
(K.KIT / 'clips-extra.json').write_text(json.dumps({n: {'frames': f, 'loop': l, 'fps': 24, 'face': FACE[n]} for n, (f, l) in EXTRA.items()}, indent=1), encoding='utf-8')
print('extra clips', list(acts))
