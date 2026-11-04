from pathlib import Path
import json,hashlib,csv,subprocess
r=Path('E:/Git/ant'); w=r/'codex-work~'
hashes=json.loads((w/'reports/original-hashes.json').read_text()); changed=[p for p,h in hashes.items() if hashlib.sha256((r/p).read_bytes()).hexdigest()!=h]
assert not changed,changed
(w/'reports/original-integrity.json').write_text(json.dumps({'checked_files':len(hashes),'changed':changed},indent=2),encoding='utf-8')
baseline=json.loads((w/'blender/previews/fit-report-motions.json').read_text()); final=json.loads((w/'blender/previews/final-till-fit.json').read_text())
items=set(baseline['overlaps'])|set(final['overlaps'])
merged={}
for item in sorted(items):
 parts=set(baseline['overlaps'].get(item,{}))|set(final['overlaps'].get(item,{}))
 for part in sorted(parts):
  clips=dict(baseline['overlaps'].get(item,{}).get(part,{}).get('list',{})); clips.pop('Tool_Till',None)
  clips.update(final['overlaps'].get(item,{}).get(part,{}).get('list',{}))
  if clips: merged.setdefault(item,{})[part]={'clips':len(clips),'worst_pairs':max(clips.values()),'list':clips}
result={'frames_checked':baseline['frames_checked'],'step':3,'method':'Original complete run plus final Tool_Till isolated replacement (12 sampled frames). Other 87 actions unchanged by curve signature. Not an all-frame proof.','overlaps':merged}
(w/'reports/remaining-armor.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
rows=[{'item':i,'part':p,'clips':v['clips'],'worst_pairs':v['worst_pairs']} for i,parts in merged.items() for p,v in parts.items()]
with (w/'reports/remaining-armor.csv').open('w',encoding='utf-8-sig',newline='') as f:
 writer=csv.DictWriter(f,fieldnames=['item','part','clips','worst_pairs']); writer.writeheader(); writer.writerows(rows)
binary=w/'fbx/Assets/_Project/Art/Kemono/Anim/Kemono@PK_Tool_Till.fbx'
manifest=[{'source':binary.relative_to(w).as_posix(),'target':'Assets/_Project/Art/Kemono/Anim/Kemono@PK_Tool_Till.fbx','bytes':binary.stat().st_size,'sha256':hashlib.sha256(binary.read_bytes()).hexdigest(),'meta':'Keep existing target .meta; no new .meta supplied.'}]
(w/'FBX-MANIFEST.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(w/'FBX-PATHS.txt').write_text('\n'.join(f"{m['source']} -> {m['target']}" for m in manifest)+'\n',encoding='utf-8')
print('Original integrity:',len(hashes),'unchanged. Armor:',len(rows),'remaining pairs. FBX:',manifest)
