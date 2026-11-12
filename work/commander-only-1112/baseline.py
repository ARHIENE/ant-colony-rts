from pathlib import Path
import json,hashlib
root=Path('Assets/_Project/Scripts');pending=Path('work/commander-only-1112/pending')
checks={str(p.relative_to(pending)).replace('\\','/'):hashlib.sha256((root/p.relative_to(pending)).read_bytes()).hexdigest() for p in pending.rglob('*.cs')}
Path('work/commander-only-1112/baselines.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
print(len(checks),'pending source baselines recorded')
