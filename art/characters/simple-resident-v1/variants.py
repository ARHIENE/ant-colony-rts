import bpy
import math
import json
from pathlib import Path
from mathutils import Vector

OUT = Path('E:/Git/ant/art/characters/simple-resident-v1')
scene = bpy.data.scenes.new('SimpleResidents_20')
bpy.context.window.scene = scene

def mat(name, color):
    m = bpy.data.materials.new('V20_' + name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    node = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    node.inputs['Base Color'].default_value = (*color, 1)
    node.inputs['Roughness'].default_value = .8
    return m

ink = mat('Ink', (.07,.043,.033))
blush = mat('Blush', (.88,.40,.36))
leaf = mat('Leaf', (.29,.49,.25))
cream = (.87,.81,.66)
peach = (.92,.65,.50)
blue = (.52,.70,.80)
pink = (.87,.58,.65)
mint = (.59,.76,.57)
lilac = (.68,.60,.79)
gold = (.88,.74,.41)

# Body radius / height, taper and ear construction deliberately vary together.
specs = [
    ('upright', .62,.82,-.05,cream,'long',0),
    ('dumpling', .78,.57,.02,peach,'none',0),
    ('drop', .60,.86,-.22,blue,'tuft',0),
    ('bean', .59,.73,-.04,pink,'flop',0),
    ('sprout', .65,.67,-.10,mint,'sprout',0),
    ('cat', .65,.68,.02,lilac,'point',0),
    ('pudding', .68,.67,-.28,gold,'button',0),
    ('longbean', .48,.93,.00,cream,'asym',0),
    ('round', .69,.67,.0,blue,'sidefin',0),
    ('peanut', .53,.84,.06,peach,'none',.15),
    ('moth', .65,.65,.00,pink,'antenna',0),
    ('seal', .78,.55,-.06,lilac,'tiny',0),
    ('bud', .57,.78,-.18,mint,'bud',0),
    ('lop', .57,.79,.04,cream,'longflop',0),
    ('star', .63,.64,-.02,gold,'triple',0),
    ('acorn', .58,.76,.14,peach,'cap',0),
    ('cloud', .76,.56,.0,blue,'cloud',0),
    ('fox', .61,.68,-.08,cream,'widepoint',0),
    ('mushroom', .66,.69,.08,lilac,'mushroom',0),
    ('snowpea', .48,.84,-.04,mint,'singleleaf',.07),
]

def ball(name, loc, scale, material, parent, tilt=0, segments=20):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=12, location=(0,0,0))
    o = bpy.context.object
    o.name = name
    o.parent = parent
    o.location, o.scale = loc, scale
    o.rotation_euler.y = tilt
    o.data.materials.append(material)
    for p in o.data.polygons:
        p.use_smooth = True
    return o

def pointed(name, x, z, width, height, material, root, tilt):
    o = ball(name, (x,0,z), (width,.12,height), material, root, tilt)
    for v in o.data.vertices:
        v.co.x *= .7 - .35*v.co.z
    return o

