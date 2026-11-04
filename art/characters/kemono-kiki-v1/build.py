import bpy
import math
from pathlib import Path
from mathutils import Vector, Quaternion

OUT=Path('E:/Git/ant/art/characters/kemono-kiki-v1')
OUT.mkdir(parents=True,exist_ok=True)
src=bpy.data.scenes['Kiki_Reference_Study']
bpy.context.window.scene=src
rig=next(o for o in src.objects if o.type=='ARMATURE')
rig.data.pose_position='POSE'
for bone in rig.pose.bones:
    bone.matrix_basis.identity()
for side,angle in [('L',.40),('R',-.40)]:
    bone=rig.pose.bones['upper_arm.'+side]
    axis=bone.bone.matrix_local.to_3x3().inverted()@Vector((0,1,0))
    bone.rotation_mode='QUATERNION'
    bone.rotation_quaternion=Quaternion(axis,angle)
face=next(o for o in src.objects if o.type=='MESH' and o.name.startswith('Face.'))
for key in face.data.shape_keys.key_blocks:
    if key.name!='Basis':key.value=0
bpy.context.view_layer.update()
deps=bpy.context.evaluated_depsgraph_get()
copies=[]
for prefix,label in [('Face.','Face'),('Hair.','Hair'),('Dress_mesh.','Body')]:
    o=next(o for o in src.objects if o.type=='MESH' and o.name.startswith(prefix))
    evaluated=o.evaluated_get(deps)
    mesh=bpy.data.meshes.new_from_object(evaluated,depsgraph=deps)
    mesh.transform(o.matrix_world)
    for v in mesh.vertices:
        if label=='Body':
            v.co.x*=1.10;v.co.y*=1.06;v.co.z*=.73
        else:
            v.co.x*=1.23;v.co.y*=1.23;v.co.z=(v.co.z-.97)*1.23+.97*.73
    copies.append((mesh,label))

s=bpy.data.scenes.new('Kiki_Rabbit_Settler')
bpy.context.window.scene=s
root=bpy.data.objects.new('KikiDerived_RabbitStudy',None);s.collection.objects.link(root)
for mesh,label in copies:
    o=bpy.data.objects.new('Settler_'+label,mesh);s.collection.objects.link(o);o.parent=root
    for poly in mesh.polygons:poly.use_smooth=True

def material(name,col):
    m=bpy.data.materials.new('Settler_'+name);m.diffuse_color=(*col,1);m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*col,1);p.inputs['Roughness'].default_value=.85
    return m

blue=material('Blue',(.20,.37,.47))
ivory=material('Linen',(.80,.74,.61))
white=material('EarFur',(.83,.80,.80))
pink=material('EarInner',(.70,.34,.37))
leather=material('Leather',(.20,.105,.06))
gold=material('Brass',(.55,.35,.12))

# Reuse the supplied texture and UVs, remapping only saturated Santa-red cloth.
body=s.objects['Settler_Body']
cloth=body.data.materials[0].copy();cloth.name='Settler_RecoloredSourceCloth'
nodes=cloth.node_tree.nodes;links=cloth.node_tree.links
tex=next(n for n in nodes if n.type=='TEX_IMAGE')
bsdf=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
sep=nodes.new('ShaderNodeSeparateColor');links.new(tex.outputs['Color'],sep.inputs[0])
sub=nodes.new('ShaderNodeMath');sub.operation='SUBTRACT';links.new(sep.outputs[0],sub.inputs[0]);links.new(sep.outputs[1],sub.inputs[1])
mask=nodes.new('ShaderNodeMath');mask.operation='GREATER_THAN';mask.inputs[1].default_value=.28;links.new(sub.outputs[0],mask.inputs[0])
mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MIX';mix.inputs[2].default_value=(.22,.40,.52,1)
links.new(mask.outputs[0],mix.inputs[0]);links.new(tex.outputs['Color'],mix.inputs[1]);links.new(mix.outputs[0],bsdf.inputs['Base Color']);links.new(mix.outputs[0],bsdf.inputs['Emission Color'])
body.data.materials[0]=cloth

def ball(name,loc,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=20,location=loc)
    o=bpy.context.object;o.name='Settler_'+name;o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mat);o.parent=root
    for p in o.data.polygons:p.use_smooth=True
    return o

