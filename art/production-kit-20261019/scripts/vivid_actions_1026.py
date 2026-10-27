"""Whole-body anticipation/follow-through on the existing, baked production and shared actions.
Keep both tool hands under the same torso transform; no independent hand/socket scaling.
Exports to a staging directory, so Unity assets are replaced together while Play is stopped.
"""
import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector,Euler
ROOT=Path('E:/Git/ant'); KIT=ROOT/'art/production-kit-20261019'; out=KIT/'exports/vivid-1026';out.mkdir(parents=True,exist_ok=True)
report=[]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'work/auto1026/animation-before/Kemono@Attack.fbx'))
reference=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
attachments={n:(reference.data.bones[n].matrix_local.copy(),reference.data.bones[n].length,reference.data.bones[n].parent.name) for n in ('Socket.Back','Socket.Hip.R')}
work_words=('Tool_', 'Chop','Mine','Hammer','GroundWork','BenchWork','Treat','Operate','Stir','Grind','Saw','Stamp','Wash','WaterPlant','Inspect','PressLever','Valve','Knead','Ladle','SewRepair','SmithRepair','BeatFire','ControlPanel')
quiet=('Idle','StandIdle','Sleep','Downed','Captive','Rest','SkyGaze','HangoverIdle')
def category(name):
 if name in quiet:return 'quiet'
 if name in ('Walk','Run','Carry','CarryPerson','WalkTipsy'):return 'locomotion'
 return 'work' if any(x in name for x in work_words) else 'action'

for source,prefix,export_prefix in [(KIT/'pk-motions.blend','Kemono|',''),(KIT/'pk-motions.blend','PK|','PK_')]:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 with bpy.data.libraries.load(str(source),link=False) as (a,b):
  b.objects=[n for n in a.objects if n.startswith('KemonoRig')]
  b.actions=[n for n in a.actions if n.startswith(prefix)]
 rig=next(o for o in b.objects if o and o.type=='ARMATURE');bpy.context.scene.collection.objects.link(rig)
 bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 for n,(matrix,length,parent) in attachments.items():
  if n not in rig.data.edit_bones:
   bone=rig.data.edit_bones.new(n);bone.matrix=matrix;bone.length=length;bone.parent=rig.data.edit_bones[parent];bone.use_deform=False
 bpy.ops.object.mode_set(mode='OBJECT')
 rig.animation_data_create();rig.data.pose_position='POSE'
 for tr in list(rig.animation_data.nla_tracks):rig.animation_data.nla_tracks.remove(tr)
 scene=bpy.context.scene;scene.render.fps=24
 for act in b.actions:
  name=act.name.split('|',1)[1].split('.')[0];kind=category(name)
  rig.animation_data.action=act
  if act.slots:rig.animation_data.action_slot=act.slots[0]
  end=int(act.get('duration_frames',act.frame_range[1]));end=max(2,end)
  loop=bool(act.get('loop',True)); scene.frame_start=1;scene.frame_end=end+1
  bones=[n for n in ('pevis','spine','spine_01','spine_02','neck','head','thigh.L','thigh.R','shin.L','shin.R','foot.L','foot.R') if n in rig.pose.bones]
  poses=[]
  for f in range(1,end+2):
   scene.frame_set(f);bpy.context.view_layer.update()
   poses.append({n:(rig.pose.bones[n].rotation_euler.copy(),rig.pose.bones[n].location.copy()) for n in bones})
  for f,pose in enumerate(poses,1):
   t=(f-1)/end;wave=math.sin(math.tau*t);pulse=(1-math.cos(math.tau*t))*.5
   amp=.28 if kind=='work' else .12 if kind=='action' else .06 if kind=='locomotion' else .015
   envelope=1 if loop else math.sin(math.pi*t)**2
   for n,(rotation,location) in pose.items():
    pb=rig.pose.bones[n];basis=pb.bone.matrix_local.to_3x3();delta=Vector((0,0,0))
    if n.startswith('spine'):delta=Vector((amp*wave*.55,amp*wave*.12,amp*math.sin(math.tau*t+.65)*.23))*envelope
    elif n=='pevis':
     delta=Vector((amp*wave*.12,0,amp*wave*.20))*envelope
     # Bend knees with pelvis lowering instead of floating the character root.
     if kind=='work':location+=basis.inverted()@Vector((.012*wave,0,-.025*pulse))
    elif kind=='work' and n.startswith('thigh'):delta.x=-.13*pulse
    elif kind=='work' and n.startswith('shin'):delta.x=.22*pulse
    elif kind=='work' and n.startswith('foot'):delta.x=-.09*pulse
    elif n in ('neck','head'):delta=Vector((amp*wave*-.16,0,amp*wave*-.12))*envelope
    q=basis.inverted()@Euler(delta).to_matrix()@basis
    pb.rotation_mode='XYZ';pb.rotation_euler=(q@rotation.to_matrix()).to_euler('XYZ',rotation);pb.location=location
    pb.keyframe_insert('rotation_euler',frame=f,group=n);pb.keyframe_insert('location',frame=f,group=n)
  for layer in act.layers:
   for strip in layer.strips:
    for bag in strip.channelbags:
     for fc in bag.fcurves:
      for key in fc.keyframe_points:key.interpolation='LINEAR'
  scene.frame_set(1);bpy.context.view_layer.update()
  for o in scene.objects:o.select_set(False)
  rig.select_set(True);bpy.context.view_layer.objects.active=rig
  path=out/('Kemono@'+export_prefix+name+'.fbx')
  bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
  report.append({'file':path.name,'frames':end+1,'category':kind,'body_amplitude_rad':amp,'source':str(source)})
  print('VIVID',path.name,kind,flush=True)
 bpy.data.libraries.write(str(KIT/('vivid-1026-'+export_prefix.rstrip('_')+'.blend')),{rig,*b.actions},fake_user=True)
(out/'manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('VIVID_TOTAL',len(report),flush=True)
