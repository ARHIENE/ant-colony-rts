"""Repair only four tool meshes in the saved library; preserve all other assets."""
import bpy,sys,shutil
from pathlib import Path
root=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(root/'scripts'))
from tool_shapes import repair_tools
path=root/'resident-tools.blend'
backup=root/'resident-tools.before-grip-fix.blend'
if not backup.exists():shutil.copy2(path,backup)
bpy.ops.wm.open_mainfile(filepath=str(path))
objects=repair_tools()
bpy.ops.wm.save_as_mainfile(filepath=str(path))
for obj in objects:
    for other in bpy.context.scene.objects:other.select_set(False)
    obj.select_set(True);bpy.context.view_layer.objects.active=obj
    loc=obj.location.copy();obj.location=(0,0,0)
    bpy.ops.export_scene.fbx(filepath=str(root/'exports'/'tools'/(obj.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS')
    obj.location=loc
print('REPAIRED', [o.name for o in objects])
