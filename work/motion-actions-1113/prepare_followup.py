from pathlib import Path
import hashlib,json
r=Path('F:/UnityProjects/ant-colony-rts'); out=r/'work/motion-actions-1113/followup';out.mkdir(exist_ok=True)
files=['Units/AntVisual.Work.cs','Units/CommanderJoy.cs']
manifest=[]
for rel in files:
 p=r/'Assets/_Project/Scripts'/rel;s=p.read_text(encoding='utf-8-sig');before=s
 if p.name=='AntVisual.Work.cs':
  old='private string JoyClip() => !commander.PersonalState.joy.freeWalk'
  s=s.replace(old,'private string JoyClip()\n        {\n            if (commander.AtPlaySpot && commander.PlaySpot) Face(commander.PlaySpot.Position - transform.position);\n            return !commander.PersonalState.joy.freeWalk')
  s=s.replace('11 => "Swing", _ => "Rest"\n        };','11 => "Swing", _ => "Rest"\n            };\n        }')
 else:
  old='var spot = RecreationSpot.All.Where(s => s != null && s.HasSeat).OrderBy(s => (s.Position - Position).sqrMagnitude).FirstOrDefault();'
  new='RecreationSpot spot = null;\n            Vector3 approach = default;\n            foreach (var candidate in RecreationSpot.All.Where(s => s != null && s.HasSeat).OrderBy(s => (s.Position - Position).sqrMagnitude))\n                if (TryPlayApproach(candidate, out approach)) { spot = candidate; break; }'
  assert old in s;s=s.replace(old,new)
  s=s.replace('            if (!TryPlayApproach(spot, out var approach))\n            { j.retrySeconds = GameBalance.StarveRetrySeconds; return false; }\n','')
 assert s!=before
 (out/p.name).write_text(s,encoding='utf-8')
 manifest.append({'target':str(p),'before_sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'candidate':str(out/p.name)})
(out/'patch-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
p=r/'AgentScripts/Motion1113FarmCheck.cs';s=p.read_text(encoding='utf-8-sig');s=s.replace('bool fitted=MeshyWorkContact.TryRadius(farm.Node,"Tool_Till",outward,c.Position.y,out float radius);','if (!Physics.Raycast(c.Position+Vector3.up*8,Vector3.down,out var floor,24,1<<8)) throw new Exception("No terrain below farmer");\n  bool fitted=MeshyWorkContact.TryRadius(farm.Node,"Tool_Till",outward,floor.point.y,out float radius);');p.write_text(s,encoding='utf-8')
print('Prepared recreation facing/reachable alternatives; corrected farm measurement helper. Assets unchanged.')
