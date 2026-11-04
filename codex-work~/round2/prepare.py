from pathlib import Path
import hashlib,json
r=Path('E:/Git/ant'); w=r/'codex-work~'
files=[r/'art/production-kit-20261019/pk-motions.blend',r/'Assets/_Project/Scripts/World/ProductionProp.cs',*list((r/'Assets/_Project/Art/Kemono/Anim').glob('*PK_Tool_*.fbx'))]
(w/'round2/reports/original-hashes.json').write_text(json.dumps({str(p.relative_to(r)):hashlib.sha256(p.read_bytes()).hexdigest() for p in files},indent=2),encoding='utf-8')
with (w/'IMPLEMENTATION_BOARD.md').open('a',encoding='utf-8') as f: f.write('\n## 2차\nCodex: PK|Tool_Dig / PK|Tool_Mine 사본 보정·전체 정수 프레임 검증·FBX 출력. 기존 소스/UI 수정 없음.\n')
