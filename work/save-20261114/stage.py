from pathlib import Path
import json,subprocess,shutil,hashlib
r=Path.cwd().resolve();o=r/'work/save-20261114';m=json.loads((o/'manifest.json').read_text(encoding='utf8'))
paths=json.loads((o/'stage-paths.json').read_text(encoding='utf8'));parts=sorted(set(p for item in m for p in item.get('parts',[])));mapping={}
for i,name in enumerate(parts):
 src=(r/name).resolve();dst=(o/'archive'/f'group-{i//24:03d}'/src.name).resolve()
 assert src.is_relative_to(o) and dst.is_relative_to(o)
 dst.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src,dst);mapping[name]=dst.relative_to(r).as_posix()
for item in m:
 if 'parts'in item:item['parts']=[mapping[p]for p in item['parts']]
 elif (r/item['path']).exists():
  data=(r/item['path']).read_bytes();item.update(bytes=len(data),sha256=hashlib.sha256(data).hexdigest())
(o/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf8')
paths=[mapping.get(p,p)for p in paths]
paths += ['work/save-20261114/'+p for p in ['manifest.json','restore.py','README.md','prepare.py','stage.py']]
paths=sorted(set(paths));assert not any(p.endswith(('.prefab','.prefab.meta'))for p in paths)
(o/'stage-null.txt').write_bytes(b'\0'.join(p.encode()for p in paths)+b'\0')
subprocess.run(['git','add','--pathspec-from-file='+str(o/'stage-null.txt'),'--pathspec-file-nul'],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
staged=subprocess.check_output(['git','diff','--cached','--name-only','-z']).decode().split('\0');staged=list(filter(None,staged));assert not any(p.endswith(('.prefab','.prefab.meta'))for p in staged)
entries=[]
for p in staged:
 if p.startswith('work/save-20261114/archive/'):continue
 data=subprocess.check_output(['git','show',':'+p]);s=data.decode('utf8');assert '\0'not in s
 entries.append(dict(path=p,mode='100644',type='blob',content=s))
(o/'entries.json').write_text(json.dumps(entries,ensure_ascii=False),encoding='utf8')
groups=sorted((o/'archive').glob('group-*'))
(o/'groups.json').write_text(json.dumps([g.name for g in groups]),encoding='utf8')
print(json.dumps({'staged':len(staged),'text_entries':len(entries),'groups':len(groups),'tree':subprocess.check_output(['git','write-tree']).decode().strip()}))
