from pathlib import Path
import subprocess,json
root=Path('E:/Git/ant'); w=root/'codex-work~'; off=w/'off'; bee=root/'Library/Bee/artifacts/1900b0aE.dag'
dotnet=Path('E:/Unity/6000.5.8f1/Editor/Data/DotNetSdk/dotnet.exe'); csc=dotnet.parent/'sdk/8.0.318/Roslyn/bincore/csc.dll'
summary={}
for name in ('Assembly-CSharp','Assembly-CSharp-Editor'):
 lines=(bee/(name+'.rsp')).read_text(encoding='utf-8-sig').splitlines(); out=[]
 for line in lines:
  if line.startswith(('-out:', '-refout:')):
   key=line.split(':',1)[0]; suffix='.ref.dll' if key=='-refout' else '.dll'; line=key+':"'+str(off/(name+suffix)).replace('\\','/')+'"'
  if line.startswith(('-analyzer:', '/analyzer:', '-additionalfile:', '/additionalfile:')): continue
  if line.strip('"').endswith('.cs'):
   rel=line.strip('"'); replacement=w/'src'/rel
   if replacement.exists(): line='"'+str(replacement).replace('\\','/')+'"'
  if name=='Assembly-CSharp-Editor' and line.startswith('-r:'):
   line=line.replace('Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.ref.dll','codex-work~/off/Assembly-CSharp.ref.dll').replace('Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.dll','codex-work~/off/Assembly-CSharp.dll')
  out.append(line)
 # Bee response may predate untracked new source files. Include current sources not already listed.
 base=root/'Assets/_Project/Scripts'; all_sources=list(base.rglob('*.cs')) if name=='Assembly-CSharp' else list((root/'Assets/_Project/Editor').rglob('*.cs'))
 known={x.strip('"').replace('\\','/') for x in out if x.strip('"').endswith('.cs')}
 for f in all_sources:
  rel=f.relative_to(root).as_posix()
  if '/Editor/' in rel and name=='Assembly-CSharp': continue
  if rel not in known and not str(w/'src'/rel).replace('\\','/') in known: out.append('"'+rel+'"')
 rsp=off/(name+'.rsp'); rsp.write_text('\n'.join(out),encoding='utf-8')
 result=subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],cwd=root,capture_output=True,text=True,encoding='utf-8',errors='replace')
 log=result.stdout+result.stderr; (off/(name+'.log')).write_text(log,encoding='utf-8'); summary[name]={'exit':result.returncode,'errors':sum(': error ' in l for l in log.splitlines()),'warnings':sum(': warning ' in l for l in log.splitlines())}; print(name,summary[name],flush=True)
(off/'compile-results.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
