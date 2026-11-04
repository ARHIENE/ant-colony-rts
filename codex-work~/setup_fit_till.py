from pathlib import Path
w=Path('E:/Git/ant/codex-work~'); p=w/'blender/scripts/fit_check.py'; s=p.read_text(); s=s.replace("PARTS = [", "ACTS = {'Tool_Till': ACTS['Tool_Till']}\nPARTS = [",1); s=s.replace("'fit-report-motions.json' if MOTIONS else 'fit-report.json'", "'final-till-fit.json' if MOTIONS else 'fit-report.json'"); (w/'blender/scripts/fit_till.py').write_text(s)
