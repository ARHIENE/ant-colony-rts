import bpy,sys,importlib,json,math
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent;sys.path.insert(0,str(HERE))
import rig,clips;importlib.reload(rig);importlib.reload(clips)
old=bpy.context.scene;scene=bpy.data.scenes.new('Resident_Animation_Library');bpy.context.window.scene=scene;scene.world=old.world.copy();scene.render.fps=24
actor=rig.make(scene,bpy.data.objects['Resident_01'],'ResidentRig_01')
actions=clips.build(actor)
scene.frame_start=1;scene.frame_end=49;scene.frame_set(1)
for ob in old.objects:
    if ob.type=='LIGHT':
        o=ob.copy();o.data=ob.data.copy();scene.collection.objects.link(o);o.location=(1,-4,5);o.data.energy=700;o.data.size=5
cam=bpy.data.objects.new('AnimCamera',bpy.data.cameras.new('AnimCamera'));scene.collection.objects.link(cam);scene.camera=cam;cam.location=(3,-7,2.8);cam.rotation_euler=(Vector((0,0,1.15))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=3.4
scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.resolution_percentage=100
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
(HERE/'clips.json').write_text(json.dumps({name:{'frames':f,'loop':loop,'fps':24,'visual_contacts':clips.CONTACT.get(name,[])} for name,(f,loop) in clips.SPECS.items()},indent=2),encoding='utf-8')
print('Rig',len(actor.data.bones),'bones',len(actor.children),'parts;',len(actions),'actions')
