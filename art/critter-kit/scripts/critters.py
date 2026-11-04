"""Ranch critters (Unity currently uses temporary capsules): aphid, silkworm, spider, bee.

Same look as the residents: soft low-poly shapes, big dark eyes with a highlight, blush.
Each species = one Generic rig + rigid-skinned meshes + shared clip names. Front = -Y, 24 fps,
kit units = resident native size (resident ~2.24 tall). Babies are the same asset scaled ~0.6 in game.
"""
import bpy, bmesh, math, sys
from mathutils import Vector, Matrix, Euler

sys.path.insert(0, r"E:\Git\ant\art\characters\toto-v1")
import forms as F

TAU = math.tau


def mat(name, rgb, rough=.8, alpha=1.0):
    m = bpy.data.materials.get('Critter_' + name) or F.material('Critter_' + name, rgb)
    p = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    p.inputs['Roughness'].default_value = rough
    if alpha < 1:
        p.inputs['Alpha'].default_value = alpha
        m.surface_render_method = 'BLENDED' if hasattr(m, 'surface_render_method') else m.surface_render_method
    return m


INK = lambda: mat('Ink', (.08, .06, .07), .4)
SHINE = lambda: mat('Shine', (1, 1, 1), .2)
BLUSH = lambda: mat('Blush', (1, .62, .66))


class Builder:
    """Collects meshes with a bone binding, then builds rig + skin."""

    def __init__(self, name, collection):
        self.name, self.col, self.parts, self.bones = name, collection, [], {}

    def bone(self, n, head, tail, parent=None):
        self.bones[n] = (Vector(head), Vector(tail), parent)

    def add(self, obj, bone):
        for c in list(obj.users_collection): c.objects.unlink(obj)
        self.col.objects.link(obj)
        obj.parent = None
        self.parts.append((obj, bone))
        return obj

    def oval(self, n, loc, scale, m, bone, seg=20, rings=10):
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1)
        bmesh.ops.transform(bm, matrix=Matrix.Translation(loc) @ Matrix.Diagonal((*scale, 1)), verts=bm.verts)
        data = bpy.data.meshes.new(self.name + '_' + n); bm.to_mesh(data); bm.free()
        data.materials.append(m)
        o = bpy.data.objects.new(data.name, data); self.col.objects.link(o)
        return self.add(o, bone)

    def tube(self, n, pts, r, m, bone, sides=8):
        return self.add(F.tube(self.name + '_' + n, pts, r, m, None, sides), bone)

    def eye(self, n, loc, r, bone, look=Vector((0, -1, 0))):
        self.oval(n, loc, (r, r * .55, r * 1.1), INK(), bone, 16, 8)
        hl = Vector(loc) + look * r * .5 + Vector((-r * .3, 0, r * .35))
        self.oval(n + 'Shine', hl, (r * .35, r * .2, r * .35), SHINE(), bone, 10, 6)

    def build(self, location):
        arm = bpy.data.armatures.new(self.name + '_Skeleton')
        rig = bpy.data.objects.new(self.name + 'Rig', arm)
        self.col.objects.link(rig)
        vl = bpy.context.scene.view_layers[0]; vl.update()
        vl.objects.active = rig
        with bpy.context.temp_override(view_layer=vl, active_object=rig, object=rig):
            bpy.ops.object.mode_set(mode='EDIT')
            for n, (h, t, p) in self.bones.items():
                b = arm.edit_bones.new(n); b.head, b.tail = h, t
                if p: b.parent = arm.edit_bones[p]
            bpy.ops.object.mode_set(mode='OBJECT')
        for pb in rig.pose.bones: pb.rotation_mode = 'XYZ'
        for o, bone in self.parts:
            o.data.transform(o.matrix_world); o.matrix_world = Matrix.Identity(4)
            g = o.vertex_groups.new(name=bone); g.add(range(len(o.data.vertices)), 1, 'REPLACE')
            o.parent = rig
            m = o.modifiers.new('Critter deform', 'ARMATURE'); m.object = rig
            for p in o.data.polygons: p.use_smooth = True
        rig.location = location
        rig['forward'] = '-Y'; rig['rig_type'] = 'Generic'; rig['fps'] = 24
        arm.display_type = 'STICK'
        return rig


# ---------------- species ----------------

