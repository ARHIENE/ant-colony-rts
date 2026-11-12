from pathlib import Path
import subprocess,json,hashlib
r=Path.cwd();o=r/'work/save-20261113'
def git(*a):return subprocess.check_output(['git',*a],stderr=subprocess.DEVNULL)
extra=['log.md','work/save-20261113/notion-evidence.json','work/save-20261113/finalize.py','work/final-motion-1113/gif-verification.json']
subprocess.run(['git','add','--',*extra],check=True,stderr=subprocess.DEVNULL)
m=json.loads((o/'manifest.json').read_text(encoding='utf8'))
for item in m:
 if 'parts' not in item:
  data=(r/item['path']).read_bytes();item.update(bytes=len(data),sha256=hashlib.sha256(data).hexdigest())
(o/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf8')
subprocess.run(['git','add','--',str(o/'manifest.json')],check=True,stderr=subprocess.DEVNULL)
paths=list(filter(None,git('diff','--cached','--name-only','-z').decode().split('\0')))
assert not any(p.endswith(('.prefab','.prefab.meta')) for p in paths)
entries=[];batch=subprocess.Popen(['git','cat-file','--batch'],stdin=subprocess.PIPE,stdout=subprocess.PIPE)
for p in paths:
 if p.startswith('work/save-20261113/archive/'):continue
 batch.stdin.write((':'+p+'\n').encode());batch.stdin.flush();header=batch.stdout.readline().decode().split();data=batch.stdout.read(int(header[2]));batch.stdout.read(1)
 entries.append(dict(path=p,mode='100644',type='blob',content=data.decode('utf8')))
batch.stdin.close();batch.wait()
chunks=[];chunk=[];length=0
for e in entries:
 n=len(json.dumps(e,ensure_ascii=False))
 if chunk and length+n>700000:chunks.append(chunk);chunk=[];length=0
 chunk.append(e);length+=n
if chunk:chunks.append(chunk)
for i,c in enumerate(chunks):(o/f'entries-{i:03d}.json').write_text(json.dumps(c,ensure_ascii=False),encoding='utf8')
tree=git('write-tree').decode().strip();archive=git('rev-parse',tree+':work/save-20261113/archive').decode().strip();groups={}
for line in git('ls-tree',archive).decode().splitlines():
 meta,name=line.split('\t');groups[name]=meta.split()[2]
result=dict(tree=tree,archive=archive,groups=groups,entry_batches=len(chunks),entries=len(entries),base_tree=git('rev-parse','HEAD^{tree}').decode().strip(),parent=git('rev-parse','HEAD').decode().strip())
(o/'tree-meta.json').write_text(json.dumps(result),encoding='utf8');print(json.dumps({k:v for k,v in result.items()if k!='groups'}))
