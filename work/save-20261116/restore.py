from pathlib import Path
import gzip,hashlib,json,sys,base64,lzma,tarfile,tempfile,collections
root=Path(__file__).resolve().parents[2];folder=Path(__file__).resolve().parent
apply='--apply' in sys.argv;manifest=json.loads((folder/'manifest.json').read_text(encoding='utf8'))
records=manifest['files'] if isinstance(manifest,dict) else manifest
if isinstance(manifest,dict):
 for item in records:
  if item.get('bundle'):item.update(manifest['bundles'][item['bundle']])
checked=restored=0;bundles=collections.defaultdict(list)
def verify(item,data):
 global checked,restored
 target=(root/item['path']).resolve()
 if not target.is_relative_to(root):raise ValueError('Unsafe target')
 if len(data)!=item['bytes'] or hashlib.sha256(data).hexdigest()!=item['sha256']:raise ValueError('Corrupt archive: '+item['path'])
 checked+=1
 if apply and (not target.exists() or hashlib.sha256(target.read_bytes()).hexdigest()!=item['sha256']):
  if target.exists():
   backup=folder/'restore-backup'/item['path'];backup.parent.mkdir(parents=True,exist_ok=True)
   if not backup.exists():backup.write_bytes(target.read_bytes())
  target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data);restored+=1
def part(path):
 p=(root/path).resolve()
 if not p.is_relative_to(root):raise ValueError('Unsafe archive path')
 data=p.read_bytes();return base64.b64decode(data,validate=True) if p.suffix=='.b64' else data
for item in records:
 if item.get('parts'):bundles[(item['encoding'],tuple(item['parts']))].append(item)
for (encoding,parts),items in bundles.items():
 if encoding=='gzip+base64':
  data=gzip.decompress(b''.join(part(p) for p in parts))
  for item in items:verify(item,data)
 elif encoding=='tar+xz+base64':
  wanted=collections.defaultdict(list)
  for item in items:wanted[item['member']].append(item)
  with tempfile.TemporaryFile() as compressed:
   for p in parts:compressed.write(part(p))
   compressed.seek(0)
   with lzma.LZMAFile(compressed) as stream:
    with tarfile.open(fileobj=stream,mode='r|') as tar:
     for member in tar:
      if not member.isfile() or member.name not in wanted:raise ValueError('Unexpected member')
      data=tar.extractfile(member).read()
      for item in wanted.pop(member.name):verify(item,data)
  if wanted:raise ValueError('Missing archive members')
 else:raise ValueError('Unknown encoding')
print(f'Verified {checked} archived originals; restored {restored}.')
