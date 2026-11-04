"""Build critter-kit.blend (4 species, rig + actions) and export FBX. Run in Blender (MCP or -b)."""
import bpy, sys, json, importlib
from pathlib import Path
sys.path.insert(0, r"E:\Git\ant\art\critter-kit\scripts")
import critters as C
importlib.reload(C)

KIT = Path(r"E:\Git\ant\art\critter-kit")
bpy.ops.wm.read_homefile(use_empty=True)
scene = bpy.context.scene; scene.name = 'Critter_Kit'; scene.render.fps = 24
manifest = {}
for i, (name, fn) in enumerate(C.SPECIES.items()):
    col = bpy.data.collections.new(name); scene.collection.children.link(col)
    rig = fn(col, (i * 2.2, 0, 0))
    acts = C.build_actions(rig, name)
    manifest[name] = {'rig': rig.name, 'bones': [b.name for b in rig.data.bones], 'meshes': len(rig.children),
                      'clips': {c: {'frames': C.CLIPS[c][0], 'loop': C.CLIPS[c][1]} for c in acts}}
bpy.ops.wm.save_as_mainfile(filepath=str(KIT / 'critter-kit.blend'))

out = KIT / 'exports'; out.mkdir(exist_ok=True)
vl = scene.view_layers[0]
for name in C.SPECIES:
    rig = bpy.data.objects[name + 'Rig']; keep = rig.location.copy(); rig.location = (0, 0, 0)
    meshes = [o for o in rig.children if o.type == 'MESH']

    def export(path, objs, anim):
        for o in scene.objects: o.select_set(o in objs, view_layer=vl)
        with bpy.context.temp_override(view_layer=vl, active_object=rig, selected_objects=objs):
            bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={'ARMATURE', 'MESH'},
                                     add_leaf_bones=False, bake_anim=anim, bake_anim_use_all_actions=False,
                                     bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0,
                                     axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS')
    act0 = rig.animation_data.action; rig.animation_data.action = None
    for pb in rig.pose.bones: pb.location = (0, 0, 0); pb.rotation_euler = (0, 0, 0)
    export(out / f'{name}.fbx', [rig] + meshes, False)
    for clip in manifest[name]['clips']:
        act = bpy.data.actions[f'{name}|{clip}']
        rig.animation_data.action = act; rig.animation_data.action_slot = act.slots[0]
        scene.frame_start, scene.frame_end = 1, C.CLIPS[clip][0] + 1
        export(out / f'{name}@{clip}.fbx', [rig], True)
    rig.animation_data.action = act0; rig.animation_data.action_slot = act0.slots[0]
    rig.location = keep
(KIT / 'critter-manifest.json').write_text(json.dumps(manifest, indent=1), encoding='utf-8')
print({k: (v['meshes'], list(v['clips'])) for k, v in manifest.items()})
