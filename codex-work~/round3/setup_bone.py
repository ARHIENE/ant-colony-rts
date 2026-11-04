from pathlib import Path
w=Path('E:/Git/ant/codex-work~'); r=w/'round3'; s=(r/'preflight.py').read_text(encoding='utf-8-sig')
s=s.replace('for mm in (2,5,8,12):','for mm in (2,5,8):')
s=s.replace('plate=BVHTree.FromPolygons(vs,ps); candidates={}; changes={}', '''plate=BVHTree.FromPolygons(vs,ps); candidates={}; changes={}
body=bpy.data.objects['Part_Body_BASE_ARMOR']
skin=BVHTree.FromPolygons([v.co.copy() for v in body.data.vertices],[p.vertices[:] for p in body.data.polygons])
bracers={side: bpy.data.objects['Armor_Bracer_'+side] for side in ('L','R')}
bracer_trees={side:BVHTree.FromPolygons([v.co.copy() for v in ob.data.vertices],[p.vertices[:] for p in ob.data.polygons]) for side,ob in bracers.items()}
''')
a=s.index('  for v in o.data.vertices:'); b=s.index('  o.data.update();',a)
s=s[:a]+'''  for v in o.data.vertices:
   if original.name!='Part_Outfit_LINEN_ARMOR' or not v.groups: continue
   bone=o.vertex_groups[max(v.groups,key=lambda g:g.weight).group].name
   if bone not in ('forearm.L','forearm.R'): continue
   p=v.co.copy(); bn=rig.data.bones[bone]; axis=(bn.tail_local-bn.head_local).normalized()
   center=bn.head_local+axis*(p-bn.head_local).dot(axis); radial=p-center
   distance=bracer_trees[bone[-1]].find_nearest(p)[3]; fade=max(0,min(1,(.03-distance)/.015))
   q,normal,_,gap=skin.find_nearest(p)
   if fade<=0 or radial.length<.005 or q is None: continue
   displacement=radial.normalized()*max(0,min(mm/1000*fade,radial.length*.5,gap-.0015)); v.co-=displacement
   if displacement.length>1e-8: moved.append(v.index)
'''+s[b:]
s=s.replace('shrink-preflight.json','bone-preflight.json').replace('shrink-trials.blend','bone-trials.blend').replace("print('PREFLIGHT'", "print('BONE'")
(r/'preflight-bone.py').write_text(s,encoding='utf-8')
