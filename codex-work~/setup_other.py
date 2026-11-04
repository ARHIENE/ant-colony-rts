from pathlib import Path
w=Path('E:/Git/ant/codex-work~'); s=(w/'blender/scripts/repair_wrist_roll.py').read_text(encoding='utf-8-sig')
s=s.replace("[('Tool_Till', 'Tool_Hoe_Steel')]","[('Tool_Mine', 'Tool_Pickaxe_Steel'), ('Tool_Dig', 'Tool_Shovel_Steel')]")
s=s.replace('range(-150, 151, 5)','range(-45, 46, 5)')
s=s.replace("count(after_clear) <= count(before_clear)","all(v['frames'] <= before_clear.get(n, {}).get('frames', 0) for n, v in after_clear.items())")
s=s.replace("root / 'reports/wrist-repair.json'","root / 'reports/wrist-other.json'")
# Independent candidate experiment: never save concurrently with the hoe experiment.
s=s.replace('if accepted:\n    backup', 'if False:\n    backup')
(w/'blender/scripts/repair_wrist_other.py').write_text(s,encoding='utf-8')
