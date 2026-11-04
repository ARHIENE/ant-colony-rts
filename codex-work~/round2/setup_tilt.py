from pathlib import Path
import shutil
w=Path('E:/Git/ant/codex-work~'); src=w/'round2-dig5'; d=w/'round2-digtilt'; (d/'blender').mkdir(parents=True,exist_ok=True); (d/'reports').mkdir(exist_ok=True); shutil.copy2(src/'blender/baseline.blend',d/'blender/baseline.blend'); shutil.copy2(src/'reports/presets.json',d/'reports/presets.json')
s=(src/'repair.py').read_text(); s=s.replace("OUT=W/'round2-dig5'", "OUT=W/'round2-digtilt'").replace(' or off.length<=.030001','')
start=s.index('     for angle in range(-45,46,15):'); end=s.index('  # Static head/torso quick gate;',start)
s=s[:start]+'''     for angle in (-15,0,15):
      for tx in (-10,-5,0,5,10):
       for tz in (-10,-5,0,5,10):
        P=Matrix.Translation(off)@socket@Matrix.Rotation(math.radians(tx),4,'X')@Matrix.Rotation(math.radians(angle),4,'Y')@Matrix.Rotation(math.radians(tz),4,'Z')
        hr=P@hand_in; hl=P@socket.inverted()@left
        c=off.length*100+.1*(abs(tx)+abs(tz))+.05*abs(angle)+.01*max(arm_cost('R',hr,shoulders['R']),arm_cost('L',hl,shoulders['L']))
        candidates.append((c,off,angle,hr,hl,P))
'''+s[end:]
s=s.replace("'roll_deg':angle,'trials':trials", "'roll_deg':angle,'socket_matrix':[list(row) for row in P],'trials':trials")
(d/'repair.py').write_text(s)
