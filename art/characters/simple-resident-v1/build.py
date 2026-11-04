import bpy
import math
from pathlib import Path
from mathutils import Vector

OUT = Path('E:/Git/ant/art/characters/simple-resident-v1')
OUT.mkdir(parents=True, exist_ok=True)
scene = bpy.data.scenes.new('SimpleResident_V1')
bpy.context.window.scene = scene

def material(name, color):
    m = bpy.data.materials.new('Simple_' + name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = .82
    return m

cream = material('Cream', (.88, .81, .65))
ink = material('Ink', (.075, .045, .035))
pink = material('Blush', (.94, .47, .38))
mint = material('Ear', (.39, .58, .40))
floor_mat = material('Floor', (.72, .79, .73))
parts = []

def ellipsoid(name, loc, scale, mat, bone='Body', segments=24, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    o = bpy.context.object
    o.name = 'Simple_' + name
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(mat)
    for p in o.data.polygons:
        p.use_smooth = True
    parts.append((o, bone))
    return o

# A continuous oval silhouette, without a separate neck or head seam.
body = ellipsoid('Body', (0, 0, 1.01), (.68, .49, .82), cream)
for v in body.data.vertices:
    z = v.co.z / .82
    v.co.x *= 1 - .08 * z

for side, x in [('L', -.27), ('R', .27)]:
    ear = ellipsoid('Ear_' + side, (x, .015, 1.86), (.115, .105, .255), cream)
    ear.rotation_euler.y = -.32 if x < 0 else .32
    inset = ellipsoid('EarInner_' + side, (x, -.085, 1.90), (.052, .018, .135), mint)
    inset.rotation_euler.y = ear.rotation_euler.y
    ellipsoid('Foot_' + side, (x, -.075, .16), (.175, .235, .16), cream, 'Foot.' + side)
    hand = ellipsoid('Hand_' + side, ((-.64 if x < 0 else .64), -.035, .67), (.145, .145, .23), cream, 'Hand.' + side)
    hand.rotation_euler.y = -.25 if x < 0 else .25

for x in [-.235, .235]:
    ellipsoid('Eye', (x, -.459, 1.16), (.037, .024, .058), ink, segments=16, rings=12)
    ellipsoid('Cheek', (x * 1.56, -.407, .995), (.085, .018, .044), pink, segments=20, rings=12)

# A tiny, shallow smile; no muzzle, nose ball or protruding mouth.
curve = bpy.data.curves.new('Simple_Smile', 'CURVE')
curve.dimensions = '3D'
curve.bevel_depth = .009
curve.bevel_resolution = 2
sp = curve.splines.new('BEZIER')
sp.bezier_points.add(4)
for p, co in zip(sp.bezier_points, [(-.057,-.495,1.025),(-.029,-.498,1.006),(0,-.499,1.019),(.029,-.498,1.006),(.057,-.495,1.025)]):
    p.co = co
    p.handle_left_type = 'AUTO'
    p.handle_right_type = 'AUTO'
mouth = bpy.data.objects.new('Simple_Smile', curve)
scene.collection.objects.link(mouth)
curve.materials.append(ink)

bpy.ops.object.armature_add(location=(0, 0, 0))
rig = bpy.context.object
rig.name = 'SimpleResident_Rig'
bpy.ops.object.mode_set(mode='EDIT')
eb = rig.data.edit_bones
root = eb[0]
root.name = 'Body'
root.head, root.tail = (0,0,.3), (0,0,1.2)
for name, head, tail in [('Hand.L',(-.58,0,.82),(-.68,0,.48)),('Hand.R',(.58,0,.82),(.68,0,.48)),('Foot.L',(-.27,0,.2),(-.27,-.2,.2)),('Foot.R',(.27,0,.2),(.27,-.2,.2))]:
    b = eb.new(name)
    b.head, b.tail, b.parent = head, tail, root
bpy.ops.object.mode_set(mode='OBJECT')
for o, bone in parts:
    group = o.vertex_groups.new(name=bone)
    group.add(list(range(len(o.data.vertices))), 1, 'REPLACE')
    mod = o.modifiers.new('SimpleRig', 'ARMATURE')
    mod.object = rig
    o.parent = rig
mouth.parent = rig
mouth.parent_type = 'BONE'
mouth.parent_bone = 'Body'
mouth.matrix_world = mouth.matrix_basis.copy()

scene.render.fps = 24
scene.frame_start, scene.frame_end = 1, 24
for f in range(1, 26):
    t = (f - 1) / 24 * math.tau
    pb = rig.pose.bones['Body']
    pb.location = (0, .018 * (1 - math.cos(2*t)), 0)
    pb.rotation_mode = 'XYZ'
    pb.rotation_euler = (0, 0, .035 * math.sin(t))
    pb.keyframe_insert('location', frame=f)
    pb.keyframe_insert('rotation_euler', frame=f)
    for side, phase in [('L',0),('R',math.pi)]:
        foot = rig.pose.bones['Foot.' + side]
        # Object offsets are expressed in each bone's local axes.
        foot.location = (0, .105 * math.sin(t + phase), .055 * max(0, math.sin(t + phase)))
        foot.keyframe_insert('location', frame=f)
        hand = rig.pose.bones['Hand.' + side]
        hand.rotation_mode = 'XYZ'
        hand.rotation_euler.x = .20 * math.sin(t + phase + math.pi)
        hand.keyframe_insert('rotation_euler', frame=f)
rig.animation_data.action.name = 'SimpleResident_Walk'

bpy.ops.mesh.primitive_plane_add(size=200)
floor = bpy.context.object
floor.name = 'Simple_StudioFloor'
floor.data.materials.append(floor_mat)
world = bpy.data.worlds.new('Simple_StudioWorld')
world.use_nodes = True
next(n for n in world.node_tree.nodes if n.type == 'BACKGROUND').inputs[0].default_value = (.72,.79,.74,1)
next(n for n in world.node_tree.nodes if n.type == 'BACKGROUND').inputs[1].default_value = .7
scene.world = world
for loc, power, size in [((-3,-4,6),450,5),((3,1,4),300,4)]:
    bpy.ops.object.light_add(type='AREA', location=loc)
    light = bpy.context.object
    light.data.energy, light.data.shape, light.data.size = power, 'DISK', size
    light.rotation_euler = (Vector((0,0,1))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3,-7,3.0))
cam = bpy.context.object
cam.rotation_euler = (Vector((0,0,1.05))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type, cam.data.ortho_scale = 'ORTHO', 3.05
scene.camera = cam
scene.render.engine = 'CYCLES'
scene.cycles.samples = 16
scene.cycles.use_denoising = True
scene.render.resolution_x, scene.render.resolution_y = 640, 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.frame_set(1)
for o in bpy.context.selected_objects:
    o.select_set(False)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.region_3d.view_perspective = 'CAMERA'
        area.spaces.active.overlay.show_overlays = False
        area.spaces.active.shading.type = 'MATERIAL'
assert len(rig.data.bones) == 5
assert len(parts) == 13
bpy.data.libraries.write(str(OUT / 'simple-resident.blend'), {scene}, fake_user=True)
scene.render.filepath = str(OUT / 'preview.png')
bpy.ops.render.render(write_still=True)
print('Created new simple character, 5 bones, 24-frame walk. Original scenes preserved.')
