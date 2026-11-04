from pathlib import Path
import json
w=Path('E:/Git/ant/codex-work~'); d=json.loads((w/'reports/remaining-armor.json').read_text()); rows=sorted([(v['worst_pairs'],i,p,v['clips']) for i,ps in d['overlaps'].items() for p,v in ps.items()],reverse=True)
print(rows[:10]); print(json.loads((w/'reports/remaining-tool-clearance.json').read_text())); print((w/'reports/action-integrity.json').read_text())
