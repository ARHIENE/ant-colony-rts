"""Run in Blender. New scene only; previous scenes are retained."""
import bpy, sys, math, json
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
import forms as f
import importlib
importlib.reload(f)

scene=bpy.data.scenes.new('Toto_Character_Studio')
bpy.context.window.scene=scene
root=bpy.data.objects.new('Toto_Model',None);scene.collection.objects.link(root)
root['status']='static concept model; no rig or animation'
root['forward']='-Y';root['scale_note']='Prototype dimensions, world scale not finalized'
cream=f.material('Toto_Cream',(.94,.84,.66))
pink=f.material('Toto_Blush',(.93,.43,.38))
inner=f.material('Toto_InnerEar',(.91,.59,.49))
dark=f.material('Toto_Cocoa',(.075,.039,.024))
bag=f.material('Toto_Leather',(.28,.135,.065))
flap=f.material('Toto_LeatherFlap',(.40,.225,.105))
gold=f.material('Toto_Button',(.78,.51,.20))
white=f.material('Toto_Glint',(.99,.97,.89))
sections=[(0,0,.14,.22,.22),(0,0,.20,.40,.32),(0,0,.32,.54,.40),(0,0,.52,.63,.46),(0,0,.73,.64,.49),(0,0,.87,.66,.51),(0,0,1.03,.79,.57),(0,0,1.21,.94,.63),(0,0,1.42,1.0,.65),(0,0,1.62,.97,.64),(0,0,1.82,.86,.59),(0,0,2.0,.68,.49),(0,0,2.14,.42,.32),(0,0,2.20,.12,.10),(0,0,2.21,.025,.025)]
body=f.rings('Toto_Body',sections,cream,root,32)
for sign in [-1,1]:
    foot=f.oval('Foot_L' if sign<0 else 'Foot_R',(sign*.30,-.14,.145),(.235,.29,.145),cream,root)
    hand=f.oval('Hand_L' if sign<0 else 'Hand_R',(sign*.63,-.41,.69),(.205,.22,.255),cream,root)
    hand.rotation_euler[1]=sign*-.35
# Bunny ears with different bent tips, both extend from the same head.
for sign in [-1,1]:
    ear=[(sign*.40,.02,1.98,.15,.12),(sign*.44,.02,2.12,.19,.14),(sign*.48,.02,2.30,.205,.145),(sign*.54,.01,2.48,.205,.14),(sign*.63,-.01,2.62,.19,.13),(sign*.75,-.04,2.67,.165,.115),(sign*.87,-.08,2.63,.13,.095),(sign*.94,-.12,2.55,.085,.07),(sign*.95,-.13,2.50,.025,.025)]
    if sign<0:ear=[(x,y,z+(.09 if i>=3 else 0),rx,ry) for i,(x,y,z,rx,ry) in enumerate(ear)]
    f.bent_ear('Ear_L' if sign<0 else 'Ear_R',ear,cream,root,16)
    patch=f.oval('InnerEar_L' if sign<0 else 'InnerEar_R',(sign*.59,-.128,2.43),(.095,.027,.25),inner,root,16,8)
    patch.rotation_euler[1]=sign*.33
# Face follows the front surface; left/right use mirrored orientation.
for sign in [-1,1]:
    eye=f.oval('Eye_L' if sign<0 else 'Eye_R',(sign*.32,-.624,1.48),(.064,.032,.105),dark,root,16,8)
    eye.rotation_euler[2]=sign*-.22
    f.oval('EyeGlint_L' if sign<0 else 'EyeGlint_R',(sign*.32-.014,-.656,1.515),(.013,.008,.019),white,root,12,6)
    f.face_patch('Cheek_L' if sign<0 else 'Cheek_R',body,sign*.61,1.25,.14,.095,pink,root)
f.tube('Mouth',[(-.083,-.649,1.335),(-.052,-.661,1.308),(-.023,-.666,1.313),(0,-.667,1.337),(.023,-.666,1.313),(.052,-.661,1.308),(.083,-.649,1.335)],.012,dark,root)
# Small satchel and rounded strap, separate editable meshes.
f.oval('Satchel',(.68,.08,.58),(.28,.24,.31),bag,root)
f.oval('SatchelFlap',(.70,-.13,.70),(.285,.065,.16),flap,root)
f.oval('SatchelButton',(.70,-.196,.64),(.045,.018,.05),gold,root,12,6)
f.tube('ShoulderStrap',[(-.55,.23,1.0),(-.65,-.07,1.02),(-.60,-.40,.98),(-.36,-.55,.86),(0,-.57,.76),(.33,-.49,.68),(.60,-.25,.67),(.78,.05,.70),(.62,.40,.85),(.1,.50,1.03),(-.38,.38,1.06),(-.55,.23,1.0)],.036,bag,root)
f.oval('Tail',(0,.44,.37),(.16,.16,.16),cream,root,16,8)

# Studio scene, not part of model exports.
floor=f.material('Studio_Warm',(.72,.67,.57))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012));bpy.context.object.name='Studio_Floor';bpy.context.object.data.materials.append(floor)
world=bpy.data.worlds.new('Toto_StudioWorld');world.use_nodes=True;scene.world=world
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.72,.78,.85,1);bg.inputs['Strength'].default_value=.45
for name,loc,power,size in [('Key',(-3,-4,6),450,4),('Fill',(4,-2,3),220,3),('Rim',(1,4,5),450,3)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
    o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1.3))-o.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('Toto_Camera');cam=bpy.data.objects.new('Toto_Camera',camdata);scene.collection.objects.link(cam);scene.camera=cam
cam.location=(3,-7,3.1);cam.rotation_euler=(Vector((0,0,1.4))-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=3.65
scene.render.resolution_x=1100;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(HERE/'preview.png')
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        area.spaces.active.shading.type='MATERIAL'
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
meshes=[o for o in root.children_recursive if o.type=='MESH']
assert len(meshes)>15 and all(len(o.data.vertices)>0 for o in meshes)
print('Toto built:',len(meshes),'meshes; ready for visual review')