manifest = []
for i, (kind, rx, rz, taper, color, ears, waist) in enumerate(specs, 1):
    root = bpy.data.objects.new('Design_%02d_%s' % (i,kind), None)
    scene.collection.objects.link(root)
    root.location = (((i-1)%5-2)*2.8,0,(3-(i-1)//5)*3.0)
    fur = mat('%02d_Fur'%i,color)
    tint = mat('%02d_Tint'%i, tuple(c*.68 for c in color))
    ry = .44
    zc = rz+.22
    body = ball('D%02d_Body'%i,(0,0,zc),(rx,ry,rz),fur,root)
    for v in body.data.vertices:
        v.co.x *= (1+taper*v.co.z) * (1-waist*math.exp(-v.co.z*v.co.z*18))
    for s in [-1,1]:
        ball('D%02d_Hand'%i,(s*rx*.96,-.015,.60),( .12,.13,.18),fur,root,-s*.18)
        ball('D%02d_Foot'%i,(s*rx*.40,-.05,.15),(.15,.20,.14),fur,root)
    top = zc+rz
    for s in [-1,1]:
        if ears in ('long','asym'):
            h = .26 if ears=='long' or s<0 else .13
            ball('Ear',(s*.25,0,top+.11),(.115,.10,h),fur,root,s*.3)
        elif ears in ('flop','longflop'):
            h = .24 if ears=='flop' else .40
            ball('Ear',(s*(rx*.87),0,top-.20),(.15,.14,h),fur,root,-s*.23)
        elif ears in ('point','widepoint'):
            pointed('Ear',s*.41,top-.025,.21,.27 if ears=='point' else .34,fur,root,s*(.28 if ears=='point' else .60))
        elif ears in ('button','tiny'):
            ball('Ear',(s*.39,0,top-.04),(.13,.11,.13 if ears=='button' else .075),fur,root)
        elif ears=='sidefin':
            ball('Fin',(s*(rx+.03),0,zc+.26),(.20,.08,.11),fur,root,s*.45)
        elif ears=='antenna':
            ball('Stalk',(s*.26,0,top+.06),(.037,.04,.15),fur,root,s*.3)
            ball('Tip',(s*.30,0,top+.19),(.10,.08,.095),tint,root)
        elif ears=='sprout':
            pointed('Leaf',s*.14,top+.12,.15,.24,leaf,root,s*.75)
        elif ears=='cloud':
            ball('CloudLobe',(s*.34,0,top-.05),(.28,.30,.21),fur,root)
    if ears in ('tuft','bud','singleleaf','cap'):
        pointed('Top',.035,top+.09,.12 if ears!='bud' else .19,.22 if ears!='cap' else .11,leaf if ears=='singleleaf' else tint,root,.30)
    if ears=='triple':
        for x,h in [(-.20,.13),(0,.23),(.20,.13)]:
            ball('Crown',(x,0,top+.015),(.10,.09,h),fur,root,x*2)
    if ears=='mushroom':
        cap = ball('Cap',(0,.025,top-.12),(.74,.49,.23),tint,root)
    # Flat little facial pieces follow the curved body rather than floating.
    eye_z = zc+.13
    def front(x,z):
        zn=(z-zc)/rz
        width=rx*(1+taper*zn)*(1-waist*math.exp(-zn*zn*18))
        return -ry*math.sqrt(max(.01,1-(x/width)**2-zn**2))-.013
    ex = rx*.33
    for s in [-1,1]:
        x=s*ex
        ball('D%02d_Eye'%i,(x,front(x,eye_z),eye_z),(.030,.020,.050 if i%4 else .037),ink,root,segments=16)
        x=s*rx*.55
        z=eye_z-.15
        ball('Blush',(x,front(x,z),z),(.070,.014,.035),blush,root)
    mz=eye_z-.16
    curve=bpy.data.curves.new('Smile','CURVE')
    curve.dimensions='3D'
    curve.bevel_depth=.008
    curve.bevel_resolution=1
    spline=curve.splines.new('BEZIER')
    spline.bezier_points.add(2)
    for p,(x,z) in zip(spline.bezier_points,[(-.04,mz+.012),(0,mz-.008),(.04,mz+.012)]):
        p.co=(x,front(x,z)-.008,z)
        p.handle_left_type=p.handle_right_type='AUTO'
    mouth=bpy.data.objects.new('D%02d_Smile'%i,curve)
    scene.collection.objects.link(mouth)
    mouth.parent=root
    curve.materials.append(ink)
    font=bpy.data.curves.new('Number','FONT')
    font.body='%02d'%i
    font.align_x='CENTER'
    font.size=.24
    label=bpy.data.objects.new('Label_%02d'%i,font)
    scene.collection.objects.link(label)
    label.location=(root.location.x,-.04,root.location.z-.28)
    label.rotation_euler.x=math.pi/2
    font.materials.append(ink)
    manifest.append({'number':i,'root':root.name,'shape':kind,'ears':ears,'body_width':rx*2,'body_height':rz*2})

world=bpy.data.worlds.new('V20_World')
world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs[0].default_value=(.86,.84,.78,1)
bg.inputs[1].default_value=.7
scene.world=world
for loc,power,size in [((-5,-8,13),1800,8),((6,-4,7),1000,7)]:
    bpy.ops.object.light_add(type='AREA',location=loc)
    o=bpy.context.object
    o.data.energy,o.data.size=power,size
    o.rotation_euler=(Vector((0,0,5.5))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(0,-25,5.5))
cam=bpy.context.object
cam.rotation_euler=(Vector((0,0,5.5))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO'
cam.data.ortho_scale=14.0
scene.camera=cam
scene.render.engine='CYCLES'
scene.cycles.samples=24
scene.cycles.use_denoising=True
scene.render.resolution_x=1800
scene.render.resolution_y=1530
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(OUT/'variants-20.png')
scene.view_settings.view_transform='Standard'
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        area.spaces.active.overlay.show_overlays=False
assert len(manifest)==20
(OUT/'variants-20.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
bpy.data.libraries.write(str(OUT/'variants-20.blend'),{scene},fake_user=True)
bpy.ops.render.render(write_still=True)
print('20 separate new character designs created and rendered.')
