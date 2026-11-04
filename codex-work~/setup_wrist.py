from pathlib import Path
w=Path('E:/Git/ant/codex-work~'); f=w/'blender/scripts/repair_wrist_roll.py'
s=f.read_text(); s=s.replace("root = Path('E:/Git/ant')", "root = Path('E:/Git/ant/codex-work~')").replace("kit = root / 'art/production-kit-20261019'", "kit = root / 'blender'")
s=s.replace("[('Tool_Mine', 'Tool_Pickaxe_Steel'), ('Tool_Dig', 'Tool_Shovel_Steel'), ('Tool_Till', 'Tool_Hoe_Steel')]", "[('Tool_Till', 'Tool_Hoe_Steel')]")
s=s.replace('range(-45, 46, 5)', 'range(-150, 151, 5)').replace('arm_cost(\'R\', h, shoulder) + math.radians(.02 * abs(angle) + .04 * abs(angle - previous))', 'arm_cost(\'R\', h, shoulder) + math.radians(.005 * abs(angle) + .01 * abs(angle - previous))')
s=s.replace("root / 'Temp/graphics-1024/pk-motions-before-wrist.blend'", "root / 'blender/pk-motions-before-wrist.blend'").replace("root / 'Temp/graphics-1024/wrist-repair.json'", "root / 'reports/wrist-repair.json'")
f.write_text(s)
