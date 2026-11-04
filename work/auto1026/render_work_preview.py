import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector
R=Path('E:/Git/ant'); O=R/'work/auto1026';O.mkdir(exist_ok=True)
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
clip=args[0] if args else 'Tool_Chop';tool=args[1] if len(args)>1 else 'Tool_Axe_Stone';prefix='PK' if clip.startswith('Tool_') else 'Kemono'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(R/'Assets/Resources/Kemono/Kemono_Character.fbx'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
for o in list(bpy.context.scene.objects):
 if o.type=='MESH' and o.name.startswith('Part_'):
  o.hide_render= not any(o.name.startswith(x) for x in ('Part_Body_','Part_Outfit_','Part_Hair_SHORT_','Part_Ears_CAT_','Part_Tail_FOX_','Part_Face_NEUTRAL_')) or '_ARMOR' in o.name
with bpy.data.libraries.load(str(R/('art/production-kit-20261019/vivid-1026-'+('PK' if prefix=='PK' else '')+'.blend')),link=False) as (a,b):
 b.actions=[n for n in a.actions if n==prefix+'|'+clip]
act=b.actions[0];rig.animation_data_create();rig.animation_data.action=act
for pb in rig.pose.bones:pb.rotation_mode='XYZ'
if act.slots:rig.animation_data.action_slot=act.slots[0]
for tr in list(rig.animation_data.nla_tracks):rig.animation_data.nla_tracks.remove(tr)
char_rig=rig
bpy.ops.import_scene.fbx(filepath=str(R/('Assets/Resources/Kemono/Handheld/'+tool+'.fbx')))
socket=next(o for o in bpy.context.scene.objects if o.name=='Socket.Tool.R') if any(o.name=='Socket.Tool.R' for o in bpy.context.scene.objects) else None
bone=next((n for n in char_rig.pose.bones.keys() if n=='Socket.Tool.R'),None)
for o in bpy.context.selected_objects:
 if o.type=='MESH':
  o.parent=char_rig;o.parent_type='BONE';o.parent_bone=bone or 'hand.R'
  # Blender bone parenting places objects at the tail; Unity sockets use the bone head.
  o.matrix_parent_inverse.identity();o.location=(0,-char_rig.data.bones[o.parent_bone].length,0);o.rotation_euler=(0,0,0)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=20
scene.world=bpy.data.worlds.new('PreviewWorld');scene.world.color=(.35,.35,.35)
scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(1.7,-2.3,1.5));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=1.4;aim(cam,(0,0,.55));scene.camera=cam
for loc,power,size in [((2,-3,4),500,4),((-2,1,2),350,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);bpy.context.object.data.energy=power;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=size;aim(bpy.context.object,(0,0,.5))
for frame in (1,9,17):
 scene.frame_set(frame);scene.render.filepath=str(O/f'preview-{clip}-{frame}.png');bpy.ops.render.render(write_still=True)
print('PREVIEW_DONE',list(char_rig.pose.bones.keys()),flush=True)
