from pathlib import Path
w=Path('E:/Git/ant/codex-work~'); r=w/'round3'; s=(r/'preflight.py').read_text(encoding='utf-8-sig')
s=s.replace('for mm in (2,5,8,12):','for mm in (2,):')
s=s.replace('plate=BVHTree.FromPolygons(vs,ps); candidates={}; changes={}', "plate=BVHTree.FromPolygons(vs,ps); candidates={}; changes={}\nprior=json.loads((R/'reports/shrink-preflight.json').read_text())")
mark='  o.data.update(); pairs.append((original.name,o));'
s=s.replace(mark,'''  # Keep every pre-existing open boundary exactly fixed; revert vertices implicated in any increase.
  edge_counts={}
  for face in original.data.polygons:
   ids=list(face.vertices)
   for a,b in zip(ids,ids[1:]+ids[:1]):
    key=tuple(sorted((a,b))); edge_counts[key]=edge_counts.get(key,0)+1
  fixed=set(prior['stats'][str(mm)]['bad_vertices'][original.name])
  for edge,count in edge_counts.items():
   if count==1: fixed.update(edge)
  for i in fixed: o.data.vertices[i].co=original.data.vertices[i].co
  moved=[i for i in moved if (original.data.vertices[i].co-o.data.vertices[i].co).length>1e-8]
'''+mark)
s=s.replace('shrink-preflight.json\').write_text','rollback-preflight.json\').write_text').replace('shrink-trials.blend','rollback-trials.blend').replace("print('PREFLIGHT'", "print('ROLLBACK'")
(r/'preflight-rollback.py').write_text(s,encoding='utf-8')
