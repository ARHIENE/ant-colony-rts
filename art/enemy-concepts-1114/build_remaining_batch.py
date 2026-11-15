import concurrent.futures, json, subprocess
from pathlib import Path
from remaining_motion_profiles import PROFILES

HERE=Path(__file__).resolve().parent
BLENDER='F:/Blender/blender-5.2.3-windows-x64/blender.exe'

def build(name):
    out=HERE/name
    for script, report in [('build_wallram_motion.py','clips.json'),('check_wallram_exports.py','export-checks.json')]:
        if (out/report).exists():
            continue
        result=subprocess.run([BLENDER,'--background','--python',str(HERE/script),'--',name],capture_output=True,text=True)
        (out/(script+'.log')).write_text(result.stdout+'\n'+result.stderr,encoding='utf-8')
        if result.returncode or not (out/report).exists():
            return {'name':name,'status':'FAILED','step':script}
    return {'name':name,'status':'PASS'}

with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    results=[]
    for result in pool.map(build, PROFILES):
        results.append(result)
        print(json.dumps(result),flush=True)
        (HERE/'remaining-batch-results.json').write_text(json.dumps(results,indent=2))
