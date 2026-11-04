"""Rounded plush-proportion revision. Reuses v1 low-poly forms, preserves v1."""
import bpy, sys, math, importlib
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE.parent/'toto-v1'))
import forms as f
importlib.reload(f)
scene=bpy.data.scenes.new('Toto_v2_Plumper')
bpy.context.window.scene=scene
root=bpy.data.objects.new('Toto_v2_Model',None);scene.collection.objects.link(root)
root['status']='static shape study, not rigged'
root['forward']='-Y'
cream=f.material('V2_SoftWhite',(.93,.91,.87))
pink=f.material('V2_RosyCheeks',(.94,.47,.53))
dark=f.material('V2_Chocolate',(.075,.032,.020))
white=f.material('V2_EyeHighlights',(1,.99,.98))
body=f.oval('V2_Tummy',(0,0,.72),(.72,.48,.62),cream,root,32,16)
sections=[(0,0,.98,.36,.26),(0,0,1.02,.60,.39),(0,0,1.10,.82,.49),(0,0,1.23,.98,.56),(0,0,1.42,1.055,.60),(0,0,1.64,1.07,.62),(0,0,1.84,1.03,.60),(0,0,2.02,.93,.55),(0,0,2.17,.77,.47),(0,0,2.27,.56,.35),(0,0,2.32,.30,.20),(0,0,2.335,.025,.025)]
head=f.rings('V2_Head',sections,cream,root,40)
for sign,label in [(-1,'L'),(1,'R')]:
    f.oval('V2_Foot_'+label,(sign*.275,-.025,.16),(.15,.20,.16),cream,root,20,10)
    arm=f.oval('V2_Arm_'+label,(sign*.74,-.015,.88),(.16,.19,.25),cream,root,20,10)
    arm.rotation_euler[1]=sign*-.55
    f.oval('V2_Ear_'+label,(sign*.64,.015,2.30),(.195,.155,.205),cream,root,24,12)
    f.face_patch('V2_Cheek_'+label,head,sign*.60,1.40,.17,.125,pink,root)
    f.face_patch('V2_Eye_'+label,head,sign*.31,1.59,.112,.135,dark,root)
    glint=f.face_patch('V2_Glint_'+label,head,sign*.31-.022,1.64,.047,.043,white,root);glint.location.y=-.008

def stroke(name,xz,radius=.014,mat=dark,offset=.021):
    points=[]
    for x,z in xz:
        ok,hit,n,index=head.ray_cast(Vector((x,-2,z)),Vector((0,1,0)))
        assert ok
        points.append((x,hit.y-offset,z))
    return f.tube(name,points,radius,mat,root,8)

for sign,label in [(-1,'L'),(1,'R')]:
    stroke('V2_EyeSmile_'+label,[(sign*.31-.045,1.54),(sign*.31-.025,1.526),(sign*.31,1.521),(sign*.31+.025,1.526),(sign*.31+.045,1.54)],.009,white,.024)
    stroke('V2_Brow_'+label,[(sign*.31-.085,1.89),(sign*.31-.045,1.91),(sign*.31,1.918),(sign*.31+.045,1.912),(sign*.31+.085,1.895)],.014)
    for j in range(3):
        x=sign*.60+(j-1)*.068
        stroke('V2_BlushMark_'+label+str(j),[(x-.018,1.36),(x+.018,1.44)],.010,dark,.022)
# Small open smiling mouth, surface-aligned to keep it readable in side views.
f.face_patch('V2_OpenSmile',head,0,1.30,.077,.095,dark,root)
inside=f.face_patch('V2_MouthInside',head,0,1.305,.049,.060,cream,root);inside.location.y=-.009
stroke('V2_Muzzle',[(-.115,1.395),(-.085,1.367),(-.048,1.365),(-.018,1.38),(0,1.415),(.018,1.38),(.048,1.365),(.085,1.367),(.115,1.395)],.014)
stroke('V2_Chin',[(-.028,1.17),(0,1.161),(.028,1.17)],.011)
f.oval('V2_Tail',(0,.43,.45),(.13,.14,.13),cream,root,16,8)

floor=f.material('V2_StudioIvory',(.87,.85,.81))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.008));bpy.context.object.name='V2_StudioFloor';bpy.context.object.data.materials.append(floor)
world=bpy.data.worlds.new('V2_StudioWorld');world.use_nodes=True;scene.world=world
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.85,.88,.94,1);bg.inputs['Strength'].default_value=.5
for name,loc,power,size in [('Key',(-3,-4,6),450,4),('Fill',(4,-2,3),300,4),('Rim',(1,4,5),450,3)]:
    data=bpy.data.lights.new('V2_'+name,'AREA');data.energy=power;data.size=size
    o=bpy.data.objects.new('V2_'+name,data);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1.2))-o.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('V2_Camera');cam=bpy.data.objects.new('V2_Camera',data);scene.collection.objects.link(cam);scene.camera=cam
cam.location=(.8,-7,2.3);cam.rotation_euler=(Vector((0,0,1.22))-cam.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=3.15
scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(HERE/'preview.png')
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.type='MATERIAL'
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
print('v2 ready:',len(root.children_recursive),'objects')
