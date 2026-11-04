from pathlib import Path
w=Path('E:/Git/ant/codex-work~'); s=(w/'blender/scripts/repair_wrist_slide_fine.py').read_text(); s=s.replace("bpy.ops.wm.open_mainfile(filepath=str(kit/'pk-motions.blend'))","bpy.ops.wm.open_mainfile(filepath='E:/Git/ant/art/production-kit-20261019/pk-motions.blend')")
s=s.replace("    best=None; candidates=[]", """    if wrist_bend('R')<59:
        poses[frame]={n:rig.pose.bones[n].matrix.copy() for s in 'RL' for n in ('upper_arm.'+s,'forearm.'+s,'hand.'+s)}
        rolls.append(0); offsets.append([0,0,0]); previous=Vector((0,0,0)); prevroll=0
        continue
    best=None; candidates=[]""")
s=s.replace("root/'reports/wrist-slide-fine.json'","root/'reports/wrist-surgical.json'")
(w/'blender/scripts/repair_wrist_surgical.py').write_text(s)
