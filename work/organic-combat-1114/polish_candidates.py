import subprocess,concurrent.futures,json
from pathlib import Path
root=Path(__file__).resolve().parent
project=root.parents[1]; art=project/'art/enemy-concepts-1114'; output=root/'polished'
names=['BloomMender','NeedleStalker','AcidSpitter','BreachBeast','BroodAlien','Bulwark','CarrionReclaimer','SiegeBrooder','VeilWeaver','WallRam']
def run(name):
    folder=output/name; folder.mkdir(parents=True,exist_ok=True)
    scripts=[art/'build_wallram_motion.py',art/'refine_death_pose.py',root/'audit_motion.py',root/'render_motion_audit.py']
    if name in ('Bulwark','CarrionReclaimer'):
        scripts.insert(2,root/'damp_attack.py')
    for script in scripts:
        result=subprocess.run(['F:/Blender/blender-5.2.3-windows-x64/blender.exe','-b','-t','2','--python',str(script),'--',name,str(output)],capture_output=True,text=True)
        (folder/(script.stem+'.log')).write_text(result.stdout+'\n'+result.stderr,encoding='utf-8')
        if result.returncode or 'Traceback' in result.stdout+result.stderr: return {'name':name,'ok':False,'step':script.name}
    return {'name':name,'ok':True}
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    results=[]
    for result in pool.map(run,names):
        results.append(result);print(json.dumps(result),flush=True)
        (root/'polish-results.json').write_text(json.dumps(results,indent=2))
