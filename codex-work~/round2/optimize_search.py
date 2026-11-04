from pathlib import Path
import ast,json
w=Path('codex-work~/round2'); text=(w/'reports/search.log').read_text(encoding='utf-16'); presets={}
for line in text.splitlines():
 if line.startswith('FOUND '):
  _,clip,body=line.split(' ',2); d=ast.literal_eval(body); presets.setdefault(clip,{})[str(d['frame'])]=d
(w/'reports/presets.json').write_text(json.dumps(presets,indent=2),encoding='utf-8')
s=(w/'repair.py').read_text(encoding='utf-8-sig')
s=s.replace('def part_trees():\n', "CACHE={}\nPRESETS=json.loads((OUT/'reports/presets.json').read_text())\ndef part_trees():\n")
s=s.replace(' for o in parts:\n  ev=o.evaluated_get', " for o in parts:\n  if o.name in CACHE:\n   trees[o.name]=CACHE[o.name]\n   if o.name in CLEAR_PARTS: tooltrees[o.name]=CACHE[o.name]\n   continue\n  ev=o.evaluated_get")
s=s.replace('  ev.to_mesh_clear()\n return trees,tooltrees', "  ev.to_mesh_clear()\n  if o.name.startswith(('Part_Hair','Part_Ears','Part_Tail')): CACHE[o.name]=trees[o.name]\n return trees,tooltrees")
s=s.replace('  scene.frame_set(f); vl.update(); matrices=', '  CACHE.clear(); scene.frame_set(f); vl.update(); matrices=')
s=s.replace("      candidates.append((c,off,angle,hr,hl,P))", "      preset=PRESETS.get(clip,{}).get(str(f))\n      if preset and (off-Vector(preset['offset_m'])).length<.00001 and angle==preset['roll_deg']: c=-100\n      candidates.append((c,off,angle,hr,hl,P))")
s=s.replace("   hnew,wnew=contacts(prop)", """   # Hair/ears/tail do not depend on these six arm bones; reject against cached static geometry first.
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
   hnew,wnew=contacts(prop)""")
s=s.replace('  scene.frame_set(f); vl.update(); h,wear=contacts(prop);', '  CACHE.clear(); scene.frame_set(f); vl.update(); h,wear=contacts(prop);')
s=s.replace("   if max(wrist_bend(s) for s in 'RL')>60:","   if trials%50==0: print('TRIAL',clip,f,trials,rejected,flush=True)\n   if max(wrist_bend(s) for s in 'RL')>60:")
(w/'repair_fast.py').write_text(s,encoding='utf-8')
print('Preserved', {k:list(v) for k,v in presets.items()})
