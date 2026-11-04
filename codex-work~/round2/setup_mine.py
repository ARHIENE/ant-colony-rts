from pathlib import Path
import shutil,json
w=Path('E:/Git/ant/codex-work~'); r=w/'round2'; d=w/'round2-mine'; (d/'blender').mkdir(parents=True,exist_ok=True); (d/'reports').mkdir(exist_ok=True); shutil.copy2(r/'blender/baseline.blend',d/'blender/baseline.blend'); (d/'reports/presets.json').write_text('{}')
s=(r/'repair_fast.py').read_text(); s=s.replace("OUT=W/'round2'", "OUT=W/'round2-mine'").replace("[('Tool_Dig','Tool_Shovel_Steel',{'Part_Hair_BOB_00'}),('Tool_Mine','Tool_Pickaxe_Steel',{'Armor_Breastplate','Armor_Breastplate_Lower'})]", "[('Tool_Mine','Tool_Pickaxe_Steel',{'Armor_Breastplate','Armor_Breastplate_Lower'})]")
(d/'repair.py').write_text(s)
