import json,collections,pathlib
j=json.loads(pathlib.Path('work/save-20261026/inventory.json').read_text(encoding='utf-8'))
a=[p for p in j['untracked_relevant'] if p.startswith('Assets/_Project/') and not p.endswith(('.cs','.cs.meta'))]
print('PROJECT NONCODE',dict(collections.Counter('/'.join(p.split('/')[:4]) for p in a)))
print('RESOURCE GROUPS',dict(collections.Counter(p.split('/')[2] for p in j['untracked_relevant'] if p.startswith('Assets/Resources/'))))
print('PROJECT SAMPLES','\n'.join(a[:35]))
