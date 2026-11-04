from pathlib import Path
import shutil
w=Path('E:/Git/ant/codex-work~'); base=w/'round2-nearest'; d=w/'round2-dig5'; (d/'blender').mkdir(parents=True,exist_ok=True); (d/'reports').mkdir(exist_ok=True); shutil.copy2(base/'blender/baseline.blend',d/'blender/baseline.blend'); shutil.copy2(base/'reports/presets.json',d/'reports/presets.json')
s=(base/'repair.py').read_text(); s=s.replace("OUT=W/'round2-nearest'", "OUT=W/'round2-dig5'").replace("[('Tool_Dig','Tool_Shovel_Steel',{'Part_Hair_BOB_00'}),('Tool_Mine','Tool_Pickaxe_Steel',{'Armor_Breastplate','Armor_Breastplate_Lower'})]", "[('Tool_Dig','Tool_Shovel_Steel',{'Part_Hair_BOB_00'})]")
s=s.replace("frames=list(range(1,int(original.get('duration_frames',original.frame_range[1]))+2))", "frames=[18]")
s=s.replace("     if off.length>RADIUS+.000001: continue", "     if off.length>RADIUS+.000001 or off.length<=.030001: continue")
(d/'repair.py').write_text(s)
