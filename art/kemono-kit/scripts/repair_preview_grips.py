import bpy,sys,shutil
from pathlib import Path
root=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(root/'scripts'))
from tool_grips import fit_tool
path=root/'kemono-preview-interactions.blend'
backup=root/'kemono-preview-interactions.before-grip-fix.blend'
if not backup.exists():shutil.copy2(path,backup)
bpy.ops.wm.open_mainfile(filepath=str(path))
changed=[]
for scene in bpy.data.scenes:
    with bpy.context.temp_override(scene=scene,view_layer=scene.view_layers[0]):
        for tool in scene.objects:
            if tool.parent_type=='BONE' and tool.name.split('.')[0] in ('Tool_Axe','Tool_Hammer','Tool_Pickaxe','Prop_Spear'):
                fit_tool(scene,tool);changed.append(scene.name)
bpy.ops.wm.save_as_mainfile(filepath=str(path))
(root/'grip-fixed-scenes.txt').write_text('\n'.join(sorted(set(changed))),encoding='utf-8')
print('GRIP_FIXED',len(changed))
