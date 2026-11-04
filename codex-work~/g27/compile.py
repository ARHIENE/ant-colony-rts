from pathlib import Path
import subprocess, json, re

root = Path('E:/Git/ant')
w = root / 'codex-work~/g27'
off = w / 'off'
off.mkdir(exist_ok=True)
bee = root / 'Library/Bee/artifacts/1900b0aE.dag'
dotnet = Path('E:/Unity/6000.5.8f1/Editor/Data/DotNetSdk/dotnet.exe')
csc = dotnet.parent / 'sdk/8.0.318/Roslyn/bincore/csc.dll'
summary = {}
for name in ('Assembly-CSharp', 'ImplG27Checks'):
    game = name == 'Assembly-CSharp'
    base = bee / ('Assembly-CSharp.rsp' if game else 'Assembly-CSharp-Editor.rsp')
    out, sources = [], []
    for line in base.read_text(encoding='utf-8-sig').splitlines():
        low = line.lower()
        if low.startswith(('-out:', '-refout:', '-analyzer:', '/analyzer:', '-additionalfile:', '/additionalfile:', '-generatedfilesout:', '/generatedfilesout:', '-errorlog:', '/errorlog:', '-doc:', '/doc:', '-pdb:', '/pdb:')):
            continue
        if line.strip('"').endswith('.cs'):
            if game:
                rel = line.strip('"')
                staged = w / 'src' / rel
                sources.append('"' + (staged.as_posix() if staged.exists() else rel) + '"')
            continue
        if not game and low.startswith(('-r:', '/reference:')) and re.search(r'Assembly-CSharp(?:\.ref)?\.dll', line):
            line = '-r:"' + (off / 'Assembly-CSharp.dll').as_posix() + '"'
        out.append(line)
    if game:
        known = {s.strip('"').replace('\\', '/') for s in sources}
        for f in (root / 'Assets/_Project/Scripts').rglob('*.cs'):
            rel = f.relative_to(root).as_posix()
            staged = w / 'src' / rel
            if '/Editor/' not in rel and rel not in known and staged.as_posix() not in known:
                sources.append('"' + (staged.as_posix() if staged.exists() else rel) + '"')
    else:
        sources = ['"' + (w / 'src/AgentScripts/ImplG27Checks.cs').as_posix() + '"']
    out += ['-out:"' + (off / (name + '.dll')).as_posix() + '"'] + sources
    rsp = off / (name + '.rsp')
    rsp.write_text('\n'.join(out), encoding='utf-8')
    result = subprocess.run([str(dotnet), str(csc), '@' + str(rsp)], cwd=root, capture_output=True, text=True, encoding='utf-8', errors='replace')
    log = result.stdout + result.stderr
    (off / (name + '.log')).write_text(log, encoding='utf-8')
    summary[name] = dict(exit=result.returncode, errors=sum(': error ' in l for l in log.splitlines()), warnings=sum(': warning ' in l for l in log.splitlines()), sources=len(sources), bee_rsp=str(base))
    print(name, summary[name], flush=True)
    if result.returncode:
        print('\n'.join(l for l in log.splitlines() if ': error ' in l), flush=True)
        break
(off / 'compile-results.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
raise SystemExit(0 if len(summary) == 2 and all(x['exit'] == 0 for x in summary.values()) else 1)
