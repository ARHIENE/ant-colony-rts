"""Build resident-animations.blend (rig + 30 shared actions, no meshes, no facilities) and export FBX.

Actions come from art/resident-anim/resident-interactions.blend (already reach-corrected there:
hands moved 0.30 forward for work clips). Bone names/rest pose are identical, so they play as-is.
"""
import bpy, sys, json, importlib
sys.path.insert(0, r"E:\Git\ant\art\resident-kit\scripts")
import kit_common as K
importlib.reload(K)

CLIPS = json.loads(open(r"E:\Git\ant\art\resident-anim\clips.json", encoding="utf-8").read())

scene = K.empty_file()
scene.name = 'Resident_Animations'
rig = K.make_rig('ResidentRig', K.collection('Rig'))
with bpy.data.libraries.load(K.SRC_ANIM) as (a, b):
    b.actions = [n for n in a.actions if n.startswith('Resident|')]
actions = {act.name.split('|')[1]: act for act in b.actions}
missing = [c for c in CLIPS if c not in actions]
assert not missing, missing
not_reached = [n for n in ('Hammer', 'BenchWork', 'Treat', 'Stir') if not actions[n].get('reach_corrected')]
rig.animation_data_create()
for name, act in actions.items():
    act.use_fake_user = True
    act['loop'] = CLIPS[name]['loop']
    act['frames'] = CLIPS[name]['frames']
rig.animation_data.action = actions['Idle']
rig.animation_data.action_slot = actions['Idle'].slots[0]
scene.frame_start, scene.frame_end = 1, CLIPS['Idle']['frames'] + 1
bpy.ops.wm.save_as_mainfile(filepath=str(K.KIT / 'resident-animations.blend'))

out = K.KIT / 'exports' / 'anim'
out.mkdir(parents=True, exist_ok=True)


def export(path, objs, anim):
    vl = scene.view_layers[0]
    for o in scene.objects: o.select_set(o in objs, view_layer=vl)
    with bpy.context.temp_override(view_layer=vl, active_object=rig, selected_objects=objs):
        bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={'ARMATURE', 'MESH'},
                                 add_leaf_bones=False, use_armature_deform_only=False, bake_anim=anim,
                                 bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                                 bake_anim_simplify_factor=0, axis_forward='-Z', axis_up='Y',
                                 apply_scale_options='FBX_SCALE_UNITS')


for name in CLIPS:
    act = actions[name]
    rig.animation_data.action = act
    rig.animation_data.action_slot = act.slots[0]
    scene.frame_start, scene.frame_end = 1, CLIPS[name]['frames'] + 1
    export(out / f'Resident@{name}.fbx', [rig], True)
rig.animation_data.action = actions['Idle']
rig.animation_data.action_slot = actions['Idle'].slots[0]
print('actions', len(actions), 'exported', len(CLIPS), 'not reach-corrected', not_reached)
