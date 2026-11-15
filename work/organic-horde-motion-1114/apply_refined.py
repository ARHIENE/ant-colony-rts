from pathlib import Path
import hashlib,json,shutil
root=Path('F:/UnityProjects/ant-colony-rts')
work=root/'work/organic-horde-motion-1114'
manifest=json.loads((work/'pending-runtime/manifest.json').read_text())
for row in manifest:
    assert hashlib.sha256((root/row['path']).read_bytes()).hexdigest()==row['sha256'],row['path']
copies=[]
for folder in sorted((work/'refined').iterdir()):
    if not folder.is_dir():continue
    kind=folder.name
    for src in folder.glob('*.fbx'):
        for dst in [root/'art/enemy-concepts-1114'/kind/src.name,root/'Assets/Resources/OrganicEnemies1114'/kind/src.name]:
            assert dst.exists(),dst
            copies.append((src,dst))
    src=folder/(kind+'_5Motions.blend')
    dst=root/'art/enemy-concepts-1114'/kind/src.name
    assert src.exists() and dst.exists(),kind
    copies.append((src,dst))
for row in manifest:copies.append((Path(row['candidate']),root/row['path']))
for src,dst in copies:
    backup=work/'before-refine'/dst.relative_to(root)
    backup.parent.mkdir(parents=True,exist_ok=True)
    if not backup.exists():shutil.copy2(dst,backup)
for src,dst in copies:shutil.copy2(src,dst)
(work/'applied-files.json').write_text(json.dumps([str(d.relative_to(root)) for s,d in copies],indent=2))
print('Applied files:',len(copies))
