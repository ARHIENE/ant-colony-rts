import copy
import json
from pathlib import Path

root = Path('work/natural-campaign-1115')
current = json.loads((root / 'checkpoint-save.json').read_text(encoding='utf-8-sig'))
prior = json.loads((root / 'post-wave3-2413.json').read_text(encoding='utf-8-sig'))
assert current['gameId'] == prior['gameId']
assert not any(b['kind'] == 'WaterSource' for b in current['buildings'])
source = copy.deepcopy(next(b for b in prior['buildings'] if b['kind'] == 'WaterSource'))
assert source['health'] == 267 and source['supply']['amount'] == 0
original = copy.deepcopy(current)
highest = max((int(b['key'][4:]) for b in current['buildings'] if b['key'].startswith('new:') and b['key'][4:].isdigit()), default=0)
source['key'] = f'new:{highest + 1}'
current['buildings'].append(source)
without_source = copy.deepcopy(current)
without_source['buildings'].pop()
assert without_source == original
(root / 'pre-source-recovery-3226.json').write_text(json.dumps(original, ensure_ascii=False, indent=2), encoding='utf-8')
(root / 'source-recovery-candidate.json').write_text(json.dumps(current, ensure_ascii=False, indent=2), encoding='utf-8')
print('Restored exactly one WaterSource record from same-game history; all other game state unchanged. Candidate still requires runtime validation.')
