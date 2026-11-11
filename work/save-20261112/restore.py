from pathlib import Path
import gzip,hashlib,json,sys,base64
root=Path(__file__).resolve().parents[2];folder=Path(__file__).resolve().parent
apply='--apply' in sys.argv
records=json.loads((folder/'manifest.json').read_text(encoding='utf8'))
checked=restored=0
for item in records:
 target=(root/item['path']).resolve()
 if not target.is_relative_to(root):raise ValueError('Unsafe target')
 if 'parts' not in item:continue
 data=gzip.decompress(b''.join(base64.b64decode((root/p).read_bytes()) if p.endswith('.b64') else (root/p).read_bytes() for p in item['parts']))
 if hashlib.sha256(data).hexdigest()!=item['sha256']:raise ValueError('Corrupt archive: '+item['path'])
 checked+=1
 if apply and (not target.exists() or hashlib.sha256(target.read_bytes()).hexdigest()!=item['sha256']):
  if target.exists():
   backup=folder/'restore-backup'/item['path'];backup.parent.mkdir(parents=True,exist_ok=True);backup.write_bytes(target.read_bytes())
  target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data);restored+=1
print(f'Verified {checked} archived originals; restored {restored}.')

