"""Round 2: bounded path/roll search, exact framewise tool and worn-armour gates."""
import ast,bpy,json,math,sys
from pathlib import Path
from mathutils import Matrix,Vector
from mathutils.bvhtree import BVHTree
W=Path('E:/Git/ant/codex-work~'); OUT=W/'round2'; KIT=W/'blender'
source=ast.parse((KIT/'scripts/build_motions.py').read_text(encoding='utf-8-sig'))
for node in source.body:
 if isinstance(node,ast.FunctionDef) and node.name in {'arm_cost','solve_arm','attach','hand_verts','clearance','wrist_bend','tool_length'}:
  code=ast.unparse(node)
  if node.name=='clearance': code=code.replace('frames[::2]','frames')
  exec(code)
 if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id in {'CLEAR_PARTS','ARMOUR_PARTS'} for t in node.targets): exec(compile(ast.Module(body=[node],type_ignores=[]),'kit_constants','exec'))
RADIUS=float(sys.argv[sys.argv.index('--')+1]) if '--' in sys.argv else .03

def setup():
 global rig,vl,scene,E,parts,armours,skip,targets
 rig=bpy.data.objects['KemonoRig']; rig.data.pose_position='POSE'; vl=bpy.context.view_layer; scene=bpy.context.scene; E=bpy.data.objects
 parts=[o for o in E if o.type=='MESH' and o.name.startswith(('Part_Body','Part_Outfit','Part_Hair','Part_Ears','Part_Tail'))]
 variants={o.name.split('_')[2] for o in parts if o.name.endswith('_ARMOR')}
 targets=[o for o in parts if not (o.name.split('_')[1] in ('Body','Outfit','Hair') and o.name.split('_')[2] in variants and not o.name.endswith('_ARMOR'))]
 armours=[o for o in bpy.data.collections['Equipment'].objects if o.get('bone') and not o.get('soft') and '_Trim_' not in o.name]
 for o in parts: o.hide_viewport=False; o.hide_set(False)
 skip=hand_verts(E['Part_Body_BASE_ARMOR'],'RL')

CACHE={}
PRESETS=json.loads((OUT/'reports/presets.json').read_text())
def part_trees():
 dg=bpy.context.evaluated_depsgraph_get(); trees={}; tooltrees={}
 for o in parts:
  if o.name in CACHE:
   trees[o.name]=CACHE[o.name]
   if o.name in CLEAR_PARTS: tooltrees[o.name]=CACHE[o.name]
   continue
  ev=o.evaluated_get(dg); me=ev.to_mesh(); vs=[ev.matrix_world@v.co for v in me.vertices]; ps=[p.vertices[:] for p in me.polygons]
  trees[o.name]=BVHTree.FromPolygons(vs,ps)
  if o.name in CLEAR_PARTS:
   tooltrees[o.name]=BVHTree.FromPolygons(vs,[p for p in ps if not(o.name=='Part_Body_BASE_ARMOR' and any(i in skip for i in p))])
  ev.to_mesh_clear()
  if o.name.startswith(('Part_Hair','Part_Ears','Part_Tail')): CACHE[o.name]=trees[o.name]
 return trees,tooltrees

def plate_trees():
 result={}
 for o in armours:
  M=rig.matrix_world@rig.pose.bones[o['bone']].matrix@rig.data.bones[o['bone']].matrix_local.inverted()@rig.matrix_world.inverted()@o.matrix_world
  result[o.name]=BVHTree.FromPolygons([M@v.co for v in o.data.vertices],[p.vertices[:] for p in o.data.polygons])
 return result

def contacts(prop):
 trees,tooltrees=part_trees(); plates=plate_trees()
 tt=BVHTree.FromPolygons([prop.matrix_world@v.co for v in prop.data.vertices],[p.vertices[:] for p in prop.data.polygons])
 hits={n:len(tt.overlap(t)) for n,t in {**tooltrees,**{n:t for n,t in plates.items() if n in ARMOUR_PARTS}}.items()}
 worn={}
 for n,t in plates.items():
  for o in targets:
   if n.startswith(('Helm_','HelmTrim')) and o.name.startswith('Part_Tail'): continue
   ears=E[n].get('ears')
   if ears and o.name.startswith('Part_Ears') and o.name.split('_')[2]!=ears: continue
   count=len(t.overlap(trees[o.name]))
   if count: worn[n+'|'+o.name]=count
 return {n:c for n,c in hits.items() if c},worn

