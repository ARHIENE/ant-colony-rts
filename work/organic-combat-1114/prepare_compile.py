from pathlib import Path
root=Path(__file__).resolve().parent
project=root.parents[1]
source=project/'Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp'
lines=source.read_text(encoding='utf-8-sig').splitlines()
drafts={('Assets/_Project/Scripts/'+p.relative_to(root/'code').as_posix()):p.relative_to(project).as_posix() for p in (root/'code').rglob('*.cs')}
seen=set(); result=[]
for line in lines:
    path=line.strip('"')
    if path in drafts:
        seen.add(path); line='"'+drafts[path]+'"'
    if line.startswith('-out:'): line='-out:"work/organic-combat-1114/Assembly-CSharp.dll"'
    if line.startswith('-refout:'): line='-refout:"work/organic-combat-1114/Assembly-CSharp.ref.dll"'
    result.append(line)
for path in drafts:
    if path not in seen: result.append('"'+drafts[path]+'"')
(root/'draft-compile.rsp').write_text('\n'.join(result),encoding='utf-8')
test=[line for line in result if not line.startswith('"') and not line.startswith('-out:') and not line.startswith('-refout:')]
test += ['-out:"work/organic-combat-1114/OrganicCombatChecks.dll"','-r:"work/organic-combat-1114/Assembly-CSharp.dll"','"work/organic-combat-1114/OrganicCombat1114Checks.cs"']
(root/'test-compile.rsp').write_text('\n'.join(test),encoding='utf-8')
print(len(drafts),'drafts; compiler outputs stay in work.')
