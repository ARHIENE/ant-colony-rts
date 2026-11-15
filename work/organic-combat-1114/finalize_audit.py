import json, hashlib
from pathlib import Path

root=Path(__file__).resolve().parent
project=root.parents[1]
art=project/'art/enemy-concepts-1114'
names=[p.name for p in (root/'polished').iterdir() if p.is_dir()]
comparison=[]
checks=[]
for name in sorted(names):
    before={r['clip']:r for r in json.loads((root/'motion-audit'/name/'metrics.json').read_text())}
    after=json.loads((root/'polished-audit'/name/'metrics.json').read_text())
    for row in after:
        old=before[row['clip']]
        comparison.append(dict(name=name,clip=row['clip'],jointBefore=old['peakJointDegrees'],jointAfter=row['peakJointDegrees'],stretchBefore=old['peakEdgeStretch999'],stretchAfter=row['peakEdgeStretch999']))
    rows=json.loads((root/'polished'/name/'export-checks.json').read_text())
    assert all(r['status']=='PASS' for r in rows)
    response=json.loads((root/'polished'/name/'unity-refresh.json').read_text(encoding='utf-8-sig'))
    assert response['success'] and response['data']['result']['success']
    checks.append(dict(name=name,ok=True,clips=len(rows)))
(root/'polished-export-results.json').write_text(json.dumps(checks,indent=2))
(root/'motion-audit/comparison.json').write_text(json.dumps(comparison,indent=2))
hashes=[]
for model in sorted((project/'Assets/Resources/OrganicEnemies1114').iterdir()):
    if not model.is_dir(): continue
    for fbx in sorted(model.glob('*.fbx')):
        source=art/model.name/fbx.name
        a=hashlib.sha256(source.read_bytes()).hexdigest()
        b=hashlib.sha256(fbx.read_bytes()).hexdigest()
        assert a==b, str(fbx)
        hashes.append(dict(model=model.name,clip=fbx.stem,sha256=a))
assert len(hashes)==86
(art/'motion-polish-hashes.json').write_text(json.dumps(hashes,indent=2))
for kind in ('unity-transition-response-after','unity-skin-response-after'):
    response=json.loads((root/'motion-audit'/(kind+'.json')).read_text(encoding='utf-8-sig'))
    assert response['success'] and response['data']['result']['success']
    (art/(kind+'.txt')).write_text(response['data']['result']['result'],encoding='utf-8')
(art/'motion-polish-comparison.json').write_text(json.dumps(comparison,indent=2))
print(f'{len(checks)} polished models / {sum(r["clips"] for r in checks)} checked clips; {len(hashes)} Unity FBX hashes match.')
