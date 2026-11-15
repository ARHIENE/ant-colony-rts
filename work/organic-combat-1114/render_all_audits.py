import subprocess,concurrent.futures,json
from pathlib import Path
root=Path(__file__).resolve().parent
names=[p.parent.name for p in (root/'motion-audit').glob('*/metrics.json')]
def run(name):
    out=root/'motion-audit'/name
    if (out/'sequence.png').exists(): return name+' exists'
    p=subprocess.run(['F:/Blender/blender-5.2.3-windows-x64/blender.exe','-b','-t','2','--python',str(root/'render_motion_audit.py'),'--',name],capture_output=True,text=True)
    (out/'render.log').write_text(p.stdout+'\n'+p.stderr,encoding='utf-8')
    return name+(' rendered' if (out/'sequence.png').exists() else ' FAILED')
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    for r in pool.map(run,names): print(r,flush=True)
