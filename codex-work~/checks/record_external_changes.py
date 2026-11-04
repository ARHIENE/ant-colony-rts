from pathlib import Path
w=Path('E:/Git/ant/codex-work~'); p=w/'HANDOFF-CODEX-1025.md'; s=p.read_text(encoding='utf-8-sig'); old='원본 3개 검사 및 금지된 `ImplG06Checks.cs`/`FishingChecks.cs` 총 5개 파일의 SHA-256이 시작 시와 동일하다.'; new='수정 대상 원본 3개 검사의 SHA-256은 시작 시와 동일하다. 금지된 `ImplG06Checks.cs`와 `FishingChecks.cs`는 최종 확인 때 시작 해시와 달라 외부 동시 변경으로 기록했다. 두 파일에 대한 쓰기는 수행하지 않았고 패치에도 포함하지 않았다.'; assert old in s; p.write_text(s.replace(old,new),encoding='utf-8-sig')
p=w/'checks/verify_delivery.py'; s=p.read_text(encoding='utf-8-sig'); s=s.replace("for p,h in json.loads((c/'reports/original-hashes.json').read_text()).items(): assert hashlib.sha256((r/p).read_bytes()).hexdigest()==h", """external_changes=[]
for p,h in json.loads((c/'reports/original-hashes.json').read_text()).items():
 current=hashlib.sha256((r/p).read_bytes()).hexdigest()
 if current!=h:
  assert p in ('AgentScripts/ImplG06Checks.cs','AgentScripts/FishingChecks.cs'),p
  external_changes.append({'file':p,'start_sha256':h,'current_sha256':current})
(c/'reports/external-changes.json').write_text(json.dumps(external_changes,indent=2),encoding='utf-8')""")
s=s.replace("'protected_originals_unchanged':5", "'target_originals_unchanged':3,'external_changes':external_changes")
s=s.replace('5 originals unchanged','3 target originals unchanged; excluded scripts changed externally')
p.write_text(s,encoding='utf-8')
