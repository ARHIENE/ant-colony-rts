import bpy, math, json, runpy, sys
from pathlib import Path
from mathutils import Vector, Quaternion

ROOT=Path('F:/UnityProjects/ant-colony-rts')
ART=ROOT/'art/enemy-concepts-1114'
OUT=ROOT/'work/organic-horde-motion-1114/refined'
profiles=runpy.run_path(str(ART/'remaining_motion_profiles.py'))['PROFILES']
special={
 'WallRam':dict(feet=[10,14,31,35],chain=2,stride=.13),
 'NeedleStalker':dict(feet=[10,15],chain=3,stride=.15),
 'BloomMender':dict(feet=[7,12,17,22,27],chain=3,stride=.08)}
results=json.loads((OUT/'results.json').read_text()) if (OUT/'results.json').exists() else []
only=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
for source in sorted(ART.glob('*/*_5Motions.blend')):
 name=source.parent.name
 if only and name not in only: continue
 folder=OUT/name; folder.mkdir(parents=True,exist_ok=True)
 bpy.ops.wm.open_mainfile(filepath=str(source))
 scene=bpy.context.scene; rig=bpy.data.objects[name+'_Rig']; mesh=bpy.data.objects[name+'_Mesh']
 profile=profiles.get(name,special.get(name)); feet=profile['feet']
 rig.animation_data.action=bpy.data.actions['Idle']; scene.frame_set(1)
 forward=Vector((-.56,-.83,0) if name=='WallRam' else (0,-1,0)).normalized()
 root=rig.pose.bones['Bone_000']; inverse=root.bone.matrix_local.to_3x3().inverted()
 idle={b.name:(b.location.copy(),b.rotation_quaternion.copy()) for b in rig.pose.bones}
 bases=[rig.matrix_world@rig.pose.bones[f'Bone_{i:03d}'].tail for i in feet]
 stride={'WallRam':.065,'AcidSpitter':.065,'BroodAlien':.10,'SiegeBrooder':.045,'VeilWeaver':.06,'BloomMender':.06,'BroodCarrier':.06}.get(name,profile.get('stride',.1))
 targets=[]
 if feet:
  for i,number in enumerate(feet):
   foot=rig.pose.bones[f'Bone_{number:03d}']
   target=bpy.data.objects.new('Contact_'+str(number),None); scene.collection.objects.link(target)
   target.location=bases[i]; con=foot.constraints.new('IK'); con.target=target; con.use_stretch=False
   chain=profile['chain']; con.chain_count=chain[i] if isinstance(chain,list) else chain
   targets.append((target,con))
  rig.animation_data.action=bpy.data.actions['Move']
  for frame in range(1,32):
   t=(frame-1)/30; scene.frame_set(frame)
   for b in rig.pose.bones: b.location=idle[b.name][0]; b.rotation_quaternion=idle[b.name][1]
   root.location+=inverse@Vector((0,0,.008*(1-math.cos(4*math.pi*t))))
   for i,(target,_) in enumerate(targets):
    phase=(t+(i%2)*.5)%1; stance=.60
    if phase<=stance:
     travel=1-2*phase/stance; lift=0
    else:
     swing=(phase-stance)/(1-stance)
     travel=-1+2*(swing*swing*(3-2*swing)); lift=min(.085,stride*.55)*math.sin(math.pi*swing)**2
    target.location=bases[i]+forward*(stride*travel)+Vector((0,0,lift))
    target.keyframe_insert('location',frame=frame)
   for b in rig.pose.bones:
    b.keyframe_insert('location',frame=frame); b.keyframe_insert('rotation_quaternion',frame=frame)
  bpy.context.view_layer.objects.active=rig
  bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
  bpy.ops.nla.bake(frame_start=1,frame_end=31,step=1,only_selected=False,visual_keying=True,clear_constraints=True,use_current_action=True,bake_types={'POSE'})
  for target,_ in targets: bpy.data.objects.remove(target,do_unlink=True)
 # Mounted upper branches counter the mount's gait and react on a separate rhythm.
 rider={'HuntRider':[25,24], 'SiegeRider':[29,28,27]}.get(name,[])
 specs=json.loads((source.parent/'clips.json').read_text())
 changed=['Move'] if feet else []
 if rider: changed=[s['name'] for s in specs]
 report={'name':name,'changed':changed,'strideSourceUnitsPerSecond':2*stride/.6 if feet else 0,'riderBones':rider,'clips':[]}
 for spec in specs:
  if spec['name'] not in changed: continue
  action=bpy.data.actions[spec['name']]; rig.animation_data.action=action
  count=spec['frames']; max_ground=0; loop_start=None; loop_end=None
  for f in range(1,count+1):
   scene.frame_set(f); t=(f-1)/(count-1); phase=2*math.pi*t
   if rider:
    motion=spec['name']
    angle=.025*math.sin(phase) if motion=='Idle' else -.09*math.sin(phase+.35) if motion=='Move' else 0
    if motion=='Attack': angle=.16*math.sin(math.pi*t)**2-.25*math.exp(-((t-.54)/.11)**2)
    if motion=='Hit': angle=-.16*math.sin(math.pi*t)**2
    if motion=='Death': angle=.28*(t*t*(3-2*t))
    for i,number in enumerate(rider):
     b=rig.pose.bones[f'Bone_{number:03d}']; axis=(b.bone.matrix_local.to_3x3().inverted()@Vector((1,0,0))).normalized()
     b.rotation_mode='QUATERNION'; b.rotation_quaternion=b.rotation_quaternion@Quaternion(axis,angle*(1 if i==0 else -.45))
     b.keyframe_insert('rotation_quaternion',frame=f)
   bpy.context.view_layer.update()
   ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
   minz=min((ev.matrix_world@v.co).z for v in ev.data.vertices)
   root.location+=inverse@Vector((0,0,-minz));root.keyframe_insert('location',frame=f)
   bpy.context.view_layer.update()
   tails=[rig.matrix_world@rig.pose.bones[f'Bone_{i:03d}'].tail for i in feet]
   if f==1:loop_start=tails
   if f==count:loop_end=tails
  # Preserve a true loop after applying the upper branch's phase offset.
  if spec['loop']:
   scene.frame_set(1); pose={b.name:(b.location.copy(),b.rotation_quaternion.copy()) for b in rig.pose.bones}
   scene.frame_set(count)
   for b in rig.pose.bones:
    b.location,b.rotation_quaternion=pose[b.name];b.keyframe_insert('location',frame=count);b.keyframe_insert('rotation_quaternion',frame=count)
  scene.frame_start=1;scene.frame_end=count
  bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=rig
  bpy.ops.export_scene.fbx(filepath=str(folder/(spec['name']+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True,axis_forward='-Z',axis_up='Y')
  report['clips'].append(spec['name'])
 rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1)
 bpy.ops.wm.save_as_mainfile(filepath=str(folder/source.name))
 (folder/'clips.json').write_text(json.dumps(specs,indent=2));(folder/'contact-profile.json').write_text(json.dumps(report,indent=2))
 results=[r for r in results if r['name']!=name];results.append(report);(OUT/'results.json').write_text(json.dumps(results,indent=2));print('REFINED',name,flush=True)
