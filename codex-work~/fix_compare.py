from pathlib import Path
p=Path('codex-work~/compare_armor.py'); s=p.read_text().replace('fit-report-motions.json','original-till-fit.json'); p.write_text(s)
