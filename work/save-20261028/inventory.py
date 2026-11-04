from pathlib import Path
import subprocess,json,collections
root=Path('E:/Git/ant')
raw=subprocess.check_output(['git','status','--porcelain=v1','-z','--untracked-files=all'],cwd=root).decode('utf-8')
rows=[]
for item in raw.split('\0'):
 if item:rows.append({'status':item[:2],'path':item[3:]})
(root/'work/save-20261028/status.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
groups=collections.Counter()
for x in rows:
 p=x['path'];groups['/'.join(p.split('/')[:3]) if p.startswith('Assets/') else p.split('/')[0]]+=1
print(json.dumps(groups,ensure_ascii=False,indent=2))
print('SOURCE/ASSET CHANGES:')
for x in rows:
 p=x['path']
 if p.startswith(('Assets/_Project/Scripts/','Assets/_Project/Editor/','AgentScripts/','Assets/Resources/','Assets/_Project/Art/Kemono/')):
  if not p.endswith(('.mat','.mat.meta','.fbx','.fbx.meta','.anim','.anim.meta')):print(x['status'],p)
print('TRACKED OTHERS:')
for x in rows:
 p=x['path']
 if x['status']!='??' and not p.startswith(('Assets/_Project/Scripts/','Assets/_Project/Editor/','Assets/Resources/','Assets/_Project/Art/Kemono/','graphify-out/')):print(x['status'],p)

