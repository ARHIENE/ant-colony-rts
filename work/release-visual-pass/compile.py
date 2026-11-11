from pathlib import Path
r=Path.cwd(); p=r/'work/release-visual-pass'; src=(r/'Temp/build-tooltip-1111/reviewed-game.rsp').read_text(encoding='utf-8-sig').splitlines()
lines=[s for s in src if not s.strip('"').endswith('.cs') and not s.startswith('-out:')]
lines += [s for s in src if s.strip('"').endswith('.cs') and 'Assets' in s and '_Project' not in s]
lines.append('-out:Temp/release-visual-pass.dll')
for f in (r/'Assets/_Project/Scripts').rglob('*.cs'):
 if 'Editor' in f.parts: continue
 draft=p/'draft'/f.name
 lines.append('"'+str(draft if draft.exists() else f)+'"')
(p/'game.rsp').write_text('\n'.join(lines),encoding='utf-8')
