from pathlib import Path
w=Path('E:/Git/ant/codex-work~'); s=(w/'blender/scripts/repair_wrist_slide_clearance.py').read_text(encoding='utf-8-sig')
s=s.replace("for x in (-.03,0,.03):", "for x in (-.03,-.02,-.01,0,.01,.02,.03):").replace("for y in (-.03,0,.03):", "for y in (-.03,-.02,-.01,0,.01,.02,.03):").replace("for z in (-.03,0,.03):", "for z in (-.03,-.02,-.01,0,.01,.02,.03):")
s=s.replace("root/'reports/wrist-slide-clearance.json'","root/'reports/wrist-slide-fine.json'")
s=s.replace("bends={'R':0,'L':0}; left_error=0", "bends={'R':0,'L':0}; per_frame=[]; left_error=0")
s=s.replace("    left_error=max(left_error,", "    per_frame.append([frame,wrist_bend('R'),wrist_bend('L')])\n    left_error=max(left_error,")
s=s.replace("report={'accepted':keep,", "report={'per_frame_deg':per_frame,'accepted':keep,")
(w/'blender/scripts/repair_wrist_slide_fine.py').write_text(s,encoding='utf-8')
