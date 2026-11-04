import bpy
import math
import sys
from pathlib import Path
from mathutils import Vector

OUT=Path('E:/Git/ant/art/characters/kemono-rabbit-v1')
OUT.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(OUT))
from face import create_face
scene=bpy.data.scenes.new('Rabbit03_Study')
bpy.context.window.scene=scene
root=bpy.data.objects.new('Rabbit03',None)
scene.collection.objects.link(root)

def mat(name,color,rough=.8):
    m=bpy.data.materials.new('R03_'+name)
    m.diffuse_color=(*color,1)
    m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough
    return m

skin=mat('Skin',(.955,.61,.43))
hair=mat('SilverHair',(.65,.60,.62))
hair_light=mat('HairLight',(.78,.73,.73))
ivory=mat('Ivory',(.88,.84,.73))
earpink=mat('EarPink',(.75,.41,.43))
blue=mat('Dress',(.25,.43,.57))
blue_light=mat('Seams',(.38,.55,.67))
brown=mat('Leather',(.19,.105,.061))
gold=mat('Brass',(.63,.42,.17),.55)
leggings=mat('Leggings',(.17,.19,.22))
floor_mat=mat('Backdrop',(.82,.79,.72))

def ball(name,loc,scale,material,segments=32,rings=20):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=loc)
    o=bpy.context.object
    o.name='R03_'+name
    o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material)
    o.parent=root
    for p in o.data.polygons:p.use_smooth=True
    return o

def between(name,a,b,width,depth,material):
    mid=(Vector(a)+Vector(b))*.5
    o=ball(name,mid,(width,depth,(Vector(b)-Vector(a)).length*.5+width*.25),material)
    o.rotation_euler=(Vector(b)-Vector(a)).to_track_quat('Z','Y').to_euler()
    return o

def loft(name,rings,material,sides=40):
    verts=[]
    for z,rx,ry in rings:
        for j in range(sides):
            t=j/sides*math.tau
            verts.append((rx*math.cos(t),ry*math.sin(t),z))
    faces=[]
    for k in range(len(rings)-1):
        for j in range(sides):
            a=k*sides+j;b=k*sides+(j+1)%sides
            faces.append((a,b,b+sides,a+sides))
    faces += [tuple(range(sides-1,-1,-1)),tuple((len(rings)-1)*sides+j for j in range(sides))]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new('R03_'+name,mesh);scene.collection.objects.link(o)
    o.parent=root;o.data.materials.append(material)
    for p in mesh.polygons:p.use_smooth=True
    bevel=o.modifiers.new('Soft edges','BEVEL');bevel.width=.018;bevel.segments=2
    return o

def lock(name,points,width,depth,material):
    # A tapered sculpted hair ribbon, not a chain of separate balls.
    verts=[];sides=10
    for i,p in enumerate(points):
        t=i/(len(points)-1)
        w=width*(math.sin(math.pi*(.10+.88*t))**.6)
        if i==len(points)-1:w=.008
        tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])
        tangent.normalize()
        across=tangent.cross(Vector((0,1,0))).normalized()
        normal=across.cross(tangent).normalized()
        for j in range(sides):
            a=math.tau*j/sides
            q=Vector(p)+across*w*math.cos(a)+normal*depth*math.sin(a)*(w/width)
            verts.append(q)
    faces=[]
    for i in range(len(points)-1):
        for j in range(sides):
            a=i*sides+j;b=i*sides+(j+1)%sides
            faces.append((a,b,b+sides,a+sides))
    faces.append(tuple(range(sides-1,-1,-1)))
    faces.append(tuple((len(points)-1)*sides+j for j in range(sides)))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new('R03_'+name,mesh);scene.collection.objects.link(o);o.parent=root
    o.data.materials.append(material)
    for p in mesh.polygons:p.use_smooth=True
    sub=o.modifiers.new('Soft lock','SUBSURF');sub.levels=1
    return o

# Human face with a soft lower cheek and a small chin.
head=ball('Head',(0,-.045,2.14),(.61,.43,.64),skin,64,48)
for v in head.data.vertices:
    zn=v.co.z/.64
    if zn<-.20:v.co.x*=1-.30*((-zn-.20)/.80)**1.4
    if zn<-.15:v.co.z+=.105*((-zn-.15)/.85)**1.3
