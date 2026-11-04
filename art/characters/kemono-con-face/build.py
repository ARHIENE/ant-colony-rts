import bpy
import math
from pathlib import Path
from mathutils import Vector

OUT = Path('E:/Git/ant/art/characters/kemono-con-face')
OUT.mkdir(parents=True, exist_ok=True)
src = bpy.data.scenes['Rabbit_NewFace_V2']
s = bpy.data.scenes.new('Rabbit_Emoticon_Face')
mapping = {}
for old in src.objects:
    new = old.copy()
    if old.data:
        new.data = old.data.copy()
    s.collection.objects.link(new)
    mapping[old] = new
for old, new in mapping.items():
    world = old.matrix_world.copy()
    new.parent = mapping.get(old.parent)
    new.matrix_world = world
s.world = src.world.copy()
s.camera = mapping[src.camera]
bpy.context.window.scene = s
face = next(o for o in s.objects if o.name.startswith('Settler_Face'))
for v in face.data.vertices:
    x, y, z = v.co
    ax = abs(x)
    side = 1 if x >= 0 else -1
    if y < -.035:
        # Move sockets, iris and eyelids together, avoiding detached decal eyes.
        w = math.exp(-((ax-.085)/.061)**6-((z-.852)/.072)**6)
        v.co.x += side * ((ax-.085)*.06-.012)*w
        v.co.z += ((z-.852)*.12-.006)*w
        cheek = math.exp(-((z-.787)/.039)**2-((ax-.111)/.045)**2)
        v.co.x += side*.003*cheek
        v.co.y -= .004*cheek
        if z < .792:
            q = min(1, max(0, (.792-z)/.064))
            v.co.z += .008*q*q
            v.co.x *= 1+.02*q
face.data.update()
bpy.context.view_layer.update()

def material(name, color):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value = (*color,1)
    p.inputs['Roughness'].default_value = .85
    return m

ink = material('Con_face_soft_brown', (.10,.035,.035))
pink = material('Con_face_blush', (.84,.28,.28))

def surface(x,z):
    hit, p, normal, idx = face.ray_cast(Vector((x,-1,z)), Vector((0,1,0)))
    return p.y-.0018 if hit else -.15

def stroke(name, points, radius, mat):
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions = '3D'
    curve.bevel_depth = radius
    curve.bevel_resolution = 3
    spline = curve.splines.new('POLY')
    spline.points.add(len(points)-1)
    for p,(x,z) in zip(spline.points,points):
        p.co = (x,surface(x,z),z,1)
    ob = bpy.data.objects.new(name,curve)
    s.collection.objects.link(ob)
    ob.matrix_world = face.matrix_world.copy()
    curve.materials.append(mat)
    return ob

# A tiny legible smile and short blush marks use the visual vocabulary of SD icons.
stroke('Con_small_smile', [(x,.757+.004*(x/.012)**2) for x in [ -.012+i*.024/24 for i in range(25)]], .0015, ink)
for side in [-1,1]:
    for i in range(2):
        x = side*(.133+i*.007)
        stroke('Con_blush', [(x-.002,.785),(x+.001,.791)], .0014,pink)

s.render.engine = 'CYCLES'
s.cycles.samples = 32
s.cycles.use_denoising = True
s.render.image_settings.file_format = 'PNG'
s.render.resolution_x = 900
s.render.resolution_y = 1000
s.render.resolution_percentage = 100
s.view_settings.exposure = .3
for name,loc,aim,scale in [
    ('face-close',(0,-3,.90),(0,0,.89),.63),
    ('front',(0,-3,.88),(0,0,.75),1.78),
    ('three-quarter',(.95,-3,.98),(0,0,.75),1.78)]:
    s.camera.location=loc
    s.camera.rotation_euler=(Vector(aim)-s.camera.location).to_track_quat('-Z','Y').to_euler()
    s.camera.data.ortho_scale=scale
    s.render.filepath=str(OUT/(name+'.png'))
    bpy.ops.render.render(write_still=True)
bpy.data.libraries.write(str(OUT/'rabbit-con-face.blend'),{s},fake_user=True)
assert face.data is not next(o for o in src.objects if o.name.startswith('Settler_Face')).data
print('Independent emoticon face model and three previews saved.')


