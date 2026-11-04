from pathlib import Path
w=Path('E:/Git/ant/codex-work~')
for name in ('fit_original_till.py','fit_till.py'):
 p=w/'blender/scripts'/name; s=p.read_text(); s=s.replace("rig.data.pose_position = 'POSE'", "rig.data.pose_position = 'POSE'\nfor pb in rig.pose.bones: pb.matrix_basis.identity()\n#"); p.write_text(s)
