from pathlib import Path
root=Path('Assets/_Project/Scripts');dest=Path('work/commander-only-1112/pending')
for rel,a,b in [
('World/WaveDefense.cs','!RocketCampaign.SuppressNewCrises && ExpeditionRules.HomeCommanders > 0','!RocketCampaign.SuppressNewCrises'),
('World/LocalIncursions.cs','if (faction == null && ExpeditionRules.HomeCommanders == 0) return false;','if (!wave && faction == null && ExpeditionRules.HomeCommanders == 0) return false;')]:
 s=(root/rel).read_text(encoding='utf-8-sig');assert a in s;s=s.replace(a,b);p=dest/rel;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(s,encoding='utf-8')
print('Unattended-wave pending changes prepared; Assets untouched.')
