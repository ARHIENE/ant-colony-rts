from pathlib import Path
import json,hashlib
w=Path('E:/Git/ant/codex-work~')
for p in ('reports/final-wrist-validation.json','reports/fbx-reimport.json'):
 d=json.loads((w/p).read_text()); assert d['passed'],p
assert json.loads((w/'reports/action-integrity.json').read_text())['changed']==['PK|Tool_Till']
assert all(x['exit']==0 and x['errors']==0 for x in json.loads((w/'off/compile-results.json').read_text()).values())
for f in json.loads((w/'FBX-MANIFEST.json').read_text()): assert hashlib.sha256((w/f['source']).read_bytes()).hexdigest()==f['sha256']
for p,h in json.loads((w/'reports/original-hashes.json').read_text()).items(): assert hashlib.sha256((w.parent/p).read_bytes()).hexdigest()==h,p
assert (w/'HANDOFF-CODEX-1025.md').stat().st_size>0
print('FINAL: wrist / FBX reimport / action scope / compile / binary SHA-256 / 53 originals / handoff all verified.')
