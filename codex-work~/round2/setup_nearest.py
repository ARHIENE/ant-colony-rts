from pathlib import Path
import ast,json,shutil
w=Path('E:/Git/ant/codex-work~'); r=w/'round2'; dest=w/'round2-nearest'; (dest/'reports').mkdir(parents=True,exist_ok=True); (dest/'blender').mkdir(exist_ok=True); shutil.copy2(r/'blender/baseline.blend',dest/'blender/baseline.blend')
# Retain proven poses using the original solver; use nearest-elbow solves on as-yet-unsolved frames.
presets=json.loads((r/'reports/presets.json').read_text())
for line in (r/'reports/search-fast.log').read_text(encoding='utf-16').splitlines():
 if line.startswith('FOUND '):
  _,clip,body=line.split(' ',2); d=ast.literal_eval(body); presets.setdefault(clip,{})[str(d['frame'])]=d
(dest/'reports/presets.json').write_text(json.dumps(presets,indent=2),encoding='utf-8')
s=(r/'repair_fast.py').read_text(); s=s.replace("OUT=W/'round2'", "OUT=W/'round2-nearest'")
s=s.replace("RADIUS=float(sys.argv", """original_solve_arm=solve_arm
solver_node=next(n for n in source.body if isinstance(n,ast.FunctionDef) and n.name=='solve_arm')
nearcode=ast.unparse(solver_node).replace('def solve_arm(', 'def nearest_solve_arm(')
nearcode=nearcode.replace('cost = fdir.angle(hand_y) +', \"cost = 50 * max(0, fdir.angle(hand_y) - math.radians(59.5)) + 30 * (E - ELBOW_REFERENCE[side]).length +\")
exec(nearcode)
ELBOW_REFERENCE={}
RADIUS=float(sys.argv""")
s=s.replace("  socket=rig.pose.bones['Socket.Tool.R'].matrix.copy();", "  ELBOW_REFERENCE={s:rig.pose.bones['forearm.'+s].head.copy() for s in 'RL'}\n  solve_arm=original_solve_arm if PRESETS.get(clip,{}).get(str(f)) else nearest_solve_arm\n  socket=rig.pose.bones['Socket.Tool.R'].matrix.copy();")
(dest/'repair.py').write_text(s,encoding='utf-8')
print('Presets', {k:list(v) for k,v in presets.items()})
