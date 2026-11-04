import ast,bpy,json,math
from pathlib import Path
from mathutils import Matrix,Vector
root=Path('E:/Git/ant/codex-work~'); kit=root/'blender'
source=ast.parse((kit/'scripts/build_motions.py').read_text(encoding='utf-8-sig'))
for node in source.body:
 if isinstance(node,ast.FunctionDef) and node.name in {'attach','hand_verts','clearance','wrist_bend','tool_length'}:
  if node.name=='clearance':
   code=ast.unparse(node).replace('frames[::2]','frames'); exec(code)
  else: exec(compile(ast.Module(body=[node],type_ignores=[]),'kit_solver','exec'))
 if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id in {'CLEAR_PARTS','ARMOUR_PARTS'} for t in node.targets): exec(compile(ast.Module(body=[node],type_ignores=[]),'kit_constants','exec'))
report={}; references={}
for label,path in [('before',Path('E:/Git/ant/art/production-kit-20261019/pk-motions.blend')),('after',kit/'pk-motions.blend')]:
 bpy.ops.wm.open_mainfile(filepath=str(path)); rig=bpy.data.objects['KemonoRig']; rig.data.pose_position='POSE'; scene=bpy.context.scene; vl=bpy.context.view_layer; E=bpy.data.objects
 action=bpy.data.actions['PK|Tool_Till']; rig.animation_data.action=action; rig.animation_data.action_slot=action.slots[0]
 frames=list(range(1,int(action.get('duration_frames',action.frame_range[1]))+2)); prop=attach('Tool_Hoe_Steel','Socket.Tool.R')
 hits=clearance(prop,frames,'RL'); bends={'R':0,'L':0}; err=0; lengths=[]; refs={}
 for f in frames:
  scene.frame_set(f); vl.update()
  for s in 'RL': bends[s]=max(bends[s],wrist_bend(s))
  err=max(err,(rig.matrix_world@rig.pose.bones['Socket.Tool.L'].head-prop.matrix_world@Vector(E['Tool_Hoe_Steel']['grip2'])).length)
  lengths.append(tool_length(prop)); refs[f]={b:list(rig.matrix_world@rig.pose.bones[b].head) for b in ('hand.R','hand.L','Socket.Tool.R','Socket.Tool.L')}
 report[label]={'frames':len(frames),'clearance':hits,'wrist_deg':bends,'left_grip_error_m':err,'length_drift_m':max(lengths)-min(lengths)}; references[label]=refs
 bpy.data.objects.remove(prop)
passed=report['after']['wrist_deg']['R']<60 and report['after']['left_grip_error_m']<.001 and report['after']['length_drift_m']<.00001 and all(v['frames']<=report['before']['clearance'].get(n,{}).get('frames',0) for n,v in report['after']['clearance'].items())
report['passed']=passed
(root/'reports/final-wrist-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('FINAL GATE',passed,report,flush=True)
if not passed: raise RuntimeError('Full-frame gate failed; FBX withheld')
scene.frame_start=1; scene.frame_end=int(action.get('duration_frames',action.frame_range[1]))
for obj in vl.objects: obj.select_set(False)
rig.hide_set(False); rig.hide_viewport=False; rig.select_set(True); vl.objects.active=rig
out=root/'fbx/Assets/_Project/Art/Kemono/Anim/Kemono@PK_Tool_Till.fbx'; out.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(out),object_types={'ARMATURE'},bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_selection=True,apply_scale_options='FBX_SCALE_ALL')
# Reimport all integer clip frames, not only the event frames.
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=str(out))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE'); scene=bpy.context.scene; vl=bpy.context.view_layer
act=rig.animation_data.action; offset=act.frame_range[0]-1; maxerr=0
for f,positions in references['after'].items():
 if f>36: continue
 scene.frame_set(int(f+offset)); vl.update()
 for b,p in positions.items(): maxerr=max(maxerr,(rig.matrix_world@rig.pose.bones[b].head-Vector(p)).length)
reimport={'file':str(out),'source_frames':[1,36],'import_frame_range':list(act.frame_range),'max_hand_and_socket_error_m':maxerr,'passed':maxerr<.001}
(root/'reports/fbx-reimport.json').write_text(json.dumps(reimport,indent=2),encoding='utf-8')
assert maxerr<.001,reimport
print('REIMPORT',reimport,flush=True)
