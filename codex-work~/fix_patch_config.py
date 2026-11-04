from pathlib import Path
p=Path('E:/Git/ant/codex-work~/make_patch.py'); s=p.read_text().replace("['git','apply','--check'", "['git','-c','core.whitespace=cr-at-eol','apply','--check'"); p.write_text(s)
