from pathlib import Path
w=Path('E:/Git/ant/codex-work~'); p=w/'blender/scripts/repair_wrist_roll.py'
s=p.read_text()
s=s.replace("angle = min(range(-150, 151, 5), key=score)", """def actual_score(angle):
            h = socket @ Matrix.Rotation(math.radians(angle), 4, 'Y') @ hand_in
            solve_arm('R', h)
            return wrist_bend('R') + .005 * abs(angle) + .01 * abs(angle - previous)
        angle = min(range(-180, 181, 5), key=actual_score)""")
s=s.replace("root / 'reports/wrist-repair.json'", "root / 'reports/wrist-actual.json'")
p=w/'blender/scripts/repair_wrist_actual.py'; p.write_text(s)
