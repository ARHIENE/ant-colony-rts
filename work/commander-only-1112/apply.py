from pathlib import Path
import hashlib,json,shutil,os
root=Path('Assets/_Project/Scripts');work=Path('work/commander-only-1112');checks=json.loads((work/'baselines.json').read_text())
for rel,digest in checks.items():
 assert hashlib.sha256((root/rel).read_bytes()).hexdigest()==digest, 'Concurrent change: '+rel
for rel in checks:
 p=root/rel;backup=work/'before'/rel;backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,backup)
 temp=p.with_suffix('.cs.takeover');shutil.copy2(work/'pending'/rel,temp);os.replace(temp,p)
print('Applied',len(checks),'files; baseline hashes matched; original files backed up.')
