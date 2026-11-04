"""Materialize only verified rollback coordinates into a clean baseline copy."""
import bpy,json,hashlib,struct,sys
from pathlib import Path
W=Path('E:/Git/ant/codex-work~'); R=W/'round3'
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['rollback-trials.blend','2','candidate']
trial,amount,label=args; amount=int(amount); coords={}
bpy.ops.wm.open_mainfile(filepath=str(R/'blender'/trial))
for o in bpy.data.objects:
 if o.get('trial_mm')==amount: coords[o['original_name']]=[v.co.copy() for v in o.data.vertices]
def mesh_state(o):
 return {'v':[[*v.co,[(g.group,g.weight) for g in v.groups]] for v in o.data.vertices],'p':[list(p.vertices) for p in o.data.polygons],'groups':[g.name for g in o.vertex_groups],'uv':[[list(x.uv) for x in layer.data] for layer in o.data.uv_layers],'materials':[m.name if m else None for m in o.data.materials]}
def states(): return {o.name:hashlib.sha256(json.dumps(mesh_state(o),sort_keys=True).encode()).hexdigest() for o in bpy.data.objects if o.type=='MESH'}
bpy.ops.wm.open_mainfile(filepath=str(R/'blender/baseline.blend')); before=states(); changes={}; bare={n:h for n,h in before.items() if n.startswith(('Part_Body','Part_Outfit')) and not n.endswith('_ARMOR')}
for name,vs in coords.items():
 o=bpy.data.objects[name]; old=[v.co.copy() for v in o.data.vertices]; edge_count={}
 for p in o.data.polygons:
  ids=list(p.vertices)
  for a,b in zip(ids,ids[1:]+ids[:1]):
   key=tuple(sorted((a,b))); edge_count[key]=edge_count.get(key,0)+1
 boundary={v for edge,n in edge_count.items() if n==1 for v in edge}
 assert all((vs[i]-old[i]).length<1e-9 for i in boundary),name+' changed boundary'
 o.data=o.data.copy()
 for v,co in zip(o.data.vertices,vs): v.co=co
 o.data.update(); changes[name]={'moved':sum((p-q).length>1e-8 for p,q in zip(old,vs)),'max_m':max((p-q).length for p,q in zip(old,vs)),'vertices':len(vs),'faces':len(o.data.polygons),'boundary_vertices':len(boundary),'boundary_moved':0}
after=states(); changed=[n for n in before if before[n]!=after[n]]
assert all(n in coords and n.endswith('_ARMOR') for n in changed),changed
assert all(after[n]==h for n,h in bare.items())
rig=bpy.data.objects['KemonoRig']; rig.animation_data.action=None
for b in rig.pose.bones: b.matrix_basis.identity()
rig.data.pose_position='REST'; bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(R/'blender'/(label+'.blend')))
(R/'reports'/(label+'-integrity.json')).write_text(json.dumps({'changed_meshes':changed,'changes':changes,'mesh_count':len(before),'unchanged_meshes':len(before)-len(changed),'bare_preserved':bare,'before':before,'after':after},indent=2),encoding='utf-8')
print('MATERIALIZED',label,changed,changes,flush=True)
