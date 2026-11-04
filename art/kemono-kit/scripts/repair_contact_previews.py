import bpy,sys,json,shutil
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(root/'scripts'))
from tool_contact import apply_contact
path=root/'kemono-preview-interactions.blend'
backup=root/'kemono-preview-interactions.before-contact.blend'
if not backup.exists():shutil.copy2(path,backup)
bpy.ops.wm.open_mainfile(filepath=str(backup))
results=[]
for scene in bpy.data.scenes:
    bpy.context.window_manager.windows[0].scene=scene
    for tool in list(scene.objects):
        if tool.parent_type!='BONE' or tool.name.split('.')[0] not in ('Tool_Axe','Tool_Pickaxe','Tool_Hammer','Prop_Spear'):continue
        apply_contact(scene,tool)
        if tool.get('contact_ik'):
            scene.frame_set(tool['contact_frame']);scene.view_layers[0].update()
            error=((tool.matrix_world@Vector(tool['contact_edge']))-Vector(tool['contact_point'])).length
            results.append({'scene':scene.name,'contact_error':error})
bpy.ops.wm.save_as_mainfile(filepath=str(path))
(root/'contact-validation.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
print('CONTACT',results)
