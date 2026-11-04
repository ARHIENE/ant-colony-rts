from pathlib import Path
import json,hashlib
r=Path('E:/Git/ant'); w=r/'codex-work~'; out=w/'round2'
m=json.loads((w/'FBX-MANIFEST.json').read_text()); assert len(m)==2
assert {p['target'].split('@PK_')[1] for p in m}=={'Tool_Till.fbx','Tool_Mine.fbx'}
for p in m: assert hashlib.sha256((w/p['source']).read_bytes()).hexdigest()==p['sha256']
assert not (w/'fbx/Assets/_Project/Art/Kemono/Anim/Kemono@PK_Tool_Dig.fbx').exists()
for p,h in json.loads((out/'reports/original-hashes.json').read_text()).items(): assert hashlib.sha256((r/p).read_bytes()).hexdigest()==h,p
for p in ('fit-delta.json','timing-events.json'): assert json.loads((out/'reports'/p).read_text())['passed']
assert json.loads((out/'reports/action-integrity.json').read_text())['changed']==['PK|Tool_Mine']
assert '## 2차' in (w/'HANDOFF-CODEX-1025.md').read_text(encoding='utf-8-sig')
assert json.loads((out/'reports/ROUND2-RESULTS.json').read_text())['Tool_Dig']['passed'] is False
print('DELIVERY VERIFIED: Mine only; 2 FBX entries including prior Till; Dig withheld; original hashes unchanged; handoff appended.')
