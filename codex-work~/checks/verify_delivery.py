from pathlib import Path
import hashlib,json,subprocess
r=Path('E:/Git/ant'); w=r/'codex-work~'; c=w/'checks'
results=json.loads((c/'off/compile-results.json').read_text()); assert len(results)==4 and all(v['exit']==0 and v['errors']==0 for v in results.values())
for d in json.loads((c/'reports/source-manifest.json').read_text()):
 assert hashlib.sha256((r/d['file']).read_bytes()).hexdigest()==d['original_sha256']
 assert hashlib.sha256((w/'src'/d['file']).read_bytes()).hexdigest()==d['modified_sha256']
external_changes=[]
for p,h in json.loads((c/'reports/original-hashes.json').read_text()).items():
 current=hashlib.sha256((r/p).read_bytes()).hexdigest()
 if current!=h:
  assert p in ('AgentScripts/ImplG06Checks.cs','AgentScripts/FishingChecks.cs'),p
  external_changes.append({'file':p,'start_sha256':h,'current_sha256':current})
(c/'reports/external-changes.json').write_text(json.dumps(external_changes,indent=2),encoding='utf-8')
patch=(w/'CHECKS-1025.patch').read_text(encoding='utf-8'); assert patch.count('--- a/AgentScripts/')==3 and 'ImplG06Checks' not in patch and 'FishingChecks' not in patch and '/UI/' not in patch
result=subprocess.run(['git','apply','--check','--whitespace=error',str(w/'CHECKS-1025.patch')],cwd=r,capture_output=True,text=True); assert result.returncode==0,result.stderr
assert '## 검사 —' in (w/'HANDOFF-CODEX-1025.md').read_text(encoding='utf-8-sig')
(c/'reports/final-verification.json').write_text(json.dumps({'game_compile_errors':0,'separate_check_compile_errors':0,'patch_check_exit':result.returncode,'modified_copies':3,'target_originals_unchanged':3,'external_changes':external_changes,'play_rerun':False},indent=2),encoding='utf-8')
print('Verified: 4 compilations / 0 errors; 3-file patch applies; LF preserved; 3 target originals unchanged; excluded scripts changed externally; handoff appended. Play not run.')
