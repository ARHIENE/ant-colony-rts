from pathlib import Path
import shutil,json,hashlib
root=Path('E:/Git/ant'); work=root/'codex-work~'; kit=root/'art/production-kit-20261019'
for f in (kit/'scripts').glob('*.py'): shutil.copy2(f,work/'blender/scripts'/f.name)
shutil.copy2(kit/'pk-motions.blend',work/'blender/pk-motions.blend')
shutil.copy2(kit/'previews/carry-placement.json',work/'blender/previews/carry-placement.json')
p='Assets/_Project/Scripts/World/ProductionProp.cs'
s=(root/p).read_text(encoding='utf-8-sig'); s=s.replace('ResourceType.Stone =>', 'ResourceType.Food => "Prop_PreservedRation",\n            ResourceType.Stone =>')
(work/'src'/p).write_text(s,encoding='utf-8')
(work/'IMPLEMENTATION_BOARD.md').write_text('# GRAPHICS-1025 / Codex\n\n담당: 보존식 표시, Blender 손목/관통 검증, 오프라인 컴파일.\n수정 소스: Assets/_Project/Scripts/World/ProductionProp.cs (src 사본만).\nBlender: blender/pk-motions.blend 및 scripts 사본. UI 및 기존 파일 수정 없음.\n',encoding='utf-8')
# Route every kit script to the isolated copy; bytecode also stays in this folder.
for f in (work/'blender/scripts').glob('*.py'):
 s=f.read_text(encoding='utf-8-sig').replace('E:/Git/ant/art/production-kit-20261019','E:/Git/ant/codex-work~/blender').replace(r'E:\Git\ant\art\production-kit-20261019\scripts',r'E:\Git\ant\codex-work~\blender\scripts')
 f.write_text(s,encoding='utf-8')
files=[root/p,kit/'pk-motions.blend',*list((root/'Assets/_Project/Art/Kemono/Anim').glob('*Tool_*.fbx')),*list((kit/'scripts').glob('*.py'))]
(work/'reports/original-hashes.json').write_text(json.dumps({str(f.relative_to(root)):hashlib.sha256(f.read_bytes()).hexdigest() for f in files},indent=2),encoding='utf-8')
