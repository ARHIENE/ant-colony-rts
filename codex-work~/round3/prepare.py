from pathlib import Path
import hashlib,json,shutil
r=Path('E:/Git/ant'); w=r/'codex-work~'; out=w/'round3'
files=[r/'art/production-kit-20261019/pk-motions.blend',r/'Assets/Resources/Kemono/Kemono_Character.fbx',r/'Assets/_Project/Art/Kemono/Anim/Kemono@PK_Tool_Till.fbx',r/'Assets/_Project/Art/Kemono/Anim/Kemono@PK_Tool_Mine.fbx']
(out/'reports/original-hashes.json').write_text(json.dumps({p.relative_to(r).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in files},indent=2),encoding='utf-8')
shutil.copy2(files[0],out/'blender/source.blend')
manifest=json.loads((w/'FBX-MANIFEST.json').read_text()); print('Applied animations:',[(m['target'],hashlib.sha256((r/m['target']).read_bytes()).hexdigest()==m['sha256']) for m in manifest])
with (w/'IMPLEMENTATION_BOARD.md').open('a',encoding='utf-8') as f: f.write('\n## GRAPHICS 3차\nCodex: Part_Body_BASE_ARMOR / Part_Outfit_LINEN_ARMOR 사본 형상 보정, 1230샘플·렌더 검증. 기본 메시/장비/UI/기존파일 변경 금지.\n')
