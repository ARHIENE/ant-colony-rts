from pathlib import Path
import subprocess,json,re
root=Path('E:/Git/ant'); w=root/'codex-work~'; off=w/'checks/off'; bee=root/'Library/Bee/artifacts/1900b0aE.dag'
dotnet=Path('E:/Unity/6000.5.8f1/Editor/Data/DotNetSdk/dotnet.exe'); csc=dotnet.parent/'sdk/8.0.318/Roslyn/bincore/csc.dll'
base={name:(bee/(name+'.rsp')).read_text(encoding='utf-8-sig').splitlines() for name in ('Assembly-CSharp','Assembly-CSharp-Editor')}
summary={}
for name in ('Assembly-CSharp','WorldMapChecks','SettlementDefenseChecks','TransportRouteChecks'):
 game=name=='Assembly-CSharp'; lines=base['Assembly-CSharp' if game else 'Assembly-CSharp-Editor']; out=[]; sources=[]
 for line in lines:
  low=line.lower()
  if low.startswith(('-out:','-refout:','-analyzer:','/analyzer:','-additionalfile:','/additionalfile:','/generatedfilesout:','-generatedfilesout:','/errorlog:','-errorlog:','/doc:','-doc:','/pdb:','-pdb:')): continue
  if line.strip('"').endswith('.cs'):
   if game: sources.append(line)
   continue
  if not game and low.startswith(('-r:','/reference:')) and re.search(r'Assembly-CSharp(?:\.ref)?\.dll',line):
   line='-r:"'+str(off/'Assembly-CSharp.dll').replace('\\','/')+'"'
  out.append(line)
 out.extend(['-out:"'+str(off/(name+'.dll')).replace('\\','/')+'"'])
 if game:
  known={s.strip('"').replace('\\','/') for s in sources}
  for f in (root/'Assets/_Project/Scripts').rglob('*.cs'):
   rel=f.relative_to(root).as_posix()
   if '/Editor/' not in rel and rel not in known: sources.append('"'+rel+'"')
 else: sources=['"'+str(w/'src/AgentScripts'/(name+'.cs')).replace('\\','/')+'"']
 out+=sources; rsp=off/(name+'.rsp'); rsp.write_text('\n'.join(out),encoding='utf-8')
 result=subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],cwd=root,capture_output=True,text=True,encoding='utf-8',errors='replace'); log=result.stdout+result.stderr
 (off/(name+'.log')).write_text(log,encoding='utf-8'); summary[name]={'exit':result.returncode,'errors':sum(': error ' in l for l in log.splitlines()),'warnings':sum(': warning ' in l for l in log.splitlines()),'sources':len(sources)}
 print(name,summary[name],flush=True)
 if result.returncode: print('\n'.join(l for l in log.splitlines() if ': error ' in l),flush=True)
(off/'compile-results.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
