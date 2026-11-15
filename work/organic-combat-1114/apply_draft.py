import hashlib,json,shutil
from pathlib import Path
root=Path(__file__).resolve().parent
project=root.parents[1]
base=project/'Assets/_Project/Scripts'
manifest=json.loads((root/'code-base-hashes.json').read_text(encoding='utf-8-sig'))
known={row['file']:row['sha256'].lower() for row in manifest}
files=list((root/'code').rglob('*.cs'))
for path in files:
    relative=path.relative_to(root/'code').as_posix(); target=base/relative
    if relative in known:
        assert hashlib.sha256(target.read_bytes()).hexdigest()==known[relative], 'Concurrent change: '+relative
    else:
        assert not target.exists(), 'New target already exists: '+relative
# Run only after explicit Unity handoff. Verify all paths before the first mutation.
for path in files:
    relative=path.relative_to(root/'code'); target=base/relative
    if target.exists():
        backup=root/'before-combat'/relative; backup.parent.mkdir(parents=True,exist_ok=True)
        assert not backup.exists(), 'Backup already exists: '+str(backup)
        shutil.copy2(target,backup)
    target.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(path,target)
print('Applied',len(files),'files; all baseline hashes matched; backups preserved.')
