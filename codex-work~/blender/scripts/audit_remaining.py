import ast,bpy,json,math,hashlib
from pathlib import Path
from mathutils import Matrix,Vector
w=Path('E:/Git/ant/codex-work~'); kit=w/'blender'
source=ast.parse((kit/'scripts/build_motions.py').read_text(encoding='utf-8-sig'))
for node in source.body:
 if isinstance(node,ast.FunctionDef) and node.name in {'attach','hand_verts','clearance','wrist_bend','tool_length'}: exec(compile(ast.Module(body=[node],type_ignores=[]),'kit_solver','exec'))
 if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id in {'CLEAR_PARTS','ARMOUR_PARTS'} for t in node.targets): exec(compile(ast.Module(body=[node],type_ignores=[]),'kit_constants','exec'))
def signatures():
 result={}
 for a in bpy.data.actions:
  curves=[]
  for layer in a.layers:
   for strip in layer.strips:
    for bag in strip.channelbags:
     for fc in bag.fcurves:
      curves.append((fc.data_path,fc.array_index,[(list(k.co),k.interpolation,list(k.handle_left),list(k.handle_right)) for k in fc.keyframe_points]))
  result[a.name]=hashlib.sha256(json.dumps(curves,sort_keys=True).encode()).hexdigest()
 return result
bpy.ops.wm.open_mainfile(filepath='E:/Git/ant/art/production-kit-20261019/pk-motions.blend'); before=signatures()
bpy.ops.wm.open_mainfile(filepath=str(kit/'pk-motions.blend')); after=signatures()
changed=[n for n in before if before[n]!=after.get(n)]; print('CHANGED',changed,flush=True)
assert changed==['PK|Tool_Till'],changed
(w/'reports/action-integrity.json').write_text(json.dumps({'actions':len(before),'changed':changed,'added':list(set(after)-set(before))},indent=2),encoding='utf-8')
rig=bpy.data.objects['KemonoRig']; rig.data.pose_position='POSE'; scene=bpy.context.scene; vl=bpy.context.view_layer; E=bpy.data.objects
spec=json.loads(Path('E:/Git/ant/art/production-kit-20261019/previews/motion-report.json').read_text(encoding='utf-8'))['clips']; report={}
for name,c in spec.items():
 if not (c.get('prop') or '').startswith('Tool_'): continue
 pid=c['prop']+'_Steel'; a=E.get(pid)
 if a is None: continue
 action=bpy.data.actions['PK|'+name]; rig.animation_data.action=action; rig.animation_data.action_slot=action.slots[0]
 prop=attach(pid,'Socket.Tool.R'); frames=list(range(1,int(action.get('duration_frames',action.frame_range[1]))+2))
 report[name]={'frames':len(frames),'clearance':clearance(prop,frames,'RL' if c.get('two_hand') else 'R')}
 print('TOOL',name,report[name],flush=True); bpy.data.objects.remove(prop)
(w/'reports/remaining-tool-clearance.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