def ribbon(name,points,width,depth,mat):
    verts=[];count=12
    for i,p in enumerate(points):
        t=i/(len(points)-1);w=width*(.62+.38*math.sin(t*math.pi))
        if i==len(points)-1:w=.003
        tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])
        tangent.normalize();a=tangent.cross(Vector((0,1,0))).normalized();b=a.cross(tangent).normalized()
        for j in range(count):
            ang=j/count*math.tau;verts.append(Vector(p)+a*w*math.cos(ang)+b*depth*math.sin(ang)*(w/width))
    faces=[]
    for i in range(len(points)-1):
        for j in range(count):
            a=i*count+j;b=i*count+(j+1)%count;faces.append((a,b,b+count,a+count))
    faces+=[tuple(range(count-1,-1,-1)),tuple((len(points)-1)*count+j for j in range(count))]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new('Settler_'+name,mesh);s.collection.objects.link(o);o.parent=root;o.data.materials.append(mat)
    for p in mesh.polygons:p.use_smooth=True
    mod=o.modifiers.new('Smooth','SUBSURF');mod.levels=2
    return o

ribbon('EarL',[(-.12,.025,1.12),(-.16,.025,1.25),(-.18,.02,1.42),(-.13,.015,1.54)],.055,.026,white)
ribbon('EarLInner',[(-.137,-.003,1.21),(-.165,-.004,1.30),(-.17,-.009,1.42),(-.135,-.012,1.48)],.026,.003,pink)
ribbon('EarR',[(.12,.035,1.13),(.16,.035,1.28),(.23,.02,1.36),(.30,-.01,1.29),(.31,-.025,1.20)],.060,.028,white)
ribbon('EarRInner',[(.15,.003,1.20),(.18,.001,1.28),(.23,-.01,1.31),(.275,-.04,1.27),(.29,-.05,1.22)],.029,.004,pink)
ball('Tail',(0,.13,.36),(.07,.065,.07),white)

# A simple apron covers the seasonal chest ornaments without altering the source.
ribbon('ApronBib',[(-.001,-.084,.67),(-.001,-.106,.61),(-.001,-.105,.54),(-.001,-.10,.49)],.075,.012,blue)
for side in [-1,1]:
    ribbon('Strap',[(side*.055,-.035,.71),(side*.059,-.075,.675),(side*.061,-.105,.59)],.013,.006,leather)
    ball('Button',(side*.058,-.12,.615),(.009,.004,.009),gold)

# Match the overall source rendering with soft light instead of glossy clay.
world=bpy.data.worlds.new('Settler_World');world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs[0].default_value=(.77,.81,.88,1);bg.inputs[1].default_value=.65;s.world=world
floor_mat=material('Floor',(.75,.70,.61))
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='Settler_Studio';floor.location.z=-.008;floor.data.materials.append(floor_mat)
for loc,power,size in [((-2,-3,4),200,4),((2,1,3),140,3)]:
    bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((0,0,.7))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(1.2,-3,.98));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=1.78;cam.rotation_euler=(Vector((0,0,.75))-cam.location).to_track_quat('-Z','Y').to_euler();s.camera=cam
s.render.engine='CYCLES';s.cycles.samples=32;s.cycles.use_denoising=True
s.render.resolution_x=900;s.render.resolution_y=1000;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG'
s.view_settings.exposure=.3
for o in s.objects:
    if o.type=='MESH':
        for m in o.data.materials:
            if m and m.use_nodes:
                for n in m.node_tree.nodes:
                    if n.type=='TEX_IMAGE' and n.image:n.image.pack()
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.overlay.show_overlays=False
bpy.data.libraries.write(str(OUT/'rabbit-settler.blend'),{s},fake_user=True)
s.render.filepath=str(OUT/'three-quarter.png');bpy.ops.render.render(write_still=True)
cam.location=(0,-3,.88);cam.rotation_euler=(Vector((0,0,.75))-cam.location).to_track_quat('-Z','Y').to_euler()
s.render.filepath=str(OUT/'front.png');bpy.ops.render.render(write_still=True)
print('Kiki-derived separate rabbit study rendered. Original Unity assets untouched.')
