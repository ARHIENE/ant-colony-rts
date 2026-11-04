from pathlib import Path
import difflib,subprocess,json,hashlib
r=Path('E:/Git/ant'); w=r/'codex-work~'; names=['WorldMapChecks','SettlementDefenseChecks','TransportRouteChecks']; patch=''; stats=[]
for name in names:
 p='AgentScripts/'+name+'.cs'; a=(r/p).read_bytes(); b=(w/'src'/p).read_bytes(); ta=a.decode('utf-8-sig'); tb=b.decode('utf-8-sig')
 assert (b'\r\n' in a)==(b'\r\n' in b),p
 assert a.startswith(b'\xef\xbb\xbf')==b.startswith(b'\xef\xbb\xbf'),p
 patch+=''.join(difflib.unified_diff(ta.splitlines(True),tb.splitlines(True),fromfile='a/'+p,tofile='b/'+p))
 stats.append({'file':p,'newline':'CRLF' if b'\r\n' in a else 'LF','original_sha256':hashlib.sha256(a).hexdigest(),'modified_sha256':hashlib.sha256(b).hexdigest()})
(w/'CHECKS-1025.patch').write_bytes(patch.encode('utf-8'))
p=subprocess.run(['git','apply','--check','--whitespace=error',str(w/'CHECKS-1025.patch')],cwd=r,capture_output=True,text=True)
(w/'checks/reports/patch-check.txt').write_text(f'exit={p.returncode}\n{p.stdout}{p.stderr}',encoding='utf-8'); assert p.returncode==0,p.stderr
for path,h in json.loads((w/'checks/reports/original-hashes.json').read_text()).items(): assert hashlib.sha256((r/path).read_bytes()).hexdigest()==h,path
(w/'checks/reports/source-manifest.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
print('Patch applies; 3 LF files; original 3 checks and protected ImplG06Checks/FishingChecks unchanged.')
