from pathlib import Path
import json,hashlib,re
r=Path('E:/Git/ant'); w=r/'codex-work~'; out=w/'round2'
manifest=json.loads((w/'FBX-MANIFEST.json').read_text()); results=json.loads((out/'reports/fbx-validation.json').read_text()); fit=json.loads((out/'reports/fit-delta.json').read_text()); meta=json.loads((out/'reports/timing-events.json').read_text()); baseline=json.loads((out/'reports/baseline-tools-all-frames.json').read_text()); final=json.loads((out/'reports/final-tools-all-frames.json').read_text())
assert fit['passed'] and meta['passed']
assert not any(n in final['Tool_Mine']['hits'] for n in ('Armor_Breastplate','Armor_Breastplate_Lower'))
assert all(v['frames']<=baseline['Tool_Mine']['hits'].get(n,{}).get('frames',0) for n,v in final['Tool_Mine']['hits'].items())
assert max(final['Tool_Mine']['wrist_max_deg'].values())<=60 and final['Tool_Mine']['left_grip_error_m']<.001
for clip,d in results.items():
 assert d['passed']; p=w/'fbx'/d['target']; assert hashlib.sha256(p.read_bytes()).hexdigest()==d['sha256']
 entry={'source':p.relative_to(w).as_posix(),'target':d['target'],'bytes':d['bytes'],'sha256':d['sha256'],'meta':'Keep existing target .meta; no new .meta supplied.','round':2}
 manifest=[m for m in manifest if m['target']!=d['target']]; manifest.append(entry)
(w/'FBX-MANIFEST.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8'); (w/'FBX-PATHS.txt').write_text('\n'.join(f"{m['source']} -> {m['target']}" for m in manifest)+'\n',encoding='utf-8')
hashes=json.loads((out/'reports/original-hashes.json').read_text()); changes=[p for p,h in hashes.items() if hashlib.sha256((r/p).read_bytes()).hexdigest()!=h]; assert not changes,changes
(out/'reports/original-integrity.json').write_text(json.dumps({'checked':len(hashes),'changed':changes},indent=2),encoding='utf-8')
failures={}
for label,path in [('roll3cm',out/'reports/search-fast.log'),('nearest3cm',w/'round2-nearest/reports/search.log'),('move5cm_partial',w/'round2-dig5/reports/search.log'),('tilt_partial',w/'round2-digtilt/reports/search.log')]:
 lines=path.read_text(encoding='utf-16').splitlines(); trials=[line for line in lines if line.startswith(('TRIAL Tool_Dig 18','NO CANDIDATE Tool_Dig'))]
 failures[label]={'last_progress':trials[-1] if trials else None,'exhausted_frame18':any(line.startswith('NO CANDIDATE Tool_Dig') and "'frame': 18" in line for line in lines)}
summary={'Tool_Mine':{'passed':True,'fbx':results['Tool_Mine'],'final':final['Tool_Mine']},'Tool_Dig':{'passed':False,'applied':False,'exported':False,'baseline':baseline['Tool_Dig'],'searches':failures,'reason':'No candidate satisfied all gates at f18 in completed 3cm searches. Larger translation/tilt trials are partial, not exhaustive.'},'original_files_changed':changes}
(out/'reports/ROUND2-RESULTS.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'manifest_entries':len(manifest),'originals_unchanged':len(hashes),'dig_trials':failures},ensure_ascii=False,indent=2))
