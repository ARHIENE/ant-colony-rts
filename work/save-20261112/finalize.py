from pathlib import Path
import subprocess,json,hashlib
r=Path.cwd();o=r/'work/save-20261112'
def git(*a):return subprocess.check_output(['git',*a],stderr=subprocess.DEVNULL)
paths=list(filter(None,git('diff','--cached','--name-only','-z').decode().split('\0')))
for name in paths:
 p=r/name
 if p.suffix in ['.cs','.md'] and p.exists():
  s=p.read_text(encoding='utf-8-sig');p.write_bytes(('\n'.join(x.rstrip() for x in s.splitlines()).rstrip()+'\n').encode('utf8'))
p=r/'log.md';s=p.read_text(encoding='utf8')
if 'Notion 첨부20개 검증' not in s:
 s+='\n- SAVE 후속: Notion 첨부20개 검증(GIF13/사진7) 완료. 개발일지 https://app.notion.com/p/415c4a0ecd3181d39ec6e4778a426c0e / 문제해결 https://app.notion.com/p/415c4a0ecd3181168942e05f007b617b. 다른채팅 최종 completed/idle 재확인. Graphify47007노드/123843관계/3361커뮤니티 갱신 종료0. 원격 develop 전송/검증 진행 중이며 종료는 아직 하지 않았다.\n'
 p.write_bytes(s.encode('utf8'))
m=json.loads((o/'manifest.json').read_text(encoding='utf8'))
for item in m:
 if 'parts' not in item:
  data=(r/item['path']).read_bytes();item.update(bytes=len(data),sha256=hashlib.sha256(data).hexdigest())
(o/'manifest.json').write_bytes(json.dumps(m,ensure_ascii=False,indent=2).encode('utf8'))
paths += ['work/save-20261112/finalize.py','work/animation-1112/notion-uploads/build-bottom-760x600.png.json']
small=sorted(set(p for p in paths if not p.startswith('work/save-20261112/archive/')))
(o/'final-small-null.txt').write_bytes(b'\0'.join(p.encode()for p in small)+b'\0')
subprocess.run(['git','add','--pathspec-from-file='+str(o/'final-small-null.txt'),'--pathspec-file-nul'],check=True,stderr=subprocess.DEVNULL)
entries=[];batch=subprocess.Popen(['git','cat-file','--batch'],stdin=subprocess.PIPE,stdout=subprocess.PIPE)
for p in small:
 batch.stdin.write((':'+p+'\n').encode());batch.stdin.flush();header=batch.stdout.readline().decode().split();size=int(header[2]);data=batch.stdout.read(size);batch.stdout.read(1)
 entries.append(dict(path=p,mode='100644',type='blob',content=data.decode('utf8')))
batch.stdin.close();batch.wait()
(o/'entries.json').write_bytes(json.dumps(entries,ensure_ascii=False).encode('utf8'))
chunks=[];chunk=[];length=0
for e in entries:
 n=len(json.dumps(e,ensure_ascii=False))
 if chunk and length+n>700000:chunks.append(chunk);chunk=[];length=0
 chunk.append(e);length+=n
if chunk:chunks.append(chunk)
for i,c in enumerate(chunks):(o/f'entries-{i:03d}.json').write_bytes(json.dumps(c,ensure_ascii=False).encode('utf8'))
tree=git('write-tree').decode().strip();archive=git('rev-parse',tree+':work/save-20261112/archive').decode().strip();groups={}
for line in git('ls-tree',archive).decode().splitlines():
 meta,name=line.split('\t');groups[name]=meta.split()[2]
result={'tree':tree,'archive':archive,'groups':groups,'entry_batches':len(chunks),'entries':len(entries),'base_tree':git('rev-parse','HEAD^{tree}').decode().strip()}
(o/'tree-meta.json').write_bytes(json.dumps(result).encode('utf8'))
print(json.dumps({k:v for k,v in result.items()if k!='groups'}))
