"""Full-frame final validation, including original fit_check carried equipment."""
import ast,bpy,json,sys,math,time
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
W=Path('E:/Git/ant/codex-work~'); OUT=W/'round2'; sys.path.insert(0,str(W/'blender/scripts'))
import pk
src=ast.parse((W/'blender/scripts/fit_check.py').read_text(encoding='utf-8-sig'))
for node in src.body:
 if isinstance(node,(ast.FunctionDef,ast.ClassDef)) and getattr(node,'name','') in {'cat','Rigid','pose_mats','part_trees','frames','play','targets','worn_items','combined','full'}:
  code=ast.unparse(node)
  if node.name=='full': code=code.replace('d[clip] = max(d.get(clip, 0), n)', "d[clip] = max(d.get(clip, 0), n)\n                    frame_pairs[r.name + '|' + name + '|' + clip + '|' + str(f)] = n")
  exec(code)

def run(label,path):
 global rig,ACTS,PARTS,REST,STEP,VARIANTS,MOTIONS,frame_pairs
 frame_pairs={}
 bpy.ops.wm.open_mainfile(filepath=str(path)); rig=bpy.data.objects['KemonoRig']; rig.data.pose_position='POSE'
 for pb in rig.pose.bones: pb.matrix_basis.identity()
 ACTS={n:bpy.data.actions['PK|'+n] for n in ('Tool_Dig','Tool_Mine')}
 PARTS=[o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith(('Part_Body','Part_Outfit','Part_Hair','Part_Ears','Part_Tail'))]
 for o in PARTS: o.hide_viewport=False; o.hide_render=False
 for o in bpy.context.view_layer.objects: o.hide_set(False)
 REST={b.name:rig.matrix_world@b.matrix_local for b in rig.data.bones}; STEP=1; MOTIONS=True
 VARIANTS={cat(o)[1] for o in PARTS if o.name.endswith('_ARMOR')}
 old=pk.write_json
 def capture(path,value): (OUT/'reports'/('fit-'+label+'.json')).write_text(json.dumps(value,indent=2),encoding='utf-8')
 pk.write_json=capture
 full(); pk.write_json=old
 (OUT/'reports'/('fit-frames-'+label+'.json')).write_text(json.dumps(frame_pairs,indent=2),encoding='utf-8')
run('before',OUT/'blender/baseline.blend')
run('after',OUT/'blender/pk-motions-round2.blend')
a=json.loads((OUT/'reports/fit-before.json').read_text()); b=json.loads((OUT/'reports/fit-after.json').read_text()); increases=[]
for item,parts in b['overlaps'].items():
 for part,metrics in parts.items():
  for clip,count in metrics['list'].items():
   old=a['overlaps'].get(item,{}).get(part,{}).get('list',{}).get(clip,0)
   if count>old: increases.append({'item':item,'part':part,'clip':clip,'before':old,'after':count})
fa=json.loads((OUT/'reports/fit-frames-before.json').read_text()); fb=json.loads((OUT/'reports/fit-frames-after.json').read_text())
for key,count in fb.items():
 if count>fa.get(key,0): increases.append({'per_frame_pair':key,'before':fa.get(key,0),'after':count})
(OUT/'reports/fit-delta.json').write_text(json.dumps({'increases':increases,'passed':not increases,'step':1,'frames_before':a['frames_checked'],'frames_after':b['frames_checked']},indent=2),encoding='utf-8')
print('FULL FIT INCREASES',increases,flush=True)
