from pathlib import Path
import subprocess
root=Path('F:/UnityProjects/ant-colony-rts')
out=root/'work/motion-actions-1113'
lines=(root/'work/early-balance-1113/pending-game.rsp').read_text(encoding='utf-8-sig').splitlines()
lines=[s for s in lines if not s.strip('"').endswith('.cs') and not s.startswith(('-out:', '-refout:'))]
lines += ['-out:"work/motion-actions-1113/candidate-game.dll"']
lines.append('"'+str(out/'CommanderPlayApproach.cs')+'"')
for f in (root/'Assets/_Project/Scripts').rglob('*.cs'):
    if 'Editor' in f.parts: continue
    draft=out/f.name
    lines.append('"'+str(draft if draft.exists() else f)+'"')
# Keep external packages compiled into the game's assembly.
for s in (root/'work/early-balance-1113/pending-game.rsp').read_text(encoding='utf-8-sig').splitlines():
    if s.strip('"').endswith('.cs') and 'Assets' in s and '_Project' not in s: lines.append(s)
rsp=out/'candidate-game.rsp'
rsp.write_text('\n'.join(lines),encoding='utf-8')
data=Path('E:/Unity/6000.5.8f1/Editor/Data/DotNetSdk')
csc=next(data.glob('sdk/*/Roslyn/bincore/csc.dll'))
p=subprocess.run([str(data/'dotnet.exe'),'exec',str(csc),'-nologo','@'+str(rsp)],cwd=root,capture_output=True,text=True)
(out/'candidate-compile.txt').write_text(p.stdout+p.stderr,encoding='utf-8')
print((p.stdout+p.stderr)[-6000:]);print('exit',p.returncode)
raise SystemExit(p.returncode)
