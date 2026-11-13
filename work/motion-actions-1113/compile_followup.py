from pathlib import Path
import subprocess
r=Path('F:/UnityProjects/ant-colony-rts');o=r/'work/motion-actions-1113/followup'
base=(r/'work/early-balance-1113/pending-game.rsp').read_text(encoding='utf-8-sig').splitlines()
args=[s for s in base if not s.strip('"').endswith('.cs') and not s.startswith(('-out:', '-refout:'))]
args+=['-out:"work/motion-actions-1113/followup/game.dll"']
for p in (r/'Assets/_Project/Scripts').rglob('*.cs'):
 if 'Editor' not in p.parts: args.append('"'+str(o/p.name if (o/p.name).exists() else p)+'"')
args.append('"'+str(o/'CommanderResearchApproach.cs')+'"')
args += [s for s in base if s.strip('"').endswith('.cs') and 'Assets' in s and '_Project' not in s]
f=o/'compile.rsp';f.write_text('\n'.join(args),encoding='utf-8')
d=Path('E:/Unity/6000.5.8f1/Editor/Data/DotNetSdk');c=next(d.glob('sdk/*/Roslyn/bincore/csc.dll'))
p=subprocess.run([str(d/'dotnet.exe'),'exec',str(c),'-nologo','@'+str(f)],cwd=r,capture_output=True,text=True)
(o/'compile.txt').write_text(p.stdout+p.stderr,encoding='utf-8');print((p.stdout+p.stderr)[-2000:]);print('exit',p.returncode)
