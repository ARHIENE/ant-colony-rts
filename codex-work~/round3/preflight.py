"""Compare bounded inward _ARMOR vertex moves over all 1230 samples before committing any candidate."""
import bpy,ast,json,math,sys,time
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
W=Path('E:/Git/ant/codex-work~'); R=W/'round3'; sys.path.insert(0,str(W/'blender/scripts')); import pk
bpy.ops.wm.open_mainfile(filepath=str(R/'blender/baseline.blend')); rig=bpy.data.objects['KemonoRig']; rig.data.pose_position='POSE'; sc=bpy.context.scene; vl=bpy.context.view_layer
src=ast.parse((W/'blender/scripts/fit_check.py').read_text(encoding='utf-8-sig'))
for n in src.body:
 if isinstance(n,(ast.ClassDef,ast.FunctionDef)) and getattr(n,'name','') in {'cat','Rigid','pose_mats','targets','worn_items','combined'}: exec(compile(ast.Module(body=[n],type_ignores=[]),'fit_reuse','exec'))
PARTS=[bpy.data.objects[n] for n in ('Part_Outfit_LINEN_ARMOR','Part_Body_BASE_ARMOR')]; REST={b.name:rig.matrix_world@b.matrix_local for b in rig.data.bones}; VARIANTS={'LINEN','BASE'}
full=next(n for n in src.body if isinstance(n,ast.FunctionDef) and n.name=='full')
for n in full.body:
 if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='rep' for t in n.targets): break
 exec(compile(ast.Module(body=[n],type_ignores=[]),'fit_items','exec'))
# Restore-space plate surface for a smooth local mask. No arms, neck or bare variants are modified.
vs=[]; ps=[]
for o in bpy.data.objects:
 if o.name.startswith('Armor_') and o.get('rigid') and '_Trim_' not in o.name:
  k=len(vs); vs += [v.co.copy() for v in o.data.vertices]; ps += [[i+k for i in p.vertices] for p in o.data.polygons]
plate=BVHTree.FromPolygons(vs,ps); candidates={}; changes={}
for mm in (2,5,8,12):
 pairs=[]; changes[mm]={}
 for original in PARTS:
  o=original.copy(); o.data=original.data.copy(); sc.collection.objects.link(o); o.name='Trial'+str(mm)+'_'+original.name; o.hide_viewport=False; o.hide_set(False); moved=[]
  for v in o.data.vertices:
   if not v.groups: continue
   bone=o.vertex_groups[max(v.groups,key=lambda g:g.weight).group].name
   if bone not in ('pevis','spine','spine_01','spine_02'): continue
   p=v.co.copy(); z=p.z; fade=max(0,min(1,(z-.29)/.06,(.69-z)/.06)); distance=plate.find_nearest(p)[3]; fade*=max(0,min(1,(.04-distance)/.02))
   radial=Vector((p.x,p.y+.018,0))
   if fade<=0 or radial.length<.02: continue
   displacement=radial.normalized()*min(mm/1000*fade,radial.length*.2); v.co-=displacement
   if displacement.length>1e-8: moved.append(v.index)
  o.data.update(); pairs.append((original.name,o)); changes[mm][original.name]={'moved_vertices':moved,'max_m':max(((original.data.vertices[i].co-o.data.vertices[i].co).length for i in moved),default=0)}
 candidates[mm]=pairs
stats={mm:{'increases':0,'improvements':0,'bad_vertices':{p.name:set() for p in PARTS},'first':[],'summary':{}} for mm in candidates}; base_summary={}; samples=[]
acts={a.name.split('|',1)[1]:a for a in bpy.data.actions if a.name.startswith(('Kemono|','PK|'))}
def tree(o):
 o.hide_viewport=False; o.hide_set(False); ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get()); me=ev.to_mesh(); t=BVHTree.FromPolygons([ev.matrix_world@v.co for v in me.vertices],[p.vertices[:] for p in me.polygons]); ev.to_mesh_clear(); return t
for clip in sorted(acts):
 a=acts[clip]; rig.animation_data.action=a; rig.animation_data.action_slot=a.slots[0]
 for f in range(1,int(a.get('duration_frames',a.frame_range[1]))+1,3):
  sc.frame_set(f); vl.update(); samples.append([clip,f]); pose=pose_mats(); its={r.name:r.tree(pose) for r,_ in items}; originals={o.name:tree(o) for o in PARTS}
  baseline={(r.name,o.name):len(its[r.name].overlap(originals[o.name])) for r,ears in items for o in PARTS if o.name in targets(r.name,ears)}
  for (item,part),c in baseline.items():
   if c: base_summary.setdefault(item,{}).setdefault(part,{})[clip]=max(base_summary.get(item,{}).get(part,{}).get(clip,0),c)
  for mm,objects in candidates.items():
   trees={name:tree(o) for name,o in objects}; result=stats[mm]
   for (item,part),old in baseline.items():
    overlaps=its[item].overlap(trees[part]); count=len(overlaps)
    if count: result['summary'].setdefault(item,{}).setdefault(part,{})[clip]=max(result['summary'].get(item,{}).get(part,{}).get(clip,0),count)
    if count<old: result['improvements']+=1
    if count>old:
     result['increases']+=1
     if len(result['first'])<12: result['first'].append([clip,f,item,part,old,count])
     original=next(o for o in PARTS if o.name==part)
     for _,faceidx in overlaps:
      for i in original.data.polygons[faceidx].vertices: result['bad_vertices'][part].add(i)
 print('PREFLIGHT',clip,len(samples),{k:[v['increases'],v['improvements']] for k,v in stats.items()},flush=True)
assert len(samples)==1230
for mm,s in stats.items(): s['bad_vertices']={p:sorted(v) for p,v in s['bad_vertices'].items()}
(R/'reports/shrink-preflight.json').write_text(json.dumps({'changes':changes,'stats':stats,'baseline':base_summary,'samples':samples},indent=2),encoding='utf-8')
rig.animation_data.action=None
for pb in rig.pose.bones: pb.matrix_basis.identity()
# Store trial shapes separately, ready for inspection. No exported asset at this stage.
for mm,objects in candidates.items():
 for name,o in objects: o['trial_mm']=mm; o['original_name']=name
bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(R/'blender/shrink-trials.blend'))
print('COMPLETE', {k:[v['increases'],v['improvements']] for k,v in stats.items()},flush=True)
