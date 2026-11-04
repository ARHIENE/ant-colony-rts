from pathlib import Path
import json
w=Path('E:/Git/ant/codex-work~'); a=json.loads((w/'blender/previews/original-till-fit.json').read_text()); b=json.loads((w/'blender/previews/final-till-fit.json').read_text())
changes=[]
for item in sorted(set(a['overlaps'])|set(b['overlaps'])):
 for part in sorted(set(a['overlaps'].get(item,{}))|set(b['overlaps'].get(item,{}))):
  x=a['overlaps'].get(item,{}).get(part,{}).get('list',{}).get('Tool_Till',0); y=b['overlaps'].get(item,{}).get(part,{}).get('list',{}).get('Tool_Till',0)
  if x!=y: changes.append((item,part,x,y))
print('ARMOR CHANGES',changes)
(w/'reports/till-armor-delta.json').write_text(json.dumps(changes,indent=2),encoding='utf-8')