def legs(b, count, side_x, y0, y1, z, foot_z, reach, m, r=.035, knee=.0):
    """Insect legs: one bone each (hip), rigid two-segment tube."""
    names = []
    for i in range(count):
        y = y0 + (y1 - y0) * i / max(1, count - 1)
        for s, sx in (('L', -1), ('R', 1)):
            n = f'Leg{i + 1}.{s}'
            hip = Vector((sx * side_x, y, z)); kneep = Vector((sx * (side_x + reach * .6), y, z + .08 + knee)); foot = Vector((sx * (side_x + reach), y + (y - (y0 + y1) / 2) * .25, foot_z))
            b.tube(n, [hip, kneep, foot], r, m, n, 6)
            b.bone(n, hip, kneep, 'Body')
            names.append(n)
    return names


def aphid(col, loc):
    b = Builder('Aphid', col)
    body = mat('AphidGreen', (.55, .82, .36)); belly = mat('AphidBelly', (.78, .93, .55)); leg = mat('AphidLeg', (.36, .55, .22))
    b.bone('Root', (0, 0, 0), (0, 0, .15)); b.bone('Body', (0, .05, .3), (0, .05, .75), 'Root'); b.bone('Head', (0, -.38, .5), (0, -.38, .85), 'Body')
    b.oval('Body', (0, .08, .48), (.46, .58, .40), body, 'Body', 24, 12)
    b.oval('Belly', (0, .02, .30), (.38, .48, .22), belly, 'Body')
    b.oval('Head', (0, -.42, .58), (.30, .26, .27), body, 'Head')
    for s, sx in (('L', -1), ('R', 1)):
        b.eye('Eye' + s, (sx * .13, -.64, .64), .075, 'Head')
        b.oval('Blush' + s, (sx * .21, -.6, .53), (.05, .015, .03), BLUSH(), 'Head', 10, 6)
        n = 'Antenna.' + s
        b.tube(n, [(sx * .1, -.52, .80), (sx * .2, -.62, 1.02), (sx * .32, -.58, 1.12)], .018, leg, n, 6)
        b.bone(n, (sx * .1, -.52, .80), (sx * .2, -.62, 1.02), 'Head')
        b.tube('Cornicle' + s, [(sx * .16, .52, .66), (sx * .2, .64, .80)], .03, leg, 'Body', 6)
    legs(b, 3, .3, -.22, .30, .26, .0, .2, leg, .05)
    return b.build(loc)


def silkworm(col, loc):
    b = Builder('Silkworm', col)
    cream = mat('SilkCream', (.96, .94, .86)); band = mat('SilkBand', (.86, .84, .76)); spot = mat('SilkSpot', (.62, .55, .55))
    b.bone('Root', (0, 0, 0), (0, 0, .15))
    prev = 'Root'
    segs = [(-.42, .30, .30), (-.12, .29, .32), (.18, .27, .30), (.46, .24, .26), (.70, .19, .2)]
    for i, (y, r, h) in enumerate(segs):
        n = f'Seg{i}'
        b.bone(n, (0, y, h * .5), (0, y, h * .5 + .3), prev if i else 'Root'); prev = n
        b.oval(n, (0, y, h), (r, .2, h), cream if i % 2 == 0 else band, n, 20, 10)
        if 0 < i < 4:
            for s, sx in (('L', -1), ('R', 1)):
                b.oval(f'Proleg{i}{s}', (sx * r * .7, y, .05), (.06, .06, .05), band, n, 8, 6)
    b.bone('Head', (0, -.62, .30), (0, -.62, .62), 'Seg0')
    b.oval('Head', (0, -.66, .36), (.24, .2, .25), cream, 'Head')
    b.oval('Tailhorn', (0, .86, .34), (.03, .06, .08), spot, 'Seg4', 8, 6)
    for s, sx in (('L', -1), ('R', 1)):
        b.eye('Eye' + s, (sx * .1, -.84, .42), .06, 'Head')
        b.oval('Blush' + s, (sx * .17, -.8, .32), (.045, .015, .028), BLUSH(), 'Head', 10, 6)
        b.oval('Spot' + s, (sx * .2, -.12, .52), (.07, .05, .05), spot, 'Seg1', 10, 6)
    return b.build(loc)


