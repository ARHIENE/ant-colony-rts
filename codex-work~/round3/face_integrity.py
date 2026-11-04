import bpy,json
from pathlib import Path
R=Path('E:/Git/ant/codex-work~/round3'); names=('Part_Body_BASE_ARMOR','Part_Outfit_LINEN_ARMOR')
bpy.ops.wm.open_mainfile(filepath=str(R/'blender/baseline.blend')); coords={n:[v.co.copy() for v in bpy.data.objects[n].data.vertices] for n in names}
bpy.ops.wm.open_mainfile(filepath=str(R/'blender/candidate.blend')); rig=bpy.data.objects['KemonoRig']; rig.data.pose_position='POSE'; sc=bpy.context.scene; vl=bpy.context.view_layer; pairs=[]
for n in names:
 o=bpy.data.objects[n]; old=o.copy(); old.data=o.data.copy(); sc.collection.objects.link(old); old.name='Reference_'+n
 for v,p in zip(old.data.vertices,coords[n]): v.co=p
 old.data.update()
 for x in (o,old): x.hide_viewport=False; x.hide_set(False)
 pairs.append((n,old,o))
acts={a.name.split('|',1)[1]:a for a in bpy.data.actions if a.name.startswith(('Kemono|','PK|'))}; flips=[]; checked=0; area_min_ratio=1
for clip in sorted(acts):
 a=acts[clip]; rig.animation_data.action=a; rig.animation_data.action_slot=a.slots[0]
 for f in range(1,int(a.get('duration_frames',a.frame_range[1]))+1,3):
  sc.frame_set(f); vl.update(); checked+=1; dg=bpy.context.evaluated_depsgraph_get()
  for n,old,new in pairs:
   ev0=old.evaluated_get(dg); ev1=new.evaluated_get(dg); m0=ev0.to_mesh(); m1=ev1.to_mesh()
   for p,q in zip(m0.polygons,m1.polygons):
    if p.area<1e-10: continue
    area_min_ratio=min(area_min_ratio,q.area/p.area)
    if p.normal.dot(q.normal)<0 or q.area<1e-10: flips.append([clip,f,n,p.index,p.area,q.area])
   ev0.to_mesh_clear(); ev1.to_mesh_clear()
 print('TOPOLOGY',clip,checked,len(flips),flush=True)
assert checked==1230
(R/'reports/face-integrity.json').write_text(json.dumps({'samples':checked,'flips':flips,'min_area_ratio':area_min_ratio,'passed':not flips},indent=2),encoding='utf-8')
print('TOPOLOGY COMPLETE',len(flips),area_min_ratio,flush=True)
