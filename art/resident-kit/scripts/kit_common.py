"""Resident kit shared definitions: paths, common rig, sockets, skin weights.

Kit space = character native size (Blender units). Front = local -Y, up = +Z, 24 fps.
"""
import bpy, math
from pathlib import Path
from mathutils import Vector, Matrix

KIT = Path(r"E:\Git\ant\art\resident-kit")
SRC_RESIDENTS = r"E:\Git\ant\art\characters\residents-24\residents-24.blend"
SRC_PARTS = r"E:\Git\ant\art\characters\parts-board\appearance-parts.blend"
SRC_ANIM = r"E:\Git\ant\art\resident-anim\resident-interactions.blend"
SRC_TOOLS = r"E:\Git\ant\art\ant-anim\ant-tools.blend"
SRC_FACILITIES = r"E:\Git\ant\art\colony-core\colony-interaction.blend"
POINTS_JSON = Path(r"E:\Git\ant\art\colony-core\interaction_points.json")

# Same deform bones and rest pose as art/resident-anim/rig.py, so its 30 actions play unchanged.
BONES = {
    'Root': ((0, 0, 0), (0, 0, .20), None),
    'Body': ((0, 0, .30), (0, 0, .78), 'Root'),
    'Head': ((0, 0, .80), (0, 0, 1.70), 'Body'),
    'Arm.L': ((-.30, -.12, .72), (-.43, -.42, .65), 'Body'),
    'Arm.R': ((.30, -.12, .72), (.43, -.42, .65), 'Body'),
    'Hand.L': ((-.43, -.42, .65), (-.43, -.42, .48), 'Arm.L'),
    'Hand.R': ((.43, -.42, .65), (.43, -.42, .48), 'Arm.R'),
    'Foot.L': ((-.27, 0, .26), (-.27, -.13, .10), 'Root'),
    'Foot.R': ((.27, 0, .26), (.27, -.13, .10), 'Root'),
    'Ear.L': ((-.50, 0, 1.95), (-.78, 0, 1.56), 'Head'),
    'Ear.R': ((.50, 0, 1.95), (.78, 0, 1.56), 'Head'),
    'Tail': ((0, .32, .30), (0, .54, .36), 'Body'),
}
# Non-deforming attach bones. Head = attach point, bone +Y = tool handle direction toward the tool head.
SOCKETS = {
    'Socket.Tool.R': ((.50, -.56, .62), (.50, -.66, .86), 'Hand.R'),
    'Socket.Tool.L': ((-.50, -.56, .62), (-.50, -.66, .86), 'Hand.L'),
    'Socket.Carry': ((0, -.78, .62), (0, -.78, .86), 'Body'),
    'Socket.Hat': ((0, 0, 2.07), (0, 0, 2.30), 'Head'),
}
DEFORM = list(BONES)


def make_rig(name, collection):
    arm = bpy.data.armatures.new(name + '_Skeleton')
    rig = bpy.data.objects.new(name, arm)
    collection.objects.link(rig)
    vl = bpy.context.scene.view_layers[0]
    vl.update()
    vl.objects.active = rig
    rig.select_set(True, view_layer=vl)
    with bpy.context.temp_override(view_layer=vl, active_object=rig, object=rig):
        bpy.ops.object.mode_set(mode='EDIT')
        build_bones(arm)
        bpy.ops.object.mode_set(mode='OBJECT')
    for pb in rig.pose.bones:
        pb.rotation_mode = 'XYZ'
    rig['forward'] = '-Y'
    rig['rig_type'] = 'Generic'
    rig['fps'] = 24
    arm.display_type = 'STICK'
    return rig


def build_bones(arm):
    for table, deform in ((BONES, True), (SOCKETS, False)):
        for n, (h, t, parent) in table.items():
            b = arm.edit_bones.new(n)
            b.head, b.tail, b.use_deform = h, t, deform
            if parent:
                b.parent = arm.edit_bones[parent]


def body_blend(z):
    """Body→Head weight across the neck, identical to resident-anim/rig.py."""
    t = max(0, min(1, (z - .66) / .32))
    return t * t * (3 - 2 * t)


def skin(obj, rig, bind):
    """bind: 'surface' (follows body/head blend), a bone name, or 'side:<Base>' (L/R by vertex x)."""
    for g in list(obj.vertex_groups):
        obj.vertex_groups.remove(g)
    groups = {n: obj.vertex_groups.new(name=n) for n in DEFORM}
    for v in obj.data.vertices:
        co = v.co
        if bind == 'surface':
            t = body_blend(co.z)
            if t < 1: groups['Body'].add([v.index], 1 - t, 'REPLACE')
            if t > 0: groups['Head'].add([v.index], t, 'REPLACE')
        elif bind.startswith('side:'):
            base = bind[5:]
            groups[base + ('.L' if co.x < 0 else '.R')].add([v.index], 1, 'REPLACE')
        else:
            groups[bind].add([v.index], 1, 'REPLACE')
    for m in [m for m in obj.modifiers if m.type == 'ARMATURE']:
        obj.modifiers.remove(m)
    mod = obj.modifiers.new('Resident deform', 'ARMATURE')
    mod.object = rig
    obj.parent = rig
    obj.matrix_parent_inverse = Matrix.Identity(4)


def collection(name, parent=None):
    c = bpy.data.collections.get(name) or bpy.data.collections.new(name)
    if c.name not in (parent or bpy.context.scene.collection).children:
        (parent or bpy.context.scene.collection).children.link(c)
    return c


def empty_file():
    bpy.ops.wm.read_homefile(use_empty=True)
    s = bpy.context.scene
    s.render.fps = 24
    return s


def join(objs):
    """Join meshes into objs[0] (keeps vertex groups and material slots)."""
    if len(objs) == 1:
        return objs[0]
    vl = bpy.context.scene.view_layers[0]
    with bpy.context.temp_override(view_layer=vl, active_object=objs[0], object=objs[0],
                                   selected_objects=objs, selected_editable_objects=objs):
        bpy.ops.object.join()
    return objs[0]