def spider(col, loc):
    b = Builder('Spider', col)
    dark = mat('SpiderDark', (.26, .2, .28)); light = mat('SpiderLight', (.55, .45, .58)); fang = mat('SpiderFang', (.9, .85, .8))
    b.bone('Root', (0, 0, 0), (0, 0, .15)); b.bone('Body', (0, -.15, .35), (0, -.15, .7), 'Root')
    b.bone('Abdomen', (0, .12, .55), (0, .7, .7), 'Body'); b.bone('Head', (0, -.35, .5), (0, -.35, .85), 'Body')
    b.oval('Abdomen', (0, .48, .62), (.46, .5, .42), dark, 'Abdomen', 24, 12)
    b.oval('AbdomenMark', (0, .55, 1.0), (.18, .2, .06), light, 'Abdomen', 12, 6)
    b.oval('Cephalo', (0, -.22, .52), (.32, .3, .27), dark, 'Body')
    b.oval('Face', (0, -.44, .56), (.24, .14, .2), light, 'Head')
    for s, sx in (('L', -1), ('R', 1)):
        b.eye('Eye' + s, (sx * .1, -.57, .62), .07, 'Head')
        b.eye('EyeSmall' + s, (sx * .2, -.52, .72), .035, 'Head')
        b.oval('Blush' + s, (sx * .18, -.56, .5), (.045, .015, .028), BLUSH(), 'Head', 10, 6)
        b.oval('Fang' + s, (sx * .05, -.58, .42), (.025, .025, .05), fang, 'Head', 8, 6)
    legs(b, 4, .26, -.42, .06, .48, .0, .5, dark, .055, .24)
    return b.build(loc)


def bee(col, loc):
    b = Builder('Bee', col)
    yellow = mat('BeeYellow', (.98, .78, .22)); black = mat('BeeBlack', (.16, .12, .1)); fuzz = mat('BeeFuzz', (1, .92, .7))
    wing = mat('BeeWing', (.85, .95, 1), .2, .55)
    b.bone('Root', (0, 0, 0), (0, 0, .15)); b.bone('Body', (0, 0, .45), (0, 0, .8), 'Root')
    b.bone('Abdomen', (0, .15, .62), (0, .75, .55), 'Body'); b.bone('Head', (0, -.4, .7), (0, -.4, 1.0), 'Body')
    b.oval('Thorax', (0, -.08, .68), (.3, .3, .3), fuzz, 'Body')
    b.oval('Abdomen', (0, .42, .62), (.36, .44, .34), yellow, 'Abdomen', 24, 12)
    for i, y in enumerate((.3, .52)):
        b.oval(f'Stripe{i}', (0, y, .62), (.37, .07, .35), black, 'Abdomen', 24, 8)
    b.oval('Stinger', (0, .88, .6), (.04, .08, .04), black, 'Abdomen', 8, 6)
    b.oval('Head', (0, -.44, .78), (.27, .24, .25), yellow, 'Head')
    for s, sx in (('L', -1), ('R', 1)):
        b.eye('Eye' + s, (sx * .12, -.64, .82), .075, 'Head')
        b.oval('Blush' + s, (sx * .2, -.6, .7), (.045, .015, .028), BLUSH(), 'Head', 10, 6)
        n = 'Antenna.' + s
        b.tube(n, [(sx * .08, -.55, .98), (sx * .16, -.62, 1.18), (sx * .26, -.6, 1.25)], .018, black, n, 6)
        b.bone(n, (sx * .08, -.55, .98), (sx * .16, -.62, 1.18), 'Head')
        n = 'Wing.' + s
        b.oval(n, (sx * .34, .05, 1.02), (.3, .14, .03), wing, n, 16, 6)
        b.bone(n, (sx * .1, .02, .95), (sx * .62, .06, 1.05), 'Body')
    legs(b, 3, .2, -.2, .1, .42, .12, .16, black, .04)
    return b.build(loc)


def wasp(col, loc):
    """Raid event enemy (EventActorKind.Wasp): slimmer bee, orange stripes, cross brows, big stinger."""
    b = Builder('Wasp', col)
    orange = mat('WaspOrange', (.98, .55, .12)); black = mat('WaspBlack', (.13, .1, .1))
    wing = mat('WaspWing', (.85, .9, 1), .2, .5)
    b.bone('Root', (0, 0, 0), (0, 0, .15)); b.bone('Body', (0, 0, .5), (0, 0, .85), 'Root')
    b.bone('Abdomen', (0, .15, .62), (0, .8, .5), 'Body'); b.bone('Head', (0, -.38, .72), (0, -.38, 1.0), 'Body')
    b.oval('Thorax', (0, -.08, .68), (.24, .26, .24), black, 'Body')
    b.oval('Waist', (0, .16, .64), (.08, .1, .08), black, 'Abdomen', 10, 6)
    b.oval('Abdomen', (0, .5, .58), (.27, .4, .26), orange, 'Abdomen', 24, 12)
    for i, y in enumerate((.36, .56, .74)):
        b.oval(f'Stripe{i}', (0, y, .58), (.28 - i * .04, .05, .27 - i * .04), black, 'Abdomen', 24, 8)
    b.oval('Stinger', (0, .95, .52), (.035, .12, .035), black, 'Abdomen', 8, 6)
    b.oval('Head', (0, -.42, .76), (.25, .22, .23), orange, 'Head')
    for s, sx in (('L', -1), ('R', 1)):
        b.eye('Eye' + s, (sx * .11, -.6, .8), .07, 'Head')
        b.tube('Brow' + s, [(sx * .19, -.6, .92), (sx * .05, -.62, .88)], .015, black, 'Head', 6)
        n = 'Antenna.' + s
        b.tube(n, [(sx * .08, -.52, .95), (sx * .18, -.6, 1.16), (sx * .3, -.56, 1.2)], .018, black, n, 6)
        b.bone(n, (sx * .08, -.52, .95), (sx * .18, -.6, 1.16), 'Head')
        n = 'Wing.' + s
        b.oval(n, (sx * .36, .08, 1.0), (.34, .12, .03), wing, n, 16, 6)
        b.bone(n, (sx * .1, .02, .93), (sx * .66, .08, 1.03), 'Body')
    legs(b, 3, .18, -.18, .08, .45, .14, .15, black, .035)
    return b.build(loc)


