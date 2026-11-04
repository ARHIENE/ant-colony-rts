from pathlib import Path
p=Path('codex-work~/round2/validate_armour.py'); s=p.read_text(); s=s.replace("if isinstance(node,(ast.FunctionDef,ast.ClassDef)) and getattr(node,'name','') in {'cat','Rigid','pose_mats','part_trees','frames','play','targets','worn_items','combined','full'}: exec(compile(ast.Module(body=[node],type_ignores=[]),'fit_check_reuse','exec'))", """if isinstance(node,(ast.FunctionDef,ast.ClassDef)) and getattr(node,'name','') in {'cat','Rigid','pose_mats','part_trees','frames','play','targets','worn_items','combined','full'}:
  code=ast.unparse(node)
  if node.name=='full': code=code.replace('d[clip] = max(d.get(clip, 0), n)', \"d[clip] = max(d.get(clip, 0), n)\\n                    frame_pairs[r.name + '|' + name + '|' + clip + '|' + str(f)] = n\")
  exec(code)""")
s=s.replace('global rig,ACTS,PARTS,REST,STEP,VARIANTS,MOTIONS','global rig,ACTS,PARTS,REST,STEP,VARIANTS,MOTIONS,frame_pairs\n frame_pairs={}')
s=s.replace('full(); pk.write_json=old', "full(); pk.write_json=old\n (OUT/'reports'/('fit-frames-'+label+'.json')).write_text(json.dumps(frame_pairs,indent=2),encoding='utf-8')")
s=s.replace("(OUT/'reports/fit-delta.json').write_text", """fa=json.loads((OUT/'reports/fit-frames-before.json').read_text()); fb=json.loads((OUT/'reports/fit-frames-after.json').read_text())
for key,count in fb.items():
 if count>fa.get(key,0): increases.append({'per_frame_pair':key,'before':fa.get(key,0),'after':count})
(OUT/'reports/fit-delta.json').write_text""")
p.write_text(s)
