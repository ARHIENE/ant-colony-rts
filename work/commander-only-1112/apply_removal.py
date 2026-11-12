from pathlib import Path
import hashlib,json,os
root=Path('Assets/_Project/Scripts')
pending=Path('work/commander-only-1112/removal-pending')
manifest=json.loads(Path('work/commander-only-1112/removal-manifest.json').read_text())
for name,expected in manifest.items():
    assert hashlib.sha256((root/name).read_bytes()).hexdigest()==expected, 'Changed since preparation: '+name
for name in manifest:
    target=root/name
    temp=target.with_suffix('.cs.takeover')
    temp.write_bytes((pending/name).read_bytes())
    os.replace(temp,target)
print('Applied',len(manifest),'files after all baseline hashes matched')
