from pathlib import Path
import subprocess,json,gzip,hashlib,base64
r=Path.cwd();o=r/'work/save-20261114';a=o/'restore-data';a.mkdir(exist_ok=True)
def git(*args):return subprocess.check_output(['git',*args],stderr=subprocess.DEVNULL).decode('utf8').splitlines()
prior={}
for m in sorted((r/'work').glob('save-*/manifest.json')):
 if m.parent==o:continue
 try:
  for item in json.loads(m.read_text(encoding='utf-8-sig')):prior[item['path']]=item['sha256']
 except (KeyError,TypeError):pass
scope=['Assets','AgentScripts','docs','AGENTS.md','CLAUDE.md','README.md','log.md','changelog.md','ProjectSettings','Packages']
paths=set(git('diff','--name-only','HEAD','--',*scope))
for p in git('ls-files','--others','--exclude-standard','--','Assets','AgentScripts','docs'):
 if '/Fonts/' in p:continue
 paths.add(p)
for folder in ['work/motion-actions-1113','work/furniture-wall-1114','work/building-inventory-1114','work/early-flow-1113','work/early-balance-1113','work/save-20261114/media','art/meshy-1113/staged']:
 for p in (r/folder).rglob('*'):
  if not p.is_file() or p.suffix.lower() not in ['.md','.txt','.json','.tsv','.csv','.png','.jpg','.gif','.fbx','.cs','.py']:continue
  if 'media/dining/' in p.as_posix():continue
  if any(x in p.parts for x in ['before','notion-uploads']):continue
  paths.add(p.relative_to(r).as_posix())
for item in json.loads((r/'work/motion-actions-1113/download-manifest.json').read_text(encoding='utf-8-sig')):
 p=Path(item['extracted'])
 for f in p.rglob('*.fbx'):paths.add(f.relative_to(r).as_posix())
paths.update(['graphify-out/graph.json','graphify-out/GRAPH_REPORT.md'])
records=[];stage=[];skipped=0;total=0;unique={};limit=350000
for p in sorted(paths):
 if p.endswith(('.prefab','.prefab.meta')) or not (r/p).is_file():continue
 data=(r/p).read_bytes();sha=hashlib.sha256(data).hexdigest()
 if prior.get(p)==sha:skipped+=1;continue
 rec={'path':p,'bytes':len(data),'sha256':sha};total+=len(data)
 # Archive all binary and large text. Text remains reviewable in the tree.
 try:
  s=data.decode('utf8');binary='\0' in s
 except UnicodeDecodeError:binary=True
 if len(data)>limit or binary:
  if sha not in unique:
   packed=gzip.compress(data,compresslevel=6,mtime=0);parts=[]
   for offset in range(0,len(packed),limit):
    q=a/f'group-{len(unique)//12:03d}'/(sha+'.'+str(offset//limit).zfill(3)+'.b64');q.parent.mkdir(exist_ok=True)
    q.write_bytes(base64.b64encode(packed[offset:offset+limit]));parts.append(q.relative_to(r).as_posix())
   unique[sha]=parts
  rec.update(encoding='gzip+base64',parts=unique[sha]);stage+=unique[sha]
 else:stage.append(p)
 records.append(rec)
(o/'manifest.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8')
(o/'stage-paths.json').write_text(json.dumps(sorted(set(stage)),ensure_ascii=False),encoding='utf8')
print(json.dumps({'originals':len(records),'original_MB':round(total/1e6),'parts':sum(len(v)for v in unique.values()),'unique_archived':len(unique),'unchanged_prior_archive':skipped,'stage_paths':len(set(stage)),'remote_MB':round(sum((r/p).stat().st_size for p in set(stage))/1e6)}))
