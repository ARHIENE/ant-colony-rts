import subprocess,concurrent.futures,json
from pathlib import Path
root=Path(__file__).resolve().parent
source=root.parents[1]/'art/enemy-concepts-1114'
names=[p.name for p in source.iterdir() if p.is_dir() and (p/'clips.json').exists()]
def run(name):
    result=subprocess.run(['F:/Blender/blender-5.2.3-windows-x64/blender.exe','-b','-t','2','--python',str(root/'audit_motion.py'),'--',name],capture_output=True,text=True)
    out=root/'motion-audit'/name
    out.mkdir(exist_ok=True)
    (out/'audit.log').write_text(result.stdout+'\n'+result.stderr,encoding='utf-8')
    return {'name':name,'ok':result.returncode==0 and 'Traceback' not in result.stdout+result.stderr and (out/'metrics.json').exists()}
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    results=[]
    for r in pool.map(run,names):
        results.append(r); print(json.dumps(r),flush=True)
        (root/'motion-audit/results.json').write_text(json.dumps(results,indent=2))
