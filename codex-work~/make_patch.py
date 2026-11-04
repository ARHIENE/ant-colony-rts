from pathlib import Path
import difflib,subprocess,json
r=Path('E:/Git/ant'); w=r/'codex-work~'; p='Assets/_Project/Scripts/World/ProductionProp.cs'
a=(r/p).read_bytes().decode('utf-8-sig'); b=a.replace('            ResourceType.Stone =>', '            ResourceType.Food => \"Prop_PreservedRation\",\r\n            ResourceType.Stone =>'); (w/'src'/p).write_bytes(b.encode('utf-8'))
patch=''.join(difflib.unified_diff(a.splitlines(True),b.splitlines(True),fromfile='a/'+p,tofile='b/'+p))
(w/'GRAPHICS-1025.patch').write_bytes(patch.encode('utf-8'))
result=subprocess.run(['git','-c','core.whitespace=cr-at-eol','apply','--check','--whitespace=error',str(w/'GRAPHICS-1025.patch')],cwd=r,capture_output=True,text=True)
(w/'reports/patch-check.txt').write_text(f'exit={result.returncode}\n{result.stdout}{result.stderr}',encoding='utf-8')
assert result.returncode==0,result.stderr
assert (r/'Assets/Resources/Kemono/Props/Prop_PreservedRation.fbx').exists()
assert a.replace('            ResourceType.Stone =>', '            ResourceType.Food => "Prop_PreservedRation",\r\n            ResourceType.Stone =>') == b
assert 'ProductionProp.ShowResource(this);' in (r/'Assets/_Project/Scripts/World/ResourceNode.cs').read_text(encoding='utf-8-sig')
print('Patch applies; only Food visual mapping differs; imported FBX exists; ConfigureLoot already calls ShowResource.')
