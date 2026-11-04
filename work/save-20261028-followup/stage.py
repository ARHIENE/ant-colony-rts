from pathlib import Path
import subprocess,json,collections
r=Path('E:/Git/ant')
def git(*a): return subprocess.check_output(['git',*a],cwd=r)
assert git('branch','--show-current').strip()==b'develop'
assert not git('diff','--cached','--name-only').strip(), 'Index already contains changes'
tracked=set(git('-c','core.quotepath=false','diff','--name-only','-z').decode().split('\0'))-{''}
untracked=set(git('ls-files','--others','--exclude-standard','-z').decode().split('\0'))-{''}
exact={'README.md','log.md','changelog.md','docs/IMPLEMENTATION_BOARD.md','docs/SAVE_AUDIT_20261028_FOLLOWUP.md','docs/verification/SAVE_20261028_FOLLOWUP.txt','Assets/_Project/Editor/KemonoImport.cs','AgentScripts/Save1028FollowupViews.cs','art/production-kit-20261019/scripts/fishing_bank_1028.py','art/production-kit-20261019/fishing-bank-1028.blend','art/production-kit-20261019/exports/fishing-bank-1028/Fishing_Bank.fbx'}
fac=['CharcoalKiln','CokeOven','Mill','OilPress','BrewingBarrel','Still']
models=['Fishing_Bank','Proc_Charcoal','Proc_Coke','Proc_Pottery','Grain_Barley','Grain_Rice','Fruit_Grape','Nut_Sunflower','Proc_WheatFlour','Proc_SunflowerOil','Gather_RubberSap','Proc_Rubber','Alc_Beer','Alc_RiceWine','Alc_FruitWine','Alc_Mead','Alc_HoneydewWine','Alc_Spirits']
for folder,names in [('Facilities',fac),('ResourceModels',models)]:
 for n in names:
  for suffix in ['.fbx','.fbx.meta']: exact.add('Assets/Resources/'+folder+'/'+n+suffix)
selected=[]
for p in tracked|untracked:
 include=p in exact or p.startswith('Assets/_Project/Scripts/')
 include|=p in tracked and p.startswith(('AgentScripts/','Assets/_Project/Art/Kemono/Anim/'))
 include|=p in untracked and p.startswith('AgentScripts/') and '1028' in p
 include|=p in untracked and p.startswith('Assets/Resources/Kemono/Materials/') and any(Path(p).name.startswith(n+'_') or Path(p).name in [n+'.mat',n+'.mat.meta'] for n in fac+models)
 if include:
  assert not p.endswith(('.prefab','.prefab.meta'))
  assert (r/p).is_file(),p
  assert (r/p).stat().st_size<95*1024*1024,p
  selected.append(p)
manifest=r/'work/save-20261028-followup/stage-paths.nul'
manifest.write_bytes(b'\0'.join(p.encode() for p in sorted(selected))+b'\0')
subprocess.run(['git','add','--pathspec-from-file='+str(manifest),'--pathspec-file-nul'],cwd=r,check=True,stdout=subprocess.DEVNULL,stderr=subprocess.PIPE)
# Stage only the PC-low inclusion, preserving existing local QualitySettings edits.
quality='ProjectSettings/QualitySettings.asset'
head=git('show','HEAD:'+quality)
old=b'    excludedTargetPlatforms:\n    - Standalone'
assert head.count(old)==1
blob=head.replace(old,b'    excludedTargetPlatforms: []',1)
sha=subprocess.check_output(['git','hash-object','-w','--stdin'],input=blob,cwd=r).decode().strip()
subprocess.run(['git','update-index','--cacheinfo','100644,'+sha+','+quality],cwd=r,check=True)
selected.append(quality)
actual=set(git('diff','--cached','--name-only','-z').decode().split('\0'))-{''}
assert actual==set(selected),(actual-set(selected),set(selected)-actual)
(r/'work/save-20261028-followup/selected.json').write_text(json.dumps(sorted(selected),ensure_ascii=False,indent=2),encoding='utf-8')
print('Selected',len(selected),'files; FBX',sum(p.endswith('.fbx') for p in selected),'animation imports',sum('/Anim/' in p for p in selected))
print(dict(collections.Counter(p.split('/')[0] for p in selected)))
