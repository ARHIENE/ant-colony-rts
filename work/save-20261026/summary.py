import json,pathlib
p=pathlib.Path('work/save-20261026')
i=json.loads((p/'inventory.json').read_text(encoding='utf-8'))
print('NEWCS', '\n'.join(x for x in i['untracked_relevant'] if x.endswith('.cs') and not x.startswith('AgentScripts/')))
for name in ['13fff952-aaf1-4403-8a25-e1595cbeb6dd','df6935fe-43ea-4908-a8f7-dd131d73587a']:
 m=json.loads((p/(name+'-assistant.json')).read_text(encoding='utf-8'))
 print('\nCLAUDE',name)
 for x in m:
  if len(x['text'])>230 and any(t in x['text'] for t in ['통과','인계','완료','작업 요약','한도']):print(x['timestamp'],x['text'][:1100])
