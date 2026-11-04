from pathlib import Path
import subprocess,json,collections
root=Path('E:/Git/ant')
assert subprocess.check_output(['git','branch','--show-current'],cwd=root,text=True).strip()=='develop'
raw=subprocess.check_output(['git','status','--porcelain=v1','-z','--untracked-files=all'],cwd=root).decode('utf-8')
rows=[{'status':s[:2],'path':s[3:]} for s in raw.split('\0') if s]
exact={'Assets/_Project/Editor/KemonoImport.cs','README.md','log.md','changelog.md','docs/IMPLEMENTATION_BOARD.md','docs/SAVE_AUDIT_20261027.md'}
exact.update('art/production-kit-20261019/scripts/'+s for s in ['repair_equipment_1026.py','vivid_actions_1026.py','build_work_saw_1026.py','pk.py'])
names=['Ore_Titanium','Ore_Starlight','Ingot_Titanium','Ingot_TitaniumAlloy']
exact.update('Assets/Resources/ResourceModels/'+n+'.fbx'+m for n in names for m in ['', '.meta'])
selected=[]
for row in rows:
 p=row['path']
 include=p in exact or p.startswith(('Assets/_Project/Scripts/','Assets/Resources/Kemono/','Assets/_Project/Art/Kemono/Anim/','AgentScripts/'))
 if include:
  assert not p.endswith(('.prefab','.prefab.meta'))
  assert (root/p).is_file(),p
  assert (root/p).stat().st_size<95*1024*1024,p
  selected.append(p)
manifest=root/'work/save-20261027/stage-paths.nul'
manifest.write_bytes(b'\0'.join(s.encode() for s in selected)+b'\0')
(root/'work/save-20261027/selected.json').write_text(json.dumps(selected,ensure_ascii=False,indent=2),encoding='utf-8')
subprocess.run(['git','add','--pathspec-from-file='+str(manifest),'--pathspec-file-nul'],cwd=root,check=True,stdout=subprocess.DEVNULL,stderr=subprocess.PIPE)
staged=set(subprocess.check_output(['git','diff','--cached','--name-only','-z'],cwd=root).decode().split('\0'))-{''}
assert staged==set(selected),(staged-set(selected),set(selected)-staged)
print('Selected files:',len(selected))
print('Bytes:',sum((root/p).stat().st_size for p in selected))
print('Groups:',dict(collections.Counter(p.split('/')[0] for p in selected)))
print('AgentScripts:',[p for p in selected if p.startswith('AgentScripts/')])
