"""Open resident-character-kit.blend and export:
exports/character/Resident_Parts.fbx  — ResidentRig + every Part_* (skinned); toggle meshes on/off in the engine.
exports/character/Resident_Combo_X.fbx — manual combo samples (rig + their parts).
"""
import bpy
from pathlib import Path

KIT = Path(r"E:\Git\ant\art\resident-kit")
bpy.ops.wm.open_mainfile(filepath=str(KIT / 'resident-character-kit.blend'))
scene = bpy.context.scene
vl = scene.view_layers[0]
out = KIT / 'exports' / 'character'
out.mkdir(parents=True, exist_ok=True)


def export(path, rig, meshes):
    objs = [rig] + meshes
    hidden = {o: (o.hide_viewport, o.hide_get(view_layer=vl)) for o in objs}
    for o in objs:
        o.hide_viewport = False; o.hide_set(False, view_layer=vl)
    for o in scene.objects: o.select_set(o in objs, view_layer=vl)
    with bpy.context.temp_override(view_layer=vl, active_object=rig, selected_objects=objs):
        bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={'ARMATURE', 'MESH'},
                                 add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False,
                                 axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
                                 use_mesh_modifiers=False)
    for o, (hv, hs) in hidden.items():
        o.hide_viewport = hv; o.hide_set(hs, view_layer=vl)


rig = bpy.data.objects['ResidentRig']
parts = [o for o in bpy.data.objects if o.name.startswith('Part_') and o.parent == rig]
export(out / 'Resident_Parts.fbx', rig, parts)
for c in [o for o in bpy.data.objects if o.type == 'ARMATURE' and o.name.startswith('Combo_')]:
    loc = c.location.copy(); c.location = (0, 0, 0)
    export(out / f'Resident_{c.name}.fbx', c, [o for o in c.children if o.type == 'MESH'])
    c.location = loc
print('exported parts', len(parts))
