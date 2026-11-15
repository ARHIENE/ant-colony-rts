from pathlib import Path
import json, hashlib
r=Path('F:/UnityProjects/ant-colony-rts'); p=r/'work/campaign-complete-1114/pending'; src=r/'Assets/_Project/Scripts'
hashfile=p.parent/'before-hashes.json'; hashes=json.loads(hashfile.read_text(encoding='utf-8-sig'))
def copy(name):
 f=src/name; dest=p/name
 if not dest.exists():
  dest.parent.mkdir(parents=True,exist_ok=True); dest.write_bytes(f.read_bytes())
  hashes.append({'Path':str(f),'Hash':hashlib.sha256(f.read_bytes()).hexdigest().upper()})
def replace(name,a,b):
 f=p/name; s=f.read_text(encoding='utf-8-sig'); assert a in s,(name,a); f.write_text(s.replace(a,b),encoding='utf-8')
for name in ['Save/SaveOrderValidation.cs','World/ExpeditionTransport.Orders.cs']: copy(name)
replace('Core/RocketCampaign.cs','if (shipBoss != null) Mothership.bossHp = shipBoss.CurrentHp; return','CaptureMothershipCombat(); return')
replace('Core/RocketCampaign.cs','operating && CampaignOutcome.Survivor(commander)\n                    && !commander.IsAwayFromHome','AwayOnMothership(commander) || operating && CampaignOutcome.Survivor(commander)\n                    && !commander.IsAwayFromHome')
replace('World/WorldMapManager.cs','if (ViewedSite == null) homeFocus = camera.FocusPoint;','if (ViewedSite == null && !AntColony.Core.RocketCampaign.ShipBounds.Contains(camera.FocusPoint)) homeFocus = camera.FocusPoint;\n            AntColony.Core.RocketCampaign.Instance?.LeaveMothershipView();')
replace('World/ExpeditionRules.cs','public static bool Paused { get; private set; } = true;','public static bool Paused { get; private set; } = false;')
replace('World/WorldMapExpeditions.cs','var party = CreateTransport(false, commanders[0].Position, true);','if (!AntColony.Core.HomeReturnPoint.TryGround(commanders[0].Position, out var gate)\n                || !commanders[0].TryWorkApproach(gate, out gate))\n            { AntColony.UI.ToastManager.Show("출정 길목까지 이어지는 경로가 없습니다."); return null; }\n            var party = CreateTransport(false, gate, true);')
replace('World/ExpeditionTransport.Orders.cs','Vector3.Distance(c.Position, Position) > 8','Vector3.Distance(c.Position, Position) > (Walking ? 3 : 8)')
replace('World/ExpeditionTransport.Home.cs','if (HomeHasEnemies()) return false;','// 도보 귀환대는 길목으로 돌아와 직접 교전한다. 별도 맞이 호위전을 요구하지 않는다.\n            if (!Walking && HomeHasEnemies()) return false;')
replace('Save/SaveOrderValidation.cs','foreach (var d in f.monsters) Add(d.key);','foreach (var d in f.monsters) Add(d.key);\n            if (f.rocket?.mothership?.reached == true) {\n                Add("mothership:boss");\n                var members = new HashSet<string>(f.commanders.Select(c => c.personalState?.id));\n                Check(f.rocket.mothership.away.All(members.Contains), "본선 원정대 소속");\n            }')
replace('Core/RocketCampaign.Mothership.cs','&& (!s.reached ? s.away.Count==0 && !s.victorious : true)','&& (!s.reached ? s.away.Count==0 && !s.victorious && !s.viewing : true)')
hashfile.write_text(json.dumps(hashes,ensure_ascii=False,indent=2),encoding='utf-8')
# Compile against current Assets, including the other chat's applied enemy implementation.
s=(r/'work/organic-combat-1114/draft-compile.rsp').read_text(encoding='utf-8-sig')
s=s.replace('work/organic-combat-1114/code/','Assets/_Project/Scripts/')
s=s.replace('work/organic-combat-1114/Assembly-CSharp','work/campaign-complete-1114/Assembly-CSharp')
for f in p.rglob('*.cs'):
 old='Assets/_Project/Scripts/'+f.relative_to(p).as_posix(); new=f.relative_to(r).as_posix()
 if old in s: s=s.replace(old,new)
 else: s+='\n"'+new+'"\n'
(p.parent/'compile.rsp').write_text(s,encoding='utf-8')
