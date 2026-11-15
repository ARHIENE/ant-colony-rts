import subprocess,concurrent.futures,json
from pathlib import Path
root=Path(__file__).resolve().parent; art=root.parents[1]/'art/enemy-concepts-1114'
names=['BloomMender','NeedleStalker','AcidSpitter','BreachBeast','BroodAlien','SiegeBrooder','VeilWeaver','WallRam','Bulwark','CarrionReclaimer']
def run(name):
    folder=root/'polished'/name
    r=subprocess.run(['F:/Blender/blender-5.2.3-windows-x64/blender.exe','-b','-t','2','--python',str(art/'check_wallram_exports.py'),'--',name,'all',str(root/'polished')],capture_output=True,text=True)
    (folder/'fbx-check.log').write_text(r.stdout+'\n'+r.stderr,encoding='utf-8')
    return {'name':name,'ok':r.returncode==0 and 'Traceback' not in r.stdout+r.stderr and (folder/'export-checks.json').exists()}
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    results=[]
    for r in pool.map(run,names):
        results.append(r); print(json.dumps(r),flush=True)
        (root/'polished-export-results.json').write_text(json.dumps(results,indent=2))
