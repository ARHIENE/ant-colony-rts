from pathlib import Path
root=Path('Assets/_Project/Scripts');dest=Path('work/commander-only-1112/pending')
changes={
'UI/BuildScreen.cs':('    병력 {c.TroopCount}/{c.CommandLimit}', '    체력 {c.PersonalHealth:0}'),
'UI/WorldMapPanel.cs':('병력 {c.TroopCount}/{c.CommandLimit} · ','장수 · '),
'UI/GameMenuWalking.cs':(' · 병력 {c.TroopCount}', ''),
'UI/GameMenuRocket.cs':('장수 1명 + 병력 {commander.TroopCount}명','장수 1명')}
for rel,(a,b) in changes.items():
 s=(root/rel).read_text(encoding='utf-8-sig');assert a in s;s=s.replace(a,b);(dest/rel).write_text(s,encoding='utf-8')
print('Prepared remaining visible troop text removal.')
