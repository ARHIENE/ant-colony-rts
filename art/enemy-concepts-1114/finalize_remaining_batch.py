import concurrent.futures,json,subprocess
from pathlib import Path
from remaining_motion_profiles import PROFILES

HERE=Path(__file__).resolve().parent
BLENDER='F:/Blender/blender-5.2.3-windows-x64/blender.exe'

def finalize(name):
    out=HERE/name
    for script,expected in [('refine_death_pose.py','death-refined.txt'),('check_wallram_exports.py','export-checks.json'),('preview_motion_strip.py','motion-strip.png')]:
        command=[BLENDER,'--background','--python',str(HERE/script),'--',name]
        if script=='check_wallram_exports.py': command.append('Death')
        result=subprocess.run(command,capture_output=True,text=True)
        (out/('final-'+script+'.log')).write_text(result.stdout+'\n'+result.stderr,encoding='utf-8')
        if result.returncode or not (out/expected).exists() or 'Traceback' in result.stderr+result.stdout:
            return {'name':name,'status':'FAILED','step':script}
    return {'name':name,'status':'PASS','clips':5}

with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    results=[]
    for result in pool.map(finalize,PROFILES):
        results.append(result)
        print(json.dumps(result),flush=True)
        (HERE/'final-batch-results.json').write_text(json.dumps(results,indent=2))
