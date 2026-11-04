from pathlib import Path
p=Path('codex-work~/round2/audit_baseline.py'); s=p.read_text(encoding='utf-8-sig').replace("R/'blender/baseline.blend'","R/'blender/pk-motions-round2.blend'").replace('baseline-tools-all-frames.json','final-tools-all-frames.json'); Path('codex-work~/round2/audit_final.py').write_text(s,encoding='utf-8')
