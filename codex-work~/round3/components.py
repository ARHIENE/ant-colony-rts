import bpy,bmesh,json,collections
from pathlib import Path
R=Path('E:/Git/ant/codex-work~/round3'); bpy.ops.wm.open_mainfile(filepath=str(R/'blender/baseline.blend')); out={}
for name in ['Part_Body_BASE_ARMOR','Part_Outfit_LINEN_ARMOR']:
 o=bpy.data.objects[name]; bm=bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table(); bm.faces.ensure_lookup_table(); unseen=set(bm.verts); comps=[]
 while unseen:
  seed=unseen.pop(); stack=[seed]; verts={seed}
  while stack:
   for edge in stack.pop().link_edges:
    for v in edge.verts:
     if v in unseen: unseen.remove(v); verts.add(v); stack.append(v)
  faces=set(f for v in verts for f in v.link_faces); edges=set(e for v in verts for e in v.link_edges)
  comps.append({'vertices':[v.index for v in verts],'faces':[f.index for f in faces],'closed':all(len(e.link_faces)==2 for e in edges),'bounds':[[min(v.co[i] for v in verts),max(v.co[i] for v in verts)] for i in range(3)]})
 out[name]=comps; bm.free()
(R/'reports/components.json').write_text(json.dumps(out,indent=2),encoding='utf-8'); print({n:[{'faces':len(c['faces']),'closed':c['closed']} for c in cs] for n,cs in out.items()},flush=True)
