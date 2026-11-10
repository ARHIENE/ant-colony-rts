from pathlib import Path
import subprocess,json,gzip,hashlib
r=Path.cwd();o=r/'work/save-20261110';a=o/'restore-data-v2';a.mkdir(exist_ok=True)
def git(*args):return subprocess.check_output(['git',*args],encoding='utf8').splitlines()
paths=set(git('diff','--name-only','HEAD','--','Assets','AgentScripts','docs','README.md','log.md','changelog.md','ProjectSettings','Packages'))
for p in git('ls-files','--others','--exclude-standard','--','Assets','AgentScripts','docs'):
 if 'MeshyAnimation1106/' in p or '/Fonts/' in p:continue
 if p.endswith('Save1110LiveGif.cs'):continue
 paths.add(p)
for folder in ['art/meshy-animation-1110','art/environment-1110','work/ui-charcoal-1110','work/claude-feel-1110','work/animation-1110','work/environment-1110']:
 for p in (r/folder).rglob('*'):
  if p.is_file() and p.suffix.lower() in ['.md','.txt','.png','.gif','.fbx','.mat','.meta','.cs'] and 'before' not in p.name.lower() and 'before' not in p.parent.name.lower():paths.add(p.relative_to(r).as_posix())
paths.update(['graphify-out/graph.json','graphify-out/GRAPH_REPORT.md'])
for p in (o/'media').glob('*.gif'):paths.add(p.relative_to(r).as_posix())
paths=sorted(p for p in paths if (r/p).is_file() and not p.endswith(('.prefab','.prefab.meta')))
records=[];stage=[];limit=350_000
for p in paths:
 data=(r/p).read_bytes();sha=hashlib.sha256(data).hexdigest();rec={'path':p,'bytes':len(data),'sha256':sha}
 if len(data)>limit:
  packed=gzip.compress(data,compresslevel=6,mtime=0);parts=[]
  for offset in range(0,len(packed),limit):
   piece=packed[offset:offset+limit];q=a/(sha+'.gz.part'+str(offset//limit).zfill(3));q.write_bytes(piece);rel=q.relative_to(r).as_posix();stage.append(rel);parts.append(rel)
  rec.update({'encoding':'gzip','parts':parts})
 else:stage.append(p)
 records.append(rec)
(o/'manifest.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8')
(o/'stage-paths.json').write_text(json.dumps(sorted(set(stage)),ensure_ascii=False),encoding='utf8')
print(json.dumps({'originals':len(records),'original_MB':round(sum(x['bytes'] for x in records)/1e6),'remote_files':len(set(stage)),'remote_MB':round(sum((r/p).stat().st_size for p in set(stage))/1e6),'compressed_files':sum('parts'in x for x in records)}))

