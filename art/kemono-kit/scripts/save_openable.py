"""Turn the source parts into a normal Blender document that opens with a visible character."""
import bpy
import json
from pathlib import Path
from mathutils import Vector
root=Path('E:/Git/ant/art/kemono-kit')
bpy.ops.wm.open_mainfile(filepath=str(root/'kemono-preview.blend'))
s=bpy.context.scene
recipe=json.loads((root/'parts-manifest.json').read_text(encoding='utf-8'))['default']
rig=next(o for o in s.objects if o.name=='KemonoRig')
rig.animation_data_clear();rig.data.pose_position='REST';rig.hide_set(False);rig.hide_render=False
for o in list(s.objects):
    if o.name.startswith('Combo_'):
        bpy.data.objects.remove(o,do_unlink=True)
    elif o.name.startswith('Part_'):
        choice=recipe.get(o['slot'],[])
        if isinstance(choice,str):choice=[choice]
        hide=o['choice'] not in choice
        o.hide_set(hide);o.hide_render=hide
s.name='Kemono_Parts_Manual'
s.camera.location=(.95,-3,.98)
s.camera.rotation_euler=(Vector((0,0,.75))-s.camera.location).to_track_quat('-Z','Y').to_euler()
s.camera.data.ortho_scale=1.78
s.render.resolution_x=900;s.render.resolution_y=1000
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        area.spaces.active.overlay.show_overlays=False
for im in bpy.data.images:
    if im.has_data and not im.packed_file:
        try:im.pack()
        except RuntimeError:pass
bpy.ops.wm.save_as_mainfile(filepath=str(root/'kemono-character-kit.blend'))
assert s.name=='Kemono_Parts_Manual' and len(s.objects)>100
print('Saved visible scene; verify by reopening from a separate Blender session:',s.name)