face_image=create_face(OUT/'face.png')
fm=skin.copy();fm.name='R03_FacePaint'
node=fm.node_tree.nodes.new('ShaderNodeTexImage');node.image=face_image
p=next(n for n in fm.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
fm.node_tree.links.new(node.outputs['Color'],p.inputs['Base Color'])
head.data.materials.append(fm)
uv=head.data.uv_layers.active
for poly in head.data.polygons:
    if poly.center.y<0:pass
    avg=sum(head.data.vertices[v].co.y for v in poly.vertices)/len(poly.vertices)
    poly.material_index=1 if avg<-.06 else 0
    for li in poly.loop_indices:
        co=head.data.vertices[head.data.loops[li].vertex_index].co+head.location
        uv.data[li].uv=((co.x/1.30)+.5,((co.z-2.14)/1.38)+.5)
ball('Neck',(0,0,1.60),(.12,.12,.17),skin)
ball('Shirt',(0,0,1.30),(.32,.23,.34),ivory)
loft('Dress',[(.64,.48,.31),(.70,.49,.32),(1.00,.39,.25),(1.23,.30,.225)],blue)
loft('Hem',[(.655,.485,.316),(.69,.492,.322)],blue_light)
# Flat bib panel and shoulder straps follow the shirt's front.
loft('Belt',[(1.16,.323,.246),(1.21,.315,.241)],brown)
bib=ball('Bib',(0,-.225,1.38),(.235,.035,.22),blue)
for s in [-1,1]:
    between('Strap',(s*.19,-.19,1.59),(s*.20,-.257,1.27),.045,.022,blue)
    ball('Button',(s*.185,-.267,1.48),(.024,.014,.024),gold,16,12)
    between('Sleeve',(s*.285,0,1.48),(s*.42,-.01,1.19),.145,.14,ivory)
    between('Forearm',(s*.42,-.01,1.20),(s*.43,-.08,1.00),.083,.085,skin)
    ball('Cuff',(s*.42,-.01,1.16),(.104,.105,.066),ivory)
    hand=ball('Hand',(s*.44,-.09,.97),(.09,.075,.12),skin)
    ball('Thumb',(s*.37,-.13,.99),(.037,.044,.062),skin,20,12)
    between('Leg',(s*.19,0,.73),(s*.19,0,.28),.09,.10,leggings)
    ball('Boot',(s*.19,-.06,.17),(.15,.205,.17),brown)
    ball('BootTop',(s*.19,.005,.30),(.12,.125,.15),brown)
    ball('BootBand',(s*.19,-.008,.34),(.126,.13,.028),gold)
ball('Tail',(0,.33,.94),(.17,.15,.17),ivory)

# Short bob volume sits behind the human face, with individual broad tapered locks.
ball('BackHair',(0,.17,2.18),(.64,.39,.66),hair)
for s in [-1,1]:
    lock('SideHair',[(s*.42,-.05,2.66),(s*.58,-.15,2.45),(s*.63,-.19,2.16),(s*.62,-.13,1.85),(s*.48,-.18,1.72)],.15,.065,hair)
    lock('SideTip',[(s*.53,.08,2.40),(s*.66,.04,2.17),(s*.67,.0,1.88),(s*.75,-.03,1.80)],.10,.065,hair_light)
lock('BangLeft',[(.06,-.15,2.77),(-.14,-.35,2.68),(-.35,-.43,2.49),(-.45,-.41,2.25)],.16,.055,hair_light)
lock('BangCenter',[(.15,-.19,2.75),(.13,-.39,2.60),(-.02,-.49,2.43),(-.19,-.49,2.31)],.15,.055,hair_light)
lock('BangRight',[(.20,-.18,2.73),(.38,-.34,2.59),(.48,-.39,2.36),(.50,-.35,2.20)],.13,.055,hair)
lock('Crown',[(.08,.04,2.74),(-.04,-.02,2.85),(-.17,-.12,2.79)],.065,.04,hair_light)
for name in ['R03_BangLeft','R03_BangCenter','R03_BangRight']:
    for v in scene.objects[name].data.vertices:
        if v.co.y<-.43:v.co.y=-.43+(v.co.y+.43)*.65

# One upright ear and one folded ear, with inset pink surfaces.
ear=ball('EarUpright',(-.27,.03,3.02),(.145,.10,.48),ivory)
ear.rotation_euler.y=-.22
inner=ball('EarInner',(-.27,-.068,3.04),(.075,.014,.33),earpink)
inner.rotation_euler.y=-.22
lock('EarFold',[(.26,.03,2.69),(.40,.02,2.97),(.57,.015,3.11),(.73,-.01,3.02),(.77,-.04,2.82)],.13,.080,ivory)
lock('EarFoldInner',[(.34,-.063,2.80),(.44,-.068,2.97),(.57,-.068,3.025),(.68,-.083,2.96),(.72,-.10,2.85)],.065,.010,earpink)

bpy.ops.mesh.primitive_plane_add(size=200)
floor=bpy.context.object;floor.name='Rabbit03_Studio';floor.data.materials.append(floor_mat)
world=bpy.data.worlds.new('Rabbit03_World');world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs[0].default_value=(.82,.86,1,1);bg.inputs[1].default_value=.45;scene.world=world
for loc,power,size in [((-3,-4,6),450,5),((3,-1,4),180,4),((0,3,5),350,3)]:
    bpy.ops.object.light_add(type='AREA',location=loc)
    l=bpy.context.object;l.data.energy=power;l.data.size=size
    l.rotation_euler=(Vector((0,0,1.7))-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3.7,-8,3.5))
cam=bpy.context.object;scene.camera=cam
cam.rotation_euler=(Vector((0,0,1.75))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO';cam.data.ortho_scale=4.2
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=900;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.view_settings.exposure=.65
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        area.spaces.active.overlay.show_overlays=False
scene.render.filepath=str(OUT/'three-quarter.png')
bpy.data.libraries.write(str(OUT/'rabbit03.blend'),{scene},fake_user=True)
bpy.ops.render.render(write_still=True)
cam.location=(0,-8,2.8)
cam.rotation_euler=(Vector((0,0,1.75))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(OUT/'front.png')
bpy.ops.render.render(write_still=True)
print('Rabbit 03 saved; painted facial texture packed; front and three-quarter rendered.')
