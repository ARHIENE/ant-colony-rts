from pathlib import Path
root=Path.cwd()
src=(root/'Temp/build-tooltip-1111/reviewed-game.rsp').read_text(encoding='utf-8-sig').splitlines()
lines=[s for s in src if not s.strip('"').endswith('.cs') and not s.startswith('-out:')]
lines += [s for s in src if s.strip('"').endswith('.cs') and 'Assets' in s and '_Project' not in s]
lines.append('-out:Temp/presentation-1111.dll')
for f in (root/'Assets/_Project/Scripts').rglob('*.cs'):
    if 'Editor' in f.parts: continue
    draft=root/'work/presentation-1111/draft'/f.name
    lines.append('"'+str(draft if draft.exists() else f)+'"')
(root/'work/presentation-1111/game.rsp').write_text('\n'.join(lines),encoding='utf-8')
board=root/'docs/IMPLEMENTATION_BOARD.md'
s=board.read_text(encoding='utf-8-sig');board.write_text(s.replace('`n- **PRESENTATION','\n- **PRESENTATION'),encoding='utf-8')
