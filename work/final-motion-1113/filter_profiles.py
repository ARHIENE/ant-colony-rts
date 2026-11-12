import json,pathlib
p=pathlib.Path('work/final-motion-1113/work-contact-profiles.json');d=json.loads(p.read_text());indices={'Tool_Mine':[0,1],'Tool_Chop':[1],'Tool_Dig':[4],'Tool_Till':[4],'Tool_Hammer':[0,1]}
for item in d['profiles']:item['points']=[v for i,v in enumerate(item['points']) if i%6 in indices[item['action']]]
pathlib.Path('Assets/Resources/MeshyNative/WorkContactProfiles.json').write_text(json.dumps(d,separators=(',',':')))
print([(p['action'],len(p['points']))for p in d['profiles']])
