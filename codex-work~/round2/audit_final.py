import ast,bpy,json,math
from pathlib import Path
from mathutils import Matrix,Vector
W=Path('E:/Git/ant/codex-work~'); R=W/'round2'; source=ast.parse((W/'blender/scripts/build_motions.py').read_text(encoding='utf-8-sig'))
for node in source.body:
 if isinstance(node,ast.FunctionDef) and node.name in {'attach','hand_verts','clearance','wrist_bend','tool_length'}:
  code=ast.unparse(node)
  if node.name=='clearance': code=code.replace('frames[::2]','frames').replace('fr[:6]','fr')
  exec(code)
 if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id in {'CLEAR_PARTS','ARMOUR_PARTS'} for t in node.targets): exec(compile(ast.Module(body=[node],type_ignores=[]),'constants','exec'))
bpy.ops.wm.open_mainfile(filepath=str(R/'blender/pk-motions-round2.blend')); rig=bpy.data.objects['KemonoRig']; rig.data.pose_position='POSE'; E=bpy.data.objects; scene=bpy.context.scene; vl=bpy.context.view_layer; result={}
for clip,tool in [('Tool_Dig','Tool_Shovel_Steel'),('Tool_Mine','Tool_Pickaxe_Steel')]:
 a=bpy.data.actions['PK|'+clip]; rig.animation_data.action=a; rig.animation_data.action_slot=a.slots[0]; frames=list(range(1,int(a['duration_frames'])+2)); prop=attach(tool,'Socket.Tool.R'); hits=clearance(prop,frames,'RL'); bend={s:0 for s in 'RL'}; grip=0
 for f in frames:
  scene.frame_set(f); vl.update()
  for s in 'RL': bend[s]=max(bend[s],wrist_bend(s))
  grip=max(grip,(rig.matrix_world@rig.pose.bones['Socket.Tool.L'].head-prop.matrix_world@Vector(E[tool]['grip2'])).length)
 result[clip]={'frames':len(frames),'hits':hits,'wrist_max_deg':bend,'left_grip_error_m':grip}; bpy.data.objects.remove(prop)
(R/'reports/final-tools-all-frames.json').write_text(json.dumps(result,indent=2),encoding='utf-8'); print(result,flush=True)