def signature():
 result={}
 for a in bpy.data.actions:
  values=[]
  for l in a.layers:
   for s in l.strips:
    for b in s.channelbags:
     for fc in b.fcurves: values.append((fc.data_path,fc.array_index,[(list(k.co),k.interpolation,list(k.handle_left),list(k.handle_right)) for k in fc.keyframe_points]))
  result[a.name]=values
 return result

bpy.ops.wm.open_mainfile(filepath=str(OUT/'blender/baseline.blend')); setup(); sig0=signature(); reports={}; accepted=[]
for clip,tool,targetnames in [('Tool_Dig','Tool_Shovel_Steel',{'Part_Hair_BOB_00'}),('Tool_Mine','Tool_Pickaxe_Steel',{'Armor_Breastplate','Armor_Breastplate_Lower'})]:
 original=bpy.data.actions['PK|'+clip]; rig.animation_data.action=original; rig.animation_data.action_slot=original.slots[0]
 frames=list(range(1,int(original.get('duration_frames',original.frame_range[1]))+2)); prop=attach(tool,'Socket.Tool.R')
 chain=[b+'.'+s for s in 'RL' for b in ('upper_arm','forearm','hand')]
 hand_in=rig.data.bones['Socket.Tool.R'].matrix_local.inverted()@rig.data.bones['hand.R'].matrix_local
 before={}; poses={}; log=[]; failed=[]; lastoff=Vector((0,0,0))
 for f in frames:
  CACHE.clear(); scene.frame_set(f); vl.update(); matrices={n:rig.pose.bones[n].matrix.copy() for n in chain}
  hits,wear=contacts(prop); before[f]={'tool':hits,'wear':wear,'wrist':{s:wrist_bend(s) for s in 'RL'}}
  need=any(hits.get(n,0) for n in targetnames)
  if not need:
   poses[f]=matrices; log.append({'frame':f,'changed':False}); continue
  print('SEARCH',clip,f,hits,flush=True)
  socket=rig.pose.bones['Socket.Tool.R'].matrix.copy(); left=rig.pose.bones['hand.L'].matrix.copy(); shoulders={s:rig.pose.bones['upper_arm.'+s].head.copy() for s in 'RL'}
  P0=prop.matrix_world.copy(); candidates=[]
  for x in range(-round(RADIUS*100),round(RADIUS*100)+1):
   for y in range(-round(RADIUS*100),round(RADIUS*100)+1):
    for z in range(-round(RADIUS*100),round(RADIUS*100)+1):
     off=Vector((x*.01,y*.01,z*.01))
     if off.length>RADIUS+.000001: continue
     for angle in range(-45,46,15):
      P=Matrix.Translation(off)@socket@Matrix.Rotation(math.radians(angle),4,'Y'); hr=P@hand_in; hl=P@socket.inverted()@left
      c=max(arm_cost('R',hr,shoulders['R']),arm_cost('L',hl,shoulders['L']))+math.radians(1)*off.length*100+math.radians(.02)*abs(angle)+math.radians(.1)*(off-lastoff).length*100
      preset=PRESETS.get(clip,{}).get(str(f))
      if preset and (off-Vector(preset['offset_m'])).length<.00001 and angle==preset['roll_deg']: c=-100
      candidates.append((c,off,angle,hr,hl,P))
  # Static head/torso quick gate; moving arms and all armour are checked after solving.
  pt,toolpt=part_trees(); plate=plate_trees(); static={n:({**toolpt,**plate})[n] for n in targetnames}; tv=[v.co.copy() for v in prop.data.vertices]; tp=[p.vertices[:] for p in prop.data.polygons]
  found=None; trials=0; rejected={}
  for c,off,angle,hr,hl,P in sorted(candidates,key=lambda v:v[0]):
   delta=rig.matrix_world@P@socket.inverted()@rig.matrix_world.inverted(); tt=BVHTree.FromPolygons([delta@P0@v for v in tv],tp)
   if any(tt.overlap(t) for t in static.values()): continue
   for n,m in matrices.items(): rig.pose.bones[n].matrix=m; vl.update()
   solve_arm('R',hr); solve_arm('L',hl); trials+=1
   if trials%50==0: print('TRIAL',clip,f,trials,rejected,flush=True)
   if max(wrist_bend(s) for s in 'RL')>60: rejected['wrist']=rejected.get('wrist',0)+1; continue
   # Hair/ears/tail do not depend on these six arm bones; reject against cached static geometry first.
   pl=plate_trees(); bad=False
   for plate_name,tree in pl.items():
    for o in targets:
     if o.name not in CACHE: continue
     if plate_name.startswith(('Helm_','HelmTrim')) and o.name.startswith('Part_Tail'): continue
     ears=E[plate_name].get('ears')
     if ears and o.name.startswith('Part_Ears') and o.name.split('_')[2]!=ears: continue
     if len(tree.overlap(CACHE[o.name]))>wear.get(plate_name+'|'+o.name,0): bad=True; break
    if bad: break
   if bad: rejected['static_armour']=rejected.get('static_armour',0)+1; continue
   hnew,wnew=contacts(prop)
   if any(hnew.get(n,0) for n in targetnames) or any(c>hits.get(n,0) for n,c in hnew.items()): rejected['tool']=rejected.get('tool',0)+1; continue
   increases={n:c for n,c in wnew.items() if c>wear.get(n,0)}
   if increases: rejected['armour']=rejected.get('armour',0)+1; continue
   err=(rig.matrix_world@rig.pose.bones['Socket.Tool.L'].head-prop.matrix_world@Vector(E[tool]['grip2'])).length
   if err>=.001: rejected['grip']=rejected.get('grip',0)+1; continue
   found={n:rig.pose.bones[n].matrix.copy() for n in chain}; lastoff=off
   log.append({'frame':f,'changed':True,'offset_m':list(off),'roll_deg':angle,'trials':trials,'rejected':rejected}); print('FOUND',clip,log[-1],flush=True); break
  if found is None:
   failed.append(f); poses[f]=matrices; log.append({'frame':f,'changed':False,'failed':True,'trials':trials,'rejected':rejected}); print('NO CANDIDATE',clip,log[-1],flush=True)
  else: poses[f]=found
 candidate=original.copy(); rig.animation_data.action=candidate; rig.animation_data.action_slot=candidate.slots[0]
 for f in frames:
  scene.frame_set(f)
  for n,m in poses[f].items():
   pb=rig.pose.bones[n]; pb.matrix=m; vl.update(); pb.keyframe_insert('rotation_euler',frame=f,group=n)
 after={}; grip=0; lengths=[]; maxbend={s:0 for s in 'RL'}; increases=[]
 for f in frames:
  CACHE.clear(); scene.frame_set(f); vl.update(); h,wear=contacts(prop); after[f]={'tool':h,'wear':wear}
  if any(h.get(n,0) for n in targetnames): increases.append({'frame':f,'reason':'target remains'})
  for label,current,old in [('tool',h,before[f]['tool']),('armour',wear,before[f]['wear'])]:
   for n,count in current.items():
    if count>old.get(n,0): increases.append({'frame':f,'reason':label,'pair':n,'before':old.get(n,0),'after':count})
  for s in 'RL': maxbend[s]=max(maxbend[s],wrist_bend(s))
  grip=max(grip,(rig.matrix_world@rig.pose.bones['Socket.Tool.L'].head-prop.matrix_world@Vector(E[tool]['grip2'])).length); lengths.append(tool_length(prop))
 keep=not failed and not increases and max(maxbend.values())<=60 and grip<.001 and max(lengths)-min(lengths)<.00001
 reports[clip]={'accepted':keep,'radius_m':RADIUS,'frames':len(frames),'search':log,'failed_frames':failed,'increases':increases,'wrist_max_deg':maxbend,'left_grip_error_m':grip,'length_drift_m':max(lengths)-min(lengths),'before':before,'after':after}
 if keep:
  bpy.data.actions.remove(original); candidate.name='PK|'+clip; candidate.use_fake_user=True; accepted.append(clip)
 else:
  rig.animation_data.action=original; bpy.data.actions.remove(candidate)
 bpy.data.objects.remove(prop)
 (OUT/'reports/search-results.json').write_text(json.dumps(reports,indent=2),encoding='utf-8'); print('RESULT',clip,keep,'failed',failed,'increases',len(increases),maxbend,grip,flush=True)
rig.animation_data.action=None
for pb in rig.pose.bones: pb.matrix_basis.identity()
if accepted: bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'blender/pk-motions-round2.blend'))
sig1=signature(); changed=[n for n in sig0 if sig0[n]!=sig1.get(n)]
(OUT/'reports/action-integrity.json').write_text(json.dumps({'changed':changed,'accepted':accepted,'actions':len(sig0)},indent=2),encoding='utf-8')
print('COMPLETE',accepted,changed,flush=True)