def night_beetle(col, loc):
    """Night raid predator (EventActorKind.NightPredator): dark shell, glowing red eyes (Unity adds a light)."""
    b = Builder('NightBeetle', col)
    shell = mat('BeetleShell', (.12, .1, .18), .35); under = mat('BeetleUnder', (.22, .18, .28))
    glow = bpy.data.materials.get('Critter_BeetleEye') or mat('BeetleEye', (1, .2, .12), .3)
    p = next(n for n in glow.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    p.inputs['Emission Color'].default_value = (1, .15, .08, 1); p.inputs['Emission Strength'].default_value = 4
    b.bone('Root', (0, 0, 0), (0, 0, .15)); b.bone('Body', (0, .05, .3), (0, .05, .7), 'Root'); b.bone('Head', (0, -.5, .4), (0, -.5, .7), 'Body')
    b.oval('Shell', (0, .1, .48), (.5, .62, .34), shell, 'Body', 24, 12)
    b.oval('Under', (0, .1, .26), (.44, .56, .14), under, 'Body')
    b.oval('Head', (0, -.56, .44), (.26, .2, .2), shell, 'Head')
    for s, sx in (('L', -1), ('R', 1)):
        b.oval('Eye' + s, (sx * .11, -.72, .5), (.06, .03, .045), glow, 'Head', 12, 6)
        b.tube('Jaw' + s, [(sx * .1, -.7, .36), (sx * .14, -.86, .36), (sx * .05, -.94, .37)], .03, under, 'Head', 6)
    legs(b, 3, .38, -.25, .35, .3, .0, .3, under, .045)
    return b.build(loc)


SPECIES = {'Aphid': aphid, 'Silkworm': silkworm, 'Spider': spider, 'Bee': bee, 'Wasp': wasp, 'NightBeetle': night_beetle}

# ---------------- clips ----------------
CLIPS = {'Idle': (48, True), 'Walk': (24, True), 'Eat': (32, True), 'Lie': (48, True), 'Bound': (32, True),
         'Hurt': (16, False), 'Death': (32, False), 'Attack': (24, False), 'Fly': (16, True)}
FOR = {'Aphid': ['Idle', 'Walk', 'Eat', 'Lie', 'Bound', 'Hurt', 'Death'],
       'Silkworm': ['Idle', 'Walk', 'Eat', 'Lie', 'Bound', 'Hurt', 'Death'],
       'Spider': ['Idle', 'Walk', 'Eat', 'Lie', 'Bound', 'Hurt', 'Death', 'Attack'],
       'Bee': ['Idle', 'Walk', 'Eat', 'Lie', 'Bound', 'Hurt', 'Death', 'Attack', 'Fly'],
       'Wasp': ['Idle', 'Walk', 'Hurt', 'Death', 'Attack', 'Fly'],
       'NightBeetle': ['Idle', 'Walk', 'Lie', 'Hurt', 'Death', 'Attack']}


def pose(rig, clip, t):
    """World-axis rotations (radians) / locations per bone at phase t."""
    s = math.sin(TAU * t); c = math.cos(TAU * t); d = {}
    def rot(n, x=0, y=0, z=0):
        if n in rig.pose.bones: d[n] = (x, y, z)
    def loc(n, x=0, y=0, z=0):
        if n in rig.pose.bones: d[n + '_loc'] = (x, y, z)
    legs_ = [b.name for b in rig.pose.bones if b.name.startswith('Leg')]
    segs = [b.name for b in rig.pose.bones if b.name.startswith('Seg')]
    rot('Antenna.L', .12 * s); rot('Antenna.R', -.12 * s)
    if 'Wing.L' in rig.pose.bones and clip not in ('Lie', 'Death', 'Bound'):
        f = 6 if clip == 'Fly' else 1
        rot('Wing.L', 0, .5 * math.sin(TAU * t * f) if clip == 'Fly' else .08 * s); rot('Wing.R', 0, -.5 * math.sin(TAU * t * f) if clip == 'Fly' else -.08 * s)
    if clip == 'Idle':
        loc('Body', z=.015 * s); rot('Head', .05 * s)
        for i, sg in enumerate(segs): loc(sg, z=.012 * math.sin(TAU * t + i * .7))
    elif clip == 'Walk':
        loc('Body', z=.02 * abs(math.sin(2 * TAU * t)))
        for n in legs_:
            idx = int(n[3]); side = 1 if n.endswith('.L') else -1
            ph = (idx + (0 if side > 0 else 1)) % 2   # alternating tripod gait
            rot(n, 0, 0, .35 * math.sin(TAU * t + ph * math.pi) * side)
            loc(n, z=.04 * max(0, math.sin(TAU * t + ph * math.pi)))
        for i, sg in enumerate(segs):   # silkworm inchworm wave
            loc(sg, y=-.05 * math.sin(TAU * t - i * .9), z=.06 * max(0, math.sin(TAU * t - i * .9)))
        rot('Head', .04 * s)
    elif clip == 'Eat':
        a = (1 - c) / 2
        rot('Head', .35 * a); loc('Head', z=-.05 * a); rot('Body', .06 * a)
        if segs: rot(segs[0], .2 * a)
    elif clip == 'Lie':
        loc('Root', z=-.08); rot('Head', .12 + .03 * s)
        for n in legs_: rot(n, 0, .45 if n.endswith('.L') else -.45, 0)
        loc('Body', z=.01 * s)
        if 'Wing.L' in rig.pose.bones: rot('Wing.L', 0, -.25); rot('Wing.R', 0, .25)
    elif clip == 'Bound':
        for n in legs_: rot(n, 0, -.5 if n.endswith('.L') else .5, 0)
        rot('Body', 0, .12 * s, 0); rot('Head', .1 * c)
        for i, sg in enumerate(segs): rot(sg, 0, .08 * math.sin(TAU * t + i), 0)
    elif clip == 'Hurt':
        a = math.sin(math.pi * t); rot('Body', -.25 * a); loc('Body', y=.08 * a); rot('Head', -.2 * a)
    elif clip == 'Death':
        a = min(1, t * 1.6); a = a * a * (3 - 2 * a)
        rot('Root', 0, math.pi * a, 0); loc('Root', z=.7 * a)
        for n in legs_: rot(n, .3 * a * math.sin(TAU * t * 3) * (1 - a), 0, .2 * a)
    elif clip == 'Attack':
        a = math.sin(math.pi * min(1, t * 2)) if t < .5 else -.25 * math.sin(math.pi * (t - .5) * 2)
        loc('Body', y=-.18 * a); rot('Body', -.12 * a); rot('Head', -.15 * a)
        rot('Abdomen', .5 * a)
        for n in legs_[:2]: rot(n, -.5 * a)
    elif clip == 'Fly':
        loc('Root', z=.55 + .05 * s); rot('Body', .1)
        for n in legs_: rot(n, -.35, 0, 0)
    return d


def build_actions(rig, species):
    rig.animation_data_create(); acts = {}
    for clip in FOR[species]:
        frames, loop = CLIPS[clip]
        act = bpy.data.actions.new(f'{species}|{clip}'); act.use_fake_user = True
        rig.animation_data.action = act
        for f in range(1, frames + 2):
            v = pose(rig, clip, (f - 1) / frames)
            for pb in rig.pose.bones:
                basis = pb.bone.matrix_local.to_3x3()
                pb.rotation_euler = (basis.inverted() @ Euler(v.get(pb.name, (0, 0, 0))).to_matrix() @ basis).to_euler()
                pb.location = basis.inverted() @ Vector(v.get(pb.name + '_loc', (0, 0, 0)))
                pb.keyframe_insert('rotation_euler', frame=f, group=pb.name); pb.keyframe_insert('location', frame=f, group=pb.name)
        for layer in act.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for fc in bag.fcurves:
                        for kp in fc.keyframe_points: kp.interpolation = 'LINEAR'
                        if loop: fc.modifiers.new('CYCLES')
        act['loop'] = loop; act['frames'] = frames; acts[clip] = act
    rig.animation_data.action = acts['Idle']; rig.animation_data.action_slot = acts['Idle'].slots[0]
    return acts
